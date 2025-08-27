using HarmonyLib;
using Il2CppSystem.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod;

public static class ImmortalManager
{
    public static readonly HashSet<byte> ImmortalPlayers = new();
    public static readonly Dictionary<byte, float> TaskCompletionTimes = new(); // playerId -> tempo completamento
    public static bool immortalAssigned = false;

    public static byte? ImmortalPlayerId = null;

    public static void OnPlayerCompletedTasks(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (player == null || player.Data == null || player.Data.IsDead) return;
        if (!Options.EnableImmortal.GetBool()) return;
        if (immortalAssigned) return;

        if (PlayerTask.AllTasksCompleted(player))
        {
            if (!TaskCompletionTimes.ContainsKey(player.PlayerId))
            {
                TaskCompletionTimes[player.PlayerId] = Time.time; // registra il tempo di completamento
                Debug.Log($"[ImmortalManager] Player {player.PlayerId} finished all tasks at time {Time.time}");
            }

            TryAssignImmortal();
        }
    }

    private static void TryAssignImmortal()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (immortalAssigned) return;
        if (TaskCompletionTimes.Count == 0) return;

        var orderedFinishers = TaskCompletionTimes
            .OrderBy(kv => kv.Value)
            .Select(kv => kv.Key)
            .Where(pid => {
                var p = BanMod.AllPlayerControls.FirstOrDefault(pc => pc.PlayerId == pid);
                return p != null && !(Utils.Engineer(p));
            })
            .ToList();
        byte? immortalCandidate = null;

        if (orderedFinishers.Count == 1)
        {
            immortalCandidate = orderedFinishers[0];
        }
        else
        {
            if (orderedFinishers[0] == Guesser.SpecialKillerId)
            {
                // Se il primo è SpecialKiller e c'è almeno un altro finisher
                immortalCandidate = orderedFinishers.Count > 1 ? orderedFinishers[1] : orderedFinishers[0];
            }
            else
            {
                immortalCandidate = orderedFinishers[0];
            }
        }
        if (immortalCandidate.HasValue)
        {
            ImmortalPlayers.Add(immortalCandidate.Value);
            ImmortalPlayerId = immortalCandidate.Value;
            immortalAssigned = true;
        }

        Debug.Log($"[ImmortalManager] Player {immortalCandidate.Value} assigned as Immortal.");

        HudManager.Instance.Notifier.AddDisconnectMessage("Immortal Added");
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(GetString("immortaladded"));
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(GetString("immortaladded"), 255, GetString("immortaltitle"));
            MessageBlocker.UpdateLastMessageTime();
        }
        var player = BanMod.AllPlayerControls.ToList().FirstOrDefault(p => p.PlayerId == immortalCandidate.Value);
        if (Options.sendtoimmortal.GetBool())
        {
            string msg = GetString("immortal");
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, immortalCandidate.Value);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg, immortalCandidate.Value, GetString("immortaltitle"));
                MessageBlocker.UpdateLastMessageTime();
            }

        }

        // Aggiungi scudo all'immortale
        if (player != null && !BanMod.ShieldedPlayers.Contains(player.PlayerId))
        {
            BanMod.ShieldedPlayers.Add(player.PlayerId);
            Utils.ForceProtect(player, overrideExisting: true);
            Logger.Info($"[ImmortalManager] Shield applied to Immortal {player.PlayerId}");
        }
    }
    public static bool IsImmortal(byte playerId)
    {
        return Options.EnableImmortal.GetBool() && ImmortalPlayers.Contains(playerId);
    }

    public static bool IsImmortal(PlayerControl player)
    {
        return player != null && IsImmortal(player.PlayerId);
    }
    public static void ResetImmortal()
    {
        ImmortalPlayers.Clear();
        TaskCompletionTimes.Clear();
        immortalAssigned = false;
        ImmortalPlayerId = null;
        Debug.Log("[ImmortalManager] Reset for new session.");
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
public static class TaskCompletePatch
{
    public static void Postfix(PlayerControl __instance, uint idx)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = __instance;

        ImmortalManager.OnPlayerCompletedTasks(player); // Questo assegna ImmortalPlayerId e lo aggiunge a ImmortalPlayers

        // Se il giocatore è ora immortale, assicurati che sia in ShieldedPlayers
        if (ImmortalManager.IsImmortal(player))
        {
            if (!BanMod.ShieldedPlayers.Contains(player.PlayerId))
            {
                BanMod.ShieldedPlayers.Add(player.PlayerId);
                Utils.ForceProtect(player, overrideExisting: true);
            }
        }
    }
}