using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Rewired.Utils.Platforms.Windows;

namespace BanMod;

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{
    public static ClientOptionItem AktiveChat;
    public static ClientOptionItem AddBanToList;
    public static ClientOptionItem ExcludeFriends;
    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        if (__instance.DisableMouseMovement == null) return;

       
        if (AddBanToList == null || AddBanToList.ToggleButton == null)
        {
            AddBanToList = ClientOptionItem.Create("AddBanToList", BanMod.AddBanToList, __instance);
        }

        if (AktiveChat == null || AktiveChat.ToggleButton == null)
        {
            AktiveChat = ClientOptionItem.Create("AktiveChat", BanMod.AktiveChat, __instance);
        }
        if (ExcludeFriends == null || ExcludeFriends.ToggleButton == null)
        {
            ExcludeFriends = ClientOptionItem.Create("ExcludeFriends", BanMod.ExcludeFriends, __instance);
        }

    }
}

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
public static class OptionsMenuBehaviourClosePatch
{
    public static void Postfix()
    {
        ClientOptionItem.CustomBackground?.gameObject.SetActive(false);
    }
}