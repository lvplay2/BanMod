using AmongUs.Data;
using AmongUs.GameOptions;
using Assets.CoreScripts;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Drawing;
using InnerNet;
using LibCpp2IL.Elf;
using MS.Internal.Xml.XPath;
using Rewired;
using Rewired.Utils.Classes.Data;
using Rewired.Utils.Platforms.Windows;
using Sentry.Unity.NativeUtils;
using StableNameDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static BanMod.ExtendedPlayerControl;
using static BanMod.SpamManager;
using static BanMod.Translator;
using static BanMod.Utils;
using static Il2CppMono.Security.X509.X520;
using static Il2CppSystem.Linq.Expressions.Interpreter.CastInstruction.CastInstructionNoT;
using static Il2CppSystem.Net.Http.Headers.Parser;
using static InnerNet.ClientData;
using static Rewired.Data.UserDataStore_PlayerPrefs.ControllerAssignmentSaveInfo;
using static UnityEngine.GraphicsBuffer;
using Color = UnityEngine.Color;

namespace BanMod;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal class ChatCommands
{
    public static List<string> ChatHistory = [];

    public static bool Prefix(ChatController __instance)
    {
        string text = __instance.freeChatField.textArea.text;
        if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
        if (__instance.timeSinceLastMessage < 3f) return false;

        ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
        string[] args = text.Split(' ');
        if (args.Length == 0) return true;

        string command = args[0].ToLowerInvariant();
        string subArgs = args.Length > 1 ? args[1] : "";

        if (MessageBlocker.CanSendMessage())
            BanMod.isChatCommand = true;


        if (AmongUsClient.Instance.AmHost)
        {
            bool canceled = HandleCommand(command, args, subArgs);
            return !canceled;
        }

        // Altrimenti, lascia passare il messaggio per l'host
        return true;

    }

