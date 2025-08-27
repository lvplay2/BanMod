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
using static BanMod.Translator; // Assicurati che Translator.GetString sia accessibile
using static BanMod.Utils;
using static UnityEngine.GraphicsBuffer;

namespace BanMod
{
    public static class GuessManager
    {
        public static GameObject guesserUI = null;
        public static GameObject closeMeetingConfirmUI = null; 
        public static IEnumerator WaitForButtonsAndCreate(MeetingHud __instance)
        {
            Logger.Info("Inizio coroutine WaitForButtonsAndCreate...");

            // Attende che l'istanza di MeetingHud non sia null
            while (__instance == null)
            {
                Logger.Info("__instance è null, aspetto...");
                yield return null;
            }

            // Attende che gli stati dei giocatori (playerStates) siano inizializzati e non vuoti
            while (__instance.playerStates == null || __instance.playerStates.Count == 0)
            {
                Logger.Info("playerStates è null o vuoto, aspetto...");
                yield return null;
            }

            // Attende che tutti i bottoni dei giocatori siano inizializzati
            while (__instance.playerStates.All(pva => pva == null || pva.Buttons == null))
            {
                Logger.Info("Bottoni non inizializzati, aspetto...");
                yield return null;
            }

            Logger.Info("Bottoni pronti. Procedo alla creazione dei guesser button e del pulsante di chiusura meeting.");
            // Crea il bottone per l'azione del Guesser (se il giocatore locale è il Guesser SpecialKiller)
            CreateGuesserButton(__instance);
        }

