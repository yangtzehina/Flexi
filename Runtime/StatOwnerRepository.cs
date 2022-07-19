using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Physalia.Stats
{
    public class StatOwnerRepository
    {
        private readonly StatDefinitionTable table;

        private readonly Dictionary<int, StatOwner> idToOwners = new();
        private readonly HashSet<StatOwner> owners = new();
        private readonly Random random = new();

        public StatOwnerRepository(StatDefinitionTable table)
        {
            this.table = table;
        }

        public StatOwner CreateOwner()
        {
            int randomId = random.Next(0, int.MaxValue);
            while (idToOwners.ContainsKey(randomId))  // Prevent unlucky collisions
            {
                randomId = random.Next(0, int.MaxValue);
            }

            var owner = new StatOwner(randomId, table, this);
            idToOwners.Add(randomId, owner);
            owners.Add(owner);
            return owner;
        }

        public StatOwner GetOwner(int id)
        {
            if (idToOwners.TryGetValue(id, out StatOwner owner))
            {
                return owner;
            }

            Debug.LogWarning($"Cannot find StatOwner with <Id:{id}>");
            return null;
        }

        internal void RemoveOwner(StatOwner owner)
        {
            if (owner == null)
            {
                Debug.LogError($"Remove owner failed! The owner is null");
                return;
            }

            if (!owners.Contains(owner))
            {
                Debug.LogError($"Remove owner failed! The owner with Id:{owner.Id} does not belong to this repository");
                return;
            }

            idToOwners.Remove(owner.Id);
            owners.Remove(owner);
        }
    }
}
