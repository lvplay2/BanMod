using BepInEx.Unity.IL2CPP.Utils;
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
using static FilterPopUp.FilterInfoUI;
using static UnityEngine.GraphicsBuffer;

namespace BanMod
{
    public static class ExilerManager
    {
        public static GameObject ExilerUI = null;
        public static IEnumerator WaitForButtonsAndCreate(MeetingHud __instance)
        {
            Logger.Info("Inizio coroutine WaitForButtonsAndCreate...");

            while (__instance == null)
            {
                Logger.Info("__instance è null, aspetto...");
                yield return null;
            }

            while (__instance.playerStates == null || __instance.playerStates.Count == 0)
            {
                Logger.Info("playerStates è null o vuoto, aspetto...");
                yield return null;
            }

            while (__instance.playerStates.All(pva => pva == null || pva.Buttons == null))
            {
                Logger.Info("Bottoni non inizializzati, aspetto...");
                yield return null;
            }

            Logger.Info("Bottoni pronti. Procedo alla creazione dei Exiler button.");

            CreateExilerButton(__instance);
        }

        public static void CreateExilerButton(MeetingHud __instance)
        {
            if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == Exiler.ExilerId)
            {
                foreach (var pva in __instance.playerStates.ToArray())
                {
                    var pc = Utils.GetPlayerById(pva.TargetPlayerId);

                    if (pc == null || !pc.IsAlive()) continue;


                    GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
                    targetBox.name = "ShootButton1";
                    targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f);


                    var sprite = Utils.LoadSprite("BanMod.Resources.image.TargetIcon1.png", 100f);
                    if (sprite == null)
                    {
                        Logger.Info("❌ Impossibile caricare l'icona TargetIcon1.png. Controlla il path e che sia una risorsa embedded.");
                    }
                    else
                    {
                        SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                        if (renderer != null)
                        {
                            renderer.sprite = sprite;
                            Logger.Info("✅ Sprite TargetIcon1 assegnato correttamente al bottone.");
                        }
                        else
                        {
                            Logger.Info("⚠️ SpriteRenderer non trovato nel bottone.");
                        }
                    }


                    PassiveButton button = targetBox.GetComponent<PassiveButton>();
                    button.OnClick.RemoveAllListeners();
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        if (PlayerControl.LocalPlayer.Data.IsDead || ExilerUI != null)
                            return;

                        ExilerOnClick(pva.TargetPlayerId, __instance);
                    }));
                }
            }
        }

        static void ExilerOnClick(byte playerId, MeetingHud __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                var pc = Utils.GetPlayerById(playerId);

                if (pc == null || !pc.IsAlive() || ExilerUI != null) return;

                ShowHostConfirmationUI3(playerId, __instance);
            }
            else
            {
                var pc = Utils.GetPlayerById(playerId);

                if (pc == null || !pc.IsAlive() || ExilerUI != null) return;
                ShowHostConfirmationUI4(playerId, __instance);
            }
        }

        static void ShowHostConfirmationUI3(byte playerId, MeetingHud __instance)
        {
            var pc = Utils.GetPlayerById(playerId);
            if (pc == null || !pc.IsAlive() || ExilerUI != null) return;

            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));
            __instance.SkipVoteButton.gameObject.SetActive(false);
            var closeMeetingButton = __instance.transform.Find("CloseMeetingButton");
            if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(false);

            GameObject canvasGO = new GameObject("ExilerUICanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject container = new GameObject("ExilerUIContainer");
            container.transform.SetParent(canvasGO.transform, false);
            ExilerUI = canvasGO;


            GameObject confirmTextObj = new GameObject("ConfirmText");
            confirmTextObj.transform.SetParent(container.transform, false);

            var tmp = confirmTextObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{GetString("ConfirmExeMessage")} {ExtendedPlayerControl.GetModdedNameByPlayer(pc)}?";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36f;
            tmp.color = Color.red;

            RectTransform rect = tmp.rectTransform;
            rect.sizeDelta = new Vector2(400, 100);
            rect.anchoredPosition = new Vector2(0, 120f);

            GameObject yesButtonObj = new GameObject("YesButton");
            yesButtonObj.transform.SetParent(container.transform, false);

            var yesRect = yesButtonObj.AddComponent<RectTransform>();
            yesRect.sizeDelta = new Vector2(160, 60);
            yesRect.anchoredPosition = new Vector2(-100, -40);

            var yesImage = yesButtonObj.AddComponent<Image>();
            yesImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            var yesButton = yesButtonObj.AddComponent<Button>();
            System.Action value = () =>
            {
                // Controlli preliminari del target player e dello stato del meeting.
                var targetPlayer = Utils.GetPlayerById(playerId);

                // Recupera NetworkedPlayerInfo dal PlayerControl.
                NetworkedPlayerInfo playerToExileInfo = (targetPlayer != null && targetPlayer.Data != null) ? GameData.Instance.GetPlayerById(targetPlayer.PlayerId) : null;

                // --- INIZIO DEI CONTROLLI DI VALIDAZIONE AGGIUNTIVI ---
                if (targetPlayer == null || targetPlayer.Data == null || targetPlayer.Data.Disconnected || playerToExileInfo == null || playerToExileInfo.Disconnected)
                {
                    Logger.Info($"[ExilerUI] Tentativo di esiliare un giocatore non valido o disconnesso (ID: {playerId}). Operazione annullata.");
                    RestoreMeetingUI(__instance, closeMeetingButton); // Ripristina UI e distruggi conferma
                    return;
                }

                if (ChatCommands.ComandoExeUsed)
                {
                    string alreadyUsedMessage = GetString("ExileAlreadyUsed");
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(alreadyUsedMessage, PlayerControl.LocalPlayer.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(alreadyUsedMessage, PlayerControl.LocalPlayer.PlayerId, GetString("Exiler"));
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    RestoreMeetingUI(__instance, closeMeetingButton); // Ripristina UI e distruggi conferma
                    return;
                }

                if (MeetingHud.Instance == null)
                {
                    ChatCommands.ShowChat("Nessuna riunione in corso. Impossibile esiliare tramite UI.");
                    Logger.Info("[ExilerUI] Nessuna MeetingHud attiva durante l'esilio tramite UI.");
                    RestoreMeetingUI(__instance, closeMeetingButton); // Ripristina UI e distruggi conferma
                    return;
                }

                // --- FINE DEI CONTROLLI DI VALIDAZIONE AGGIUNTIVI ---
                var action = Options.ExilerAction.GetValue();
                if (action == 1)
                {
                    List<MeetingHud.VoterState> statesList = new List<MeetingHud.VoterState>();
                    MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), playerToExileInfo, false);
                    string killerName = ExtendedPlayerControl.GetModdedNameByPlayer(PlayerControl.LocalPlayer);
                    string targetName = ExtendedPlayerControl.GetModdedNameByPlayer(targetPlayer);
                    // Chiudi la schermata del meeting
                    MeetingHud.Instance.Close();
                    MeetingHud.Instance.RpcClose();
                    MatchSummary.PresidentExeFailed = false;
                    MatchSummary.PresidentName = killerName;
                    MatchSummary.PresidentTargetName = targetName;
                    ExilerManager.CleanupAfterMeeting(); // Chiama il cleanup dal tuo ExilerManager

                    // CORREZIONE RPCSETNAME: Prendi il nome attuale prima di aggiungerci "(Exiled)"
                    string actualPlayerName = ExtendedPlayerControl.GetModdedNameByPlayer(targetPlayer);
                    targetPlayer.RpcSetName($"{actualPlayerName} {GetString("exiled")}");
                    ExtendedPlayerControl.StoreExiledName(targetPlayer.PlayerId);

                    // Marca il comando come usato per questa partita
                    ChatCommands.ComandoExeUsed = true;
                }
                else if (action == 0)
                {
                    string killerName = ExtendedPlayerControl.GetModdedNameByPlayer(PlayerControl.LocalPlayer);
                    string targetName = ExtendedPlayerControl.GetModdedNameByPlayer(targetPlayer);
                    PlayerControl.LocalPlayer.StartCoroutine(KillAsPlayer(targetPlayer, pc));
                    string actualPlayerName = ExtendedPlayerControl.GetModdedNameByPlayer(targetPlayer);
                    targetPlayer.RpcSetName($"{actualPlayerName} {GetString("guessed")}");
                    ExtendedPlayerControl.StoreGuessName(targetPlayer.PlayerId);
                    MatchSummary.PresidentKillFailed = false;
                    MatchSummary.PresidentName = killerName;
                    MatchSummary.PresidentTargetName = targetName;
                    string msg = string.Format(GetString("Guessedsuccededsender"));
                    string msg1 = string.Format(GetString("Guessedsuccededtarget"));
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(msg1, targetPlayer.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(msg1, targetPlayer.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    ChatCommands.ComandoExeUsed = true;
                }

                RestoreMeetingUI(__instance, closeMeetingButton); // Ripristino finale dell'UI del meeting e distruzione dell'UI di conferma.
            };
            yesButton.onClick.AddListener((UnityEngine.Events.UnityAction)value);

            // Aggiungi testo al pulsante "Sì"
            GameObject yesTextObj = new GameObject("YesText");
            yesTextObj.transform.SetParent(yesButtonObj.transform, false);

            var yesTMP = yesTextObj.AddComponent<TextMeshProUGUI>();
            yesTMP.text = $"{GetString("ConfirmButtonText")}";
            yesTMP.alignment = TextAlignmentOptions.Center;
            yesTMP.fontSize = 30;
            yesTMP.color = Color.black;

            var yesTMPRect = yesTMP.GetComponent<RectTransform>();
            yesTMPRect.anchorMin = Vector2.zero;
            yesTMPRect.anchorMax = Vector2.one;
            yesTMPRect.offsetMin = Vector2.zero;
            yesTMPRect.offsetMax = Vector2.zero;

            // === Pulsante No ===
            GameObject noButtonObj = new GameObject("NoButton");
            noButtonObj.transform.SetParent(container.transform, false);

            var noRect = noButtonObj.AddComponent<RectTransform>();
            noRect.sizeDelta = new Vector2(160, 60);
            noRect.anchoredPosition = new Vector2(100, -40);

            var noImage = noButtonObj.AddComponent<Image>();
            noImage.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Colore rosso

            var noButton = noButtonObj.AddComponent<Button>();
            noButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                RestoreMeetingUI(__instance, closeMeetingButton); // Ripristina UI del meeting e distruggi conferma
            }));

            // Aggiungi testo al pulsante "No"
            GameObject noTextObj = new GameObject("NoText");
            noTextObj.transform.SetParent(noButtonObj.transform, false);

            var noTMP = noTextObj.AddComponent<TextMeshProUGUI>();
            noTMP.text = $"{GetString("CancelButtonText")}";
            noTMP.alignment = TextAlignmentOptions.Center;
            noTMP.fontSize = 30;
            noTMP.color = Color.black;

            var noTMPRect = noTMP.GetComponent<RectTransform>();
            noTMPRect.anchorMin = Vector2.zero;
            noTMPRect.anchorMax = Vector2.one;
            noTMPRect.offsetMin = Vector2.zero;
            noTMPRect.offsetMax = Vector2.zero;
        }

        static void ShowHostConfirmationUI4(byte playerId, MeetingHud __instance)
        {
            var pc = Utils.GetPlayerById(playerId);
            if (pc == null || !pc.IsAlive() || ExilerUI != null) return;

            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));
            __instance.SkipVoteButton.gameObject.SetActive(false);
            var closeMeetingButton = __instance.transform.Find("CloseMeetingButton");
            if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(false);


            // Creo Canvas per contenere la UI
            GameObject canvasGO = new GameObject("ExilerUICanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject container = new GameObject("ExilerUIContainer");
            container.transform.SetParent(canvasGO.transform, false);
            ExilerUI = canvasGO; // Salva il riferimento al canvas per distruggerlo in seguito

            // Creo oggetto testo conferma con TextMeshProUGUI
            GameObject confirmTextObj = new GameObject("ConfirmText");
            confirmTextObj.transform.SetParent(container.transform, false);

            var tmp = confirmTextObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{GetString("ConfirmExeMessage")} {ExtendedPlayerControl.GetModdedNameByPlayer(pc)}?"; // Testo di conferma personalizzato
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36f;
            tmp.color = Color.red;

            RectTransform rect = tmp.rectTransform;
            rect.sizeDelta = new Vector2(400, 100);
            rect.anchoredPosition = new Vector2(0, 120f);

            // Template bottoni (assumendo siano UI button sotto Canvas)
            // === Pulsante Sì ===
            GameObject yesButtonObj = new GameObject("YesButton");
            yesButtonObj.transform.SetParent(container.transform, false);

            // Aggiungi componenti UI
            var yesRect = yesButtonObj.AddComponent<RectTransform>();
            yesRect.sizeDelta = new Vector2(160, 60);
            yesRect.anchoredPosition = new Vector2(-100, -40);

            var yesImage = yesButtonObj.AddComponent<Image>();
            yesImage.color = new Color(0.2f, 0.8f, 0.2f, 1f); // verde

            var yesButton = yesButtonObj.AddComponent<Button>();
            System.Action value = () =>
            {

                ChatCommands.ComandoExeUsed = true;
                string command = $"/bm {playerId}";
                var chatController = ChatControllerUpdatePatch.Instance;
                if (chatController != null)
                {
                    chatController.freeChatField.textArea.SetText(command);
                    chatController.SendChat();
                }

                RestoreMeetingUI(__instance, closeMeetingButton); // Ripristina UI del meeting e distruggi conferma
            };
            yesButton.onClick.AddListener((UnityEngine.Events.UnityAction)value);

            GameObject yesTextObj = new GameObject("YesText");
            yesTextObj.transform.SetParent(yesButtonObj.transform, false);

            var yesTMP = yesTextObj.AddComponent<TextMeshProUGUI>();
            yesTMP.text = $"{GetString("ConfirmButtonText")}";
            yesTMP.alignment = TextAlignmentOptions.Center;
            yesTMP.fontSize = 30;
            yesTMP.color = Color.black;

            var yesTMPRect = yesTMP.GetComponent<RectTransform>();
            yesTMPRect.anchorMin = Vector2.zero;
            yesTMPRect.anchorMax = Vector2.one;
            yesTMPRect.offsetMin = Vector2.zero;
            yesTMPRect.offsetMax = Vector2.zero;

            // === Pulsante No ===
            GameObject noButtonObj = new GameObject("NoButton");
            noButtonObj.transform.SetParent(container.transform, false);

            var noRect = noButtonObj.AddComponent<RectTransform>();
            noRect.sizeDelta = new Vector2(160, 60);
            noRect.anchoredPosition = new Vector2(100, -40);

            var noImage = noButtonObj.AddComponent<Image>();
            noImage.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Colore rosso

            var noButton = noButtonObj.AddComponent<Button>();
            noButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                RestoreMeetingUI(__instance, closeMeetingButton); // Ripristina UI del meeting e distruggi conferma
            }));

            // Aggiungi testo al pulsante "No"
            GameObject noTextObj = new GameObject("NoText");
            noTextObj.transform.SetParent(noButtonObj.transform, false);

            var noTMP = noTextObj.AddComponent<TextMeshProUGUI>();
            noTMP.text = $"{GetString("CancelButtonText")}";
            noTMP.alignment = TextAlignmentOptions.Center;
            noTMP.fontSize = 30;
            noTMP.color = Color.black;

            var noTMPRect = noTMP.GetComponent<RectTransform>();
            noTMPRect.anchorMin = Vector2.zero;
            noTMPRect.anchorMax = Vector2.one;
            noTMPRect.offsetMin = Vector2.zero;
            noTMPRect.offsetMax = Vector2.zero;
        }

        // Funzione helper per ripristinare l'UI del meeting e distruggere l'ExilerUI
        private static void RestoreMeetingUI(MeetingHud __instance, Transform closeMeetingButton)
        {
            if (__instance != null)
            {
                __instance.playerStates.ToList().ForEach(x =>
                {
                    x.gameObject.SetActive(true);
                    if (x.Buttons != null) x.Buttons.transform.gameObject.SetActive(false);
                });
                __instance.SkipVoteButton.gameObject.SetActive(true);
                if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(true);
            }

            if (ExilerUI != null)
            {
                UnityEngine.Object.Destroy(ExilerUI);
                ExilerUI = null;
            }
        }


        public static void ResetForNewGame()
        {
            Logger.Info("Reset GuessManager per nuova partita.");
            if (ExilerUI != null)
            {
                UnityEngine.Object.Destroy(ExilerUI);
                ExilerUI = null;
            }
        }
        public static void CleanupAfterMeeting()
        {
            Logger.Info("Reset GuessManager per prossimo meeting.");
            if (ExilerUI != null)
            {
                UnityEngine.Object.Destroy(ExilerUI);
                ExilerUI = null;
            }
        }
    }
}