using System;
// Assicurati di includere BanMod per accedere a OptionCategory
using BanMod;

namespace BanMod
{
    public class StringOptionItem : OptionItem
    {
        public IntegerValueRule Rule; // Assicurati che IntegerValueRule sia definita o accessibile
        public string[] Selections;

        // Costruttore: Cambia 'TabGroup tab' in 'OptionCategory category'
        public StringOptionItem(int id, string name, int defaultValue, OptionCategory category, bool isSingleValue, string[] selections)
        : base(id, name, defaultValue, category, isSingleValue) // Passa 'category' al base constructor
        {
            Rule = (0, selections.Length - 1, 1); // Assicurati che IntegerValueRule possa essere inizializzata così
            Selections = selections;
        }

        // Metodo Create (versione 1): Cambia 'TabGroup tab' in 'OptionCategory category'
        public static StringOptionItem Create(
            int id, string name, string[] selections, int defaultIndex, OptionCategory category, bool isSingleValue // Cambia qui
        )
        {
            return new StringOptionItem(
                id, name, defaultIndex, category, isSingleValue, selections // Passa 'category'
            );
        }

        // Metodo Create (versione 2): Cambia 'TabGroup tab' in 'OptionCategory category'
        public static StringOptionItem Create(
            int id, Enum name, string[] selections, int defaultIndex, OptionCategory category, bool isSingleValue // Cambia qui
        )
        {
            return new StringOptionItem(
                id, name.ToString(), defaultIndex, category, isSingleValue, selections // Passa 'category'
            );
        }

        public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()
        {
            // Assicurati che Translator sia accessibile (potrebbe richiedere un 'using static BanMod.Translator;')
            return Translator.GetString(Selections[Rule.GetValueByIndex(CurrentValue)]);
        }

        public int GetChance()
        {
            if (Selections.Length == 2) return CurrentValue * 100;

            var offset = 12 - Selections.Length;
            var index = CurrentValue + offset;
            var rate = index <= 1 ? index * 5 : (index - 1) * 10;
            return rate;
        }

        public override int GetValue()
            => Rule.RepeatIndex(base.GetValue());

        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(Rule.RepeatIndex(value), doSync);
        }
    }
}