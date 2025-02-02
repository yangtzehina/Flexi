using System.Collections.Generic;

namespace Physalia.Flexi
{
    internal static class StatTestHelper
    {
        internal static readonly int HEALTH = 1;
        internal static readonly int MAX_HEALTH = 2;
        internal static readonly int ATTACK = 11;

        internal static readonly List<StatDefinition> ValidList = new()
        {
            new StatDefinition
            {
                Id = HEALTH,
                Name = "Health"
            },
            new StatDefinition
            {
                Id = MAX_HEALTH,
                Name = "MaxHealth"
            },
            new StatDefinition
            {
                Id = ATTACK,
                Name = "Attack"
            },
        };

        internal static readonly List<StatDefinition> IdConflictList = new()
        {
            new StatDefinition
            {
                Id = 1,
                Name = "Health"
            },
            new StatDefinition
            {
                Id = 2,
                Name = "MaxHealth"
            },
            new StatDefinition
            {
                Id = 2,
                Name = "Attack"
            },
        };
    }
}
