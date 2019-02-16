using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace DeploymentToolkit.Scripting.Tests
{
    [TestClass()]
    public class PreProcessorTests
    {
        [TestMethod()]
        public void ProcessTest()
        {
            var conditions = new List<ExpectedConditon>()
            {
                new ExpectedConditon()
                {
                    Condition = "$Is64Bit$"
                },
                new ExpectedConditon()
                {
                    Condition = "$Is32Bit$"
                },
                new ExpectedConditon()
                {
                    Condition = "$DT_InstallPath$"
                },
                new ExpectedConditon()
                {
                    Condition = "$DT_FilesPath$"
                },
                new ExpectedConditon()
                {
                    Condition = "$DT_IsTaskSequence$"
                },
            };

            foreach(var condition in conditions)
            {
                Assert.AreNotEqual(condition.Condition, PreProcessor.Process(condition.Condition));
            }
        }

        [TestMethod()]
        public void ProcessTestFunctions()
        {
            var conditions = new List<ExpectedConditon>()
            {
                new ExpectedConditon()
                {
                    Condition = @"$FileExists(C:\Windows\explorer.exe)$"
                },
                new ExpectedConditon()
                {
                    Condition = @"$DirectoryExists(C:\Windows)$"
                },
            };

            foreach (var condition in conditions)
            {
                Assert.AreNotEqual(condition.Condition, PreProcessor.Process(condition.Condition));
            }
        }
    }
}