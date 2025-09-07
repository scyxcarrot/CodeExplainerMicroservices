using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.V2.Logics;
using IDS.CMF.Visualization;
using IDS.Core.V2.Utilities;
using IDS.RhinoInterface.Converter;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IDS.CMF.Query
{
    public class QcDocBoneThicknessMapData
    {
        public double LowerBoundThickness { get;  }
        public double UpperBoundThickness { get; }
        public Mesh ThicknessMesh { get; }

        public double[] ThicknessData { get; }

        public QcDocBoneThicknessMapData(double lowerBoundThickness, double upperBoundThickness, Mesh thicknessMesh, double[] thicknessData)
        {
            LowerBoundThickness = lowerBoundThickness;
            UpperBoundThickness = upperBoundThickness;
            ThicknessMesh = thicknessMesh;
            ThicknessData = thicknessData;
        }
    }

    public class QcDocBoneThicknessMapQuery
    {
        private readonly CMFImplantDirector _director;
        private readonly Dictionary<CasePreferenceDataModel, Dictionary<RhinoObject, List<Screw>>> _casePrefDict;
        private readonly Dictionary<RhinoObject, QcDocBoneThicknessMapData> _boneDict;

        private CMFScrewAnalysis _screwAnalysis;
        private CMFScrewAnalysis ScrewAnalysis
        {
            get
            {
                if (_screwAnalysis == null)
                {
                    _screwAnalysis = new CMFScrewAnalysis(_director);
                }

                return _screwAnalysis;
            }
        }

        public QcDocBoneThicknessMapQuery(CMFImplantDirector director)
        {
            _director = director;
            _casePrefDict = new Dictionary<CasePreferenceDataModel, Dictionary<RhinoObject, List<Screw>>>();
            _boneDict = new Dictionary<RhinoObject, QcDocBoneThicknessMapData>();
        }

        public List<KeyValuePair<string, TimeSpan>> GenerateAllNeededBoneThicknessData()
        {
            var processingTimes = new List<KeyValuePair<string, TimeSpan>>();

            foreach (var casePrefData in _director.CasePrefManager.CasePreferences)
            {
                var processingTime = GenerateBoneThicknessData(casePrefData);
                if (processingTime.Any())
                {
                    processingTimes.AddRange(processingTime);
                }
            }

            return processingTimes;
        }

        public List<KeyValuePair<string, TimeSpan>> GenerateBoneThicknessData(CasePreferenceDataModel casePrefData)
        {
            var processingTimes = new List<KeyValuePair<string, TimeSpan>>();

            if (_casePrefDict.ContainsKey(casePrefData))
            {
                return processingTimes;
            }
            else
            {
                _casePrefDict.Add(casePrefData, new Dictionary<RhinoObject, List<Screw>>());
            }

            var bonesScrewsData = ScrewAnalysis.GroupScrewWithBone(casePrefData);

            foreach (var boneScrewData in bonesScrewsData)
            {
                var bone = boneScrewData.Key;

                if (_boneDict.ContainsKey(bone))
                {
                    _casePrefDict[casePrefData].Add(bone, boneScrewData.Value);
                    continue;
                }

                processingTimes.Add(GenerateBoneThicknessData(bone));

                _boneDict.Add(bone, null);
                _casePrefDict[casePrefData].Add(bone, boneScrewData.Value);
            }

            return processingTimes;
        }

        public KeyValuePair<string, TimeSpan> GenerateBoneThicknessData(RhinoObject bone)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var helper = new BoneThicknessAnalysisGenerationHelper(_director, bone);
            helper.EasyBoneThicknessGeneration();

            stopwatch.Stop();
            return new KeyValuePair<string, TimeSpan>($"GenerateBoneThicknessData {bone.Name}", stopwatch.Elapsed);
        }

        public Dictionary<RhinoObject, List<Screw>> GetGroupScrewWithBone(CasePreferenceDataModel casePrefData)
        {
            GenerateBoneThicknessData(casePrefData);
            return _casePrefDict[casePrefData];
        }

        public Mesh DoWallThicknessAnalysisForQCDoc(RhinoObject rhObject, out double lowerBound, out double upperBound)
        {
            if (_boneDict.ContainsKey(rhObject) && _boneDict[rhObject] != null)
            {
                var value = _boneDict[rhObject];
                lowerBound = value.LowerBoundThickness;
                upperBound = value.UpperBoundThickness;
                return value.ThicknessMesh;
            }

            var generationHelper = new BoneThicknessAnalysisGenerationHelper(_director, rhObject);
            var thicknessData = generationHelper.EasyBoneThicknessGeneration();

            var reportingHelper = new BoneThicknessAnalysisReportingHelper(_director, rhObject, thicknessData, false, false);
            var thicknessMesh = reportingHelper.DoWallThicknessAnalysisForQCDoc(out lowerBound, out upperBound);
            _boneDict[rhObject] = new QcDocBoneThicknessMapData(lowerBound, upperBound, thicknessMesh, thicknessData);
            
            return thicknessMesh;
        }

        public Mesh CreateWallThicknessAnalysisMeshForART(RhinoObject rhinoObject, double minThickness, double maxThickness)
        {
            if (!_boneDict.ContainsKey(rhinoObject) || 
                _boneDict[rhinoObject] == null)
            {
                return null;
            }

            var thicknessData = _boneDict[rhinoObject].ThicknessData;
            var constraintThicknessData = LimitUtilities.ApplyLimitForDoubleArray(thicknessData, 
                minThickness - BoneThicknessAnalysisForART.Tolerant, 
                maxThickness);
            var boneMesh = (Mesh)rhinoObject.Geometry;
            MeshAnalysisUtilities.CreateTriangleDiagnosticMesh(RhinoMeshConverter.ToIDSMesh(boneMesh), 
                minThickness, maxThickness, constraintThicknessData,
                BoneThicknessAnalysisForART.OutOfRangeColor, out var newMesh, out var verticesColors);

            return BoneThicknessAnalysisReportingHelper.CreateRhinoThicknessMesh(newMesh, verticesColors);
        }
    }
}
