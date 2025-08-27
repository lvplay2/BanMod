using System;
// Assicurati di includere BanMod per accedere a OptionCategory
using BanMod;

namespace BanMod
{
    public class BooleanOptionItem : OptionItem
    {
        public const string TEXT_true = "ColoredOn";
        public const string TEXT_false = "ColoredOff";

        // Costruttore: Cambia 'TabGroup tab' in 'OptionCategory category'
        public BooleanOptionItem(int id, string name, bool defaultValue, OptionCategory category, bool isSingleValue)
        : base(id, name, defaultValue ? 1 : 0, category, isSingleValue) // Passa 'category' al base constructor
        {
        }

        // Metodo Create (versione 1): Cambia 'TabGroup tab' in 'OptionCategory category'
        public static BooleanOptionItem Create(
            int id, string name, bool defaultValue, OptionCategory category, bool isSingleValue // Cambia qui
        )
        {
            return new BooleanOptionItem(
                id, name, defaultValue, category, isSingleValue // Passa 'category'
            );
        }

        // Metodo Create (versione 2): Cambia 'TabGroup tab' in 'OptionCategory category'
        public static BooleanOptionItem Create(
            int id, Enum name, bool defaultValue, OptionCategory category, bool isSingleValue // Cambia qui
        )
        {
            return new BooleanOptionItem(
                id, name.ToString(), defaultValue, category, isSingleValue // Passa 'category'
            );
        }

        public override string GetString()
        {
            // Assicurati che Translator sia accessibile (potrebbe richiedere un 'using static BanMod.Translator;')
            return Translator.GetString(GetBool() ? TEXT_true : TEXT_false);
        }

        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(value % 2 == 0 ? 0 : 1, doSync);
        }
    }
}