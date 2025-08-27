using AmongUs.GameOptions;
using BanMod;
using Epic.OnlineServices;
using HarmonyLib;
using Il2CppSystem.IO;
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using static BanMod.Translator;
using Object = UnityEngine.Object;

namespace BanMod
{
    [HarmonyPatch(typeof(GameSettingMenu))]
    public static class GameSettingMenuPatch
    {
        private static GameOptionsMenu SettingsTab;
        public static PassiveButton SettingsButton;
        public static PassiveButton BanButton;
        public static PassiveButton ModdedButton;
        public static PassiveButton OtherButton;
        public static CategoryHeaderMasked AfkCategoryHeader { get; private set; }
        public static CategoryHeaderMasked BlocklistCategoryHeader { get; private set; }
        public static CategoryHeaderMasked WordlistCategoryHeader { get; private set; }
        public static CategoryHeaderMasked SpamlistCategoryHeader { get; private set; }
        public static CategoryHeaderMasked PhantomCategoryHeader { get; private set; }
        public static CategoryHeaderMasked ShapeshifterCategoryHeader { get; private set; }
        public static CategoryHeaderMasked PhantomModdedCategoryHeader { get; private set; }
        public static CategoryHeaderMasked ShapeshifterModdedCategoryHeader { get; private set; }
        public static CategoryHeaderMasked ImpostorCategoryHeader { get; private set; }
        public static CategoryHeaderMasked EngineerCategoryHeader { get; private set; }
        public static CategoryHeaderMasked EngineerModdedCategoryHeader { get; private set; }
        public static CategoryHeaderMasked ImmortalCategoryHeader { get; private set; }
        public static CategoryHeaderMasked ScientistCategoryHeader { get; private set; }
        public static CategoryHeaderMasked ExilerCategoryHeader { get; private set; }
        public static CategoryHeaderMasked CrewmateCategoryHeader { get; private set; }
        public static CategoryHeaderMasked HostCategoryHeader { get; private set; }
        public static CategoryHeaderMasked GeneralModdedCategoryHeader { get; private set; }
        public static CategoryHeaderMasked GeneralCategoryHeader { get; private set; }
        public static CategoryHeaderMasked SabotageCategoryHeader { get; private set; }

        public static FreeChatInputField InputField;

        public const string MenuName = "ModTab";

