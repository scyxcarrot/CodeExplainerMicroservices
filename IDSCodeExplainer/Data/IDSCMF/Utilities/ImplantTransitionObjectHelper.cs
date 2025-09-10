using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.CMF.Utilities
{
    public class ImplantTransitionObjectHelper
    {
        private const string TransitionDerivedObjectGuidKey = "derived_object_guid";
        private const string TransitionFullCurveKey = "full_curve";
        private const string TransitionTrimmedCurveKey = "trimmed_curve";

        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly ImplantSupportManager _implantSupportManager;

        public ImplantTransitionObjectHelper(CMFImplantDirector director)
        {
            this._director = director;
            _objectManager = new CMFObjectManager(director);
            _implantSupportManager = new ImplantSupportManager(_objectManager);
        }

        public void AddNewTransition(Mesh transitionMesh, ImplantTransitionDataModel dataModel)
        {
            var id = _objectManager.AddNewBuildingBlock(IBB.ImplantTransition, transitionMesh);
            var transitionRhObj = _director.Document.Objects.Find(id);

            SetAttributes(ref transitionRhObj, dataModel.CurveA, "_a");
            SetAttributes(ref transitionRhObj, dataModel.CurveB, "_b");

            _implantSupportManager.SetImplantSupportInputObjectGuidKey(transitionRhObj, true);
        }

        public List<Guid> GetDependentTransitionIds(List<Guid> derievedObjectIdsToCheck)
        {
            var transitionIds = new List<Guid>();

            var objectManager = new CMFObjectManager(_director);
            var implantTransitionBlocks = objectManager.GetAllBuildingBlocks(IBB.ImplantTransition);
            foreach (var implantTransitionBlock in implantTransitionBlocks)
            {
                var derievedObjectIds = GetDerivedObjectGuids(implantTransitionBlock);
                foreach (var id in derievedObjectIds)
                {
                    if (derievedObjectIdsToCheck.Contains(id))
                    {
                        transitionIds.Add(implantTransitionBlock.Id);
                    }
                }
            }

            return transitionIds;
        }

        private void SetAttributes(ref RhinoObject rhinoObject, ImplantTransitionInputCurveDataModel dataModel, string postfix)
        {
            rhinoObject.Attributes.UserDictionary.Set($"{TransitionDerivedObjectGuidKey}{postfix}", dataModel.DerivedObjectGuid);
            rhinoObject.Attributes.UserDictionary.Set($"{TransitionFullCurveKey}{postfix}", dataModel.FullCurve);
            rhinoObject.Attributes.UserDictionary.Set($"{TransitionTrimmedCurveKey}{postfix}", dataModel.TrimmedCurve);
        }

        public List<Guid> GetDerivedObjectGuids(RhinoObject rhinoObject)
        {
            var ids = new List<Guid>();
            ids.Add(GetDerivedObjectGuid(rhinoObject, "_a"));
            ids.Add(GetDerivedObjectGuid(rhinoObject, "_b"));
            return ids;
        }

        private Guid GetDerivedObjectGuid(RhinoObject rhinoObject, string postfix)
        {
            if (rhinoObject.Attributes.UserDictionary.ContainsKey($"{TransitionDerivedObjectGuidKey}{postfix}"))
            {
                return (Guid)rhinoObject.Attributes.UserDictionary[$"{TransitionDerivedObjectGuidKey}{postfix}"];
            }

            throw new Exception($"Missing derieved object guid {postfix} information!");
        }
    }
}
