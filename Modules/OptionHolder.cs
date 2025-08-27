using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Data; 
using InnerNet; 
using UnityEngine;
using BanMod; 

namespace BanMod
{
    [HarmonyPatch]
    public static class Options
    {
        static Task taskOptionsLoad;

        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
        public static void OptionsLoadStart()
        {
            Logger.Info("Options.Load Start", "Options");
            taskOptionsLoad = Task.Run(() =>
            {
                try
                {
                    Load();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error loading options: {ex.Message}", "Options");
                }
            });
        }

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
        public static void WaitOptionsLoad()
        {
            taskOptionsLoad.Wait();
            Logger.Info("Options.Load End", "Options");
        }

        //Afk
        public static OptionItem EnableDetector;
        public static OptionItem EnableAfkKick;
        public static OptionItem EnableShield;
        public static IntegerOptionItem TimeToActivate;
        public static IntegerOptionItem DetectionDelay;
        //Word
        public static OptionItem WordList;
        public static OptionItem AutoKickStopWords;
        public static OptionItem AutoKickStopWordsAsBan;
        public static IntegerOptionItem AutoKickStopWordsTimes;
        public static OptionItem SendAutoKickStopWordsMsg;
        //SpamStart
        public static OptionItem SpamList;
        public static OptionItem AutoKickStart;
        public static OptionItem AutoKickStartAsBan;
        public static IntegerOptionItem AutoKickStartTimes;
        public static OptionItem SendAutoKickStartMsg;
        //Block
        public static OptionItem ApplyDenyNameList;
        public static OptionItem CheckBanList;
        public static OptionItem CheckBlockList;
        public static OptionItem CheckFriendCode;
        
        public static StringOptionItem MalumActionOption;
        public static OptionItem ShowMsgAlert;
        public static OptionItem sendInfocomand;
        public static OptionItem AktiveLobby;
        public static OptionItem sharelobby;
        public static OptionItem DisableLobbyMusic;
        public static OptionItem EnableZoom;
        public static OptionItem DarkTheme;
        public static OptionItem dleks;
        public static OptionItem buttonvisibile;
        public static OptionItem RetoreModdedName;
        public static OptionItem Taskremain;
        public static OptionItem EngineerFixer;
        public static StringOptionItem ExilerAction;
        public static IntegerOptionItem VentTimes;
        public static OptionItem sendwelcome;
        public static OptionItem ScientistTime;
        public static OptionItem KickInvalidVersion;
        public static OptionItem changename;
        public static OptionItem renamename;
        public static OptionItem changecolor;
        public static OptionItem Guess;
        public static OptionItem ImpostorGuess;
        public static OptionItem ExilerExe;
        public static OptionItem namewithid;
        public static OptionItem SendSummary;
        public static OptionItem Immortalesentvote;

        public static OptionItem ProtectFirst;
        public static OptionItem EnableImmortal;
        public static OptionItem sendkillmessage;
        public static OptionItem sendtoimmortal;

        public static OptionItem Enablesabotage;

        public static OptionItem DisableAllSabotages;
        public static OptionItem DisableReactorSabotage;
        public static OptionItem DisableCommsSabotage;
        public static OptionItem DisableO2Sabotage;
        public static OptionItem DisableElectricalSabotage;
        public static OptionItem DisableLaboratorySabotage;
        public static OptionItem DisableHeliSabotage;
        public static OptionItem DisableMushroomSabotage;
        public static OptionItem DisableDoorSabotage;
        public static OptionItem DisableSecurity;
        public static OptionItem DisableHQ;
        public static OptionItem DisableFungleCam;
        public static OptionItem DisableAdmin;



        public static bool ForceOwnLanguage = true;
        public static bool IsLoaded = false;

