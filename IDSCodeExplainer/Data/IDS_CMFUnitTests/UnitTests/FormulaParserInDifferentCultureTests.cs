using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Threading;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class FormulaParserInDutchBelgiumTests : FormulaParserTests
    {
        [TestInitialize]
        public void TestInitialize_SetupCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("nl-BE");
        }
    }

    [TestClass]
    public class FormulaParserInFrenchFranceTests : FormulaParserTests
    {
        [TestInitialize]
        public void TestInitialize_SetupCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
        }
    }

    [TestClass]
    public class FormulaParserInEnglishMalaysiaTests : FormulaParserTests
    {
        [TestInitialize]
        public void TestInitialize_SetupCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-MY");
        }
    }

    [TestClass]
    public class FormulaParserInEnglishUnitedStatesTests : FormulaParserTests
    {
        [TestInitialize]
        public void TestInitialize_SetupCulture()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
        }
    }
}
