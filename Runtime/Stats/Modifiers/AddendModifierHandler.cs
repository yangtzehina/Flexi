using System.Collections.Generic;

namespace Physalia.AbilityFramework
{
    public class AddendModifierHandler : IModifierHandler
    {
        private readonly Dictionary<int, int> sumsCache = new();

        public void RefreshStats(StatOwner owner)
        {
            foreach (StatModifierInstance modifierInstance in owner.Modifiers)
            {
                for (var i = 0; i < modifierInstance.Items.Count; i++)
                {
                    StatModifierItem modifierItem = modifierInstance.Items[i];
                    if (modifierItem.op == StatModifierItem.Operator.ADD)
                    {
                        if (sumsCache.ContainsKey(modifierItem.statId))
                        {
                            sumsCache[modifierItem.statId] += modifierItem.value;
                        }
                        else
                        {
                            sumsCache.Add(modifierItem.statId, modifierItem.value);
                        }
                    }
                }
            }

            foreach (KeyValuePair<int, int> pair in sumsCache)
            {
                int statId = pair.Key;
                if (owner.Stats.TryGetValue(statId, out Stat stat))
                {
                    int sum = pair.Value;
                    stat.CurrentValue += sum;
                }
            }
        }
    }
}