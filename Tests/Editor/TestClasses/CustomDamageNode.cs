using System.Collections.Generic;

namespace Physalia.AbilityFramework.Tests
{
    public class CustomDamageNode : ProcessNode
    {
        public Inport<CustomUnit> instigatorPort;
        public Inport<List<CustomUnit>> targets;
        public Inport<int> baseValue;

        protected override AbilityState DoLogic()
        {
            List<CustomUnit> list = targets.GetValue();
            int damage = baseValue.GetValue();
            for (var i = 0; i < list.Count; i++)
            {
                Stat stat = list[i].Owner.GetStat(CustomStats.HEALTH);
                stat.CurrentBase -= damage;

                Instance.System?.AddEventToLast(new CustomDamageEvent
                {
                    instigator = instigatorPort.GetValue(),
                    target = list[i],
                });
            }

            return AbilityState.RUNNING;
        }
    }
}