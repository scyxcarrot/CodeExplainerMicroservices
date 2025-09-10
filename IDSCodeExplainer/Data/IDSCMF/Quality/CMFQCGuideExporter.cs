using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Plane = Rhino.Geometry.Plane;

namespace IDS.CMF.Quality
{
    public class CMFQCGuideExporter
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly ScrewLabelTagHelper _screwLabelTagHelper;
        private readonly GuideCaseComponent _guideComponent;
        private readonly ImplantCaseComponent _implantComponent;
        public List<GuidePreferenceDataModel> NotifyUserNeedToPerformManualQprtOnThisActualGuide { get; set; }

        private string FileNameTemplate
        {
            get
            {
                return $"{_director.caseId}_{{0}}_G{{1}}_{{2}}_v{_director.version:D}_draft{_director.draft:D}";
            }
        }

        public CMFQCGuideExporter(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
            _screwLabelTagHelper = new ScrewLabelTagHelper(director);
            _guideComponent = new GuideCaseComponent();
            _implantComponent = new ImplantCaseComponent();
            NotifyUserNeedToPerformManualQprtOnThisActualGuide = new List<GuidePreferenceDataModel>();
        }
        
        public void ExportRegisteredBarrel(string outputDirectory, bool alsoWithEntities)
        {
            var fileNameTemplate = $"{_director.caseId}_{{0}}_I{{1}}_{{2}}_v{_director.version:D}_draft{_director.draft:D}";

            foreach (var casePreferenceData in _director.CasePrefManager.CasePreferences)
            {
                var caseFileNameTemplate = string.Format(fileNameTemplate, casePreferenceData.CasePrefData.ImplantTypeValue, casePreferenceData.NCase, "{0}");
                ExportRegisteredBarrelAndCenterline(casePreferenceData, outputDirectory, caseFileNameTemplate, alsoWithEntities);
            }
        }

        public void ExportGuideFlanges(string outputDirectory)
        {
            foreach (var casePreferenceData in _director.CasePrefManager.GuidePreferences)
            {
                var guideTypeName = GeneralUtilities.CheckForTSGGuideTypeName(
                    _director, casePreferenceData);
                var caseFileNameTemplate = string.Format(FileNameTemplate, guideTypeName, casePreferenceData.NCase, "{0}");

                ExportGuideBuildingBlocks(casePreferenceData, outputDirectory, string.Format(caseFileNameTemplate, "GuideFlange"), IBB.GuideFlange, Colors.GuideFlange);
            }
        }

        public void ExportGuideTeethBlock(string outputDirectory)
        {
            foreach (var casePreferenceData in _director.CasePrefManager.GuidePreferences)
            {
                var guideTypeName = GeneralUtilities.CheckForTSGGuideTypeName(
                    _director, casePreferenceData);
                var caseFileNameTemplate = string.Format(FileNameTemplate, guideTypeName, casePreferenceData.NCase, "{0}");
                ExportGuideBuildingBlocks(casePreferenceData, outputDirectory, string.Format(caseFileNameTemplate, "TeethBlock"), IBB.TeethBlock, Colors.TeethBlock);
            }
        }

        public void ExportGuidePreview(string outputDirectory)
        {
            ExportGuideComponent(outputDirectory, IBB.GuidePreviewSmoothen, "Preview");
        }

        public void ExportGuideActual(string outputDirectory)
        {
            var posfixes = new Dictionary<GuidePreferenceDataModel, string>();
            NotifyUserNeedToPerformManualQprtOnThisActualGuide.ForEach(x =>
            {
                posfixes.Add(x, "_WARNING_DO_MANUAL_QPRT_AND_FURTHER_FIXINGS");
            });

            ExportGuideComponent(outputDirectory, IBB.ActualGuide, "Actual", posfixes);
        }

        public void ExportGuideBaseLightWeighted(string outputDirectory)
        {
            ExportGuideComponent(outputDirectory, IBB.GuideBaseWithLightweight, "GuideWithLightweight");
        }

