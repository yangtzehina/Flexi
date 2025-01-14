using NUnit.Framework;
using UnityEngine;

namespace Physalia.Flexi.Tests
{
    public class StatDefinitionTableTests
    {
        [Test]
        public void CreateTable_WithValidList_ReturnsTableAsExpected()
        {
            var list = StatTestHelper.ValidList;
            StatDefinitionTable table = new StatDefinitionTable.Factory().Create(list);
            for (var i = 0; i < list.Count; i++)
            {
                Assert.AreSame(list[i], table.GetStatDefinition(list[i].Id));
            }
        }

        [Test]
        public void CreateTable_With1IdConfliction_ReturnsNullAndLog2Error()
        {
            StatDefinitionTable table = new StatDefinitionTable.Factory().Create(StatTestHelper.IdConflictList);
            Assert.IsNull(table);
            TestUtilities.LogAssertAnyString(LogType.Error);
            TestUtilities.LogAssertAnyString(LogType.Error);
        }

        [Test]
        public void GetDefinition_WithNonExistedId_ReturnsNullAndLogError()
        {
            var table = new StatDefinitionTable();
            Assert.AreSame(null, table.GetStatDefinition(999));
            TestUtilities.LogAssertAnyString(LogType.Error);
        }
    }
}
