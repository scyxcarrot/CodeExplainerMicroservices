using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.Utilities;
using IDS.Core.V2.Geometries;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Testing.UnitTests
{
#if (Rhino7Installed)

    public static class ImplantScrewTestUtilities
    {
        const string ImplantType = "Lefort";
        const string ScrewType = "Matrix Orthognathic Ø1.85";
        const int CaseNum = 1;

        public static Screw CreateScrew(IDSPoint3D testPoint, Transform plannedPartTransform, bool skipOsteotomy = false,
            string plannedPartName = "05GEN", string originalPartName = "01GEN", bool slantedOsteotomy = false)
        {
            return CreateScrew(testPoint, plannedPartTransform, out _, skipOsteotomy, 
                plannedPartName, originalPartName, slantedOsteotomy);
        }

        public static Screw CreateScrew(IDSPoint3D testPoint, Transform plannedPartTransform, out CMFImplantDirector director, bool skipOsteotomy = false,
            string plannedPartName = "05GEN", string originalPartName = "01GEN", bool slantedOsteotomy = false)
        {
            CreateDirectorAndImplantPreferenceDataModel(plannedPartTransform, out director,
                out var implantPreferenceDataModels, skipOsteotomy, plannedPartName, originalPartName, slantedOsteotomy);

            var implantPreferenceDataModel = implantPreferenceDataModels[0];
            var pastilleDiameter = implantPreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = implantPreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = implantPreferenceDataModel.CasePrefData.PlateWidthMm;

            var direction = RhinoVector3dConverter.ToVector3d(new IDSVector3D(1.0, 0.0, 0.0));
            var randomPoint = new IDSPoint3D(2.0, 2.1673, 10.2956);
            var dotTest = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(testPoint),
                direction, plateThickness, pastilleDiameter);
            var dotB = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(randomPoint),
                direction, plateThickness, pastilleDiameter);

            var connections = new List<IConnection>()
            {
                ImplantCreationUtilities.CreateConnection(dotTest, dotB, plateThickness, plateWidth, true),
            };
            implantPreferenceDataModel.ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, implantPreferenceDataModel);

            return (Screw)director.Document.Objects.Find(dotTest.Screw.Id);
        }

        public static List<Screw> CreateMultipleScrews(List<IDSPoint3D> testPoints, Transform plannedPartTransform, bool skipOsteotomy = false,
            string plannedPartName = "05GEN", string originalPartName = "01GEN", bool slantedOsteotomy = false)
        {
            CreateDirectorAndImplantPreferenceDataModel(plannedPartTransform, out var director,
                out var implantPreferenceDataModels, skipOsteotomy, plannedPartName, originalPartName, slantedOsteotomy);

            var implantPreferenceDataModel = implantPreferenceDataModels[0];
            var pastilleDiameter = implantPreferenceDataModel.CasePrefData.PastilleDiameter;
            var plateThickness = implantPreferenceDataModel.CasePrefData.PlateThicknessMm;
            var plateWidth = implantPreferenceDataModel.CasePrefData.PlateWidthMm;

            var direction = RhinoVector3dConverter.ToVector3d(new IDSVector3D(1.0, 0.0, 0.0));
            var randomPoint = new IDSPoint3D(2.0, 2.1673, 10.2956);

            DotPastille dotTest = new DotPastille(); 
            List<Screw> outputScrews = new List<Screw>();
            List<IConnection> connections = new List<IConnection>();

            foreach (var testPoint in testPoints)
            {
                dotTest = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(testPoint),
                    direction, plateThickness, pastilleDiameter);
                var dotB = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(randomPoint),
                    direction, plateThickness, pastilleDiameter);

                connections.Add(
                    ImplantCreationUtilities.CreateConnection(dotTest, dotB, plateThickness, plateWidth, true));
            }

            implantPreferenceDataModel.ImplantDataModel.Update(connections);

            var screwCreator = new ScrewCreator(director);
            screwCreator.CreateAllScrewBuildingBlock(true, implantPreferenceDataModel);

            connections.ForEach(c =>
            {
                if (c.A is DotPastille dotPastille)
                {
                    outputScrews.Add((Screw)director.Document.Objects.Find(dotPastille.Screw.Id));
                }
            });

            return outputScrews;
        }

        public static List<Screw> CreateMultipleScrewsAndImplants(List<IDSPoint3D> testPoints, Transform plannedPartTransform, 
            int numberOfImplantPreferenceDataModelToMake, bool skipOsteotomy = false,
            string plannedPartName = "05GEN", string originalPartName = "01GEN", bool slantedOsteotomy = false)
        {
            CreateDirectorAndImplantPreferenceDataModel(plannedPartTransform, out var director,
                out var implantPreferenceDataModels, skipOsteotomy, plannedPartName, originalPartName, slantedOsteotomy, numberOfImplantPreferenceDataModelToMake);
            
            List<Screw> outputScrews = new List<Screw>();

            var index = 0;
            foreach (ImplantPreferenceModel implantPreferenceDataModel in implantPreferenceDataModels)
            {
                var pastilleDiameter = implantPreferenceDataModel.CasePrefData.PastilleDiameter;
                var plateThickness = implantPreferenceDataModel.CasePrefData.PlateThicknessMm;
                var plateWidth = implantPreferenceDataModel.CasePrefData.PlateWidthMm;

                var direction = RhinoVector3dConverter.ToVector3d(new IDSVector3D(1.0, 0.0, 0.0));
                

                DotPastille dotTest = new DotPastille();
                
                List<IConnection> connections = new List<IConnection>();
                var randomPoint = new IDSPoint3D(2.0, 2.1673, 10.2956);
                dotTest = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(testPoints[index]),
                    direction, plateThickness, pastilleDiameter);
                var dotB = DataModelUtilities.CreateDotPastille(RhinoPoint3dConverter.ToPoint3d(randomPoint),
                    direction, plateThickness, pastilleDiameter);

                connections.Add(
                    ImplantCreationUtilities.CreateConnection(dotTest, dotB, plateThickness, plateWidth, true));

                implantPreferenceDataModel.ImplantDataModel.Update(connections);

                var screwCreator = new ScrewCreator(director);
                screwCreator.CreateAllScrewBuildingBlock(true, implantPreferenceDataModel);

                outputScrews.Add((Screw)director.Document.Objects.Find(dotTest.Screw.Id));
                index++;
            }

            return outputScrews;
        }

        public static void CreateDirectorAndImplantPreferenceDataModel(Transform plannedPartTransform,
            out CMFImplantDirector director, out List<ImplantPreferenceModel> implantPreferenceDataModels,
            bool skipOsteotomy = false,
            string plannedPartName = "05GEN", string originalPartName = "01GEN", bool slantedOsteotomy = false, 
            int numberOfImplantPreferenceDataModelToMake = 1)
        {
            director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes, ESurgeryType.Orthognathic);
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();

            var preOpPartName = "00MAN_comp";
            var osteotomyPartName = "01Geniocut";

            var resource = new TestResources();

            StlUtilities.StlBinary2RhinoMesh(resource.BoneStlFilePath, out var preOpMesh);
            var preOpBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(preOpPartName);
            objectManager.AddNewBuildingBlockWithTransform(preOpBlock, preOpMesh, Transform.Identity);

            var originalMesh = preOpMesh.DuplicateMesh();
            var originalBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(originalPartName);
            objectManager.AddNewBuildingBlockWithTransform(originalBlock, originalMesh, Transform.Identity);

            var plannedMesh = originalMesh.DuplicateMesh();
            plannedMesh.Transform(plannedPartTransform);
            var plannedBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(plannedPartName);
            objectManager.AddNewBuildingBlockWithTransform(plannedBlock, plannedMesh, plannedPartTransform);

            if (!skipOsteotomy)
            {
                StlUtilities.StlBinary2RhinoMesh(slantedOsteotomy ? resource.OsteotomySlantedStlFilePath : resource.OsteotomyStlFilePath, out var osteotomyMesh);
                var osteotomyBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(osteotomyPartName);
                objectManager.AddNewBuildingBlockWithTransform(osteotomyBlock, osteotomyMesh, Transform.Identity);
            }

            ProPlanImportUtilities.RegenerateImplantSupportGuidingOutlines(objectManager);

            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            implantPreferenceDataModels = new List<ImplantPreferenceModel>();

            for (int i = 0; i < numberOfImplantPreferenceDataModelToMake; i++)
            {
                ImplantPreferenceModel implantPreferenceDataModel = casePreferencesHelper.AddNewImplantCase();
                CasePreferencesDataModelHelper.ConfigureImplantCase(implantPreferenceDataModel, "Lefort", "Matrix Orthognathic Ø1.85", i+1);

                var implantComponent = new ImplantCaseComponent();
                var supportMesh = plannedMesh.DuplicateMesh();
                var implantSupportBb = implantComponent.GetImplantBuildingBlock(IBB.ImplantSupport, implantPreferenceDataModel);
                objectManager.AddNewBuildingBlock(implantSupportBb, supportMesh);

                implantPreferenceDataModels.Add(implantPreferenceDataModel);
            }
            
        }

        public static List<Screw> CreateImplantScrewWithPoints(out CMFImplantDirector director, IEnumerable<(IDSPoint3D, IDSPoint3D)> screwPairs,
           out ImplantPreferenceModel implantPreferenceDataModel)
        {
            InstantiateClasses(out director, out implantPreferenceDataModel, out var objectManager, out var implantScrewBb, out var screwAideDict);
            var screws = new List<Screw>();

            var index = 1;
            foreach (var screwPair in screwPairs)
            {
                var screwHeadPt = RhinoPoint3dConverter.ToPoint3d(screwPair.Item1);
                var screwTipPt = RhinoPoint3dConverter.ToPoint3d(screwPair.Item2);

                var screw = new Screw(director, screwHeadPt, screwTipPt, screwAideDict, index, ScrewType);
                objectManager.AddNewBuildingBlock(implantScrewBb, screw);
                screws.Add(screw);

                index += 1;
            }

            return screws;
        }

        private static void InstantiateClasses(out CMFImplantDirector director, out ImplantPreferenceModel implantPreferenceDataModel,
            out CMFObjectManager objectManager, out ExtendedImplantBuildingBlock implantScrewBb, out Dictionary<string, GeometryBase> screwAideDict)
        {
            director = ImplantDirectorHelper.CreateActualCMFImplantDirector(EScrewBrand.Synthes,
                ESurgeryType.Orthognathic);
            var casePreferencesHelper = new CasePreferencesDataModelHelper(director);
            implantPreferenceDataModel = casePreferencesHelper.AddNewImplantCase();
            CasePreferencesDataModelHelper.ConfigureImplantCase(implantPreferenceDataModel, ImplantType, ScrewType, CaseNum);

            objectManager = new CMFObjectManager(director);
            var implantCaseComponent = new ImplantCaseComponent();
            implantScrewBb =
                implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, implantPreferenceDataModel);
            screwAideDict = implantPreferenceDataModel.ScrewAideData.GenerateScrewAideDictionary();
        }
    }

#endif
}
