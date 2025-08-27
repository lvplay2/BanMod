using System;
using System.Collections.Generic;
using UnityEngine;
using static BanMod.Utils;
using static BanMod.Translator;

namespace BanMod
{
    public static class AFKDetector
    {
        private static bool wasInMeeting = false;
        public static readonly Dictionary<byte, Data> PlayerData = new();
        private const float periodicInterval = 20f;
        public static bool IsPlayerAfk;


        public static void RecordPosition(PlayerControl pc)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableDetector.GetBool() || !GameStates.IsInGameplay || pc == null)
                return;

            PlayerData[pc.PlayerId] = new Data
            {
                LastPosition = pc.Pos(),
                Timer = Options.DetectionDelay.GetInt(),
                CurrentPhase = Data.Phase.Detection,
                OriginalName = pc.Data.PlayerName
            };
        }

        public static void OnFixedUpdate(PlayerControl pc, bool force = false)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!Options.EnableDetector.GetBool() || !GameStates.IsInGameplay || pc == null)
                return;

            DeviceUsageTracker.UpdateUsage();
            if (DeviceUsageTracker.IsUsingAnyDevice(pc.PlayerId)) return;
            if (pc.Data.IsDead) return;
            if (!PlayerData.TryGetValue(pc.PlayerId, out var data)) return;

            if (GameStates.IsMeeting)
            {
                if (!wasInMeeting)
                {
                    wasInMeeting = true;

                    if (data.CurrentPhase == Data.Phase.Warning)
                    {
                        RestoreOriginalName(pc, data);
                        if (!ImmortalManager.IsImmortal(pc))
                        {
                            Utils.RemovePlayerFromShield(pc);
                        }
                        else
                        {
                        }
                    }
                    PlayerData.Remove(pc.PlayerId);
                    RecordPosition(pc);
                }
                return;
            }
            else
            {
                wasInMeeting = false;
            }

            if (Vector2.Distance(pc.Pos(), data.LastPosition) > 0.1f)
            {
                if (data.CurrentPhase == Data.Phase.Warning)
                    RestoreOriginalName(pc, data);

                if (!ImmortalManager.IsImmortal(pc))
                {
                    Utils.RemovePlayerFromShield(pc);
                }

                PlayerData.Remove(pc.PlayerId);
                RecordPosition(pc);
                return;
            }

            var lastTimer = (int)Math.Round(data.Timer);
            data.Timer -= Time.fixedDeltaTime;
            var currentTimer = (int)Math.Round(data.Timer);

            if (data.Timer <= 0f)
            {
                switch (data.CurrentPhase)
                {
                    case Data.Phase.Detection:
                        data.CurrentPhase = Data.Phase.Warning;
                        data.Timer = Options.TimeToActivate.GetInt() * 60;

                        if (Options.EnableShield.GetBool() && !ImmortalManager.IsImmortal(pc) && !BanMod.ShieldedPlayers.Contains(pc.PlayerId))
                        {
                            BanMod.ShieldedPlayers.Add(pc.PlayerId);
                            Utils.ForceProtect(pc, overrideExisting: true);
                        }
                        else if (ImmortalManager.IsImmortal(pc))
                        {
                        }
                        break;

                    case Data.Phase.Consequence:
                        data.CurrentPhase = Data.Phase.Consequence;

                        RestoreOriginalName(pc, data);
                        if (!ImmortalManager.IsImmortal(pc))
                        {
                            Utils.RemovePlayerFromShield(pc);
                        }

                        bool isFriend = Utils.IsFriends(pc.FriendCode);
                        bool isHostPlayer = pc.PlayerId == PlayerControl.LocalPlayer.PlayerId;

                        if (isFriend || isHostPlayer)
                        {
                            PlayerData.Remove(pc.PlayerId);
                            return;
                        }

                        if (Options.EnableAfkKick.GetBool())
                        {
                            AmongUsClient.Instance.KickPlayer(pc.GetClientId(), false);
                            string text = $"{pc.Data.PlayerName} {GetString("afkkicked")}";
                            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                            {
                                Utils.RequestProxyMessage(text, 255);
                                MessageBlocker.UpdateLastMessageTime();
                            }
                            else
                            {
                                Utils.SendMessage(text, 255, GetString("AfkTitle"));
                                MessageBlocker.UpdateLastMessageTime();

                            }
                            PlayerData.Remove(pc.PlayerId);
                        }
                        return;
                }
            }

            if (data.CurrentPhase == Data.Phase.Warning)
            {
                string suffix = GetLocalizedAfkSuffix(Options.EnableShield.GetBool());
                if (!pc.Data.PlayerName.EndsWith(suffix))
                {
                    pc.RpcSetName($"{data.OriginalName}{suffix}");
                }
            }
        }

        private static string GetLocalizedAfkSuffix(bool isShielded)
        {
            string key = isShielded ? "afk_shielded" : "afk";
            string suffix = GetString(key) ?? (isShielded ? "(AFK-SCUDATO)" : "(AFK)");

            string color = isShielded ? "#00FFFF" : "#FFA500";
            return $"<color={color}>{suffix}</color>";
        }

        private static void RestoreOriginalName(PlayerControl pc, Data data)
        {
            if (pc != null && data != null && pc.Data != null)
            {
                if (pc.Data.PlayerName != data.OriginalName)
                {
                    pc.RpcSetName(data.OriginalName);
                }
            }
        }

        public static void EnsureTrackedPlayers()
        {
            foreach (var player in BanMod.AllAlivePlayerControls)
            {
                if (!PlayerData.ContainsKey(player.PlayerId))
                {
                    RecordPosition(player);
                }
            }
        }

        public class Data
        {
            public enum Phase
            {
                Detection,
                Warning,
                Consequence
            }

            public Vector2 LastPosition { get; init; }
            public float Timer { get; set; }
            public Phase CurrentPhase { get; set; }
            public string OriginalName { get; init; }
        }
    }
}