namespace Physalia.AbilityFramework
{
    public class FalseNode : ValueNode
    {
        public Outport<bool> value;

        protected override void EvaluateSelf()
        {
            value.SetValue(false);
        }
    }
}