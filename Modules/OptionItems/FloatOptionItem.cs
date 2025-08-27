using System;
// Assicurati di includere BanMod per accedere a OptionCategory
using BanMod;

namespace BanMod
{
    public class FloatOptionItem : OptionItem
    {
        public FloatValueRule Rule;

        // Costruttore: Cambia 'TabGroup tab' in 'OptionCategory category'
        public FloatOptionItem(int id, string name, float defaultValue, OptionCategory category, bool isSingleValue, FloatValueRule rule)
        : base(id, name, rule.GetNearestIndex(defaultValue), category, isSingleValue) // Passa 'category' al base constructor
        {
            Rule = rule;
        }

        // Metodo Create (versione 1): Cambia 'TabGroup tab' in 'OptionCategory category'
        public static FloatOptionItem Create(
            int id, string name, FloatValueRule rule, float defaultValue, OptionCategory category, bool isSingleValue // Cambia qui
        )
        {
            return new FloatOptionItem(
                id, name, defaultValue, category, isSingleValue, rule // Passa 'category'
            );
        }

        // Metodo Create (versione 2): Cambia 'TabGroup tab' in 'OptionCategory category'
        public static FloatOptionItem Create(
            int id, Enum name, FloatValueRule rule, float defaultValue, OptionCategory category, bool isSingleValue // Cambia qui
        )
        {
            return new FloatOptionItem(
                id, name.ToString(), defaultValue, category, isSingleValue, rule // Passa 'category'
            );
        }

        public override int GetInt() => (int)Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()
        {
            // Assicurati che ApplyFormat sia accessibile (è un metodo della classe base OptionItem)
            return ApplyFormat(Rule.GetValueByIndex(CurrentValue).ToString());
        }
        public override int GetValue()
            => Rule.RepeatIndex(base.GetValue());

        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(Rule.RepeatIndex(value), doSync);
        }
    }
}