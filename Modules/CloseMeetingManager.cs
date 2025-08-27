using BepInEx.Unity.IL2CPP.Utils;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod
{
    public static class CloseMeetingManager
    {
        public static GameObject closeMeetingConfirmUI = null; // Variabile globale per l'UI di conferma chiusura meeting

        /// <summary>
        /// Coroutine che attende l'inizializzazione dei bottoni del meeting e poi crea il pulsante di chiusura.
        /// </summary>
        /// <param name="__instance">L'istanza corrente di MeetingHud.</param>
        public static IEnumerator WaitForCloseMeetingButton(MeetingHud __instance)
        {
            Logger.Info("Inizio coroutine WaitForCloseMeetingButton...");

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

            Logger.Info("Bottoni pronti. Procedo alla creazione del pulsante di chiusura meeting.");
            CreateCloseMeetingButton(__instance);
        }

        /// <summary>
        /// Metodo per creare il pulsante "Chiudi Meeting".
        /// </summary>
        /// <param name="__instance">L'istanza corrente di MeetingHud.</param>
        public static void CreateCloseMeetingButton(MeetingHud __instance)
        {
            // Crea il pulsante solo se il giocatore è l'Host e se non esiste già
            if (AmongUsClient.Instance.AmHost && __instance.transform.Find("CloseMeetingButton") == null)
            {
                // Usa SkipVoteButton come template per consistenza nello stile
                GameObject template = __instance.SkipVoteButton.gameObject;
                GameObject closeButtonGO = UnityEngine.Object.Instantiate(template, __instance.transform);
                closeButtonGO.name = "CloseMeetingButton";

                // Posiziona il pulsante al centro orizzontale, leggermente sopra il bordo inferiore.
                closeButtonGO.transform.localPosition = new Vector3(0f, -2.5f, 0f);

                // Assicurati che il pulsante sia attivo
                closeButtonGO.SetActive(true);

                // --- GESTIONE TESTO ---
                TextMeshPro textMesh = closeButtonGO.GetComponentInChildren<TextMeshPro>();
                if (textMesh != null)
                {
                    textMesh.gameObject.SetActive(false);
                    Logger.Info("TextMeshPro del CloseMeetingButton disabilitato per lasciare spazio all'immagine.");
                }
                else
                {
                    Logger.Info("⚠️ Componente TextMeshPro non trovato nel template del CloseMeetingButton.");
                }

                // --- GESTIONE IMMAGINE DEL PULSANTE ---
                SpriteRenderer spriteRenderer = closeButtonGO.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = closeButtonGO.AddComponent<SpriteRenderer>();
                    Logger.Info("⚠️ SpriteRenderer non trovato sul CloseMeetingButton, ne aggiungo uno.");
                }

                var customSprite = Utils.LoadSprite("BanMod.Resources.image.CloseMeetingIcon.png", 100f);
                if (customSprite != null)
                {
                    spriteRenderer.sprite = customSprite;
                    spriteRenderer.color = Color.white;
                    Logger.Info("✅ Sprite 'CloseMeetingIcon.png' assegnato correttamente al bottone 'Chiudi Meeting'.");
                }
                else
                {
                    Logger.Info("❌ Impossibile caricare l'icona 'CloseMeetingIcon.png'. Il bottone userà un colore di fallback.");
                    spriteRenderer.color = new Color(0.8f, 0.4f, 0.1f, 1f);
                }
                // --- FINE GESTIONE IMMAGINE ---

                PassiveButton button = closeButtonGO.GetComponent<PassiveButton>();
                if (button != null)
                {
                    button.OnClick.RemoveAllListeners();
                    System.Action value = () =>
                    {
                        // Impedisci di aprire più finestre di conferma o di attivare se altre UI modali sono attive
                        // Accesso a GuesserManager.guesserUI per coordinamento tra le UI
                        if (CloseMeetingManager.closeMeetingConfirmUI != null || GuessManager.guesserUI != null)
                            return;
                        ShowCloseMeetingConfirmation(__instance);
                    };
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)value);
                    Logger.Info("✅ Bottone 'Chiudi Meeting' creato e listener aggiunto.");
                }
                else
                {
                    Logger.Info("❌ Componente PassiveButton non trovato nel template del CloseMeetingButton.");
                }
            }
        }

        /// <summary>
        /// Metodo per mostrare la finestra di conferma per la chiusura del meeting.
        /// </summary>
        /// <param name="__instance">L'istanza corrente di MeetingHud.</param>
        public static void ShowCloseMeetingConfirmation(MeetingHud __instance)
        {
            Logger.Info("Mostra la finestra di conferma chiusura meeting.");

            // Nascondi tutti gli elementi UI interattivi del meeting
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));
            __instance.SkipVoteButton.gameObject.SetActive(false);

            // Nascondi temporaneamente il pulsante "Chiudi Meeting"
            var closeMeetingButton = __instance.transform.Find("CloseMeetingButton");
            if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(false);


            // Crea un Canvas per contenere l'UI di conferma
            GameObject canvasGO = new GameObject("CloseMeetingConfirmCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();

            closeMeetingConfirmUI = canvasGO;

            // Crea il container come figlio del Canvas
            GameObject container = new GameObject("CloseMeetingConfirmContainer");
            container.transform.SetParent(canvasGO.transform, false);

            // Crea l'oggetto testo di conferma
            GameObject confirmTextObj = new GameObject("ConfirmCloseText");
            confirmTextObj.transform.SetParent(container.transform, false);

            var tmp = confirmTextObj.AddComponent<TextMeshProUGUI>();
            tmp.text = GetString("ConfirmCloseMeetingMessage");
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36f;
            tmp.color = Color.yellow;

            RectTransform rect = tmp.rectTransform;
            rect.sizeDelta = new Vector2(600, 100);
            rect.anchoredPosition = new Vector2(0, 120f);

            // === Pulsante Sì ===
            GameObject yesButtonObj = new GameObject("YesCloseButton");
            yesButtonObj.transform.SetParent(container.transform, false);

            var yesRect = yesButtonObj.AddComponent<RectTransform>();
            yesRect.sizeDelta = new Vector2(200, 70);
            yesRect.anchoredPosition = new Vector2(-150, -40);

            var yesImage = yesButtonObj.AddComponent<Image>();
            yesImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);

            var yesButton = yesButtonObj.AddComponent<Button>();
            yesButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                Logger.Info("Conferma chiusura meeting: SI");
                // Ripristina gli elementi UI originali
                __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                __instance.SkipVoteButton.gameObject.SetActive(true);
                if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(true);

                UnityEngine.Object.Destroy(closeMeetingConfirmUI);
                closeMeetingConfirmUI = null;

                // Esegui l'azione: chiudi il meeting
                PlayerControl.LocalPlayer.StartCoroutine(Utils.DelayedCloseMeeting());
                Logger.Info("Coroutina 'DelayedCloseMeeting' avviata.");
            }));

            GameObject yesTextObj = new GameObject("YesCloseText");
            yesTextObj.transform.SetParent(yesButtonObj.transform, false);
            var yesTMP = yesTextObj.AddComponent<TextMeshProUGUI>();
            yesTMP.text = GetString("YesButtonText");
            yesTMP.alignment = TextAlignmentOptions.Center;
            yesTMP.fontSize = 30;
            yesTMP.color = Color.black;
            var yesTMPRect = yesTMP.GetComponent<RectTransform>();
            yesTMPRect.anchorMin = Vector2.zero;
            yesTMPRect.anchorMax = Vector2.one;
            yesTMPRect.offsetMin = Vector2.zero;
            yesTMPRect.offsetMax = Vector2.zero;

            // === Pulsante No ===
            GameObject noButtonObj = new GameObject("NoCloseButton");
            noButtonObj.transform.SetParent(container.transform, false);

            var noRect = noButtonObj.AddComponent<RectTransform>();
            noRect.sizeDelta = new Vector2(200, 70);
            noRect.anchoredPosition = new Vector2(150, -40);

            var noImage = noButtonObj.AddComponent<Image>();
            noImage.color = new Color(0.8f, 0.2f, 0.2f, 1f);

            var noButton = noButtonObj.AddComponent<Button>();
            noButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() =>
            {
                Logger.Info("Conferma chiusura meeting: NO");
                // Ripristina gli elementi UI originali
                __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                __instance.SkipVoteButton.gameObject.SetActive(true);
                if (closeMeetingButton != null) closeMeetingButton.gameObject.SetActive(true);

                UnityEngine.Object.Destroy(closeMeetingConfirmUI);
                closeMeetingConfirmUI = null;
                Logger.Info("Tornato al meeting normale.");
            }));

            GameObject noTextObj = new GameObject("NoCloseText");
            noTextObj.transform.SetParent(noButtonObj.transform, false);
            var noTMP = noTextObj.AddComponent<TextMeshProUGUI>();
            noTMP.text = GetString("NoButtonText");
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
        /// Resetta lo stato di CloseMeetingManager per una nuova partita, distruggendo eventuali UI residue.
        /// </summary>
        public static void ResetForNewGame()
        {
            Logger.Info("Reset CloseMeetingManager per nuova partita.");
            if (closeMeetingConfirmUI != null)
            {
                UnityEngine.Object.Destroy(closeMeetingConfirmUI);
                closeMeetingConfirmUI = null;
            }
            // Distruggi anche il pulsante ChiudiMeetingButton se esiste nel MeetingHud
            if (MeetingHud.Instance != null)
            {
                var existingButton = MeetingHud.Instance.transform.Find("CloseMeetingButton");
                if (existingButton != null)
                {
                    UnityEngine.Object.Destroy(existingButton.gameObject);
                }
            }
            // Non gestiamo guesserUI qui, è responsabilità di GuesserManager
        }

        /// <summary>
        /// Esegue la pulizia dopo ogni meeting per il CloseMeetingManager.
        /// </summary>
        public static void CleanupAfterMeeting()
        {
            Logger.Info("Reset CloseMeetingManager per prossimo meeting.");
            if (closeMeetingConfirmUI != null)
            {
                UnityEngine.Object.Destroy(closeMeetingConfirmUI);
                closeMeetingConfirmUI = null;
            }
            // Distruggi anche il pulsante ChiudiMeetingButton se esiste nel MeetingHud
            if (MeetingHud.Instance != null)
            {
                var existingButton = MeetingHud.Instance.transform.Find("CloseMeetingButton");
                if (existingButton != null)
                {
                    UnityEngine.Object.Destroy(existingButton.gameObject);
                }
            }
            // Non gestiamo guesserUI qui, è responsabilità di GuesserManager
        }
    }
}