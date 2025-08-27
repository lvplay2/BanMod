using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using InnerNet;

namespace BanMod;

public static class BanManager
{
    private const string DenyNameListPath = "./BAN_DATA/DENIED/DenyName.txt";
    private const string BanListPath = "./BAN_DATA/DENIED/BanList.txt";

    public static void Initialize()
    {
        try
        {
            Directory.CreateDirectory("BAN_DATA/DENIED");

            if (!File.Exists(BanListPath))
            {
                File.Create(BanListPath).Close();
            }

            if (!File.Exists(DenyNameListPath))
            {
                File.Create(DenyNameListPath).Close();
            }

        }
        catch (Exception)
        {
        }
    }
    public static bool CheckDenyNamePlayer(PlayerControl player, string name)
    {

        if (!AmongUsClient.Instance.AmHost || !Options.ApplyDenyNameList.GetBool()) return false;

        try
        {
            Directory.CreateDirectory("BAN_DATA/DENIED");
            if (!File.Exists(DenyNameListPath)) File.Create(DenyNameListPath).Close();
            using StreamReader sr = new(DenyNameListPath);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                name = name.Trim().ToLower();
                if (line.Contains("Amogus"))
                {
                    AmongUsClient.Instance.KickPlayer(player.OwnerId, false);
                    return true;
                }
                if (line.Contains("Amogus V"))
                {
                    AmongUsClient.Instance.KickPlayer(player.OwnerId, false);
                    return true;
                }

                if (Regex.IsMatch(name, line))
                {
                    AmongUsClient.Instance.KickPlayer(player.OwnerId, false);
                    return true;
                }
            }
            return false;
        }
        catch (Exception )
        {
            return true;
        }
    }

    public static string GetResourcesTxt(string path)
    {
        var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
        stream.Position = 0;
        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static string GetHashedPuid(this ClientData player)
    {
        if (player == null) return string.Empty;
        string puid = player.ProductUserId;
        using SHA256 sha256 = SHA256.Create();
        byte[] sha256Bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(puid));
        string sha256Hash = BitConverter.ToString(sha256Bytes).Replace("-", "").ToLower();

        return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
    }

    public static void AddBanPlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !BanMod.AddBanToList.Value || player == null) return;
        if (!CheckBanList(player.FriendCode, player.GetHashedPuid()))
        {
            if (player.GetHashedPuid() != "" && player.GetHashedPuid() != null && player.GetHashedPuid() != "e3b0cb855")
            {
                File.AppendAllText(BanListPath, $"{player.FriendCode},{player.GetHashedPuid()},{player.PlayerName.RemoveHtmlTags()}\n");
                HudManager.Instance.Notifier.AddDisconnectMessage(string.Format((player.PlayerName) + Translator.GetString("PlayerinBanList")));

            }
            else HudManager.Instance.Notifier.AddDisconnectMessage(string.Format((player.PlayerName) + Translator.GetString("PlayerNotinBanList")));
        }
    }


    public static void CheckBanPlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        string friendcode = player?.FriendCode;
        if (Options.CheckFriendCode.GetBool() && friendcode?.Length < 10)
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            HudManager.Instance.Notifier.AddDisconnectMessage(string.Format((player.PlayerName) + Translator.GetString("PlayerCodeInvalid")));
            return;
        }


        if (Options.CheckFriendCode.GetBool() && friendcode?.Count(c => c == '#') != 1)
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            HudManager.Instance.Notifier.AddDisconnectMessage(string.Format((player.PlayerName) + Translator.GetString("PlayerCodeInvalid")));
            return;
        }

        if (!AmongUsClient.Instance.AmHost || !Options.CheckBanList.GetBool()) return;
        const string pattern = @"[\W\d]";
        if (Regex.IsMatch(friendcode[..friendcode.IndexOf("#", StringComparison.Ordinal)], pattern))
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            HudManager.Instance.Notifier.AddDisconnectMessage(string.Format((player.PlayerName) + Translator.GetString("PlayerCodeInvalid")));
            return;
        }

        if (CheckBanList(player?.FriendCode, player?.GetHashedPuid()))
        {
            AmongUsClient.Instance.KickPlayer(player.Id, true);
            HudManager.Instance.Notifier.AddDisconnectMessage(string.Format((player.PlayerName) + Translator.GetString("PlayerIsInBanList")));
            return;
        }

    }

    public static bool CheckBanList(string code, string hashedpuid = "")
    {
        if (!AmongUsClient.Instance.AmHost)
            return false;
        bool OnlyCheckPuid = false;
        if (code == "" && hashedpuid != "") OnlyCheckPuid = true;
        else if (code == "") return false;
        try
        {
            Directory.CreateDirectory("BAN_DATA/DENIED");
            if (!File.Exists(BanListPath)) File.Create(BanListPath).Close();
            using StreamReader sr = new(BanListPath);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (!OnlyCheckPuid)
                    if (line.Contains(code))
                        return true;
                if (line.Contains(hashedpuid)) return true;
            }
        }
        catch (Exception )
        {
        }

        return false;
    }

}


[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
class BanMenuSelectPatch
{
    public static void Postfix(BanMenu __instance, int clientId)
    {
        if (!AmongUsClient.Instance.AmHost)
            return;
        ClientData recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
        if (recentClient == null) return;
        if (!BanManager.CheckBanList(recentClient.FriendCode, recentClient.GetHashedPuid()))
            __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();

    }

}