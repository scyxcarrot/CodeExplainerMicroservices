using IDS.CMF.V2.Logics;
using IDS.Core.Plugin;
using IDS.Interface.Logic;
using IDS.RhinoInterface.Converter;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;

namespace IDS.CMF.Visualization
{
    public class BoneThicknessAnalysisGenerationHelper : IBoneThicknessAnalysisGenerationHelper
    {
        private readonly CMFObjectManager _objectManager;
        private readonly RhinoObject _boneObject;

        public BoneThicknessAnalysisGenerationHelper(CMFImplantDirector director, RhinoObject boneObject)
        {
            _objectManager = new CMFObjectManager(director);
            _boneObject = boneObject;
        }

        public bool GetBoneThicknessCacheData(Guid objectId, out double[] thicknessData)
        {
            return _objectManager.GetBuildingBlockThicknessData(objectId, out thicknessData);
        }

        public void SetBoneThicknessCacheData(Guid objectId, double[] thicknessData)
        {
            _objectManager.SetBuildingBlockThicknessData(objectId, thicknessData);
        }

        public LogicStatus PrepareLogicParameters(out BoneThicknessAnalysisGenerationParameters parameters)
        {
            var boneMesh = (Mesh)_boneObject.Geometry;

            if (boneMesh == null)
            {
                throw new ArgumentException("Bone Object is not a mesh");
            }

            boneMesh.Faces.CullDegenerateFaces();
            boneMesh.Compact();

            parameters = new BoneThicknessAnalysisGenerationParameters
            {
                ObjectId = _boneObject.Id,
                BoneMesh = RhinoMeshConverter.ToIDSMesh(boneMesh)
            };

            return LogicStatus.Success;
        }

        public LogicStatus ProcessLogicResult(BoneThicknessAnalysisGenerationResult result)
        {
            return LogicStatus.Success;
        }

        public double[] EasyBoneThicknessGeneration()
        {
            var logic = new BoneThicknessAnalysisGenerationLogic(new IDSRhinoConsole(), this);
            return (logic.Execute(out var result) == LogicStatus.Success) ? result.ThicknessData : null;
        }
    }
}
