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

                new ExpectedConditon()
                {
                    Condition = @"('1' == '$Is64Bit$')"
                },

                // Wrongs
                new ExpectedConditon()
                {
                    Condition = "$DT_IdoNotExist$"
                },
            };

            foreach(var condition in conditions)
            {
                if (condition.ExpectedResult)
                    Assert.AreEqual(condition.Condition, PreProcessor.Process(condition.Condition));
                else
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
                    Condition = @"('1' == '1')",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = @"$DirectoryExists()",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = @"$FileExists(C:\Windows\explorer.exe)$"
                },
                new ExpectedConditon()
                {
                    Condition = @"$DirectoryExists(C:\Windows)$"
                },

                // Wrongs
                new ExpectedConditon()
                {
                    Condition = @"$DirectoryExists()$",
                },
                new ExpectedConditon()
                {
                    Condition = @"$DirectoryExists($",
                },
                new ExpectedConditon()
                {
                    Condition = @"$IdoNotExist(Test)$"
                },
            };

            foreach (var condition in conditions)
            {
                if(condition.ExpectedResult)
                    Assert.AreEqual(condition.Condition, PreProcessor.Process(condition.Condition));
                else
                    Assert.AreNotEqual(condition.Condition, PreProcessor.Process(condition.Condition));
            }
        }
    }
}