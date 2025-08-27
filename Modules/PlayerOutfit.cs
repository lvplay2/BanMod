using BanMod;
using Hazel;
using System;
using UnityEngine; // Assicurati di avere questo using se usi tipi come 'byte' che potrebbero essere parte di Unity in qualche contesto, anche se byte è standard C#

public static class PlayerOutfit
{
    public class OriginalPlayerOutfit
    {
        public byte ColorId { get; set; }
        public string HatId { get; set; }
        public string SkinId { get; set; }
        public string VisorId { get; set; }
    }

    public static OriginalPlayerOutfit LocalPlayerOriginalState { get; private set; }

    public static void SaveLocalPlayerState(PlayerControl localPlayer)
    {
        if (localPlayer == null || localPlayer.Data == null || localPlayer.Data.DefaultOutfit == null)
        {
            return;
        }

        LocalPlayerOriginalState = new OriginalPlayerOutfit
        {
            ColorId = (byte)localPlayer.Data.DefaultOutfit.ColorId,
            HatId = localPlayer.Data.DefaultOutfit.HatId,
            SkinId = localPlayer.Data.DefaultOutfit.SkinId,
            VisorId = localPlayer.Data.DefaultOutfit.VisorId,
        };
    }


    public static void ApplyAnonimusOutfit(PlayerControl localPlayer)
    {
        localPlayer.RpcSetColor(18);
        localPlayer.RpcSetHat("");
        localPlayer.RpcSetSkin("");
        localPlayer.RpcSetVisor("");
    }
    public static void ApplyOriginalOutfit(PlayerControl localPlayer)
    {
        localPlayer.RpcSetColor(LocalPlayerOriginalState.ColorId);
        localPlayer.RpcSetHat(LocalPlayerOriginalState.HatId);
        localPlayer.RpcSetSkin(LocalPlayerOriginalState.SkinId);
        localPlayer.RpcSetVisor(LocalPlayerOriginalState.VisorId);
    }
}