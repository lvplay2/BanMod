using AmongUs.Data;
using AmongUs.Data.Player;
using AmongUs.GameOptions;
using AmongUs.InnerNet.GameDataMessages;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Hazel;
using Il2CppInterop.Runtime;
using Il2CppSystem.Diagnostics;
using Il2CppSystem.IO.Ports;
using InnerNet;
using MonoMod.Cil;
using MS.Internal.Xml.XPath;
using Rewired.Utils.Platforms.Windows;
using Sentry.Protocol;
using Sentry.Unity.NativeUtils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements.UIR;
using static BanMod.ChatCommands;
using static BanMod.SpamManager;
using static BanMod.Scientist;
using static BanMod.Translator;
using static BanMod.Utils;
using static BanMod.ExtendedPlayerControl;
using static FilterPopUp.FilterInfoUI;
using static Il2CppMono.Security.X509.X520;
using static InnerNet.InnerNetClient;
using static PlayerOutfit;
using static Rewired.Utils.Classes.Utility.ObjectInstanceTracker;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.ProBuilder.AutoUnwrapSettings;
using static UnityEngine.UIElements.UIR.Allocator2D;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;
using StackFrame = System.Diagnostics.StackFrame;
using StackTrace = System.Diagnostics.StackTrace;
using Timer = System.Timers.Timer;
using Vector2 = UnityEngine.Vector2;
using Random = UnityEngine.Random;

