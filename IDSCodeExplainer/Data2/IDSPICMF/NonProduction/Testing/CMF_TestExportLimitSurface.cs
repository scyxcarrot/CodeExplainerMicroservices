#if STAGING
using IDS.CMF;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Geometry;
using IDS.Core.V2.Utilities;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.IO;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("16D1C7C3-4790-44D4-9E90-75E9BAEC542A")]
    [CommandStyle(Style.ScriptRunner)]
    public class CMF_TestExportLimitSurface : CmfCommandBase
    {
        public override string EnglishName => "CMF_TestExportLimitSurface";
        public static CMF_TestExportLimitSurface Instance { get; private set; }
        public CMF_TestExportLimitSurface()
        {
            Instance = this;
        }

        public class LimitSurfaceData
        {
            public string ObjectName { get; set; }
            public List<Point3d> OriginalCurvePoints { get; set; } = new List<Point3d>();
            public List<Point3d> OuterCurvePoints { get; set; } = new List<Point3d>();
        }

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objManager = new CMFObjectManager(director);

            var hasMaxillaSurface = objManager.HasBuildingBlock(IBB.LimitingSurfaceMaxilla);
            var hasMandibleSurface = objManager.HasBuildingBlock(IBB.LimitingSurfaceMandible);

            if (!hasMaxillaSurface && !hasMandibleSurface)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "This case does not have limiting surface(s)!");
                return Result.Cancel;
            }
            // Get Rhino objects from the available parts
            var rhinoObjects = new List<RhinoObject>();
            if (hasMandibleSurface)
            {
                rhinoObjects.Add(objManager.GetBuildingBlock(IBB.LimitingSurfaceMandible));
            }

            if (hasMaxillaSurface)
            {
                rhinoObjects.Add(objManager.GetBuildingBlock(IBB.LimitingSurfaceMaxilla));
            }
            var workingDir = IDS.CMF.FileSystem.DirectoryStructure.GetWorkingDir(director.Document);
            var exportData = new List<LimitSurfaceData>();
           
            // Extract points from rhinoObjects and project lines
            foreach (var rhinoObj in rhinoObjects)
            {
                var hasOriginalCurve = rhinoObj.Attributes.UserDictionary.TryGetString("OriginalCurvePoints", out var originalCurvePoints);
                var hasOuterCurve = rhinoObj.Attributes.UserDictionary.TryGetString("OuterCurvePoints", out var outerCurvePoints);

                if (!hasOriginalCurve && !hasOuterCurve)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"{rhinoObj.Name} do not have limit surface.");
                   
                }
                else
                {
                    // Export to STL
                    var idsMesh = RhinoMeshConverter.ToIDSMesh((Mesh)rhinoObj.Geometry);
                    var surfaceData = new LimitSurfaceData
                    {
                        ObjectName = rhinoObj.Name
                    };

                    if (hasOriginalCurve)
                    {
                        surfaceData.OriginalCurvePoints = ParsePointsFromString(originalCurvePoints);
                    }

                    if (hasOuterCurve)
                    {
                        surfaceData.OuterCurvePoints = ParsePointsFromString(outerCurvePoints);
                    }

                    exportData.Add(surfaceData);

                    var stlExportPath = Path.Combine(workingDir, $"{rhinoObj.Name}_LimitSurface.stl");
                    StlUtilitiesV2.IDSMeshToStlBinary(idsMesh, stlExportPath);
                }
            }
            //Export the points to JSON
            var jsonOutput = JsonUtilities.Serialize(exportData, Newtonsoft.Json.Formatting.Indented);
            var exportPath = Path.Combine(workingDir, "LimitSurfacePoints.json");
            File.WriteAllText(exportPath, jsonOutput);

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Exported limit surface points to: {workingDir}");
            return Result.Success;
        }

        private List<Point3d> ParsePointsFromString(string pointsString)
        {
            var points = new List<Point3d>();
            var pointStrings = pointsString.Split('|');
            foreach (var pointString in pointStrings)
            {
                if (!string.IsNullOrWhiteSpace(pointString))
                {
                    var coordinates = pointString.Split(',');
                    if (coordinates.Length >= 3)
                    {
                        if (double.TryParse(coordinates[0], out double x) &&
                            double.TryParse(coordinates[1], out double y) &&
                            double.TryParse(coordinates[2], out double z))
                        {
                            points.Add(new Point3d(x, y, z));
                        }
                    }
                }
            }
            return points;
        }
    }
}

#endif