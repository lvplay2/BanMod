using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;
using InnerNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static BanMod.Utils;
using static Il2CppMono.Security.X509.X520;

namespace BanMod;

public enum CustomRPC : byte
{
    RequestSendMessage = 100,
    ModdedHandshake = 101,
    ProxySendChat = 102,
    SetSpecialKiller = 104,
    SetExiler = 105
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
internal class RPCHandlerPatch
{
    public static readonly Dictionary<byte, string> ModdedClients = new(); // [clientId] = modId

    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
    {
        MessageReader subReader = MessageReader.Get(reader);

        // CUSTOM RPC
        if (Enum.IsDefined(typeof(CustomRPC), callId))
        {
            var customType = (CustomRPC)callId;
            switch (customType)
            {
                case CustomRPC.ModdedHandshake:
                    string modInfo = subReader.ReadString();
                    byte clientId = __instance.PlayerId;
                    ModdedClients[clientId] = modInfo;

                    Logger.Info($"Modded client {__instance.name} connected with mod: {modInfo}");
                    HudManager.Instance.Notifier.AddDisconnectMessage($"Modded client {__instance.name} connected with mod: {modInfo}");
                    return false;
                
                case CustomRPC.RequestSendMessage:
                    // Gestisci qui se vuoi fare qualcosa con questo
                    Logger.Info("Received RequestSendMessage");
                    return false;
                case CustomRPC.ProxySendChat:
                    {
                        string message = subReader.ReadString();
                        subReader.ReadString(); // titolo ignorato
                        byte target = subReader.ReadByte();

                        var localPlayer = PlayerControl.LocalPlayer;

                        // Aggiungi controlli di null per localPlayer e localPlayer.Data per robustezza
                        if (localPlayer == null || localPlayer.Data == null)
                        {
                            Logger.Info("[ProxyReceiver] localPlayer o localPlayer.Data è null. Impossibile processare messaggio proxy.");
                            return false;
                        }

                        if (!localPlayer.Data.IsDead && !localPlayer.Data.Disconnected)
                        {
                            message = message.RemoveHtmlTags().Replace("\n", " ").Trim();
                            Logger.Info($"[ProxyReceiver] Ricevuto messaggio dal proxy: {message}");

                            int targetClientId;
                            // bool alsoShowLocally = false; // Non usiamo più questa variabile per decidere la visualizzazione locale

                            if (target == byte.MaxValue)
                            {
                                targetClientId = -1; // broadcast
                                                     // Among Us di solito mostra i broadcast anche localmente automaticamente.
                            }
                            else if (target == localPlayer.PlayerId)
                            {
                                targetClientId = localPlayer.GetClientId();
                                // Se il messaggio è destinato a noi stessi, l'RPC lo mostrerà localmente.
                            }
                            else
                            {
                                var player = Utils.GetPlayerById(target);
                                // Assicurati che 'player' non sia null prima di chiamare GetClientId()
                                targetClientId = player != null ? player.GetClientId() : -1;

                                // Se il player target non è stato trovato o è invalido, potrebbe essere meglio non inviare il messaggio.
                                if (targetClientId == -1 && target != byte.MaxValue) // Se non è broadcast ma il target non esiste
                                {
                                    Logger.Info($"[ProxyReceiver] Target player (ID: {target}) non trovato per il messaggio proxy.");
                                    return false; // Non inviare se il target specifico non esiste
                                }
                            }

                            if (!MessageBlocker.CanSendMessage())
                            {
                                // Potresti voler passare anche targetClientId e altre info alla coda
                                ProxyMessageQueue.Enqueue(message, targetClientId); // Adatta Enqueue se necessario
                                Logger.Info("[ProxyReceiver] Messaggio accodato perché in cooldown, sarà inviato appena possibile");
                                return false; // blocca invio immediato ma non il messaggio
                            }

                            var writer = CustomRpcSender.Create("ProxySendChatDirect", SendOption.Reliable);
                            writer.StartMessage(targetClientId); // targetClientId -1 per broadcast, o l'ID specifico
                            writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SendChat)
                                .Write(message)
                                .EndRpc();
                            writer.EndMessage();
                            writer.SendMessage();


                            MessageBlocker.UpdateLastMessageTime();
                        }
                        else
                        {
                            Logger.Info("[ProxyReceiver] Messaggio proxy ignorato perché sono morto o disconnesso.");
                        }

                        return false; // Restituisce false per indicare che l'RPC è stato gestito
                    }
                case CustomRPC.SetSpecialKiller:
                    {
                        byte killerId = subReader.ReadByte();
                        Guesser.SpecialKillerId = killerId;
                        Guesser.SpecialKillerSelected = true;
                        Logger.Info($"[Guesser] Ricevuto SpecialKillerId = {killerId} dal server.");
                        break;
                    }
                case CustomRPC.SetExiler:
                    {
                        byte ExilerId = subReader.ReadByte();
                        Exiler.ExilerId = ExilerId;
                        Exiler.ExilerSelected = true;
                        Logger.Info($"[Guesser] Ricevuto ExilerId = {ExilerId} dal server.");
                        break;
                    }

            }
        }

        // STANDARD RPC
        var rpcType = (RpcCalls)callId;
        switch (rpcType)
        {
            case RpcCalls.SendChat:
                var text = subReader.ReadString();
                ChatCommands.OnReceiveChat(__instance, text, out var canceled);
                if (canceled) return false;
                break;

            case RpcCalls.SendQuickChat:
                ChatCommands.OnReceiveChat(__instance, "Quick chat", out var canceledQuick);
                if (canceledQuick) return false;
                break;
        }

        return true; // continua normalmente se non bloccato sopra
    }

    public static bool IsClientModded(byte playerId) => ModdedClients.ContainsKey(playerId);

    public static List<(PlayerControl player, string modInfo)> GetModdedPlayersWithInfo()
    {
        return PlayerControl.AllPlayerControls
            .ToArray()
            .Where(p => ModdedClients.ContainsKey(p.PlayerId))
            .Select(p => (p, ModdedClients[p.PlayerId]))
            .ToList();
    }
}
