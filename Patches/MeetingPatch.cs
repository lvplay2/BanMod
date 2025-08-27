using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppSystem.Linq;
using InnerNet;
using MS.Internal.Xml.XPath;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;
using static UnityEngine.GraphicsBuffer;

namespace BanMod;

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Awake))]
public static class MeetingHudAwakePatch
{
    public static void Prefix(MeetingHud __instance)
    {

        if (!AmongUsClient.Instance.AmHost)
            return;
        if (!Options.sendInfocomand.GetBool())
            return;
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(GetString("ComandInfo"), 255);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(GetString("ComandInfo"), 255, GetString("ComandInfoTitle"));
            MessageBlocker.UpdateLastMessageTime();
        }
    }
}


[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
public static class MeetingHudStartPatch
{
    private static void Postfix(MeetingHud __instance)
    {

        __instance.StartCoroutine(GuessManager.WaitForButtonsAndCreate(__instance));
        __instance.StartCoroutine(ExilerManager.WaitForButtonsAndCreate(__instance));

        if (!AmongUsClient.Instance.AmHost)
            return;
        __instance.StartCoroutine(CloseMeetingManager.WaitForCloseMeetingButton(__instance));

        if (Options.ScientistTime.GetBool())
        {
            Scientist.OnMeetingStarted();
        }


    }

}


[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDisable))]
public static class MeetingHudClosePatch
{
    private static void Postfix()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost) return;
        if (Options.ProtectFirst.GetBool())
            if (BanMod.InitiallyProtectedFriendCode != null) // Controlla il giocatore protetto inizialmente
            {
                PlayerControl playerToUnshield = BanMod.AllPlayerControls
                    .FirstOrDefault(p => p != null && p.Data != null && p.Data.FriendCode == BanMod.InitiallyProtectedFriendCode);

                if (playerToUnshield != null && !playerToUnshield.Data.IsDead)
                {
                    Utils.RemovePlayerFromShield(playerToUnshield);
                    Logger.Info($"[MeetingHudClosePatch] Scudo rimosso dal giocatore protetto inizialmente ({playerToUnshield.Data.PlayerName}) dopo il meeting.");
                }
                // Resetta la variabile *solo dopo* aver tentato di rimuovere lo scudo.
                BanMod.InitiallyProtectedFriendCode = null;
            }
        BanMod.playerDeathTimes.Clear();
        GuessManager.CleanupAfterMeeting();
        ExilerManager.CleanupAfterMeeting();
        CloseMeetingManager.CleanupAfterMeeting();


        MessageRetryHandler.ClearQueue();
        BanMod.PlayersKilledByKillCommand.Clear();
        foreach (var player in BanMod.AllAlivePlayerControls)
        {
            if (player?.Data == null) continue;

            if (ImmortalManager.IsImmortal(player))
            {
                if (!BanMod.ShieldedPlayers.Contains(player.PlayerId))
                {
                    BanMod.ShieldedPlayers.Add(player.PlayerId);
                    Utils.ForceProtect(player, overrideExisting: true);
                }
                Logger.Info($"[MeetingHudClosePatch] Reinserito scudo a immortale {player.PlayerId}", null);
            }
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
class ReportDeadBodyPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] NetworkedPlayerInfo target)
    {
        if (!AmongUsClient.Instance.AmHost)
            return true;

        try
        {
            if (target != null && BanMod.UnreportableBodies.Contains(target.PlayerId))
            {
                DeadBody[] allBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
                DeadBody body = allBodies.FirstOrDefault(b => b.ParentId == target.PlayerId);

                if (body != null)
                {
                    UnityEngine.Object.Destroy(body.gameObject);
                }

                return false;
            }

        }
        catch (Exception)
        {
            // Opzionale: logga errore
        }

        return true;
    }
}
[HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.SetCosmetics))]
public static class PlayerVoteAreaPatch
{
    private static void Postfix(PlayerVoteArea __instance, ref NetworkedPlayerInfo playerInfo)
    {
        var player = playerInfo.Object;
        if (player == null || player.Data == null || __instance.NameText == null || PlayerControl.LocalPlayer == null)
            return;

        var local = PlayerControl.LocalPlayer;

        // ?? Nome base
        string displayName = player.Data.PlayerName;

        // ?? Aggiungi ID se attivo
        if (Options.namewithid.GetBool())
        {
            string friendCode = player.Data.FriendCode;
            string moddedName = ExtendedPlayerControl.moddedNamesByFriendCode.TryGetValue(friendCode, out string nameFromCode)
                ? nameFromCode
                : player.Data.PlayerName;

            displayName = $"{moddedName} <color=#FFA500>(Id{player.PlayerId})</color>";
        }

        if (Options.Taskremain.GetBool() && AmongUsClient.Instance.IsGameStarted)
        {
            bool isLocalImpostor = local.Data.Role.TeamType == RoleTeamTypes.Impostor;
            bool isLocalDead = local.Data.IsDead;
            bool isSamePlayer = local.PlayerId == player.PlayerId;
            bool isTargetCrewmate = player.Data.Role.TeamType != RoleTeamTypes.Impostor;

            bool showTasks = false;
            if (isLocalImpostor && Options.ImpostorGuess.GetBool()) return;
            if (isLocalImpostor)
            {
                // Impostore vede solo crewmate (non se stesso)
                showTasks = isTargetCrewmate;
            }
            else
            {
                // Locale è crewmate
                if (isLocalDead)
                {
                    // Morto: vede tutti i crewmate
                    showTasks = isTargetCrewmate;
                }
                else
                {
                    // Vivo: vede solo se stesso
                    showTasks = isSamePlayer;
                }
            }

            if (showTasks)
            {
                int totalTasks = player.Data.Tasks.Count;
                int tasksDone = 0;
                foreach (var task in player.Data.Tasks)
                {
                    if (task.Complete)
                        tasksDone++;
                }

                displayName += $" <color=#00FFFF>({tasksDone}/{totalTasks})</color>";
            }
        }

        __instance.NameText.text = displayName;
    }
}
