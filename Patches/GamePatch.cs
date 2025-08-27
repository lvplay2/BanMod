using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppSystem.Linq;
using InnerNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.ExtendedPlayerControl;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.OnDestroy))]
public static class GameEndPatch
{
    private static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        RPCHandlerPatch.ModdedClients.Clear();
        Logger.Info("handshake moddato eliminato");
        Guesser.ResetSpecialKiller();
        Exiler.ResetExiler();
        ImmortalManager.ResetImmortal();
        ClearGuessNames();
        ClearExiledNames();
        ClearAfkNames();
        ClearDoppelNames();
        ClearVictimNames();
        GuessManager.ResetForNewGame();
        ExilerManager.ResetForNewGame();
        CloseMeetingManager.ResetForNewGame();
        BanMod.playerDeathTimes.Clear();
        BanMod.ShieldedPlayers.Clear();
        BanMod.UnreportableBodies.Clear();
        ChatCommands.ComandoExeUsed = false;
        LobbyStartPatch.hasSentSummary = false;
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
public static class GameStartPatch
{
    private static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        RestoreOriginalNames(); // Ripristina i nomi originali
        RestoreModdedNames();   // Applica i nomi modificati basati sui ruoli
        MatchSummary.Reset();
        TaskTracker.Clear();
        ImpostorTracker.Clear();
        BanMod.InitiallyProtectedFriendCode = null;
        if (Options.ProtectFirst.GetBool())
            if (BanMod.FirstDeadFriendCode != null)
            {
                PlayerControl playerToProtect = BanMod.AllPlayerControls
                    .FirstOrDefault(p => p != null && p.Data != null && p.Data.FriendCode == BanMod.FirstDeadFriendCode);

                if (playerToProtect != null && !playerToProtect.Data.IsDead)
                {
                    Utils.ForceProtect(playerToProtect, overrideExisting: true);
                    // Memorizza chi è stato protetto inizialmente per rimuovere lo scudo dopo il meeting.
                    BanMod.InitiallyProtectedFriendCode = playerToProtect.Data.FriendCode;
                    Logger.Info($"[GameStartPatch] Protezione applicata al primo morto del gioco precedente ({playerToProtect.Data.PlayerName}) fino al primo meeting.");
                }
                else
                {
                    Logger.Info($"[GameStartPatch] Il primo morto del gioco precedente (FriendCode: {BanMod.FirstDeadFriendCode}) non è stato trovato o è morto nella nuova partita.");
                }
                BanMod.FirstDeadFriendCode = null;
            }

        AmongUsClient.Instance.StartCoroutine(WaitForLocalPlayerAndExecute());
    }

    private static IEnumerator WaitForLocalPlayerAndExecute()
    {
        // Aspetta finché LocalPlayer non è disponibile
        while (PlayerControl.LocalPlayer == null)
            yield return null;

        // Aspetta finché i dati non sono disponibili
        while (PlayerControl.LocalPlayer.Data == null)
            yield return null;

        // Aspetta finché il ruolo non è assegnato
        while (!BanMod.AllPlayerControls.All(p => p != null && p.Data != null && (p.roleAssigned || p.Data.Disconnected)))
        yield return null;
        

        if (Options.EngineerFixer.GetBool())
            Engineer.SendEngineerMessage();

        if (Options.ImpostorGuess.GetBool())
            ImpostorGuesser.SendPhantomPlayerMessage();

        if (Options.ScientistTime.GetBool())
            Scientist.SendScientistMessage();

        if (Options.sendkillmessage.GetBool())
        {
            ImpostorManager.DetectImpostors();
            ImpostorNameSender();
        }

        if (Options.Guess.GetBool())
        {
            Guesser.OnStart();
            Guesser.SendKillerMessage();
        }

        if (Options.ExilerExe.GetBool())
        {
            Exiler.OnStart();
            Exiler.SendExilerMessage();
        }

        Debug.Log("[Message: Unity] Operazioni completate dopo l'assegnazione dei ruoli!");
    }
}
[HarmonyPatch(typeof(LogicGameFlowNormal), nameof(LogicGameFlowNormal.CheckEndCriteria))]
public static class CheckEndCriteriaPatch
{
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        // Salva impostori
        TaskTracker.Clear();
        ImpostorTracker.Clear();

        ImpostorTracker.DetectImpostors();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            TaskTracker.UpdatePlayerTask(player);
        }

        var impostorNames = ImpostorTracker.GetImpostors().Select(i => i.PlayerName);
        var taskCount = TaskTracker.GetAllTaskData().Count;

    }
}
[HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
public static class EndGameSavePatch
{
    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var reason = EndGameResult.CachedGameOverReason;

        if (GameManager.Instance.DidHumansWin(reason))
        {
            MatchSummary.CrewmateWin = true;
            MatchSummary.ImpostorWin = false;
        }
        else
        {
            MatchSummary.CrewmateWin = false;
            MatchSummary.ImpostorWin = true;
        }

        MatchSummary.SaveToHistory();
    }
}