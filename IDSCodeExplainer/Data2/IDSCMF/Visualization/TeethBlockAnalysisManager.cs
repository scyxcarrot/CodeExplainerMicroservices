using IDS.CMF.CasePreferences;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.V2.MTLS.Operation;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class TeethBlockAnalysisManager
    {
        public const double DefaultThickness = 0.8;
        public const double MinThicknessValue = 0.0;
        public const double MaxThicknessValue = DefaultThickness;

        private readonly CMFImplantDirector _director;

        public TeethBlockAnalysisManager(CMFImplantDirector director)
        {
            _director = director;
        }

        public static bool CheckIfGotVertexColor(CMFImplantDirector director)
        {
            var rhObjects = GetTeethBlockRhinoObjectsIfGotVertexColors(director);
            return rhObjects.Any();
        }

        public static void HandleRemoveAllVertexColor(CMFImplantDirector director)
        {
            var rhObjs = GetTeethBlockRhinoObjectsIfGotVertexColors(director);
            if (rhObjs.Any())
            {
                rhObjs.ForEach(x => HandleRemoveVertexColor(director, x));
            }

            AnalysisScaleConduit.ConduitProxy.Enabled = false;
        }

        public void PerformThicknessAnalysis(GuidePreferenceDataModel guidePrefData, out double[] thicknessData)
        {
            var teethBlockMesh = (Mesh)GetTeethBlockRhinoObject(guidePrefData).Geometry;

            var teethBlockIMesh = RhinoMeshConverter.ToIDSMesh(teethBlockMesh);

            var console = new IDSRhinoConsole();
            WallThicknessAnalysis.MeshWallThicknessInMM(console, teethBlockIMesh, out thicknessData);

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Minimum: {thicknessData.Min()}, Maximum: {thicknessData.Max()}");
        }

        public Mesh ApplyThicknessAnalysis(GuidePreferenceDataModel guidePrefData, double[] thicknessData, bool setBuildingBlock,
            out double lowerBound, out double upperBound)
        {
            var teethBlockRhinoObject = GetTeethBlockRhinoObject(guidePrefData);
            var teethBlockMesh = (Mesh)teethBlockRhinoObject.Geometry;

            var teethBlockIMesh = RhinoMeshConverter.ToIDSMesh(teethBlockMesh);

            MeshAnalysisUtilities.ConstraintThicknessData(thicknessData, MinThicknessValue, MaxThicknessValue,
                out var constraintThicknessData, out lowerBound, out upperBound);

            MeshAnalysisUtilities.CreateTriangleDiagnosticMesh(teethBlockIMesh, lowerBound, upperBound,
                constraintThicknessData, System.Drawing.Color.LightGray, out var newMesh, out var verticesColors);
            
            var rhinoMesh = RhinoMeshConverter.ToRhinoMesh(newMesh);
            rhinoMesh.VertexColors.SetColors(verticesColors);

            if (setBuildingBlock)
            {
                SetTeethBlockBuildingBlock(_director, teethBlockRhinoObject, rhinoMesh);
            }

            return rhinoMesh;
        }

        private static List<RhinoObject> GetTeethBlockRhinoObjects(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            return objectManager.GetAllBuildingBlocks(IBB.TeethBlock).ToList();
        }

        private static List<RhinoObject> GetTeethBlockRhinoObjectsIfGotVertexColors(CMFImplantDirector director)
        {
            var res = new List<RhinoObject>();

            var teethBlockObjects = GetTeethBlockRhinoObjects(director);
            if (teethBlockObjects != null && teethBlockObjects.Any())
            {
                teethBlockObjects.ForEach(x =>
                {
                    var m = x.Geometry as Mesh;
                    if (m != null && m.VertexColors.Any())
                    {
                        res.Add(x);
                    }
                });
            }

            return res;
        }

        private static void HandleRemoveVertexColor(CMFImplantDirector director, RhinoObject rhObj)
        {
            director.Document.Objects.Unlock(rhObj.Id, true);
            var mesh = (Mesh)rhObj.Geometry;
            mesh.VertexColors.Clear();

            SetTeethBlockBuildingBlock(director, rhObj, mesh);
        }

        private RhinoObject GetTeethBlockRhinoObject(GuidePreferenceDataModel guidePrefData)
        {
            var objectManager = new CMFObjectManager(_director);
            var guideComponent = new GuideCaseComponent();
            var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePrefData);
            return objectManager.GetBuildingBlock(extendedBuildingBlock);
        }

        private static void SetTeethBlockBuildingBlock(CMFImplantDirector director, RhinoObject rhObj, Mesh mesh)
        {
            var objectManager = new CMFObjectManager(director);
            var guidePrefData = objectManager.GetGuidePreference(rhObj);

            var guideComponent = new GuideCaseComponent();
            var extendedBuildingBlock = guideComponent.GetGuideBuildingBlock(IBB.TeethBlock, guidePrefData);

            var prevRecordState = RhinoDoc.ActiveDoc.UndoRecordingEnabled;
            RhinoDoc.ActiveDoc.UndoRecordingEnabled = false;

            objectManager.SetBuildingBlock(extendedBuildingBlock, mesh, rhObj.Id);

            RhinoDoc.ActiveDoc.UndoRecordingEnabled = prevRecordState;
        }
    }
}