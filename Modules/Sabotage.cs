using BanMod;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static BanMod.BanMod;
using static BanMod.Utils;

namespace BanMod
{

    [HarmonyPatch(typeof(MapBehaviour))]
    [HarmonyPatch(nameof(MapBehaviour.ShowSabotageMap))]
    public static class MapBehaviour_ShowSabotageMap_CustomButtonsPatch
    {
        public static void Postfix(MapBehaviour __instance)
        {
            Logger.Info("[CustomSabotageButtons] Avvio Postfix per ShowSabotageMap.");

            Transform parentTransform = __instance.infectedOverlay?.transform;
            if (parentTransform == null)
            {
                Logger.Info("[CustomSabotageButtons] infectedOverlay nullo!");
                return;
            }
            if (!Options.Enablesabotage.GetBool())
            {
                Logger.Info("[CustomSabotageButtons] Opzione sabotaggio disabilitata. Nessun pulsante mostrato.");
                return;
            }

            CreateButton(__instance, parentTransform, "CustomSabotageButton_1", new Vector2(-0.0f, 2.3f), "BanMod.Resources.image.SabotageIcon1.png", () =>
            {
                SabotageManager.TryActivateSabotage(SystemTypes.Comms, 128);
                __instance.Close();
            });

            CreateButton(__instance, parentTransform, "CustomSabotageButton_2", new Vector2(-0.75f, 2.3f), "BanMod.Resources.image.SabotageIcon2.png", () =>
            {
                SabotageManager.TryActivateSabotage(SystemTypes.LifeSupp, 128);
                __instance.Close();
            });

            CreateButton(__instance, parentTransform, "CustomSabotageButton_3", new Vector2(0.75f, 2.3f), "BanMod.Resources.image.SabotageIcon3.png", () =>
            {
                SabotageManager.TryActivateSabotage(SystemTypes.Reactor, 128);
                __instance.Close();
            });

            CreateButton(__instance, parentTransform, "CustomSabotageButton_4", new Vector2(1.50f, 2.3f), "BanMod.Resources.image.SabotageIcon4.png", () =>
            {
                byte id = 4;
                for (int i = 0; i < 5; i++) id |= (byte)(1 << i);
                id |= 128;
                SabotageManager.TryActivateSabotage(SystemTypes.Electrical, id);
                __instance.Close();
            });
        }

        private static void CreateButton(MapBehaviour __instance, Transform parent, string name, Vector2 position, string spritePath, Action onClick)
        {
            GameObject buttonGO = new GameObject(name);
            buttonGO.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = buttonGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 999;

            buttonGO.AddComponent<CanvasScaler>();
            buttonGO.AddComponent<GraphicRaycaster>();

            CanvasGroup canvasGroup = buttonGO.AddComponent<CanvasGroup>();
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            RectTransform rectTransform = buttonGO.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.localPosition = position;
            rectTransform.sizeDelta = new Vector2(100f, 100f);
            rectTransform.localScale = Vector3.one * 0.01f;

            buttonGO.AddComponent<CanvasRenderer>();
            Image buttonImage = buttonGO.AddComponent<Image>();

            Sprite sprite = Utils.LoadSprite(spritePath, 100f);
            if (sprite != null)
            {
                buttonImage.sprite = sprite;
                buttonImage.SetNativeSize();
                Logger.Info($"[CustomSabotageButtons] Sprite caricato per {name}");
            }
            else
            {
                buttonImage.color = Color.gray;
                Logger.Info($"[CustomSabotageButtons] Sprite NON trovato per {name}");
            }

            Button button = buttonGO.AddComponent<Button>();
            Action value = () => onClick.Invoke();
            button.onClick.AddListener(value);

        }
    }
}
[HarmonyPatch(typeof(SabotageSystemType))]
public static class SabotageSystemTypePatch
{
    [HarmonyPatch(nameof(SabotageSystemType.UpdateSystem))]
    [HarmonyPostfix]
    public static void Postfix_UpdateSystem(SabotageSystemType __instance)
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        if (__instance.Timer > 0f && !SabotageManager.IsSabotageActive)
        {
            SabotageManager.SetSabotageActiveState(true);
        }

