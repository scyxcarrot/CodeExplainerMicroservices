using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.Interface.Loader;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class AnatomyUpdater : RecutImporter
    {
        private readonly string keyTransformationMatrix = "transformation_matrix";
        private readonly string keyIsAddedAnatomy = AttributeKeys.KeyIsAddedAnatomy;
        private readonly string keyIsReplacedAnatomy = AttributeKeys.KeyIsReplacedAnatomy;

        private readonly List<Tuple<string, Transform>> _transformationMatrixMap;
        private readonly List<IOsteotomyHandler> _osteotomyHandler;

        public AnatomyUpdater(CMFImplantDirector director, List<Tuple<string, Transform>> transformationMatrixMap, List<IOsteotomyHandler> osteotomyHandler) : 
            base(director, false, true, false, false)
        {
            _transformationMatrixMap = transformationMatrixMap;
            _osteotomyHandler = osteotomyHandler;
        }

        protected override void UpdateTransformationMatrix(RhinoObject existingRhinoObject, RhinoObject newRhinoObject)
        {
            var proPlanImportComponent = new ProPlanImportComponent();
            var partName = proPlanImportComponent.GetPartName(newRhinoObject.Name);

            var transform = GetTransformationMatrix(partName);
            if (transform == Transform.Identity && existingRhinoObject != null && existingRhinoObject.Attributes.UserDictionary.ContainsKey(keyTransformationMatrix))
            {
                transform = (Transform)existingRhinoObject.Attributes.UserDictionary[keyTransformationMatrix];
            }

            newRhinoObject.Attributes.UserDictionary.Set(keyTransformationMatrix, transform);
        }

        protected override void UpdateOsteotomyHandler(RhinoObject newRhinoObject)
        {
            var osteotomyHandler = new OsteotomyHandlerData();
            var data = _osteotomyHandler.FirstOrDefault(h => newRhinoObject.Name.ToLower().Contains(h.Name.ToLower()));

            if (data == null)
            {
                // This means that we are replacing the part without any osteotomy handler information
                osteotomyHandler.ClearSerialized(newRhinoObject.Attributes.UserDictionary);
                return;
            }

            osteotomyHandler = new OsteotomyHandlerData(data.Type, data.Thickness, data.Identifier, data.Coordinate);
            osteotomyHandler.Serialize(newRhinoObject.Attributes.UserDictionary);
        }

        protected override void UpdateFlag(RhinoObject existingRhinoObject, RhinoObject newRhinoObject)
        {
            if (newRhinoObject.Attributes.UserDictionary.ContainsKey(keyIsAddedAnatomy) || newRhinoObject.Attributes.UserDictionary.ContainsKey(keyIsReplacedAnatomy))
            {
                return;
            }
            
            var flagKey = keyIsReplacedAnatomy;

            if (existingRhinoObject == null)
            {
                flagKey = keyIsAddedAnatomy;
            }
                
            newRhinoObject.Attributes.UserDictionary.Set(flagKey, true);
        }

        protected override Transform GetTransformationMatrix(string partName)
        {
            var transform = Transform.Identity;

            var transformationMatrixItem = _transformationMatrixMap.FirstOrDefault(m => m.Item1.ToLower() == partName.ToLower());
            if (transformationMatrixItem != null)
            {
                transform = transformationMatrixItem.Item2;
            }

            return transform;
        }

        protected override bool HasPartsRepositioned(Dictionary<string, Mesh> meshList)
        {
            //no checking for Update anatomy
            return false;
        }
    }
}
