using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Data;
using InnerNet;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using UnityEngine;
using static BanMod.ImmortalManager;
using static BanMod.Utils;

namespace BanMod;


[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
static class OnPlayerJoinedPatch
{

    public static bool IsDisconnected(this ClientData client)
    {
        
        var __instance = AmongUsClient.Instance;
        for (int i = 0; i < __instance.allClients.Count; i++)
        {
            ClientData clientData = __instance.allClients[i];
            if (clientData.Id == client.Id)
            {
                return true;
            }
        }
        return false;
    }
    
    static bool IsPlayerFriend(this ClientData client)
    {
        var __instance = FriendsListManager.Instance;
        {
            if (IsPlayerFriend == FriendsListManager.Instance.IsPlayerFriend)
            {
                return true;
            }
        }

        return false;

    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    static class OnPlayerJoined_ClientData_Patch
    {
        public static void Postfix([HarmonyArgument(0)] ClientData client)
        {
            Logger.Info($"{client.PlayerName} (ClientID: {client.Id} / FriendCode: {client.FriendCode} / Hashed PUID: {client.GetHashedPuid()}) joined the lobby", "Session");

            if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode)
                && AmongUsClient.Instance.AmHost
                && Options.CheckBlockList.GetBool())
            {
                AmongUsClient.Instance.KickPlayer(client.Id, true);
                HudManager.Instance.Notifier.AddDisconnectMessage($"{client.PlayerName} {Translator.GetString("Blocked")}");
            }

            BanManager.CheckBanPlayer(client);
        }
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
static class OnPlayerJoined_PlayerControl_Patch
{
    public static void Postfix(PlayerControl __instance)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (__instance == null)
        {
            Logger.Info("PlayerControl __instance è null, salto coroutine.");
            return;
        }

        var friendCode = __instance.Data?.FriendCode;
        if (Options.RetoreModdedName.GetBool())
            if (!string.IsNullOrEmpty(friendCode) && ExtendedPlayerControl.moddedNamesByFriendCode.ContainsKey(friendCode))
            {

                __instance.StartCoroutine(CallRestoreModdedNameDelayed(__instance));
                Logger.Info($"Programmata verifica nome per PlayerId: {__instance.PlayerId} (FriendCode: {friendCode}) perché presente in moddedNamesByFriendCode.");
            }
            else
            {
                Logger.Info($"Nessun ripristino nome programmato per PlayerId: {__instance.PlayerId}. FriendCode: {friendCode}. Non presente in moddedNamesByFriendCode.");
            }
    }

    private static IEnumerator CallRestoreModdedNameDelayed(PlayerControl playerToRestore)
    {
        yield return new WaitForSeconds(4f); // Attendi 3 secondi

        if (playerToRestore != null && playerToRestore.Data != null)
        {
            // Verifica se il player è uno Shapeshifter e se è attualmente trasformato.
            // Utilizziamo la tua funzione Shapeshifter() e il controllo su CurrentOutfitType.
            if (Shapeshifter(playerToRestore) && playerToRestore.CurrentOutfitType == PlayerOutfitType.Shapeshifted)
            {
                Logger.Info($"Player {playerToRestore.PlayerId} è uno Shapeshifter e ha l'outfit mutato, salto ripristino nome.");
            }
            else
            {
                ExtendedPlayerControl.RestoreModdedName(playerToRestore);
                Logger.Info($"Ripristinato nome per PlayerId: {playerToRestore.PlayerId} (FriendCode: {playerToRestore.Data.FriendCode})");
            }
        }
        else
        {
            Logger.Info($"Impossibile ripristinare il nome: PlayerControl o PlayerData è nullo dopo il ritardo per l'istanza che ha scatenato l'Awake (PlayerId iniziale: {playerToRestore?.PlayerId ?? -1}).");
        }
    }

    // La funzione Shapeshifter che hai fornito
    public static bool Shapeshifter(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Shapeshifter;
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
public static class PlayerControlStartPatch
{
    private static void Postfix(PlayerControl __instance)
    {
        if (__instance.AmOwner && GameStates.isOnlineGame)
        {
            __instance.StartCoroutine(InitialHandshake());

        }
    }
    private static IEnumerator InitialHandshake()
    {
        yield return new WaitForSeconds(5f); // delay iniziale
        //Logger.Info("Invio handshake moddato dopo delay...");
        Utils.SendModdedHandshake();
        //Logger.Info("handshake moddato inviato");

        if (AmongUsClient.Instance.AmHost)
            GuessManager.ResetForNewGame();
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
static class PlayerControlApiPatch
{
    private static float _lastSuccessfulSendTime;
    private static readonly float SendInterval = 59f; // Intervallo di invio in secondi
    private static readonly float RetryInterval = 1f;  // Intervallo tra i tentativi di invio falliti
    private static bool _isCoroutineRunning = false;   // Flag per verificare se la coroutine è già attiva
    private static Coroutine _sendStatusCoroutine;     // Riferimento alla coroutine

    // Metodo chiamato ogni volta che viene eseguito il FixedUpdate
    public static void Postfix(PlayerControl __instance)
    {
        // Avvia la coroutine solo se il client è il proprietario e la coroutine non è già in esecuzione
        // Usiamo la patch del FixedUpdate solo per il controllo di avvio
        if (__instance.AmOwner && !_isCoroutineRunning)
        {
            _isCoroutineRunning = true;
            // IMPORTANTE: Avvia la coroutine su un oggetto persistente come AmongUsClient.Instance
            // in modo che non venga terminata durante le transizioni di scena.
            _sendStatusCoroutine = AmongUsClient.Instance.StartCoroutine(SendStatusLoop());
        }
    }

    // Controlla se è il momento di inviare lo stato del giocatore
    private static bool ShouldSendStatus()
    {
        return Time.time - _lastSuccessfulSendTime >= SendInterval;
    }

    // Coroutine che invia lo stato del giocatore ogni intervallo
    private static IEnumerator SendStatusLoop()
    {
        Logger.Info("SendStatusLoop avviata su un oggetto persistente.");
        try
        {
            while (true)
            {
                // Ottiene il giocatore locale all'inizio di ogni ciclo
                PlayerControl player = PlayerControl.LocalPlayer;

                // Controlla se il gioco è online e se il giocatore è il proprietario
                // Se non lo siamo, attendiamo un momento e riproviamo.
                if (player == null || !player.AmOwner || !GameStates.isOnlineGame)
                {
                    Logger.Info($"Stato attuale: player == null? {player == null}, player.AmOwner? {player?.AmOwner}, GameStates.isOnlineGame? {GameStates.isOnlineGame}. Attendo e riprovo.");
                    yield return new WaitForSeconds(RetryInterval);
                    continue; // Torna all'inizio del ciclo per riprovare
                }

                // A questo punto, abbiamo un riferimento valido al giocatore e siamo online.
                // Controlla se è il momento di inviare i dati
                if (ShouldSendStatus())
                {
                    bool sentSuccessfully = false;
                    yield return SendStatus(player, success => sentSuccessfully = success);

                    if (sentSuccessfully)
                    {
                        _lastSuccessfulSendTime = Time.time;
                        // Dopo un invio riuscito, aspetta l'intervallo completo prima di un nuovo invio
                        yield return new WaitForSeconds(SendInterval);
                    }
                }
                else
                {
                    // Se non è ancora il momento di inviare, fa una breve pausa e controlla di nuovo
                    yield return new WaitForSeconds(RetryInterval);
                }
            }
        }
        finally
        {
            // Imposta il flag a false quando la coroutine è completata o interrotta
            _isCoroutineRunning = false;
            _sendStatusCoroutine = null;
            Logger.Info("SendStatusLoop terminata.");
        }
    }

    // Coroutine per inviare lo stato del giocatore
    private static IEnumerator SendStatus(PlayerControl player, Action<bool> onComplete)
    {
        string playerName = ExtendedPlayerControl.GetOriginalNameByPlayer(player);
        string friendCode = PlayerPrefs.GetString("PlayerCODE");
        bool isOnline = true;
        bool shareLobby = Options.sharelobby.GetBool();
        bool success = false;

        // Controlla se il client è l'host e se la condivisione è attiva
        if (player.AmOwner && shareLobby)
        {
            // Se siamo in una lobby, i dati sono completi
            //if (GameStates.IsLobby)
            {
                string gameCode = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
                int playerInLobby = GetCurrentLobbyPlayerCount();

                yield return PlayerAPI.SendPlayerStatusCoroutine(
                    playerName, friendCode, gameCode, Utils.GetRegionName(),
                    LanguageUtils.GetLanguageName(LanguageUtils.GetCurrentGameOptions()),
                    playerInLobby, isOnline, shareLobby
                );
                Logger.Info($"Host - Invio completo in lobby: {playerName}, {friendCode}, {gameCode}, {playerInLobby} players");
                success = true;
            }
        }
        else
        {
            yield return PlayerAPI.SendMinimalPlayerStatusCoroutine1(friendCode, isOnline);
            Logger.Info($"Invio minimale (shareLobby OFF): {friendCode}");
            success = true;
        }

        onComplete(success);
    }

    // Metodo per resettare lo stato. Chiamato al OnDisable per interrompere la coroutine.
    public static void ResetState()
    {
        _lastSuccessfulSendTime = 0f;
        _isCoroutineRunning = false;
        // Ferma esplicitamente la coroutine
        if (_sendStatusCoroutine != null)
        {
            AmongUsClient.Instance.StopCoroutine(_sendStatusCoroutine);
            _sendStatusCoroutine = null;
        }
        Logger.Info("Stato resettato.");
    }
}

//// Questa patch ora serve per fermare la coroutine quando il PlayerControl è disabilitato
//// (es. al disconnettersi dal gioco), ma l'avvio su AmongUsClient la rende persistente
//// tra i cambi di scena.
//[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.OnDisable))]
//static class PlayerControlResetApiPatch
//{
//    public static void Postfix(PlayerControl __instance)
//    {
//        if (__instance.AmOwner)
//        {
//            PlayerControlApiPatch.ResetState();
//        }
//    }
//}
[HarmonyPatch(typeof(ControllerManager), nameof(ControllerManager.ResetAll))]
public static class UiControllerResetAllPatch
{
    public static void Postfix()
    {
        {
            PlayerControlApiPatch.ResetState();
        }
    }
}