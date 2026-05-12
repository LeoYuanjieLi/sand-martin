using System;
using Grasshopper.Kernel;
using SandMartin.Host.Services;
using Xunit;
using Moq;

namespace SandMartin.Host.Tests
{
    public class SetComponentCodeTests
    {
        // Mock classes that mimic the structure of Grasshopper components for reflection
        public class Rhino8ComponentMock : GH_Component
        {
            public Rhino8ComponentMock() : base("Mock", "M", "Mock", "Test", "Test") { }
            public override Guid ComponentGuid => Guid.NewGuid();
            protected override void RegisterInputParams(GH_InputParamManager pManager) { }
            protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }
            protected override void SolveInstance(IGH_DataAccess DA) { }
            
            // In Rhino 8 this is a Field, not a Property
            public Rhino8Context Context = new Rhino8Context();
            
            public bool WasSolutionExpired { get; private set; }
            public override void ExpireSolution(bool recompute) { 
                base.ExpireSolution(recompute);
                WasSolutionExpired = true; 
            }
        }

        public class Rhino8Context
        {
            public string Text { get; private set; }
            public bool Rebuilt { get; private set; }
            public void SetText(string text) => Text = text;
            public void ReBuild() => Rebuilt = true;
        }

        public class LegacyComponentMock : GH_Component
        {
            public LegacyComponentMock() : base("Mock", "M", "Mock", "Test", "Test") { }
            public override Guid ComponentGuid => Guid.NewGuid();
            protected override void RegisterInputParams(GH_InputParamManager pManager) { }
            protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }
            protected override void SolveInstance(IGH_DataAccess DA) { }
            
            public LegacyScriptSource ScriptSource { get; set; } = new LegacyScriptSource();
            
            public bool WasSolutionExpired { get; private set; }
            public override void ExpireSolution(bool recompute) { 
                base.ExpireSolution(recompute);
                WasSolutionExpired = true; 
            }
        }

        public class LegacyScriptSource
        {
            public string ScriptCode { get; set; }
        }

        public class SimpleComponentMock : GH_Component
        {
            public SimpleComponentMock() : base("Mock", "M", "Mock", "Test", "Test") { }
            public override Guid ComponentGuid => Guid.NewGuid();
            protected override void RegisterInputParams(GH_InputParamManager pManager) { }
            protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }
            protected override void SolveInstance(IGH_DataAccess DA) { }
            
            public string Code { get; set; }
            
            public bool WasSolutionExpired { get; private set; }
            public override void ExpireSolution(bool recompute) { 
                base.ExpireSolution(recompute);
                WasSolutionExpired = true; 
            }
        }

        public class UnsupportedComponentMock : GH_Component
        {
            public UnsupportedComponentMock() : base("Mock", "M", "Mock", "Test", "Test") { }
            public override Guid ComponentGuid => Guid.NewGuid();
            protected override void RegisterInputParams(GH_InputParamManager pManager) { }
            protected override void RegisterOutputParams(GH_OutputParamManager pManager) { }
            protected override void SolveInstance(IGH_DataAccess DA) { }
        }

        [Fact]
        public void SetComponentCode_Rhino8Style_SetsTextAndRebuilds()
        {
            // Arrange
            var component = new Rhino8ComponentMock();
            string testCode = "print('hello')";

            // Act
            bool result = ScriptInjector.SetComponentCode(component, testCode);

            // Assert
            Assert.True(result);
            Assert.Equal(testCode, component.Context.Text);
            Assert.True(component.Context.Rebuilt);
            Assert.True(component.WasSolutionExpired);
        }

        [Fact]
        public void SetComponentCode_LegacyStyle_SetsScriptCode()
        {
            // Arrange
            var component = new LegacyComponentMock();
            string testCode = "print('legacy')";

            // Act
            bool result = ScriptInjector.SetComponentCode(component, testCode);

            // Assert
            Assert.True(result);
            Assert.Equal(testCode, component.ScriptSource.ScriptCode);
            Assert.True(component.WasSolutionExpired);
        }

        [Fact]
        public void SetComponentCode_SimpleStyle_SetsCodeProperty()
        {
            // Arrange
            var component = new SimpleComponentMock();
            string testCode = "print('simple')";

            // Act
            bool result = ScriptInjector.SetComponentCode(component, testCode);

            // Assert
            Assert.True(result);
            Assert.Equal(testCode, component.Code);
            Assert.True(component.WasSolutionExpired);
        }

        [Fact]
        public void SetComponentCode_UnsupportedComponent_ReturnsFalse()
        {
            // Arrange
            var component = new UnsupportedComponentMock();

            // Act
            bool result = ScriptInjector.SetComponentCode(component, "some code");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void SetComponentCode_SanitizesNewLines()
        {
            // Arrange
            var component = new SimpleComponentMock();
            string escapedCode = "line1\\nline2";
            string expectedCode = "line1\nline2";

            // Act
            bool result = ScriptInjector.SetComponentCode(component, escapedCode);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedCode, component.Code);
        }
    }
}
