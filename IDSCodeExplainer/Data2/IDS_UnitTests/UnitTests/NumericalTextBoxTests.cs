using IDS.Core.WPFControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Threading;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class NumericalTextBoxTests
    {
        private static void SetParameters(ref NumericalTextBoxHelper helper, string currentText, int decimalPlaces, int caretIndex)
        {
            helper.CurrentText = currentText;
            helper.DecimalPlaces = decimalPlaces;
            helper.CaretIndex = caretIndex;
        }

        private static void SetParameters(ref NumericalTextBoxHelper helper, string currentText, int decimalPlaces, int selectionStart, int selectionLength)
        {
            helper.CurrentText = currentText;
            helper.DecimalPlaces = decimalPlaces;
            helper.SelectionStart = selectionStart;
            helper.SelectionLength = selectionLength;
        }

        [TestMethod]
        public void TestFractionCounter()
        {
            var helper = new NumericalTextBoxHelper();

            var nFractions = 0;

            //Test for right-ness
            SetParameters(ref helper, "0.0", 1, 0);
            nFractions = helper.CountCurrentTextFractions();
            Assert.AreEqual(nFractions, 1);

            SetParameters(ref helper, "0.03", 1, 0);
            nFractions = helper.CountCurrentTextFractions();
            Assert.AreEqual(nFractions, 2);

            SetParameters(ref helper, "0.0001", 1, 0);
            nFractions = helper.CountCurrentTextFractions();
            Assert.AreEqual(nFractions, 4);

            SetParameters(ref helper, "0", 1, 0);
            nFractions = helper.CountCurrentTextFractions();
            Assert.AreEqual(nFractions, 0);

            SetParameters(ref helper, "0.", 1, 0);
            nFractions = helper.CountCurrentTextFractions();
            Assert.AreEqual(nFractions, 0);

            //Test for wrong-ness
            SetParameters(ref helper, "0.0", 1, 0);
            nFractions = helper.CountCurrentTextFractions();
            Assert.AreNotEqual(nFractions, 2);

            SetParameters(ref helper, "0.0001", 1, 0);
            nFractions = helper.CountCurrentTextFractions();
            Assert.AreNotEqual(nFractions, 3);

            SetParameters(ref helper, "0.000", 1, 0);
            nFractions = helper.CountCurrentTextFractions();
            Assert.AreNotEqual(nFractions, 2);
        }

        [TestMethod]
        public void TestSelectionsIsDigit()
        {
            var helper = new NumericalTextBoxHelper();

            var selectionIsNumber = false;

            //Test for right-ness
            SetParameters(ref helper, "0.0", 1, 0, 1);
            selectionIsNumber = helper.SelectionIsNumber();
            Assert.IsTrue(selectionIsNumber);

            SetParameters(ref helper, "0.001", 1, 2, 3);
            selectionIsNumber = helper.SelectionIsNumber();
            Assert.IsTrue(selectionIsNumber);

            SetParameters(ref helper, "0.00134", 1, 3, 2);
            selectionIsNumber = helper.SelectionIsNumber();
            Assert.IsTrue(selectionIsNumber);

            //Test for wrong-ness
            SetParameters(ref helper, "0.00*", 1, 2, 3);
            selectionIsNumber = helper.SelectionIsNumber();
            Assert.IsFalse(selectionIsNumber);

            SetParameters(ref helper, "0.01", 1, 0, 3);
            selectionIsNumber = helper.SelectionIsNumber();
            Assert.IsFalse(selectionIsNumber);

            SetParameters(ref helper, "0.01*", 1, 3, 2);
            selectionIsNumber = helper.SelectionIsNumber();
            Assert.IsFalse(selectionIsNumber);
        }

        [TestMethod]
        public void TestAddingNumberByCaret()
        {
            var helper = new NumericalTextBoxHelper();

            var okToAdd = false;

            SetParameters(ref helper, "0.0", 1, 2);
            okToAdd = helper.AddStringCheckIsOk("0");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "0.0", 1, 3);
            okToAdd = helper.AddStringCheckIsOk("0");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "0.", 1, 2);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.0", 2, 3);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.01", 2, 4);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "0.01", 2, 0);
            okToAdd = helper.AddStringCheckIsOk("-");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.0120", 4, 4);
            okToAdd = helper.AddStringCheckIsOk("11");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "0.00", 4, 3);
            okToAdd = helper.AddStringCheckIsOk("11");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.00", 2, 3);
            okToAdd = helper.AddStringCheckIsOk("11");
            Assert.IsFalse(okToAdd);
        }

        [TestMethod]
        public void TestAddingNumberBySelection()
        {
            var helper = new NumericalTextBoxHelper();

            var okToAdd = false;

            SetParameters(ref helper, "0.0", 1, 2, 1);
            okToAdd = helper.AddStringCheckIsOk("0");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.01", 1, 2, 2);
            okToAdd = helper.AddStringCheckIsOk("0");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.", 1, 2, 0);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.0", 2, 0, 2);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.01", 2, 1, 2);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.01*", 2, 2, 3);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.01*", 2, 4, 1);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "0.01**", 2, 3, 2);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "0.01**", 2, 3, 3);
            okToAdd = helper.AddStringCheckIsOk("1");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.01", 2, 2, 2);
            okToAdd = helper.AddStringCheckIsOk("12");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.01", 2, 1, 3);
            okToAdd = helper.AddStringCheckIsOk("12");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "0.01", 2, 2, 2);
            okToAdd = helper.AddStringCheckIsOk("123");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "0.0123", 4, 3, 2);
            okToAdd = helper.AddStringCheckIsOk("123");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "0.0123",7, 3, 1);
            okToAdd = helper.AddStringCheckIsOk("123");
            Assert.IsTrue(okToAdd);
        }

        [TestMethod]
        public void TestAddingNegativity()
        {
            var helper = new NumericalTextBoxHelper();

            var okToAdd = false;

            //By Caret
            SetParameters(ref helper, "1.0", 1, 0);
            okToAdd = helper.AddStringCheckIsOk("-");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "1.0", 1, 1);
            okToAdd = helper.AddStringCheckIsOk("-");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "-1.0", 1, 1);
            okToAdd = helper.AddStringCheckIsOk("-");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "1.0", 1, 0);
            okToAdd = helper.AddStringCheckIsOk("-");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "-1.0", 1, 0);
            okToAdd = helper.AddStringCheckIsOk("-10");
            Assert.IsFalse(okToAdd);

            //By Selection
            SetParameters(ref helper, "12.0", 1, 0, 1);
            okToAdd = helper.AddStringCheckIsOk("-1");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "-12.0", 1, 1, 1);
            okToAdd = helper.AddStringCheckIsOk("-1");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "12.0", 1, 1, 2);
            okToAdd = helper.AddStringCheckIsOk("-");
            Assert.IsFalse(okToAdd);

            SetParameters(ref helper, "-12.0", 1, 0, 2);
            okToAdd = helper.AddStringCheckIsOk("-13");
            Assert.IsTrue(okToAdd);

            SetParameters(ref helper, "-12.0", 1, 1, 2);
            okToAdd = helper.AddStringCheckIsOk("-13");
            Assert.IsFalse(okToAdd);

        }

        [TestMethod]
        public void TestDifferentCultureInfo()
        {
            var helper = new NumericalTextBoxHelper();

            var parsed = false;
            double value;
            var currentCulture = Thread.CurrentThread.CurrentCulture;

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            parsed = helper.TryParseAsDouble("1.0", out value);
            Assert.IsTrue(parsed);
            Assert.AreEqual(value, 1.0);

            parsed = double.TryParse("1.0", out value);
            Assert.IsTrue(parsed);
            Assert.AreEqual(value, 1.0);

            Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-BE") {NumberFormat = {NumberGroupSeparator = "."}};
            //to simulate 1.0 translate to 10 (as the default NumberGroupSeparator for "nl-BE" is " ")

            parsed = helper.TryParseAsDouble("1.0", out value);
            Assert.IsTrue(parsed);
            Assert.AreEqual(value, 1.0);

            parsed = double.TryParse("1.0", out value);
            Assert.IsTrue(parsed);
            Assert.AreEqual(value, 10);

            Thread.CurrentThread.CurrentCulture = currentCulture;
        }
    }
}
