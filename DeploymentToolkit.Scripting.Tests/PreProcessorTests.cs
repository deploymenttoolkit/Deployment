using DeploymentToolkit.Scripting;
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

            foreach (var condition in conditions)
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
                if (condition.ExpectedResult)
                    Assert.AreEqual(condition.Condition, PreProcessor.Process(condition.Condition));
                else
                    Assert.AreNotEqual(condition.Condition, PreProcessor.Process(condition.Condition));
            }
        }

        [TestMethod()]
        public void AddVariableTest()
        {
            var scripts = new List<ExpectedScript>()
            {
                new ExpectedScript()
                {
                    Name = "IsWindowsInstalled",
                    Script = @"function IsWindowsInstalled { return Test-Path C:\Windows }",
                    Result = true,
                    TestCondition = new ExpectedConditon()
                    {
                        Condition = @"('$IsWindowsInstalled$' == '1')",
                        ExpectedResult = true
                    }
                },
                new ExpectedScript()
                {
                    Name = "StringTest",
                    Script = @"function StringTest { return 'Test' }",
                    Result = true,
                    TestCondition = new ExpectedConditon()
                    {
                        Condition = @"('$StringTest$' == 'Test')",
                        ExpectedResult = true
                    }
                },

                // Same environments tests
                new ExpectedScript()
                {
                    Name = "IsEnvironment",
                    Script = "$environment = 'TEST'; function IsEnvironment { return 'Test' }",
                    Environment = "TEST",
                    Result = true,
                    TestCondition = new ExpectedConditon()
                    {
                        Condition = @"('$IsEnvironment$' == 'Test')",
                        ExpectedResult = true
                    }
                },
                new ExpectedScript()
                {
                    Name = "IsEnvironmentTest",
                    Script = @"function IsEnvironmentTest { return $environment }",
                    Environment = "TEST",
                    Result = true,
                    TestCondition = new ExpectedConditon()
                    {
                        Condition = @"('$IsEnvironmentTest$' == 'TEST')",
                        ExpectedResult = true
                    }
                },

                // Errors
                new ExpectedScript()
                {
                    Name = "Test",
                    Script = @"function WrongName { return Test-Path C:\Windows }",
                    Result = false,
                },

                // Double declaration
                new ExpectedScript()
                {
                    Name = "Test",
                    Script = @"function Test { return Test-Path C:\Windows }",
                    Result = true,
                    TestCondition = new ExpectedConditon()
                    {
                        Condition = @"('$Test$' == '1')",
                        ExpectedResult = true
                    }
                },
                new ExpectedScript()
                {
                    Name = "Test",
                    Script = @"function Test { return Test-Path C:\Windows }",
                    Result = false,
                }
            };

            foreach(var script in scripts)
            {
                var result = PreProcessor.AddVariable(script.Name, script.Script, script.Environment);
                Assert.AreEqual(script.Result, result);

                if (!result)
                    continue;

                var preProcessed = PreProcessor.Process(script.TestCondition.Condition);
                Assert.AreEqual(script.TestCondition.ExpectedResult, Evaluation.Evaluate(preProcessed));
            }
        }

        [TestMethod]
        public void TestPowerShellEnvironmentDipose()
        {
            Assert.IsTrue(PreProcessor.DisposePowerShellEnvironments());
        }
    }
}