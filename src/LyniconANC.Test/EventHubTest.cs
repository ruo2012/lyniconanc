﻿using Lynicon.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace LyniconANC.Test
{
    public class EventHubTests
    {
        [Fact]
        public void ProcessorOrdering()
        {
            var eh = new EventHub();
            List<string> processorsCalled = new List<string>();

            eh.RegisterEventProcessor("ev",
                ehd =>
                {
                    processorsCalled.Add("p1");
                    return ehd.Data;
                }, "TestMod");

            eh.RegisterEventProcessor("ev",
                ehd =>
                {
                    processorsCalled.Add("p2");
                    return ehd.Data;
                }, "TestMod2");

            eh.RegisterEventProcessor("ev",
                ehd =>
                {
                    processorsCalled.Add("p3");
                    return ehd.Data;
                }, "TestMod3", new OrderConstraint("TestMod3", new string[] { "TestMod2" }, new string[] { "TestMod" }));

            eh.RegisterEventProcessor("ev",
                ehd =>
                {
                    processorsCalled.Add("p4");
                    return ehd.Data;
                }, "TestMod4", new OrderConstraint("TestMod4", ConstraintType.ItemsBefore, "TestMod5"));

            eh.RegisterEventProcessor("ev",
                ehd =>
                {
                    processorsCalled.Add("p5");
                    return ehd.Data;
                }, "TestMod5", new OrderConstraint("TestMod5", ConstraintType.ItemsAfter, "TestMod2"));

            eh.ProcessEvent("x", this, null);
            Assert.Equal(0, processorsCalled.Count);

            eh.ProcessEvent("ev", this, null);
            Assert.True(processorsCalled.IndexOf("p2") < processorsCalled.IndexOf("p3"), "processor ordering");
            Assert.True(processorsCalled.IndexOf("p3") < processorsCalled.IndexOf("p1"), "processor ordering");
            Assert.True(processorsCalled.IndexOf("p5") < processorsCalled.IndexOf("p4"), "processor ordering");
            Assert.True(processorsCalled.IndexOf("p5") < processorsCalled.IndexOf("p2"), "processor ordering");
        }

        [Fact]
        public void EventHierarchy()
        {
            var eh = new EventHub();
            var processorsCalled = new List<string>();
            eh.RegisterEventProcessor("a",
                ehd =>
                {
                    processorsCalled.Add("a");
                    return ehd.Data;
                }, "TestMod");
            eh.RegisterEventProcessor("b",
                ehd =>
                {
                    processorsCalled.Add("b");
                    return ehd.Data;
                }, "TestMod");
            eh.RegisterEventProcessor("b.b",
                ehd =>
                {
                    processorsCalled.Add("b.b");
                    return ehd.Data;
                }, "TestMod2");
            eh.RegisterEventProcessor("c.b.b",
                ehd =>
                {
                    processorsCalled.Add("c.b.b");
                    return ehd.Data;
                }, "TestMod2");
            eh.RegisterEventProcessor("c.b",
                ehd =>
                {
                    processorsCalled.Add("c.b");
                    return ehd.Data;
                }, "TestMod3");

            eh.ProcessEvent("c.a.a", this, null);
            Assert.Equal(0, processorsCalled.Count);
            processorsCalled.Clear();

            eh.ProcessEvent("c.b.a", this, null);
            Assert.True(processorsCalled.Contains("c.b") && processorsCalled.Count == 1);
            processorsCalled.Clear();

            eh.ProcessEvent("b", this, null);
            Assert.True(processorsCalled.Contains("b") && processorsCalled.Count == 1);
            processorsCalled.Clear();

            eh.ProcessEvent("a.b.b", this, null);
            Assert.True(processorsCalled.Contains("a") && processorsCalled.Count == 1);
            processorsCalled.Clear();

            eh.ProcessEvent("c.b.b", this, null);
            Assert.True(processorsCalled.Contains("c.b") && processorsCalled.Contains("c.b.b") && processorsCalled.Count == 2);
            processorsCalled.Clear();

            eh.ProcessEvent("c.b.b.a", this, null);
            Assert.True(processorsCalled.Contains("c.b") && processorsCalled.Contains("c.b.b") && processorsCalled.Count == 2);
            processorsCalled.Clear();
        }
    }
}
