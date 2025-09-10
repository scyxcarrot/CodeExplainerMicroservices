using IDS.CMF.CasePreferences;
using IDS.CMF.ExternalTools;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Operations;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class GuideSupportSourcesExporter
    {
        public const string SubFolderName = "Guide_Support";
        private const string StlFileExtension = ".stl";

        private readonly CMFImplantDirector _director;
        private readonly SupportSourcesExporterHelper _exporterHelper;
        private readonly string _filePrefix;
        private readonly int _version;
        private readonly int _draft;

        public GuideSupportSourcesExporter(CMFImplantDirector director)
        {
            _director = director;
            _exporterHelper = new SupportSourcesExporterHelper(director);
            _filePrefix = director.caseId;
            _version = director.version;
            _draft = director.draft;
        }
        
        public void ExportSources(string workingDir, bool exportMxp)
        {
            var directory = SystemTools.HandleCreateDirectory(workingDir, SubFolderName);

            ExportOriginalMeshParts(directory);
            ExportRegisteredBarrels(directory);
            ExportMeshParts(directory, Constants.ProPlanImport.PreopLayer);
            ExportActualGuide(directory);

            if (exportMxp)
            {
                ExportGuideSupportSourcesMxp(directory);
                return;
            }
            
            ExportManualConvertMxpFolder(workingDir);
        }

        private void ExportOriginalMeshParts(string exportDir)
        {
            //Export everything under Original layer
            //This includes Guide Support Mesh
            ExportMeshParts(exportDir, Constants.ProPlanImport.OriginalLayer);
        }

        private void ExportMeshParts(string exportDir, string layerName)
        {
            var blocks = _exporterHelper.GetBuildingBlocksOfMeshOrBrepByLayerForExport(layerName);
            BlockExporter.ExportBuildingBlocks(_director, blocks, exportDir);
        }

        private void ExportRegisteredBarrels(string exportDir)
        {
            var block = IBB.RegisteredBarrel;
            var objectManager = new CMFObjectManager(_director);
            if (!objectManager.HasBuildingBlock(block))
            {
                return;
            }

            var barrelsRhObj = objectManager.GetAllBuildingBlocks(block);
            var barrels = barrelsRhObj.Select(barrel => (Brep) barrel.Geometry);
            var barrelsInASingleBrep = BrepUtilities.Append(barrels.ToArray());
            var barrelMesh = MeshUtilities.ConvertBrepToMesh(barrelsInASingleBrep, true);

            Mesh registeredBarrelDidntMeetSpecs = null;
            foreach (var rhinoObject in barrelsRhObj)
            {
                if (GuideCreationUtilities.IsLeveledBarrelsNotMeetingSpecs(rhinoObject))
                {
                    if (registeredBarrelDidntMeetSpecs == null)
                    {
                        registeredBarrelDidntMeetSpecs = new Mesh();
                    }

                    registeredBarrelDidntMeetSpecs.Append(MeshUtilities.ConvertBrepToMesh((Brep)rhinoObject.Geometry));
                }
            }

            var staticBlock = BuildingBlocks.Blocks[block];
            var filePath = FormatFilePath(exportDir, "RegisteredBarrel", StlFileExtension);
            var filePathForBarrelsNotMeetingSpecs = FormatFilePath(exportDir, "RegisteredBarrel_red", StlFileExtension);

            var blockColor = staticBlock.Color;
            var meshColor = new int[] {blockColor.R, blockColor.G, blockColor.B};

            StlUtilities.RhinoMesh2StlBinary(barrelMesh, filePath, meshColor);

            ExportSpecialBarrel(registeredBarrelDidntMeetSpecs, filePathForBarrelsNotMeetingSpecs,
                Colors.BarrelLevelingNotMeetingSpecs);
        }

        private void ExportSpecialBarrel(Mesh barrel, string filePath, Color color)
        {
            if (barrel != null)
            {
                var colorInt = new int[]
                {
                    color.R, color.G, color.B
                };
                StlUtilities.RhinoMesh2StlBinary(barrel, filePath, colorInt);
            }
        }

        private void ExportActualGuide(string directory)
        {
            var qcGuideExporter = new CMFQCGuideExporter(_director);
            qcGuideExporter.ExportGuideComponent(directory, IBB.ActualGuide, "Actual", new Dictionary<GuidePreferenceDataModel, string>());
        }

        private static void ExportGuideSupportSourcesMxp(string directory)
        {
            var trimaticInteropGuidePhase = new TrimaticInteropGuidePhase();
            trimaticInteropGuidePhase.GenerateMxpFromStl(directory);
        }

        private static void ExportManualConvertMxpFolder(string exportDir)
        {
            var trimaticInteropGuidePhase = new TrimaticInteropGuidePhase();
            trimaticInteropGuidePhase.ExportStlToMxpManualConvertFolder(exportDir);
        }

        private string FormatFilePath(string exportDir, string partName, string fileExtension)
        {
            var fileSuffix = string.Format("v{1:D}_draft{0:D}", _draft, _version);
            return exportDir + "\\" + $"{_filePrefix}_{partName}_{fileSuffix}{fileExtension}";
        }

        private List<Mesh> FindMetalUsedInGuideSupportRoiMetalIntegration(List<Mesh> metalParts)
        {
            var integratedMetalParts = new List<Mesh>();

            var objectManager = new CMFObjectManager(_director);
            var guideSupportRoi = (Mesh)objectManager.GetBuildingBlock(IBB.GuideSupportRoI).DuplicateGeometry();

            foreach (var metalPart in metalParts)
            {
                var metalPartArea = MeshUtilities.ComputeTotalSurfaceArea(metalPart);
                var metalPartVolume = metalPart.Volume();

                var getMetalAndRoIBooleanIntersection = Booleans.PerformBooleanIntersection(metalPart, guideSupportRoi);
                var getMetalAndRoIBooleanIntersectionArea =
                    MeshUtilities.ComputeTotalSurfaceArea(getMetalAndRoIBooleanIntersection);
                var getMetalAndRoIBooleanIntersectionVolume = getMetalAndRoIBooleanIntersection.Volume();

                if (getMetalAndRoIBooleanIntersectionArea < 0.10 * metalPartArea 
                    && getMetalAndRoIBooleanIntersectionVolume < 0.10 * metalPartVolume)
                {
                    continue;
                }
                integratedMetalParts.Add(metalPart);
            }

            return integratedMetalParts;
        }
    }
}
