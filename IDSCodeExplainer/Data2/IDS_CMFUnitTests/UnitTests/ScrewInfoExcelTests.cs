using IDS.CMF.Quality;
using IDS.CMF;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.TestLib.Utilities;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class ScrewInfoExcelTests
    {
        [TestMethod]
        public void FormatImplantTypeString_ValidInput_ReturnsFormattedString()
        {
            // Arrange
            var director = CMFImplantDirectorUtilities.CreateHeadlessCMFImplantDirector();
            var implantScrewWriter = new ImplantScrewTableExcelSheetWriter(director, new List<string>());
            var implantTypes = new List<string>
            {
                "Implant 1_Alpha",
                "Implant 2_Beta",
                "Implant 3_Alpha"
            };

            // Act
            var result = implantScrewWriter.FormatImplantTypeString(implantTypes);

            // Assert
            Assert.AreEqual("Alpha & Beta (Implant 1-3)", result);
        }

        [TestMethod]
        public void FormatImplantTypeString_EmptyInput_ReturnsEmptyString()
        {
            // Arrange
            var director = CMFImplantDirectorUtilities.CreateHeadlessCMFImplantDirector();
            var implantScrewWriter = new ImplantScrewTableExcelSheetWriter(director, new List<string>());
            var implantTypes = new List<string>();

            // Act
            var result = implantScrewWriter.FormatImplantTypeString(implantTypes);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }
    }
}
