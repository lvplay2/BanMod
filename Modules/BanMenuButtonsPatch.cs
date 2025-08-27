using HarmonyLib;
using Rewired.Utils.Classes.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static BanMod.ChatCommands;
using static BanMod.Translator;
using static BanMod.Utils;
using Object = UnityEngine.Object;

namespace BanMod
{
    public class SubMenuButtonData
    {
        public string Title;
        public string Message;
    }

    [HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Show))]
    public static class BanMenu_Show_Patch
    {
        static void Postfix(BanMenu __instance)
        {
            BanMenuButtonsPatch patch = __instance.GetComponent<BanMenuButtonsPatch>();
            if (patch == null)
            {
                patch = __instance.gameObject.AddComponent<BanMenuButtonsPatch>();
                // These assignments are crucial for the patch to interact with the static menu instances
                patch.MenuController1 = BanMod.menuUI1;
                patch.MenuController2 = BanMod.menuUI2;
                patch.Init(__instance);
            }
        }
    }

    public class BanMenuButtonsPatch : MonoBehaviour
    {
        public SpriteRenderer CustomButton1Prefab;
        public SpriteRenderer CustomButton2Prefab;
        public SpriteRenderer CustomButton3Prefab;
        public SpriteRenderer CustomButton4Prefab;

        public SpriteRenderer Background;

        private SpriteRenderer customButton1Instance;
        private SpriteRenderer customButton2Instance;
        private SpriteRenderer customButton3Instance;
        private SpriteRenderer customButton4Instance;

        private BanMenu banMenu;

        public float desiredOffsetX_Add = 1.23f;
        public float desiredOffsetY_Add = 1.5f;

        public float desiredOffsetX_Delete = 1.23f;
        public float desiredOffsetY_Delete = 0.75f;

        public float desiredOffsetX_Button3 = 0.40f;
        public float desiredOffsetY_Button3 = -2.05f;

        public float desiredOffsetX_Button4 = 1.23f;
        public float desiredOffsetY_Button4 = -2.05f;


        private SpriteRenderer reportButtonForPosition;
        // Keep these public properties to be assigned by BanMenu_Show_Patch
        public MenuUI1 MenuController1;
        public MenuUI2 MenuController2;

        public void Init(BanMenu targetMenu)
        {
            banMenu = targetMenu;
            Background = targetMenu.Background;

            CustomButton1Prefab = targetMenu.BanButton;
            CustomButton2Prefab = targetMenu.KickButton;
            CustomButton3Prefab = targetMenu.BanButton;
            CustomButton4Prefab = targetMenu.KickButton;

            reportButtonForPosition = targetMenu.ReportButton;
            if (reportButtonForPosition == null)
            {
                Debug.LogError("Could not find BanMenu's ReportButton (SpriteRenderer) for position. Custom buttons might not follow correctly.");
            }
        }

        void Update()
        {
            if (banMenu == null) return;

            bool menuIsActive = banMenu.gameObject.activeInHierarchy;
            bool buttonsShouldBeVisible = Options.buttonvisibile.GetBool();

            if (menuIsActive && buttonsShouldBeVisible)
            {
                if (customButton1Instance == null || customButton2Instance == null || customButton3Instance == null || customButton4Instance == null)
                {
                    Debug.Log("Creating custom buttons (menu is active and buttons visible option is true)");
                    AddCustomButtons();
                }

                if (customButton1Instance != null) customButton1Instance.gameObject.SetActive(true);
                if (customButton2Instance != null) customButton2Instance.gameObject.SetActive(true);
                if (customButton3Instance != null) customButton3Instance.gameObject.SetActive(true);
                if (customButton4Instance != null) customButton4Instance.gameObject.SetActive(true);

                RecalculateButtonPositions();
            }
            else // If menu is not active OR buttons should not be visible
            {
                if (customButton1Instance != null) { customButton1Instance.gameObject.SetActive(false); }
                if (customButton2Instance != null) { customButton2Instance.gameObject.SetActive(false); }
                if (customButton3Instance != null) { customButton3Instance.gameObject.SetActive(false); }
                if (customButton4Instance != null) { customButton4Instance.gameObject.SetActive(false); }

                // Destroy buttons if the menu is not active to save resources
                // This will only happen if the BanMenu itself is closed (menuIsActive is false)
                // but we keep the instances alive if only buttonsShouldBeVisible is false (e.g., BanMenu open, but buttons hidden by config)
                if (!menuIsActive && (customButton1Instance != null || customButton2Instance != null || customButton3Instance != null || customButton4Instance != null))
                {
                    Debug.Log("Destroying custom buttons (menu is not active)");
                    if (customButton1Instance != null) { Destroy(customButton1Instance.gameObject); customButton1Instance = null; }
                    if (customButton2Instance != null) { Destroy(customButton2Instance.gameObject); customButton2Instance = null; }
                    if (customButton3Instance != null) { Destroy(customButton3Instance.gameObject); customButton3Instance = null; }
                    if (customButton4Instance != null) { Destroy(customButton4Instance.gameObject); customButton4Instance = null; }
                }
            }

            // *** THE KEY INPUT LOGIC HAS BEEN MOVED TO MenuKeyInputHandler ***
        }

        private void AddCustomButtons()
        {
            if (customButton1Instance != null && customButton2Instance != null && customButton3Instance != null && customButton4Instance != null) return;

            // --- Creazione AddButton (ID 1) ---
            GameObject addGO = null;
            SpriteRenderer sr1 = null;

            if (CustomButton1Prefab != null)
            {
                addGO = Object.Instantiate(CustomButton1Prefab.gameObject, banMenu.transform);
                addGO.name = "AddButton_Cloned";
                sr1 = addGO.GetComponent<SpriteRenderer>();

                if (addGO.TryGetComponent<ButtonRolloverHandler>(out var bh)) Object.DestroyImmediate(bh);
                if (addGO.TryGetComponent<PassiveButton>(out var pb)) Object.DestroyImmediate(pb);
                if (addGO.TryGetComponent<BanButton>(out var bb)) Object.DestroyImmediate(bb);
                if (addGO.TryGetComponent<BoxCollider2D>(out var existingCollider)) Object.DestroyImmediate(existingCollider);

                addGO.transform.localScale = new Vector3(0.88f, 2.5f, 1.5f);
            }
            else
            {
                Debug.LogError("CustomButton1Prefab (BanButton) is NULL. Cannot clone. AddButton will not be created.");
                return;
            }

            var addSprite = Utils.LoadSprite("BanMod.Resources.image.AddIcon.png", 150f);
            if (sr1 != null)
            {
                if (addSprite != null)
                {
                    sr1.sprite = addSprite;
                    sr1.color = Color.white;
                }
                else
                {
                    sr1.color = Color.green;
                    Debug.LogError("Failed to load AddIcon.png. Using green color for AddButton.");
                }
            }

            var col1 = addGO.AddComponent<BoxCollider2D>();
            col1.size = new Vector2(0.5f, 0.5f);
            col1.isTrigger = true;

            var handler1 = addGO.AddComponent<CustomButtonHandler>();
            handler1.ButtonId = 1;
            handler1.Mod = this;
            handler1.normalColor = sr1.color;
            handler1.hoverColor = sr1.color;
            handler1.AffectsSelection = true;

            customButton1Instance = sr1;
            if (CustomButton1Prefab != null)
            {
                customButton1Instance.sortingLayerName = CustomButton1Prefab.sortingLayerName;
                customButton1Instance.sortingOrder = CustomButton1Prefab.sortingOrder + 100;
            }
            else
            {
                customButton1Instance.sortingOrder = 500;
            }

            // --- Creazione DeleteButton (ID 2) ---
            GameObject delGO = null;
            SpriteRenderer sr2 = null;

            if (CustomButton2Prefab != null)
            {
                delGO = Object.Instantiate(CustomButton2Prefab.gameObject, banMenu.transform);
                delGO.name = "DeleteButton_Cloned";
                sr2 = delGO.GetComponent<SpriteRenderer>();

                if (delGO.TryGetComponent<ButtonRolloverHandler>(out var bh2)) Object.DestroyImmediate(bh2);
                if (delGO.TryGetComponent<PassiveButton>(out var pb2)) Object.DestroyImmediate(pb2);
                if (delGO.TryGetComponent<BanButton>(out var bb2)) Object.DestroyImmediate(bb2);
                if (delGO.TryGetComponent<BoxCollider2D>(out var existingCollider2)) Object.DestroyImmediate(existingCollider2);

                delGO.transform.localScale = new Vector3(0.88f, 2.5f, 1.5f);
            }
            else
            {
                Debug.LogError("CustomButton2Prefab (KickButton) is NULL. Cannot clone. DeleteButton will not be created.");
                return;
            }

            var delSprite = Utils.LoadSprite("BanMod.Resources.image.DeleteIcon.png", 150f);
            if (sr2 != null)
            {
                if (delSprite != null)
                {
                    sr2.sprite = delSprite;
                    sr2.color = Color.white;
                }
                else
                {
                    sr2.color = new Color(0.8f, 0.4f, 0.1f, 1f);
                    Debug.LogError("Failed to load DeleteIcon.png. Using orange color for DeleteButton.");
                }
            }

            var col2 = delGO.AddComponent<BoxCollider2D>();
            col2.size = new Vector2(0.5f, 0.5f);
            col2.isTrigger = true;

            var handler2 = delGO.AddComponent<CustomButtonHandler>();
            handler2.ButtonId = 2;
            handler2.Mod = this;
            handler2.normalColor = sr2.color;
            handler2.hoverColor = sr2.color;
            handler2.AffectsSelection = true;

            customButton2Instance = sr2;
            if (CustomButton2Prefab != null)
            {
                customButton2Instance.sortingLayerName = CustomButton2Prefab.sortingLayerName;
                customButton2Instance.sortingOrder = CustomButton2Prefab.sortingOrder + 100;
            }
            else
            {
                customButton2Instance.sortingOrder = 500;
            }

            // --- Creazione CustomButton3 (ID 3) ---
            GameObject btn3GO = null;
            SpriteRenderer sr3 = null;

            if (CustomButton3Prefab != null)
            {
                btn3GO = Object.Instantiate(CustomButton3Prefab.gameObject, banMenu.transform);
                btn3GO.name = "CustomButton3_Settings";
                sr3 = btn3GO.GetComponent<SpriteRenderer>();

                if (btn3GO.TryGetComponent<ButtonRolloverHandler>(out var bh3)) Object.DestroyImmediate(bh3);
                if (btn3GO.TryGetComponent<PassiveButton>(out var pb3)) Object.DestroyImmediate(pb3);
                if (btn3GO.TryGetComponent<BanButton>(out var bb3)) Object.DestroyImmediate(bb3);
                if (btn3GO.TryGetComponent<BoxCollider2D>(out var existingCollider3)) Object.DestroyImmediate(existingCollider3);

                btn3GO.transform.localScale = new Vector3(0.88f, 2.5f, 1.5f);
            }
            else
            {
                Debug.LogError("CustomButton3Prefab is NULL. Cannot clone. CustomButton3 will not be created.");
                return;
            }

            var customSprite3 = Utils.LoadSprite("BanMod.Resources.image.SettingsIcon.png", 150f);
            if (sr3 != null)
            {
                if (customSprite3 != null)
                {
                    sr3.sprite = customSprite3;
                    sr3.color = Color.white;
                }
                else
                {
                    sr3.color = Color.blue;
                    Debug.LogError("Failed to load SettingsIcon.png. Using blue color for CustomButton3.");
                }
            }

            var col3 = btn3GO.AddComponent<BoxCollider2D>();
            col3.size = new Vector2(0.5f, 0.5f);
            col3.isTrigger = true;

            var handler3 = btn3GO.AddComponent<CustomButtonHandler>();
            handler3.ButtonId = 3;
            handler3.Mod = this;
            handler3.normalColor = sr3.color;
            handler3.hoverColor = sr3.color;
            handler3.AffectsSelection = false;

            customButton3Instance = sr3;
            if (CustomButton3Prefab != null)
            {
                customButton3Instance.sortingLayerName = CustomButton3Prefab.sortingLayerName;
                customButton3Instance.sortingOrder = CustomButton3Prefab.sortingOrder + 100;
            }
            else
            {
                customButton3Instance.sortingOrder = 500;
            }

            // --- Creazione CustomButton4 (ID 4) ---
            GameObject btn4GO = null;
            SpriteRenderer sr4 = null;

            if (CustomButton4Prefab != null)
            {
                btn4GO = Object.Instantiate(CustomButton4Prefab.gameObject, banMenu.transform);
                btn4GO.name = "CustomButton4_Info";
                sr4 = btn4GO.GetComponent<SpriteRenderer>();

                if (btn4GO.TryGetComponent<ButtonRolloverHandler>(out var bh4)) Object.DestroyImmediate(bh4);
                if (btn4GO.TryGetComponent<PassiveButton>(out var pb4)) Object.DestroyImmediate(pb4);
                if (btn4GO.TryGetComponent<BanButton>(out var bb4)) Object.DestroyImmediate(bb4);
                if (btn4GO.TryGetComponent<BoxCollider2D>(out var existingCollider4)) Object.DestroyImmediate(existingCollider4);

                btn4GO.transform.localScale = new Vector3(0.88f, 2.5f, 1.5f);
            }
            else
            {
                Debug.LogError("CustomButton4Prefab is NULL. Cannot clone. CustomButton4 will not be created.");
                return;
            }

            var customSprite4 = Utils.LoadSprite("BanMod.Resources.image.InfoIcon.png", 150f);
            if (sr4 != null)
            {
                if (customSprite4 != null)
                {
                    sr4.sprite = customSprite4;
                    sr4.color = Color.white;
                }
                else
                {
                    sr4.color = Color.magenta;
                    Debug.LogError("Failed to load InfoIcon.png. Using magenta color for CustomButton4.");
                }
            }

            var col4 = btn4GO.AddComponent<BoxCollider2D>();
            col4.size = new Vector2(0.5f, 0.5f);
            col4.isTrigger = true;

            var handler4 = btn4GO.AddComponent<CustomButtonHandler>();
            handler4.ButtonId = 4;
            handler4.Mod = this;
            handler4.normalColor = sr4.color;
            handler4.hoverColor = sr4.color;
            handler4.AffectsSelection = false;

            customButton4Instance = sr4;
            if (CustomButton4Prefab != null)
            {
                customButton4Instance.sortingLayerName = CustomButton4Prefab.sortingLayerName;
                customButton4Instance.sortingOrder = CustomButton4Prefab.sortingOrder + 100;
            }
            else
            {
                customButton4Instance.sortingOrder = 500;
            }
        }

        public void RecalculateButtonPositions()
        {
            if (reportButtonForPosition == null || customButton1Instance == null || customButton2Instance == null || customButton3Instance == null || customButton4Instance == null)
            {
                return;
            }

            Vector3 reportButtonWorldPos = reportButtonForPosition.transform.position;
            Vector3 banMenuWorldPos = banMenu.transform.position;
            Vector3 referenceOffsetFromReportButtonOrigin = reportButtonWorldPos - banMenuWorldPos;

            if (customButton1Instance.gameObject.activeSelf)
            {
                customButton1Instance.transform.localPosition = new Vector3(
                    referenceOffsetFromReportButtonOrigin.x + desiredOffsetX_Add,
                    referenceOffsetFromReportButtonOrigin.y + desiredOffsetY_Add,
                    -100f
                );
            }

            if (customButton2Instance.gameObject.activeSelf)
            {
                customButton2Instance.transform.localPosition = new Vector3(
                    referenceOffsetFromReportButtonOrigin.x + desiredOffsetX_Delete,
                    referenceOffsetFromReportButtonOrigin.y + desiredOffsetY_Delete,
                    -100f
                );
            }

            if (customButton3Instance.gameObject.activeSelf)
            {
                customButton3Instance.transform.localPosition = new Vector3(
                    referenceOffsetFromReportButtonOrigin.x + desiredOffsetX_Button3,
                    referenceOffsetFromReportButtonOrigin.y + desiredOffsetY_Button3,
                    -100f
                );
            }

            if (customButton4Instance.gameObject.activeSelf)
            {
                customButton4Instance.transform.localPosition = new Vector3(
                    referenceOffsetFromReportButtonOrigin.x + desiredOffsetX_Button4,
                    referenceOffsetFromReportButtonOrigin.y + desiredOffsetY_Button4,
                    -100f
                );
            }
        }

        public void OnCustomButtonClicked(int id)
        {
            string filePath = "./BAN_DATA/Friends.txt";
            var (playerId, playerName, friendCode) = GetSelectedPlayerInfo();

            bool isPlayerRelatedButton = (id == 1 || id == 2);

            if (isPlayerRelatedButton && string.IsNullOrEmpty(playerId))
            {
                Debug.LogWarning($"Attempted to click custom button {id} without a player selected.");
                return;
            }

            switch (id)
            {
                case 1: // Add
                    if (!IsFriends(friendCode))
                    {
                        System.IO.File.AppendAllText(filePath, $"\n{friendCode}, {playerName}");
                        ShowChat(playerName + GetString("AddedtoFriendList"));
                    }
                    else
                    {
                        ShowChat(playerName + GetString("PlayerinFriendList"));
                    }
                    break;

                case 2: // Delete
                    if (IsFriends(friendCode))
                    {
                        var updatedLines = System.IO.File
                            .ReadAllLines(filePath)
                            .Where(line =>
                            {
                                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                                return !(parts.Length > 0 && parts[0].Equals(friendCode, StringComparison.OrdinalIgnoreCase));
                            });

                        System.IO.File.WriteAllLines(filePath, updatedLines);
                        ShowChat(playerName + GetString("PlayerRemovedFromFriendsList"));
                    }
                    else
                    {
                        ShowChat(playerName + GetString("PlayerNotInFriendList"));
                    }
                    break;
                case 3: // Toggle MenuUI1 (and close MenuUI2 if open)
                    if (MenuController1 != null)
                    {
                        if (MenuController1.IsOpen())
                        {
                            MenuController1.CloseMenu();
                        }
                        else
                        {
                            MenuController1.OpenMenu();
                            if (MenuController2 != null && MenuController2.IsOpen())
                            {
                                MenuController2.CloseMenu();
                            }
                        }
                    }
                    break;

                case 4: // Toggle MenuUI2 (and close MenuUI1 if open)
                    if (MenuController2 != null)
                    {
                        if (MenuController2.IsOpen())
                        {
                            MenuController2.CloseMenu();
                        }
                        else
                        {
                            MenuController2.OpenMenu();
                            if (MenuController1 != null && MenuController1.IsOpen())
                            {
                                MenuController1.CloseMenu();
                            }
                        }
                    }
                    break;
            }
        }

        public (string id, string name, string friendCode) GetSelectedPlayerInfo()
        {
            if (banMenu.selectedClientId < 0)
            {
                return (null, null, null);
            }

            var client = AmongUsClient.Instance?.GetRecentClient(banMenu.selectedClientId);
            if (client == null)
            {
                Logger.Info("Client non trovato.");
                return (null, null, null);
            }

            string playerIdStr = client.Id.ToString();

            if (!int.TryParse(playerIdStr, out int playerId))
            {
                return (playerIdStr, client.PlayerName ?? "Unknown", client.FriendCode ?? "N/A");
            }

            PlayerControl player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);
            if (player == null)
            {
                return (playerIdStr, client.PlayerName ?? "Unknown", client.FriendCode ?? "N/A");
            }

            string playerName = player.GetRealName() ?? player.name ?? client.PlayerName ?? "Unknown";
            string friendCode = client.FriendCode ?? "N/A";

            return (playerIdStr, playerName, friendCode);
        }

        private string GetSelectedPlayerId()
        {
            if (banMenu.selectedClientId < 0) return null;

            var clientData = AmongUsClient.Instance.GetRecentClient(banMenu.selectedClientId);
            if (clientData == null) return null;

            return clientData.Id.ToString();
        }

        public class CustomButtonHandler : MonoBehaviour
        {
            public int ButtonId;
            public BanMenuButtonsPatch Mod;

            public Color normalColor = Color.white;
            public Color hoverColor = Color.white;
            public bool AffectsSelection = true;

            private SpriteRenderer spriteRenderer;
            private Vector3 originalScale;
            private Vector3 hoverScale;

            private void Start()
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                    spriteRenderer.color = normalColor;

                originalScale = transform.localScale;
                hoverScale = originalScale * 1.1f;
            }

            private void Update()
            {
                if (!gameObject.activeSelf) return;

                var wp = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var hit = Physics2D.OverlapPoint(wp);

                bool isMouseOver = (hit != null && hit.gameObject == gameObject);
                bool playerCurrentlySelected = AffectsSelection ? !string.IsNullOrEmpty(Mod?.GetSelectedPlayerInfo().id) : true;

                if (isMouseOver)
                {
                    if (transform.localScale != hoverScale)
                        transform.localScale = hoverScale;
                }
                else
                {
                    if (transform.localScale != originalScale)
                        transform.localScale = originalScale;
                }

                if (isMouseOver && Input.GetMouseButtonDown(0))
                {
                    if (AffectsSelection)
                    {
                        if (playerCurrentlySelected)
                        {
                            Mod?.OnCustomButtonClicked(ButtonId);
                        }
                        else
                        {
                            Debug.Log($"Click detected on {gameObject.name} but no player is selected (Button requires selection). ID: {Mod?.banMenu.selectedClientId}");
                            ShowChat(GetString("NoPlayerSelected"));
                        }
                    }
                    else
                    {
                        Mod?.OnCustomButtonClicked(ButtonId);
                    }
                }
            }
        }
    }
}