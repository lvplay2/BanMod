using HarmonyLib;
using Rewired.Utils.Platforms.Windows;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BanMod.Translator;
using Object = UnityEngine.Object;

namespace BanMod
{
    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPriority(Priority.First)]
    public class MainMenuManagerStartPatch
    {

        public static SpriteRenderer Logo { get; private set; }

        private static void Postfix(MainMenuManager __instance)
        {
            if (__instance == null)
            {
                Debug.LogError("MainMenuManager non è ancora disponibile.");
                return;
            }
            var rightPanel = __instance.gameModeButtons.transform.parent;
            var logoObject = new GameObject("titleLogo_BanMod");
            var logoTransform = logoObject.transform;
            

            Logo = logoObject.AddComponent<SpriteRenderer>();
            logoTransform.parent = rightPanel;
            logoTransform.localPosition = new(-0.16f, 0f, 1f);
            logoTransform.localScale *= 1.2f;
        }
    }

    [HarmonyPatch(typeof(MainMenuManager))]
    public static class MainMenuManagerPatch
    {
        private static PassiveButton template;
        private static PassiveButton websiteButton;
        private static PassiveButton lobbyButton;
        private static PassiveButton contactsButton;
        public static PassiveButton UpdateButton { get; private set; }

        [HarmonyPatch(nameof(MainMenuManager.Start)), HarmonyPostfix, HarmonyPriority(Priority.Normal)]
        public static void Start_Postfix(MainMenuManager __instance)
        {
            if (template == null) template = __instance.quitButton;

            // FPS
            __instance.screenTint.gameObject.transform.localPosition += new Vector3(1000f, 0f);
            __instance.screenTint.enabled = false;
            __instance.rightPanelMask.SetActive(true);

            // Disabling unnecessary UI elements
            DisableUIElement(__instance.mainMenuUI, "BackgroundTexture");
            DisableUIElement(__instance.mainMenuUI, "WindowShine");
            DisableUIElement(__instance.mainMenuUI, "ScreenCover");

            // Left and Right Panels modifications
            ModifyPanel(__instance.mainMenuUI, "LeftPanel");
            ModifyPanel(__instance.mainMenuUI, "RightPanel");

            // Splash art
            CreateSplashArt();

            if (template == null) return;

            if (contactsButton == null)
            {
                contactsButton = CreateButton(
                    "ContactsButton",
                    new(-1.8f, -1.1f, 1f),
                    new Color32(70, 130, 180, 255),
                    new Color32(65, 105, 225, 255),
                    (UnityEngine.Events.UnityAction)ShowContactsPopup,
                    GetString("Contacts")
                );
            }
            contactsButton.gameObject.SetActive(true);

            // GitHub Button
            if (websiteButton == null)
            {
                websiteButton = CreateButton(
                    "WebsiteButton",
                    new(-1.8f, -1.5f, 1f),
                    new Color32(70, 130, 180, 255),
                    new Color32(65, 105, 225, 255), 
                    (UnityEngine.Events.UnityAction)(() => Application.OpenURL(BanMod.GitsiteUrl)),
                    GetString("GitHub"));
            }
            websiteButton.gameObject.SetActive(BanMod.ShowWebsiteButton);

            if (lobbyButton == null)
            {
                lobbyButton = CreateButton(
                    "lobbyButton",
                    new(-1.8f, -1.9f, 1f),
                    new Color32(70, 130, 180, 255),
                    new Color32(65, 105, 225, 255),
                    (UnityEngine.Events.UnityAction)(() => Application.OpenURL(BanMod.LobbysiteUrl)),
                    GetString("LobbyCode"));
            }
            lobbyButton.gameObject.SetActive(BanMod.ShowlobbyButton);

            if (UpdateButton == null)
            {
                UpdateButton = CreateButton(
                    "UpdateButton",
                    new(-1.8f, -2.3f, 1f),
                    new(251, 81, 44, byte.MaxValue),
                    new(211, 77, 48, byte.MaxValue),
                    (UnityEngine.Events.UnityAction)(() => ModUpdater.StartUpdate(ModUpdater.downloadUrl)),
                    GetString("UpdateButton"));
            }
            UpdateButton.gameObject.SetActive(false);

        }
        private static void ShowContactsPopup()
        {
            GameObject popup = new("BanMod_ContactsPopup");
            popup.transform.position = new Vector3(0f, 0f, -10f);

            // Canvas
            var canvas = popup.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            popup.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            popup.AddComponent<GraphicRaycaster>();

            // Background
            var bg = new GameObject("Background");
            bg.transform.SetParent(popup.transform, false);
            var bgRect = bg.AddComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(400, 250);
            bgRect.anchoredPosition = Vector2.zero;
            var image = bg.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.85f);

            // Contatti
            string contactText =
                "<b><color=#D44638>Email:</color></b>\nbanmod.giannibart@gmail.com\n\n" +
                "<b><color=#0088CC>Telegram:</color></b>\nhttps://t.me/Giannibart\n\n" +
                "<b><color=#CCCCCC>GitHub:</color></b>\nhttps://github.com/Giannibart/BanMod";

            var textGO = new GameObject("ContactText");
            textGO.transform.SetParent(bg.transform, false);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = contactText;
            text.fontSize = 18;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.TopLeft;

            var textRect = text.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(360, 160);
            textRect.anchoredPosition = new Vector2(0, 30);

            // Pulsante Chiudi
            GameObject closeGO = new GameObject("CloseButton");
            closeGO.transform.SetParent(bg.transform, false);
            var closeRect = closeGO.AddComponent<RectTransform>();
            closeRect.sizeDelta = new Vector2(120, 40);
            closeRect.anchoredPosition = new Vector2(0, -90);

            var closeImage = closeGO.AddComponent<Image>();
            closeImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var closeBtn = closeGO.AddComponent<Button>();
            Action value = () => GameObject.Destroy(popup);
            closeBtn.onClick.AddListener(value);

            var closeTextGO = new GameObject("Text");
            closeTextGO.transform.SetParent(closeGO.transform, false);
            var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
            closeText.text = "Close";
            closeText.fontSize = 18;
            closeText.alignment = TextAlignmentOptions.Center;
            closeText.color = Color.white;

            var closeTextRect = closeText.GetComponent<RectTransform>();
            closeTextRect.anchorMin = Vector2.zero;
            closeTextRect.anchorMax = Vector2.one;
            closeTextRect.offsetMin = Vector2.zero;
            closeTextRect.offsetMax = Vector2.zero;
        }


        private static void DisableUIElement(GameObject uiParent, string elementName)
        {
            var element = uiParent.FindChild<SpriteRenderer>(elementName)?.transform.gameObject;
            if (element != null)
            {
                element.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"Element {elementName} not found.");
            }
        }

        private static void ModifyPanel(GameObject uiParent, string panelName)
        {
            var panel = uiParent.FindChild<Transform>(panelName)?.gameObject;
            if (panel != null)
            {
                panel.GetComponent<SpriteRenderer>().enabled = false;
                var maskedBlackScreen = panel.FindChild<Transform>("MaskedBlackScreen")?.gameObject;
                if (maskedBlackScreen != null)
                {
                    maskedBlackScreen.GetComponent<SpriteRenderer>().enabled = false;
                    maskedBlackScreen.transform.localScale = new Vector3(7.35f, 4.5f, 4f);
                }
                else
                {
                    Debug.LogWarning($"MaskedBlackScreen in {panelName} not found.");
                }
            }
            else
            {
                Debug.LogWarning($"Panel {panelName} not found.");
            }
        }

        private static void CreateSplashArt()
        {
            GameObject splashArt = new("SplashArt");
            splashArt.transform.position = new Vector3(0, 0f, 600f); // Adjusted position
            var spriteRenderer = splashArt.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = Utils.LoadSprite("BanMod.Resources.image.image.png", 150f);
        }

        public static PassiveButton CreateButton(string name, Vector3 localPosition, Color32 normalColor, Color32 hoverColor, UnityEngine.Events.UnityAction action, string label, Vector2? scale = null)
        {
            if (template == null)
            {
                Debug.LogError("Template for buttons is not set!");
                return null;
            }

            var button = Object.Instantiate(template, MainMenuManagerStartPatch.Logo.transform);
            button.name = name;
            Object.Destroy(button.GetComponent<AspectPosition>());
            button.transform.localPosition = localPosition;

            button.OnClick = new();
            button.OnClick.AddListener(action);

            var buttonText = button.transform.Find("FontPlacer/Text_TMP").GetComponent<TMP_Text>();
            buttonText.DestroyTranslator();
            buttonText.fontSize = buttonText.fontSizeMax = buttonText.fontSizeMin = 3.5f;
            buttonText.enableWordWrapping = false;
            buttonText.text = label;
            var normalSprite = button.inactiveSprites.GetComponent<SpriteRenderer>();
            var hoverSprite = button.activeSprites.GetComponent<SpriteRenderer>();
            normalSprite.color = normalColor;
            hoverSprite.color = hoverColor;

            var container = buttonText.transform.parent;
            Object.Destroy(container.GetComponent<AspectPosition>());
            Object.Destroy(buttonText.GetComponent<AspectPosition>());
            container.SetLocalX(0f);
            buttonText.transform.SetLocalX(0f);
            buttonText.horizontalAlignment = HorizontalAlignmentOptions.Center;

            var buttonCollider = button.GetComponent<BoxCollider2D>();
            if (scale.HasValue)
            {
                normalSprite.size = hoverSprite.size = buttonCollider.size = scale.Value;
            }

            buttonCollider.offset = new(0f, 0f);

            return button;
        }

        public static T FindChild<T>(this MonoBehaviour obj, string name) where T : Object
        {
            var child = obj.GetComponentsInChildren<T>().FirstOrDefault(c => c.name == name);
            if (child == null)
            {
                Debug.LogWarning($"Child with name {name} not found on {obj.name}");
            }
            return child;
        }

        public static T FindChild<T>(this GameObject obj, string name) where T : Object
        {
            var child = obj.GetComponentsInChildren<T>().FirstOrDefault(c => c.name == name);
            if (child == null)
            {
                Debug.LogWarning($"Child with name {name} not found on {obj.name}");
            }
            return child;
        }
    }
}

