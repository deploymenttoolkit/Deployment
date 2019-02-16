// <copyright file="EvaluatorTest.cs">Copyright ©  2019</copyright>
using System;
using System.Collections.Generic;
using DeploymentToolkit.Scripting.Exceptions;
using Microsoft.Pex.Framework;
using Microsoft.Pex.Framework.Validation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DeploymentToolkit.Scripting.Tests
{
    /// <summary>This class contains parameterized unit tests for Evaluator</summary>
    [PexClass(typeof(Evaluation))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(InvalidOperationException))]
    [PexAllowedExceptionFromTypeUnderTest(typeof(ArgumentException), AcceptExceptionSubtypes = true)]
    [TestClass]
    public partial class EvaluatorTest
    {
        [TestMethod()]
        public void EvaluateTest()
        {
            var conditions = new List<ExpectedConditon>()
            {
                
                // Exception tests
                new ExpectedConditon()
                {
                    Condition = "('first == 'second')",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidStringException)
                },
                new ExpectedConditon()
                {
                    Condition = "('first' == second')",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidStringException)
                },
                new ExpectedConditon()
                {
                    Condition = "(('1g1' == '1g2) And ('2g1' == '2g2'))",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidStringException)
                },
                new ExpectedConditon()
                {
                    Condition = "(('1g1' == '1g2') And ('2g1 == '2g2'))",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidStringException)
                },
                new ExpectedConditon()
                {
                    Condition = "('first' !! 'second')",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidOperatorException)
                },
                new ExpectedConditon()
                {
                    Condition = "()",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidGroupException)
                },
                new ExpectedConditon()
                {
                    Condition = "('first' != 'second' != 'third')",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidConditionException)
                },
                new ExpectedConditon()
                {
                    Condition = "Or ('first' != 'second')",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidConditionException)
                },
                new ExpectedConditon()
                {
                    Condition = "('first' != 'second') And",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidConditionException)
                },
                new ExpectedConditon()
                {
                    Condition = "(('first' != 'second') ('first' != 'second'))",
                    ExpectedThrow = true,
                    ExpectedException = typeof(ScriptingInvalidGroupException)
                },

                // String operations
                new ExpectedConditon()
                {
                    Condition = "('first' == 'second')",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "('first' != 'second')",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "('hello' == 'hello')",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "((('first' == 'second')))",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "((('hello' == 'hello')))",
                    ExpectedResult = true
                },

                // Number operations
                new ExpectedConditon()
                {
                    Condition = "('1' < '2')",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "('1' > '2')",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "('1' >= '2')",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "('1' <= '2')",
                    ExpectedResult = true
                },

                new ExpectedConditon()
                {
                    Condition = "('1' > '1')",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "('1' < '1')",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "('1' >= '1')",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "('1' <= '1')",
                    ExpectedResult = true
                },
                

                // Multiple groups
                new ExpectedConditon()
                {
                    Condition = "(('1g1' == '1g2') And ('2g1' == '2g2'))",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "(('1g1' == '1g2') And ('2g1' == '2g2') And ('3g1' == '3g2'))",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "(('1g1' == '1g2') Or ('2g1' == '2g2'))",
                    ExpectedResult = false
                },

                new ExpectedConditon()
                {
                    Condition = "(('1' > '1') And ('1' < '1'))",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' > '2') And ('1' < '2'))",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' > '2') Or ('1' < '2'))",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' > '2') Or ('1' < '2') And ('1' < '2'))",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' > '2') Or ('1' < '2') And ('1' > '2'))",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' > '2') And ('hello' == 'hello') Or ('1' < '2') And ('1' > '2'))",
                    ExpectedResult = false
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' < '2') And ('hello' == 'hello') Or ('1' < '2') And ('1' > '2'))",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' > '2') And ('hello' == 'hello') Or ('1' < '2') And ('1' < '2'))",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' > '2') Or ('hello' == 'hello') And ('1' < '2') And ('1' < '2'))",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' > '2') Or ('hello' == 'hello') Or ('1' < '2') Or ('1' < '2'))",
                    ExpectedResult = true
                },
                new ExpectedConditon()
                {
                    Condition = "(('1' > '2') And ('hello' == 'hello') And ('1' < '2') Or ('1' < '2'))",
                    ExpectedResult = true
                },
            };

            foreach (var condition in conditions)
            {
                if (condition.ExpectedThrow)
                {
                    Action action = delegate ()
                    {
                        Evaluation.Evaluate(condition.Condition);
                    };

                    if(condition.ExpectedException == typeof(ScriptingInvalidStringException))
                        Assert.ThrowsException<ScriptingInvalidStringException>(action);
                    else if (condition.ExpectedException == typeof(ScriptingInvalidOperatorException))
                        Assert.ThrowsException<ScriptingInvalidOperatorException>(action);
                    else if(condition.ExpectedException == typeof(ScriptingInvalidConditionException))
                        Assert.ThrowsException<ScriptingInvalidConditionException>(action);
                    else if(condition.ExpectedException == typeof(ScriptingInvalidGroupException))
                        Assert.ThrowsException<ScriptingInvalidGroupException>(action);
                    else
                        Assert.ThrowsException<ScriptingException>(action);
                }
                else
                    Assert.AreEqual(condition.ExpectedResult, Evaluation.Evaluate(condition.Condition));
            }
        }
    }
}