        SabotageManager.SetGameSabotageCooldown(__instance.Timer);
    }

    [HarmonyPatch(nameof(SabotageSystemType.Deteriorate))]
    [HarmonyPostfix]
    public static void Postfix_Deteriorate(SabotageSystemType __instance)
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        SabotageManager.SetGameSabotageCooldown(__instance.Timer);

        if (__instance.Timer <= 0f && !__instance.AnyActive && SabotageManager.IsSabotageActive)
        {
            SabotageManager.SetSabotageActiveState(false);
        }
    }

    [HarmonyPatch(nameof(SabotageSystemType.SetInitialSabotageCooldown))]
    [HarmonyPostfix]
    public static void Postfix_InitialCooldown(SabotageSystemType __instance)
    {
        if (!AmongUsClient.Instance.AmHost)
            return;
        // Inizio partita: setta il cooldown a 10f
        SabotageManager.SetGameSabotageCooldown(__instance.Timer);
       // Logger.Info("[SabotageSystemTypePatch] Cooldown iniziale impostato a 10s.");
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
public static class ShipStatus_FixedUpdate_Patch
{
    [HarmonyPrefix]
    public static bool Prefix(ShipStatus __instance)
    {
        if (!AmongUsClient.Instance.AmHost)
            return true;

        if (__instance == null)
            return true;

        if (Options.DisableAllSabotages.GetBool())
        {
            FixAllSabotages(__instance);
            return true;
        }

        if (Options.DisableReactorSabotage.GetBool())
            FixSabotage(__instance, SystemTypes.Reactor);

        if (Options.DisableCommsSabotage.GetBool())
            FixSabotage(__instance, SystemTypes.Comms);

        if (Options.DisableO2Sabotage.GetBool())
            FixSabotage(__instance, SystemTypes.LifeSupp);

        if (Options.DisableElectricalSabotage.GetBool())
            FixSabotage(__instance, SystemTypes.Electrical);

        if (Options.DisableLaboratorySabotage.GetBool())
            FixSabotage(__instance, SystemTypes.Laboratory);

        if (Options.DisableHeliSabotage.GetBool())
            FixSabotage(__instance, SystemTypes.HeliSabotage);

        if (Options.DisableMushroomSabotage.GetBool())
            FixSabotage(__instance, SystemTypes.MushroomMixupSabotage);

        return true;
    }

    private static void FixSabotage(ShipStatus shipStatus, SystemTypes systemType)
    {
        if (!Utils.IsActive(systemType)) return;

        shipStatus.RpcUpdateSystem(systemType, 16);

        if (systemType == SystemTypes.Electrical && shipStatus.Systems.TryGetValue(systemType, out var system))
        {
            var elecSys = system.Cast<SwitchSystem>();
            for (var i = 0; i < 5; i++)
            {
                int switchMask = 1 << i;
                if ((elecSys.ActualSwitches & switchMask) != (elecSys.ExpectedSwitches & switchMask))
                {
                    shipStatus.RpcUpdateSystem(SystemTypes.Electrical, (byte)i);
                }
            }
        }

        //Logger.Info($"[SABOTAGE] Sabotaggio {systemType} disabilitato e fissato in FixedUpdate.");
    }

    private static void FixAllSabotages(ShipStatus shipStatus)
    {
        if (!Utils.AnySabotageIsActive()) return;

        FixSabotage(shipStatus, SystemTypes.Reactor);
        FixSabotage(shipStatus, SystemTypes.Laboratory);
        FixSabotage(shipStatus, SystemTypes.HeliSabotage);
        FixSabotage(shipStatus, SystemTypes.LifeSupp);
        FixSabotage(shipStatus, SystemTypes.Comms);
        FixSabotage(shipStatus, SystemTypes.Electrical);
        FixSabotage(shipStatus, SystemTypes.MushroomMixupSabotage);

        //Logger.Info("[SABOTAGE] Tutti sabotaggi disabilitati e fissati in FixedUpdate.");
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
public static class BlockCloseDoorsPatch
{
    public static bool Prefix(SystemTypes room)
    {
        if (Options.DisableDoorSabotage.GetBool())
        {
            Debug.Log($"[BlockCloseDoorsPatch] Tentativo di chiudere porta in {room} bloccato.");
            return false; // Blocca la chiusura delle porte
        }
        return true; // Permetti la chiusura normale
    }
}
[HarmonyPatch(typeof(SecurityCameraSystemType), nameof(SecurityCameraSystemType.UpdateSystem))]
[HarmonyPatch(typeof(SurveillanceMinigame), nameof(SurveillanceMinigame.Update))]
public static class AlwaysStaticCameraPatch
{
    public static bool Prefix(SurveillanceMinigame __instance)
    {
        if (!Options.DisableSecurity.GetBool()) return true;

        if (!__instance.isStatic)
        {
            __instance.isStatic = true;
            for (int i = 0; i < __instance.ViewPorts.Length; i++)
            {
                __instance.ViewPorts[i].sharedMaterial = __instance.StaticMaterial;
                __instance.SabText[i].gameObject.SetActive(true);
            }
        }

        return false;
    }
}
[HarmonyPatch(typeof(PlanetSurveillanceMinigame), nameof(PlanetSurveillanceMinigame.Update))]
public static class AlwaysStaticPlanetCameras
{
    public static bool Prefix(PlanetSurveillanceMinigame __instance)
    {
        if (!Options.DisableHQ.GetBool()) return true;

        if (!__instance.isStatic)
        {
            __instance.isStatic = true;
            __instance.ViewPort.sharedMaterial = __instance.StaticMaterial;
            __instance.SabText.gameObject.SetActive(true);
        }

        return false;
    }
}
[HarmonyPatch(typeof(FungleSurveillanceMinigame), nameof(FungleSurveillanceMinigame.Begin))]
public static class AlwaysStaticFungleCameras
{
    public static void Postfix(FungleSurveillanceMinigame __instance)
    {
        if (!Options.DisableFungleCam.GetBool()) return;

        if (__instance != null && __instance.viewport != null)
        {
            __instance.viewport.material.SetTexture("_MainTex", null);
            // __instance.viewport.sharedMaterial = __instance.StaticMaterial; // opzionale
        }
    }
}
[HarmonyPatch(typeof(MapCountOverlay), nameof(MapCountOverlay.Update))]
public static class DisableMapCountOverlayUpdate
{
    public static bool Prefix()
    {
        return !Options.DisableAdmin.GetBool(); // Disattiva se true
    }
}