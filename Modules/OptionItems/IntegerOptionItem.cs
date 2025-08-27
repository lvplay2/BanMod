using System;
// Assicurati di includere BanMod per accedere a OptionCategory
using BanMod;

namespace BanMod
{
    public class IntegerOptionItem : OptionItem
    {
        public IntegerValueRule Rule;

        // Costruttore: Cambia 'TabGroup tab' in 'OptionCategory category'
        public IntegerOptionItem(int id, string name, int defaultValue, OptionCategory category, bool isSingleValue, IntegerValueRule rule)
        : base(id, name, rule.GetNearestIndex(defaultValue), category, isSingleValue) // Passa 'category' al base constructor
        {
            Rule = rule;
        }

        // Metodo Create (versione 1): Cambia 'TabGroup tab' in 'OptionCategory category'
        public static IntegerOptionItem Create(
            int id, string name, IntegerValueRule rule, int defaultValue, OptionCategory category, bool isSingleValue // Cambia qui
        )
        {
            return new IntegerOptionItem(
                id, name, defaultValue, category, isSingleValue, rule // Passa 'category'
            );
        }

        // Metodo Create (versione 2): Cambia 'TabGroup tab' in 'OptionCategory category'
        public static IntegerOptionItem Create(
            int id, Enum name, IntegerValueRule rule, int defaultValue, OptionCategory category, bool isSingleValue // Cambia qui
        )
        {
            return new IntegerOptionItem(
                id, name.ToString(), defaultValue, category, isSingleValue, rule // Passa 'category'
            );
        }

        public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
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