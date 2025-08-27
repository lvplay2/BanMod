using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using GooglePlayGames;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using InnerNet;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.SceneManagement;
using static BanMod.BanMenuButtonsPatch;
using static BanMod.Utils;

namespace BanMod;

[BepInPlugin(PluginGuid, "BanMod", PluginVersion)]
//[BepInIncompatibility("MalumMenu")]
[BepInProcess("Among Us.exe")]
public partial class BanMod : BasePlugin
{
    public Harmony Harmony { get; } = new(PluginGuid);
    public static string modVersion = "2.0.3";
    public const string PluginGuid = "com.GianniBart.BanMod";
    public const string PluginVersion = "2.0.3";
    public const string VersionRequired = PluginVersion;
    public static Version version = Version.Parse(PluginVersion);
    public static List<string> supportedAU = new List<string> { "2025.5.23" };
    public static readonly string ModName = "BanMod";
    public static ManualLogSource Logger;
    public static MenuUI1 menuUI1;
    public static MenuUI2 menuUI2;
    public static HashSet<byte> playersKilledByCommand = new HashSet<byte>();
    public static Dictionary<byte, bool> originalIsDeadStates = new Dictionary<byte, bool>();
    public static Dictionary<byte, float> playerDeathTimes = new Dictionary<byte, float>();
    public static readonly HashSet<byte> ShieldedPlayers = new HashSet<byte>();
    public static HashSet<int> PlayersKilledByKillCommand = new HashSet<int>();
    public static readonly HashSet<byte> UnreportableBodies = [];
    public static float protectionReapplyInterval = 20.0f;
    public static Dictionary<byte, float> lastProtectionReapplyTime = new Dictionary<byte, float>();
    public static bool IsChatCommand;
    public static bool isChatCommand = false;
    public static Dictionary<byte, PlayerState> PlayerStates = [];
    public static bool IntroDestroyed;
    public static int UpdateTime;
    public static string credentialsText;
    public static int ProtectedPlayerId = -1;
    public static OptionBackupData RealOptionsData;
    public static string InitiallyProtectedFriendCode = null;
    public static string FirstDeadFriendCode = null;
    public static string FriendCodeToRemoveShield = null;
    public static readonly HashSet<int> ModdedClients = new();
    public float Timer { get; set; }
    public static readonly List<(string Message, byte ReceiverID, string Title)> MessagesToSend = [];
    public static readonly Dictionary<byte, Color32> PlayerColors = [];
    public static ConfigEntry<int> MessageWait { get; private set; }
    public static ConfigEntry<string> WebhookUrl { get; private set; }
    public static bool CheckBanPlayer;
    public static readonly Dictionary<byte, string> AllPlayerNames = [];
    public static ConfigEntry<string> HideColor { get; private set; }
    public const string ModColor = "#FFA500";
    public static readonly Dictionary<int, int> SayStartTimes = [];
    public static readonly Dictionary<int, int> SayBanwordsTimes = [];
    public static readonly Dictionary<int, string> AllClientRealNames = [];
    public static readonly bool ShowinfoButton = true;
    public static readonly bool ShowWebsiteButton = true;
    public static readonly bool ShowlobbyButton = true;
    public static readonly bool ShowUpdateButton = true;
    public static readonly string GitsiteUrl = "https://giannibart.github.io/BanMod/";
    public static readonly string LobbysiteUrl = "https://banmod.online/";
    public static string TabGroup { get; internal set; }

    private static Color? _unityModColor;
    public static BanMod Instance;

    public static PlayerControl[] AllPlayerControls
    {
        get
        {
            int count = PlayerControl.AllPlayerControls.Count;
            var result = new PlayerControl[count];
            var i = 0;

            foreach (PlayerControl pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.PlayerId == 255) continue;

                result[i++] = pc;
            }

            if (i == 0) return [];

            Array.Resize(ref result, i);
            return result;
        }
    }

    public static List<PlayerControl> AllAlivePlayerControls
    {
        get
        {
            if (AllPlayerControls == null || AmongUsClient.Instance == null || !AmongUsClient.Instance.IsGameStarted)
                return new List<PlayerControl>();

            return AllPlayerControls
                .Where(p => p != null && p.Data != null && !p.Data.IsDead)
                .ToList();
        }
    }
    public static Color UnityModColor
    {
        get
        {
            if (!_unityModColor.HasValue)
            {
                if (ColorUtility.TryParseHtmlString(ModColor, out var unityColor))
                {
                    _unityModColor = unityColor;
                }
                else
                {
                    return Color.gray;
                }
            }
            return _unityModColor.Value;
        }
    }
    public static ConfigEntry<string> spoofLevel { get; private set; }
    public static ConfigEntry<string> menuKeybind1 { get; private set; }
    public static ConfigEntry<string> menuKeybind2 { get; private set; }
    public static ConfigEntry<string> menuHtmlColor { get; private set; }
    public static ConfigEntry<bool> AktiveChat { get; private set; }
    public static ConfigEntry<bool> ExcludeFriends { get; private set; }
    public static ConfigEntry<string> FriendCode { get; private set; }
    public static ConfigEntry<bool> AddBanToList { get; private set; }
    public override void Load()
    {
        Instance = this;

        ClassInjector.RegisterTypeInIl2Cpp<MenuUI1>();
        ClassInjector.RegisterTypeInIl2Cpp<MenuUI2>();
        ClassInjector.RegisterTypeInIl2Cpp<BanMenuButtonsPatch>();
        ClassInjector.RegisterTypeInIl2Cpp<CustomButtonHandler>();

        spoofLevel = Config.Bind("Client Options", "Level", "");
        menuHtmlColor = Config.Bind("Client Options", "Color", "");
        menuKeybind1 = Config.Bind("Client Options", "Keybind1", "PageUp");
        menuKeybind2 = Config.Bind("Client Options", "Keybind2", "PageDown");
        AktiveChat = Config.Bind("Client Options", "AktiveChat", false);
        ExcludeFriends = Config.Bind("Client Options", "ExcludeFriends", true);
        AddBanToList = Config.Bind("Client Options", "AddBanToList", true);
        TemplateLoader.LoadTemplate("WelcomeTemplate");
        TemplateLoader.LoadTemplate("InfoTemplate");
        Translator.Initialize();
        SpamManager.Initialize();
        BanManager.Initialize();
        MenuUI1.Initialize();
        MenuUI2.Initialize();
        OptionSaver.Initialize();
        PlayerIDManager.Initialize();
        Harmony.PatchAll();
        menuUI1 = AddComponent<MenuUI1>();
        menuUI2 = AddComponent<MenuUI2>();
        Options.Load(); 
        OptionSaver.Load();

    }

}