        public static void CreateGuesserButton(MeetingHud __instance)
        {
            // Verifica se il giocatore locale è il "Guesser" (SpecialKillerId)
            if (PlayerControl.LocalPlayer != null && PlayerControl.LocalPlayer.PlayerId == Guesser.SpecialKillerId)
            {
                // Itera su tutti gli stati dei giocatori nel meeting
                foreach (var pva in __instance.playerStates.ToArray())
                {
                    var pc = Utils.GetPlayerById(pva.TargetPlayerId);
                    // Salta i giocatori non validi o già morti
                    if (pc == null || !pc.IsAlive()) continue;

                    // Usa il "CancelButton" come template per creare un nuovo bottone
                    GameObject template = pva.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject targetBox = UnityEngine.Object.Instantiate(template, pva.transform);
                    targetBox.name = "ShootButton"; // Assegna un nome identificativo
                    targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1.31f); // Posiziona il bottone

                    // Carica l'icona personalizzata dal file embedded
                    var sprite = Utils.LoadSprite("BanMod.Resources.image.TargetIcon.png", 100f);
                    if (sprite == null)
                    {
                        Logger.Info("❌ Impossibile caricare l'icona TargetIcon.png. Controlla il path e che sia una risorsa embedded.");
                    }
                    else
                    {
                        SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                        if (renderer != null)
                        {
                            renderer.sprite = sprite; // Assegna la sprite
                            Logger.Info("✅ Sprite TargetIcon assegnato correttamente al bottone.");
                        }
                        else
                        {
                            Logger.Info("⚠️ SpriteRenderer non trovato nel bottone.");
                        }
                    }

                    // Ottiene il componente PassiveButton e configura il suo listener
                    PassiveButton button = targetBox.GetComponent<PassiveButton>();
                    button.OnClick.RemoveAllListeners(); // Rimuove listener esistenti
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                    {
                        // Impedisce l'apertura di più finestre di conferma o l'attivazione se il giocatore è morto
                        if (PlayerControl.LocalPlayer.Data.IsDead || guesserUI != null || closeMeetingConfirmUI != null)
                            return;
                        // Chiama il metodo per gestire il click del bottone "Guesser"
                        GuesserOnClick(pva.TargetPlayerId, __instance);
                    }));
                }
            }
        }

        static void GuesserOnClick(byte playerId, MeetingHud __instance)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                // === HOST ===: mostra UI di conferma, poi gestisce tutto localmente
                var pc = Utils.GetPlayerById(playerId);
                // Impedisce di attivare se il giocatore è morto o se altre UI modali sono attive
                if (pc == null || !pc.IsAlive() || guesserUI != null || closeMeetingConfirmUI != null) return;

                ShowHostConfirmationUI(playerId, __instance);
            }
            else
            {
                var pc = Utils.GetPlayerById(playerId);
                // Impedisce di attivare se il giocatore è morto o se altre UI modali sono attive
                if (pc == null || !pc.IsAlive() || guesserUI != null || closeMeetingConfirmUI != null) return;
                ShowHostConfirmationUI1(playerId, __instance);
            }
        }

        /// <summary>
        /// Mostra la UI di conferma all'Host prima di eseguire l'azione "spara" (o "indovina").
        /// </summary>
        /// <param name="playerId">L'ID del giocatore bersaglio.</param>
        /// <param name="__instance">L'istanza corrente di MeetingHud.</param>
        static void ShowHostConfirmationUI(byte playerId, MeetingHud __instance)
        {
            var pc = Utils.GetPlayerById(playerId);
            // Impedisci di attivare se il giocatore è morto o se altre UI modali sono attive (guesserUI o closeMeetingConfirmUI)
            if (pc == null || !pc.IsAlive() || guesserUI != null || closeMeetingConfirmUI != null) return;

            // Nascondi tutti gli elementi UI interattivi del meeting
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));
            __instance.SkipVoteButton.gameObject.SetActive(false);
            // Nascondi temporaneamente anche il pulsante "Chiudi Meeting"
            var closeMeetingButton = __instance.transform.Find("CloseMeetingButton");
            if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(false);


            // Creo Canvas per contenere la UI
            GameObject canvasGO = new GameObject("GuesserUICanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Creo il container come figlio del Canvas
            GameObject container = new GameObject("GuesserUIContainer");
            container.transform.SetParent(canvasGO.transform, false);
            guesserUI = canvasGO; // Salva il riferimento al canvas per distruggerlo in seguito

            // Creo oggetto testo conferma con TextMeshProUGUI
            GameObject confirmTextObj = new GameObject("ConfirmText");
            confirmTextObj.transform.SetParent(container.transform, false);

            var tmp = confirmTextObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{GetString("ConfirmKillMessage")} {ExtendedPlayerControl.GetModdedNameByPlayer(pc)}?"; // Testo di conferma personalizzato
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
            yesImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);  // verde

            var yesButton = yesButtonObj.AddComponent<Button>();
            System.Action value = () =>
            {
                var player = PlayerControl.LocalPlayer;
                var targetPlayer = Utils.GetPlayerById(playerId);

                ImpostorManager.DetectImpostors();
                var impostors = ImpostorManager.GetImpostorsList();
                bool isImpostor = impostors.Any(i => i.PlayerId == targetPlayer.PlayerId);

                string killerName = ExtendedPlayerControl.GetModdedNameByPlayer(player);
                string targetName = ExtendedPlayerControl.GetModdedNameByPlayer(targetPlayer);

                if (isImpostor)
                {
                    PlayerControl.LocalPlayer.StartCoroutine(KillAsPlayer(targetPlayer, player));
                    BanMod.playersKilledByCommand.Add(targetPlayer.PlayerId);
                    BanMod.PlayersKilledByKillCommand.Add(targetPlayer.PlayerId);
                    targetPlayer.RpcSetName($"{targetName} {GetString("guessed")}"); // Cambia nome del bersaglio (es. "Player1 (indovinato)")
                    ExtendedPlayerControl.StoreGuessName(targetPlayer.PlayerId); // Memorizza il nome per la fine del gioco
                    MatchSummary.SpecialKillerFailed = false;
                    MatchSummary.GuesserName = killerName;
                    MatchSummary.GuessedTargetName = targetName;
                    string msg = string.Format(GetString("Guessedsuccededsender"));
                    string msg1 = string.Format(GetString("Guessedsuccededtarget"));
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                        Utils.RequestProxyMessage(msg1, targetPlayer.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                        Utils.SendMessage(msg1, targetPlayer.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    //string msg10 = string.Format(GetString("DeadinMeeting"), targetName); // Messaggio di successo
                    //// Invia il messaggio di successo a tutti i giocatori (con gestione per Host morto)
                    //if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    //{
                    //    Utils.RequestProxyMessage(msg10, 255);
                    //}
                    //else
                    //{
                    //    Utils.SendMessage(msg10, 255, GetString("DeadinMeetingTitle"));
                    //}
                    //MessageBlocker.UpdateLastMessageTime();
                }
                else
                {
                    PlayerControl.LocalPlayer.StartCoroutine(KillAsPlayer(player, player));
                    foreach (var pva in __instance.playerStates)
                    {
                        var shootBtn = pva.Buttons.transform.Find("ShootButton");
                        if (shootBtn != null)
                            UnityEngine.Object.Destroy(shootBtn.gameObject);
                    }
                    BanMod.playersKilledByCommand.Add(player.PlayerId);
                    BanMod.PlayersKilledByKillCommand.Add(player.PlayerId);
                    player.RpcSetName($"{killerName} {GetString("suicide")}"); // Cambia nome del Guesser (es. "Player1 (suicidio)")
                    ExtendedPlayerControl.StoreGuessName(player.PlayerId); // Memorizza il nome per la fine del gioco
                    MatchSummary.SpecialKillerFailed = true;
                    MatchSummary.GuesserName = killerName;
                    MatchSummary.GuessedTargetName = targetName;
                    string msg = string.Format(GetString("Suicedefailed"));
                    if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    {
                        Utils.RequestProxyMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    else
                    {
                        Utils.SendMessage(msg, player.PlayerId);
                        MessageBlocker.UpdateLastMessageTime();
                    }
                    //string msg11 = string.Format(GetString("DeadinMeeting"), killerName); // Messaggio di fallimento
                    //// Invia il messaggio di fallimento a tutti i giocatori (con gestione per Host morto)
                    //if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                    //{
                    //    Utils.RequestProxyMessage(msg11, 255);
                    //}
                    //else
                    //{
                    //    Utils.SendMessage(msg11, 255, GetString("DeadinMeetingTitle"));
                    //}
                    //MessageBlocker.UpdateLastMessageTime();
                }

                // Chiudo la UI e ripristino gli elementi UI originali
                __instance.playerStates.ToList().ForEach(x =>
                {
                    x.gameObject.SetActive(true);
                    // Questo sembra nascondere i pulsanti d'azione del giocatore, probabilmente per evitare ulteriori interazioni
                    x.Buttons.transform.gameObject.SetActive(false);
                });
                __instance.SkipVoteButton.gameObject.SetActive(true);
                if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(true); // Mostra di nuovo il pulsante "Chiudi Meeting"

                UnityEngine.Object.Destroy(guesserUI); // Distruggi l'UI di conferma
                guesserUI = null;
            };
            yesButton.onClick.AddListener((UnityEngine.Events.UnityAction)value);

            // Aggiungi testo al pulsante "Sì"
            GameObject yesTextObj = new GameObject("YesText");
            yesTextObj.transform.SetParent(yesButtonObj.transform, false);

            var yesTMP = yesTextObj.AddComponent<TextMeshProUGUI>();
            yesTMP.text = $"{GetString("ConfirmButtonText")}"; // Testo: "Conferma"
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
                // Ripristina gli elementi UI originali e distrugge l'UI di conferma
                __instance.playerStates.ToList().ForEach(x =>
                {
                    x.gameObject.SetActive(true);
                    x.Buttons.transform.gameObject.SetActive(false);
                });
                __instance.SkipVoteButton.gameObject.SetActive(true);
                if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(true); // Mostra di nuovo il pulsante "Chiudi Meeting"

                UnityEngine.Object.Destroy(guesserUI);
                guesserUI = null;
            }));

            // Aggiungi testo al pulsante "No"
            GameObject noTextObj = new GameObject("NoText");
            noTextObj.transform.SetParent(noButtonObj.transform, false);

            var noTMP = noTextObj.AddComponent<TextMeshProUGUI>();
            noTMP.text = $"{GetString("CancelButtonText")}"; // Testo: "Annulla"
            noTMP.alignment = TextAlignmentOptions.Center;
            noTMP.fontSize = 30;
            noTMP.color = Color.black;

            var noTMPRect = noTMP.GetComponent<RectTransform>();
            noTMPRect.anchorMin = Vector2.zero;
            noTMPRect.anchorMax = Vector2.one;
            noTMPRect.offsetMin = Vector2.zero;
            noTMPRect.offsetMax = Vector2.zero;
        }
        static void ShowHostConfirmationUI1(byte playerId, MeetingHud __instance)
        {
            var pc = Utils.GetPlayerById(playerId);
            // Impedisci di attivare se il giocatore è morto o se altre UI modali sono attive (guesserUI o closeMeetingConfirmUI)
            if (pc == null || !pc.IsAlive() || guesserUI != null || closeMeetingConfirmUI != null) return;

            // Nascondi tutti gli elementi UI interattivi del meeting
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));
            __instance.SkipVoteButton.gameObject.SetActive(false);
            // Nascondi temporaneamente anche il pulsante "Chiudi Meeting"
            var closeMeetingButton = __instance.transform.Find("CloseMeetingButton");
            if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(false);


            // Creo Canvas per contenere la UI
            GameObject canvasGO = new GameObject("GuesserUICanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            // Creo il container come figlio del Canvas
            GameObject container = new GameObject("GuesserUIContainer");
            container.transform.SetParent(canvasGO.transform, false);
            guesserUI = canvasGO; // Salva il riferimento al canvas per distruggerlo in seguito

            // Creo oggetto testo conferma con TextMeshProUGUI
            GameObject confirmTextObj = new GameObject("ConfirmText");
            confirmTextObj.transform.SetParent(container.transform, false);

            var tmp = confirmTextObj.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{GetString("ConfirmKillMessage")} {ExtendedPlayerControl.GetModdedNameByPlayer(pc)}?"; // Testo di conferma personalizzato
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
            yesImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);  // verde

            var yesButton = yesButtonObj.AddComponent<Button>();
            System.Action value = () =>
            {
                string command = $"/bm {playerId}";
                var chatController = ChatControllerUpdatePatch.Instance;
                if (chatController != null)
                {
                    chatController.freeChatField.textArea.SetText(command);
                    chatController.SendChat();
                }


                // Chiudo la UI e ripristino gli elementi UI originali
                __instance.playerStates.ToList().ForEach(x =>
                {
                    x.gameObject.SetActive(true);
                    // Questo sembra nascondere i pulsanti d'azione del giocatore, probabilmente per evitare ulteriori interazioni
                    x.Buttons.transform.gameObject.SetActive(false);
                });
                __instance.SkipVoteButton.gameObject.SetActive(true);
                if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(true); // Mostra di nuovo il pulsante "Chiudi Meeting"

                UnityEngine.Object.Destroy(guesserUI); // Distruggi l'UI di conferma
                guesserUI = null;
            };
            yesButton.onClick.AddListener((UnityEngine.Events.UnityAction)value);

            // Aggiungi testo al pulsante "Sì"
            GameObject yesTextObj = new GameObject("YesText");
            yesTextObj.transform.SetParent(yesButtonObj.transform, false);

            var yesTMP = yesTextObj.AddComponent<TextMeshProUGUI>();
            yesTMP.text = $"{GetString("ConfirmButtonText")}"; // Testo: "Conferma"
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
                // Ripristina gli elementi UI originali e distrugge l'UI di conferma
                __instance.playerStates.ToList().ForEach(x =>
                {
                    x.gameObject.SetActive(true);
                    x.Buttons.transform.gameObject.SetActive(false);
                });
                __instance.SkipVoteButton.gameObject.SetActive(true);
                if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(true); // Mostra di nuovo il pulsante "Chiudi Meeting"

                UnityEngine.Object.Destroy(guesserUI);
                guesserUI = null;
            }));

            // Aggiungi testo al pulsante "No"
            GameObject noTextObj = new GameObject("NoText");
            noTextObj.transform.SetParent(noButtonObj.transform, false);

            var noTMP = noTextObj.AddComponent<TextMeshProUGUI>();
            noTMP.text = $"{GetString("CancelButtonText")}"; // Testo: "Annulla"
            noTMP.alignment = TextAlignmentOptions.Center;
            noTMP.fontSize = 30;
            noTMP.color = Color.black;

            var noTMPRect = noTMP.GetComponent<RectTransform>();
            noTMPRect.anchorMin = Vector2.zero;
            noTMPRect.anchorMax = Vector2.one;
            noTMPRect.offsetMin = Vector2.zero;
            noTMPRect.offsetMax = Vector2.zero;
        }
        /// <summary>
        /// Resetta lo stato di GuessManager per una nuova partita, distruggendo eventuali UI residue.
        /// </summary>
        public static void ResetForNewGame()
        {
            Logger.Info("Reset GuessManager per nuova partita.");
            guesserUI = null;
            if (closeMeetingConfirmUI != null)
            {
                UnityEngine.Object.Destroy(closeMeetingConfirmUI);
                closeMeetingConfirmUI = null;
            }
            // Distrugge anche il pulsante ChiudiMeetingButton se esiste
            if (MeetingHud.Instance != null)
            {
                var existingButton = MeetingHud.Instance.transform.Find("CloseMeetingButton");
                if (existingButton != null)
                {
                    UnityEngine.Object.Destroy(existingButton.gameObject);
                }
            }
        }

        /// <summary>
        /// Esegue la pulizia dopo ogni meeting, distruggendo le UI di conferma.
        /// </summary>
        public static void CleanupAfterMeeting()
        {
            Logger.Info("Reset GuessManager per prossimo meeting.");
            guesserUI = null;
            if (closeMeetingConfirmUI != null)
            {
                UnityEngine.Object.Destroy(closeMeetingConfirmUI);
                closeMeetingConfirmUI = null;
            }
            // Distrugge anche il pulsante ChiudiMeetingButton se esiste
            if (MeetingHud.Instance != null)
            {
                var existingButton = MeetingHud.Instance.transform.Find("CloseMeetingButton");
                if (existingButton != null)
                {
                    UnityEngine.Object.Destroy(existingButton.gameObject);
                }
            }
        }
    }
}