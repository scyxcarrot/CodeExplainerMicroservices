using IDS.CMF.CustomMainObjects;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class ImplantMarginHelper
    {
        private const string MarginCurveKey = "margin_curve_guid";
        private const string TrimmedMarginCurveKey = "trimmed_margin_curve";
        private const string MarginThicknessKey = "margin_thickness";
        private const string OriginalPartKey = "original_part_guid";
        private const string OffsettedMarginCurveKey = "offsetted_margin_curve";

        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly ImplantSupportManager _implantSupportManager;

        public ImplantMarginHelper(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(_director);
            _implantSupportManager = new ImplantSupportManager(_objectManager);
        }

        public void AddNewMargin(Mesh marginMesh, Guid marginCurveGuid, Curve trimmedMarginCurve,
            double marginThickness, Guid originalPartGuid, Curve offsettedCurve)
        {
            var id = _objectManager.AddNewBuildingBlock(IBB.ImplantMargin, marginMesh);
            var implantMarginRhObj = _director.Document.Objects.Find(id);
            
            SetImplantMarginAttributes(implantMarginRhObj, marginCurveGuid, trimmedMarginCurve, marginThickness, originalPartGuid, offsettedCurve);
            _implantSupportManager.SetImplantSupportInputObjectGuidKey(implantMarginRhObj, true);
        }

        private void SetImplantMarginAttributes(RhinoObject implantMarginRhObj, Guid marginCurveGuid, Curve trimmedMarginCurve,
            double marginThickness, Guid originalPartGuid, Curve offsettedCurve)
        {
            implantMarginRhObj.Attributes.UserDictionary.Set(MarginCurveKey, marginCurveGuid);
            implantMarginRhObj.Attributes.UserDictionary.Set(TrimmedMarginCurveKey, trimmedMarginCurve);
            implantMarginRhObj.Attributes.UserDictionary.Set(MarginThicknessKey, marginThickness);
            implantMarginRhObj.Attributes.UserDictionary.Set(OriginalPartKey, originalPartGuid);

            if (offsettedCurve != null)
            {
                implantMarginRhObj.Attributes.UserDictionary.Set(OffsettedMarginCurveKey, offsettedCurve);
            }
        }

        public List<RhinoObject> GetAllMargins()
        {
            var objManager = new CMFObjectManager(_director);
            return objManager.GetAllBuildingBlocks(IBB.ImplantMargin).ToList();
        }

        public Guid GetMarginCurve(RhinoObject marginRhinoObj)
        {
            if (marginRhinoObj.Attributes.UserDictionary.ContainsKey(MarginCurveKey))
            {
                return (Guid)marginRhinoObj.Attributes.UserDictionary[MarginCurveKey];
            }

            throw new Exception("Missing implant margin curve information!");
        }

        public Curve GetTrimmedMarginCurve(RhinoObject marginRhinoObj)
        {
            if (marginRhinoObj.Attributes.UserDictionary.ContainsKey(TrimmedMarginCurveKey))
            {
                return (Curve)marginRhinoObj.Attributes.UserDictionary[TrimmedMarginCurveKey];
            }

            throw new Exception("Missing trimmed implant margin curve information!");
        }

        public Curve GetOffsettedMarginCurve(RhinoObject marginRhinoObj)
        {
            if (marginRhinoObj.Attributes.UserDictionary.ContainsKey(OffsettedMarginCurveKey))
            {
                return (Curve)marginRhinoObj.Attributes.UserDictionary[OffsettedMarginCurveKey];
            }

            throw new Exception("Missing offsetted implant margin curve information!");
        }

        public double GetMarginThickness(RhinoObject marginRhinoObj)
        {
            if (marginRhinoObj.Attributes.UserDictionary.ContainsKey(MarginThicknessKey))
            {
                return (double)marginRhinoObj.Attributes.UserDictionary[MarginThicknessKey];
            }

            throw new Exception("Missing implant margin thickness information!");
        }

        public Guid GetOriginalPartBelongTo(RhinoObject marginRhinoObj)
        {
            if (marginRhinoObj.Attributes.UserDictionary.ContainsKey(OriginalPartKey))
            {
                return (Guid)marginRhinoObj.Attributes.UserDictionary[OriginalPartKey];
            }

            throw new Exception("Missing the original part that implant margin belong to!");
        }

        public void ReplaceExistingMargin(Guid existingMarginId, Mesh marginMesh, Curve trimmedMarginCurve, Curve offsettedCurve)
        {
            var objManager = new CMFObjectManager(_director);
            var rhinoObject = _director.Document.Objects.Find(existingMarginId);
            var marginCurve = (Guid)rhinoObject.Attributes.UserDictionary[MarginCurveKey];
            var marginThickness = (double)rhinoObject.Attributes.UserDictionary[MarginThicknessKey];
            var originalPartGuid = (Guid)rhinoObject.Attributes.UserDictionary[OriginalPartKey];

            var id = objManager.SetBuildingBlock(IBB.ImplantMargin, marginMesh, existingMarginId);
            rhinoObject = _director.Document.Objects.Find(id);

            rhinoObject.Attributes.UserDictionary.Set(MarginCurveKey, marginCurve);
            rhinoObject.Attributes.UserDictionary.Set(TrimmedMarginCurveKey, trimmedMarginCurve);
            rhinoObject.Attributes.UserDictionary.Set(MarginThicknessKey, marginThickness);
            rhinoObject.Attributes.UserDictionary.Set(OriginalPartKey, originalPartGuid);

            if (offsettedCurve != null)
            {
                rhinoObject.Attributes.UserDictionary.Set(OffsettedMarginCurveKey, offsettedCurve);
            }
            else if (rhinoObject.Attributes.UserDictionary.ContainsKey(OffsettedMarginCurveKey))
            {
                rhinoObject.Attributes.UserDictionary.Remove(OffsettedMarginCurveKey);
            }

            InvalidateDependentTransitions(new List<Guid> {existingMarginId}, out var dependentTransitionIds);

            var invalidatedInputIds = new List<Guid>() {existingMarginId};
            invalidatedInputIds.AddRange(dependentTransitionIds);
            _implantSupportManager.SetDependentImplantSupportsOutdated(invalidatedInputIds);
            _implantSupportManager.SetImplantSupportInputObjectGuidKey(rhinoObject, true, false);
        }

        public void InvalidateAllMargins()
        {
            var objectManager = new CMFObjectManager(_director);
            if (!objectManager.HasBuildingBlock(IBB.ImplantMargin))
            {
                return;
            }

            var implantMarginBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantMargin).ToList();
            implantMarginBlocks.ToList().ForEach(block => objectManager.DeleteObject(block.Id));
        }

        public void InvalidateAllTransitions()
        {
            var objectManager = new CMFObjectManager(_director);
            if (!objectManager.HasBuildingBlock(IBB.ImplantTransition))
            {
                return;
            }

            var implantTransitionBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantTransition);
            implantTransitionBlocks.ToList().ForEach(block => objectManager.DeleteObject(block.Id));
        }

        public void InvalidateDependentTransitions(List<Guid> marginIds, out List<Guid> dependentTransitionIds)
        {
            var helper = new ImplantTransitionObjectHelper(_director);
            dependentTransitionIds = helper.GetDependentTransitionIds(marginIds);
            dependentTransitionIds.ForEach(id => _objectManager.DeleteObject(id));
        }
    }
}
