using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BanMod
{
    public abstract class OptionItem
    {
        #region static
        public static IReadOnlyList<OptionItem> AllOptions => _allOptions;
        private static List<OptionItem> _allOptions = new(1024);

        // Ora useremo un dizionario per mappare le categorie alle liste
        private static Dictionary<OptionCategory, List<OptionItem>> _categorizedOptions = new Dictionary<OptionCategory, List<OptionItem>>();

        // Rendiamo le liste di sola lettura basate sul dizionario per mantenere la struttura esistente,
        // ma la gestione interna avverrà tramite il dizionario.
        public static IReadOnlyList<OptionItem> AfkOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Afk, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> BlocklistOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Blocklist, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> WordlistOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Wordlist, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> SpamlistOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Spamlist, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> PhantomOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Phantom, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> ShapeshifterOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Shapeshifter, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> PhantomModdedOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.PhantomModded, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> ShapeshifterModdedOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.ShapeshifterModded, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> ImpostorOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Impostor, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> EngineerOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Engineer, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> EngineerModdedOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.EngineerModded, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> ImmortalOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Immortal, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> ScientistOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Scientist, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> ExilerOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Exiler, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> CrewmateOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Crewmate, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> HostOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Host, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> GeneralModdedOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.GeneralModded, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> GeneralOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.General, new List<OptionItem>());
        public static IReadOnlyList<OptionItem> SabotageOptions => _categorizedOptions.GetValueOrDefault(OptionCategory.Sabotage, new List<OptionItem>());

        public static IReadOnlyDictionary<int, OptionItem> FastOptions => _fastOptions;
        private static Dictionary<int, OptionItem> _fastOptions = new(1024);
        public static int CurrentPreset { get; set; }
#if DEBUG
        public static bool IdDuplicated { get; private set; } = false;
#endif
        #endregion

        public int Id { get; }
        public string Name { get; }
        public int DefaultValue { get; }
        public OptionCategory Category { get; } // Nuova proprietà per la categoria

        public bool IsSingleValue { get; }

        public Color NameColor { get; protected set; }
        public OptionFormat ValueFormat { get; protected set; }
        public bool IsHeader { get; protected set; }
        public bool IsHidden { get; protected set; }
        public Dictionary<string, string> ReplacementDictionary
        {
            get => _replacementDictionary;
            set
            {
                if (value == null) _replacementDictionary?.Clear();
                else _replacementDictionary = value;
            }
        }
        private Dictionary<string, string> _replacementDictionary;

        public int[] AllValues { get; private set; } = new int[NumPresets];
        public int CurrentValue
        {
            get => GetValue();
            set => SetValue(value);
        }
        public int SingleValue { get; private set; }

        public OptionItem Parent { get; private set; }
        public static object ApplyDenyNameList { get; internal set; } 

        public List<OptionItem> Children;

        public StringOption OptionBehaviour;

        public event EventHandler<UpdateValueEventArgs> UpdateValueEvent;
        public void SetEnabled(bool enabled)
        {
            if (OptionBehaviour != null)
            {
                OptionBehaviour.gameObject.SetActive(enabled);
            }
        }
        public OptionItem(int id, string name, int defaultValue, OptionCategory category, bool isSingleValue)
        {
            Id = id;
            Name = name;
            DefaultValue = defaultValue;
            Category = category; // Assegna la nuova proprietà
            IsSingleValue = isSingleValue;

            NameColor = Color.white;
            ValueFormat = OptionFormat.None;
            IsHeader = false;
            IsHidden = false;

            Children = new();

            if (Id == PresetId)
            {
                SingleValue = DefaultValue;
                CurrentPreset = SingleValue;
            }
            else if (IsSingleValue)
            {
                SingleValue = DefaultValue;
            }
            else
            {
                for (int i = 0; i < NumPresets; i++)
                {
                    AllValues[i] = DefaultValue;
                }
            }

            if (_fastOptions.TryAdd(id, this))
            {
                _allOptions.Add(this);
                // Inizializza la lista per la categoria se non esiste
                if (!_categorizedOptions.ContainsKey(category))
                {
                    _categorizedOptions[category] = new List<OptionItem>();
                }
                _categorizedOptions[category].Add(this); 

            }
            else
            {
                Logger.Error($"ID:{id}が重複しています", "OptionItem");
#if DEBUG
                IdDuplicated = true;
#endif
            }
        }

        public OptionItem Do(Action<OptionItem> action)
        {
            action(this);
            return this;
        }

        public OptionItem SetColor(Color value) => Do(i => i.NameColor = value);
        public OptionItem SetValueFormat(OptionFormat value) => Do(i => i.ValueFormat = value);
        public OptionItem SetHeader(bool value) => Do(i => i.IsHeader = value);
        public OptionItem SetHidden(bool value) => Do(i => i.IsHidden = value);

        public OptionItem SetParent(OptionItem parent) => Do(i =>
        {
            i.Parent = parent;
            parent.SetChild(i);
        });
        public OptionItem SetChild(OptionItem child) => Do(i => i.Children.Add(child));
        public OptionItem RegisterUpdateValueEvent(EventHandler<UpdateValueEventArgs> handler)
            => Do(i => UpdateValueEvent += handler);

        public OptionItem AddReplacement((string key, string value) kvp)
            => Do(i =>
            {
                ReplacementDictionary ??= new();
                ReplacementDictionary.Add(kvp.key, kvp.value);
            });
        public OptionItem RemoveReplacement(string key)
            => Do(i => ReplacementDictionary?.Remove(key));

        public virtual string GetName(bool disableColor = false)
        {
            return disableColor ?
                Translator.GetString(Name, ReplacementDictionary) :
                Utils.ColorString(NameColor, Translator.GetString(Name, ReplacementDictionary));
        }
        public virtual bool GetBool() => CurrentValue != 0 && (Parent == null || Parent.GetBool());
        public virtual int GetInt() => CurrentValue;
        public virtual float GetFloat() => CurrentValue;
        public virtual string GetString()
        {
            return ApplyFormat(CurrentValue.ToString());
        }
        public virtual int GetValue() => IsSingleValue ? SingleValue : AllValues[CurrentPreset];


        public string ApplyFormat(string value)
        {
            if (ValueFormat == OptionFormat.None) return value;
            return string.Format(Translator.GetString("Format." + ValueFormat), value);
        }

        public virtual void Refresh()
        {
            if (OptionBehaviour is not null and StringOption opt)
            {
                opt.TitleText.text = GetName();
                opt.ValueText.text = GetString();
                opt.oldValue = opt.Value = CurrentValue;
            }
        }
        public virtual void SetValue(int afterValue, bool doSave, bool doSync = true)
        {
            int beforeValue = CurrentValue;
            if (IsSingleValue)
            {
                SingleValue = afterValue;
            }
            else
            {
                AllValues[CurrentPreset] = afterValue;
            }

            CallUpdateValueEvent(beforeValue, afterValue);
            Refresh();
            if (doSync)
            {
                SyncAllOptions();
            }
            OptionSaver.Save();
        }
        public virtual void SetValue(int afterValue, bool doSync = true)
        {
            SetValue(afterValue, true, doSync);
        }
        public void SetAllValues(int[] values)
        {
            AllValues = values;
        }

        public static OptionItem operator ++(OptionItem item)
            => item.Do(item => item.SetValue(item.CurrentValue + 1));
        public static OptionItem operator --(OptionItem item)
            => item.Do(item => item.SetValue(item.CurrentValue - 1));

        public static void SwitchPreset(int newPreset)
        {
            CurrentPreset = Math.Clamp(newPreset, 0, NumPresets - 1);

            foreach (var op in AllOptions)
                op.Refresh();

            SyncAllOptions();
        }
        public static void SyncAllOptions()
        {
            if (
                BanMod.AllPlayerControls.Count() <= 1 ||
                AmongUsClient.Instance.AmHost == false ||
                PlayerControl.LocalPlayer == null
            ) return;

        }

        private void CallUpdateValueEvent(int beforeValue, int currentValue)
        {
            if (UpdateValueEvent == null) return;
            try
            {
                UpdateValueEvent(this, new UpdateValueEventArgs(beforeValue, currentValue));
            }
            catch (Exception ex)
            {
                Logger.Error($"[{Name}] Eccezione durante la chiamata di UpdateValueEvent", "OptionItem.UpdateValueEvent");
                Logger.Exception(ex, "OptionItem.UpdateValueEvent");
            }
        }

        public class UpdateValueEventArgs : EventArgs
        {
            public int CurrentValue { get; set; }
            public int BeforeValue { get; set; }
            public UpdateValueEventArgs(int beforeValue, int currentValue)
            {
                CurrentValue = currentValue;
                BeforeValue = beforeValue;
            }
        }

        public const int NumPresets = 5;
        public const int PresetId = 0;
    }

    // RINOMINATO TabGroup in OptionCategory per coerenza
    public enum OptionCategory
    {
        Afk,
        Blocklist,
        Wordlist,
        Spamlist,
        Phantom,
        Shapeshifter,
        PhantomModded,
        ShapeshifterModded,
        EngineerModded,
        Impostor,
        Engineer,
        Immortal,
        Scientist,
        Exiler,
        Crewmate,
        Host,
        GeneralModded,
        General,
        Sabotage
    }

    public enum OptionFormat
    {
        None,
        Players,
        Seconds,
        Percent,
        Times,
        Multiplier,
        Votes,
        Pieces,
    }
}