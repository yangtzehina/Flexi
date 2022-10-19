namespace Physalia.AbilityFramework
{
    public class TrueNode : ValueNode
    {
        public Outport<bool> value;

        protected override void EvaluateSelf()
        {
            value.SetValue(true);
        }
    }
}