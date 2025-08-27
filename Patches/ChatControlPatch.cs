using AmongUs.Data;
using AmongUs.GameOptions;
using BepInEx.Logging;
using HarmonyLib;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using static BanMod.Utils;

namespace BanMod;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
class ChatControllerUpdatePatch
{
    private static SpriteRenderer QuickChatIcon;
    private static SpriteRenderer OpenBanMenuIcon;
    private static SpriteRenderer OpenKeyboardIcon;
    public static int CurrentHistorySelection = -1;
    public static bool timelastmessage;
    public static void Prefix()
    {
        if (AmongUsClient.Instance.AmHost && DataManager.Settings.Multiplayer.ChatMode == InnerNet.QuickChatModes.QuickChatOnly)
            DataManager.Settings.Multiplayer.ChatMode = InnerNet.QuickChatModes.FreeChatOrQuickChat;

    }
    public static ChatController Instance;

    public static void Postfix(ChatController __instance)
    {
        if (Instance == null)
            Instance = __instance;
        
        if (__instance.timeSinceLastMessage > 3.3f)
        {
            timelastmessage = true;
        }
        else timelastmessage = false;

        if (!__instance.freeChatField.textArea.hasFocus) return;

        __instance.freeChatField.textArea.characterLimit = 120;

        if (Options.DarkTheme.GetBool())
        {

            // free chat
            __instance.freeChatField.background.color = new(0.1f, 0.1f, 0.1f, 1f);
            __instance.freeChatField.textArea.compoText.Color(Color.white);
            __instance.freeChatField.textArea.outputText.color = Color.white;

            // quick chat
            __instance.quickChatField.background.color = new(0.1f, 0.1f, 0.1f, 1f);
            __instance.quickChatField.text.color = Color.white;

            if (QuickChatIcon == null)
                QuickChatIcon = GameObject.Find("QuickChatIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                QuickChatIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.DarkQuickChat.png", 100f);

            if (OpenBanMenuIcon == null)
                OpenBanMenuIcon = GameObject.Find("OpenBanMenuIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenBanMenuIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.DarkReport.png", 100f);

            if (OpenKeyboardIcon == null)
                OpenKeyboardIcon = GameObject.Find("OpenKeyboardIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenKeyboardIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.DarkKeyboard.png", 100f);

            if (GameStates.IsDead)
                __instance.freeChatField.background.color = new(0.1f, 0.1f, 0.1f, 0.6f);
                __instance.quickChatField.background.color = new(0.1f, 0.1f, 0.1f, 0.6f);
        }

        else
        {
            __instance.freeChatField.textArea.outputText.color = Color.black;
            if (QuickChatIcon == null)
                QuickChatIcon = GameObject.Find("QuickChatIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                QuickChatIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.QuickChat.png", 100f);

            if (OpenBanMenuIcon == null)
                OpenBanMenuIcon = GameObject.Find("OpenBanMenuIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenBanMenuIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.Report.png", 100f);

            if (OpenKeyboardIcon == null)
                OpenKeyboardIcon = GameObject.Find("OpenKeyboardIcon")?.transform.GetComponent<SpriteRenderer>();
            else
                OpenKeyboardIcon.sprite = Utils.LoadSprite("BanMod.Resources.image.Keyboard.png", 100f);
        }


        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.V))
            __instance.freeChatField.textArea.SetText(__instance.freeChatField.textArea.text + GUIUtility.systemCopyBuffer);

        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.X))
        {
            ClipboardHelper.PutClipboardString(__instance.freeChatField.textArea.text);
            __instance.freeChatField.textArea.SetText("");
        }
         __instance.freeChatField.textArea.characterLimit = 120;
        
        if (Input.GetKeyDown(KeyCode.UpArrow) && ChatCommands.ChatHistory.Any())
        {
            CurrentHistorySelection = Mathf.Clamp(--CurrentHistorySelection, 0, ChatCommands.ChatHistory.Count - 1);
            __instance.freeChatField.textArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow) && ChatCommands.ChatHistory.Any())
        {
            CurrentHistorySelection++;
            if (CurrentHistorySelection < ChatCommands.ChatHistory.Count)
                __instance.freeChatField.textArea.SetText(ChatCommands.ChatHistory[CurrentHistorySelection]);
            else __instance.freeChatField.textArea.SetText("");
        }
    }
    
}
[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
class ChatSendPatch
{
    static bool Prefix(ChatController __instance)
    {
        // BLOCCA se il messaggio è troppo lungo
        string message = __instance.freeChatField?.textArea?.text;
        if (!string.IsNullOrEmpty(message) && message.Length > 120)
        {
            Debug.LogError("Messaggio troppo lungo! Max 120 caratteri.");
            return false; // BLOCCA invio
        }

        // BLOCCA se non sono passati 3.3 secondi
        if (__instance.timeSinceLastMessage <= 3.3f)
        {
            Debug.LogWarning("Stai inviando messaggi troppo velocemente!");
            return false; // BLOCCA invio
        }

        if (!MessageBlocker.CanSendMessage())
        {
            if (!MessageBlocker.CanSendMessage())
            {
                if (Options.ShowMsgAlert.GetBool())
                {
                    HudManager.Instance.Notifier.AddDisconnectMessage(Translator.GetString("Waitasecond"));
                }
                return false;
            }
        }

        MessageBlocker.UpdateLastMessageTime(); // ?? AGGIORNA IL TEMPO
        return true; // consenti invio
    }
            
}
[HarmonyPatch(typeof(FreeChatInputField), nameof(FreeChatInputField.UpdateCharCount))]
internal class UpdateCharCountPatch
{
    public static void Postfix(FreeChatInputField __instance)
    {
        int length = __instance.textArea.text.Length;
        __instance.charCountText.SetText(length <= 0 ? Translator.GetString("BANMOD") : $"{length}/{__instance.textArea.characterLimit}");
        __instance.charCountText.enableWordWrapping = false;
        if (length < (AmongUsClient.Instance.AmHost ? 80 : 100))
            __instance.charCountText.color = Color.cyan;
        else if (length < (AmongUsClient.Instance.AmHost ? 101 : 120))
            __instance.charCountText.color = new Color(1f, 1f, 0f, 1f);
        else
            __instance.charCountText.color = Color.red;
    }
}
