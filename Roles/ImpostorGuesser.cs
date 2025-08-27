using AmongUs.GameOptions;
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

namespace BanMod;

public static class ImpostorGuesser
{
    public static byte PhantomPlayerId;
    public static void SendPhantomPlayerMessage()
    {
        if (!Options.ImpostorGuess.GetBool()) return;
        var allPlayers = BanMod.AllPlayerControls;

        var phantomPlayer = allPlayers
            .FirstOrDefault(p => p.Data != null && !p.Data.IsDead && Phantom(p));

        if (phantomPlayer == null)
        {
            // Nessun Phantom trovato, esci o gestisci il caso
            return;
        }

        // Ora puoi prendere il suo id
        byte phantomPlayerId = phantomPlayer.PlayerId;

        // Assegna anche alla variabile statica se vuoi tenerlo a livello globale
        PhantomPlayerId = phantomPlayerId;

        if (phantomPlayer == null || phantomPlayer.Data == null || phantomPlayer.Data.IsDead) return;

        string msg = string.Format(GetString("PhantomInfo"));
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, PhantomPlayerId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, PhantomPlayerId, GetString("PhantomTitle"));
            MessageBlocker.UpdateLastMessageTime();

        }

    }
    public static void SendPhantomPlayerMessageTest()
    {
        string msg = string.Format(GetString("PhantomInfo"));

        {
            Utils.SendMessage(msg, 255, GetString("PhantomTitle"));
            MessageBlocker.UpdateLastMessageTime();

        }
    }
}