        public static void Load()
        {
            if (IsLoaded) return;
            //Host
            sharelobby = BooleanOptionItem.Create(0, "sharelobby", true, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            namewithid = BooleanOptionItem.Create(1, "namewithid", false, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            Taskremain = BooleanOptionItem.Create(2, "Taskremain", true, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            Enablesabotage = BooleanOptionItem.Create(3, "Enablesabotage", true, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            DarkTheme = BooleanOptionItem.Create(4, "DarkTheme", true, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            AktiveLobby = BooleanOptionItem.Create(5, "AktiveLobby", true, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            buttonvisibile = BooleanOptionItem.Create(6, "buttonvisibile", true, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            dleks = BooleanOptionItem.Create(7, "dleks", false, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            EnableZoom = BooleanOptionItem.Create(8, "EnableZoom", false, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            DisableLobbyMusic = BooleanOptionItem.Create(9, "DisableLobbyMusic", true, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            ShowMsgAlert = BooleanOptionItem.Create(10, "ShowMsgAlert", true, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            sendInfocomand = BooleanOptionItem.Create(11, "sendInfocomand", true, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            // MalumActionOption = (StringOptionItem)StringOptionItem.Create(11, "Malum Action", new[] { "nothing", "notify_host", "notify_all", "notify_player", "kick" }, 0, OptionCategory.Host, true).SetColor(new Color32(0, 153, 255, 255));
            
             //crewmate
             ProtectFirst = BooleanOptionItem.Create(100, "ProtectFirst", true, OptionCategory.Crewmate, true).SetColor(new Color32(0, 153, 255, 255));
            Guess = BooleanOptionItem.Create(101, "Guess", true, OptionCategory.Crewmate, true).SetColor(new Color32(0, 153, 255, 255));
            sendkillmessage = BooleanOptionItem.Create(102, "sendkillmessage", true, OptionCategory.Impostor, true).SetColor(new Color32(0, 153, 255, 255));
            //immortal
            EnableImmortal = BooleanOptionItem.Create(200, "EnableImmortal", true, OptionCategory.Immortal, true).SetColor(new Color32(0, 153, 255, 255));
            sendtoimmortal = BooleanOptionItem.Create(201, "sendtoimmortal", true, OptionCategory.Immortal, true).SetColor(new Color32(0, 153, 255, 255));
            Immortalesentvote = BooleanOptionItem.Create(202, "Immortalesentvote", true, OptionCategory.Immortal, true).SetColor(new Color32(0, 153, 255, 255));
            //engineer
            EngineerFixer = BooleanOptionItem.Create(300, "EnginerFixer", true, OptionCategory.Engineer, true).SetColor(new Color32(0, 153, 255, 255));
            VentTimes = (IntegerOptionItem)IntegerOptionItem.Create(301, "VentTimes", new(0, 5, 1), 3, OptionCategory.Engineer, true).SetColor(new Color32(0, 153, 255, 255));
            //phantom
            ImpostorGuess = BooleanOptionItem.Create(400, "ImpostorGuess", true, OptionCategory.Impostor, true).SetColor(new Color32(0, 153, 255, 255));
            //scientist
            ScientistTime = BooleanOptionItem.Create(500, "ScientistTime", true, OptionCategory.Scientist, true).SetColor(new Color32(0, 153, 255, 255));
            //exiler
            ExilerExe = BooleanOptionItem.Create(600, "ExilerExe", true, OptionCategory.Exiler, true).SetColor(new Color32(0, 153, 255, 255));
            ExilerAction = (StringOptionItem)StringOptionItem.Create(601, "ExilerAction", new[] { "Kill", "Exile" }, 0, OptionCategory.Exiler, true).SetColor(new Color32(0, 153, 255, 255));
            //General
            RetoreModdedName = BooleanOptionItem.Create(700, "RetoreModdedName", true, OptionCategory.General, true).SetColor(new Color32(0, 153, 255, 255));
            changename = BooleanOptionItem.Create(701, "changename", true, OptionCategory.General, true).SetColor(new Color32(0, 153, 255, 255));
            renamename = BooleanOptionItem.Create(702, "renamename", false, OptionCategory.General, true).SetColor(new Color32(0, 153, 255, 255));
            changecolor = BooleanOptionItem.Create(703, "changecolor", true, OptionCategory.General, true).SetColor(new Color32(0, 153, 255, 255));
            sendwelcome = BooleanOptionItem.Create(704, "sendwelcome", true, OptionCategory.General, true).SetColor(new Color32(0, 153, 255, 255));
            SendSummary = BooleanOptionItem.Create(705, "SendSummary", false, OptionCategory.General, true).SetColor(new Color32(0, 153, 255, 255));

            //Afk
            EnableDetector = BooleanOptionItem.Create(800, "EnableAFKDetector", true, OptionCategory.Afk, true).SetColor(new Color32(0, 153, 255, 255));
            EnableAfkKick = BooleanOptionItem.Create(801, "EnableAfkKick", true, OptionCategory.Afk, true).SetColor(new Color32(0, 153, 255, 255));
            EnableShield = BooleanOptionItem.Create(802, "EnableShield", true, OptionCategory.Afk, true).SetColor(new Color32(0, 153, 255, 255));
            DetectionDelay = (IntegerOptionItem)IntegerOptionItem.Create(803, "AFKDetectionDelay(sec)", new(5, 60, 5), 45, OptionCategory.Afk, true).SetColor(new Color32(0, 153, 255, 255));
            TimeToActivate = (IntegerOptionItem)IntegerOptionItem.Create(804, "TimeToActivate(min)", new(1, 10, 1), 1, OptionCategory.Afk, true).SetColor(new Color32(0, 153, 255, 255));
            //Denyname
            ApplyDenyNameList = BooleanOptionItem.Create(900, "ApplyDenyNameList", true, OptionCategory.Blocklist, true).SetColor(new Color32(0, 153, 255, 255));
            CheckBanList = BooleanOptionItem.Create(901, "CheckBanList", true, OptionCategory.Blocklist, true).SetColor(new Color32(0, 153, 255, 255));
            CheckBlockList = BooleanOptionItem.Create(902, "CheckBlockList", true, OptionCategory.Blocklist, true).SetColor(new Color32(0, 153, 255, 255));
            CheckFriendCode = BooleanOptionItem.Create(903, "CheckFriendCode", true, OptionCategory.Blocklist, true).SetColor(new Color32(0, 153, 255, 255));
            //spamstart
            AutoKickStart = BooleanOptionItem.Create(1000, "AutoKickStart", true, OptionCategory.Spamlist, true).SetColor(new Color32(0, 153, 255, 255));
            AutoKickStartAsBan = BooleanOptionItem.Create(1001, "AutoKickStartAsBan", false, OptionCategory.Spamlist, true).SetColor(new Color32(0, 153, 255, 255));
            AutoKickStartTimes = (IntegerOptionItem)IntegerOptionItem.Create(1002, "AutoKickStartTimes", new(0, 5, 1), 2, OptionCategory.Spamlist, true).SetColor(new Color32(0, 153, 255, 255));
            SendAutoKickStartMsg = BooleanOptionItem.Create(1003, "SendAutoKickStartMsg", true, OptionCategory.Spamlist, true).SetColor(new Color32(0, 153, 255, 255));
            //spamword
            AutoKickStopWords = BooleanOptionItem.Create(1100, "AutoKickStopWords", true, OptionCategory.Wordlist, true).SetColor(new Color32(0, 153, 255, 255));
            AutoKickStopWordsAsBan = BooleanOptionItem.Create(1101, "AutoKickStopWordsAsBan", true, OptionCategory.Wordlist, true).SetColor(new Color32(0, 153, 255, 255));
            AutoKickStopWordsTimes = (IntegerOptionItem)IntegerOptionItem.Create(1102, "AutoKickStopWordsTimes", new(0, 5, 1), 1, OptionCategory.Wordlist, true).SetValueFormat(OptionFormat.Times).SetColor(new Color32(0, 153, 255, 255));
            SendAutoKickStopWordsMsg = BooleanOptionItem.Create(1103, "SendAutoKickStopWordsMsg", true, OptionCategory.Wordlist, true).SetColor(new Color32(0, 153, 255, 255));

            DisableAllSabotages = BooleanOptionItem.Create(1200, "DisableAllSabotages", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableReactorSabotage = BooleanOptionItem.Create(1201, "DisableReactorSabotage", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableCommsSabotage = BooleanOptionItem.Create(1202, "DisableCommsSabotage", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableO2Sabotage = BooleanOptionItem.Create(1203, "DisableO2Sabotage", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableElectricalSabotage = BooleanOptionItem.Create(1204, "DisableElectricalSabotage", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableLaboratorySabotage = BooleanOptionItem.Create(1205, "DisableLaboratorySabotage", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableHeliSabotage = BooleanOptionItem.Create(1206, "DisableHeliSabotage", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableMushroomSabotage = BooleanOptionItem.Create(1207, "DisableMushroomSabotage", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableAdmin = BooleanOptionItem.Create(1208, "DisableAdmin", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableDoorSabotage = BooleanOptionItem.Create(1209, "DisableDoorSabotage", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableSecurity = BooleanOptionItem.Create(1210, "DisableSecurity", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableHQ = BooleanOptionItem.Create(1211, "DisableHQ", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableFungleCam = BooleanOptionItem.Create(1212, "DisableFungleCam", false, OptionCategory.Sabotage, true).SetColor(new Color32(0, 153, 255, 255));
            DisableAllSabotages.RegisterUpdateValueEvent((sender, args) =>
            {
                bool disableOthers = ((OptionItem)sender).GetBool();

                DisableReactorSabotage.SetEnabled(!disableOthers);
                DisableCommsSabotage.SetEnabled(!disableOthers);
                DisableO2Sabotage.SetEnabled(!disableOthers);
                DisableElectricalSabotage.SetEnabled(!disableOthers);
                DisableLaboratorySabotage.SetEnabled(!disableOthers);
                DisableHeliSabotage.SetEnabled(!disableOthers);
                DisableMushroomSabotage.SetEnabled(!disableOthers);

                if (disableOthers)
                {
                    DisableReactorSabotage.SetValue(0);
                    DisableCommsSabotage.SetValue(0);
                    DisableO2Sabotage.SetValue(0);
                    DisableElectricalSabotage.SetValue(0);
                    DisableLaboratorySabotage.SetValue(0);
                    DisableHeliSabotage.SetValue(0);
                    DisableMushroomSabotage.SetValue(0);
                }
            });
            IsLoaded = true;
        }
    }
}