namespace BanMod;
public static class Utils
{
    public static string RemoveHtmlTags(this string str) => Regex.Replace(str, "<[^>]*?>", string.Empty);
    public static string ColorString(Color32 color, string str) => $"<color=#{color.r:x2}{color.g:x2}{color.b:x2}{color.a:x2}>{str}</color>";
    private static readonly Dictionary<string, Sprite> CachedSprites = [];
    private static readonly DateTime TimeStampStartTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public static NetworkedPlayerInfo GetPlayerInfoById(int PlayerId) =>
       GameData.Instance.AllPlayers.ToArray().FirstOrDefault(info => info.PlayerId == PlayerId);
    public static IEnumerator TeleportKill(PlayerControl killer, PlayerControl target)
    {
        Vector2 killerOriginalPosition = killer.transform.position;
        Vector2 teleportPosition = new Vector2(-1.0f, 1.0f);
        target.RpcTeleport(teleportPosition);
        killer.RpcMurderPlayer(target, true);
        yield return new WaitForSeconds(0.05f); 
        killer.RpcTeleport(killerOriginalPosition);
        killerOriginalPosition = Vector2.zero;
    }
    public static int GetCurrentLobbyPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < GameData.Instance.PlayerCount; i++)
        {
            var player = GameData.Instance.AllPlayers[i];
            if (player != null && !player.Disconnected)
            {
                count++;
            }
        }
        return count;
    }
    public static string GetRegionName(IRegionInfo region = null)
    {
        region ??= ServerManager.Instance.CurrentRegion;

        string name = region.Name;

        if (AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            name = "Local Games";
            return name;
        }

        if (region.PingServer.EndsWith("among.us", StringComparison.Ordinal))
        {
            // Official server
            if (name == "North America") name = "NA";
            else if (name == "Europe") name = "EU";
            else if (name == "Asia") name = "AS";

            return name;
        }
        return name;
    }
    
    public static class LanguageUtils
    {
        public static string GetLanguageName(IGameOptions options)
        {
            if (options == null) return "Unknown";

            GameKeywords keywords = (GameKeywords)options.Keywords;

            if (keywords.HasFlag(GameKeywords.English)) return "English";
            if (keywords.HasFlag(GameKeywords.Italian)) return "Italian";
            if (keywords.HasFlag(GameKeywords.French)) return "French";
            if (keywords.HasFlag(GameKeywords.German)) return "German";
            if (keywords.HasFlag(GameKeywords.SpanishLA)) return "Spanish (LA)";
            if (keywords.HasFlag(GameKeywords.SpanishEU)) return "Spanish (EU)";
            if (keywords.HasFlag(GameKeywords.Brazilian)) return "Brazilian Portuguese";
            if (keywords.HasFlag(GameKeywords.Portuguese)) return "Portuguese";
            if (keywords.HasFlag(GameKeywords.Korean)) return "Korean";
            if (keywords.HasFlag(GameKeywords.Russian)) return "Russian";
            if (keywords.HasFlag(GameKeywords.Dutch)) return "Dutch";
            if (keywords.HasFlag(GameKeywords.Filipino)) return "Filipino";
            if (keywords.HasFlag(GameKeywords.Japanese)) return "Japanese";
            if (keywords.HasFlag(GameKeywords.Arabic)) return "Arabic";
            if (keywords.HasFlag(GameKeywords.Polish)) return "Polish";
            if (keywords.HasFlag(GameKeywords.SChinese)) return "Simplified Chinese";
            if (keywords.HasFlag(GameKeywords.TChinese)) return "Traditional Chinese";
            if (keywords.HasFlag(GameKeywords.Irish)) return "Irish";

            if (keywords == GameKeywords.All) return "All";
            if (keywords == GameKeywords.Other) return "Other";

            return "Unknown";
        }

        // Per comodità, per ottenere l'IGameOptions corrente da GameOptionsManager
        public static IGameOptions GetCurrentGameOptions()
        {
            // Preferisco usare GameHostOptions se host, altrimenti GameSearchOptions
            if (GameOptionsManager.Instance == null)
                return null;

            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            {
                return GameOptionsManager.Instance.GameHostOptions;
            }
            else
            {
                return GameOptionsManager.Instance.GameSearchOptions;
            }
        }
    }
    public class ButtonData
    {
        public string Title { get; set; } // Il testo che apparirà sul pulsante
        public string Message { get; set; } // Il messaggio da inviare quando cliccato
    }
    public static string ColorString(UnityEngine.Color c, string s)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{s}</color>";
    }
    public static void KillPlayer(PlayerControl target)
    {
        if (target == null || target.Data.IsDead) return;

        PlayerControl killer = PlayerControl.LocalPlayer;
        
        if (killer != null && AmongUsClient.Instance.AmHost)
        {
            killer.StartCoroutine(KillPlayer1(killer, target));

        }

    }

    private static IEnumerator KillPlayer1(PlayerControl killer, PlayerControl target)
    {

        Vector2 killerOriginalPosition1 = killer.transform.position;
        killer.MurderPlayer(target, MurderResultFlags.Succeeded);
        yield return new WaitForSeconds(0.1f); // Ritardo
        killer.RpcTeleport(killerOriginalPosition1);
        killerOriginalPosition1 = Vector2.zero;
        if (!BanMod.UnreportableBodies.Contains(target.PlayerId))
        {
            BanMod.UnreportableBodies.Add(target.PlayerId);
            Logger.Info($"Body of {target.PlayerId} is now unreportable", "KillPlayer");
        }
    }

    public static class NameNormalizer
    {
        public static readonly Dictionary<char, char> SpecialCharMap = new Dictionary<char, char>
    {
        { '卂', 'A' }, { '卄', 'H' }, { '卩', 'P' },
        { '乃', 'B' }, { '丨', 'I' }, { 'Ɋ', 'Q' },
        { '匚', 'C' }, { 'ㄥ', 'L' }, { '尺', 'R' },
        { 'ᗪ', 'D' }, { '爪', 'M' }, { '丂', 'S' },
        { '乇', 'E' }, { '几', 'N' }, { 'ᐯ', 'V' },
        { '千', 'F' }, { 'ㄒ', 'T' }, { '乙', 'Z' },
        { 'Ꮆ', 'G' }, { 'ㄖ', 'O' }, { 'ㄩ', 'U' },

        { '4', 'a' }, { '3', 'e' }, { '5', 's' },
        { '1', 'i' }, { '7', 't' }, { '0', 'o' },

        { 'Ö', 'O' }, { 'Ø', 'O' }, { 'ö', 'o' }, { 'ø', 'o' },
        { 'à', 'a' }, { 'á', 'a' }, { 'â', 'a' }, { 'ã', 'a' },
        { 'è', 'e' }, { 'é', 'e' }, { 'ê', 'e' }, { 'ë', 'e' },
        { 'ì', 'i' }, { 'í', 'i' }, { 'î', 'i' }, { 'ï', 'i' },
        { 'ò', 'o' }, { 'ó', 'o' }, { 'ô', 'o' }, { 'õ', 'o' }, 
        { 'ù', 'u' }, { 'ú', 'u' }, { 'û', 'u' }, { 'ü', 'u' },
        { 'ç', 'c' }, { 'ñ', 'n' }, { 'ﾑ', 'a' }, { 'ﾉ', 'i' },
        { 'À', 'A' }, { 'Á', 'A' }, { 'Â', 'A' }, { 'Ã', 'A' },
        { 'È', 'E' }, { 'É', 'E' }, { 'Ê', 'E' }, { 'Ë', 'E' },
        { 'Ì', 'I' }, { 'Í', 'I' }, { 'Î', 'I' }, { 'Ï', 'I' },
        { 'Ò', 'O' }, { 'Ó', 'O' }, { 'Ô', 'O' }, { 'Õ', 'O' }, 
        { 'Ù', 'U' }, { 'Ú', 'U' }, { 'Û', 'U' }, { 'Ü', 'U' },
        { 'Ç', 'C' }, { 'Ñ', 'N' }, { 'ä', 'a' }, { 'å', 'a' },

        { 'а', 'a' }, { 'б', 'b' }, { 'в', 'v' }, { 'г', 'g' }, { 'д', 'd' }, { 'е', 'e' }, { 'ё', 'e' },
        { 'з', 'z' }, { 'и', 'i' }, { 'й', 'y' }, { 'к', 'k' }, { 'л', 'l' }, { 'м', 'm' },
        { 'н', 'n' }, { 'о', 'o' }, { 'п', 'p' }, { 'р', 'r' }, { 'с', 's' }, { 'т', 't' }, { 'у', 'u' },
        { 'ф', 'f' }, { 'х', 'h' }, { 'Ä', 'A' }, { 'Å', 'A' }, { 'ы', 'y' }, { 'э', 'e' },
        { 'А', 'A' }, { 'Б', 'B' }, { 'В', 'V' }, { 'Г', 'G' }, { 'Д', 'D' }, { 'Е', 'E' }, { 'Ё', 'E' },
        { 'З', 'Z' }, { 'И', 'I' }, { 'Й', 'Y' }, { 'К', 'K' }, { 'Л', 'L' }, { 'М', 'M' },
        { 'Н', 'N' }, { 'О', 'O' }, { 'П', 'P' }, { 'Р', 'R' }, { 'С', 'S' }, { 'Т', 'T' }, { 'У', 'U' },
        { 'Ф', 'F' }, { 'Х', 'H' },
        { 'Ы', 'Y' }, { 'Э', 'E' },

        { 'Ð', 'D' }, { 'ð', 'd' }
    };

        public static string NormalizeInputName(string inputName)
        {
            if (string.IsNullOrEmpty(inputName))
            {
                return inputName;
            }

            // Usiamo un StringBuilder per efficienza se i nomi sono lunghi o ci sono molte modifiche
            System.Text.StringBuilder sb = new System.Text.StringBuilder(inputName.Length);

            foreach (char c in inputName)
            {
                if (SpecialCharMap.TryGetValue(c, out char normalizedChar))
                {
                    sb.Append(normalizedChar);
                }
                else if (char.IsLetterOrDigit(c)) // Mantieni lettere e numeri
                {
                    sb.Append(char.ToLowerInvariant(c)); // Converti in minuscolo per ricerca case-insensitive
                }
                // Ignora tutti gli altri caratteri speciali o simboli che non sono mappati
            }
            return sb.ToString();
        }
    }
    public static Sprite CreateSprite(Color color, int width = 64, int height = 64)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        texture.SetPixels(pixels);
        texture.Apply();

        Rect rect = new Rect(0, 0, texture.width, texture.height);
        Vector2 pivot = new Vector2(0.5f, 0.5f);
        return Sprite.Create(texture, rect, pivot);
    }
    public static IEnumerator KillAsPlayer(PlayerControl targetToKill, PlayerControl disguiseTarget)
    {
        var self = PlayerControl.LocalPlayer;

        if (self == null || targetToKill == null || disguiseTarget == null || disguiseTarget.Data?.DefaultOutfit == null)
            yield break;

        var myOutfit = self.Data?.DefaultOutfit;
        if (myOutfit == null) yield break;

        var disguiseOutfit = disguiseTarget.Data.DefaultOutfit;

        // Backup outfit manualmente
        var originalOutfit = new NetworkedPlayerInfo.PlayerOutfit()
        {
            ColorId = myOutfit.ColorId,
            HatId = myOutfit.HatId,
            SkinId = myOutfit.SkinId,
            VisorId = myOutfit.VisorId,
            PetId = myOutfit.PetId,
            NamePlateId = myOutfit.NamePlateId
        };

        string originalName = self.Data.PlayerName;

        // Applica l'aspetto dell'altro
        self.RpcSetColor((byte)disguiseOutfit.ColorId);
        self.RpcSetHat(disguiseOutfit.HatId);
        self.RpcSetSkin(disguiseOutfit.SkinId);
        self.RpcSetVisor(disguiseOutfit.VisorId);
        self.RpcSetName(disguiseTarget.Data.PlayerName);
        self.RpcSetPet(disguiseOutfit.PetId);

        // Delay per propagazione visiva
        yield return new WaitForSeconds(0.2f);

        // Kill se siamo host
        if (AmongUsClient.Instance.AmHost)
        {
            Utils.KillPlayer(targetToKill);
        }

        yield return new WaitForSeconds(0.3f);

        // Ripristina aspetto originale
        self.RpcSetName(originalName);
        self.RpcSetColor((byte)originalOutfit.ColorId);
        self.RpcSetHat(originalOutfit.HatId);
        self.RpcSetSkin(originalOutfit.SkinId);
        self.RpcSetVisor(originalOutfit.VisorId);
        self.RpcSetPet(originalOutfit.PetId);
    }
    private static Stack<NetworkedPlayerInfo.PlayerOutfit> savedOutfits = new Stack<NetworkedPlayerInfo.PlayerOutfit>();
    private static Stack<string> savedNames = new Stack<string>();

    public static class OutfitManager
    {
        public static Stack<NetworkedPlayerInfo.PlayerOutfit> savedOutfits = new Stack<NetworkedPlayerInfo.PlayerOutfit>();
        public static Stack<string> savedNames = new Stack<string>();
        public static Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> OriginalPlayerOutfits = new Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit>();
        public static Dictionary<byte, string> OriginalPlayerNames = new Dictionary<byte, string>();

        // Metodo per applicare un outfit *specifico* a se stessi (usato per il killer che diventa vittima dopo la kill)
        public static void ApplySpecificOutfitToSelf(PlayerControl self, NetworkedPlayerInfo.PlayerOutfit targetOutfit, string targetName)
        {
            if (self == null || targetOutfit == null) return;

            // Salva l'outfit CORRENTE del self (il killer) per un eventuale futuro ripristino con RevertKillerOutfit (stack).
            // Questo è rilevante se il killer deve tornare al suo outfit originale più tardi nel round.
            var myOutfit = self.Data?.DefaultOutfit;
            if (myOutfit != null)
            {
                var currentOutfit = new NetworkedPlayerInfo.PlayerOutfit()
                {
                    ColorId = myOutfit.ColorId,
                    HatId = myOutfit.HatId,
                    SkinId = myOutfit.SkinId,
                    VisorId = myOutfit.VisorId,
                    PetId = myOutfit.PetId,
                    NamePlateId = myOutfit.NamePlateId
                };
                savedOutfits.Push(currentOutfit);
                savedNames.Push(self.Data.PlayerName ?? string.Empty);
                Logger.Info($"Saved current outfit for killer {self.PlayerId} for doppelganger revert.", "Doppelganger");
            }
            else
            {
                Logger.Info($"Could not save current outfit for killer {self.PlayerId} as DefaultOutfit was null.", "Doppelganger");
            }

            self.RpcSetColor((byte)targetOutfit.ColorId);
            self.RpcSetHat(targetOutfit.HatId);
            self.RpcSetSkin(targetOutfit.SkinId);
            self.RpcSetVisor(targetOutfit.VisorId);
            self.RpcSetName(targetName); // Usa il nome passato
            self.RpcSetPet(targetOutfit.PetId);
            self.RpcSetNamePlate(targetOutfit.NamePlateId); // Imposta anche il NamePlate
            Logger.Info($"Applied specific outfit (original target's) to self (killer {self.PlayerId}).", "Doppelganger");
        }

        // Metodo per applicare un outfit *specifico* a un target (usato per la vittima che diventa killer prima della kill)
        public static void ApplySpecificOutfitToTarget(PlayerControl target, NetworkedPlayerInfo.PlayerOutfit sourceOutfit, string sourceName)
        {
            if (target == null || sourceOutfit == null) return;

            Logger.Info($"Attempting to apply specific outfit for {sourceName} to target {target.PlayerId}. Color: {(byte)sourceOutfit.ColorId}, Hat: {sourceOutfit.HatId}, Skin: {sourceOutfit.SkinId}, Visor: {sourceOutfit.VisorId}, Pet: {sourceOutfit.PetId}, NamePlate: {sourceOutfit.NamePlateId}", "DoppelgangerDebug");

            target.RpcSetColor((byte)sourceOutfit.ColorId);
            target.RpcSetHat(sourceOutfit.HatId);
            target.RpcSetSkin(sourceOutfit.SkinId);
            target.RpcSetVisor(sourceOutfit.VisorId);
            target.RpcSetName(sourceName); // Usa il nome passato
            target.RpcSetPet(sourceOutfit.PetId);
            target.RpcSetNamePlate(sourceOutfit.NamePlateId); // Imposta anche il NamePlate
            Logger.Info($"Applied specific outfit (original killer's) to target {target.PlayerId}.", "Doppelganger");
        }

        public static void RevertKillerOutfit(PlayerControl self)
        {
            if (self == null) return;
            if (savedOutfits.Count > 0 && savedNames.Count > 0)
            {
                var previousOutfit = savedOutfits.Pop();
                var previousName = savedNames.Pop();
                self.RpcSetName(previousName);
                self.RpcSetColor((byte)previousOutfit.ColorId);
                self.RpcSetHat(previousOutfit.HatId);
                self.RpcSetSkin(previousOutfit.SkinId);
                self.RpcSetVisor(previousOutfit.VisorId);
                self.RpcSetPet(previousOutfit.PetId);
                self.RpcSetNamePlate(previousOutfit.NamePlateId); // Assicurati di ripristinare anche il NamePlate
                Logger.Info($"Reverted killer {self.PlayerId} to previous outfit (specific doppelganger).", "Doppelganger");
            }
            else
            {
                Logger.Info("Nessun outfit precedente specifico da ripristinare per il killer.", "Doppelganger");
            }
        }
    }



    public static void SendInfo()
    {
        // Carico il template così com'è, senza formattazione
        string title = TemplateLoader.LoadTemplate("InfoTemplate");
        Utils.SendMessage("", 255, title); // Manda a tutti
        MessageBlocker.UpdateLastMessageTime();
    }

    public static void SendWelcome(PlayerControl player)
    {
        string name = player.GetRealName() ?? player.name ?? "Player";
        string title = TemplateLoader.FormatTemplate("WelcomeTemplate", name);
        Utils.SendMessage("", player.PlayerId, title);
        MessageBlocker.UpdateLastMessageTime();
    }

    public static readonly Dictionary<byte, byte> ActiveProtections = new(); // targetId -> protectorId

    public static PlayerControl SelectRandomGenericProtector()
    {
        if (!AmongUsClient.Instance.AmHost) return null;

        var allPlayers = BanMod.AllPlayerControls
            .Where(p => p != null && p.Data != null)
            .ToList();

        PlayerControl hostPlayer = PlayerControl.LocalPlayer; // Questo è l'host

        if (hostPlayer.Data.IsDead && !Utils.Angel(hostPlayer))
        {
            Logger.Info("[SelectProtector] Host morto e non angelo scelto come protettore.");
            return hostPlayer;
        }

        var deadNonAngelPlayers = allPlayers
            .Where(p => p.Data.IsDead && !Utils.Angel(p) && p.PlayerId != hostPlayer.PlayerId) // Escludi l'host se già considerato
            .ToList();

        if (deadNonAngelPlayers.Any())
        {
            System.Random rand = new System.Random();
            PlayerControl selected = deadNonAngelPlayers[rand.Next(deadNonAngelPlayers.Count)];
            Logger.Info($"[SelectProtector] Scelto protettore generico morto (non angelo): {selected.Data.PlayerName} (ID: {selected.PlayerId}).");
            return selected;
        }

        var liveGenericPlayers = allPlayers
            .Where(p => !p.Data.IsDead &&
                        !Utils.Scientist(p) &&
                        !Utils.Engineer(p) &&
                        !Utils.Angel(p) &&
                        !Utils.Tracker(p) &&
                        !Utils.Impostor(p) && // Escludi impostori vivi
                        !Utils.Phantom(p) &&
                        !Utils.Shapeshifter(p))
            .ToList();

        if (liveGenericPlayers.Any())
        {
            System.Random rand = new System.Random();
            PlayerControl selected = liveGenericPlayers[rand.Next(liveGenericPlayers.Count)];
            Logger.Info($"[SelectProtector] Scelto protettore generico vivo (non ruolo speciale): {selected.Data.PlayerName} (ID: {selected.PlayerId}).");
            return selected;
        }

        // --- Priorità 4: Impostore morto (se esiste) ---
        var deadPriority4Players = allPlayers
            .Where(p => p.Data.IsDead &&
                        (Utils.Impostor(p) || Utils.Phantom(p) || Utils.Shapeshifter(p)))
            .ToList();

        if (deadPriority4Players.Any())
        {
            System.Random rand = new System.Random();
            PlayerControl selected = deadPriority4Players[rand.Next(deadPriority4Players.Count)];
            Logger.Info($"[SelectProtector] Scelto protettore da Impostore morto, Phantom morto o Shapeshifter morto: {selected.Data.PlayerName} (ID: {selected.PlayerId}).");
            return selected;
        }

        // --- Priorità 5: Se non trova nessuno, si autoprotegge il target ---
        Logger.Info("[SelectProtector] Nessun protettore idoneo trovato secondo le priorità. Il target si auto-proteggerà.");
        return null; // Il chiamante (ForceProtect) gestirà l'auto-protezione se ritorna null
    }

    public static Dictionary<byte, float> ActiveProtectionsTimestamps = new Dictionary<byte, float>();
    public static void ForceProtect(PlayerControl target, bool overrideExisting = true)
    {
        if (target == null || target.Data.IsDead)
        {
            return;
        }

        if (!BanMod.ShieldedPlayers.Contains(target.PlayerId))
        {
            BanMod.ShieldedPlayers.Add(target.PlayerId);
            Logger.Info($"[ForceProtect] Aggiunto {target.Data.PlayerName} (ID: {target.PlayerId}) a BanMod.ShieldedPlayers.");
        }

        PlayerControl actualProtector = SelectRandomGenericProtector();
        if (actualProtector == null)
        {
            actualProtector = target;
            Logger.Info($"[ForceProtect] Nessun protettore esterno trovato. {target.Data.PlayerName} (ID: {target.PlayerId}) si auto-proteggerà.");
        }

        if (AmongUsClient.Instance.AmHost)
        {
            // ✅ Nuovo controllo: se lo scudo è ancora attivo, non fare nulla
            if (!overrideExisting &&
                Utils.ActiveProtections.TryGetValue(target.PlayerId, out var currentProtectorId) &&
                Utils.ActiveProtectionsTimestamps.TryGetValue(target.PlayerId, out var expiresAt) &&
                Time.time < expiresAt)
            {
                Logger.Info($"[ForceProtect] Scudo per {target.PlayerId} ancora attivo fino a {expiresAt:F2}. Skip RPC.");
                return;
            }

            actualProtector.RpcProtectPlayer(target, actualProtector.Data.DefaultOutfit.ColorId);
            Utils.ActiveProtections[target.PlayerId] = actualProtector.PlayerId;

            // Aggiorna il timestamp di scadenza (es: 30 secondi da ora)
            Utils.ActiveProtectionsTimestamps[target.PlayerId] = Time.time + 30f;

            Logger.Info($"[AFKShield] Protezione applicata a {target.Data.PlayerName} (ID: {target.PlayerId}) da {actualProtector.Data.PlayerName} (ID: {actualProtector.PlayerId}) ({(actualProtector == target ? "Auto-protezione" : "Protettore generico")})");
        }
    }
    public static class ProtectionManager
    {
        private static float _protectionReapplyTimer = 0f;
        private const float ProtectionReapplyInterval = 5f; // Controlla ogni 5 secondi

        public static void PeriodicReapplyProtection()
        {
            if (!AmongUsClient.Instance.AmHost) return;

            _protectionReapplyTimer -= Time.fixedDeltaTime;
            if (_protectionReapplyTimer > 0f) return;

            _protectionReapplyTimer = ProtectionReapplyInterval;

            var playersToShield = BanMod.ShieldedPlayers.ToList();

            foreach (byte playerId in playersToShield)
            {
                PlayerControl player = BanMod.AllPlayerControls.FirstOrDefault(p => p.PlayerId == playerId);

                if (player != null && !player.Data.IsDead)
                {
                    // ✅ Se NON ha lo scudo (guardianId == -1), lo riapplichi
                    if (player.protectedByGuardianId == -1)
                    {
                        Utils.ForceProtect(player, overrideExisting: true);
                        // Logger.Info($"[ProtectionManager] Scudo riapplicato a {player.Data.PlayerName} (ID: {player.PlayerId})");
                    }
                    // Altrimenti non fare nulla: lo scudo è ancora attivo
                }
                else
                {
                    // Rimuovi da ShieldedPlayers e protezioni attive
                    BanMod.ShieldedPlayers.Remove(playerId);
                    Utils.ActiveProtections.Remove(playerId);
                    // Logger.Info($"[ProtectionManager] Rimosso player {playerId} (non trovato o morto)");
                }
            }
        }
    }

    public static void RemovePlayerFromShield(PlayerControl player)
    {
        if (player == null) return;

        if (ActiveProtections.ContainsKey(player.PlayerId))
        {
            ActiveProtections.Remove(player.PlayerId);
            //Logger.Info($"[Utils] Rimosso {player.name} ({player.PlayerId}) da ActiveProtections.");
        }

        if (BanMod.ShieldedPlayers.Contains(player.PlayerId))
        {
            BanMod.ShieldedPlayers.Remove(player.PlayerId);
           // Logger.Info($"[Utils] Rimosso {player.name} ({player.PlayerId}) da BanMod.ShieldedPlayers.");
        }
    }
    public static bool ShieldRemovedAfterMeeting = false;
    public static byte? PrevFirstDeadPlayerId = null;

    public static void CloseMeeting()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        List<MeetingHud.VoterState> statesList = [];
        MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), null, true);
        MeetingHud.Instance.Close();
        MeetingHud.Instance.RpcClose();
        GuessManager.CleanupAfterMeeting();
    }
    public static IEnumerator DelayedCloseMeeting()
    {
        // Attendi che il meeting sia visibile e attivo (non solo istanziato)
        while (MeetingHud.Instance == null || !MeetingHud.Instance.gameObject.activeInHierarchy)
            yield return null;

        // Attendi almeno un frame dopo che è attivo
        yield return null;

        // Attendi un minimo tempo per sicurezza (facoltativo)
        yield return new WaitForSeconds(0.1f);

        CloseMeeting();
    }

    public static void SendModdedHandshake()
    {
        if (PlayerControl.LocalPlayer == null)
        {
            Logger.Info("LocalPlayer è null, handshake non inviato!");
            return;
        }

        var sender = CustomRpcSender.Create("ModdedHandshakeSender", SendOption.Reliable);
        sender.StartMessage(-1)
              .StartRpc(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ModdedHandshake)
              .Write($"BanMod {BanMod.PluginVersion}") // o una stringa identificativa unica
              .EndRpc()
              .SendMessage();

        Logger.Info("Handshake moddato inviato con successo!", "ModdedHandshake");
    }

    public static class TemplateLoader
    {
        private static readonly string TemplatesFolder = "./BAN_DATA/TEMPLATE";
        // Cache: nome template -> contenuto
        private static readonly Dictionary<string, string> CachedTemplates = new Dictionary<string, string>();

        // Carica un template specifico
        public static string LoadTemplate(string templateName)
        {
            string filePath = Path.Combine(TemplatesFolder, templateName + ".txt");

            if (!File.Exists(filePath))
            {
                Directory.CreateDirectory(TemplatesFolder);
                // Creo un template di default per il nome specificato (puoi personalizzarlo)
                string defaultContent = $"<color=#00ff00><b>Default {templateName} Template</b></color> <color=#007fff>{{player}}</color>";
                File.WriteAllText(filePath, defaultContent);
            }

            string content = File.ReadAllText(filePath);
            content = content.Replace("\\n", "\n");

            // Aggiorno la cache
            CachedTemplates[templateName] = content;

            return content;
        }

        // Formatta un template specifico con il nome del giocatore
        public static string FormatTemplate(string templateName, string playerName)
        {
            if (!CachedTemplates.ContainsKey(templateName) || string.IsNullOrEmpty(CachedTemplates[templateName]))
            {
                LoadTemplate(templateName);
            }

            string template = CachedTemplates[templateName];

            if (string.IsNullOrWhiteSpace(template))
            {
                // Messaggio di default nel caso il template fosse vuoto
                return $"<color=#007fff>WELCOME </color><color=#8f00ff>{{player}}</color>\\nThis Lobby is MODDED and offers some customizations and role modifications\\n \r\n<color=#ff0000> Insults and \"toxic\" behavior are forbidden here!\\n</color> (Insults, teaming, and cheating will result in an immediate ban from the lobby)\\n \r\nTo see the commands, type /help and for info about your role, type /m (in-game only)\r\n";
            }

            return template.Replace("{player}", playerName);
        }
    }



    public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
    {
        try
        {
            if (CachedSprites.TryGetValue(path + pixelsPerUnit, out var sprite)) return sprite;
            Texture2D texture = LoadTextureFromResources(path);
            sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f), pixelsPerUnit);
            sprite.hideFlags |= HideFlags.HideAndDontSave | HideFlags.DontSaveInEditor;
            return CachedSprites[path + pixelsPerUnit] = sprite;
        }
        catch
        {
            Logger.Error($"Error loading texture from: {path}", "LoadImage");
        }
        return null;
    }
    public static Texture2D LoadTextureFromResources(string path)
    {
        try
        {
            var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            using MemoryStream ms = new();
            stream?.CopyTo(ms);
            texture.LoadImage(ms.ToArray(), false);
            return texture;
        }
        catch
        {
            Logger.Error($"读入Texture失败：{path}", "LoadImage");
        }
        return null;
    }

    public static void ShowCommand()
    {
        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (
            Translator.GetString("CommandList")
            + $"\n  ○ <color=#FF0000><b>/dn (name)</b></color> {Translator.GetString("Command.dn")}"
            + $"\n  ○ <color=#FF0000><b>/ddn (name)</b></color> {Translator.GetString("Command.ddn")}"
            + $"\n  ○ <color=#FF0000><b>/dw (word)</b></color> {Translator.GetString("Command.dw")}"
            + $"\n  ○ <color=#FF0000><b>/ddw (word)</b></color> {Translator.GetString("Command.ddw")}"
            + $"\n  ○ <color=#FF0000><b>/ds (start)</b></color> {Translator.GetString("Command.ds")}"
            + $"\n  ○ <color=#FF0000><b>/dds (start)</b></color> {Translator.GetString("Command.dds")}"
            + $"\n  ○ <color=#FF0000><b>/f (id)</b></color> {Translator.GetString("Command.f")}"
            + $"\n  ○ <color=#FF0000><b>/df (id)</b></color> {Translator.GetString("Command.df")}"
            ));

    }
    public static void ShowCommand2()
    {
        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (
            Translator.GetString("CommandList")
            + $"\n  ○ <color=#FF0000><b>/sn</b></color> {Translator.GetString("Command.storename")}"
            + $"\n  ○ <color=#FF0000><b>/rn</b></color> {Translator.GetString("Command.restorename")}"
            + $"\n  ○ <color=#FF0000><b>/cn</b></color> {Translator.GetString("Command.clearname")}"
            + $"\n  ○ <color=#FF0000><b>/mn</b></color> {Translator.GetString("Command.moddedname")}"
            + $"\n  ○ <color=#FF0000><b>/snall</b></color> {Translator.GetString("Command.storenameall")}"
            + $"\n  ○ <color=#FF0000><b>/rnall</b></color> {Translator.GetString("Command.restorenameall")}"
            + $"\n  ○ <color=#FF0000><b>/cnall</b></color> {Translator.GetString("Command.clearnameall")}"
            + $"\n  ○ <color=#FF0000><b>/mnall</b></color> {Translator.GetString("Command.moddednameall")}"
            ));

    }
    public static void ShowCommand3()
    {
        DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (
            Translator.GetString("CommandList")
            + $"\n  ○ <color=#FF0000><b>/level (num)</b></color> {Translator.GetString("Command.level")}"
            + $"\n  ○ <color=#FF0000><b>/n (color)</b></color> {Translator.GetString("Command.n")}"
            + $"\n  ○ <color=#FF0000><b>/s (symbol)</b></color> {Translator.GetString("Command.s")}"
            + $"\n  ○ <color=#FF0000><b>/d (symbol)</b></color> {Translator.GetString("Command.d")}"
            + $"\n  ○ <color=#FF0000><b>/rename (name)</b></color> {Translator.GetString("Command.rn")}"
            + $"\n  ○ <color=#FF0000><b>/reset</b></color> {Translator.GetString("Command.rs")}"
            + $"\n  ○ <color=#FF0000><b>/color (color)</b></color> {Translator.GetString("Command.color")}"
            + $"\n  ○ <color=#FF0000><b>/id</b></color> {Translator.GetString("Command.id")}"
            + $"\n  ○ <color=#FF0000><b>/m</b></color> {Translator.GetString("Command.role")}"
            + $"\n  ○ <color=#FF0000><b>/bm (playercolor)</b></color> {Translator.GetString("Command.bm")}"
            + $"\n  ○ <color=#FF0000><b>/role</b></color> {Translator.GetString("Command.roles")}"
            + $"\n  ○ <color=#FF0000><b>/all</b></color> {Translator.GetString("Command.all")}"
            ));

    }
    public static void ShowCommandClient()
    {
        Utils.SendMessage ("", 255,Translator.GetString("CommandList")
            + $"\n  ○ <color=#FF0000><b>/n (color)</b></color> {Translator.GetString("Command.n")}"
            + $"\n  ○ <color=#FF0000><b>/s (symbol)</b></color> {Translator.GetString("Command.s")}"
            + $"\n  ○ <color=#FF0000><b>/d (symbol)</b></color> {Translator.GetString("Command.d")}"
            + $"\n  ○ <color=#FF0000><b>/rename</b></color> {Translator.GetString("Command.rn")}"
            + $"\n  ○ <color=#FF0000><b>/reset</b></color> {Translator.GetString("Command.rs")}"
            + $"\n  ○ <color=#FF0000><b>/color (color)</b></color> {Translator.GetString("Command.color")}"
            + $"\n  ○ <color=#FF0000><b>/id</b></color> {Translator.GetString("Command.id")}"
            + $"\n  ○ <color=#FF0000><b>/m</b></color> {Translator.GetString("Command.role")}"
            + $"\n  ○ <color=#FF0000><b>/bm (playercolor)</b></color> {Translator.GetString("Command.bm")}"
            );
        MessageBlocker.UpdateLastMessageTime();

    }
    public static void ShowRoleClient()
    {
        string msg =
            GetString("TabGroup.Phantom") + "\n" +
            GetString("TabGroup.Engineer") + "\n" +
            GetString("TabGroup.Immortal") + "\n" +
            GetString("TabGroup.Scientist") + "\n" +
            GetString("TabGroup.Exiler") + "\n" +
            GetString("TabGroup.Guesser") + "\n" +
            GetString("TabGroup.Info"); 
        Utils.SendMessage(msg, 255, Translator.GetString("RoleList"));
        MessageBlocker.UpdateLastMessageTime();

    }
    public static readonly Dictionary<string, Vector2> DevicePos = new()
    {
        ["SkeldAdmin"] = new(3.48f, -8.62f),
        ["SkeldCamera"] = new(-13.06f, -2.45f),
        ["MiraHQAdmin"] = new(21.02f, 19.09f),
        ["MiraHQDoorLog"] = new(16.22f, 5.82f),
        ["PolusLeftAdmin"] = new(22.80f, -21.52f),
        ["PolusRightAdmin"] = new(24.66f, -21.52f),
        ["PolusCamera"] = new(2.96f, -12.74f),
        ["PolusVital"] = new(26.70f, -15.94f),
        ["DleksAdmin"] = new(-3.48f, -8.62f),
        ["DleksCamera"] = new(13.06f, -2.45f),
        ["AirshipCockpitAdmin"] = new(-22.32f, 0.91f),
        ["AirshipRecordsAdmin"] = new(19.89f, 12.60f),
        ["AirshipCamera"] = new(8.10f, -9.63f),
        ["AirshipVital"] = new(25.24f, -7.94f),
        ["FungleCamera"] = new(6.20f, 0.10f),
        ["FungleVital"] = new(-2.50f, -9.80f)
    };
    public static readonly Dictionary<string, string> colorMap = new()
{
    { "white", "#FFFFFF" }, { "bianco", "#FFFFFF" }, { "weiß", "#FFFFFF" }, { "белый", "#FFFFFF" }, { "blanc", "#FFFFFF" },
    { "blu", "#0000FF" }, { "blue", "#0000FF" }, { "blau", "#0000FF" }, { "синий", "#0000FF" }, { "bleu", "#0000FF" },
    { "verde", "#00FF00" }, { "green", "#00FF00" }, { "grün", "#00FF00" }, { "зелёный", "#00FF00" }, { "vert", "#00FF00" },
    { "fucsia", "#FF00FF" }, { "fuchsia", "#FF00FF" }, { "fuchsie", "#FF00FF" }, { "фуксия", "#FF00FF" }, { "pink", "#FFC0CB" },
    { "arancio", "#FFA500" }, { "arancione", "#FFA500" }, { "orange", "#FFA500" }, { "оранжевый", "#FFA500" },
    { "giallo", "#FFFF00" }, { "gialla", "#FFFF00" }, { "yellow", "#FFFF00" }, { "gelb", "#FFFF00" }, { "жёлтый", "#FFFF00" }, { "jaune", "#FFFF00" },
    { "nero", "#000000" }, { "nera", "#000000" }, { "black", "#000000" }, { "schwarz", "#000000" }, { "чёрный", "#000000" }, { "noir", "#000000" },
    { "viola", "#800080" }, { "purple", "#800080" }, { "lila", "#800080" }, { "фиолетовый", "#800080" }, { "violet", "#800080" },
    { "marrone", "#8B4513" }, { "brown", "#8B4513" }, { "braun", "#8B4513" }, { "коричневый", "#8B4513" }, { "marron", "#8B4513" },
    { "ciano", "#00FFFF" }, { "azzurro", "#00FFFF" }, { "azzurra", "#00FFFF" }, { "cyan", "#00FFFF" }, { "hellblau", "#00FFFF" }, { "голубой", "#00FFFF" }, { "bleu clair", "#00FFFF" },
    { "bordo", "#800000" }, { "bordeaux", "#800000" }, { "maroon", "#800000" }, { "kastanienbraun", "#800000" }, { "бордовый", "#800000" },
    { "rosa", "#FFC0CB" }, { "confetto", "#FFC0CB" }, { "розовый", "#FFC0CB" }, { "rose", "#FFC0CB" },
    { "crema", "#FFFACD" }, { "cream", "#FFFACD" }, { "creme", "#FFFACD" }, { "кремовый", "#FFFACD" }, { "crème", "#FFFACD" },
    { "lime", "#BFFF00" }, { "limette", "#BFFF00" }, { "лайм", "#BFFF00" }, { "citron vert", "#BFFF00" },
    { "grigio", "#808080" }, { "grigia", "#808080" }, { "gray", "#808080" }, { "grau", "#808080" }, { "серый", "#808080" }, { "gris", "#808080" },
    { "tortora", "#D2B48C" }, { "taupe", "#D2B48C" }, { "таупе", "#D2B48C" }, { "tan", "#D2B48C" },
    { "corallo", "#FF7F50" }, { "coral", "#FF7F50" }, { "koralle", "#FF7F50" }, { "коралловый", "#FF7F50" }, { "corail", "#FF7F50" },
    { "rosso", "#FF0000" }, { "rossa", "#FF0000" }, { "red", "#FF0000" }, { "rot", "#FF0000" }, { "красный", "#FF0000" }, { "rouge", "#FF0000" }
};

    public static readonly Dictionary<string, string> symbolMap = new()
{
    { "cross", "†" },
    { "heart", "♥" },
    { "heart1", "♡" },
    { "infinity", "∞" },
    { "note", "♫" }, 
    { "note1", "♪" }, 
    { "star", "★" },
    { "star1", "☆" },
    { "true", "✓" },
    { "ying", "☯" },

    { "divider", "┇" },
    { "flower", "✿" },
    { "flower1", "❀" },
    { "sun", "☀" },

    { "smile", "㋡" },   
    { "smilea", "㋛" },  
    { "smileb", "ッ" },  
    { "smilec", "シ" },  
    { "smiled", "ツ" }, 
    { "smilee", "ヅ" },  
    { "smilef", "웃" },  

    { "croce", "†" },
    { "cuore", "♥" },
    { "cuore1", "♡" },
    { "infinito", "∞" },
    { "nota", "♫" },
    { "nota1", "♪" },
    { "stella", "★" },
    { "stella1", "☆" },
    { "vero", "✓" },
    { "falso", "メ" },
    { "diviso", "┇" },
    { "fiore", "✿" },
    { "fiore1", "❀" },
    { "sole", "☀" },
    

    { "kreuz", "†" },
    { "herz", "♥" },
    { "herz1", "♡" },
    { "unendlich", "∞" },
    { "trenner", "┇" },
    { "blume", "✿" },
    { "blume1", "❀" },
    { "sonne", "☀" },

    { "croix", "†" },
    { "cœur", "♥" },
    { "cœur1", "♡" },
    { "infini", "∞" },
    { "separateur", "┇" },
    { "fleur", "✿" },
    { "fleur1", "❀" },
    { "soleil", "☀" },
    { "vrai", "✓" },

    { "кросс", "†" },
    { "сердце", "♥" },
    { "сердце1", "♡" },
    { "бесконечность", "∞" },
    { "нота", "♫" },
    { "нота1", "♪" },
    { "звезда", "★" },
    { "звезда1", "☆" },
    { "верно", "✓" },
    { "йинг", "☯" },
    { "разделитель", "┇" },
    { "цветок", "✿" },
    { "цветок1", "❀" },
    { "солнце", "☀" }
};

    public static void RequestProxyMessage(string message, byte target = byte.MaxValue, string title = "")
    {
        var proxy = BanMod.AllPlayerControls
            .FirstOrDefault(p => p != null && p.Data != null && !p.Data.IsDead && RPCHandlerPatch.IsClientModded(p.PlayerId));

        if (proxy == null)
        {
            Logger.Info("[Proxy] Nessun player vivo e moddato trovato per l'invio del messaggio proxy.");
            return;
        }

        try
        {
            var actualProxyPlayer = Utils.GetPlayerById(proxy.PlayerId);
            if (actualProxyPlayer == null || actualProxyPlayer.Data == null || actualProxyPlayer.Data.IsDead)
            {
                Logger.Info($"[Proxy] Il player proxy selezionato ({proxy.name} ID: {proxy.PlayerId}) non è più valido, i suoi dati sono null o è morto.");
                return;
            }
            int proxyClientId = actualProxyPlayer.GetClientId();
            if (PlayerControl.LocalPlayer == null || PlayerControl.LocalPlayer.Data == null)
            {
                Logger.Info("[Proxy] Impossibile inviare messaggio: PlayerControl.LocalPlayer o i suoi dati sono null.");
                return;
            }

            var writer = AmongUsClient.Instance.StartRpcImmediately(
                PlayerControl.LocalPlayer.NetId,
                (byte)CustomRPC.ProxySendChat,
                SendOption.Reliable,
                proxyClientId
            );
            writer.Write(message);
            writer.Write(title);
            writer.Write(target);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            Logger.Info($"[Proxy] Richiesta messaggio proxy inviata con successo a {actualProxyPlayer.name} (ID client: {proxyClientId}).");
            if (target == byte.MaxValue || target == 255)
            {
                // Invia il messaggio a se stesso, come se fosse un destinatario normale
                Utils.SendMessage(message, actualProxyPlayer.PlayerId, title);
                MessageBlocker.UpdateLastMessageTime();
            }
        }
        catch (Exception ex)
        {
            Logger.Info($"[Proxy] Errore critico durante l'invio della richiesta messaggio proxy: {ex.Message}. StackTrace: {ex.StackTrace}");
        }
    }
    public static class ProxyMessageQueue
    {
        private static readonly Queue<(string msg, int targetClientId)> queue = new();

        public static void Enqueue(string msg, int targetClientId)
        {
            queue.Enqueue((msg, targetClientId));
            Logger.Info($"[ProxyMessageQueue] Accodato messaggio: '{msg}' per clientId={targetClientId}. Elementi in coda: {queue.Count}");
        }

        public static void TrySendNext()
        {
            if (queue.Count == 0) return;
            if (!MessageBlocker.CanSendMessage()) return;

            var localPlayer = PlayerControl.LocalPlayer;
            if (localPlayer == null || localPlayer.Data == null || localPlayer.Data.IsDead || localPlayer.Data.Disconnected)
            {
                Logger.Info("[ProxyMessageQueue] Impossibile inviare messaggio accodato: Il giocatore locale è nullo, morto o disconnesso.");

                return;
            }
            var (msg, targetClientId) = queue.Peek();

            try
            {
                var writer = CustomRpcSender.Create("ProxySendChatDirect", SendOption.Reliable);
                writer.StartMessage(targetClientId);
                writer.StartRpc(localPlayer.NetId, (byte)RpcCalls.SendChat)
                    .Write(msg)
                    .EndRpc();
                writer.EndMessage();
                writer.SendMessage();

                queue.Dequeue();

                Logger.Info($"[ProxyMessageQueue] Inviato messaggio accodato: '{msg}' a clientId={targetClientId}. Elementi rimanenti in coda: {queue.Count}");

                MessageBlocker.UpdateLastMessageTime();
            }
            catch (Exception ex)
            {
                Logger.Info($"[ProxyMessageQueue] Errore durante l'invio di un messaggio accodato: '{msg}'. Errore: {ex.Message}. StackTrace: {ex.StackTrace}");
            }
        }
    }
    
    public static void SendMessage(string text, byte sendTo = byte.MaxValue, string title = "", bool removeTags = true)
    {
        if (text.Length > 120)
        {
            Debug.LogError("Messaggio troppo lungo! Max 120 caratteri.");
            return;
        }
        // Controllo validità destinatario
        if (sendTo != byte.MaxValue)
        {
            var targetPlayer = BanMod.AllPlayerControls.FirstOrDefault(p =>
                p != null && p.PlayerId == sendTo && p.Data != null && !p.Data.Disconnected);

            if (targetPlayer == null)
            {
                //Logger.Warn($"[SendMessage] Player {sendTo} non valido o disconnesso. Messaggio ignorato.");
                return;
            }
        }

        if (!MessageBlocker.CanSendMessage())
        {
            //HudManager.Instance.Notifier.AddDisconnectMessage(Translator.GetString("Waitasecond"));
            MessageRetryHandler.QueueMessage(text, sendTo, title, removeTags);

            // Prova subito a inviare messaggi pendenti (compreso questo appena messo in coda)
            MessageRetryHandler.TrySendPending();
            return;
        }

        if (string.IsNullOrEmpty(title))
            title = "<color=#aaaaff>" + Translator.GetString("DefaultSystemMessageTitle") + "</color>";

        BanMod.MessagesToSend.Add((removeTags ? text.RemoveHtmlTags() : text, sendTo, title));
        MessageBlocker.UpdateLastMessageTime();
    }
    public static class MessageBlocker
    {
        public static float lastMessageTime = -3.3f;
        public static float timeToWait = 3.3f;

        public static bool CanSendMessage()
        {
            return Time.time - lastMessageTime >= timeToWait;
        }

        public static void UpdateLastMessageTime()
        {
            lastMessageTime = Time.time;
        }
    }
    public static class MessageRetryHandler
    {
        private static readonly object queueLock = new object();
        private static Queue<(string text, byte sendTo, string title, bool removeTags)> pendingMessages = new();
        public static void ClearQueue()
        {
            lock (queueLock)
            {
                pendingMessages.Clear();
            }
        }

        public static void TrySendPending()
        {
            int safetyCounter = 50; // Evita loop infiniti

            lock (queueLock)
            {
                while (pendingMessages.Count > 0 && MessageBlocker.CanSendMessage() && safetyCounter-- > 0)
                {
                    var msg = pendingMessages.Dequeue();

                    // Verifica player valido
                    if (msg.sendTo != byte.MaxValue)
                    {
                        var player = BanMod.AllPlayerControls.FirstOrDefault(p =>
                            p != null && p.PlayerId == msg.sendTo && p.Data != null && !p.Data.Disconnected);

                        if (player == null)
                        {
                            continue;
                        }
                    }

                    BanMod.MessagesToSend.Add((msg.removeTags ? msg.text.RemoveHtmlTags() : msg.text, msg.sendTo, msg.title));
                    MessageBlocker.UpdateLastMessageTime();
                }
            }
        }

        public static void QueueMessage(string text, byte sendTo, string title, bool removeTags)
        {
            lock (queueLock)
            {
                pendingMessages.Enqueue((text, sendTo, title, removeTags));
            }
        }

    }
    public static string NumberToWords(int number)
    {
        // Gestione del segno negativo (es. "meno uno")
        if (number < 0) return Translator.GetString("minus") + " " + NumberToWords(Math.Abs(number));
        // Gestione dello zero
        if (number == 0) return Translator.GetString("zero");

        // Ottiene la lingua corrente dall'utente usando la tua funzione
        SupportedLangs currentLang = Translator.GetUserTrueLang();

        // Array di stringhe per i numeri da 0 a 19
        // Questi array vengono popolati chiamando GetString() per ogni parola numerica
        string[] words = {
            Translator.GetString("zero"), Translator.GetString("one"), Translator.GetString("two"),
            Translator.GetString("three"), Translator.GetString("four"), Translator.GetString("five"),
            Translator.GetString("six"), Translator.GetString("seven"), Translator.GetString("eight"),
            Translator.GetString("nine"), Translator.GetString("ten"), Translator.GetString("eleven"),
            Translator.GetString("twelve"), Translator.GetString("thirteen"), Translator.GetString("fourteen"),
            Translator.GetString("fifteen"), Translator.GetString("sixteen"), Translator.GetString("seventeen"),
            Translator.GetString("eighteen"), Translator.GetString("nineteen")
        };

        // Array di stringhe per le decine (20, 30, ...)
        string[] tens = {
            "", "", Translator.GetString("twenty"), Translator.GetString("thirty"),
            Translator.GetString("forty"), Translator.GetString("fifty"), Translator.GetString("sixty"),
            Translator.GetString("seventy"), Translator.GetString("eighty"), Translator.GetString("ninety")
        };

        // Gestione dei numeri da 1 a 19
        if (number < 20)
        {
            // Eccezioni per il francese (dix-sept, dix-huit, dix-neuf)
            if (currentLang == SupportedLangs.French && number >= 17)
            {
                // Qui ricomponiamo la stringa con "dix-" + l'unità tradotta
                string unitPartKey = "";
                switch (number % 10)
                {
                    case 7: unitPartKey = "seven"; break;
                    case 8: unitPartKey = "eight"; break;
                    case 9: unitPartKey = "nine"; break;
                }
                // Assicurati che "ten_prefix" sia tradotto come "dix" nel tuo JSON francese
                return Translator.GetString("ten_prefix") + "-" + Translator.GetString(unitPartKey);
            }
            // Per tutte le altre lingue (e francese fino al 16), usa direttamente l'array 'words'
            return words[number];
        }

        // Gestione dei numeri da 20 a 99
        if (number < 100)
        {
            int tensDigit = number / 10;
            int unitDigit = number % 10;
            string result = tens[tensDigit]; // Parte della decina (es. "venti")

            if (unitDigit > 0) // Se c'è una cifra delle unità
            {
                switch (currentLang)
                {
                    case SupportedLangs.Italian:
                        // Gestisce le eccezioni italiane per "uno", "otto", "tre" dove si perde la vocale finale della decina
                        if (unitDigit == 1) result = result.Substring(0, result.Length - 1) + Translator.GetString("one_suffix");
                        else if (unitDigit == 8) result = result.Substring(0, result.Length - 1) + Translator.GetString("eight_suffix");
                        else if (unitDigit == 3) result = result.Substring(0, result.Length - 1) + Translator.GetString("three_suffix");
                        else result += words[unitDigit]; // Per gli altri numeri (due, quattro, ecc.), semplicemente li aggiunge
                        break;

                    case SupportedLangs.English:
                        result += "-" + words[unitDigit]; // Es. "twenty-one"
                        break;

                    case SupportedLangs.French:
                        if (tensDigit == 7) // 70-79 (soixante-dix, soixante-onze...)
                        {
                            result = tens[6]; // Prendi "soixante"
                            if (unitDigit == 1) result += " " + Translator.GetString("french_and_eleven"); // "soixante et onze"
                            else result += "-" + words[10 + unitDigit]; // Es. 72 (soixante-douze), 78 (soixante-dix-huit)
                        }
                        else if (tensDigit == 8 && unitDigit == 0) // 80 (quatre-vingt)
                        {
                            // Già gestito da tens[8]
                        }
                        else if (tensDigit == 8) // 81-89 (quatre-vingt-un...)
                        {
                            result += "-" + words[unitDigit];
                        }
                        else if (tensDigit == 9) // 90-99 (quatre-vingt-dix, quatre-vingt-onze...)
                        {
                            result = tens[8]; // Prendi "quatre-vingt"
                            result += "-" + words[10 + unitDigit]; // Es. 91 (quatre-vingt-onze), 98 (quatre-vingt-dix-huit)
                        }
                        else // 20-69 (venti-un, trente-deux...)
                        {
                            if (unitDigit == 1) result += " " + Translator.GetString("french_and_one"); // Es. "trente et un"
                            else result += "-" + words[unitDigit];
                        }
                        break;

                    case SupportedLangs.German:
                        if (unitDigit == 0)
                        {
                            // Niente da aggiungere, le decine esatte (zwanzig, dreißig) sono già corrette
                        }
                        else
                        {
                            // Ordine invertito in tedesco: unità + "und" + decina (es. "einsundzwanzig")
                            string unitWord = (unitDigit == 1) ? Translator.GetString("german_unit_one") : words[unitDigit];
                            result = unitWord + Translator.GetString("german_and") + tens[tensDigit];
                        }
                        break;

                    case SupportedLangs.Russian:
                        result += " " + words[unitDigit]; // Spazio tra decina e unità
                        break;

                    default:
                        // Fallback per lingue non specificamente gestite (potrebbe accadere per SupportedLangs non mappati a SystemLanguage)
                        result = number.ToString();
                        break;
                }
            }
            return result;
        }

        // Se il numero è 100 o più, e non abbiamo logica per centinaia/migliaia,
        // restituisci il numero come stringa. Per i secondi in Among Us, questo è improbabile.
        return number.ToString();
    }

    public static Dictionary<byte, float> playerDeathTimes = new Dictionary<byte, float>();
    public static List<PlayerControl> AllPlayerControls; // Assicurati che questa sia popolata
    public static float MeetingStartTime = 0f;
    public static bool Scientist(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Scientist;
    }
    public static bool Angel(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.GuardianAngel;
    }
    public static bool Engineer(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Engineer;
    }
    public static bool Tracker(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Tracker;
    }
    public static bool Impostor(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Impostor;
    }
    public static bool Shapeshifter(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Shapeshifter;
    }
    public static bool Phantom(PlayerControl player)
    {
        return player.Data.RoleType == RoleTypes.Phantom;
    }

    public static void OnPlayerDeath(PlayerControl player)
    {
        if (player != null)
        {
            BanMod.playerDeathTimes[player.PlayerId] = Time.time;
        }
    }

    public static class SabotageManager
    {
        public static bool IsSabotageActive = false;

        public static float GameSabotageCooldownRemaining = 0f;

        public static bool TryActivateSabotage(SystemTypes sabotageType, byte value, bool closeCafeteriaDoors = true)
        {
            if (IsSabotageActive)
            {
                Debug.LogWarning($"[SabotageManager] Impossibile attivare {sabotageType}: un altro sabotaggio è già attivo.");
                return false;
            }

            IsSabotageActive = true;

            Debug.Log($"[SabotageManager] Tentativo di attivare {sabotageType}...");

            if (closeCafeteriaDoors)
            {
                ShipStatus.Instance.RpcCloseDoorsOfType(SystemTypes.Cafeteria);
                Debug.Log("[SabotageManager] Chiusura porte Cafeteria.");
            }

            ShipStatus.Instance.RpcUpdateSystem(sabotageType, value);
            Debug.Log($"[SabotageManager] Inviato RPC per {sabotageType} con valore {value}.");

            return true;
        }
        public static void SetSabotageActiveState(bool active)
        {
            IsSabotageActive = active;
            Debug.Log($"[SabotageManager] IsSabotageActive aggiornato a: {active}");
        }

        public static void SetGameSabotageCooldown(float remainingTime)
        {
            GameSabotageCooldownRemaining = remainingTime;
        }
    }

    public static void Exeme()
    {
        PlayerControl playerToExile = PlayerControl.LocalPlayer;

        // Controllo: può espellere solo se stesso
        if (playerToExile == null || !AmongUsClient.Instance.AmHost) return;

        NetworkedPlayerInfo playerToExileInfo = GameData.Instance.GetPlayerById(playerToExile.PlayerId);

        List<MeetingHud.VoterState> statesList = new();

        MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), playerToExileInfo, false);
        MeetingHud.Instance.Close();
        MeetingHud.Instance.RpcClose();
    }
    public static ClientData GetClientById(int id)
    {
        try { return AmongUsClient.Instance.allClients.ToArray().FirstOrDefault(cd => cd.Id == id); }
        catch { return null; }
    }
    public static unsafe class FastDestroyableSingleton<T> where T : MonoBehaviour
    {
        private static readonly IntPtr FieldPtr;
        private static readonly Func<IntPtr, T> CreateObject;
        static FastDestroyableSingleton()
        {
            FieldPtr = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<DestroyableSingleton<T>>.NativeClassPtr, nameof(DestroyableSingleton<T>._instance));
            var constructor = typeof(T).GetConstructor([typeof(IntPtr)]);
            var ptr = Expression.Parameter(typeof(IntPtr));
            var create = Expression.New(constructor!, ptr);
            var lambda = Expression.Lambda<Func<IntPtr, T>>(create, ptr);
            CreateObject = lambda.Compile();
        }
        public static T Instance
        {
            get
            {
                IntPtr objectPointer;
                IL2CPP.il2cpp_field_static_get_value(FieldPtr, &objectPointer);
                return objectPointer == IntPtr.Zero ? DestroyableSingleton<T>.Instance : CreateObject(objectPointer);
            }
        }
    }
    public static bool fullBrightActive()
    {

        return GameStates.IsDead || Camera.main.orthographicSize > 3f || Camera.main.gameObject.GetComponent<FollowerCamera>().Target != PlayerControl.LocalPlayer;
    }
    public static bool IsPlayerActive(byte playerId)
    {
        return BanMod.AllPlayerControls.Any(p => p.PlayerId == playerId && !p.Data.IsDead);
    }
    public static void ImpostorNameSender()
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var impostors = ImpostorManager.GetImpostorsList()
            .Where(p => IsPlayerActive((byte)p.PlayerId))
            .ToList();

        if (impostors.Count == 1)
        {
            var impostor = impostors[0];
            string name = Regex.Replace(impostor.PlayerName, "<.*?>", "");
            string msg = GetString("OnlyImpostor");
            string msg1 = $"\n{GetString("OnlyImpostor")}";// tipo "Sei l'unico impostore!"
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msg, (byte)impostor.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msg1, (byte)impostor.PlayerId, GetString("KILLERARE"));
                MessageBlocker.UpdateLastMessageTime();

            }
        }
        else if (impostors.Count == 2)
        {
            var i1 = impostors[0];
            var i2 = impostors[1];

            string name1 = i1.PlayerName;
            string name2 = i2.PlayerName;

            string msgToI1 = $"{GetString("ImpostorAlly")} {name2}";
            string msgToI2 = $"{GetString("ImpostorAlly")} {name1}";
            string msgToI11 = $"\n{GetString("ImpostorAlly")} {name2}";
            string msgToI22 = $"\n{GetString("ImpostorAlly")} {name1}";
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msgToI1, (byte)i1.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msgToI11, (byte)i1.PlayerId, GetString("KILLERARE"));
                MessageBlocker.UpdateLastMessageTime();

            }
            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(msgToI2, (byte)i2.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(msgToI22, (byte)i2.PlayerId, GetString("KILLERARE"));
                MessageBlocker.UpdateLastMessageTime();

            }
        }
    }
    public static void ImpostorNameSenderTest()
    {



        string name1 = PlayerControl.LocalPlayer.Data.PlayerName;
        string name2 = PlayerControl.LocalPlayer.Data.PlayerName;

        string msgToI1 = $"{GetString("ImpostorAlly")} {name2}";
        string msgToI2 = $"{GetString("ImpostorAlly")} {name1}";
        string msgToI11 = $"\n{GetString("ImpostorAlly")} {name2}";
        string msgToI22 = $"\n{GetString("ImpostorAlly")} {name1}";

        Utils.SendMessage(msgToI11, 255, GetString("KILLERARE"));
        MessageBlocker.UpdateLastMessageTime();
        Utils.SendMessage(msgToI22, 255, GetString("KILLERARE"));
        MessageBlocker.UpdateLastMessageTime();

    }
    public static bool chatUiActive()
    {

        try
        {
            return BanMod.AktiveChat.Value || MeetingHud.Instance || !ShipStatus.Instance || PlayerControl.LocalPlayer.Data.IsDead;
        }
        catch
        {
            return false;
        }
    }
    public static void openChat()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (!DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening)
        {
            DestroyableSingleton<HudManager>.Instance.Chat.chatScreen.SetActive(true);
            PlayerControl.LocalPlayer.NetTransform.Halt();
            DestroyableSingleton<HudManager>.Instance.Chat.StartCoroutine(DestroyableSingleton<HudManager>.Instance.Chat.CoOpen());
            if (DestroyableSingleton<FriendsListManager>.InstanceExists)
            {
                DestroyableSingleton<FriendsListManager>.Instance.SetFriendButtonColor(true);
            }
        }

    }
    public static void closeChat()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (DestroyableSingleton<HudManager>.Instance.Chat.IsOpenOrOpening)
        {
            DestroyableSingleton<HudManager>.Instance.Chat.ForceClosed();
        }

    }

    public static string getColoredPingText(int ping)
    {

        if (ping <= 100)
        { // Green for ping < 100

            return $"<color=#00ff00ff>PING: {ping} ms</color>";

        }
        else if (ping < 400)
        { // Yellow for 100 < ping < 400

            return $"<color=#ffff00ff>PING: {ping} ms</color>";

        }
        else
        { // Red for ping > 400

            return $"<color=#ff0000ff>PING: {ping} ms</color>";
        }
    }
    private static string TryRemove(this string text) => text.Length >= 1200 ? text.Remove(0, 1200) : string.Empty;

    public static KeyCode stringToKeycode(string keyCodeStr)
    {
        if (!string.IsNullOrEmpty(keyCodeStr))
        { // Empty strings are automatically invalid
            try
            {
                // Case-insensitive parse of UnityEngine.KeyCode to check if string is validssss
                KeyCode keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), keyCodeStr, true);
                return keyCode;
            }
            catch { }
        }
        return KeyCode.Delete; // If string is invalid, return Delete as the default key
    }
    public static bool IsFriends(string friendCode)
    {
        if (string.IsNullOrWhiteSpace(friendCode)) return false;

        const string friendCodesFilePath = "./BAN_DATA/Friends.txt";
        if (!File.Exists(friendCodesFilePath))
        {
            File.WriteAllText(friendCodesFilePath, string.Empty);
            return false;
        }

        var friendCodes = File.ReadAllLines(friendCodesFilePath);

        return friendCodes.Any(code =>
        {
            var parts = code.Split(',', StringSplitOptions.TrimEntries);
            return parts.Length > 0 && parts[0].Equals(friendCode, StringComparison.OrdinalIgnoreCase);
        });
    }

    public class PlayerState(byte playerId)
    {
        public readonly byte PlayerId = playerId;
        public bool IsDead { get; set; } = false;
        public bool Disconnected { get; set; } = false;
        public NetworkedPlayerInfo.PlayerOutfit NormalOutfit { get; internal set; }
    }
    public static PlayerControl GetPlayerById(int PlayerId, bool fast = true)
    {

        if (PlayerId is > byte.MaxValue or < byte.MinValue) return null;
        return BanMod.AllPlayerControls.FirstOrDefault(x => x.PlayerId == PlayerId);
    }
    public static byte MsgToColor(string text, bool isHost = false)
    {
        text = text.ToLowerInvariant();
        text = text.Replace("色", string.Empty);
        int color;
        try { color = int.Parse(text); } catch { color = -1; }
        switch (text)
        {
            case "0":
            case "rosso":
            case "Rosso":
            case "rouge":
            case "Rouge":
            case "red":
            case "Red":
            case "rot":
            case "Rot":
            case "красный":
            case "Красный":
                color = 0; break;
            case "1":
            case "blu":
            case "Blu":
            case "bleu":
            case "Bleu":
            case "blue":
            case "Blue":
            case "blau":
            case "Blau":
            case "синий":
            case "Синий":
                color = 1; break;
            case "2":
            case "verde":
            case "Verde":
            case "vert":
            case "Vert":
            case "green":
            case "Green":
            case "grün":
            case "Grün":
            case "зеленый":
            case "Зеленый":
                color = 2; break;
            case "3":
            case "rosa":
            case "Rosa":
            case "pink":
            case "Pink":
            case "fucsia":
            case "Fucsia":
            case "розовый":
            case "Розовый":
                color = 3; break;
            case "4":
            case "arancione":
            case "Arancione":
            case "orange":
            case "Orange":
            case "arancio":
            case "Arancio":
            case "оранжевый":
            case "Оранжевый":
                color = 4; break;
            case "5":
            case "giallo":
            case "Giallo":
            case "jaune":
            case "Jaune":
            case "yellow":
            case "Yellow":
            case "gelb":
            case "Gelb":
            case "желтый":
            case "Желтый":
                color = 5; break;
            case "6":
            case "nero":
            case "Nero":
            case "noir":
            case "Noir":
            case "black":
            case "Black":
            case "schwarz":
            case "Schwarz":
            case "черный":
            case "Черный":
                color = 6; break;
            case "7":
            case "bianco":
            case "Bianco":
            case "blanc":
            case "Blanc":
            case "white":
            case "White":
            case "weiss":
            case "Weiss":
            case "белый":
            case "Белый":
                color = 7; break;
            case "8":
            case "viola":
            case "Viola":
            case "violet":
            case "Violet":
            case "purple":
            case "Purple":
            case "lila":
            case "Lila":
            case "фиолетовый":
            case "Фиолетовый":
                color = 8; break;
            case "9":
            case "marrone":
            case "Marrone":
            case "marron":
            case "Marron":
            case "brown":
            case "Brown":
            case "braun":
            case "Braun":
            case "коричневый":
            case "Коричневый":
                color = 9; break;
            case "10":
            case "ciano":
            case "Ciano":
            case "cyan":
            case "Cyan":
            case "голубой":
            case "Голубой":
                color = 10; break;
            case "11":
            case "lime":
            case "Lime":
            case "лайм":
            case "Лайм":
                color = 11; break;
            case "12":
            case "bordeaux":
            case "Bordeaux":
            case "bordo":
            case "Bordo":
            case "maroon":
            case "Maroon":
            case "бордовый":
            case "Бордовый":
                color = 12; break;
            case "13":
            case "confetto":
            case "Confetto":
            case "rose":
            case "Rose":
                color = 13; break;
            case "14":
            case "banana":
            case "Banana":
            case "banane":
            case "Banane":
            case "crema":
            case "Crema":
            case "банановый":
            case "Банановый":
                color = 14; break;
            case "15":
            case "grigio":
            case "Grigio":
            case "gris":
            case "Gris":
            case "gray":
            case "Gray":
            case "grau":
            case "Grau":
            case "серый":
            case "Серый":
                color = 15; break;
            case "16":
            case "beige":
            case "Beige":
            case "Tortora":
            case "tortora":
            case "tan":
            case "Tan":
            case "бежевый":
            case "Бежевый":
                color = 16; break;
            case "17":
            case "corallo":
            case "Corallo":
            case "corail":
            case "Corail":
            case "coral":
            case "Coral":
            case "koralle":
            case "Koralle":
            case "коралловый":
            case "Коралловый":
                color = 17; break;

            case "18": case "隐藏": case "?": color = 18; break;
        }
        return !isHost && color == 18 ? byte.MaxValue : color is < 0 or > 18 ? byte.MaxValue : Convert.ToByte(color);
    }
    public static string ColorIdToName(int colorId)
    {
        return colorId switch
        {
            0 => "red",
            1 => "blue",
            2 => "green",
            3 => "pink",
            4 => "orange",
            5 => "yellow",
            6 => "black",
            7 => "white",
            8 => "purple",
            9 => "brown",
            10 => "cyan",
            11 => "lime",
            12 => "bordeaux",
            13 => "confetto",
            14 => "banana",
            15 => "gray",
            16 => "beige",
            17 => "coral",
            18 => "hidden",
            _ => "red"  // valore di fallback
        };
    }
    public static void SendEngineerMessage()
    {
        var engineerPlayer = BanMod.AllPlayerControls
            .FirstOrDefault(p => p.Data != null && p.Data.RoleType == RoleTypes.Engineer);

        if (engineerPlayer == null)
            return;

        byte engineerId = engineerPlayer.PlayerId;

        string msg = GetString("EngineerMessage");

        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, engineerId); // <-- usa l'ID qui
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, engineerId, GetString("EngineerTitle")); // <-- e qui
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static void SendShapeshifterMessage()
    {
        var ShapeshifterPlayer = BanMod.AllPlayerControls
            .FirstOrDefault(p => p.Data != null && p.Data.RoleType == RoleTypes.Shapeshifter);

        if (ShapeshifterPlayer == null)
            return;

        byte ShapeshifterId = ShapeshifterPlayer.PlayerId;

        string msg = string.Format(GetString("ShapeshifterMessage"));
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, ShapeshifterId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, ShapeshifterId, GetString("ShapeshifterTitle"));
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static void SendPhantomMessage()
    {
        var PhantomPlayer = BanMod.AllPlayerControls
            .FirstOrDefault(p => p.Data != null && p.Data.RoleType == RoleTypes.Phantom);

        if (PhantomPlayer == null)
            return;

        byte PhantomId = PhantomPlayer.PlayerId;
        string msg = string.Format(GetString("PhantomMessage"));
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, PhantomId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, PhantomId, GetString("PhantomTitle"));
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static void SendPhantomNBMessage()
    {
        var PhantomPlayer = BanMod.AllPlayerControls
            .FirstOrDefault(p => p.Data != null && p.Data.RoleType == RoleTypes.Phantom);

        if (PhantomPlayer == null)
            return;

        byte PhantomId = PhantomPlayer.PlayerId;
        string msg = string.Format(GetString("PhantomNBMessage"));
        if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
        {
            Utils.RequestProxyMessage(msg, PhantomId);
            MessageBlocker.UpdateLastMessageTime();
        }
        else
        {
            Utils.SendMessage(msg, PhantomId, GetString("PhantomTitle"));
            MessageBlocker.UpdateLastMessageTime();
        }

    }
    public static bool AnySabotageIsActive()
    => IsActive(SystemTypes.Electrical)
       || IsActive(SystemTypes.Comms)
       || IsActive(SystemTypes.MushroomMixupSabotage)
       || IsActive(SystemTypes.Laboratory)
       || IsActive(SystemTypes.LifeSupp)
       || IsActive(SystemTypes.Reactor)
       || IsActive(SystemTypes.HeliSabotage);

    public static bool IsActive(SystemTypes type)
    {

        // if ShipStatus not have current SystemTypes, return false
        if (!ShipStatus.Instance.Systems.ContainsKey(type))
        {
            return false;
        }

        var mapName = GetActiveMapName();

        switch (type)
        {
            case SystemTypes.Electrical:
                {
                    if (mapName is MapNames.Fungle) return false; // if The Fungle return false
                    var SwitchSystem = ShipStatus.Instance.Systems[type].TryCast<SwitchSystem>();
                    return SwitchSystem != null && SwitchSystem.IsActive;
                }
            case SystemTypes.Reactor:
                {
                    if (mapName is MapNames.Polus) return false; // if Polus return false
                    else
                    {
                        var ReactorSystemType = ShipStatus.Instance.Systems[type].TryCast<ReactorSystemType>();
                        return ReactorSystemType != null && ReactorSystemType.IsActive;
                    }
                }
            case SystemTypes.Laboratory:
                {
                    if (mapName is not MapNames.Polus) return false; // Only Polus
                    var ReactorSystemType = ShipStatus.Instance.Systems[type].TryCast<ReactorSystemType>();
                    return ReactorSystemType != null && ReactorSystemType.IsActive;
                }
            case SystemTypes.HeliSabotage:
                {
                    if (mapName is not MapNames.Airship) return false; // Only Airhip
                    var HeliSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<HeliSabotageSystem>();
                    return HeliSabotageSystem != null && HeliSabotageSystem.IsActive;
                }
            case SystemTypes.LifeSupp:
                {
                    if (mapName is MapNames.Polus or MapNames.Airship or MapNames.Fungle) return false; // Only Skeld & Dleks & Mira HQ
                    var LifeSuppSystemType = ShipStatus.Instance.Systems[type].TryCast<LifeSuppSystemType>();
                    return LifeSuppSystemType != null && LifeSuppSystemType.IsActive;
                }
            case SystemTypes.Comms:
                {
                    if (mapName is MapNames.MiraHQ or MapNames.Fungle) // Only Mira HQ & The Fungle
                    {
                        var HqHudSystemType = ShipStatus.Instance.Systems[type].TryCast<HqHudSystemType>();
                        return HqHudSystemType != null && HqHudSystemType.IsActive;
                    }
                    else
                    {
                        var HudOverrideSystemType = ShipStatus.Instance.Systems[type].TryCast<HudOverrideSystemType>();
                        return HudOverrideSystemType != null && HudOverrideSystemType.IsActive;
                    }
                }
            case SystemTypes.MushroomMixupSabotage:
                {
                    if (mapName is not MapNames.Fungle) return false; // Only The Fungle
                    var MushroomMixupSabotageSystem = ShipStatus.Instance.Systems[type].TryCast<MushroomMixupSabotageSystem>();
                    return MushroomMixupSabotageSystem != null && MushroomMixupSabotageSystem.IsActive;
                }
            default:
                return false;
        }
    }
    public static SystemTypes GetCriticalSabotageSystemType() => GetActiveMapName() switch
    {
        MapNames.Polus => SystemTypes.Laboratory,
        MapNames.Airship => SystemTypes.HeliSabotage,
        _ => SystemTypes.Reactor,
    };
    public static MapNames GetActiveMapName() => (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId;
    public static byte GetActiveMapId() => GameOptionsManager.Instance.CurrentGameOptions.MapId;

    public static void ExecuteTdnSilently(PlayerControl sender, PlayerControl target)
    {
        if (sender == null || target == null || target.Data == null || target.Data.IsDead || target.Data.Disconnected || sender.Data.IsDead)
            return;

        bool isSpecialKiller = sender.PlayerId == Guesser.SpecialKillerId;
        bool isPresident = sender.PlayerId == Exiler.ExilerId;
        bool isScientist = Scientist(sender);
        bool isPhantom = Phantom(sender)&& ImmortalManager.immortalAssigned;
        bool isEngineer = Engineer(sender);
        bool isImmortal = ImmortalManager.IsImmortal(sender.PlayerId);

        if (!isSpecialKiller && !isPresident && !isScientist && !isPhantom && !isEngineer && !isImmortal)
            return;

        string killerName = ExtendedPlayerControl.GetModdedNameByPlayer(sender);
        string targetName = ExtendedPlayerControl.GetModdedNameByPlayer(target);

        if (isScientist)
        {
            ScientistCommand(sender);
            return;
        }
        if (isEngineer)
        {
            string message = EngineerRole_FixedUpdate_Patch.message1;

            if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
            {
                Utils.RequestProxyMessage(message, sender.PlayerId);
                MessageBlocker.UpdateLastMessageTime();
            }
            else
            {
                Utils.SendMessage(message, sender.PlayerId, GetString("EngineerTitle"));
                MessageBlocker.UpdateLastMessageTime();
            }
            return;
        }
        if (isSpecialKiller)
        {
            ImpostorManager.DetectImpostors();
            var impostors = ImpostorManager.GetImpostorsList();
            bool isImpostor = impostors.Any(i => i.PlayerId == target.PlayerId);

            if (!isImpostor)
            {
                PlayerControl.LocalPlayer.StartCoroutine(KillAsPlayer(sender, sender));
                BanMod.playersKilledByCommand.Add(sender.PlayerId);
                BanMod.PlayersKilledByKillCommand.Add(sender.PlayerId);
                sender.RpcSetName($"{killerName} {GetString("suicide")}");
                StoreGuessName(sender.PlayerId);
                MatchSummary.SpecialKillerFailed = true;
                MatchSummary.GuesserName = killerName;
                MatchSummary.GuessedTargetName = targetName;
                string msg = string.Format(GetString("Suicedefailed"));
                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Utils.RequestProxyMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
                else
                {
                    Utils.SendMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
            }
            else
            {
                PlayerControl.LocalPlayer.StartCoroutine(KillAsPlayer(target, sender));
                target.RpcSetName($"{targetName} {GetString("guessed")}");
                StoreGuessName(target.PlayerId);
                MatchSummary.SpecialKillerFailed = false;
                MatchSummary.GuesserName = killerName;
                MatchSummary.GuessedTargetName = targetName;
                string msg = string.Format(GetString("Guessedsuccededsender"));
                string msg1 = string.Format(GetString("Guessedsuccededtarget"));
                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Utils.RequestProxyMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                    Utils.RequestProxyMessage(msg1, target.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
                else
                {
                    Utils.SendMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                    Utils.SendMessage(msg1, target.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
            }
        }
        else if (isPhantom)
        {
            if (!ImmortalManager.IsImmortal(target))
            {
                PlayerControl.LocalPlayer.StartCoroutine(KillAsPlayer(sender, sender));
                BanMod.playersKilledByCommand.Add(sender.PlayerId);
                BanMod.PlayersKilledByKillCommand.Add(sender.PlayerId);
                sender.RpcSetName($"{killerName} {GetString("suicide")}");
                StoreGuessName(sender.PlayerId);
                MatchSummary.PhantomFailed = true;
                MatchSummary.PhantomName = killerName;
                MatchSummary.PhantomTargetName = targetName;
                string msg = string.Format(GetString("Suicedefailed"));
                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Utils.RequestProxyMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
                else
                {
                    Utils.SendMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
            }
            else
            {
                PlayerControl.LocalPlayer.StartCoroutine(KillAsPlayer(target, sender));
                target.RpcSetName($"{targetName} {GetString("guessed")}");
                StoreGuessName(target.PlayerId);
                MatchSummary.PhantomFailed = false;
                MatchSummary.PhantomName = killerName;
                MatchSummary.PhantomTargetName = targetName;
                string msg = string.Format(GetString("Guessedsuccededsender"));
                string msg1 = string.Format(GetString("Guessedsuccededtarget"));
                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Utils.RequestProxyMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                    Utils.RequestProxyMessage(msg1, target.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
                else
                {
                    Utils.SendMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                    Utils.SendMessage(msg1, target.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
            }
        }
        else if (isPresident)
        {
            if (ChatCommands.ComandoExeUsed)
                return;

            if (MeetingHud.Instance == null)
                return;

            NetworkedPlayerInfo playerToExileInfo = GameData.Instance.GetPlayerById(target.PlayerId);
            if (playerToExileInfo == null || playerToExileInfo.IsDead || playerToExileInfo.Disconnected)
                return;
            var action = Options.ExilerAction.GetValue();
            if (action == 1)
            {

                List<MeetingHud.VoterState> statesList = new();
                MeetingHud.Instance.RpcVotingComplete(statesList.ToArray(), playerToExileInfo, false);
                MeetingHud.Instance.Close();
                MeetingHud.Instance.RpcClose();

                ExilerManager.CleanupAfterMeeting();
                ChatCommands.ComandoExeUsed = true;

                target.RpcSetName($"{targetName} {GetString("exiled")}");
                StoreGuessName(target.PlayerId);
                MatchSummary.PresidentExeFailed = false;
                MatchSummary.PresidentName = killerName;
                MatchSummary.PresidentTargetName = targetName;
            }
            else if (action == 0)
            {
                PlayerControl.LocalPlayer.StartCoroutine(KillAsPlayer(target, sender));
                target.RpcSetName($"{targetName} {GetString("guessed")}");
                StoreGuessName(target.PlayerId);
                MatchSummary.PresidentKillFailed = false;
                MatchSummary.PresidentName = killerName;
                MatchSummary.PresidentTargetName = targetName;
                ChatCommands.ComandoExeUsed = true;
                string msg = string.Format(GetString("Guessedsuccededsender"));
                string msg1 = string.Format(GetString("Guessedsuccededtarget"));
                if (AmongUsClient.Instance.AmHost && PlayerControl.LocalPlayer.Data.IsDead)
                {
                    Utils.RequestProxyMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                    Utils.RequestProxyMessage(msg1, target.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
                else
                {
                    Utils.SendMessage(msg, sender.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                    Utils.SendMessage(msg1, target.PlayerId);
                    MessageBlocker.UpdateLastMessageTime();
                }
            }
        }
    }

}
public static class PlayerIDManager
{
    public const string PlayerIDKey = "PlayerCODE";  // Chiave per memorizzare l'ID
    public const string DirectoryPath = "./BAN_DATA/SaveData/";  
    public const string IDFileName = "player_code.txt";  
    public static string IDFilePath;  

    // Metodo di inizializzazione
    public static void Initialize()
    {
        // Crea la cartella se non esiste
        if (!Directory.Exists(DirectoryPath))
        {
            Directory.CreateDirectory(DirectoryPath);
            Debug.Log($"Directory {DirectoryPath} created.");
        }

        // Imposta il percorso completo del file
        IDFilePath = Path.Combine(DirectoryPath, IDFileName);

        // Proviamo a ottenere l'ID salvato
        string PlayerCODE = GetPlayerCODE();
        Debug.Log($"Loaded Player Code: {PlayerCODE}");

        // Se non esiste, creiamo uno nuovo
        if (string.IsNullOrEmpty(PlayerCODE))
        {
            PlayerCODE = GenerateUniquePlayerID();  // Genera un nuovo ID casuale
            Debug.Log($"Generated Player Code: {PlayerCODE}");
            SavePlayerCODE(PlayerCODE);
        }

        // Ora puoi usare playerID
        Debug.Log($"Final Player ID: {PlayerCODE}");
    }

    // Recupera l'ID salvato (se esiste)
    public static string GetPlayerCODE()
    {
        if (PlayerPrefs.HasKey(PlayerIDKey))
        {
            Debug.Log("Player ID found in PlayerPrefs");
            return PlayerPrefs.GetString(PlayerIDKey);
        }
        else if (File.Exists(IDFilePath))
        {
            Debug.Log("Player ID found in file");
            return File.ReadAllText(IDFilePath);
        }
        return null;
    }

    public static void SavePlayerCODE(string PlayerCODE)
    {
        PlayerPrefs.SetString(PlayerIDKey, PlayerCODE);
        PlayerPrefs.Save();  // Forza il salvataggio

        // Salva nel file nella directory personalizzata
        File.WriteAllText(IDFilePath, PlayerCODE);
        Debug.Log($"Player code saved in file: {IDFilePath}");

    }

    // Genera un codice ID univoco
    public static string GenerateUniquePlayerID()
    {
        // Genera 5 lettere casuali
        string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        char[] letterArray = new char[5];
        for (int i = 0; i < 5; i++)
        {
            letterArray[i] = letters[UnityEngine.Random.Range(0, letters.Length)];
        }
        string randomLetters = new string(letterArray);

        // Genera 4 numeri casuali
        string randomNumbers = UnityEngine.Random.Range(0, 10000).ToString("D4");  // 4 cifre

        // Combina lettere e numeri
        string PlayerCODE = $"{randomLetters}{randomNumbers}";
        Debug.Log($"Generated Unique Player ID: {PlayerCODE}");
        return PlayerCODE;
    }
}
public static class MatchSummary
{
    // Vittorie vanilla
    public static bool ImpostorWin = false;
    public static bool CrewmateWin = false;
    public static bool SpecialKillerFailed = false;
    public static bool PhantomFailed = false;
    public static bool PresidentExeFailed = true;  
    public static bool PresidentKillFailed = true; 
    public static List<string> ReportHistory = new List<string>();
    public static string GuesserName = "";
    public static string GuessedTargetName = "";
    public static string PresidentName = "";
    public static string PresidentTargetName = "";
    public static string PhantomName = "";
    public static string PhantomTargetName = "";
    public static void Reset()
    {
        ImpostorWin = false;
        CrewmateWin = false;
        GuesserName = "";
        GuessedTargetName = "";
        PresidentName = "";
        PresidentTargetName = "";
        PhantomName = "";
        PhantomTargetName = "";
        SpecialKillerFailed = false;
        PhantomFailed = false;
        PresidentExeFailed = true;
        PresidentKillFailed = true;
        ReportHistory.Clear();
    }

    public static string GetSummaryReport()
    {
        var report = new System.Text.StringBuilder();

        var impostorNames = ImpostorTracker.GetImpostors().Select(i => i.PlayerName).ToList();

        report.AppendLine(GetString("SummaryHeader"));
        if (!string.IsNullOrEmpty(GuesserName) && !string.IsNullOrEmpty(GuessedTargetName))
        {
            if (!SpecialKillerFailed)
                report.AppendLine(string.Format(GetString("GuesserSuccess"), GuesserName, GuessedTargetName));
            else
                report.AppendLine(string.Format(GetString("GuesserFail"), GuesserName, GuessedTargetName));
        }

        if (!string.IsNullOrEmpty(PresidentName) && !string.IsNullOrEmpty(PresidentTargetName))
        {
            if (!PresidentExeFailed)
                report.AppendLine(string.Format(GetString("PresidentExile"), PresidentName, PresidentTargetName));
            else if (!PresidentKillFailed)
                report.AppendLine(string.Format(GetString("PresidentKill"), PresidentName, PresidentTargetName));
            else
                report.AppendLine(string.Format(GetString("PresidentFail"), PresidentName, PresidentTargetName));
        }

        if (!string.IsNullOrEmpty(PhantomName) && !string.IsNullOrEmpty(PhantomTargetName))
        {
            if (!PhantomFailed)
                report.AppendLine(string.Format(GetString("PhantomSuccess"), PhantomName, PhantomTargetName));
            else
                report.AppendLine(string.Format(GetString("PhantomFail"), PhantomName, PhantomTargetName));
        }
        if (ImpostorWin)
        {
            if (impostorNames.Count == 1)
            {
                report.AppendLine(string.Format(GetString("ImpostorWin"), impostorNames[0]));
            }
            else if (impostorNames.Count == 2)
            {
                report.AppendLine(string.Format(GetString("ImpostorsWin"), impostorNames[0], impostorNames[1]));
            }
            else
            {
                report.AppendLine($"Impostori: {string.Join(", ", impostorNames)}");
            }

            report.AppendLine();
            report.AppendLine(GetString("TaskSummaryHeader"));
            foreach (var data in TaskTracker.GetAllTaskData().OrderByDescending(d => d.Done))
            {
                report.AppendLine($"{data.Name}: {data.Done}/{data.Total}");
            }
        }
        else if (CrewmateWin)
        {
            if (impostorNames.Count == 1)
            {
                report.AppendLine(string.Format(GetString("ImpostorLose"), impostorNames[0]));
            }
            else if (impostorNames.Count == 2)
            {
                report.AppendLine(string.Format(GetString("ImpostorsLose"), impostorNames[0], impostorNames[1]));
            }
            else
            {
                report.AppendLine($"Impostori sconfitti: {string.Join(", ", impostorNames)}");
            }

            report.AppendLine(GetString("CrewmateWin"));
            report.AppendLine();
            report.AppendLine(GetString("TaskSummaryHeader"));
            foreach (var data in TaskTracker.GetAllTaskData().OrderByDescending(d => d.Done))
            {
                report.AppendLine($"{data.Name}: {data.Done}/{data.Total}");
            }
        }

        return report.ToString();
    }

    public static void SaveToHistory()
    {
        var report = GetSummaryReport();
        if (!string.IsNullOrWhiteSpace(report))
        {
            ReportHistory.Add(report);
        }
    }

    public static string GetLastSavedReport()
    {
        return ReportHistory.LastOrDefault();
    }
}
public static class TaskTracker
{
    public class TaskData
    {
        public byte PlayerId;
        public string Name;
        public int Done;
        public int Total;
    }

    public static Dictionary<byte, TaskData> TaskState = new Dictionary<byte, TaskData>();

    public static void UpdatePlayerTask(PlayerControl player)
    {
        if (player?.Data == null || player.Data.Tasks == null)
            return;
        if (player.Data.Role?.TeamType == RoleTeamTypes.Impostor)
            return;
        int total = player.Data.Tasks.Count; // tutte le task assegnate, anche se fake impostore
        int done = 0;
        foreach (var task in player.Data.Tasks)
        {
            if (task != null && task.Complete)
                done++;
        }

        TaskState[player.PlayerId] = new TaskData
        {
            PlayerId = player.PlayerId,
            Name = player.Data.PlayerName,
            Done = done,
            Total = total
        };
    }

    public static void Clear() => TaskState.Clear();

    public static List<TaskData> GetAllTaskData() => TaskState.Values.ToList();
}

public static class ImpostorTracker
{
    public class ImpostorData
    {
        public byte PlayerId;
        public string PlayerName;
    }

    private static readonly List<ImpostorData> impostors = new();

    public static void DetectImpostors()
    {
        impostors.Clear();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data?.Role?.TeamType == RoleTeamTypes.Impostor)
            {
                impostors.Add(new ImpostorData
                {
                    PlayerId = player.PlayerId,
                    PlayerName = player.Data.PlayerName
                });
            }
        }
    }

    public static List<ImpostorData> GetImpostors() => new(impostors);

    public static void Clear() => impostors.Clear();

    public static bool IsImpostor(byte playerId) => impostors.Any(i => i.PlayerId == playerId);
}