        private void ExportGuideComponent(string outputDirectory, IBB guideComponentBlock, string componentName)
        {
            ExportGuideComponent(outputDirectory, guideComponentBlock, componentName, new Dictionary<GuidePreferenceDataModel, string>());
        }

        public void ExportGuideComponent(string outputDirectory, IBB guideComponentBlock, string componentName, Dictionary<GuidePreferenceDataModel, string> postFix)
        {
            foreach (var casePreferenceData in _director.CasePrefManager.GuidePreferences)
            {
                var guideTypeName = GeneralUtilities.CheckForTSGGuideTypeName(
                    _director, casePreferenceData);
                var caseFileNameTemplate = string.Format(FileNameTemplate, guideTypeName, casePreferenceData.NCase, "{0}");
                var guideComponentEiBB = _guideComponent.GetGuideBuildingBlock(guideComponentBlock, casePreferenceData);
                var guideComponentMesh = new Mesh();

                if (_objectManager.HasBuildingBlock(guideComponentEiBB))
                {
                    var guideComponentRhObjs = _objectManager.GetAllBuildingBlocks(guideComponentEiBB.Block).ToList();
                    guideComponentRhObjs.ForEach(x =>
                    {
                        var guideComponent = ((Mesh)x.Geometry).DuplicateMesh();
                        guideComponentMesh.Append(guideComponent);
                        guideComponent.Dispose();
                    });

                    var color = CasePreferencesHelper.GetColor(casePreferenceData.NCase);

                    var componentNameWithPostfix = componentName;
                    if (postFix.ContainsKey(casePreferenceData))
                    {
                        componentNameWithPostfix += postFix[casePreferenceData];
                    }

                    ExportStl(guideComponentMesh, outputDirectory, string.Format(caseFileNameTemplate, componentNameWithPostfix), color);
                }
            }
        }

        public void ExportGuideBridges(string outputDirectory)
        {
            var parameters = CMFPreferences.GetActualGuideParameters();

            foreach (var casePreferenceData in _director.CasePrefManager.GuidePreferences)
            {
                var guideBridgeEiBB = _guideComponent.GetGuideBuildingBlock(IBB.GuideBridge, casePreferenceData);
                var guideBridgeRhObjs = _objectManager.GetAllBuildingBlocks(guideBridgeEiBB.Block).ToList(); 
                var guideBridges = new List<KeyValuePair<Brep, Plane>>();

                guideBridgeRhObjs.ForEach(x =>
                {
                    Plane cs;
                    _objectManager.GetBuildingBlockCoordinateSystem(x.Id, out cs);
                    var val = new KeyValuePair<Brep, Plane>((Brep)x.Geometry, cs);
                    guideBridges.Add(val);
                });

                var appendedGuideBridgesMesh = new Mesh();
                foreach (var guideBridge in guideBridges)
                {
                    guideBridge.Key.UserDictionary.TryGetString(AttributeKeys.KeyGuideBridgeType, out var bridgeType);
                    var lwBridge = GuideBridgeUtilities.GenerateGuideBridgeWithLightweightFromBrep(guideBridge.Key,
                        guideBridge.Value,
                        parameters.LightweightParams.SegmentRadius,
                        parameters.LightweightParams.FractionalTriangleEdgeLength,
                        bridgeType == GuideBridgeType.OctagonalBridge
                            ? parameters.LightweightParams.OctagonalBridgeCompensation
                            : 0.0);

                    appendedGuideBridgesMesh.Append(lwBridge);
                }

                var guideTypeName = GeneralUtilities.CheckForTSGGuideTypeName(
                    _director, casePreferenceData);
                var caseFileNameTemplate = string.Format(FileNameTemplate, guideTypeName, casePreferenceData.NCase, "{0}");

                ExportStl(appendedGuideBridgesMesh, outputDirectory, string.Format(caseFileNameTemplate, "Bridge"), Colors.GuideBridge);
                appendedGuideBridgesMesh.Dispose();
            }
        }

