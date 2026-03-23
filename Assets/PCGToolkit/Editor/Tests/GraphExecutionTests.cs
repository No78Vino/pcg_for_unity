using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Graph;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class GraphExecutionTests
    {
        [Test]
        public void SimpleGraph_BoxNode_ProducesGeometry()
        {
            var graphData = ScriptableObject.CreateInstance<PCGGraphData>();
            graphData.GraphName = "Test_SingleBox";

            var boxNode = new PCGNodeData
            {
                NodeId = "node_box",
                NodeType = "Box",
                Parameters = new List<PCGSerializedParameter>
                {
                    new PCGSerializedParameter { Key = "sizeX", ValueType = "float", ValueJson = "2" },
                    new PCGSerializedParameter { Key = "sizeY", ValueType = "float", ValueJson = "1" },
                    new PCGSerializedParameter { Key = "sizeZ", ValueType = "float", ValueJson = "2" },
                }
            };
            graphData.Nodes.Add(boxNode);

            var executor = new PCGGraphExecutor(graphData);
            executor.Execute();

            var output = executor.GetNodeOutput("node_box");
            Assert.IsNotNull(output);
            Assert.GreaterOrEqual(output.Points.Count, 8);
        }

        [Test]
        public void ContinueOnError_SkipsFailedNode()
        {
            var graphData = ScriptableObject.CreateInstance<PCGGraphData>();
            graphData.GraphName = "Test_ContinueOnError";

            // Box -> (missing node type) -> should still work in continue mode
            var boxNode = new PCGNodeData
            {
                NodeId = "node_box",
                NodeType = "Box",
                Parameters = new List<PCGSerializedParameter>()
            };
            graphData.Nodes.Add(boxNode);

            var executor = new PCGGraphExecutor(graphData);
            executor.Execute(continueOnError: true);

            Assert.IsNotNull(executor.GetNodeOutput("node_box"));
        }

        [Test]
        public void ContextMultiError_CollectsAllErrors()
        {
            var ctx = new PCGContext(debug: true);
            ctx.CurrentNodeId = "node_a";
            ctx.LogError("Error 1");
            ctx.CurrentNodeId = "node_b";
            ctx.LogError("Error 2");
            ctx.CurrentNodeId = "node_c";
            ctx.LogWarning("Warning 1");

            Assert.IsTrue(ctx.HasError);
            Assert.AreEqual(3, ctx.Errors.Count);
            Assert.AreEqual(2, ctx.GetNodeErrors("node_a").Count() + ctx.GetNodeErrors("node_b").Count());
            Assert.AreEqual("Error 2", ctx.ErrorMessage);
        }

        [Test]
        public void ContextClearCache_ResetsErrors()
        {
            var ctx = new PCGContext();
            ctx.CurrentNodeId = "test";
            ctx.LogError("test error");
            Assert.IsTrue(ctx.HasError);

            ctx.ClearCache();
            Assert.IsFalse(ctx.HasError);
            Assert.AreEqual(0, ctx.Errors.Count);
        }
    }
}
