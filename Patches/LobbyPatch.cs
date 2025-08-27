using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using Hazel;
using InnerNet;
using Rewired.Utils.Platforms.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static BanMod.Utils;

namespace BanMod;

[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Start))]
public class LobbyStartPatch
{
    private static GameObject LobbyPaintObject;
    private static GameObject DecorationsObject;
    private static Sprite LobbyPaintSprite;
    private static Sprite DropshipDecorationsSprite;
    public static bool hasSentSummary = false;


    public static void Prefix()
    {
        LobbyPaintSprite = Utils.LoadSprite("BanMod.Resources.image.LobbyPaint.png", 290f);
        DropshipDecorationsSprite = Utils.LoadSprite("BanMod.Resources.image.Decoration.png", 60f);
    }
    public static void Postfix(LobbyBehaviour __instance)
    {
        __instance.StartCoroutine(CoLoadDecorations().WrapToIl2Cpp());
        if (AmongUsClient.Instance.AmHost && Options.SendSummary.GetBool())
        {
            __instance.StartCoroutine(SendSummaryDelayed().WrapToIl2Cpp());
        }

        static System.Collections.IEnumerator CoLoadDecorations()
        {
            var LeftBox = GameObject.Find("Leftbox");
            if (LeftBox != null)
            {
                LobbyPaintObject = Object.Instantiate(LeftBox, LeftBox.transform.parent.transform);
                LobbyPaintObject.name = "Lobby Paint";
                LobbyPaintObject.transform.localPosition = new Vector3(0.042f, -2.59f, -10.5f);
                SpriteRenderer renderer = LobbyPaintObject.GetComponent<SpriteRenderer>();
                renderer.sprite = LobbyPaintSprite;
            }

            yield return null;

            if (Options.AktiveLobby.GetBool())
            {
                var Dropship = GameObject.Find("SmallBox");
                if (Dropship != null)
                {
                    DecorationsObject = Object.Instantiate(Dropship, Object.FindAnyObjectByType<LobbyBehaviour>().transform);
                    DecorationsObject.name = "Lobby_Decorations";
                    DecorationsObject.transform.DestroyChildren();
                    Object.Destroy(DecorationsObject.GetComponent<PolygonCollider2D>());
                    DecorationsObject.GetComponent<SpriteRenderer>().sprite = DropshipDecorationsSprite;
                    DecorationsObject.transform.SetSiblingIndex(1);
                    DecorationsObject.transform.localPosition = new(0.05f, 0.8334f);

                }
            }

            yield return null;

        }
    }
    private static System.Collections.IEnumerator SendSummaryDelayed()
    {
        // Attendi 15 secondi reali
        yield return new WaitForSeconds(10f);

        // Previeni invii doppi
        if (LobbyStartPatch.hasSentSummary)
            yield break;

        string report = MatchSummary.GetLastSavedReport();
        if (!string.IsNullOrWhiteSpace(report))
        {
            Utils.SendMessage("", 255, report);
            MessageBlocker.UpdateLastMessageTime();
            LobbyStartPatch.hasSentSummary = true;
            Logger.Info("Report inviato automaticamente dopo 15 secondi.");
        }
    }
}
// https://github.com/SuperNewRoles/SuperNewRoles/blob/master/SuperNewRoles/Patches/LobbyBehaviourPatch.cs
[HarmonyPatch(typeof(LobbyBehaviour))]
public class LobbyBehaviourPatch
{
    [HarmonyPatch(nameof(LobbyBehaviour.Update)), HarmonyPostfix]
    public static void Update_Postfix(LobbyBehaviour __instance)
    {
        System.Func<ISoundPlayer, bool> lobbybgm = x => x.Name.Equals("MapTheme");
        ISoundPlayer MapThemeSound = SoundManager.Instance.soundPlayers.Find(lobbybgm);
        if (Options.DisableLobbyMusic.GetBool())
        {
            if (MapThemeSound == null) return;
            SoundManager.Instance.StopNamedSound("MapTheme");
        }
        else
        {
            if (MapThemeSound != null) return;
            SoundManager.Instance.CrossFadeSound("MapTheme", __instance.MapTheme, 0.5f);
        }
    }
}
[HarmonyPatch(typeof(LobbyBehaviour), nameof(LobbyBehaviour.Update))]
public static class LobbyBehaviour_Update_Patch
{
    public static Dictionary<string, float> playerJoinTimes = new Dictionary<string, float>();
    public static Dictionary<string, float> playersToMessage = new Dictionary<string, float>();
    public static HashSet<string> messagedPlayers = new HashSet<string>();
    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost)
            return;
        if (GameData.Instance == null || GameData.Instance.AllPlayers == null || !Options.sendwelcome.GetBool())
            return;

        float currentTime = Time.time;


        foreach (var player in BanMod.AllPlayerControls)
        {
            var friendCode = player.Data?.FriendCode;
            if (string.IsNullOrEmpty(friendCode)) continue;

            // Registra orario entrata player, se non già fatto
            if (!playerJoinTimes.ContainsKey(friendCode))
            {
                playerJoinTimes[friendCode] = currentTime;
                Logger.Info($"Registrato orario entrata per {friendCode} a {currentTime}");
            }

            // Dopo 5 secondi salva il nome originale se non è già stato salvato
            if (currentTime - playerJoinTimes[friendCode] >= 2.0f)
            {

                if (!ExtendedPlayerControl.originalNamesByFriendCode.ContainsKey(friendCode))
                {
                    ExtendedPlayerControl.originalNamesByFriendCode[friendCode] = player.name;
                    Logger.Info($"Salvato nome originale: {player.name} per FriendCode {friendCode} dopo 2 secondi");
                }

            }

            // Programma invio messaggio di benvenuto (se non ancora fatto e player vivo)
            if (!playersToMessage.ContainsKey(friendCode) && !messagedPlayers.Contains(friendCode) && !player.Data.IsDead)
            {
                playersToMessage[friendCode] = currentTime + 1f;
            }

        }

        // Invia messaggi programmati
        var toSend = new List<string>();
        foreach (var kvp in playersToMessage)
        {
            if (currentTime >= kvp.Value)
            {
                toSend.Add(kvp.Key);
            }
        }

        foreach (var friendCode in toSend)
        {
            var player = BanMod.AllPlayerControls.FirstOrDefault(p => p.Data?.FriendCode == friendCode);
            if (player != null)
            {
                string name = player.GetRealName() ?? player.name ?? "Player";
                string title = TemplateLoader.FormatTemplate("WelcomeTemplate", name);
                Utils.SendMessage("", player.PlayerId, title);
                MessageBlocker.UpdateLastMessageTime();
            }
            playersToMessage.Remove(friendCode);
            messagedPlayers.Add(friendCode);

        }
    }

}