        public void ExportSmoothGuideBaseSurface(string outputDirectory)
        {
            foreach (var casePreferenceData in _director.CasePrefManager.GuidePreferences)
            {
                var smoothGuideBaseSurfaceEiBB = _guideComponent.GetGuideBuildingBlock(IBB.SmoothGuideBaseSurface, casePreferenceData);
                var smoothGuideBaseSurfaces = _objectManager.GetAllBuildingBlocks(smoothGuideBaseSurfaceEiBB.Block);

                var appendedSmoothGuideBaseSurfacesMesh = new Mesh();
                foreach (var smoothGuideBaseSurface in smoothGuideBaseSurfaces)
                {
                    var smoothGuideBaseSurfaceMesh = ((Mesh)smoothGuideBaseSurface.Geometry).DuplicateMesh();
                    appendedSmoothGuideBaseSurfacesMesh.Append(smoothGuideBaseSurfaceMesh);

                    smoothGuideBaseSurfaceMesh.Dispose();
                }

                var guideTypeName = GeneralUtilities.CheckForTSGGuideTypeName(
                    _director, casePreferenceData);
                var caseFileNameTemplate = string.Format(FileNameTemplate, guideTypeName, casePreferenceData.NCase, "{0}");
                ExportStl(appendedSmoothGuideBaseSurfacesMesh, outputDirectory, string.Format(caseFileNameTemplate, "SmoothGuideBaseSurface"), Colors.GeneralGrey);
                appendedSmoothGuideBaseSurfacesMesh.Dispose();
            }
        }

        private void ExportGuideBuildingBlocks(GuidePreferenceDataModel casePreferenceData,
            string outputDirectory, string fileName, IBB block, Color color)
        {
            var buildingBlock = _guideComponent.GetGuideBuildingBlock(block, casePreferenceData);
            var rhinoObjects = _objectManager.GetAllBuildingBlocks(buildingBlock);

            var mesh = new Mesh();
            foreach (var rhinoObject in rhinoObjects)
            {
                if (rhinoObject.ObjectType == ObjectType.Mesh)
                {
                    mesh.Append((Mesh) rhinoObject.Geometry);
                }
                else if (rhinoObject.ObjectType == ObjectType.Brep)
                {
                    mesh.Append(MeshUtilities.ConvertBrepToMesh((Brep) rhinoObject.Geometry, true));
                }
            }
            
            ExportStl(mesh, outputDirectory, fileName, color);
        }