        [HarmonyPatch(nameof(GameSettingMenu.Start)), HarmonyPostfix]
        public static void StartPostfix(GameSettingMenu __instance)
        {
            // Evita di istanziare il tab e i pulsanti se sono già stati creati
            if (SettingsTab != null) return;

            SettingsTab = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
            SettingsTab.name = MenuName;
            var vanillaOptions = SettingsTab.GetComponentsInChildren<OptionBehaviour>();
            foreach (var vanillaOption in vanillaOptions)
                Object.Destroy(vanillaOption.gameObject);

            var gameSettingsLabel = __instance.transform.Find("GameSettingsLabel");
            if (gameSettingsLabel)
                gameSettingsLabel.localPosition += Vector3.up * 0.2f;
            __instance.MenuDescriptionText.transform.parent.localPosition += Vector3.up * 0.4f;
            __instance.GamePresetsButton.transform.parent.localPosition += Vector3.up * 0.5f;

            // Pulsanti
            SettingsButton = Object.Instantiate(__instance.GameSettingsButton, __instance.GameSettingsButton.transform.parent);
            SettingsButton.name = "SettingsButton";
            SettingsButton.transform.localPosition = new Vector3(-3.86f, -2.23f, -2.00f);
            ResizeButton(SettingsButton, 0.4f, 0.5f);
            SettingsButton.buttonText.DestroyTranslator();
            SettingsButton.buttonText.text = GetString("HostSettingsLabel");
            SettingsButton.buttonText.fontSize = 12;
            SettingsButton.buttonText.color = Color.white;
            SetButtonColor(SettingsButton, BanMod.UnityModColor);

            BanButton = Object.Instantiate(SettingsButton, SettingsButton.transform.parent);
            BanButton.name = "BanButton"; 
            BanButton.transform.localPosition = new Vector3(-2.46f, -2.23f, -2.00f);
            ResizeButton(BanButton, 0.4f, 0.5f);
            BanButton.buttonText.text = GetString("BanOption");
            BanButton.buttonText.fontSize = 12;
            BanButton.buttonText.color = Color.white;
            SetButtonColor(BanButton, BanMod.UnityModColor);

            ModdedButton = Object.Instantiate(SettingsButton, SettingsButton.transform.parent);
            ModdedButton.name = "ModdedButton";
            ModdedButton.transform.localPosition = new Vector3(-3.86f, -2.63f, -2.00f);
            ResizeButton(ModdedButton, 0.4f, 0.5f);
            ModdedButton.buttonText.text = GetString("GeneralOption");
            ModdedButton.buttonText.fontSize = 12;
            ModdedButton.buttonText.color = Color.white;
            SetButtonColor(ModdedButton, BanMod.UnityModColor);

            OtherButton = Object.Instantiate(SettingsButton, SettingsButton.transform.parent);
            OtherButton.name = "OtherButton";
            OtherButton.transform.localPosition = new Vector3(-2.46f, -2.63f, -2.00f);
            ResizeButton(OtherButton, 0.4f, 0.5f);
            OtherButton.buttonText.text = GetString("RoleOption");
            OtherButton.buttonText.fontSize = 12;
            OtherButton.buttonText.color = Color.white;
            SetButtonColor(OtherButton, BanMod.UnityModColor);

            // Listener per SettingsButton
            Action value = () =>
            {
                __instance.ChangeTab(-1, false);
                SettingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.text = GetString("SettingsTabDescription");
                SettingsButton.SelectButton(true);
                // La logica di ShowOptions deve essere più specifica per ciò che vuoi mostrare con questo tab
                ShowOptions(OptionCategory.General); // Mostra solo le opzioni di tipo "Settings"
            };
            SettingsButton.OnClick.AddListener(value);

            // Listener per BanButton
            Action value1 = () =>
            {
                __instance.ChangeTab(-1, false);
                SettingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.text = GetString("BanTabDescription");
                BanButton.SelectButton(true);
                // Mostra solo block/spam/word
                ShowOptions(OptionCategory.Afk); // Passa una categoria specifica o un flag
            };
            BanButton.OnClick.AddListener(value1);

            // Listener per ModdedButton
            Action value2 = () =>
            {
                __instance.ChangeTab(-1, false);
                SettingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.text = GetString("ModdedTabDescription"); // Assicurati di avere questa stringa nel Translator
                ModdedButton.SelectButton(true);
                // Mostra le opzioni relative ai "Modded" (da definire in OptionItem)
                ShowOptions(OptionCategory.GeneralModded);
            };
            ModdedButton.OnClick.AddListener(value2);

            // Listener per OtherButton
            Action value3 = () =>
            {
                __instance.ChangeTab(-1, false);
                SettingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.text = GetString("OtherTabDescription"); // Assicurati di avere questa stringa nel Translator
                OtherButton.SelectButton(true);
                // Mostra le opzioni relative a "Other" o "Role"
                ShowOptions(OptionCategory.Impostor);
            };
            OtherButton.OnClick.AddListener(value3);


            // Category headers
            AfkCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Afk");
            BlocklistCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Block");
            WordlistCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Word");
            SpamlistCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Spam");
            PhantomCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Phantom");
            ShapeshifterCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Shape");
            PhantomModdedCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.PhantomModded");
            ShapeshifterModdedCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.ShapeModded");
            ImpostorCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Impostor");
            EngineerCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Engineer");
            EngineerModdedCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.EngineerModded");
            ImmortalCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Immortal");
            ScientistCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Scientist");
            ExilerCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Exiler");
            CrewmateCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Crew");
            HostCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Host");
            GeneralModdedCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.GenMod");
            GeneralCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Gen");
            SabotageCategoryHeader = CreateCategoryHeader(__instance, SettingsTab, "TabGroup.Sabotage");

            // Opzioni
            var template = __instance.GameSettingsTab.stringOptionOrigin;
            var scOptions = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
            foreach (var option in OptionItem.AllOptions)
            {
                if (option.OptionBehaviour == null)
                {
                    var stringOption = Object.Instantiate(template, SettingsTab.settingsContainer);
                    scOptions.Add(stringOption);
                    stringOption.SetClickMask(__instance.GameSettingsButton.ClickMask);
                    stringOption.SetUpFromData(stringOption.data, GameOptionsMenu.MASK_LAYER);
                    stringOption.OnValueChanged = new Action<OptionBehaviour>((o) => { });
                    stringOption.TitleText.text = option.Name;
                    stringOption.Value = stringOption.oldValue = option.CurrentValue;
                    stringOption.ValueText.text = option.GetString();
                    stringOption.name = option.Name;

                    var indent = 0f;
                    var parent = option.Parent;
                    while (parent != null)
                    {
                        indent += 0.15f;
                        parent = parent.Parent;
                    }
                    stringOption.LabelBackground.size += new Vector2(2f - indent * 2, 0f);
                    stringOption.LabelBackground.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);
                    stringOption.TitleText.rectTransform.sizeDelta += new Vector2(2f - indent * 2, 0f);
                    stringOption.TitleText.transform.localPosition += new Vector3(-1f + indent, 0f, 0f);

                    option.OptionBehaviour = stringOption;
                }
                option.OptionBehaviour.gameObject.SetActive(true); // Imposta attivo di default, poi ShowOptions() lo gestirà
            }
            SettingsTab.Children = scOptions;
            SettingsTab.gameObject.SetActive(false);
        }

