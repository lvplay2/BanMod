using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using AmongUs.QuickChat;
using BepInEx.Unity.IL2CPP.Utils;
using Epic.OnlineServices;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMono.Security.Interface;
using Il2CppSystem;
using Il2CppSystem.Linq;
using Il2CppSystem.Security.Cryptography;
using InnerNet;
using MS.Internal.Xml.XPath;
using Rewired;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using UnityEngine.UIElements.UIR;
using static BanMod.ChatCommands;
using static BanMod.ExtendedPlayerControl;
using static BanMod.Translator;
using static BanMod.Utils;
using static BanMod.ImmortalManager;
using static InnerNet.InnerNetClient;
using static Rewired.Controller;
using static UnityEngine.AudioSettings;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ParticleSystem.PlaybackState;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
using static UnityEngine.UIElements.UIR.Allocator2D;
using Action = System.Action;
using Array = System.Array;
using Exception = System.Exception;

namespace BanMod;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
public static class PingTracker_Update
{
    public static void Postfix(PingTracker __instance)
    {
        __instance.text.alignment = TextAlignmentOptions.Center;
        __instance.aspectPosition.DistanceFromEdge = new Vector3(-0f, 0.50f, 0f);
        __instance.text.text = $"{Utils.getColoredPingText(AmongUsClient.Instance.Ping)}";
    }
}


[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public static class VersionShower_Start
{
    private static void Postfix(VersionShower __instance)
    {
        BanMod.credentialsText = $"<b><size=120%><color={BanMod.ModColor}>{BanMod.ModName}</color> v{BanMod.PluginVersion}</b>\n<color=#a54aff>By <color=#f34c50>Bart</color>";
        var credentials = UnityEngine.Object.Instantiate(__instance.text);
        credentials.text = BanMod.credentialsText;
        credentials.alignment = TextAlignmentOptions.Left;
        credentials.transform.position = new Vector3(1f, 2.67f, -2f);
        credentials.fontSize = credentials.fontSizeMax = credentials.fontSizeMin = 2f;
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class HudManager_Update
{
    public static void Postfix(HudManager __instance)
    {
        __instance.ShadowQuad.gameObject.SetActive(!Utils.fullBrightActive());

        if (Utils.chatUiActive())
            __instance.Chat.gameObject.SetActive(true);
        else
        {
            Utils.closeChat();
            __instance.Chat.gameObject.SetActive(false);
        }
    }
}

[HarmonyPatch(typeof(AprilFoolsMode), nameof(AprilFoolsMode.ShouldFlipSkeld))]
public static class ShouldShowTogglePatch
{
    public static void Postfix(ref bool __result)
    {
        __result = Options.dleks.GetBool();
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.Update))]
public static class AmongUsClient_Update
{
    public static void Postfix()
    {
        Spoof.spoofLevel();
    }
}

[HarmonyPatch(typeof(PlayerBanData), nameof(PlayerBanData.BanMinutesLeft), MethodType.Getter)]
public static class RemoveDisconnectPenalty_PlayerBanData_BanMinutesLeft_Postfix
{
    public static void Postfix(PlayerBanData __instance, ref int __result)
    {
        __instance.BanPoints = 0f;
        __result = 0;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckName))]
class PlayerControlCheckNamePatch
{
    public static void Postfix(PlayerControl __instance, ref string playerName)
    {
        playerName = __instance.Data.PlayerName ?? playerName;
        if (!AmongUsClient.Instance.AmHost || !GameStates.IsLobby) return;
        if (BanManager.CheckDenyNamePlayer(__instance, playerName)) return;

    }
}

[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
class ModManagerLateUpdatePatch
{
    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();

    }
}


