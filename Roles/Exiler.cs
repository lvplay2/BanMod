using BepInEx.Unity.IL2CPP.Utils;
using Hazel;
using Rewired.UI.ControlMapper;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class Exiler
    {
        public static byte ExilerId = 255; 
        public static bool ExilerSelected = false;
        public static void OnStart()
        {
            if (Options.Guess.GetBool() && (!Guesser.SpecialKillerSelected))
                Guesser.SelectSpecialKiller(); 

            if (Options.ExilerExe.GetBool() && (!Exiler.ExilerSelected))
                Exiler.SelectExiler(); // Poi escludi il killer dal selettore Exiler
        }

        public static void SelectExiler()
        {
            if (!Options.ExilerExe.GetBool()) return;
            var allPlayers = BanMod.AllPlayerControls;

            // Seleziona tutti i giocatori vivi, ESCLUDENDO lo SpecialKiller (se selezionato)
            var alivePlayers = allPlayers
                .Where(p => p.Data != null && !p.Data.IsDead && p.PlayerId != Guesser.SpecialKillerId && !Scientist(p) && !Engineer(p) && !Phantom(p))
                .ToList();

            if (alivePlayers.Count == 0) return;

            var randomPlayer = alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)];
            ExilerId = randomPlayer.PlayerId;
            ExilerSelected = true;

            // ? Se per caso lo stesso è stato già selezionato come SpecialKiller, riseleziona SpecialKiller
            if (ExilerId == Guesser.SpecialKillerId)
            {
                Guesser.SpecialKillerSelected = false;
                Guesser.SelectSpecialKiller();
            }

            if (AmongUsClient.Instance.AmHost)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetExiler, SendOption.Reliable, -1);
                writer.Write(ExilerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                Logger.Info($"[Guesser] Inviato ExilerId = {ExilerId} a tutti i client.");
            }
        }
        public static void SendExilerMessage()
        {
            if (ExilerId == 255) return;
            var allPlayers = BanMod.AllPlayerControls;
            var killer = allPlayers.FirstOrDefault(p => p.PlayerId == ExilerId);
            if (killer == null || killer.Data == null || killer.Data.IsDead) return;

            string msg = string.Format(GetString("ExilerInfo"));
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, ExilerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg, ExilerId, GetString("Exiler"));
                MessageBlocker.UpdateLastMessageTime();

            }
        }
        public static void SendExilerMessageTest()
        {
            string msg = string.Format(GetString("ExilerInfo"));
            {
                Utils.SendMessage(msg, 255, GetString("Exiler"));
                MessageBlocker.UpdateLastMessageTime();

            }


        }
        public static void ResetExiler()
        {
            ExilerSelected = false;
        }
    }
}