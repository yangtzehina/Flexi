using System.Collections.Generic;

namespace Physalia.AbilitySystem
{
    public abstract class ProcessNode : FlowNode
    {
        internal Inport<FlowNode> previous;
        internal Outport<FlowNode> next;

        public override FlowNode Previous
        {
            get
            {
                IReadOnlyList<Outport> connections = previous.GetConnections();
                return connections.Count > 0 ? connections[0].Node as FlowNode : null;
            }
        }

        public override FlowNode Next
        {
            get
            {
                IReadOnlyList<Inport> connections = next.GetConnections();
                return connections.Count > 0 ? connections[0].Node as FlowNode : null;
            }
        }
    }
}
