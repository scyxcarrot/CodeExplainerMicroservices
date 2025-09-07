using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IDS.CMF.ImplantProposal;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.MTLS.Operation;
using IDS.Testing.UnitTests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace IDS_CMFUnitTests.UnitTests
{
    [TestClass]
    public class AutoImplantTests
    {
        private static AutoImplantProposalResult GetMockedAutoImplantProposalResult()
        {
            return new AutoImplantProposalResult
            {
                LinkConnections = new long[,]
                {
                    {1, 2},
                    {2, 3},
                    {4, 5},
                    {5, 6}
                },
                PlateConnections = new long[,]
                {
                    {1, 4},
                    {2, 5},
                    {3, 6}
                },
                ScrewHeads = new double[,]
                {
                    {81.306788201515317, 22.82289903167241, 40.696261932792609},
                    {88.8170827877474, 20.446118089917945, 41.698422851318639},
                    {95.827395578298919, 22.101623284207324, 40.603759439175519},
                    {81.1237954494706, 17.052362264692686, 28.785145317726485},
                    {89.42907805267609, 15.702463878354846, 34.153289438625393},
                    {97.079457071201787, 16.995406164127211, 28.592380850223108}
                },
                ScrewIssues = new byte[]
                {
                    0, 0, 0, 0, 0, 0
                },
                ScrewNumbers = new long[]
                {
                    1, 2, 3, 4, 5, 6

                },
                ScrewTips = new double[,]
                {
                    { 82.538047331320271, 28.661923974949794, 40.071931357651138 },
                    { 88.754115292797124, 26.367147246338519, 40.730203442611504 },
                    { 94.630020360179458, 27.918883729985634, 39.75183603122354 },
                    { 82.600260115104575, 22.384896862952676, 26.4647731817331 },
                    { 89.430789556964527, 20.800092091205926, 30.988769937295409 },
                    { 96.554325545858688, 22.218201102968031, 25.686068769955483 }
                }
            };
        }

        [TestMethod]
        public void Check_AutoImplant_Links_Plate_Pastille_Tests()
        {
            const string implantType = "Genioplasty";
            const string screwType = "Matrix Orthognathic Ø1.85";
            const int caseNum = 1;
            CasePreferencesDataModelHelper.CreateSingleSimpleImplantCaseWithBoneAndSupport(EScrewBrand.Synthes, ESurgeryType.Orthognathic, implantType,
                screwType, caseNum, out var director, out var implantPreferenceModel);
            implantPreferenceModel.CasePrefData.LinkWidthMm = 1;
            implantPreferenceModel.CasePrefData.PlateThicknessMm = 1;
            implantPreferenceModel.CasePrefData.PlateWidthMm = 1;
            implantPreferenceModel.CasePrefData.PastilleDiameter = 1;

            var implantProposalOutput = new ImplantProposalOutput(director);
            implantProposalOutput.CreateScrewsAndDotPastilles(GetMockedAutoImplantProposalResult(), ref implantPreferenceModel);

            var implantDataModel = implantPreferenceModel.ImplantDataModel;
            Assert.AreEqual(implantDataModel.DotList.Count, 6, "Incorrect amount of DotPastille generated!");
            Assert.AreEqual(implantDataModel.ConnectionList.FindAll(c => c is ConnectionLink).Count, 4, "Incorrect amount of Link Connections!");
            Assert.AreEqual(implantDataModel.ConnectionList.FindAll(c => c is ConnectionPlate).Count, 3, "Incorrect amount of Plate Connections!");
        }
    }
}
