using IDS.CMF.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class FormulaParserTests
    {
        [TestMethod]
        public void FormulaParser_Can_Parse_BasicAddFormula()
        {
            //arrange
            const string formula = "15.62+0.2";
            const double expectedResult = 15.82;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult, 0.01);
        }

        [TestMethod]
        public void FormulaParser_Can_Parse_BasicMinusFormula()
        {
            //arrange
            const string formula = "15.62-0.2";
            const double expectedResult = 15.42;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        public void FormulaParser_Can_Parse_BasicMultiplyFormula()
        {
            //arrange
            const string formula = "15.62*0.2";
            const double expectedResult = 3.124;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        public void FormulaParser_Can_Parse_BasicDivideFormula()
        {
            //arrange
            const string formula = "15.62/0.2";
            const double expectedResult = 78.1;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        public void FormulaParser_Can_Parse_MultipleOperatorsFormula()
        {
            //arrange
            const string formula = "0.7+4.16*0.8-0.2/5";
            const double expectedResult = 0.7376;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        public void FormulaParser_Can_Parse_MultipleOperatorsFormula_WithOrder()
        {
            //arrange
            const string formula = "0.7+(4.16*0.8)-(0.2/5)";
            const double expectedResult = 3.988;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult, 0.01);
        }
        
        [TestMethod]
        public void FormulaParser_Can_Parse_FormulaWithDoubleBrackets()
        {
            //arrange
            const string formula = "15.62*((1.68-0.2))";
            const double expectedResult = 23.1176;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult);
        }
        
        [TestMethod]
        public void FormulaParser_Can_Parse_MultipleOperatorsFormula_WithBracketsWithinBracket()
        {
            //arrange
            const string formula = "5.20*1.05-((0.8-0.2)*(3.1-0.2))";
            const double expectedResult = 3.72;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult, 0.01);
        }
        
        [TestMethod]
        public void FormulaParser_Can_Parse_WithNegativeValues()
        {
            //arrange
            const string formula = "(-0.1)+(0.3)+(-0.5)";
            const double expectedResult = -0.3;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult, 0.01);
        }

        [TestMethod]
        public void FormulaParser_Can_Parse_WithNegativeValues_AndOrder()
        {
            //arrange
            const string formula = "(-0.1)+(0.3*(-0.5))";
            const double expectedResult = -0.25;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult);
        }

        [TestMethod]
        public void FormulaParser_Can_Parse_FormulaWithVariables()
        {
            //arrange
            const string formula = "5.20*T*((W-0.2)*(W-0.2))";
            var variables = new Dictionary<string, double>
            {
                { "T", 1.2 },
                { "W", 3.5 },
            };
            const double expectedResult = 67.9536;

            //act
            var parser = new FormulaParser();
            var calculateFunc = parser.Parse(formula, variables);
            var result = calculateFunc();

            //assert
            Assert.AreEqual(result, expectedResult, 0.01);
        }
    }
}