    public static bool HandleCommand(string command, string[] args, string subArgs)
    {
        var player = PlayerControl.LocalPlayer;
        string playername = player.GetRealName();
        switch (command)
        {
            case "/endgame":
                if (!AmongUsClient.Instance.AmHost)
                    return true;

                GameManager.Instance.RpcEndGame(GameOverReason.CrewmatesByTask, false);
                break;

            case "/endmeeting":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                PlayerControl.LocalPlayer.StartCoroutine(Utils.DelayedCloseMeeting());
                break;

            case "/exeme":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                Utils.Exeme();
                break;

            case "/killme":
                if (!AmongUsClient.Instance.AmHost)
                    return true;
                Utils.KillPlayer(PlayerControl.LocalPlayer);
                break;

            case "/tpout":
                if (!GameStates.IsLobby) break;
                PlayerControl.LocalPlayer.RpcTeleport(new Vector2(0.1f, 3.8f));
                break;

            case "/tpin":
                if (!GameStates.IsLobby) break;
                PlayerControl.LocalPlayer.RpcTeleport(new Vector2(-0.2f, 1.3f));
                break;

            case "/time":
                Scientist.ScientistCommandHost(); // oppure OnMeetingStarted()
                return true;

            case "/summary":
            case "/result":
                {
                    string report = MatchSummary.GetSummaryReport();

                    {
                        Utils.SendMessage("", 255, report);
                        MessageBlocker.UpdateLastMessageTime();

                    }
                    return true;
                }

            case "/info":
                subArgs = args.Length < 2 ? "" : args[1];
                subArgs = args.Length < 2 ? "" : args[1].ToLowerInvariant(); // normalizza input
                switch (subArgs)
                {
                    // Giustiziere / Guesser
                    case "giustiziere":
                    case "guesser":
                    case "guess":
                    case "giustiz":
                    case "g":
                    case "devin":           // FR
                    case "vermuten":        // DE
                    case "Предсказатель":   // RU
                        Utils.SendMessage("", 255, GetString("guesser.cm"));
                        MessageBlocker.UpdateLastMessageTime();

                        break;

                    // Presidente / Exiler
                    case "presidente":
                    case "president":
                    case "exiler":
                    case "p":
                    case "président":       // FR
                    case "präsident":       // DE
                    case "президент":       // RU
                        Utils.SendMessage("", 255, GetString("exiler.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    // Spettro / Fantasma / Phantom
                    case "spettro":
                    case "fantasma":
                    case "phantom":
                    case "ph":
                    case "fantôme":         // FR
                    case "geist":           // DE
                    case "призрак":         // RU
                        Utils.SendMessage("", 255, GetString("phantom.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    // Immortale / Immortal
                    case "immortale":
                    case "immortal":
                    case "imm":
                    case "immortel":        // FR
                    case "unsterblich":     // DE
                    case "бессмертный":     // RU
                        Utils.SendMessage("", 255, GetString("immortal.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    // Ingegnere / Engineer
                    case "ing":
                    case "ingegnere":
                    case "engineer":
                    case "eng":
                    case "ingénieur":       // FR
                    case "ingenieur":       // DE
                    case "инженер":         // RU
                        Utils.SendMessage("", 255, GetString("engineer.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    // Scienziato / Scientist
                    case "scienziato":
                    case "scientist":
                    case "sci":
                    case "scientifique":    // FR
                    case "wissenschaftler": // DE
                    case "учёный":          // RU
                        Utils.SendMessage("", 255, GetString("scientist.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    default:
                        Utils.ShowRoleClient();
                        break;
                }
                return true;

            case "/m":

                bool isSpecialKiller1 = PlayerControl.LocalPlayer.PlayerId == Guesser.SpecialKillerId;
                bool isPresident1 = PlayerControl.LocalPlayer.PlayerId == Exiler.ExilerId;
                bool isScientist1 = Scientist(PlayerControl.LocalPlayer);
                bool isPhantom1 = Phantom(PlayerControl.LocalPlayer);
                bool isEngineer1 = Engineer(PlayerControl.LocalPlayer);
                bool isImmortal1 = ImmortalManager.IsImmortal(PlayerControl.LocalPlayer.PlayerId);
                bool Shapeshifter1 = Shapeshifter(PlayerControl.LocalPlayer);

                if (isEngineer1)
                {
                    Engineer.SendEngineerMessage();
                }
                if (Shapeshifter1)
                {
                    ImpostorNameSender();
                }
                if (isPhantom1)
                {
                    ImpostorNameSender();
                    ImpostorGuesser.SendPhantomPlayerMessage();
                }
                if (isScientist1)
                {
                    Scientist.SendScientistMessage();
                }
                if (isSpecialKiller1)
                {
                    Guesser.SendKillerMessage();
                }
                if (isPresident1)
                {
                    Exiler.SendExilerMessage();
                }
                if (isImmortal1)
                {
                    string msg = GetString("immortal");
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(msg, player.PlayerId, GetString("immortaltitle"));
                        MessageBlocker.UpdateLastMessageTime();
                    }
                }
                else
                {
                    string msg = string.Format(GetString("NeutralInfo"));
                    Utils.SendMessage(msg, PlayerControl.LocalPlayer.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();

                }

                return true;

            case "/role":
                if (!AmongUsClient.Instance.AmHost)
                    return true;

                if (Options.EngineerFixer.GetBool())
                {
                    Engineer.SendEngineerMessage();
                }
                else if (Options.ImpostorGuess.GetBool())
                {
                    ImpostorGuesser.SendPhantomPlayerMessage();
                }
                else if (Options.ScientistTime.GetBool())
                {
                    Scientist.SendScientistMessage();
                }
                if (Options.sendkillmessage.GetBool())
                {
                    ImpostorNameSender();
                }

                if (Options.Guess.GetBool())
                {
                    Guesser.SendKillerMessage();
                }
                if (Options.ExilerExe.GetBool())
                {
                    Exiler.SendExilerMessage();
                }
                return true;


            case "/exe":
            case "/kill":
            case "/bm":
                {
                    if (!AmongUsClient.Instance.AmHost)
                    {
                        return true;
                    }
                    if (PlayerControl.LocalPlayer.Data.IsDead) return true;
                    PlayerControl targetPlayer = null;

                    // Controlla se c'è almeno un argomento dopo /info
                    if (args.Length >= 2)
                    {
                        string targetInput = args[1];
                        string normalizedTargetInput = NameNormalizer.NormalizeInputName(targetInput);

                        // Prova a trovare il player per ID
                        if (int.TryParse(targetInput, out int targetId))
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == targetId);
                        }

                        // Se non trovato, prova con il colore
                        if (targetPlayer == null)
                        {
                            byte colorId = MsgToColor(targetInput);
                            if (colorId != byte.MaxValue)
                            {
                                targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                    p != null && p.Data != null && p.Data.DefaultOutfit.ColorId == colorId);
                            }
                        }

                        // Se ancora nulla, prova con nome normalizzato
                        if (targetPlayer == null)
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                p != null && p.Data != null &&
                                NameNormalizer.NormalizeInputName(p.GetRealName()).Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                        }

                        // Prova con nome originale da friend code
                        if (targetPlayer == null)
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                p != null && p.Data != null &&
                                ExtendedPlayerControl.originalNamesByFriendCode.TryGetValue(p.Data.FriendCode, out string originalName) &&
                                NameNormalizer.NormalizeInputName(originalName).Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                        }

                        // Se abbiamo un target valido, fai controlli speciali o esegui comando
                        if (targetPlayer != null)
                        {
                            bool isSpecialKiller = player.PlayerId == Guesser.SpecialKillerId;
                            bool isPresident = player.PlayerId == Exiler.ExilerId;
                            bool isScientist = Scientist(player);
                            bool isPhantom = Phantom(player) && ImmortalManager.immortalAssigned;
                            bool isEngineer = Engineer(player);
                            bool isImmortal = ImmortalManager.IsImmortal(player.PlayerId);

                            if (isSpecialKiller && Options.Guess.GetBool())
                            {
                                ExecuteTdnSilently(PlayerControl.LocalPlayer, targetPlayer);
                            }
                            if (isPresident && Options.ExilerExe.GetBool())
                            {
                                ExecuteTdnSilently(PlayerControl.LocalPlayer, targetPlayer);
                            }
                            if (isScientist && Options.ScientistTime.GetBool())
                            {
                                ExecuteTdnSilently(PlayerControl.LocalPlayer, targetPlayer);
                            }
                            if (isPhantom && Options.ImpostorGuess.GetBool())
                            {
                                ExecuteTdnSilently(PlayerControl.LocalPlayer, targetPlayer);
                            }
                            if (isEngineer && Options.EngineerFixer.GetBool())
                            {
                                ExecuteTdnSilently(PlayerControl.LocalPlayer, targetPlayer);
                            }
                            if (isImmortal && Options.EnableImmortal.GetBool())
                            {
                                ExecuteTdnSilently(PlayerControl.LocalPlayer, targetPlayer);
                            }
                            if (!isSpecialKiller && !isPresident && !isScientist && !isPhantom && !isEngineer && !isImmortal)
                            {
                                string msg = string.Format(GetString("NeutralInfo"));
                                Utils.SendMessage(msg, PlayerControl.LocalPlayer.PlayerId);
                                MessageBlocker.UpdateLastMessageTime();

                            }
                        }
                    
                        // Se targetPlayer == null, non fare nulla, non inviare messaggi
                    }
                }
                return true;

            case "/coms":
                if (PlayerControl.LocalPlayer != null && (
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Phantom ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Shapeshifter))
                {
                    SabotageManager.TryActivateSabotage(SystemTypes.Comms, 128);
                }
                return true;


            case "/o2":
                if (PlayerControl.LocalPlayer != null && (
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Phantom ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Shapeshifter))
                {
                    SabotageManager.TryActivateSabotage(SystemTypes.LifeSupp, 128);
                }
                return true;

            case "/reactor":
                if (PlayerControl.LocalPlayer != null && (
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Phantom ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Shapeshifter))
                {
                    SabotageManager.TryActivateSabotage(SystemTypes.Reactor, 128);
                }
                return true;

            case "/light":
                if (PlayerControl.LocalPlayer != null && (
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Impostor ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Phantom ||
                    PlayerControl.LocalPlayer.Data.RoleType == RoleTypes.Shapeshifter))
                {
                    byte electricalSabotageId = 4;
                    for (int i = 0; i < 5; i++)
                    {
                        electricalSabotageId |= (byte)(1 << i);

                    }
                    electricalSabotageId |= 128;

                    SabotageManager.TryActivateSabotage(SystemTypes.Electrical, electricalSabotageId);
                }
                return true;


            case "/colour":
            case "/color":
            case "/colore":
                subArgs = args.Length < 2 ? "" : args[1];
                var color = Utils.MsgToColor(subArgs, true);
                if (color == byte.MaxValue)
                {
                    break;
                }
                PlayerControl.LocalPlayer.RpcSetColor(color);
                break;

            case "/символ":
            case "/symbole":
            case "/simboli":
            case "/symbol":
                {
                    string symbolmsg = GetString("symbolcm");
                    Utils.SendMessage("", 255, symbolmsg);
                    MessageBlocker.UpdateLastMessageTime();
                }
                return true;

            case "/sg":
            case "/setspecialkiller":
                if (args.Length == 2 && byte.TryParse(subArgs, out byte targetId1))
                {
                    var target = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p.PlayerId == targetId1 &&
                        p.Data != null &&
                        !p.Data.IsDead &&
                        p.Data.Role?.TeamType != RoleTeamTypes.Impostor);

                    if (target != null)
                    {
                        Guesser.SpecialKillerId = targetId1;
                        Guesser.SpecialKillerSelected = true;
                        ShowChat($"Special Killer impostato su {target.name} (ID: {targetId1}).");

                        if (AmongUsClient.Instance.AmHost)
                        {
                            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetSpecialKiller, SendOption.Reliable, -1);
                            writer.Write(targetId1);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            Logger.Info($"[Guesser] Inviato SpecialKillerId = {targetId1} a tutti i client.");
                        }
                        else
                        {
                            ShowChat("Solo l'host può impostare lo Special Killer.");
                        }
                    }
                    else
                    {
                        ShowChat($"Giocatore con ID {targetId1} non valido, è morto, o è un impostore.");
                    }
                }
                else
                {
                    ShowChat("Uso corretto: /setsk ");
                }
                return true;

            case "/se":
            case "/setexiler":
                if (args.Length == 2 && byte.TryParse(subArgs, out byte targetId2))
                {
                    var target = BanMod.AllPlayerControls.FirstOrDefault(p =>
                        p.PlayerId == targetId2 &&
                        p.Data != null &&
                        !p.Data.IsDead );

                    if (target != null)
                    {
                        Exiler.ExilerId = targetId2;
                        Exiler.ExilerSelected = true;
                        ShowChat($"Presidente impostato su {target.name} (ID: {targetId2}).");

                        if (AmongUsClient.Instance.AmHost)
                        {
                            var writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetExiler, SendOption.Reliable, -1);
                            writer.Write(targetId2);
                            AmongUsClient.Instance.FinishRpcImmediately(writer);
                            Logger.Info($"[Exiler] Inviato ExilerId = {targetId2} a tutti i client.");
                        }
                        else
                        {
                            ShowChat("Solo l'host può impostare il Presidente.");
                        }
                    }
                    else
                    {
                        ShowChat($"Giocatore con ID {targetId2} non valido.");
                    }
                }
                else
                {
                    ShowChat("Uso corretto: /setexiler ");
                }
                return true;

            case "/sn":
            case "/storenames":
                if (args.Length == 2 && subArgs.Equals("all", StringComparison.OrdinalIgnoreCase) || subArgs.Equals("all"))
                {
                    StoreOriginalNames();
                    ShowChat(GetString("StoredNamesAll"));
                }
                else if (args.Length == 2 && int.TryParse(subArgs, out int storeId))
                {
                    StoreOriginalName(storeId);
                    ShowChat(string.Format(GetString("StoredNameSingle"), storeId));
                }
                else
                {
                    ShowChat(GetString("InvalidId"));
                }
                return true;

            case "/snall":
            case "/storenamesall":
                StoreOriginalNames();
                ShowChat(GetString("StoredNamesAll"));
                return true;

            // --- RESTORE NAMES ---

            case "/rn":
            case "/restorenames":
                if (args.Length == 2 && subArgs.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    RestoreOriginalNames();
                    ShowChat(GetString("RestoredNamesAll"));
                }
                else if (args.Length == 2 && int.TryParse(subArgs, out int restoreId))
                {
                    RestoreOriginalName(restoreId);
                    ShowChat(string.Format(GetString("RestoredNameSingle"), restoreId));
                }
                else
                {
                    ShowChat(GetString("InvalidId"));
                }
                return true;

            case "/rnall":
            case "/restorenamesall":
                RestoreOriginalNames();
                ShowChat(GetString("RestoredNamesAll"));
                return true;

            // --- CLEAR NAMES ---

            case "/cn":
            case "/clearnames":
                if (args.Length == 2 && subArgs.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    ClearOriginalNames();
                    ShowChat(GetString("ClearedNamesAll"));
                }
                else if (args.Length == 2 && int.TryParse(subArgs, out int clearId))
                {
                    ClearOriginalName(clearId);
                    ShowChat(string.Format(GetString("ClearedNameSingle"), clearId));
                }
                else
                {
                    ShowChat(GetString("InvalidId"));
                }
                return true;

            case "/cnall":
            case "/clearnamesall":
                ClearOriginalNames();
                ShowChat(GetString("ClearedNamesAll"));
                return true;

            case "/mn":
                if (args.Length == 2 && int.TryParse(subArgs, out int moddedId))
                {
                    RestoreModdedName(moddedId);
                    ShowChat(string.Format(GetString("RestoredModdedNameSingle"), moddedId));
                }
                else if (args.Length == 2 && subArgs.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    RestoreModdedNames();
                    ShowChat(GetString("RestoredModdedNamesAll"));
                }
                else
                {
                    ShowChat(GetString("InvalidId"));
                }
                return true;

            case "/mnall":
            case "/moddednamesall":
                RestoreModdedNames();
                ShowChat(GetString("RestoredModdedNamesAll"));
                return true;

            case "/cmn":
            case "/clearmoddednames":
                if (args.Length == 2 && subArgs.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    ClearModdedNames();
                    ShowChat(GetString("ClearedNamesAll"));
                }
                else if (args.Length == 2 && int.TryParse(subArgs, out int clearId))
                {
                    ClearModdedName(clearId);
                    ShowChat(string.Format(GetString("ClearModdedNameSingle"), clearId));
                }
                else
                {
                    ShowChat(GetString("InvalidId"));
                }
                return true;

            case "/cmnall":
            case "/clearmoddednamesall":
                ClearModdedNames();
                ShowChat(GetString("ClearModdedNamesAll"));
                return true;

            case "/listnames":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                {
                    foreach (var kv in originalNamesByFriendCode)
                    {
                        ShowChat($"<color=#88ff88>{kv.Key}</color>: {kv.Value}");
                    }
                }
                return true;


            case "/all":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                Utils.ShowCommand();
                Utils.ShowCommand2();
                Utils.ShowCommand3();
                return true;

            case "/help":
            case "/aiuto":
            case "/hilfe":
            case "/aide":
            case "/помощь":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                Utils.ShowCommand3();
                return true;

            case "/dn": return AppendToFile("DenyName.txt", string.Join(" ", subArgs), "AddedtoDenyname");
            case "/ddn": return RemoveFromFile("DenyName.txt", string.Join(" ", subArgs), "DeletedtoDenyname");
            case "/dw": return AppendToFile("BanWords.txt", string.Join(" ", subArgs), "AddedtoDenyWord"); // Unisce le parole
            case "/ddw": return RemoveFromFile("BanWords.txt", string.Join(" ", subArgs), "DeletedtoDenyWord"); // Unisce le parole
            case "/ds": return AppendToFile("SpamStart.txt", string.Join(" ", subArgs), "AddedtoDenystart");
            case "/dds": return RemoveFromFile("SpamStart.txt", string.Join(" ", subArgs), "DeletedtoDenystart");
            case "/f": return ManageFriend(subArgs, add: true);
            case "/df": return ManageFriend(subArgs, add: false);

            case "/id":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                string msg5 = GetString("PlayerIdList") + string.Join("\n", BanMod.AllPlayerControls
                    .Where(p => p != null)
                    .Select(p => $"{p.PlayerId} ({NumberToWords(p.PlayerId)}) → {p.GetRealName()}")); // Modifica qui
                ShowChat(msg5);
                return true;


            case "/level":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                if (int.TryParse(subArgs, out int level) && level is >= 1 and <= 99999)
                {
                    uint lvl = Convert.ToUInt32(level - 1);
                    player.RpcSetLevel(lvl);
                    DataManager.Player.stats.level = lvl;
                    DataManager.Player.Save();
                    ShowChat(GetString("Message.SetLevel") + subArgs);
                }
                else
                {
                    ShowChat(GetString("Message.AllowLevelRange"));
                }
                return true;

            case "/say":
            case "/scrivi":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                return PrivateMessage(args);

            case "/rename":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                player.RpcSetName(subArgs);
                StoreModdedName(player.PlayerId);
                return true;

            case "/n":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                return SetColoredName(subArgs);

            case "/s": // sinistra
            case "/l": // left
            case "/d": // destra
            case "/r": // right
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                return AddSymbol(args, command == "/s");

            case "/reset":
            case "/resetname":
                if (!AmongUsClient.Instance.AmHost)
                {
                    return true; // blocca l'esecuzione lato client
                }
                return ResetName();

        }

        return false;
    }

    // Funzione per aggiungere la parola/frasè al file
    static bool AppendToFile(string file, string value, string msgKey)
    {
        // Aggiungi al file solo la stringa, senza modificare il formato (evitando che venga separato in parole)
        File.AppendAllText($"./BAN_DATA/{file}", $"\n{value}");
        ShowChat(value + GetString(msgKey));
        return true;
    }

    // Funzione per rimuovere la parola/frasè dal file
    static bool RemoveFromFile(string file, string value, string msgKey)
    {
        // Legge tutte le righe del file e rimuove la riga che contiene la stringa completa "value"
        var lines = File.ReadAllLines($"./BAN_DATA/{file}").Where(line => !line.Contains(value)).ToList();
        File.WriteAllLines($"./BAN_DATA/{file}", lines);
        ShowChat(value + GetString(msgKey));
        return true;
    }
    public static bool ComandoExeUsed = false;
    public static bool ManageFriend(string idStr, bool add)
    {
        if (!byte.TryParse(idStr, out var id)) return true;
        var player = GetPlayerById(id);
        if (player == null) return true;

        string code = player.FriendCode;
        string name = player.GetRealName();
        string filePath = "./BAN_DATA/Friends.txt";

        if (add)
        {
            if (!IsFriends(code))
            {
                File.AppendAllText(filePath, $"\n{code}, {name}");
                ShowChat(name + GetString("AddedtoFriendList"));
            }
            else
            {
                ShowChat(name + GetString("PlayerinFriendList"));
            }
        }
        else
        {
            if (IsFriends(code))
            {
                var lines = File.ReadAllLines(filePath)
                                .Where(line =>
                                {
                                    var parts = line.Split(',', StringSplitOptions.TrimEntries);
                                    return !(parts.Length > 0 && parts[0].Equals(code, StringComparison.OrdinalIgnoreCase));
                                });

                File.WriteAllLines(filePath, lines);
                ShowChat(name + GetString("PlayerRemovedFromFriendsList"));
            }
            else
            {
                ShowChat(name + GetString("PlayerNotInFriendList"));
            }
        }

        return true;
    }

    static bool PrivateMessage(string[] args)
    {
        if (args.Length < 2) return false;

        string msg;
        byte id = byte.MaxValue; // Default: broadcast to all

        if (byte.TryParse(args[1], out byte parsedId) && args.Length >= 3)
        {
            id = parsedId;
            msg = string.Join(" ", args.Skip(2));
        }
        else
        {
            msg = string.Join(" ", args.Skip(1));
        }

        var target = id == byte.MaxValue ? null : GetPlayerById(id);
        if (id != byte.MaxValue && target == null) return false;
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, id);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, id);
            MessageBlocker.UpdateLastMessageTime();
        }

