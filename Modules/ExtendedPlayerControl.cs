using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using InnerNet;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using static BanMod.Utils;
using static Rewired.Utils.Classes.Utility.ObjectInstanceTracker;

namespace BanMod;

static class ExtendedPlayerControl
{

    public static ClientData GetClient(this PlayerControl player)
    {

        try
        {
            return AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
        }
        catch
        {
            return null;
        }
    }

    public static int GetClientId(this PlayerControl player)
    {

        if (player == null) return -1;
        var client = player.GetClient();
        return client?.Id ?? -1;
    }
    public static string GetFriendCode(this PlayerControl player)
    {
        if (player == null) return null;
        var client = player.GetClient();
        return client?.FriendCode;
    }
    public static Vector2 Pos(this PlayerControl pc)
    {
        return new(pc.transform.position.x, pc.transform.position.y);
    }

    public static bool IsAlive(this PlayerControl target)
    {
        //In lobby all is alive
        if (GameStates.IsLobby && !GameStates.InGame)
        {
            return true;
        }
        //if target is null, it is not alive
        if (target == null)
        {
            return false;
        }

        //if the target status is alive
        return !BanMod.PlayerStates.TryGetValue(target.PlayerId, out var playerState) || !playerState.IsDead;
    }
    public static string GetRealName(this PlayerControl player, bool isMeeting = false, bool clientData = false)
    {
        if (clientData || player == null)
        {
            var client = player.GetClient();

            if (client != null)
            {
                if (BanMod.AllClientRealNames.TryGetValue(client.Id, out var realname))
                {
                    return realname;
                }
                return player.GetClient().PlayerName;
            }
        }
        return isMeeting || player == null ? player?.Data?.PlayerName : player?.name;
    }
    public static string GetOriginalNameByPlayer(PlayerControl player)
    {
        if (player == null || string.IsNullOrEmpty(player.FriendCode))
        {
            return null; // O una stringa vuota, a seconda della logica desiderata
        }

        string friendCode = player.FriendCode; // Ottieni il FriendCode del giocatore target

        // Cerca il nome originale nel dizionario
        if (originalNamesByFriendCode.TryGetValue(friendCode, out string originalName))
        {
            return originalName;
        }
        else
        {
            // Se non trovato, puoi restituire il nome corrente del giocatore
            // o null/stringa vuota a seconda di ciò che vuoi che accada
            return player.name;
        }
    }
    public static string GetModdedNameByPlayer(PlayerControl player)
    {
        if (player == null || string.IsNullOrEmpty(player.FriendCode))
        {
            return null; // O una stringa vuota, a seconda della logica desiderata
        }

        string friendCode = player.FriendCode; // Ottieni il FriendCode del giocatore target

        // Cerca il nome originale nel dizionario
        if (moddedNamesByFriendCode.TryGetValue(friendCode, out string moddedName))
        {
            return moddedName;
        }
        else
        {
            // Se non trovato, puoi restituire il nome corrente del giocatore
            // o null/stringa vuota a seconda di ciò che vuoi che accada
            return GetOriginalNameByPlayer(player);
        }
    }
    public static readonly Dictionary<string, string> originalNamesByFriendCode = new();
    public static void StoreOriginalName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && !originalNamesByFriendCode.ContainsKey(friendCode))
        {
            originalNamesByFriendCode[friendCode] = player.name;
        }
    }

    // Salva i nomi originali di tutti i giocatori
    public static void StoreOriginalNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            StoreOriginalName(player.PlayerId);
        }
    }

    // Ripristina il nome originale di un singolo giocatore tramite ID
    public static void RestoreOriginalName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        // --- INIZIO NUOVA LOGICA DI ESCLUSIONE SHAPESHIFTER ---
        // Se il giocatore è uno Shapeshifter e ha l'outfit mutato, non ripristinare il nome.
        if (Utils.Shapeshifter(player) && player.CurrentOutfitType == PlayerOutfitType.Shapeshifted|| Utils.Phantom(player))
        {
            Logger.Info($"[RestoreOriginalName] Player {player.PlayerId} è uno Shapeshifter mutato, salto ripristino nome.");
            return; // Esci dal metodo senza ripristinare il nome
        }
        // --- FINE NUOVA LOGICA DI ESCLUSIONE SHAPESHIFTER ---

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && originalNamesByFriendCode.TryGetValue(friendCode, out string originalName))
        {
            player.RpcSetName(originalName);
            Logger.Info($"[RestoreOriginalName] Ripristinato nome originale per PlayerId: {player.PlayerId} ({originalName})");
        }
        else
        {
            Logger.Info($"[RestoreOriginalName] Nessun nome originale trovato per PlayerId: {player.PlayerId} (FriendCode: {friendCode ?? "null"}) o FriendCode nullo/vuoto.");
        }
    }

    // Ripristina i nomi originali di tutti i giocatori
    public static void RestoreOriginalNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Logger.Info("[RestoreOriginalNames] Avvio ripristino nomi originali per tutti i giocatori.");
        foreach (var player in BanMod.AllPlayerControls)
        {
            // La logica di esclusione Shapeshifter è già dentro RestoreOriginalName(int playerId)
            RestoreOriginalName(player.PlayerId);
        }
        Logger.Info("[RestoreOriginalNames] Ripristino nomi originali completato.");
    }

    // Cancella il nome originale salvato di un singolo giocatore tramite ID
    public static void ClearOriginalName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode))
        {
            originalNamesByFriendCode.Remove(friendCode);
        }
    }

    // Cancella tutti i nomi originali salvati
    public static void ClearOriginalNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        originalNamesByFriendCode.Clear();
    }

    public static readonly Dictionary<string, string> GuessNamesByFriendCode = new();
    public static void StoreGuessName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && !GuessNamesByFriendCode.ContainsKey(friendCode))
        {
            GuessNamesByFriendCode[friendCode] = player.name;
        }
    }

    // Salva i nomi originali di tutti i giocatori
    public static void StoreGuessNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            StoreGuessName(player.PlayerId);
        }
    }

    // Ripristina il nome originale di un singolo giocatore tramite ID
    public static void RestoreGuessName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && GuessNamesByFriendCode.TryGetValue(friendCode, out string guessName))
        {
            player.RpcSetName(guessName);
        }
    }

    // Ripristina i nomi originali di tutti i giocatori
    public static void RestoreGuessNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            RestoreGuessName(player.PlayerId);
        }
    }

    // Cancella il nome originale salvato di un singolo giocatore tramite ID
    public static void ClearGuessName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode))
        {
            GuessNamesByFriendCode.Remove(friendCode);
        }
    }

    // Cancella tutti i nomi originali salvati
    public static void ClearGuessNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        GuessNamesByFriendCode.Clear();
    }
    public static readonly Dictionary<string, string> moddedNamesByFriendCode = new();
    public static void StoreModdedName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode))
        {
            moddedNamesByFriendCode[friendCode] = player.name;
        }
    }

    // Salva i nomi originali di tutti i giocatori
    public static void StoreModdedNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            StoreModdedName(player.PlayerId);
        }
    }
    public static readonly Dictionary<string, string> ExiledNamesByFriendCode = new();
    public static void StoreExiledName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && !ExiledNamesByFriendCode.ContainsKey(friendCode))
        {
            ExiledNamesByFriendCode[friendCode] = player.name;
        }
    }

    // Salva i nomi originali di tutti i giocatori
    public static void StoreExiledNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            StoreExiledName(player.PlayerId);
        }
    }

    // Ripristina il nome originale di un singolo giocatore tramite ID
    public static void RestoreExiledName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && ExiledNamesByFriendCode.TryGetValue(friendCode, out string ExiledName))
        {
            player.RpcSetName(ExiledName);
        }
    }

    // Ripristina i nomi originali di tutti i giocatori
    public static void RestoreExiledNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            RestoreExiledName(player.PlayerId);
        }
    }

    // Cancella il nome originale salvato di un singolo giocatore tramite ID
    public static void ClearExiledName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode))
        {
            ExiledNamesByFriendCode.Remove(friendCode);
        }
    }

    // Cancella tutti i nomi originali salvati
    public static void ClearExiledNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        ExiledNamesByFriendCode.Clear();
    }
    

    // Ripristina il nome originale di un singolo giocatore tramite ID
    public static void RestoreModdedName(PlayerControl player) // Nuovo overload che accetta PlayerControl
    {
        if (!AmongUsClient.Instance.AmHost) return; // Solo l'host può eseguire questa operazione

        // Controlla che l'istanza del giocatore e i suoi dati siano validi
        if (player == null || player.Data == null)
        {
            Logger.Info("RestoreModdedName(PlayerControl): Giocatore o Player.Data è nullo. Impossibile ripristinare il nome.");
            return;
        }

        // --- INIZIO NUOVA LOGICA DI ESCLUSIONE SHAPESHIFTER ---
        // Se il giocatore è uno Shapeshifter e ha l'outfit mutato, non ripristinare il nome.
        // Questo impedisce di sovrascrivere il nome della trasformazione dello Shapeshifter.
        if (Utils.Shapeshifter(player) && player.CurrentOutfitType == PlayerOutfitType.Shapeshifted)
        {
            Logger.Info($"[RestoreModdedName(PlayerControl)] Player {player.PlayerId} è uno Shapeshifter mutato, salto ripristino nome.");
            return; // Esci dal metodo senza ripristinare il nome
        }
        if (player.Data.IsDead)
        {
            Logger.Info($"[RestoreModdedName(PlayerControl)] Player {player.PlayerId} è morto. Salto ripristino nome.");
            return; // Esci dal metodo senza ripristinare il nome
        }
        // --- FINE NUOVA LOGICA DI ESCLUSIONE SHAPESHIFTER ---

        string friendCode = player.Data.FriendCode; // Accesso al FriendCode tramite player.Data
        if (!string.IsNullOrEmpty(friendCode) && moddedNamesByFriendCode.TryGetValue(friendCode, out string nameToRestore))
        {
            // Ripristina il nome solo se è diverso da quello attuale per evitare RPC inutili
            if (player.name != nameToRestore)
            {
                player.RpcSetName(nameToRestore); // Chiama l'RPC sull'istanza corretta del giocatore
                Logger.Info($"Nome ripristinato per {player.name} (ID: {player.PlayerId}, FriendCode: {friendCode}) a '{nameToRestore}'");
            }
            else
            {
                Logger.Info($"Il nome del giocatore {player.name} (ID: {player.PlayerId}, FC: {friendCode}) è già corretto. Nessun ripristino necessario.");
            }
        }
        else
        {
            Logger.Info($"Nessun nome da ripristinare o FriendCode nullo/vuoto per PlayerId: {player.PlayerId}, FriendCode: {friendCode}.");
        }
    }

    // Mantieni la versione originale che prende un ID, ma falla richiamare l'overload più robusto
    public static void RestoreModdedName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        // Se il giocatore viene trovato, chiama l'overload che accetta PlayerControl
        if (player != null)
        {
            RestoreModdedName(player); // Questo chiamerà l'overload PlayerControl, che ha la logica di esclusione
        }
        else
        {
            Logger.Info($"RestoreModdedName(int): Nessun giocatore trovato con PlayerId: {playerId}.");
        }
    }

    // Mantieni questo metodo per ripristinare tutti i nomi (es. quando l'host rientra in lobby)
    public static void RestoreModdedNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Logger.Info("[RestoreModdedNames] Avvio ripristino nomi moddati per tutti i giocatori.");
        foreach (var player in BanMod.AllPlayerControls)
        {
            RestoreModdedName(player); // Questo chiamerà l'overload PlayerControl, che ha la logica di esclusione
        }
        Logger.Info("[RestoreModdedNames] Ripristino nomi moddati completato.");
    }

    // Cancella il nome originale salvato di un singolo giocatore tramite ID
    public static void ClearModdedName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode))
        {
            moddedNamesByFriendCode.Remove(friendCode);
        }
    }

    // Cancella tutti i nomi originali salvati
    public static void ClearModdedNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        moddedNamesByFriendCode.Clear();
    }
    public static readonly Dictionary<string, string> afkNamesByFriendCode = new(); // Nuovo dizionario per i nomi AFK

    public static void StoreAfkName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && !afkNamesByFriendCode.ContainsKey(friendCode))
        {
            afkNamesByFriendCode[friendCode] = player.name;
        }
    }

    // Salva i nomi AFK di tutti i giocatori
    public static void StoreAfkNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            StoreAfkName(player.PlayerId);
        }
    }

    // Ripristina il nome AFK di un singolo giocatore tramite ID
    public static void RestoreAfkName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && afkNamesByFriendCode.TryGetValue(friendCode, out string afkName))
        {
            player.RpcSetName(afkName);
        }
    }

    // Ripristina i nomi AFK di tutti i giocatori
    public static void RestoreAfkNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            RestoreAfkName(player.PlayerId);
        }
    }

    // Cancella il nome AFK salvato di un singolo giocatore tramite ID
    public static void ClearAfkName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode))
        {
            afkNamesByFriendCode.Remove(friendCode);
        }
    }

    // Cancella tutti i nomi AFK salvati
    public static void ClearAfkNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        afkNamesByFriendCode.Clear();
    }
    public static readonly Dictionary<string, string> doppelNamesByFriendCode = new(); // Nuovo dizionario per i nomi AFK

    public static void StoreDoppelName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && !doppelNamesByFriendCode.ContainsKey(friendCode))
        {
            doppelNamesByFriendCode[friendCode] = player.name;
        }
    }

    // Salva i nomi AFK di tutti i giocatori
    public static void StoreDoppelNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            StoreDoppelName(player.PlayerId);
        }
    }

    // Ripristina il nome AFK di un singolo giocatore tramite ID
    public static void RestoreDoppelName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && doppelNamesByFriendCode.TryGetValue(friendCode, out string doppelName))
        {
            player.RpcSetName(doppelName);
        }
    }

    // Ripristina i nomi AFK di tutti i giocatori
    public static void RestoreDoppelNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            RestoreDoppelName(player.PlayerId);
        }
    }

    // Cancella il nome AFK salvato di un singolo giocatore tramite ID
    public static void ClearDoppelName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode))
        {
            doppelNamesByFriendCode.Remove(friendCode);
        }
    }

    // Cancella tutti i nomi AFK salvati
    public static void ClearDoppelNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        doppelNamesByFriendCode.Clear();
    }
    public static readonly Dictionary<string, string> victimNamesByFriendCode = new(); // Nuovo dizionario per i nomi AFK

    public static void StoreVictimName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && !victimNamesByFriendCode.ContainsKey(friendCode))
        {
            victimNamesByFriendCode[friendCode] = player.name;
        }
    }

    // Salva i nomi AFK di tutti i giocatori
    public static void StoreVictimNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            StoreVictimName(player.PlayerId);
        }
    }

    // Ripristina il nome AFK di un singolo giocatore tramite ID
    public static void RestoreVictimName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode) && victimNamesByFriendCode.TryGetValue(friendCode, out string victimName))
        {
            player.RpcSetName(victimName);
        }
    }

    // Ripristina i nomi AFK di tutti i giocatori
    public static void RestoreVictimNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        foreach (var player in BanMod.AllPlayerControls)
        {
            RestoreVictimName(player.PlayerId);
        }
    }

    // Cancella il nome AFK salvato di un singolo giocatore tramite ID
    public static void ClearVictimName(int playerId)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
        if (player == null) return;

        string friendCode = player.FriendCode;
        if (!string.IsNullOrEmpty(friendCode))
        {
            victimNamesByFriendCode.Remove(friendCode);
        }
    }

    // Cancella tutti i nomi AFK salvati
    public static void ClearVictimNames()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        victimNamesByFriendCode.Clear();
    }
    public static void RpcTeleport(this PlayerControl player, Vector2 position, bool isRandomSpawn = false, bool sendInfoInLogs = true)
    {
        if (sendInfoInLogs)
        {
            Logger.Info($" Player Id: {player.PlayerId}", "RpcTeleport");
        }

        var netTransform = player.NetTransform;

        if (AmongUsClient.Instance.AmHost)
        {
            netTransform.SnapTo(position, (ushort)(netTransform.lastSequenceId + 328));
            netTransform.SetDirtyBit(uint.MaxValue);
        }

        var sendOption = SendOption.Reliable;


        ushort newSid = (ushort)(netTransform.lastSequenceId + 8);
        MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(netTransform.NetId, (byte)RpcCalls.SnapTo, sendOption);
        NetHelpers.WriteVector2(position, messageWriter);
        messageWriter.Write(newSid);
        AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
    }
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
public static class FixedUpdateDispatcherPatch // Rinominato per chiarezza
{
    private static void Prefix(PlayerControl __instance)
    {
        ProxyMessageQueue.TrySendNext();
        if (!AmongUsClient.Instance.AmHost) return;

        Utils.MessageRetryHandler.TrySendPending();
        ProtectionManager.PeriodicReapplyProtection();
        AFKDetector.OnFixedUpdate(__instance, false);

    }
    private static void Postfix(PlayerControl __instance)
    {
        if (!Options.Taskremain.GetBool())
            return;
        if (!GameStates.IsInTask)
            return;
        if (__instance == null || __instance.Data == null || __instance.cosmetics == null || __instance.cosmetics.nameText == null || PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null || __instance.Data.Tasks == null)
            return;

        var local = PlayerControl.LocalPlayer;

        bool isSamePlayer = local.PlayerId == __instance.PlayerId;
        bool isLocalImpostor = local.Data.Role.TeamType == RoleTeamTypes.Impostor;
        bool isLocalDead = local.Data.IsDead;

        bool isTargetImpostor = __instance.Data.Role.TeamType == RoleTeamTypes.Impostor;
        bool isTargetCrewmate = !isTargetImpostor;

        if (Utils.Shapeshifter(__instance))
            return;

        if (isLocalImpostor && isSamePlayer)
            return;

        if (!Options.Taskremain.GetBool() || !AmongUsClient.Instance.IsGameStarted)
            return;

        bool showTasks = false;

        if (isLocalImpostor)
        {
            showTasks = !isSamePlayer && isTargetCrewmate;
        }
        else
        {
            if (isLocalDead)
            {
                showTasks = isTargetCrewmate;
            }
            else
            {
                showTasks = isSamePlayer;
            }
        }

        if (showTasks)
        {
            int totalTasks = __instance.Data.Tasks.Count;
            int tasksDone = 0;
            foreach (var task in __instance.Data.Tasks)
            {
                if (task != null && task.Complete)
                    tasksDone++;
            }

            __instance.cosmetics.nameText.text = $"{__instance.Data.PlayerName} <color=#00FFFF>({tasksDone}/{totalTasks})</color>";
        }
        else
        {
            __instance.cosmetics.nameText.text = __instance.Data.PlayerName;
        }
    }
}
