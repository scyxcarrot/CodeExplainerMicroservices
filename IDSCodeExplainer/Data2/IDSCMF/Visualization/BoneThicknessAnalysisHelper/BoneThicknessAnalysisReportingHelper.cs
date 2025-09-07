using IDS.CMF.V2.Constants;
using IDS.CMF.V2.Logics;
using IDS.Core.Plugin;
using IDS.Interface.Geometry;
using IDS.Interface.Logic;
using IDS.RhinoInterface.Converter;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.CMF.Visualization
{
    public class BoneThicknessAnalysisReportingHelper : IBoneThicknessAnalysisReportingHelper
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly RhinoObject _boneObject;
        private readonly double[] _thicknessData;
        private double _minWallThickness;
        private double _maxWallThickness;
        private readonly bool _showScale;
        private readonly bool _overrideObject;

        public BoneThicknessAnalysisReportingHelper(CMFImplantDirector director, RhinoObject boneObject, double[] thicknessData,
            bool showScale = true, bool overrideObject = true):
            this(director, boneObject, thicknessData, BoneThicknessAnalysisConstants.DefaultMinThickness, 
                BoneThicknessAnalysisConstants.DefaultMaxThickness, showScale, overrideObject)
        {
        }

        public BoneThicknessAnalysisReportingHelper(CMFImplantDirector director, RhinoObject boneObject, double[] thicknessData, 
            double minWallThickness, double maxWallThickness,
            bool showScale = true, bool overrideObject = true)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
            _boneObject = boneObject;
            _thicknessData = thicknessData;
            _minWallThickness = minWallThickness;
            _maxWallThickness = maxWallThickness;
            _showScale = showScale;
            _overrideObject = overrideObject;
        }

        public bool GetBuildingBlockThicknessMinMax(Guid objectId, ref double minWallThickness, ref double maxWallThickness)
        {
            return _objectManager.GetBuildingBlockThicknessMinMax(objectId, ref minWallThickness, ref maxWallThickness);
        }

        public void SetBuildingBlockThicknessMinMax(Guid objectId, double minWallThickness, double maxWallThickness)
        {
            _objectManager.SetBuildingBlockThicknessMinMax(objectId, minWallThickness, maxWallThickness);
        }

        public static Mesh CreateRhinoThicknessMesh(BoneThicknessAnalysisReportingResult result)
        {
            return CreateRhinoThicknessMesh(result.NewBoneMesh, result.VerticesColors);
        }


        public static Mesh CreateRhinoThicknessMesh(IMesh boneMesh, Color[] verticesColors)
        {
            var rhinoMesh = RhinoMeshConverter.ToRhinoMesh(boneMesh);

            foreach (var vertexColor in verticesColors)
            {
                rhinoMesh.VertexColors.Add(vertexColor);
            }

            return rhinoMesh;
        }

        public LogicStatus PrepareLogicParameters(out BoneThicknessAnalysisReportingParameters parameters)
        {
            var boneMesh = (Mesh)_boneObject.Geometry;

            if (boneMesh == null)
            {
                throw new ArgumentException("Bone Object is not a mesh");
            }

            boneMesh.Faces.CullDegenerateFaces();
            boneMesh.Compact();

            parameters = new BoneThicknessAnalysisReportingParameters
            {
                ObjectId = _boneObject.Id,
                BoneMesh = RhinoMeshConverter.ToIDSMesh(boneMesh),
                ThicknessData = _thicknessData,
                DefaultColor = _boneObject.Attributes.ObjectColor,
                MinWallThickness = _minWallThickness,
                MaxWallThickness = _maxWallThickness
            };

            return LogicStatus.Success;
        }

        public LogicStatus ProcessLogicResult(BoneThicknessAnalysisReportingResult result)
        {
            var thicknessMesh = CreateRhinoThicknessMesh(result);

            if (_overrideObject && !BoneThicknessAnalyzableObjectManager.HandleObjectOverriding(_director, thicknessMesh, _boneObject))
            {
                return LogicStatus.Failure;
            }

            AnalysisScaleConduit.ConduitProxy.LowerBound = result.LowerBound;
            AnalysisScaleConduit.ConduitProxy.UpperBound = result.UpperBound;
            AnalysisScaleConduit.ConduitProxy.Title = "Bone Thickness Analysis";
            AnalysisScaleConduit.ConduitProxy.Enabled = _showScale;

            _director.Document.Views.Redraw();

            return LogicStatus.Success;
        }

        public Mesh DoWallThicknessAnalysisForQCDoc(out double lowerBound, out double upperBound)
        {
            GetBuildingBlockThicknessMinMax(_boneObject.Id, ref _minWallThickness, ref _maxWallThickness);

            lowerBound = _minWallThickness;
            upperBound = _maxWallThickness;

            var boneThicknessAnalysisLogic = new BoneThicknessAnalysisReportingLogic(new IDSRhinoConsole(), this);
            if (boneThicknessAnalysisLogic.Execute(out var result) != LogicStatus.Success)
            {
                return null;
            }

            lowerBound = result.LowerBound;
            upperBound = result.UpperBound;

            return CreateRhinoThicknessMesh(result);
        }
    }
}
