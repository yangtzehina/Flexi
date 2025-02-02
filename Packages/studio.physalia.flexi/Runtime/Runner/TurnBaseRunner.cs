using static Physalia.Flexi.AbilityFlowStepper;

namespace Physalia.Flexi
{
    public abstract class TurnBaseRunner : AbilityFlowRunner
    {
        private RunningState runningState = RunningState.IDLE;

        public abstract IAbilityFlow Peek();
        internal abstract IAbilityFlow DequeueFlow();

        public override void Start()
        {
            if (runningState != RunningState.IDLE)
            {
                Logger.Error($"[{nameof(TurnBaseRunner)}] Failed to start! The runner is still running or waiting.");
                return;
            }

            IAbilityFlow flow = Peek();
            while (flow != null)
            {
                StepResult result = ExecuteStep(flow);
                bool keepRunning = HandleStepResult(result);
                if (!keepRunning)
                {
                    return;
                }

                flow = Peek();
            }

            runningState = RunningState.IDLE;
        }

        public override void Resume(IResumeContext resumeContext)
        {
            if (runningState != RunningState.PAUSE)
            {
                Logger.Error($"[{nameof(TurnBaseRunner)}] Failed to resume! The runner is not in PAUSE state.");
                return;
            }

            IAbilityFlow flow = Peek();
            StepResult result = ResumeStep(flow, resumeContext);
            if (result.state == ResultState.FAILED)
            {
                Logger.Error($"[{nameof(TurnBaseRunner)}] Failed to resume runner! The resume context is invalid, NodeType: {flow.Current.GetType()}");
            }

            bool keepRunning = HandleStepResult(result);
            if (!keepRunning)
            {
                return;
            }

            flow = Peek();
            while (flow != null)
            {
                result = ExecuteStep(flow);
                keepRunning = HandleStepResult(result);
                if (!keepRunning)
                {
                    return;
                }

                flow = Peek();
            }

            runningState = RunningState.IDLE;
        }

        public override void Tick()
        {
            IAbilityFlow flow = Peek();
            if (flow == null)
            {
                return;
            }

            if (runningState != RunningState.PAUSE)
            {
                Logger.Error($"[{nameof(TurnBaseRunner)}] Failed to tick! The runner is not in PAUSE state.");
                return;
            }

            StepResult result = TickStep(flow);
            bool keepRunning = HandleStepResult(result);
            if (!keepRunning)
            {
                return;
            }

            flow = Peek();
            while (flow != null)
            {
                result = ExecuteStep(flow);
                keepRunning = HandleStepResult(result);
                if (!keepRunning)
                {
                    return;
                }

                flow = Peek();
            }

            runningState = RunningState.IDLE;
        }

        private bool HandleStepResult(StepResult result)
        {
            var keepRunning = true;

            switch (result.type)
            {
                case ExecutionType.NODE_EXECUTION:
                case ExecutionType.NODE_RESUME:
                case ExecutionType.NODE_TICK:
                    if (result.state == ResultState.FAILED)
                    {
                        keepRunning = false;
                        break;
                    }
                    else if (result.state == ResultState.ABORT)
                    {
                        _ = DequeueFlow();
                    }
                    else if (result.state == ResultState.PAUSE)
                    {
                        keepRunning = false;
                        runningState = RunningState.PAUSE;
                    }
                    break;
                case ExecutionType.FLOW_FINISH:
                    _ = DequeueFlow();
                    break;
            }

            TriggerEvent(result);
            NotifyStepResult(result);

            return keepRunning;
        }

        private void TriggerEvent(StepResult result)
        {
            if (abilitySystem == null)
            {
                return;
            }

            switch (eventTriggerMode)
            {
                case EventTriggerMode.EACH_NODE:
                    if (result.type != ExecutionType.NODE_EXECUTION && result.type != ExecutionType.NODE_RESUME)
                    {
                        return;
                    }
                    break;
                case EventTriggerMode.EACH_FLOW:
                    if (result.type != ExecutionType.FLOW_FINISH)
                    {
                        return;
                    }
                    break;
                case EventTriggerMode.NEVER:
                    return;
            }

            abilitySystem.TriggerCachedEvents(this);
            abilitySystem.RefreshStatsAndModifiers();
        }

        public override void Clear()
        {
            runningState = RunningState.IDLE;
        }
    }
}
