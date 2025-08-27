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
    public static class Guesser
    {
        public static byte SpecialKillerId = 255; // 255 = non assegnato
        public static bool SpecialKillerSelected = false;

        public static void OnStart()
        {
            if (Options.Guess.GetBool() && (!Guesser.SpecialKillerSelected))
                Guesser.SelectSpecialKiller();

            if (Options.ExilerExe.GetBool() && (!Exiler.ExilerSelected))
                Exiler.SelectExiler(); // Poi escludi il killer dal selettore Exiler
        }
        public static void SelectSpecialKiller()
        {
            if (!Options.Guess.GetBool()) return;
            var allPlayers = BanMod.AllPlayerControls;

            // Seleziona tutti i giocatori vivi, ESCLUDENDO l'Exiler (se selezionato)
            var guesserPlayers = allPlayers
                .Where(p => p.Data != null && !p.Data.IsDead && p.PlayerId != Exiler.ExilerId && !Impostor(p) && !Shapeshifter(p) && !Phantom(p) && !Scientist(p) && !Engineer(p))
                .ToList();

            if (guesserPlayers.Count == 0) return;

            var randomPlayer = guesserPlayers[UnityEngine.Random.Range(0, guesserPlayers.Count)];
            SpecialKillerId = randomPlayer.PlayerId;
            SpecialKillerSelected = true;

            // ? Se per caso lo stesso è stato già selezionato come Exiler, riseleziona Exiler
            if (SpecialKillerId == Exiler.ExilerId)
            {
                Exiler.ExilerSelected = false;
                Exiler.SelectExiler();
            }

            if (AmongUsClient.Instance.AmHost)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSpecialKiller, SendOption.Reliable, -1);
                writer.Write(SpecialKillerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                Logger.Info($"[Guesser] Inviato SpecialKillerId = {SpecialKillerId} a tutti i client.");
            }
        }
        public static void SendKillerMessage()
        {
            if (SpecialKillerId == 255) return;
            var allPlayers = BanMod.AllPlayerControls;
            var killer = allPlayers.FirstOrDefault(p => p.PlayerId == SpecialKillerId);
            if (killer == null || killer.Data == null || killer.Data.IsDead) return;
            string msg = string.Format(GetString("GuesserInfo"));
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, SpecialKillerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg, SpecialKillerId, GetString("guesser"));
                MessageBlocker.UpdateLastMessageTime();

            }

        }
        public static void SendKillerMessageTest()
        {
            string msg = string.Format(GetString("GuesserInfo"));

            {
                Utils.SendMessage(msg, 255, GetString("guesser"));
                MessageBlocker.UpdateLastMessageTime();

            }

        }
        public static void ResetSpecialKiller()
        {
            SpecialKillerSelected = false;
        }
    }
}