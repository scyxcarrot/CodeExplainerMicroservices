using IDS.Core.ImplantDirector;
using IDS.Core.Quality;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class QCReporterTests
    {
        [TestMethod]
        public void FormatFromDictionaryTest()
        {
            const string inputString = "Test {not a key} [TO_REPLACE_ONE] and [TO_REPLACE_TWO] as well";
            var inputDictionary = new Dictionary<string, string> {{"TO_REPLACE_ONE", "one"}, {"TO_REPLACE_TWO", "two"}};
            const string expectedOutput = "Test {not a key} one and two as well";
            var actualOutput = TestQualityReportExporter.FormatFromDictionary(inputString, inputDictionary);

            Assert.AreEqual(CleanString(expectedOutput), CleanString(actualOutput));
        }

        [TestMethod]
        public void JavaScriptArrayTest()
        {
            var inputArray = new[] {"one", "two", "three"};
            const string arrayName = "testArray";

            const string expectedJavaScriptArray =
                "var testArray = new Array();\ntestArray.push('one');\ntestArray.push('two');\ntestArray.push('three');";
            var actualJavaScriptArray = TestQualityReportExporter.CreateJavaScriptArray(inputArray, arrayName);

            Assert.AreEqual(CleanString(expectedJavaScriptArray), CleanString(actualJavaScriptArray));
        }

        [TestMethod]
        public void JavaScriptArrayOfArraysTest()
        {
            var inputArray = new string[][]
            {
                new string[] {"one", "two", "three"},
                new string[] {"four", "five", "six"},
                new string[] {"seven", "eight", "nine"}
            };
            const string arrayName = "testArray";
            const string subArrayName = "testSubArray";

            const string expectedJavaScriptArray =
                "var testArray = new Array();\nvar testSubArray0 = new Array();\ntestSubArray0.push('one');\ntestSubArray0.push('two');\ntestSubArray0.push('three');\ntestArray.push(testSubArray0);\nvar testSubArray1 = new Array();\ntestSubArray1.push('four');\ntestSubArray1.push('five');\ntestSubArray1.push('six');\ntestArray.push(testSubArray1);\nvar testSubArray2 = new Array();\ntestSubArray2.push('seven');\ntestSubArray2.push('eight');\ntestSubArray2.push('nine');\ntestArray.push(testSubArray2);";
            var actualJavaScriptArray =
                TestQualityReportExporter.CreateJavaScriptArrayOfArrays(inputArray, arrayName, subArrayName);

            Assert.AreEqual(CleanString(expectedJavaScriptArray), CleanString(actualJavaScriptArray));
        }

        private static string CleanString(string inputString)
        {
            return inputString.Trim(); //.Replace(" ", string.Empty);
        }
    }

    public class TestQualityReportExporter : QualityReportExporter
    {
        protected override bool FillReport(IImplantDirector directorInterface, string filename, out Dictionary<string, string> reportValues)
        {
            //do nothing here
            reportValues = new Dictionary<string, string>();
            return true;
        }

        public new static string FormatFromDictionary(string formatString, Dictionary<string, string> ValueDict)
        {
            return QCReportUtilities.FormatFromDictionary(formatString, ValueDict);
        }

        public new static string CreateJavaScriptArray(string[] imageStringsArray, string arrayName)
        {
            return QualityReportExporter.CreateJavaScriptArray(imageStringsArray, arrayName);
        }

        public new static string CreateJavaScriptArrayOfArrays(string[][] imageStringsMatrix, string arrayName, string subArrayName)
        {
            return QualityReportExporter.CreateJavaScriptArrayOfArrays(imageStringsMatrix, arrayName, subArrayName);
        }
    }
}
