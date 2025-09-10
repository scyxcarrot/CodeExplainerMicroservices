using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Interface.Implant;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class PastilleDeformedScrewQcTests
    {
        [TestMethod]
        public void PastilleDeformedScrewQcCheck_With_Normal_Pastille_Should_Return_IsPastilleDeformed_False()
        {
            GetMockedCase(out var director, out var casePreferenceDataModel, false);

            var screwManager = new ScrewManager(director);
            var screwList = screwManager.GetAllScrews(false);
            var pastilleDeformedChecker = new PastilleDeformedChecker(director);

            foreach (var screw in screwList)
            {
                var pastilleDeformedContent =
                    pastilleDeformedChecker.PerformPastilleDeformedCheck(casePreferenceDataModel, screw.Id);
                Assert.IsFalse(pastilleDeformedContent.IsPastilleDeformed, "Result of PastilleDeformedChecker for Normal Pastille is incorrect!");
            }
        }

        [TestMethod]
        public void PastilleDeformedScrewQcCheck_With_Deformed_Pastille_Should_Return_IsPastilleDeformed_True()
        {
            GetMockedCase(out var director, out var casePreferenceDataModel, true);

            var screwManager = new ScrewManager(director);
            var screwList = screwManager.GetAllScrews(false);
            var pastilleDeformedChecker = new PastilleDeformedChecker(director);

            foreach (var screw in screwList)
            {
                var pastilleDeformedContent =
                    pastilleDeformedChecker.PerformPastilleDeformedCheck(casePreferenceDataModel, screw.Id);
                Assert.IsTrue(pastilleDeformedContent.IsPastilleDeformed, "Result of PastilleDeformedChecker for Deformed Pastille is incorrect!");
            }
        }

        public static void GetMockedCase(out CMFImplantDirector director, out CasePreferenceDataModel casePreferenceDataModel, bool createDeformPastilles)
        {
            const EScrewBrand screwBrand = EScrewBrand.Synthes;
            const ESurgeryType surgeryType = ESurgeryType.Orthognathic;
            const string implantType = "Lefort";
            const string screwType = "Matrix Orthognathic Ø1.85";
            const int caseNum = 1;

            CasePreferencesDataModelHelper.CreateSingleSimpleImplantCaseWithBoneAndSupport(screwBrand, surgeryType, implantType,
                screwType, caseNum, out director, out var implantPreferenceModel);
            casePreferenceDataModel = implantPreferenceModel;

            var pastilleDiameter = implantPreferenceModel.CasePrefData.PastilleDiameter;
            var plateThickness = implantPreferenceModel.CasePrefData.PlateThicknessMm;
            var plateWidth = implantPreferenceModel.CasePrefData.PlateWidthMm;

            var dotA = DataModelUtilities.CreateDotPastille(Point3d.Origin, Vector3d.ZAxis, plateThickness,
                pastilleDiameter);
            dotA.CreationAlgoMethod = DotPastille.CreationAlgoMethods[createDeformPastilles ? 1 : 0];

            var dotB = DataModelUtilities.CreateDotPastille(new Point3d(1, 1, 0), Vector3d.ZAxis, plateThickness,
                pastilleDiameter);
            dotB.CreationAlgoMethod = DotPastille.CreationAlgoMethods[createDeformPastilles ? 1 : 0];

            var con1 = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth, true);
            var connections = new List<IConnection>()
            {
                con1
            };

            implantPreferenceModel.ImplantDataModel.Update(connections);
            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, implantPreferenceModel);
        }
    }
}
