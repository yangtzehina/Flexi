using System.Collections.Generic;

namespace Physalia.AbilityFramework
{
    public sealed class AbilityFlow
    {
        private readonly AbilitySystem system;
        private readonly Ability ability;
        private readonly AbilityGraph graph;

        public object userData;

        private readonly Dictionary<string, int> blackboard = new();

        private Actor actor;
        private IEventContext payload;
        private AbilityState currentState = AbilityState.CLEAN;

        public AbilitySystem System => system;
        internal Ability Ability => ability;
        internal AbilityGraph Graph => graph;

        public Actor Actor => actor;
        internal IEventContext Payload => payload;
        public AbilityState CurrentState => currentState;

        internal AbilityFlow(AbilityGraph graph) : this(null, graph, null)
        {

        }

        internal AbilityFlow(AbilitySystem system, AbilityGraph graph, Ability ability)
        {
            this.system = system;
            this.ability = ability;
            this.graph = graph;

            for (var i = 0; i < graph.Nodes.Count; i++)
            {
                graph.Nodes[i].flow = this;
            }

            for (var i = 0; i < graph.BlackboardVariables.Count; i++)
            {
                BlackboardVariable variable = graph.BlackboardVariables[i];
                blackboard.Add(variable.key, variable.value);
            }
        }

        internal void SetOwner(Actor actor)
        {
            this.actor = actor;
        }

        public void SetPayload(IEventContext payload)
        {
            this.payload = payload;
        }

        public void OverrideBlackboardVariable(string key, int value)
        {
            if (!blackboard.ContainsKey(key))
            {
                Logger.Warn($"[{nameof(AbilityFlow)}] Blackboard does not have key: {key}. Cancel the override");
                return;
            }

            blackboard[key] = value;
        }

        public int GetBlackboardVariable(string key)
        {
            if (blackboard.TryGetValue(key, out int value))
            {
                return value;
            }

            Logger.Warn($"[{nameof(AbilityFlow)}] Blackboard does not have key: {key}. Returns 0");
            return 0;
        }

        public bool CanExecute(IEventContext payload)
        {
            if (graph.EntryNodes.Count == 0)
            {
                return false;
            }

            bool result = graph.EntryNodes[0].CanExecute(payload);
            return result;
        }

        public void Execute()
        {
            if (currentState != AbilityState.CLEAN && currentState != AbilityState.ABORT && currentState != AbilityState.DONE)
            {
                Logger.Error($"[{nameof(AbilityFlow)}] You can not execute any unfinished ability instance!");
                return;
            }

            if (!CanExecute(payload))
            {
                Logger.Error($"[{nameof(AbilityFlow)}] Cannot execute ability, because the payload doesn't match the condition. Normally you should call CanExecute() to check.");
                return;
            }

            graph.Reset(0);

            IterateGraph();
        }

        public void Resume(IResumeContext resumeContext)
        {
            if (currentState != AbilityState.PAUSE)
            {
                Logger.Error($"[{nameof(AbilityFlow)}] You can not resume any unpaused ability instance!");
                return;
            }

            bool success = graph.Current.CheckNodeContext(resumeContext);
            if (!success)
            {
                Logger.Error($"[{nameof(AbilityFlow)}] The resume context is invalid, NodeType: {graph.Current.GetType()}");
                return;
            }

            currentState = graph.Current.Resume(resumeContext);
            if (currentState != AbilityState.RUNNING)
            {
                return;
            }

            IterateGraph();
        }

        private void IterateGraph()
        {
            while (graph.MoveNext())
            {
                currentState = graph.Current.Run();
                if (currentState != AbilityState.RUNNING)
                {
                    return;
                }
            }

            currentState = AbilityState.DONE;
        }

        internal void Push(FlowNode flowNode)
        {
            graph.Push(flowNode);
        }

        public void Reset()
        {
            graph.Reset(0);
            currentState = AbilityState.CLEAN;
            SetPayload(null);

            blackboard.Clear();
            for (var i = 0; i < graph.BlackboardVariables.Count; i++)
            {
                BlackboardVariable variable = graph.BlackboardVariables[i];
                blackboard.Add(variable.key, variable.value);
            }
        }
    }
}