        string recipient = id == byte.MaxValue ? "tutti" : target.GetRealName();
        ShowChat($"✓ Messaggio inviato a {recipient}");
        return true;
    }
    private static Stack<NetworkedPlayerInfo.PlayerOutfit> savedOutfits2 = new Stack<NetworkedPlayerInfo.PlayerOutfit>();
    private static Stack<string> savedNames2 = new Stack<string>();
    public static bool SetColoredName(string colorKey)
    {
        var player = PlayerControl.LocalPlayer;
        string friendCode = player.FriendCode;
        string baseName = Regex.Replace(player.GetRealName(), "<.*?>", "");


        if (colorMap.TryGetValue(colorKey, out var hex) || Regex.IsMatch(colorKey, "^#([0-9A-Fa-f]{6})$"))
        {
            string color = hex ?? colorKey;
            player.RpcSetName($"<color={color}>{baseName}</color>");
            StoreModdedName(player.PlayerId);
            return true;
        }

        return false;
    }

    static bool AddSymbol(string[] args, bool prefix)
    {
        string symbolKey = args.ElementAtOrDefault(1)?.ToLowerInvariant() ?? "";
        string colorKey = args.ElementAtOrDefault(2)?.ToLowerInvariant() ?? "";

        if (!symbolMap.TryGetValue(symbolKey, out string symbol)) return false;
        string color = colorMap.TryGetValue(colorKey, out var hex) ? hex : "#FFFFFF";

        string colored = $"<color={color}>{symbol}</color>";
        string name = PlayerControl.LocalPlayer.GetRealName();

        PlayerControl.LocalPlayer.RpcSetName(prefix ? colored + name : name + colored);
        StoreModdedName(PlayerControl.LocalPlayer.PlayerId);
        return true;
    }

    static bool ResetName()
    {
        RestoreOriginalName(PlayerControl.LocalPlayer.PlayerId);
        return true;
    }
   
    public static void ShowChat(string msg) => DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg);

    public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
    {
        canceled = false;

        // Se io (local player) non sono host, esco subito senza fare nulla
        if (!AmongUsClient.Instance.AmHost)
            return;

        string playerName = player.GetRealName();
        string[] args = text.Split(' ');
        string command = args[0].ToLowerInvariant();
        string subArg = args.Length > 1 ? args[1].ToLowerInvariant() : "";
        string subArgs = args.Length > 1 ? args[1] : "";


        switch (command)
        {
            // Cambia colore del nome
            case "/n":
                if (!Options.changename.GetBool()) return;
                if (GameStates.isLobby)
                {
                    string baseName = Regex.Replace(playerName, "<.*?>", "");
                    string friendCode = player.FriendCode;

                    {
                        string nameColorHex = colorMap.ContainsKey(subArg)
                            ? colorMap[subArg]
                            : (Regex.IsMatch(subArg, "^#([0-9A-Fa-f]{6})$") ? subArg : null);

                        if (nameColorHex != null)
                        {
                            string newName = $"<color={nameColorHex}>{baseName}</color>";
                            player.RpcSetName(newName);
                            StoreModdedName(player.PlayerId);
                        }
                    }
                }
                break;


            case "/s": // sinistra
            case "/l": // left
            case "/d": // destra
            case "/r": // right
                if (!Options.changename.GetBool()) return;
                if (GameStates.isLobby)
                    if (args.Length > 1)
                    {
                        string symbolKey = subArg;
                        string colorArg = args.Length > 2 ? args[2].ToLowerInvariant() : "";

                        if (symbolMap.TryGetValue(symbolKey, out string rawSymbol))
                        {
                            string colorHex = colorMap.ContainsKey(colorArg)
                                ? colorMap[colorArg]
                                : (Regex.IsMatch(colorArg, "^#([0-9A-Fa-f]{6})$") ? colorArg : "#FFFFFF");

                            string coloredSymbol = $"<color={colorHex}>{rawSymbol}</color>";
                            string newName = command == "/s"
                                ? $"{coloredSymbol}{playerName}"
                                : $"{playerName}{coloredSymbol}";
                            player.RpcSetName(newName);
                            StoreModdedName(player.PlayerId);

                        }
                    }
                break;

            case "/reset":
            case "/resetname":
                if (!Options.changename.GetBool()) return;
                if (GameStates.isLobby)
                {
                    RestoreOriginalName(player.PlayerId);
                }
                break;


            case "/colour":
            case "/color":
            case "/colore":
                subArgs = args.Length < 2 ? "" : args[1];
                var color = Utils.MsgToColor(subArgs, true);
                if (color == byte.MaxValue)
                {
                    break;
                }
                if (!Options.changecolor.GetBool()) return;
                if (GameStates.isLobby)
                    player.RpcSetColor(color);
                break;

           
            case "/exe":
            case "/kill":
            case "/time":
            case "/bm":
                {
                    if (GameStates.isLobby) return;
                    if (player.Data.IsDead) return;
                    PlayerControl targetPlayer = null;

                    // Controlla se c'è almeno un argomento dopo /info
                    if (args.Length >= 2)
                    {
                        string targetInput = args[1];
                        string normalizedTargetInput = NameNormalizer.NormalizeInputName(targetInput);

                        // Prova a trovare il player per ID
                        if (int.TryParse(targetInput, out int targetId))
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == targetId);
                        }

                        // Se non trovato, prova con il colore
                        if (targetPlayer == null)
                        {
                            byte colorId = MsgToColor(targetInput);
                            if (colorId != byte.MaxValue)
                            {
                                targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                    p != null && p.Data != null && p.Data.DefaultOutfit.ColorId == colorId);
                            }
                        }

                        // Se ancora nulla, prova con nome normalizzato
                        if (targetPlayer == null)
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                p != null && p.Data != null &&
                                NameNormalizer.NormalizeInputName(p.GetRealName()).Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                        }

                        // Prova con nome originale da friend code
                        if (targetPlayer == null)
                        {
                            targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                                p != null && p.Data != null &&
                                ExtendedPlayerControl.originalNamesByFriendCode.TryGetValue(p.Data.FriendCode, out string originalName) &&
                                NameNormalizer.NormalizeInputName(originalName).Equals(normalizedTargetInput, StringComparison.OrdinalIgnoreCase));
                        }

                        // Se abbiamo un target valido, fai controlli speciali o esegui comando
                        if (targetPlayer != null)
                        {
                            bool isSpecialKiller = player.PlayerId == Guesser.SpecialKillerId;
                            bool isPresident = player.PlayerId == Exiler.ExilerId;
                            bool isScientist = Scientist(player);
                            bool isPhantom = Phantom(player) && ImmortalManager.immortalAssigned;
                            bool isEngineer = Engineer(player);
                            bool isImmortal = ImmortalManager.IsImmortal(player.PlayerId);

                            if (isSpecialKiller && Options.Guess.GetBool())
                            {
                                ExecuteTdnSilently(player, targetPlayer);
                            }
                            if (isPresident && Options.ExilerExe.GetBool())
                            {
                                ExecuteTdnSilently(player, targetPlayer);
                            }
                            if (isScientist && Options.ScientistTime.GetBool())
                            {
                                ExecuteTdnSilently(player, targetPlayer);
                            }
                            if (isPhantom && Options.ImpostorGuess.GetBool())
                            {
                                ExecuteTdnSilently(player, targetPlayer);
                            }
                            if (isEngineer && Options.EngineerFixer.GetBool())
                            {
                                ExecuteTdnSilently(player, targetPlayer);
                            }
                            if (isImmortal && Options.EnableImmortal.GetBool())
                            {
                                ExecuteTdnSilently(player, targetPlayer);
                            }
                            if (!isSpecialKiller && !isPresident && !isScientist && !isPhantom && !isEngineer && !isImmortal)
                            {
                                string msg = string.Format(GetString("NeutralInfo"));
                                Utils.SendMessage(msg, player.PlayerId);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                        }
                        // Se targetPlayer == null, non fare nulla, non inviare messaggi
                    }
                }
                return;

            case "/m":
                if (GameStates.isLobby) return;
                bool isSpecialKiller1 = player.PlayerId == Guesser.SpecialKillerId;
                bool isPresident1 = player.PlayerId == Exiler.ExilerId;
                bool isScientist1 = Scientist(player);
                bool isPhantom1 = Phantom(player);
                bool isEngineer1 = Engineer(player);
                bool isImmortal1 = ImmortalManager.IsImmortal(player.PlayerId);
                bool Shapeshifter1 = Shapeshifter(player);

                if (isEngineer1)
                {
                    Engineer.SendEngineerMessage();
                }
                if (Shapeshifter1)
                {
                    ImpostorNameSender();
                }
                else if (isPhantom1)
                {
                    ImpostorNameSender();
                    ImpostorGuesser.SendPhantomPlayerMessage();
                }
                else if (isScientist1)
                {
                    Scientist.SendScientistMessage();
                }
                else if (isSpecialKiller1)
                {
                    Guesser.SendKillerMessage();
                }
                else if (isPresident1)
                {
                    Exiler.SendExilerMessage();
                }
                else if (isImmortal1)
                {
                    string msg = GetString("immortal");
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(msg, player.PlayerId, GetString("immortaltitle"));
                        MessageBlocker.UpdateLastMessageTime();
                    }
                }
                else
                {
                    string msg = string.Format(GetString("NeutralInfo"));
                    Utils.SendMessage(msg, player.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
                return;


            case "/id":
                if (GameStates.isLobby) return;
                string msg5 = GetString("PlayerIdList") + string.Join("\n", BanMod.AllPlayerControls
                    .Where(p => p != null)
                    .Select(p => $"{p.PlayerId} ({NumberToWords(p.PlayerId)}) → {p.GetRealName()}")); // Modifica qui
                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Utils.RequestProxyMessage(msg5, player.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
                else
                {
                    Utils.SendMessage(msg5, player.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
                return;

            case "/info":
                if (!GameStates.isLobby) return;
                subArgs = args.Length < 2 ? "" : args[1].ToLowerInvariant(); // normalizza input
                switch (subArgs)
                {
                    // Giustiziere / Guesser
                    case "giustiziere":
                    case "guesser":
                    case "guess":
                    case "giustiz":
                    case "g":
                    case "devin":           // FR
                    case "vermuten":        // DE
                    case "предсказатель":   // RU
                        Utils.SendMessage("", 255, GetString("guesser.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    // Presidente / Exiler
                    case "presidente":
                    case "president":
                    case "exiler":
                    case "p":
                    case "président":       // FR
                    case "präsident":       // DE
                    case "президент":       // RU
                        Utils.SendMessage("", 255, GetString("exiler.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    // Spettro / Fantasma / Phantom
                    case "spettro":
                    case "fantasma":
                    case "phantom":
                    case "ph":
                    case "fantôme":         // FR
                    case "geist":           // DE
                    case "призрак":         // RU
                        Utils.SendMessage("", 255, GetString("phantom.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    // Immortale / Immortal
                    case "immortale":
                    case "immortal":
                    case "imm":
                    case "immortel":        // FR
                    case "unsterblich":     // DE
                    case "бессмертный":     // RU
                        Utils.SendMessage("", 255, GetString("immortal.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    // Ingegnere / Engineer
                    case "ing":
                    case "ingegnere":
                    case "engineer":
                    case "eng":
                    case "ingénieur":       // FR
                    case "ingenieur":       // DE
                    case "инженер":         // RU
                        Utils.SendMessage("", 255, GetString("engineer.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    // Scienziato / Scientist
                    case "scienziato":
                    case "scientist":
                    case "sci":
                    case "scientifique":    // FR
                    case "wissenschaftler": // DE
                    case "учёный":          // RU
                        Utils.SendMessage("", 255, GetString("scientist.cm"));
                        MessageBlocker.UpdateLastMessageTime();
                        break;

                    default:
                        Utils.ShowRoleClient();
                        break;
                }
                break;
            //case "/help":
            //case "/aiuto":
            //case "/hilfe":
            //case "/aide":
            //case "/помощь":
            //    Utils.ShowCommandClient();
            //    break;

            case "/rename":
                if (!Options.renamename.GetBool()) return;
                if (GameStates.isLobby)
                {
                    player.RpcSetName(subArgs);
                    StoreModdedName(player.PlayerId);

                }
                break;

            default:
                if (SpamManager.CheckStart(player, text) ||
                    SpamManager.CheckWord(player, text)) return;

                break;
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    public static class ChatUpdatePatch_SendMessage
    {
        private static float lastMessageTime = -3.3f;
        private static float timeToWait = 3.3f;

        public static void Postfix(ChatController __instance)
        {
            if (BanMod.MessagesToSend.Count == 0) return;
            if (Time.time - lastMessageTime < timeToWait) return;
            var localPlayer = PlayerControl.LocalPlayer;

            var (msg, sendTo, title) = BanMod.MessagesToSend[0];
            if (sendTo != byte.MaxValue)
            {
                var player = BanMod.AllPlayerControls.FirstOrDefault(p =>
                    p != null && p.PlayerId == sendTo && p.Data != null && !p.Data.Disconnected);

                if (player == null)
                {
                    // Il destinatario ha quittato, scarta il messaggio
                    BanMod.MessagesToSend.RemoveAt(0);
                    Debug.LogWarning($"[SendMessage] Messaggio per PlayerId {sendTo} annullato: giocatore disconnesso.");
                    return;
                }
            }
            BanMod.MessagesToSend.RemoveAt(0);

            int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo)?.GetClientId() ?? -1;
            string originalName = localPlayer.Data.PlayerName;
            if (clientId == -1)
            {
                localPlayer.SetName(title);
                FastDestroyableSingleton<HudManager>.Instance.Chat.AddChat(localPlayer, msg);
                localPlayer.SetName(originalName);
            }


            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.Reliable);

            writer.StartMessage(clientId);
            writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SetName)
                .Write(localPlayer.Data.NetId)
                .Write(title)
                .EndRpc();

            writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();

            writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SetName)
                .Write(localPlayer.Data.NetId)
                .Write(originalName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();

            lastMessageTime = Time.time;
        }
    }
}