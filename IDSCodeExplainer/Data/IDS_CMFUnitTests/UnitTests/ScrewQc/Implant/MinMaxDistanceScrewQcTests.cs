using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Quality;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Testing.UnitTests
{
    [TestClass]
    public class MinMaxDistanceScrewQcTests
    {
        [TestMethod]
        public void Get_Connected_Plate_Screws_Test()
        {
            // arrange
            GetSampleCase(out var director, out var casePreferenceDataModel);

            var pastilleDiameter = casePreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = casePreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = casePreferenceDataModel.CasePrefData.PlateWidthMm;

            var zAxis = RhinoVector3dConverter.ToVector3d(new IDSVector3D(0, 0, 1));
            var dotA = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(1, 1, 0)), zAxis, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, 0, 0)), zAxis, plateThickness, pastilleDiameter);
            var dotC = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-1, -1, 0)), zAxis, plateThickness, pastilleDiameter);

            var connections = new List<IConnection>()
            {
                ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth * 0.5, true),
                ImplantCreationUtilities.CreateConnection(dotB, dotC, plateThickness, plateWidth, true)
            };
            ((ImplantPreferenceModel)casePreferenceDataModel).ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, casePreferenceDataModel);

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);
            var dotPastilleScrewMap = new Dictionary<DotPastille, Screw>();
            foreach (var screw in screws)
            {
                dotPastilleScrewMap.Add(ImplantCreationUtilities.GetDotPastille(screw), screw);
            }

            //act
            var screwAnalysis = new CMFScrewAnalysis(director);
            var actualConnectedScrews = TestUtilities.RunPrivateMethod<CMFScrewAnalysis, Dictionary<Screw, List<Screw>>>(screwAnalysis, 
                "GetConnectedWithPlateScrews", casePreferenceDataModel, screws, Type.Missing);

            //assert
            var expectedConnectedScrews = new Dictionary<Screw, List<Screw>>
            {
                {dotPastilleScrewMap[dotA], new List<Screw>() {dotPastilleScrewMap[dotB]}},
                {dotPastilleScrewMap[dotB], new List<Screw>() {dotPastilleScrewMap[dotA], dotPastilleScrewMap[dotC]}},
                {dotPastilleScrewMap[dotC], new List<Screw>() {dotPastilleScrewMap[dotB]}}
            };

            foreach (var actualConnectedScrew in actualConnectedScrews)
            {
                CollectionAssert.AreEquivalent(expectedConnectedScrews[actualConnectedScrew.Key], actualConnectedScrew.Value);
            }
        }

        [TestMethod]
        public void Perform_Max_Distance_Test()
        {
            // arrange
            GetSampleCase(out var director, out var casePreferenceDataModel);

            var pastilleDiameter = casePreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = casePreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = casePreferenceDataModel.CasePrefData.PlateWidthMm;

            var zAxis = RhinoVector3dConverter.ToVector3d(new IDSVector3D(0, 0, 1));
            var dotA = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(1, 1, 0)),
                zAxis, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, 0, 0)), 
                zAxis, plateThickness, pastilleDiameter);
            var dotC = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-1, -1, 0)), 
                zAxis, plateThickness, pastilleDiameter);

            var con1 = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth * 0.5, true);
            var con2 = ImplantCreationUtilities.CreateConnection(dotB, dotC, plateThickness, plateWidth, true);

            var connections = new List<IConnection>()
            {
                con1,
                con2
            };

            ((ImplantPreferenceModel)casePreferenceDataModel).ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, casePreferenceDataModel);

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);

            var dotPastilleScrewMap = new Dictionary<DotPastille, Screw>();
            foreach (var screw in screws)
            {
                dotPastilleScrewMap.Add(ImplantCreationUtilities.GetDotPastille(screw), screw);
            }

            //act
            var screwAnalysis = new CMFScrewAnalysis(director);
            var acceptanceMaxDistanceCon1 = TestUtilities.RunPrivateMethod<CMFScrewAnalysis, double>(screwAnalysis,
                "GetAcceptableMaxDistanceBetweenTwoScrew", dotPastilleScrewMap[dotA], dotPastilleScrewMap[dotB], 
                director.CasePrefManager.SurgeryInformation.ScrewBrand);

            var acceptanceMaxDistanceCon2 = TestUtilities.RunPrivateMethod<CMFScrewAnalysis, double>(screwAnalysis,
                "GetAcceptableMaxDistanceBetweenTwoScrew", dotPastilleScrewMap[dotB], dotPastilleScrewMap[dotC],
                director.CasePrefManager.SurgeryInformation.ScrewBrand);

            //assert
            /*
             * Con1:   15.62*((1)^2)*((2.6 * 0.5)-0.2) = 17.182
             * Con2:   15.62*((1)^2)*(2.6-0.2) = 37.488
             */
            Assert.IsTrue(Math.Abs(acceptanceMaxDistanceCon1 - 17.182) < 0.001);
            Assert.IsTrue(Math.Abs(acceptanceMaxDistanceCon2 - 37.488) < 0.001);
        }

        [TestMethod]
        public void Perform_Min_Max_Distance_Test()
        {
            // arrange
            GetSampleCaseWithLoDLowGeneration(out var director, out var casePreferenceDataModel);

            var pastilleDiameter = casePreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = casePreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = casePreferenceDataModel.CasePrefData.PlateWidthMm;

            //assert
            /*
             * ScrewDistanceMin :   3.5,
             * ScrewDistanceMax :   15.62*T*T*((W-0.2))
             * Con1:   ScrewDistanceMax > 15.62*((1)^2)*((2.6 * 0.5)-0.2) = 17.182         
             * Con2:   ScrewDistanceMax > 15.62*((1)^2)*(2.6-0.2) = 37.488                 
             * Con3:   ScrewDistanceMin < 3.5                                              
             */

            var zAxis = RhinoVector3dConverter.ToVector3d(new IDSVector3D(0, 0, 1));
            GetXYByRadiusAndAngle(18, 45, out var x, out var y);
            var dotA = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(x, y, 0)),
                zAxis, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, 0, 0)),
                zAxis, plateThickness, pastilleDiameter);
            GetXYByRadiusAndAngle(37, 225, out x, out y);
            var dotC = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(x, y, 0)),
                zAxis, plateThickness, pastilleDiameter);
            GetXYByRadiusAndAngle(2, 225, out var x1, out var y1);
            var dotD = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(x + x1, y + y1, 0)),
                zAxis, plateThickness, pastilleDiameter);

            /*
             * Con1:   18(Distance) > 17.182(ScrewDistanceMax) : Trigger Max Distance
             * Con2:   37(Distance) < 37.488(ScrewDistanceMax) : Nothing
             * Con3:   2(Distance)  < 3.5(ScrewDistanceMin)    : Trigger Min Distance
             */
            var con1 = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth * 0.5, true);
            var con2 = ImplantCreationUtilities.CreateConnection(dotB, dotC, plateThickness, plateWidth, true);
            var con3 = ImplantCreationUtilities.CreateConnection(dotC, dotD, plateThickness, plateWidth, true);

            var connections = new List<IConnection>()
            {
                con1,
                con2,
                con3
            };

            ((ImplantPreferenceModel)casePreferenceDataModel).ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, casePreferenceDataModel);

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);

            var count = 1;
            foreach (var screw in screws)
            {
                screw.Index = count++;
            }

            //act
            var minMaxDistanceChecker = new MinMaxDistancesChecker(director);
            var minMaxDistanceContentList = new List<MinMaxDistanceContent>();
            foreach (Screw screw in screws)
            {
                var minMaxDistanceContent = minMaxDistanceChecker.MinMaxDistanceCheck(screw);
                minMaxDistanceContentList.Add(minMaxDistanceContent);
            }

            var expectedTooCloseScrewsCountValues = new List<int> { 1, 1, 0, 0 };
            var expectedTooFarScrewsCountValues = new List<int> { 0, 0, 1, 1 };
            var expectedTooCloseScrewsGuids = new List<List<Guid>>
            {
                new List<Guid>{screws[1].Id},
                new List<Guid>{screws[0].Id},
                new List<Guid>(),
                new List<Guid>()
            };
            var expectedTooFarScrewsGuids = new List<List<Guid>>
            {
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid>{screws[3].Id},
                new List<Guid>{screws[2].Id},
            };

            // assert
            AssertMinMaxContentList(minMaxDistanceContentList, expectedTooCloseScrewsCountValues,
                expectedTooFarScrewsCountValues, expectedTooCloseScrewsGuids, expectedTooFarScrewsGuids);
        }

        [TestMethod]
        public void Perform_Default_Min_Distance_Tests()
        {
            //Perform variation of Min Distance tests
            //1. Default: Screws_On_Same_Bone_With_Distance_Less_Than_AcceptableMinDistance_Will_Be_Reported (A-B for Plate; A-C for Link; A-F for not connected)
            //2. Default: Screws_On_Same_Bone_With_Distance_More_Than_AcceptableMinDistance_Will_Not_Be_Reported (A-D for Plate; A-E for Link; A-G for not connected)

            // arrange
            GetSampleCaseWithLoDLowGeneration(out var director, out var casePreferenceDataModel);

            var pastilleDiameter = casePreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = casePreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = casePreferenceDataModel.CasePrefData.PlateWidthMm;
            var linkWidth = casePreferenceDataModel.CasePrefData.LinkWidthMm;

            var acceptableMinDistance = CasePreferencesHelper.GetAcceptableMinScrewDistance(director.CasePrefManager.SurgeryInformation.ScrewBrand, casePreferenceDataModel.CasePrefData.ImplantTypeValue);
            var acceptableMaxDistance = CasePreferencesHelper.GetAcceptableMaxScrewDistance(director.CasePrefManager.SurgeryInformation.ScrewBrand, casePreferenceDataModel.CasePrefData.ImplantTypeValue, plateThickness, plateWidth);
            Assert.IsTrue(acceptableMaxDistance > acceptableMinDistance);

            var pointA = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, 0, 0));
            var pointB = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(acceptableMinDistance - 0.1, 0, 0));
            var pointC = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, acceptableMinDistance - 0.1, 0));
            var pointD = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(acceptableMaxDistance - 0.1, 0, 0));
            var pointE = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, acceptableMaxDistance - 0.1, 0));
            var pointF = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-acceptableMinDistance + 0.1, 0, 0));
            var pointG = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, -acceptableMaxDistance + 0.1, 0));

            var zAxis = RhinoVector3dConverter.ToVector3d(new IDSVector3D(0, 0, 1));

            var dotA = DataModelUtilities.CreateDotPastille(pointA, zAxis, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(pointB, zAxis, plateThickness, pastilleDiameter);
            var dotC = DataModelUtilities.CreateDotPastille(pointC, zAxis, plateThickness, pastilleDiameter);
            var dotD = DataModelUtilities.CreateDotPastille(pointD, zAxis, plateThickness, pastilleDiameter);
            var dotE = DataModelUtilities.CreateDotPastille(pointE, zAxis, plateThickness, pastilleDiameter);
            var dotF = DataModelUtilities.CreateDotPastille(pointF, zAxis, plateThickness, pastilleDiameter);
            var dotG = DataModelUtilities.CreateDotPastille(pointG, zAxis, plateThickness, pastilleDiameter);

            var conAB = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth, true);
            var conAC = ImplantCreationUtilities.CreateConnection(dotA, dotC, plateThickness, linkWidth, false);
            var conAD = ImplantCreationUtilities.CreateConnection(dotA, dotD, plateThickness, plateWidth, true);
            var conAE = ImplantCreationUtilities.CreateConnection(dotA, dotE, plateThickness, linkWidth, false);
            var conDF = ImplantCreationUtilities.CreateConnection(dotD, dotF, plateThickness, plateWidth, true);
            var conEG = ImplantCreationUtilities.CreateConnection(dotE, dotG, plateThickness, plateWidth, true);

            var connections = new List<IConnection>()
            {
                conAB,
                conAC,
                conAD,
                conAE,
                conDF,
                conEG
            };

            ((ImplantPreferenceModel)casePreferenceDataModel).ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, casePreferenceDataModel);

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);

            var count = 1;
            foreach (var screw in screws)
            {
                screw.Index = count++;
            }

            //act
            var minMaxDistanceChecker = new MinMaxDistancesChecker(director);
            var minMaxDistanceContentList = new List<MinMaxDistanceContent>();

            foreach (Screw screw in screws)
            {
                var minMaxDistanceContent = minMaxDistanceChecker.MinMaxDistanceCheck(screw);
                minMaxDistanceContentList.Add(minMaxDistanceContent);
            }

            var expectedTooCloseScrewsCountValues = new List<int> { 0, 1, 1, 1, 0, 0, 3 };
            var expectedTooFarScrewsCountValues = new List<int> { 1, 1, 0, 0, 1, 1, 0 };
            var expectedTooCloseScrewsGuids = new List<List<Guid>> 
            { 
                new List<Guid> { Guid.Empty },
                new List<Guid> {screws[6].Id}, 
                new List<Guid> {screws[6].Id}, 
                new List<Guid> {screws[6].Id},
                new List<Guid> { Guid.Empty },
                new List<Guid> { Guid.Empty },
                new List<Guid> { screws[1].Id, screws[2].Id, screws[3].Id }
            };

            var expectedTooFarScrewsGuids = new List<List<Guid>> 
            {
                new List<Guid> {screws[4].Id},
                new List<Guid> {screws[5].Id},
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid> {screws[0].Id},
                new List<Guid> {screws[1].Id},
                new List<Guid>()
            };

            // assert
            AssertMinMaxContentList(minMaxDistanceContentList, expectedTooCloseScrewsCountValues,
                expectedTooFarScrewsCountValues, expectedTooCloseScrewsGuids, expectedTooFarScrewsGuids);
        }

        [TestMethod]
        public void Perform_Different_Bones_Min_Distance_Tests()
        {
            //Perform variation of Min Distance tests
            //1. Diff Bone: Screws_On_Different_Bone_Connected_By_Plate_With_Distance_Less_Than_AcceptableMinDistance_Will_Be_Reported (A-B)
            //2. Diff Bone: Screws_On_Different_Bone_Connected_By_Link_With_Distance_Less_Than_AcceptableMinDistance_Will_Not_Be_Reported (A-C)
            //3. Diff Bone: Screws_On_Different_Bone_With_Distance_More_Than_AcceptableMinDistance_Will_Not_Be_Reported (A-D for Plate; A-E for Link)

            // arrange
            GetSampleCaseWithLoDLowGeneration(out var director, out var casePreferenceDataModel, true);

            var pastilleDiameter = casePreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = casePreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = casePreferenceDataModel.CasePrefData.PlateWidthMm;
            var linkWidth = casePreferenceDataModel.CasePrefData.LinkWidthMm;

            var acceptableMinDistance = CasePreferencesHelper.GetAcceptableMinScrewDistance(director.CasePrefManager.SurgeryInformation.ScrewBrand, casePreferenceDataModel.CasePrefData.ImplantTypeValue);

            var pointA = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-1, -1, 0));
            var pointB = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-2 + acceptableMinDistance, -1, 0));
            var pointC = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-1.5 + acceptableMinDistance, -1, 0));
            var pointD = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(acceptableMinDistance, -1, 0));
            var pointE = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-0.5 +acceptableMinDistance, -1, 0));
            
            var zAxis = RhinoVector3dConverter.ToVector3d(new IDSVector3D(0, 0, 1));

            var dotA = DataModelUtilities.CreateDotPastille(pointA, zAxis, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(pointB, zAxis, plateThickness, pastilleDiameter);
            var dotC = DataModelUtilities.CreateDotPastille(pointC, zAxis, plateThickness, pastilleDiameter);
            var dotD = DataModelUtilities.CreateDotPastille(pointD, zAxis, plateThickness, pastilleDiameter);
            var dotE = DataModelUtilities.CreateDotPastille(pointE, zAxis, plateThickness, pastilleDiameter);

            var conAB = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth, true);
            var conAC = ImplantCreationUtilities.CreateConnection(dotA, dotC, plateThickness, linkWidth, false);
            var conAD = ImplantCreationUtilities.CreateConnection(dotA, dotD, plateThickness, plateWidth, true);
            var conAE = ImplantCreationUtilities.CreateConnection(dotA, dotE, plateThickness, linkWidth, false);

            var connections = new List<IConnection>()
            {
                conAB,
                conAC,
                conAD,
                conAE
            };

            ((ImplantPreferenceModel)casePreferenceDataModel).ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, casePreferenceDataModel);

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);

            var count = 1;
            foreach (var screw in screws)
            {
                screw.Index = count++;
            }

            //act
            var minMaxDistanceChecker = new MinMaxDistancesChecker(director);
            var minMaxDistanceContentList = new List<MinMaxDistanceContent>();

            foreach (Screw screw in screws)
            {
                var minMaxDistanceContent = minMaxDistanceChecker.MinMaxDistanceCheck(screw);
                minMaxDistanceContentList.Add(minMaxDistanceContent);
            }

            var expectedTooCloseScrewsCountValues = new List<int> { 3, 3, 3, 4, 1 };
            var expectedTooFarScrewsCountValues = new List<int> { 0, 0, 0, 0, 0 };
            var expectedTooCloseScrewsGuids = new List<List<Guid>>
            {
                new List<Guid> { screws[1].Id, screws[2].Id, screws[3].Id },
                new List<Guid> { screws[0].Id, screws[2].Id, screws[3].Id },
                new List<Guid> { screws[0].Id, screws[1].Id, screws[3].Id },
                new List<Guid> { screws[0].Id, screws[1].Id, screws[2].Id, screws[4].Id },
                new List<Guid> { screws[3].Id },
            };

            var expectedTooFarScrewsGuids = new List<List<Guid>>
            {
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid>()
            };

            // assert
            AssertMinMaxContentList(minMaxDistanceContentList, expectedTooCloseScrewsCountValues,
                expectedTooFarScrewsCountValues, expectedTooCloseScrewsGuids, expectedTooFarScrewsGuids);
        }

        [TestMethod]
        public void Perform_Default_Max_Distance_Tests()
        {
            //Perform variation of Max Distance tests
            //1. Default: Screws_Connected_By_Plate_With_Distance_Less_Than_AcceptableMaxDistance_Will_Not_Be_Reported (A-B)
            //2. Default: Screws_Connected_By_Plate_With_Distance_More_Than_AcceptableMaxDistance_Will_Be_Reported (A-C)
            //3. Default: Screws_Connected_By_Link_With_Distance_More_Than_AcceptableMaxDistance_Will_Not_Be_Reported (A-D)
            //4. Multi-parts: Screw_Connected_By_MultiPlates_With_Largest_Distance_More_Than_AcceptableMaxDistance_Will_Be_Reported (B-E)

            // arrange
            GetSampleCaseWithLoDLowGeneration(out var director, out var casePreferenceDataModel);

            var pastilleDiameter = casePreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = casePreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = casePreferenceDataModel.CasePrefData.PlateWidthMm;
            var linkWidth = casePreferenceDataModel.CasePrefData.LinkWidthMm;

            var acceptableMaxDistance = CasePreferencesHelper.GetAcceptableMaxScrewDistance(director.CasePrefManager.SurgeryInformation.ScrewBrand, casePreferenceDataModel.CasePrefData.ImplantTypeValue, plateThickness, plateWidth);

            var pointA = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, 0, 0));
            var pointB = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(acceptableMaxDistance-0.1, 0, 0));
            var pointC = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(acceptableMaxDistance+0.1, 0, 0));
            var pointD = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(0, acceptableMaxDistance+0.1, 0));
            var pointE = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-0.2, 0, 0));

            var zAxis = RhinoVector3dConverter.ToVector3d(new IDSVector3D(0,0,1));

            var dotA = DataModelUtilities.CreateDotPastille(pointA, zAxis, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(pointB, zAxis, plateThickness, pastilleDiameter);
            var dotC = DataModelUtilities.CreateDotPastille(pointC, zAxis, plateThickness, pastilleDiameter);
            var dotD = DataModelUtilities.CreateDotPastille(pointD, zAxis, plateThickness, pastilleDiameter);
            var dotE = DataModelUtilities.CreateDotPastille(pointE, zAxis, plateThickness, pastilleDiameter);

            var conAB = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidth, true);
            var conAC = ImplantCreationUtilities.CreateConnection(dotA, dotC, plateThickness, plateWidth, true);
            var conAD = ImplantCreationUtilities.CreateConnection(dotA, dotD, plateThickness, linkWidth, false);
            var conBE = ImplantCreationUtilities.CreateConnection(dotB, dotE, plateThickness, plateWidth, true);

            var connections = new List<IConnection>()
            {
                conAB,
                conAC,
                conAD,
                conBE
            };

            ((ImplantPreferenceModel)casePreferenceDataModel).ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, casePreferenceDataModel);

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);

            var count = 1;
            foreach (var screw in screws)
            {
                screw.Index = count++;
            }

            //act
            var minMaxDistanceChecker = new MinMaxDistancesChecker(director);
            var minMaxDistanceContentList = new List<MinMaxDistanceContent>();

            foreach (Screw screw in screws)
            {
                var minMaxDistanceContent = minMaxDistanceChecker.MinMaxDistanceCheck(screw);
                minMaxDistanceContentList.Add(minMaxDistanceContent);
            }

            var expectedTooCloseScrewsCountValues = new List<int> { 1, 0, 1, 1, 1 };
            var expectedTooFarScrewsCountValues = new List<int> { 1, 0, 1, 1, 1 };
            var expectedTooCloseScrewsGuids = new List<List<Guid>>
            {
                new List<Guid> { screws[4].Id },
                new List<Guid>(),
                new List<Guid> { screws[3].Id },
                new List<Guid> { screws[2].Id },
                new List<Guid> { screws[0].Id },
            };

            var expectedTooFarScrewsGuids = new List<List<Guid>>
            {
                new List<Guid> { screws[3].Id },
                new List<Guid>(),
                new List<Guid> { screws[4].Id },
                new List<Guid> { screws[0].Id },
                new List<Guid> { screws[2].Id },
            };

            // assert
            AssertMinMaxContentList(minMaxDistanceContentList, expectedTooCloseScrewsCountValues,
                expectedTooFarScrewsCountValues, expectedTooCloseScrewsGuids, expectedTooFarScrewsGuids);
        }

        [TestMethod]
        public void Perform_Width_Overridden_Max_Distance_Tests()
        {
            //Perform variation of Max Distance tests
            //1. Width override: Screws_Connected_By_Plate_With_Width_Override_With_Distance_Less_Than_AcceptableMaxDistance_Will_Not_Be_Reported (A-B)
            //2. Width override: Screws_Connected_By_Plate_With_Width_Override_With_Distance_More_Than_AcceptableMaxDistance_Will_Be_Reported (A-C)

            // arrange
            GetSampleCaseWithLoDLowGeneration(out var director, out var casePreferenceDataModel);

            var pastilleDiameter = casePreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = casePreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = casePreferenceDataModel.CasePrefData.PlateWidthMm;

            var implant = director.ScrewBrandCasePreferences.Implants.FirstOrDefault(impl => impl.ImplantType == casePreferenceDataModel.CasePrefData.ImplantTypeValue);
            var plateWidthMin = implant.PlateWidthMin;
            var plateWidthMax = implant.PlateWidthMax;

            var acceptableMaxDistance = CasePreferencesHelper.GetAcceptableMaxScrewDistance(director.CasePrefManager.SurgeryInformation.ScrewBrand, casePreferenceDataModel.CasePrefData.ImplantTypeValue, plateThickness, plateWidth);
            var acceptableMaxDistanceForWidthMin = CasePreferencesHelper.GetAcceptableMaxScrewDistance(director.CasePrefManager.SurgeryInformation.ScrewBrand, casePreferenceDataModel.CasePrefData.ImplantTypeValue, plateThickness, plateWidthMin);
            var acceptableMaxDistanceForWidthMax = CasePreferencesHelper.GetAcceptableMaxScrewDistance(director.CasePrefManager.SurgeryInformation.ScrewBrand, casePreferenceDataModel.CasePrefData.ImplantTypeValue, plateThickness, plateWidthMax);

            var pointA = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-49,-49,0));
            var pointB = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-49 + acceptableMaxDistanceForWidthMin - 0.1, -49,0));
            var pointC = RhinoPoint3dConverter.ToPoint3d(new IDSPoint3D(-49 + acceptableMaxDistanceForWidthMax + 0.1, -49,0));

            var zAxis = RhinoVector3dConverter.ToVector3d(new IDSVector3D(0, 0, 1));

            var dotA = DataModelUtilities.CreateDotPastille(pointA, zAxis, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(pointB, zAxis, plateThickness, pastilleDiameter);
            var dotC = DataModelUtilities.CreateDotPastille(pointC, zAxis, plateThickness, pastilleDiameter);

            var conAB = ImplantCreationUtilities.CreateConnection(dotA, dotB, plateThickness, plateWidthMin, true);
            var conAC = ImplantCreationUtilities.CreateConnection(dotA, dotC, plateThickness, plateWidthMax, true);

            var connections = new List<IConnection>()
            {
                conAB,
                conAC
            };

            ((ImplantPreferenceModel)casePreferenceDataModel).ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, casePreferenceDataModel);

            var screwManager = new ScrewManager(director);
            var screws = screwManager.GetAllScrews(false);

            var count = 1;
            foreach (var screw in screws)
            {
                screw.Index = count++;
            }

            //act
            var minMaxDistanceChecker = new MinMaxDistancesChecker(director);
            var minMaxDistanceContentList = new List<MinMaxDistanceContent>();

            foreach (Screw screw in screws)
            {
                var minMaxDistanceContent = minMaxDistanceChecker.MinMaxDistanceCheck(screw);
                minMaxDistanceContentList.Add(minMaxDistanceContent);
            }

            var expectedTooCloseScrewsCountValues = new List<int> { 0, 0, 0 };
            var expectedTooFarScrewsCountValues = new List<int> { 1, 0, 1 };
            var expectedTooCloseScrewsGuids = new List<List<Guid>>
            {
                new List<Guid>(),
                new List<Guid>(),
                new List<Guid>(),
            };

            var expectedTooFarScrewsGuids = new List<List<Guid>>
            {
                new List<Guid> { screws[2].Id },
                new List<Guid>(),
                new List<Guid> { screws[0].Id },
            };

            // assert
            AssertMinMaxContentList(minMaxDistanceContentList, expectedTooCloseScrewsCountValues,
                expectedTooFarScrewsCountValues, expectedTooCloseScrewsGuids, expectedTooFarScrewsGuids);
        }

        #region Helper Method

        public static void GetSampleCase(out CMFImplantDirector director, out CasePreferenceDataModel casePreferenceDataModel, bool addAdditionalBone = false)
        {
            /*
             * ScrewBrand       :   Synthes,
             * SurgeryType      :   Orthognathic,
             * ImplantType      :   Lefort,
             * ScrewType        :   Matrix Orthognathic Ø1.85,
             * PastilleDiameter :   5.2,
             * PlateThickness   :   1.0,
             * PlateWidth       :   2.6,
             * ScrewDistanceMin :   3.5,
             * ScrewDistanceMax :   15.62*T*T*((W-0.2))
             * Normal           :   15.62*((1)^2)*(2.6-0.2) = 37.488
             * Custom           :   15.62*((1)^2)*((2.6 * 0.5)-0.2) = 17.182
             */
            const EScrewBrand screwBrand = EScrewBrand.Synthes;
            const ESurgeryType surgeryType = ESurgeryType.Orthognathic;
            const string implantType = "Lefort";
            const string screwType = "Matrix Orthognathic Ø1.85";
            const int caseNum = 1;

            CasePreferencesDataModelHelper.CreateSingleSimpleImplantCaseWithBoneAndSupport(screwBrand, surgeryType, implantType,
                screwType, caseNum, out director, out var implantPreferenceModel, addAdditionalBone);
            casePreferenceDataModel = implantPreferenceModel;

            // DotPastille on top of implant support
            var pastilleDiameter = implantPreferenceModel.CasePrefData.PastilleDiameter;
            Assert.AreEqual(pastilleDiameter, 5.2);
            var plateThickness = implantPreferenceModel.CasePrefData.PlateThicknessMm;
            Assert.AreEqual(plateThickness, 1.0);
            var plateWidth = implantPreferenceModel.CasePrefData.PlateWidthMm;
            Assert.AreEqual(plateWidth, 2.6);
        }

        private void GetSampleCaseWithLoDLowGeneration(out CMFImplantDirector director, out CasePreferenceDataModel casePreferenceDataModel, bool addAdditionalBone = false)
        {
            GetSampleCase(out director, out casePreferenceDataModel, addAdditionalBone);

            //attempt to generate LoD Low
            GenerateLoDLow(director, "05GEN");
            
            if (addAdditionalBone)
            {
                GenerateLoDLow(director, "05RAM_L");
            }
        }

        private void GenerateLoDLow(CMFImplantDirector director, string partName)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var objectManager = new CMFObjectManager(director);

            var partBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);
            var partId = objectManager.GetBuildingBlockId(partBlock);

            Mesh tmpLowLod = null;
            var i = 0;
            while (tmpLowLod == null && i < 5)
            {
                objectManager.GetBuildingBlockLoDLow(partId, out tmpLowLod);
                i++;
            }
        }

        private void GetXYByRadiusAndAngle(double radius, double angle, out double x, out double y)
        {
            var radian = angle / 180 * Math.PI;
            x = radius * Math.Cos(radian);
            y = radius * Math.Sin(radian);
        }

        private void AssertMinMaxContentList(
            List<MinMaxDistanceContent> minMaxDistanceContentList, 
            List<int> expectedTooCloseScrewsCountValues,
            List<int> expectedTooFarScrewsCountValues,
            List<List<Guid>> expectedTooCloseScrewsGuids,
            List<List<Guid>> expectedTooFarScrewsGuids)
        {
            var index = 0;
            foreach (var minMaxDistanceContent in minMaxDistanceContentList)
            {
                Assert.AreEqual(expectedTooCloseScrewsCountValues[index], minMaxDistanceContent.TooCloseScrews.Count);
                Assert.AreEqual(expectedTooFarScrewsCountValues[index], minMaxDistanceContent.TooFarScrews.Count);

                if (minMaxDistanceContent.TooCloseScrews.Count > 0)
                {
                    var tooCloseScrewsGuids = minMaxDistanceContent.TooCloseScrews.Select(screwInfoRecord => screwInfoRecord.Id).ToList();
                    CollectionAssert.AreEqual(tooCloseScrewsGuids, expectedTooCloseScrewsGuids[index]);
                }

                if (minMaxDistanceContent.TooFarScrews.Count > 0)
                {
                    var tooFarScrewsGuids = minMaxDistanceContent.TooFarScrews.Select(screwInfoRecord => screwInfoRecord.Id).ToList();
                    CollectionAssert.AreEqual(tooFarScrewsGuids, expectedTooFarScrewsGuids[index]);
                }

                index++;
            }
        }
        #endregion
    }
}
