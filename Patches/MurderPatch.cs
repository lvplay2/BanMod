using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BanMod.Translator;
using static BanMod.Utils;

namespace BanMod;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
public static class RpcMurderPlayerPatch
{
    public static bool Prefix(PlayerControl __instance, PlayerControl target, bool didSucceed)
    {
        if (!AmongUsClient.Instance.AmHost || __instance == null || target == null || __instance.Data == null || target.Data == null)
            return true;


        bool targetIsProtected = target.protectedByGuardianId > -1 || BanMod.ShieldedPlayers.Contains(target.PlayerId) || ImmortalManager.IsImmortal(target.PlayerId);
        if (targetIsProtected)
        {
            Utils.ForceProtect(target, overrideExisting: true);
            return false; // Blocca kill se target protetto
        }

        return true;
    }

}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
public static class MurderPlayerCombinedPatch
{
    public static void Postfix(PlayerControl __instance, PlayerControl target, MurderResultFlags resultFlags)
    {
        if (!AmongUsClient.Instance.AmHost || target == null || target.Data == null)
            return;

        if (BanMod.FirstDeadFriendCode == null && resultFlags.HasFlag(MurderResultFlags.Succeeded))
        {
            var friendCode = target.Data.FriendCode;
            if (!string.IsNullOrEmpty(friendCode))
            {
                BanMod.FirstDeadFriendCode = friendCode;
            }
        }

        if (!BanMod.playerDeathTimes.ContainsKey(target.PlayerId) && resultFlags.HasFlag(MurderResultFlags.Succeeded))
        {
            BanMod.playerDeathTimes[target.PlayerId] = Time.time;
        }
    }
}
