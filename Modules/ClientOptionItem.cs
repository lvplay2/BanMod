using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;


namespace BanMod;

// Credit: https://github.com/tukasa0001/TownOfHost/pull/1265
public class ClientOptionItem
{
    public static SpriteRenderer CustomBackground;
    private static int NumOptions;
    private readonly ConfigEntry<bool> Config;
    public readonly ToggleButtonBehaviour ToggleButton;

    public static ToggleButtonBehaviour ModOptionsButtonReference;

    private ClientOptionItem(
        string name,
        ConfigEntry<bool> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction = null)
    {
        try
        {
            Config = config;

            ToggleButtonBehaviour mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

            if (CustomBackground == null)
            {
                NumOptions = 0;
                CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
                CustomBackground.name = "CustomBackground";
                CustomBackground.transform.localScale = new(0.9f, 0.9f, 1f);
                CustomBackground.transform.localPosition += Vector3.back * 8;
                CustomBackground.gameObject.SetActive(false);

                ToggleButtonBehaviour closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                closeButton.transform.localPosition = new(1.3f, -2.3f, -6f);
                closeButton.name = "Back";
                closeButton.Text.text = Translator.GetString("Back");
                closeButton.Background.color = Palette.DisabledGrey;
                var closePassiveButton = closeButton.GetComponent<PassiveButton>();
                closePassiveButton.OnClick = new();
                closePassiveButton.OnClick.AddListener(new Action(() => { CustomBackground.gameObject.SetActive(false); }));

                UiElement[] selectableButtons = optionsMenuBehaviour.ControllerSelectable.ToArray();
                PassiveButton leaveButton = null;
                PassiveButton returnButton = null;

                foreach (UiElement button in selectableButtons)
                {
                    if (button == null) continue;

                    switch (button.name)
                    {
                        case "LeaveGameButton":
                            leaveButton = button.GetComponent<PassiveButton>();
                            break;
                        case "ReturnToGameButton":
                            returnButton = button.GetComponent<PassiveButton>();
                            break;
                    }
                }

                Transform generalTab = mouseMoveToggle.transform.parent.parent.parent;

                ToggleButtonBehaviour modOptionsButton = Object.Instantiate(mouseMoveToggle, generalTab);
                modOptionsButton.transform.localPosition = leaveButton?.transform.localPosition ?? new(0f, -2.4f, 1f);
                modOptionsButton.name = "Options";
                modOptionsButton.Text.text = Translator.GetString("Options");
                modOptionsButton.Background.color = new Color32(255, 215, 0, byte.MaxValue);
                var modOptionsPassiveButton = modOptionsButton.GetComponent<PassiveButton>();
                modOptionsPassiveButton.OnClick = new();
                modOptionsPassiveButton.OnClick.AddListener(new Action(() => CustomBackground.gameObject.SetActive(true)));

                ClientOptionItem.ModOptionsButtonReference = modOptionsButton;

                if (leaveButton != null && leaveButton.transform != null) leaveButton.transform.localPosition = new(-1.35f, -2.411f, -1f);

                if (returnButton != null) returnButton.transform.localPosition = new(1.35f, -2.411f, -1f);
            }

            ToggleButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);

            ToggleButton.transform.localPosition = new(
                NumOptions % 2 == 0 ? -1.3f : 1.3f,
                2.2f - (0.5f * (NumOptions / 2)),
                -6f);

            ToggleButton.name = name;
            ToggleButton.Text.text = Translator.GetString(name);
            var passiveButton = ToggleButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new();

            passiveButton.OnClick.AddListener(new Action(() =>
            {
                if (config != null) config.Value = !config.Value;

                UpdateToggle();
                additionalOnClickAction?.Invoke();
            }));

            UpdateToggle();
        }
        finally
        {
            NumOptions++;
        }
    }

    public static ClientOptionItem Create(
        string name,
        ConfigEntry<bool> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction = null)
    {
        return new(name, config, optionsMenuBehaviour, additionalOnClickAction);
    }

    public void UpdateToggle()
    {
        if (ToggleButton == null) return;

        Color32 color = Config is { Value: true } ? new(255, 215, 0, byte.MaxValue) : new Color32(77, 77, 77, byte.MaxValue);
        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
    }
}

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Update))]
public static class OptionsMenuButtonMovementPatch
{
    private static float moveSpeed = 0.01f;

    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        if (ClientOptionItem.ModOptionsButtonReference == null || !__instance.gameObject.activeSelf) return;

        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            Vector3 currentPos = ClientOptionItem.ModOptionsButtonReference.transform.localPosition;

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                ClientOptionItem.ModOptionsButtonReference.transform.localPosition = new Vector3(currentPos.x - moveSpeed, currentPos.y, currentPos.z);
            }
            else if (Input.GetKey(KeyCode.RightArrow))
            {
                ClientOptionItem.ModOptionsButtonReference.transform.localPosition = new Vector3(currentPos.x + moveSpeed, currentPos.y, currentPos.z);
            }
            else if (Input.GetKey(KeyCode.UpArrow))
            {
                ClientOptionItem.ModOptionsButtonReference.transform.localPosition = new Vector3(currentPos.x, currentPos.y + moveSpeed, currentPos.z);
            }
            else if (Input.GetKey(KeyCode.DownArrow))
            {
                ClientOptionItem.ModOptionsButtonReference.transform.localPosition = new Vector3(currentPos.x, currentPos.y - moveSpeed, currentPos.z);
            }
        }
    }
}