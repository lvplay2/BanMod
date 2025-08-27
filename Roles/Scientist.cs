using HarmonyLib;
using Il2CppSystem.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;
using static BanMod.ChatCommands;

namespace BanMod;

public static class Scientist
{
    public static void OnMeetingStarted()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!Options.ScientistTime.GetBool()) return;

        MeetingStartTime = Time.time;
        float now = MeetingStartTime;

        foreach (var player in BanMod.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.IsDead) continue;

            if (Scientist(player))
            {
                List<string> messages = new List<string>();

                foreach (var kvp in BanMod.playerDeathTimes)
                {
                    byte deadId = kvp.Key;
                    float deathTime = kvp.Value;

                    PlayerControl deadPlayer = GetPlayerById(deadId);
                    if (deadPlayer == null) continue;

                    int secondsAgo = Mathf.FloorToInt(now - deathTime);
                    string deadName = deadPlayer.Data.PlayerName;

                    string secondsInWords = Utils.NumberToWords(secondsAgo);

                    // --- RIGA MODIFICATA ---
                    string line = $"{deadName} - {secondsAgo} ({secondsInWords}) {Translator.GetString("seconds_suffix")}";
                    messages.Add(line);
                }

                if (messages.Count > 0)
                {
                    string finalMessage = string.Join("\n", messages);
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(finalMessage, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(finalMessage, player.PlayerId, GetString("scientist.playerDiedTime"));
                        MessageBlocker.UpdateLastMessageTime();
                    }
                }
            }
        }
    }
    public static void ScientistCommandHost()
    {
        float now = MeetingStartTime; // Usa il tempo bloccato dal meeting

        List<string> messages = new List<string>();

        foreach (var kvp in BanMod.playerDeathTimes)
        {
            byte deadId = kvp.Key;
            float deathTime = kvp.Value;

            PlayerControl deadPlayer = Utils.GetPlayerById(deadId);
            if (deadPlayer == null) continue;

            // Calcola i secondi trascorsi usando il tempo bloccato
            int secondsAgo = Mathf.FloorToInt(now - deathTime);
            string deadName = deadPlayer.Data.PlayerName;
            string secondsInWords = Utils.NumberToWords(secondsAgo);
            string line = $"{deadName} - {secondsAgo} ({secondsInWords}) {Translator.GetString("seconds_suffix")}";
            messages.Add(line);
        }

        // Costruisci il messaggio finale UNA VOLTA, dopo aver aggiunto tutti i giocatori alla lista
        if (messages.Count > 0) // Controlla se ci sono messaggi da inviare
        {
            string finalMessage = string.Join("\n", messages);
            ShowChat(finalMessage);
        }
        else
        {
            // Puoi aggiungere una logica qui per informare che non ci sono morti, se necessario
            ShowChat(Translator.GetString("no_deaths_recorded")); // Esempio
        }
    }

    public static void ScientistCommand(PlayerControl targetPlayer)
    {
        if (targetPlayer == null || !Scientist(targetPlayer)) return;

        if (MeetingStartTime == 0f)
        {
            Utils.SendMessage(Translator.GetString("scientist.commandNotReady"), targetPlayer.PlayerId, "");
            MessageBlocker.UpdateLastMessageTime();
            return;
        }

        float now = MeetingStartTime; // Usa il tempo bloccato dal meeting

        List<string> messages = new List<string>();

        foreach (var kvp in BanMod.playerDeathTimes)
        {
            byte deadId = kvp.Key;
            float deathTime = kvp.Value;

            PlayerControl deadPlayer = GetPlayerById(deadId);
            if (deadPlayer == null) continue;

            // Calcola i secondi trascorsi usando il tempo bloccato
            int secondsAgo = Mathf.FloorToInt(now - deathTime);
            string deadName = deadPlayer.Data.PlayerName;
            string secondsInWords = Utils.NumberToWords(secondsAgo);
            string line = $"{deadName} - {secondsAgo} ({secondsInWords}) {Translator.GetString("seconds_suffix")}";
            messages.Add(line);
        }

        // Costruisci il messaggio finale UNA VOLTA, dopo aver aggiunto tutti i giocatori alla lista
        if (messages.Count > 0)
        {
            string finalMessage = string.Join("\n", messages);
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(finalMessage, targetPlayer.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(finalMessage, targetPlayer.PlayerId, GetString("scientist.playerDiedTime"));
                MessageBlocker.UpdateLastMessageTime();
            }
        }
        else
        {
            // Se non ci sono morti registrate, invia un messaggio appropriato
            Utils.SendMessage(Translator.GetString("scientist.noDeathsYet"), targetPlayer.PlayerId, "");
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static void SendScientistMessage()
    {
        if (!Options.ScientistTime.GetBool()) return;

        // Trova lo scienziato una sola volta, fuori dal ciclo
        PlayerControl scientist = BanMod.AllPlayerControls
            .FirstOrDefault(p => p != null && p.Data != null && Scientist(p));

        // Se non c'è uno scienziato, non fare nulla
        if (scientist == null) return;

        string msg1 = string.Format(GetString("ScientistInfo"));
        byte scientistPlayerId = scientist.PlayerId;

        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg1, scientistPlayerId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg1, scientistPlayerId, GetString("Scientist"));
            MessageBlocker.UpdateLastMessageTime();

        }

    }
    public static void SendScientistMessageTest()
    {

        string msg1 = string.Format(GetString("ScientistInfo"));

        {
            Utils.SendMessage(msg1, 255, GetString("Scientist"));
            MessageBlocker.UpdateLastMessageTime();

        }

    }
}