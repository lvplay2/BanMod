using System;
using System.Collections.Generic;
using System.Linq;
using BanMod;

namespace BanMod;
public static class ImpostorManager
{

    public static List<(int PlayerId, string PlayerName)> ImpostorsList = new List<(int PlayerId, string PlayerName)>();

    public static void DetectImpostors()
    {
        if (!AmongUsClient.Instance.AmHost)
            return;
        var alive = BanMod.AllAlivePlayerControls;
        if (alive == null || alive.Count == 0)
            return;
        var impostors = BanMod.AllAlivePlayerControls
            .Where(p => p.Data?.Role?.TeamType == RoleTeamTypes.Impostor)
            .Select(p => (p.PlayerId, p.Data.PlayerName))
            .ToList();

        ImpostorsList.Clear();  
        foreach (var impostor in impostors)
        {
            ImpostorsList.Add(impostor);
        }
    }

    public static List<(int PlayerId, string PlayerName)> GetImpostorsList()
    {

        return ImpostorsList;
    }
}