        public void ExportGuideFixationScrew(string outputDirectory, bool alsoWithItsEntities)
        {
            foreach (var casePreferenceData in _director.CasePrefManager.GuidePreferences)
            {
                var screwBuildingBlock = _guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, casePreferenceData);
                var screws = _objectManager.GetAllBuildingBlocks(screwBuildingBlock.Block).Select(screw => (Screw)screw);

                var screwEyeLabelTag = new Mesh();
                var screwEyeLabelTagShape = new Mesh();
                var eyeSubtractor = new Mesh();
                var appendedAllScrewMesh = new Mesh();

                foreach (var screw in screws)
                {
                    var screwEyeLabelTagComponent = GetScrewEyeLabelTag(casePreferenceData.GuideScrewAideData, screw, true);
                    screwEyeLabelTag.Append(MeshUtilities.ConvertBrepToMesh(screwEyeLabelTagComponent, true));

                    if (alsoWithItsEntities)
                    {
                        var screwEyeLabelTagShapeComponent = GetScrewEyeLabelTag(casePreferenceData.GuideScrewAideData, screw, false);
                        screwEyeLabelTagShape.Append(MeshUtilities.ConvertBrepToMesh(screwEyeLabelTagShapeComponent, true));

                        var eyeSubtractorComponent = new Brep();
                        eyeSubtractorComponent.Append(casePreferenceData.GuideScrewAideData.ScrewEyeSubtractor);
                        eyeSubtractorComponent.Transform(screw.AlignmentTransform);
                        eyeSubtractor.Append(MeshUtilities.ConvertBrepToMesh(eyeSubtractorComponent, true));
                    }

                    var screwBrep = ((Brep)screw.Geometry).DuplicateBrep();
                    var screwMesh = Mesh.CreateFromBrep(screwBrep, MeshParameters.IDS());
                    appendedAllScrewMesh.Append(screwMesh);
                    screwBrep.Dispose();
                    screw.Dispose();
                }

                var guideTypeName = GeneralUtilities.CheckForTSGGuideTypeName(
                    _director, casePreferenceData);
                var caseFileNameTemplate = string.Format(FileNameTemplate, guideTypeName, casePreferenceData.NCase, "{0}");

                ExportStl(screwEyeLabelTag, outputDirectory, string.Format(caseFileNameTemplate, "Eye"), Colors.GuideScrewFixation);
                if (alsoWithItsEntities)
                {
                    ExportStl(screwEyeLabelTagShape, outputDirectory, string.Format(caseFileNameTemplate, "EyeShape"), Colors.GuideScrewFixation);
                    ExportStl(eyeSubtractor, outputDirectory, string.Format(caseFileNameTemplate, "EyeSubtractor"), Colors.GuideScrewFixation);
                }

                ExportStl(appendedAllScrewMesh, outputDirectory, string.Format(caseFileNameTemplate, "Screws"), Colors.GuideScrewFixation);

                screwEyeLabelTag.Dispose();
                screwEyeLabelTagShape.Dispose();
                eyeSubtractor.Dispose();

                appendedAllScrewMesh.Dispose();
            }
        }

        public void ExportRegisteredBarrelAndCenterline(CasePreferenceDataModel casePreferenceData, string outputDirectory, string fileNameTemplate, bool alsoWithItsEntities)
        {
            var barrelBuildingBlock = _implantComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, casePreferenceData);
            var barrels = _objectManager.GetAllBuildingBlocks(barrelBuildingBlock.Block);
            var registeredBarrel = new Mesh();
            var registeredBarrelShape = new Mesh();
            var registeredBarrelSubtractor = new Mesh();
            Mesh registeredBarrelDidntMeetSpecs = null;

            var registeredBarrelCenterline = new Mesh();
            var barrelHelper = new BarrelHelper(_director);
            var guideSupport = _objectManager.HasBuildingBlock(IBB.GuideSupport) ? (Mesh)_objectManager.GetBuildingBlock(IBB.GuideSupport).Geometry : null;

            var screwBuildingBlock = _implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
            var screws = _objectManager.GetAllBuildingBlocks(screwBuildingBlock.Block).Select(x => (Screw)x);
            foreach (var barrel in barrels)
            {
                var alignTransform = (Transform)barrel.Attributes.UserDictionary["transformation_matrix"];
                var barrelAideData = casePreferenceData.BarrelAideData;
                foreach (var screw in screws)
                {
                    if (screw.RegisteredBarrelId != barrel.Id)
                    {
                        continue;
                    }

                    if (casePreferenceData.CasePrefData.BarrelTypeValue != screw.BarrelType)
                    {
                        barrelAideData = new BarrelAideDataModel(screw.ScrewType, screw.BarrelType);
                    }
                    break;
                }
                
                registeredBarrel.Append(MeshUtilities.ConvertBrepToMesh(GetRegisteredBarrelComponent(barrelAideData.ScrewBarrel, alignTransform)));

                var centerlineCurve = barrelHelper.GetBarrelCenterline(casePreferenceData, alignTransform, guideSupport);
                registeredBarrelCenterline.Append(barrelHelper.ConvertCurveToMesh(centerlineCurve));

                if (alsoWithItsEntities)
                {
                    registeredBarrelShape.Append(MeshUtilities.ConvertBrepToMesh(GetRegisteredBarrelComponent(barrelAideData.ScrewBarrelShape, alignTransform)));
                    registeredBarrelSubtractor.Append(MeshUtilities.ConvertBrepToMesh(GetRegisteredBarrelComponent(barrelAideData.ScrewBarrelSubtractor, alignTransform)));
                }

                if (GuideCreationUtilities.IsLeveledBarrelsNotMeetingSpecs(barrel)) //Being set in GuideScrewRegistration
                {
                    if (registeredBarrelDidntMeetSpecs == null)
                    {
                        registeredBarrelDidntMeetSpecs = new Mesh();
                    }

                    registeredBarrelDidntMeetSpecs.Append(
                        MeshUtilities.ConvertBrepToMesh(GetRegisteredBarrelComponent(barrelAideData.ScrewBarrel, alignTransform)));
                }
            }

            ExportStl(registeredBarrel, outputDirectory, string.Format(fileNameTemplate, "RegisteredBarrels"), Colors.GeneralGrey);
            ExportStl(registeredBarrelCenterline, outputDirectory, string.Format(fileNameTemplate, "RegisteredBarrelCenterline"), Colors.GeneralGrey);

            if (alsoWithItsEntities)
            {
                ExportStl(registeredBarrelShape, outputDirectory, string.Format(fileNameTemplate, "RegisteredBarrelsShape"), Colors.GeneralGrey);
                ExportStl(registeredBarrelSubtractor, outputDirectory, string.Format(fileNameTemplate, "BarrelSubtractor"), Colors.GeneralGrey);
            }

            if (registeredBarrelDidntMeetSpecs != null)
            {
                ExportStl(registeredBarrelDidntMeetSpecs, outputDirectory, string.Format(fileNameTemplate, "RegisteredBarrels_red"), Colors.BarrelLevelingNotMeetingSpecs);
            }
        }

        private Brep GetScrewEyeLabelTag(ScrewAideDataModel screwAideData, Screw screw, bool subtracted)
        {
            var screwComponent = new Brep();

            if (screw.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewLabelTag))
            {
                screwComponent.Append(subtracted ? screwAideData.ScrewLabelTag : screwAideData.ScrewLabelTagShape);

                var transform = _screwLabelTagHelper.GetLabelTagTransformFromDefaultOrientationOnScrew(screw);
                screwComponent.Transform(screw.AlignmentTransform);
                screwComponent.Transform(transform);
            }
            else
            {
                screwComponent.Append(subtracted ? screwAideData.ScrewEye : screwAideData.ScrewEyeShape);
                screwComponent.Transform(screw.AlignmentTransform);
            }

            return screwComponent;
        }

        private Brep GetRegisteredBarrelComponent(Brep component, Transform screwAlignment)
        {
            var screwComponent = new Brep();
            screwComponent.Append(component);
            screwComponent.Transform(screwAlignment);
            return screwComponent;
        }

        private void ExportStl(Mesh mesh, string outputDirectory, string fileName, Color color)
        {
            if (mesh.Faces.Count == 0)
            {
                return;
            }

            var filePath = Path.Combine(outputDirectory, $"{fileName}.stl");
            var stlColor = new int[] { color.R, color.G, color.B };
            StlUtilities.RhinoMesh2StlBinary(mesh, filePath, stlColor);
        }

        public bool ExportFixationScrewGauge(GuidePreferenceDataModel guideCasePrefData, GuideCaseComponent implantComponent,
            string guideDirectory, string guideName, string guideFixationScrewGaugeSuffix)
        {
            var gaugeExporter = new ScrewGaugeExporter();
            return gaugeExporter.ExportGuideScrewGauges(guideCasePrefData, _objectManager, guideDirectory, guideName, guideFixationScrewGaugeSuffix);
        }
    }
}
