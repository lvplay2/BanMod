using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.Data;
using AmongUs.GameOptions;
using HarmonyLib;
using InnerNet;
using Rewired.Utils.Platforms.Windows;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;


namespace BanMod;

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public static class GameStartManagerUpdatePatch
{
    public static void Prefix(GameStartManager __instance)
    {
        {
            __instance.MinPlayers = 1;
        }
    }
}

public static class GameStartManagerPatch
{
    public static float Timer { get; set; } = 600f;
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    public class GameStartManagerStartPatch
    {
        public static TextMeshPro HideName;
        public static TextMeshPro GameCountdown;

        public static void Postfix(GameStartManager __instance)
        {
            {
                if (__instance == null) return;

                var temp = __instance.PlayerCounter;
                GameCountdown = Object.Instantiate(temp, __instance.StartButton.transform);
                GameCountdown.text = string.Empty;


                if (AmongUsClient.Instance.AmHost)
                {
                    __instance.GameStartTextParent.GetComponent<SpriteRenderer>().sprite = null;
                    __instance.StartButton.ChangeButtonText(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.StartLabel));
                    __instance.GameStartText.transform.localPosition = new(__instance.GameStartText.transform.localPosition.x, 2f, __instance.GameStartText.transform.localPosition.z);
                    __instance.StartButton.activeTextColor = __instance.StartButton.inactiveTextColor = Color.white;

                    __instance.EditButton.activeTextColor = __instance.EditButton.inactiveTextColor = Color.black;
                    __instance.EditButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new(0f, 0.647f, 1f, 1f);
                    __instance.EditButton.activeSprites.GetComponent<SpriteRenderer>().color = new(0f, 0.847f, 1f, 1f);
                    __instance.EditButton.inactiveSprites.transform.Find("Shine").GetComponent<SpriteRenderer>().color = new(0f, 1f, 1f, 0.5f);

                    __instance.HostViewButton.activeTextColor = __instance.HostViewButton.inactiveTextColor = Color.black;
                    __instance.HostViewButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new(0f, 0.647f, 1f, 1f);
                    __instance.HostViewButton.activeSprites.GetComponent<SpriteRenderer>().color = new(0f, 0.847f, 1f, 1f);
                    __instance.HostViewButton.inactiveSprites.transform.Find("Shine").GetComponent<SpriteRenderer>().color = new(0f, 1f, 1f, 0.5f);
                }

                if (AmongUsClient.Instance == null || AmongUsClient.Instance.IsGameStarted || GameStates.InGame || __instance.startState == GameStartManager.StartingStates.Starting) return;

                // Reset lobby countdown timer
                Timer = 600f;
           
                if (!AmongUsClient.Instance.AmHost) return;
            }
        }

    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    public class GameStartManagerUpdatePatch
    {

        public static bool Prefix(GameStartManager __instance)
        {
            if (AmongUsClient.Instance.AmHost)
                VanillaUpdate(__instance);
            if (AmongUsClient.Instance == null || GameData.Instance == null || !AmongUsClient.Instance.AmHost || !GameData.Instance) return true;
            return false;
        }

        private static void VanillaUpdate(GameStartManager instance)
        {
            if (!GameData.Instance || !GameManager.Instance) return;

            try
            {
                instance.UpdateMapImage((MapNames)GameManager.Instance.LogicOptions.MapId);
            }
            catch (Exception)
            {
            }

            instance.CheckSettingsDiffs();
            instance.StartButton.gameObject.SetActive(true);
            instance.RulesPresetText.text = DestroyableSingleton<TranslationController>.Instance.GetString(GameOptionsManager.Instance.CurrentGameOptions.GetRulesPresetTitle());
            if (GameCode.IntToGameName(AmongUsClient.Instance.GameId) == null) instance.privatePublicPanelText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.LocalButton);
            else if (AmongUsClient.Instance.IsGamePublic) instance.privatePublicPanelText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PublicHeader);
            else instance.privatePublicPanelText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.PrivateHeader);
            instance.HostPrivateButton.gameObject.SetActive(!AmongUsClient.Instance.IsGamePublic);
            instance.HostPublicButton.gameObject.SetActive(AmongUsClient.Instance.IsGamePublic);
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.C))
                ClipboardHelper.PutClipboardString(GameCode.IntToGameName(AmongUsClient.Instance.GameId));
            if (GameData.Instance.PlayerCount != instance.LastPlayerCount)
            {
                instance.LastPlayerCount = GameData.Instance.PlayerCount;
                string text = "<color=#FF0000FF>";
                if (instance.LastPlayerCount > instance.MinPlayers) text = "<color=#00FF00FF>";
                if (instance.LastPlayerCount == instance.MinPlayers) text = "<color=#FFFF00FF>";
                instance.PlayerCounter.text = $"{text}{instance.LastPlayerCount}/{(AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame ? 15 : GameManager.Instance.LogicOptions.MaxPlayers)}";
                instance.StartButton.SetButtonEnableState(instance.LastPlayerCount >= instance.MinPlayers);
                ActionMapGlyphDisplay startButtonGlyph = instance.StartButtonGlyph;
                startButtonGlyph?.SetColor((instance.LastPlayerCount >= instance.MinPlayers) ? Palette.EnabledColor : Palette.DisabledClear);
                if (DestroyableSingleton<DiscordManager>.InstanceExists)
                {
                    if (AmongUsClient.Instance.AmHost && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
                        DestroyableSingleton<DiscordManager>.Instance.SetInLobbyHost(instance.LastPlayerCount, GameManager.Instance.LogicOptions.MaxPlayers, AmongUsClient.Instance.GameId);
                    else DestroyableSingleton<DiscordManager>.Instance.SetInLobbyClient(instance.LastPlayerCount, GameManager.Instance.LogicOptions.MaxPlayers, AmongUsClient.Instance.GameId);
                }
            }

            if (AmongUsClient.Instance.AmHost)
            {
                if (instance.startState == GameStartManager.StartingStates.Countdown)
                {
                    instance.StartButton.ChangeButtonText(string.Format("STOP"));
                    instance.StartButton.DestroyTranslator();
                    instance.StartButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new(0.8f, 0f, 0f, 1f);
                    instance.StartButton.activeSprites.GetComponent<SpriteRenderer>().color = Color.red;
                    instance.StartButton.inactiveSprites.transform.Find("Shine").GetComponent<SpriteRenderer>().color = new(0.8f, 0.4f, 0.4f, 1f);
                    instance.StartButton.activeTextColor = instance.StartButton.inactiveTextColor = Color.white;
                    int num = Mathf.CeilToInt(instance.countDownTimer);
                    instance.countDownTimer -= Time.deltaTime;
                    int num2 = Mathf.CeilToInt(instance.countDownTimer);
                    if (!instance.GameStartTextParent.activeSelf) SoundManager.Instance.PlaySound(instance.gameStartSound, false);
                    instance.GameStartTextParent.SetActive(true);
                    instance.GameStartText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.GameStarting, num2);
                    if (num != num2) PlayerControl.LocalPlayer.RpcSetStartCounter(num2);
                    if (num2 <= 0) instance.FinallyBegin();
                }
                else
                {
                    instance.StartButton.ChangeButtonText(DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.StartLabel));
                    instance.StartButton.inactiveSprites.GetComponent<SpriteRenderer>().color = new(0.1f, 0.1f, 0.1f, 1f);
                    instance.StartButton.activeSprites.GetComponent<SpriteRenderer>().color = new(0.2f, 0.2f, 0.2f, 1f);
                    instance.StartButton.inactiveSprites.transform.Find("Shine").GetComponent<SpriteRenderer>().color = new(0.3f, 0.3f, 0.3f, 0.5f);
                    instance.StartButton.activeTextColor = instance.StartButton.inactiveTextColor = Color.white;
                    instance.GameStartTextParent.SetActive(false);
                    instance.GameStartText.text = string.Empty;
                }
            }

            if (instance.LobbyInfoPane.gameObject.activeSelf && DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening) instance.LobbyInfoPane.DeactivatePane();
            instance.LobbyInfoPane.gameObject.SetActive(!DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening);
        }
        public static void Postfix(GameStartManager __instance)
        {
            {
                if (AmongUsClient.Instance == null || AmongUsClient.Instance.IsGameStarted || GameStates.InGame || __instance == null || __instance.startState == GameStartManager.StartingStates.Starting) return;



                if (AmongUsClient.Instance.AmHost)
                {

                    {
                        __instance.StartButton.gameObject.SetActive(true);
                    }
                    Timer = Mathf.Max(0f, Timer -= Time.deltaTime);
                    int minutes = (int)Timer / 60;
                    int seconds = (int)Timer % 60;
                    string suffix = $"{minutes:00}:{seconds:00}";
                    if (Timer <= 60) suffix = Utils.ColorString((int)Timer % 2 == 0 ? Color.yellow : Color.red, suffix);

                    TextMeshPro tmp = GameStartManagerStartPatch.GameCountdown;

                    if (tmp.text == string.Empty)
                    {
                        tmp.name = "LobbyTimer";
                        tmp.fontSize = tmp.fontSizeMin = tmp.fontSizeMax = 5f;
                        tmp.autoSizeTextContainer = true;
                        tmp.alignment = TextAlignmentOptions.Center;
                        tmp.color = Color.cyan;
                        tmp.outlineColor = Color.black;
                        tmp.outlineWidth = 0.4f;
                        tmp.transform.localPosition += new Vector3(-0.8f, -0.42f, 0f);
                        tmp.transform.localScale = new(0.5f, 0.5f, 1f);
                    }

                    tmp.text = suffix;

                }
                else
                {

                    {
                        __instance.StartButton.gameObject.SetActive(false);
                    }

                }
            }
        }


    }

}



[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.ResetStartState))]
class ResetStartStatePatch
{
    public static void Prefix(GameStartManager __instance)
    {
        SoundManager.Instance.StopSound(__instance.gameStartSound);

    }
}


public static class GameStartManagerBeginPatch
{
    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.BeginGame))]
    public class GameStartManagerStartPatch
    {
        public static bool Prefix(GameStartManager __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;

            if (__instance.startState == GameStartManager.StartingStates.Countdown)
            {
                __instance.ResetStartState();
                return false;
            }

            __instance.startState = GameStartManager.StartingStates.Countdown;
            __instance.GameSizePopup.SetActive(false);
            DataManager.Player.Onboarding.AlwaysShowMinPlayerWarning = false;
            DataManager.Player.Onboarding.ViewedMinPlayerWarning = true;
            DataManager.Player.Save();
            __instance.StartButton.gameObject.SetActive(false);
            __instance.StartButtonClient.gameObject.SetActive(false);
            __instance.GameStartTextParent.SetActive(false);
            __instance.countDownTimer = 5.0001f;
            __instance.startState = GameStartManager.StartingStates.Countdown;
            AmongUsClient.Instance.KickNotJoinedPlayers();
            return false;
        }
    }
}