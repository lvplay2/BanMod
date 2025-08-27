using AmongUs.Data;
using InnerNet;
using System.Linq;

namespace BanMod;
public static class Spoof
{
    public static uint parsedLevel;

    public static void spoofLevel()
    {
        // Parse Spoofing.Level config entry and turn it into a uint
        if (!string.IsNullOrEmpty(BanMod.spoofLevel.Value) &&
            uint.TryParse(BanMod.spoofLevel.Value, out parsedLevel) &&
            parsedLevel != DataManager.Player.Stats.Level)
        {

            // Temporarily save the spoofed level using DataManager
            DataManager.Player.stats.level = parsedLevel - 1;
            DataManager.Player.Save();
        }
    }

}
