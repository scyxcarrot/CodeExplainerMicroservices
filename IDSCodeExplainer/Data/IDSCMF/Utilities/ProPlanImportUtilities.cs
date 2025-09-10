using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Logics;
using IDS.Core.Utilities;
using IDS.Core.V2.Utilities;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.Utilities
{
    public static class ProPlanImportUtilities
    {
        private static Dictionary<ProPlanImportPartType, List<string>> _categorisedSubLayer = null;

        private static Dictionary<ProPlanImportPartType, List<string>> CategorisedSubLayersEncapsulated
        {
            get
            {
                if (_categorisedSubLayer == null)
                {
                    _categorisedSubLayer = new Dictionary<ProPlanImportPartType, List<string>>();
                    var proplanParser = new ProPlanImportBlockJsonParser();
                    var proplanBlocks = proplanParser.LoadBlocks();

                    foreach (var proplanBlock in proplanBlocks)
                    {
                        if (proplanBlock.SubLayer == null)
                        {
                            continue;
                        }

                        if (!_categorisedSubLayer.ContainsKey(proplanBlock.PartType))
                        {
                            _categorisedSubLayer.Add(proplanBlock.PartType, new List<string>());
                        }

                        if (!_categorisedSubLayer[proplanBlock.PartType].Contains(proplanBlock.SubLayer))
                        {
                            _categorisedSubLayer[proplanBlock.PartType].Add(proplanBlock.SubLayer);
                        }
                    }
                }
                return _categorisedSubLayer;
            }
        }

        public static bool IsPartOfBoneType(string partName, ProplanBoneType boneType)
        {
            switch (boneType)
            {
                case ProplanBoneType.Preop:
                    return ProPlanPartsUtilitiesV2.IsPreopPart(partName);
                case ProplanBoneType.Original:
                    return ProPlanPartsUtilitiesV2.IsOriginalPart(partName);
                case ProplanBoneType.Planned:
                    return ProPlanPartsUtilitiesV2.IsPlannedPart(partName);
                default:
                    throw new ArgumentOutOfRangeException(nameof(boneType), boneType, null);
            }
        }

        public static List<Mesh> GetAllOriginalOsteotomyParts(RhinoDoc doc)
        {
            var layerIndex = doc.GetLayerWithName(ProPlanImport.OriginalLayer);
            return GetOsteotomyParts(doc, layerIndex);
        }

        public static List<Mesh> GetAllPlannedOsteotomyParts(RhinoDoc doc)
        {
            var layerIndex = doc.GetLayerWithName(ProPlanImport.PlannedLayer);
            return GetOsteotomyParts(doc, layerIndex);
        }

        public static List<RhinoObject> GetAllOriginalOsteotomyPartsRhinoObjects(RhinoDoc doc)
        {
            var layerIndex = doc.GetLayerWithName(ProPlanImport.OriginalLayer);
            return GetOsteotomyPartsRhinoObject(doc, layerIndex);
        }

        public static List<RhinoObject> GetAllPlannedOsteotomyPartsRhinoObjects(RhinoDoc doc)
        {
            var layerIndex = doc.GetLayerWithName(ProPlanImport.PlannedLayer);
            return GetOsteotomyPartsRhinoObject(doc, layerIndex);
        }

        private static List<Mesh> GetOsteotomyParts(RhinoDoc doc, int layerIndex)
        {
            var res = new List<Mesh>();

            var tmpMesh =  GetOsteotomyPartsRhinoObject(doc, layerIndex).Select(x => (Mesh)x.Geometry).ToList();
            tmpMesh.ForEach(x =>
            {
                res.Add(x);
            });

            return res;
        }

        private static List<RhinoObject> GetOsteotomyPartsRhinoObject(RhinoDoc doc, int layerIndex)
        {
            var parentLayer = doc.Layers[layerIndex];
            var osteotomyLayers = parentLayer.GetChildren().Where(l =>
                IsPartAsPartType(ProPlanImportPartType.OsteotomyPlane, l.FullPath));
            return osteotomyLayers.SelectMany(layer => doc.Objects.FindByLayer(layer)).ToList();
        }

        public static Mesh CloseOsteotomyPart(ExtendedImplantBuildingBlock block, Mesh mesh)
        {
            var solidMesh = mesh;
            if (IsPartAsPartType(ProPlanImportPartType.OsteotomyPlane, block.Block.Layer))
            {
                solidMesh = ClosePart(solidMesh);
            }

            return solidMesh;
        }

        public static Mesh ClosePart(Mesh mesh)
        {
            var solidMesh = mesh;
            if (!solidMesh.IsClosed)
            {
                var contour = MeshUtilities.GetValidContours(solidMesh, duplast: false, raiseIfInvalid: true);
                if (contour.Count == 1 && contour[0].Length == 4)
                {
                    var vertices = contour[0];
                    solidMesh.Faces.AddFace(vertices[0], vertices[1], vertices[2]);
                    solidMesh.Faces.AddFace(vertices[0], vertices[2], vertices[3]);
                    solidMesh.UnifyNormals();
                }
            }

            return solidMesh;
        }

        [Obsolete("Rhino Decoupling - Use IDSPICMF.UpdateProPlanHelper.GetAllPlannedLayerObjects instead")]
        public static List<RhinoObject> GetAllPlannedLayerObjects(RhinoDoc doc)
        {
            return GetAllObjects(doc, doc.GetLayerWithName(ProPlanImport.PlannedLayer));
        }

        [Obsolete("Rhino Decoupling - Use IDSPICMF.UpdateProPlanHelper.GetAllOriginalLayerObjects instead")]
        public static List<RhinoObject> GetAllOriginalLayerObjects(RhinoDoc doc)
        {
            return GetAllObjects(doc, doc.GetLayerWithName(ProPlanImport.OriginalLayer));
        }

        [Obsolete("Rhino Decoupling - Use IDSPICMF.UpdateProPlanHelper.GetAllPreOpLayerObjects instead")]
        public static List<RhinoObject> GetAllPreOpLayerObjects(RhinoDoc doc)
        {
            return GetAllObjects(doc, doc.GetLayerWithName(ProPlanImport.PreopLayer));
        }

        public static List<RhinoObject> GetAllObjects(RhinoDoc doc, ProplanBoneType boneType)
        {
            switch (boneType)
            {
                case ProplanBoneType.Preop:
                    return GetAllPreOpLayerObjects(doc);
                case ProplanBoneType.Original:
                    return GetAllOriginalLayerObjects(doc);
                case ProplanBoneType.Planned:
                    return GetAllPlannedLayerObjects(doc);
                default:
                    throw new ArgumentOutOfRangeException(nameof(boneType), boneType, null);
            }
        }

        public static List<RhinoObject> GetAllProPlanObjects(RhinoDoc doc, ProplanBoneType boneType)
        {
            return GetAllObjects(doc, boneType)
                .Where(x => x.Geometry is Mesh && x.Name.Contains(ProPlanImport.ObjectPrefix)).ToList();
        }

        public static List<RhinoObject> GetAllObjects(RhinoDoc doc, int layerIndex)
        {
            var parentLayer = doc.Layers[layerIndex];
            var childs = parentLayer.GetChildren();
            return childs.SelectMany(layer => doc.Objects.FindByLayer(layer)).ToList();
        }

        public static bool IsOsteotomyPlane(string partName)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var osteotomyBlocks = proPlanImportComponent.Blocks.Where(b => b.PartType == ProPlanImportPartType.OsteotomyPlane);
            foreach (var block in osteotomyBlocks)
            {
                if (Regex.IsMatch(partName, $"^{block.PartNamePattern}$", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool RegenerateGuideGuidingOutlines(CMFObjectManager objectManager)
        {
            var director = objectManager.GetDirector();
            var guideFlangeGuidingBlocks = objectManager.GetAllBuildingBlocks(IBB.GuideFlangeGuidingOutline);
            guideFlangeGuidingBlocks.ToList().ForEach(block => objectManager.DeleteObject(block.Id));

            var guideGuidingOutlineCreator = new GuideGuidingOutlineCreator(director);

            guideGuidingOutlineCreator.CreateGuideFlangeGuidingOutline(out var guideFlangeGuidingOutlines);
            if (guideFlangeGuidingOutlines.Count == 0)
            {
                return false;
            }
            
            guideFlangeGuidingOutlines.ForEach(x => objectManager.AddNewBuildingBlock(IBB.GuideFlangeGuidingOutline, x));
            return true;
        }

        public static bool RegenerateImplantSupportGuidingOutlines(CMFObjectManager objectManager)
        {
            var director = objectManager.GetDirector();
            // Remove obsoleted margin outlines
            var implantMarginGuidingBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantMarginGuidingOutline);
            implantMarginGuidingBlocks.ToList().ForEach(block => objectManager.DeleteObject(block.Id));

            var implantSupportGuidingBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportGuidingOutline);
            implantSupportGuidingBlocks.ToList().ForEach(block => objectManager.DeleteObject(block.Id));

            director.OsteotomiesPreop = null;

            var implantSupportGuidingOutlineCreator = new ImplantSupportGuidingOutlineCreator(director);

            implantSupportGuidingOutlineCreator.CreateImplantSupportGuidingOutlines(
                out var implantSupportGuidingOutlinesInfo, out var osteotomiesPreop);
            
            director.GeneratedImplantSupportGuidingOutlines = true;

            if (implantSupportGuidingOutlinesInfo.Count == 0 || osteotomiesPreop == null)
            {
                return false;
            }

            var implantSupportGuidingOutlineHelper = new ImplantSupportGuidingOutlineHelper(director);
            var implantMarginInputGetterHelper = new ImplantMarginInputGetterHelper(director);

            foreach (var implantSupportGuidingOutlineInfo in implantSupportGuidingOutlinesInfo)
            {
                var originalPart = implantSupportGuidingOutlineInfo.Value;
                var transform = implantMarginInputGetterHelper.GetMarginTransform(originalPart);

                var curve = implantSupportGuidingOutlineInfo.Key.DuplicateCurve();
                curve.Transform(transform);

                implantSupportGuidingOutlineHelper.AddImplantSupportGuidingOutlineBuildingBlocks(curve,
                    implantSupportGuidingOutlineInfo.Value);
            }

            director.OsteotomiesPreop = osteotomiesPreop;
            return true;
        }

        public static List<string> GetComponentSubLayerNames(ProPlanImportPartType partType)
        {
            return CategorisedSubLayersEncapsulated[partType].ToList();
        }

        public static bool IsPartAsPartType(ProPlanImportPartType partType, string layerName)
        {
            return IsPartAsRangePartType(new List<ProPlanImportPartType>(){partType}, layerName);
        }

        public static bool IsPartAsRangePartType(IEnumerable<ProPlanImportPartType> partTypes, string layerName)
        {
            var subLayers = new List<string>();
            foreach (var partType in partTypes)
            {
                subLayers.AddRange(CategorisedSubLayersEncapsulated[partType]);
            }
            var lowerCaseLayerName = layerName.ToLower();
            return subLayers.Any(s => lowerCaseLayerName.Contains(s.ToLower()));
        }

        public static List<RhinoObject> GetAllProplanPartsAsRangePartType(RhinoDoc doc, ProplanBoneType boneType, 
            IEnumerable<ProPlanImportPartType> partTypes)
        {
            var allProplanParts = GetAllProPlanObjects(doc, boneType);
            
            return allProplanParts.Where(proplanPart =>
                IsPartAsRangePartType(partTypes, doc.Layers[proplanPart.Attributes.LayerIndex].Name)).ToList();
        }

        public static IEnumerable<string> GetFullLayerNamesByPartType(
            ProPlanImportPartType partType, string parentLayerName)
        {
            return GetFullLayerNamesByRangePartType(
                new List<ProPlanImportPartType>(){ partType }, parentLayerName);
        }

        public static IEnumerable<string> GetFullLayerNamesByRangePartType(
            IEnumerable<ProPlanImportPartType> partTypes, string parentLayerName)
        {
            var fullLayersName = new List<string>();

            foreach (var partType in partTypes)
            {
                var subLayers = CategorisedSubLayersEncapsulated[partType];
                fullLayersName.AddRange(subLayers.Select(subLayer => $"{parentLayerName}::{subLayer}"));
            }

            return fullLayersName;
        }

        public static List<string> GetNerveRelatedComponentSubLayerNames()
        {
            var nerveRelatedSubLayerNames = GetComponentSubLayerNames(ProPlanImportPartType.Nerve);
            nerveRelatedSubLayerNames.AddRange(GetComponentSubLayerNames(ProPlanImportPartType.NerveRegistered));
            return nerveRelatedSubLayerNames;
        }

        public static IEnumerable<string> GetNerveComponentPartNames(RhinoDoc doc, string parentLayerName)
        {
            var nerveRelatedSubLayerNames = GetNerveRelatedComponentSubLayerNames();
            return GetPartNames(doc, parentLayerName, nerveRelatedSubLayerNames);
        }

        public static IEnumerable<string> GetTeethComponentPartNames(RhinoDoc doc, string parentLayerName)
        {
            var subLayerNames = GetComponentSubLayerNames(ProPlanImportPartType.Teeth);
            return GetPartNames(doc, parentLayerName, subLayerNames);
        }

        public static IEnumerable<string> GetPartNames(RhinoDoc doc, string parentLayerName, List<string> subLayerNames)
        {
            var layerIndex = doc.GetLayerWithName(parentLayerName);

            var parentLayer = doc.Layers[layerIndex];
            var componentLayers = parentLayer.GetChildren().Where(layer => subLayerNames.Contains(layer.Name));
            
            return componentLayers.SelectMany(layer => doc.Objects.FindByLayer(layer)).Select(rhi => rhi.Name);
        }

        public static bool IsTransformationMatrixCompatibleWithPart(string partName, Transform matrix)
        {
            //Planned parts (02 - 09) should have transformation matrix NOT IDENTITY
            //Original parts (01) should have transformation matrix IDENTITY

            if (ProPlanPartsUtilitiesV2.IsOriginalPart(partName))
            {
                return matrix.IsIdentity;
            }
            else if (ProPlanPartsUtilitiesV2.IsPlannedPart(partName))
            {
                return !matrix.IsIdentity;
            }

            return true;
        }

        public static RhinoObject GetPlannedObjectByOriginalObject(RhinoDoc document, RhinoObject originalObject)
        {
            return GetFilteredObjectByObjectName(document, ProplanBoneType.Planned, originalObject.Name);
        }

        public static RhinoObject GetFilteredObjectByObjectName(RhinoDoc document, ProplanBoneType boneType, string objectName)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            if (!proPlanImportComponent.GetPurePartNameFromBlockName(objectName, out _, out var purePartName))
            {
                purePartName = objectName;
            }

            var parts = GetAllObjects(document, boneType);
            return parts.FirstOrDefault(part =>
            {
                if (!proPlanImportComponent.GetPurePartNameFromBlockName(part.Name, out _, out var boneTypePurePartName))
                {
                    boneTypePurePartName = part.Name;
                }

                return boneTypePurePartName == purePartName;
            });
        }

        public static List<RhinoObject> GetGeneralPreopParts(RhinoDoc doc)
        {
            // get all parts under PreOp layer that were imported via import pro plan or import recut except Others layer

            return GetGeneralParts(doc, ProplanBoneType.Preop);
        }

        public static List<RhinoObject> GetGeneralParts(RhinoDoc doc, ProplanBoneType boneType)
        {
            var parts = new List<RhinoObject>();

            var rhObjs = GetAllObjects(doc, boneType).Where(x => x.Geometry is Mesh && x.Name.Contains(ProPlanImport.ObjectPrefix));
            foreach (var rhinoObject in rhObjs)
            {
                if (IsPartAsRangePartType(
                        new List<ProPlanImportPartType>() { ProPlanImportPartType.Other, ProPlanImportPartType.Nerve},
                        doc.Layers[rhinoObject.Attributes.LayerIndex].Name))
                {
                    continue;
                }

                parts.Add(rhinoObject);
            }

            return parts;
        }

        public static List<RhinoObject> GetMetalParts(RhinoDoc doc)
        {
            var parts = new List<RhinoObject>();
            var rhObjs = GetAllPreOpLayerObjects(doc).Where(x => x.Geometry is Mesh && x.Name.Contains(ProPlanImport.ObjectPrefix));
            foreach (var rhinoObject in rhObjs)
            {
                if (!IsPartAsRangePartType(
                        new List<ProPlanImportPartType>() { ProPlanImportPartType.Metal},
                        doc.Layers[rhinoObject.Attributes.LayerIndex].Name))
                {
                    continue;
                }

                parts.Add(rhinoObject);
            }

            return parts;
        }

        public static void PostProPlanPartsCreation(CMFImplantDirector director, out Dictionary<string, string> trackingParameters)
        {
            trackingParameters = new Dictionary<string, string>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var objectManager = new CMFObjectManager(director);

            ProPlanImportUtilities.RegenerateImplantSupportGuidingOutlines(objectManager);
            stopwatch.Stop();
            trackingParameters.Add("Generate Support Outlines",
                StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));
            stopwatch.Restart();

            var anatomicalObstaclesCreator = new AnatomicalObstaclesCreator(director);
            var anatomicalObstacles = anatomicalObstaclesCreator.CreateDefaultAnatomicalObstacles();
            foreach (var anatomicalObstacle in anatomicalObstacles)
            {
                objectManager.AddNewBuildingBlock(IBB.AnatomicalObstacles, anatomicalObstacle);
            }

            stopwatch.Stop();
            trackingParameters.Add("Create Anatomical Obstacles",
                StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));
            stopwatch.Restart();

            CreateWrappedObjects(objectManager);

            stopwatch.Stop();
            trackingParameters.Add("Create Wrapped Objects",
                StringUtilitiesV2.ElapsedTimeSpanToString(stopwatch.Elapsed));
        }

        public static void CreateWrappedObjects(CMFObjectManager objectManager)
        {
            //Create Wrapped Nerve
            var wrappedNerveCreator = new WrappedNerveCreator(objectManager);
            var plannedWrappedNerve = wrappedNerveCreator.CreatePlannedWrapNerves();
            objectManager.AddNewBuildingBlock(IBB.NervesWrapped, plannedWrappedNerve);
            var originalWrapNerve = wrappedNerveCreator.CreateOriginalNerves();
            objectManager.AddNewBuildingBlock(IBB.OriginalNervesWrapped, originalWrapNerve);

            var wrappedTeethCreator = new WrappedTeethCreator(objectManager);
            var originalMaxillaWrapTeeth = wrappedTeethCreator.CreateOriginalWrapTeeth(TeethLayer.MaxillaTeeth);
            objectManager.AddNewBuildingBlock(IBB.OriginalMaxillaTeethWrapped, originalMaxillaWrapTeeth);
            var originalMandibleWrapTeeth = wrappedTeethCreator.CreateOriginalWrapTeeth(TeethLayer.MandibleTeeth);
            objectManager.AddNewBuildingBlock(IBB.OriginalMandibleTeethWrapped, originalMandibleWrapTeeth);

            var plannedMaxillaWrapTeeth = wrappedTeethCreator.CreatePlannedWrapTeeth(TeethLayer.MaxillaTeeth);
            objectManager.AddNewBuildingBlock(IBB.PlannedMaxillaTeethWrapped, plannedMaxillaWrapTeeth);
            var plannedMandibleWrapTeeth = wrappedTeethCreator.CreatePlannedWrapTeeth(TeethLayer.MandibleTeeth);
            objectManager.AddNewBuildingBlock(IBB.PlannedMandibleTeethWrapped, plannedMandibleWrapTeeth);
        }

        public static ExtendedImplantBuildingBlock GetProPlanImportExtendedImplantBuildingBlock(CMFImplantDirector director, RhinoObject rhObject)
        {
            var partName = rhObject.Name;
            var partNameRemovedPrefix = partName.Remove(0, "ProPlanImport_".Length);
            var restorer = new BuildingBlockRestorer(director);
            return restorer.GetExtendedBuildingBlock(IBB.ProPlanImport, partNameRemovedPrefix);
        }
    }
}