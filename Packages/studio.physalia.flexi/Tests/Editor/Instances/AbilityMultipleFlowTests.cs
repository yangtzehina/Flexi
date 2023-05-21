using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Physalia.Flexi.Tests
{
    public class AbilityMultipleFlowTests
    {
        private AbilitySystem abilitySystem;

        [SetUp]
        public void SetUp()
        {
            AbilitySystemBuilder builder = new AbilitySystemBuilder();

            var statDefinitionListAsset = ScriptableObject.CreateInstance<StatDefinitionListAsset>();
            builder.SetStatDefinitions(statDefinitionListAsset);

            abilitySystem = builder.Build();
        }

        [Test]
        public void NormalMultipleFlows_AllFlowsExecute()
        {
            // Create an ability graph that logs
            var abilityGraph = new AbilityGraph();
            StartNode startNode = abilityGraph.AddNewNode<StartNode>();
            LogNode logNode = abilityGraph.AddNewNode<LogNode>();
            StringNode stringNode = abilityGraph.AddNewNode<StringNode>();
            startNode.next.Connect(logNode.previous);
            logNode.text.Connect(stringNode.output);

            // Create an ability data with 3 flows
            var abilityData = new AbilityData();
            for (var i = 0; i < 3; i++)
            {
                stringNode.text.Value = "Test" + i.ToString();
                string json = AbilityGraphUtility.Serialize(abilityGraph);
                abilityData.graphJsons.Add(json);
            }

            // Get an ability, run it
            Ability ability = abilitySystem.GetAbility(abilityData);
            abilitySystem.TryEnqueueAbility(ability, null);
            abilitySystem.Run();

            // Assert
            LogAssert.Expect(LogType.Log, "Test0");
            LogAssert.Expect(LogType.Log, "Test1");
            LogAssert.Expect(LogType.Log, "Test2");
        }

        [Test]
        public void DisableFlow_OnlyThatFlowDoesNotExecute()
        {
            // Create an ability graph that can pause
            var abilityGraph = new AbilityGraph();
            StartNode startNode = abilityGraph.AddNewNode<StartNode>();
            LogNode logNode = abilityGraph.AddNewNode<LogNode>();
            StringNode stringNode = abilityGraph.AddNewNode<StringNode>();
            startNode.next.Connect(logNode.previous);
            logNode.text.Connect(stringNode.output);

            // Create an ability data with 3 flows
            var abilityData = new AbilityData();
            for (var i = 0; i < 3; i++)
            {
                stringNode.text.Value = "Test" + i.ToString();
                string json = AbilityGraphUtility.Serialize(abilityGraph);
                abilityData.graphJsons.Add(json);
            }

            // Get an ability, run it
            Ability ability = abilitySystem.GetAbility(abilityData);
            ability.SetEnable(1, false);

            abilitySystem.TryEnqueueAbility(ability, null);
            abilitySystem.Run();

            // Assert
            LogAssert.Expect(LogType.Log, "Test0");
            LogAssert.Expect(LogType.Log, "Test2");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
