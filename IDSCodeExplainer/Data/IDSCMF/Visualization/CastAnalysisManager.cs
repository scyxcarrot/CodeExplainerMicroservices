using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.V2.MTLS.Operation;
using IDS.Core.V2.Utilities;
using IDS.RhinoInterface.Converter;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class CastAnalysisManager
    {
        public const double MinTeethImpressionDepthValue = 0.0;
        public const double MaxTeethImpressionDepthValue = 3.0;

        private readonly CMFImplantDirector _director;

        public CastAnalysisManager(CMFImplantDirector director)
        {
            _director = director;
        }

        public static bool CheckIfGotVertexColor(RhinoDoc doc)
        {
            var rhObjects = GetCastRhinoObjectsIfGotVertexColors(doc);
            return rhObjects.Any();
        }

        public static void HandleRemoveAllVertexColor(CMFImplantDirector director)
        {
            var rhObjs = GetCastRhinoObjectsIfGotVertexColors(director.Document);
            if (rhObjs.Any())
            {
                rhObjs.ForEach(x => HandleRemoveVertexColor(director, x));
            }

            AnalysisScaleConduit.ConduitProxy.Enabled = false;
        }

        public RhinoObject GetCastRhinoObject(ProPlanImportPartType castType)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            return GetCastRhinoObjects(_director.Document).FirstOrDefault(c =>
                proPlanImportComponent.GetBlock(proPlanImportComponent.GetPartName(c.Name)).PartType == castType);
        }

        public bool HasInputsForTeethImpressionDepthAnalysis(ProPlanImportPartType castType)
        {
            var hasCastObject = GetCastRhinoObject(castType) != null;
            var hasLimitingSurface = GetLimitingSurfaceMesh(castType) != null;
            return hasCastObject && hasLimitingSurface;
        }

        public void PerformTeethImpressionDepthAnalysis(ProPlanImportPartType castType, out double[] triangleCenterDistances)
        {
            var castMesh = (Mesh)GetCastRhinoObject(castType).Geometry;
            var limitingSurfaceMesh = GetLimitingSurfaceMesh(castType);

            var fromIMesh = RhinoMeshConverter.ToIDSMesh(castMesh);
            var toIMesh = RhinoMeshConverter.ToIDSMesh(limitingSurfaceMesh);

            var console = new IDSRhinoConsole();
            TriangleSurfaceDistanceV2.DistanceMeshToMesh(console, fromIMesh, toIMesh,
                out var _, out triangleCenterDistances);

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Minimum: {triangleCenterDistances.Min()}, Maximum: {triangleCenterDistances.Max()}");
        }

        public Mesh ApplyTeethImpressionDepthAnalysis(ProPlanImportPartType castType, double[] triangleCenterDistances, bool setBuildingBlock)
        {
            var castRhinoObject = GetCastRhinoObject(castType);
            var castMesh = (Mesh)castRhinoObject.Geometry;

            var fromIMesh = RhinoMeshConverter.ToIDSMesh(castMesh);

            MeshAnalysisUtilities.CreateTriangleDiagnosticMesh(fromIMesh, MinTeethImpressionDepthValue, MaxTeethImpressionDepthValue,
                triangleCenterDistances, System.Drawing.Color.LightGray, out var newMesh, out var verticesColors);
            
            var rhinoMesh = RhinoMeshConverter.ToRhinoMesh(newMesh);
            rhinoMesh.VertexColors.SetColors(verticesColors);

            if (setBuildingBlock)
            {
                SetCastBuildingBlock(_director, castRhinoObject, rhinoMesh);
            }

            return rhinoMesh;
        }

        public bool GetAccurateTeethImpressionMaxDepth(ProPlanImportPartType castType, out double maxDistance)
        {
            var castMesh = (Mesh)GetCastRhinoObject(castType).Geometry;
            var limitingSurfaceMesh = GetLimitingSurfaceMesh(castType);
            var offseted = limitingSurfaceMesh.Offset(0.1, true);

            var fromIMesh = RhinoMeshConverter.ToIDSMesh(castMesh);
            var toIMesh = RhinoMeshConverter.ToIDSMesh(limitingSurfaceMesh);
            var offsetedIMesh = RhinoMeshConverter.ToIDSMesh(offseted);

            var console = new IDSRhinoConsole();
            var subtracted = BooleansV2.PerformBooleanSubtraction(console, fromIMesh, offsetedIMesh);
            var shells = MeshDiagnostics.SplitByShells(console, subtracted, out _).Select(s => new
            {
                Shell = s,
                Volume = MeshDiagnostics.GetMeshDimensions(console, s).Volume
            }).OrderBy(m => m.Volume);

            var filteredShells = shells.Take(shells.Count() - 1);
            if (filteredShells.All(s => s.Volume < 100))
            {
                //unable to perform a clean cut to produce teeth cusps
                maxDistance = double.MinValue;
                return false;
            }

            var teethCusps = MeshUtilitiesV2.AppendMeshes(filteredShells.Select(s => s.Shell));

            TriangleSurfaceDistanceV2.DistanceMeshToMesh(console, teethCusps, toIMesh,
                out var _, out var triangleCenterDistances);
            maxDistance = triangleCenterDistances.Max();

            return true;
        }

        public ProPlanImportPartType FindParentCastPartType(Guid childId)
        {
            //find parent cast to determine the cast type
            var idsDoc = _director.IdsDocument;
            var doc = _director.Document;

            if (!idsDoc.IsNodeInTree(childId))
            {
                return ProPlanImportPartType.NonProPlanItem;
            }

            var castObjects = GetCastRhinoObjects(doc);
            if (castObjects == null || !castObjects.Any())
            {
                return ProPlanImportPartType.NonProPlanItem;
            }

            var proPlanImportComponent = new ProPlanImportComponent();

            foreach (var castObject in castObjects)
            {
                if (!idsDoc.IsNodeInTree(castObject.Id))
                {
                    continue;
                }

                var childrenIds = GetAllChildrenIds(castObject.Id);
                if (childrenIds.Contains(childId))
                {
                    return proPlanImportComponent.GetBlock(proPlanImportComponent.GetPartName(castObject.Name)).PartType;
                }
            }

            return ProPlanImportPartType.NonProPlanItem;
        }

        private static List<RhinoObject> GetCastRhinoObjects(RhinoDoc doc)
        {
            var originalCastRhinoObjects = ProPlanImportUtilities.GetAllProplanPartsAsRangePartType(
               doc, ProplanBoneType.Original, new List<ProPlanImportPartType>()
               {
                    ProPlanImportPartType.MandibleCast,
                    ProPlanImportPartType.MaxillaCast
               });
            return originalCastRhinoObjects;
        }

        private static List<RhinoObject> GetCastRhinoObjectsIfGotVertexColors(RhinoDoc doc)
        {
            var res = new List<RhinoObject>();

            var castObjects = GetCastRhinoObjects(doc);
            if (castObjects != null && castObjects.Any())
            {
                castObjects.ForEach(x =>
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

            SetCastBuildingBlock(director, rhObj, mesh);
        }

        private Mesh GetLimitingSurfaceMesh(ProPlanImportPartType castType)
        {
            IBB buildingBlock;

            switch (castType)
            {
                case ProPlanImportPartType.MandibleCast:
                    buildingBlock = IBB.LimitingSurfaceMandible;
                    break;
                case ProPlanImportPartType.MaxillaCast:
                    buildingBlock = IBB.LimitingSurfaceMaxilla;
                    break;
                default:
                    throw new ArgumentException("Unsupported PartType", castType.ToString());
            }

            Mesh limitingSurface = null;
            var objectManager = new CMFObjectManager(_director);
            if (objectManager.HasBuildingBlock(buildingBlock))
            {
                limitingSurface = (Mesh)objectManager.GetBuildingBlock(buildingBlock).Geometry;
            }
            return limitingSurface;
        }

        private static void SetCastBuildingBlock(CMFImplantDirector director, RhinoObject rhObj, Mesh mesh)
        {
            var objectManager = new CMFObjectManager(director);
            var prevRecordState = RhinoDoc.ActiveDoc.UndoRecordingEnabled;
            RhinoDoc.ActiveDoc.UndoRecordingEnabled = false;

            objectManager.SetBuildingBlock(
                ProPlanImportUtilities.GetProPlanImportExtendedImplantBuildingBlock(director, rhObj), mesh,
                rhObj.Id);

            RhinoDoc.ActiveDoc.UndoRecordingEnabled = prevRecordState;
        }

        private List<Guid> GetAllChildrenIds(Guid parentId)
        {
            var idsDoc = _director.IdsDocument;

            var firstLevelChildrenIds = idsDoc.GetChildrenInTree(parentId);
            var childrenIds = new List<Guid>();

            foreach (var childId in firstLevelChildrenIds)
            {
                childrenIds.AddRange(GetAllChildrenIds(childId));
            }

            childrenIds.AddRange(firstLevelChildrenIds);
            return childrenIds;
        }
    }
}