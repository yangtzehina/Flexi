namespace Physalia.AbilityFramework
{
    public class AndNode : ValueNode
    {
        public Inport<bool> a;
        public Inport<bool> b;
        public Outport<bool> result;

        protected override void EvaluateSelf()
        {
            result.SetValue(a.GetValue() && b.GetValue());
        }
    }
}
