using AmongUs.GameOptions;
using HarmonyLib;
using UnityEngine;

namespace BanMod;

[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetRight))]
class ChatBubbleSetRightPatch
{
    public static void Postfix(ChatBubble __instance)
    {
        if (BanMod.isChatCommand) __instance.SetLeft();
    }
}
[HarmonyPatch(typeof(ChatBubble), nameof(ChatBubble.SetName))]
class ChatBubbleSetNamePatch
{
    public static void Postfix(ChatBubble __instance, [HarmonyArgument(1)] bool isDead)
    {
        if (Options.DarkTheme.GetBool())
        {
            if (isDead)
                __instance.Background.color = new(0.1f, 0.1f, 0.1f, 0.5f);
            else 
                __instance.Background.color = new(0.1f, 0.1f, 0.1f, 1f);

            __instance.TextArea.color = Color.white;
        }

 
    }
}