        private static void SetButtonColor(PassiveButton button, Color color)
        {
            var activeSprite = button.activeSprites.GetComponent<SpriteRenderer>();
            var selectedSprite = button.selectedSprites.GetComponent<SpriteRenderer>();
            activeSprite.color = selectedSprite.color = color;
        }

        private static void ResizeButton(PassiveButton button, float scaleX, float scaleY = 1f)
        {
            button.transform.localScale = new Vector3(scaleX, scaleY, 1f);

            var rt = button.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(rt.sizeDelta.x * scaleX, rt.sizeDelta.y * scaleY);
            }
        }

        private static void ShowOptions(OptionCategory categoryToShow)
        {
            foreach (var option in OptionItem.AllOptions)
            {
                if (option?.OptionBehaviour == null) continue;

                bool shouldBeActive = false;
                switch (categoryToShow)
                {
                    case OptionCategory.Afk:
                        shouldBeActive = option.Category == OptionCategory.Blocklist ||
                                         option.Category == OptionCategory.Wordlist ||
                                         option.Category == OptionCategory.Spamlist ||
                                         option.Category == OptionCategory.Afk;
                        break;
                    case OptionCategory.General:
                        shouldBeActive = option.Category == OptionCategory.Host;
                        break;
                    case OptionCategory.GeneralModded:
                        shouldBeActive = option.Category == OptionCategory.General ||
                                         option.Category == OptionCategory.GeneralModded ||
                                         option.Category == OptionCategory.PhantomModded ||
                                         option.Category == OptionCategory.EngineerModded ||
                                         option.Category == OptionCategory.ShapeshifterModded ||
                                         option.Category == OptionCategory.Sabotage;
                        break;
                    case OptionCategory.Impostor:
                        shouldBeActive = option.Category == OptionCategory.Impostor ||
                                         option.Category == OptionCategory.Engineer ||
                                         option.Category == OptionCategory.Scientist ||
                                         option.Category == OptionCategory.Exiler ||
                                         option.Category == OptionCategory.Crewmate ||
                                         option.Category == OptionCategory.Phantom ||
                                         option.Category == OptionCategory.Shapeshifter ||
                                         option.Category == OptionCategory.Immortal;
                        break;

                    default:
                        // Se non viene specificata una categoria, mostra tutto o niente a seconda della logica predefinita
                        shouldBeActive = true; // O false, a seconda del comportamento desiderato
                        break;
                }
                option.OptionBehaviour.gameObject.SetActive(shouldBeActive);
            }

            // Assicurati che gli header delle categorie siano mostrati solo quando hanno opzioni visibili
            AfkCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Afk);
            BlocklistCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Blocklist);
            WordlistCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Wordlist); // Se AFK è parte di Role
            SpamlistCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Spamlist);
            PhantomCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Phantom);
            ShapeshifterCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Shapeshifter);
            PhantomModdedCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.PhantomModded);
            ShapeshifterModdedCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.ShapeshifterModded);
            ImpostorCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Impostor);
            EngineerCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Engineer);
            EngineerModdedCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.EngineerModded);
            ImmortalCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Immortal);
            ScientistCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Scientist);
            ExilerCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Exiler);
            CrewmateCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Crewmate);
            HostCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Host);
            GeneralModdedCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.GeneralModded);
            GeneralCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.General);
            SabotageCategoryHeader.gameObject.SetActive(categoryToShow == OptionCategory.Sabotage);

        }


        private static CategoryHeaderMasked CreateCategoryHeader(GameSettingMenu __instance, GameOptionsMenu tohTab, string translationKey)
        {
            var categoryHeader = Object.Instantiate(__instance.GameSettingsTab.categoryHeaderOrigin, Vector3.zero, Quaternion.identity, tohTab.settingsContainer);
            categoryHeader.name = translationKey;
            categoryHeader.Title.text = GetString(translationKey);
            var maskLayer = GameOptionsMenu.MASK_LAYER;
            categoryHeader.Background.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
            if (categoryHeader.Divider != null)
                categoryHeader.Divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);
            categoryHeader.Title.fontMaterial.SetFloat("_StencilComp", 3f);
            categoryHeader.Title.fontMaterial.SetFloat("_Stencil", (float)maskLayer);
            categoryHeader.transform.localScale = Vector3.one * GameOptionsMenu.HEADER_SCALE;
            return categoryHeader;
        }

        [HarmonyPatch(nameof(GameSettingMenu.ChangeTab)), HarmonyPrefix]
        public static void ChangeTabPrefix(bool previewOnly)
        {
            if (!previewOnly)
            {
                if (SettingsTab) SettingsTab.gameObject.SetActive(false);
                if (SettingsButton) SettingsButton.SelectButton(false);
                if (BanButton) BanButton.SelectButton(false);
                if (ModdedButton) ModdedButton.SelectButton(false); // Deseleziona anche ModdedButton
                if (OtherButton) OtherButton.SelectButton(false);   // Deseleziona anche OtherButton
            }
        }
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    public class GameOptionsMenuUpdatePatch
    {
        private static float _timer = 1f;

        public static void Postfix(GameOptionsMenu __instance)
        {
            if (__instance.name != GameSettingMenuPatch.MenuName) return;

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            float offset = 2.7f;
            // bool isOdd = true; // isOdd non viene usato, può essere rimosso

            // Modifica per usare le liste di OptionItem già definite o filtrate
            void RenderGroup(CategoryHeaderMasked header, System.Collections.Generic.IEnumerable<OptionItem> options)
            {
                // Filtra le opzioni attive prima di controllare anyVisible
                var visibleOptions = options.Where(opt => opt.OptionBehaviour != null && opt.OptionBehaviour.gameObject.activeSelf).ToList();
                bool anyVisible = visibleOptions.Any();
                header.gameObject.SetActive(anyVisible);
                if (!anyVisible) return;

                offset -= GameOptionsMenu.HEADER_HEIGHT;
                header.transform.localPosition = new Vector3(GameOptionsMenu.HEADER_X, offset, -2f);

                foreach (var option in visibleOptions)
                {
                    // L'opzione è già stata filtrata come attiva, non serve un ulteriore controllo qui
                    // if (option?.OptionBehaviour == null || !option.OptionBehaviour.gameObject.activeSelf) continue;

                    offset -= GameOptionsMenu.SPACING_Y;
                    if (option.IsHeader) // Assicurati che IsHeader sia impostato correttamente in OptionItem
                        offset -= HeaderSpacingY;

                    option.OptionBehaviour.transform.localPosition = new Vector3(GameOptionsMenu.START_POS_X, offset, -2f);
                    // isOdd = !isOdd; // Rimuovi se non utilizzato
                }
            }

            // Chiamate a RenderGroup con le liste filtrate per categoria
            RenderGroup(GameSettingMenuPatch.AfkCategoryHeader, OptionItem.AfkOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.BlocklistCategoryHeader, OptionItem.BlocklistOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.WordlistCategoryHeader, OptionItem.WordlistOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.SpamlistCategoryHeader, OptionItem.SpamlistOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.PhantomCategoryHeader, OptionItem.PhantomOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.ShapeshifterCategoryHeader, OptionItem.ShapeshifterOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.PhantomModdedCategoryHeader, OptionItem.PhantomModdedOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.ShapeshifterModdedCategoryHeader, OptionItem.ShapeshifterModdedOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.ImpostorCategoryHeader, OptionItem.ImpostorOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.EngineerCategoryHeader, OptionItem.EngineerOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.EngineerModdedCategoryHeader, OptionItem.EngineerModdedOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.ImmortalCategoryHeader, OptionItem.ImmortalOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.ScientistCategoryHeader, OptionItem.ScientistOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.ExilerCategoryHeader, OptionItem.ExilerOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.CrewmateCategoryHeader, OptionItem.CrewmateOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.HostCategoryHeader, OptionItem.HostOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.GeneralModdedCategoryHeader, OptionItem.GeneralModdedOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.GeneralCategoryHeader, OptionItem.GeneralOptions.Cast<OptionItem>());
            RenderGroup(GameSettingMenuPatch.SabotageCategoryHeader, OptionItem.SabotageOptions.Cast<OptionItem>());

            __instance.scrollBar.ContentYBounds.max = (-offset) - 1.5f;
        }

        private const float HeaderSpacingY = 0.2f;
    }

    // Le patch seguenti sembrano già corrette, le lascio invariate
    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Initialize))]
    public class StringOptionInitializePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
            __instance.TitleText.text = option.GetName();
            __instance.Value = __instance.oldValue = option.CurrentValue;
            __instance.ValueText.text = option.GetString();

            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            option.SetValue(option.CurrentValue + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            option.SetValue(option.CurrentValue - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            OptionItem.SyncAllOptions();
        }
    }
}
[HarmonyPatch(typeof(NormalGameOptionsV09), nameof(NormalGameOptionsV09.SetRecommendations), [typeof(int), typeof(bool), typeof(RulesPresets)])]
public static class SetRecommendationsPatch
{
    public static bool Prefix(NormalGameOptionsV09 __instance, int numPlayers, bool isOnline, RulesPresets rulesPresets)
    {
        switch (rulesPresets)
        {
            case RulesPresets.Standard: SetStandardRecommendations(__instance, numPlayers, isOnline); return false;
            default: return true;
        }
    }
    private static void SetStandardRecommendations(NormalGameOptionsV09 __instance, int numPlayers, bool isOnline)
    {
        __instance.MaxPlayers = 15;
        __instance.NumImpostors = 2;
        __instance.PlayerSpeedMod = 1.75f;
        __instance.CrewLightMod = 0.75f;
        __instance.ImpostorLightMod = 2.0f;
        __instance.KillCooldown = 17.5f;
        __instance.NumCommonTasks = 1;
        __instance.NumLongTasks = 0;
        __instance.NumShortTasks = 4;
        __instance.NumEmergencyMeetings = 2;
        __instance.AnonymousVotes = false;
        __instance.TaskBarMode = (AmongUs.GameOptions.TaskBarMode) 1;
        __instance.KillDistance = 0;
        __instance.EmergencyCooldown = 15;
        __instance.DiscussionTime = 45;
        __instance.VotingTime = 60;
        __instance.IsDefaults = true;
        __instance.ConfirmImpostor = true;
        __instance.VisualTasks = false;

        __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 1, 100);
        __instance.roleOptions.SetRoleRate(RoleTypes.Phantom, 1, 100);
        __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, 1, 100);
        __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 1, 100);
        __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, 1, 100);
        __instance.roleOptions.SetRoleRate(RoleTypes.Noisemaker, 1, 100);
        __instance.roleOptions.SetRoleRate(RoleTypes.Tracker, 1, 100);

    }
}
