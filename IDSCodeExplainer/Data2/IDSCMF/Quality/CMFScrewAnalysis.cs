using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.RhinoFree.Utilities;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.RhinoInterfaces.Converter;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Quality
{
    public class CMFScrewAnalysis : IDisposable
    {
        private readonly CMFImplantDirector _director;
        private readonly CMFObjectManager _objectManager;
        private readonly ImplantManager _implantManager;
        private readonly CasePreferenceManager _casePrefManager;
        private readonly List<RhinoObject> _implantConstraintRhObjs;

        private List<Mesh> _implantLowLoDConstraintMeshes;

        private List<Mesh> implantLowLoDConstraintMeshes
        {
            get { 

                if (_implantLowLoDConstraintMeshes == null)
                {
                    _implantLowLoDConstraintMeshes = new List<Mesh>();
                }

                if (_implantLowLoDConstraintMeshes.Count != _implantConstraintRhObjs.Count)
                {
                    _implantLowLoDConstraintMeshes.ForEach(x => x.Dispose());
                    _implantLowLoDConstraintMeshes.Clear();
                }

                if (!_implantLowLoDConstraintMeshes.Any())
                {
                    _implantConstraintRhObjs.ForEach(x =>
                    {
                        Mesh tmpLowLod;
                        _objectManager.GetBuildingBlockLoDLow(x.Id, out tmpLowLod);
                        _implantLowLoDConstraintMeshes.Add(tmpLowLod.DuplicateMesh());
                    });
                }

                return _implantLowLoDConstraintMeshes;
            }
        }

        public CMFScrewAnalysis(CMFImplantDirector director)
        {
            _director = director;
            _objectManager = new CMFObjectManager(director);
            _implantManager = director.ImplantManager;
            _casePrefManager = director.CasePrefManager;

            var constraintMeshQuery = new ConstraintMeshQuery(_objectManager);
            _implantConstraintRhObjs = constraintMeshQuery.GetConstraintRhinoObjectForImplant().ToList();
        }

        public void PerformMinMaxDistanceCheck(Screw screw, out List<Screw> tooCloseDistanceProblems,
            out List<Screw> tooFarDistanceProblems, bool needUpdateConnection = true)
        {
            var screwBrand = _director.CasePrefManager.SurgeryInformation.ScrewBrand;

            PerformMinMaxDistanceCheck(screw, screwBrand, out tooCloseDistanceProblems, out tooFarDistanceProblems, needUpdateConnection);
        }

        public void PerformMinMaxDistanceCheck(Screw screw, EScrewBrand screwBrand,
            out List<Screw> tooCloseDistanceProblems,
            out List<Screw> tooFarDistanceProblems, bool needUpdateConnection = true)
        {
            var casePreferenceData = _objectManager.GetCasePreference(screw);

            var acceptableMinDistance = CasePreferencesHelper.GetAcceptableMinScrewDistance(screwBrand, casePreferenceData.CasePrefData.ImplantTypeValue);

            var screws = GetScrewsBelongTheCase(casePreferenceData);
            var distances = GetScrewDistances(screw, screws);
            var bones = GetScrewBone(screws);
            var connectedWithPlateScrews = GetConnectedWithPlateScrews(casePreferenceData, screws, needUpdateConnection).ToDictionary(kv => kv.Key.Id, kv => kv.Value);
            var connectedWithPlateScrew = new List<Screw>();
            if (connectedWithPlateScrews.ContainsKey(screw.Id))
            {
                connectedWithPlateScrew = connectedWithPlateScrews[screw.Id];
            }
            var acceptableMaxDistances = GetScrewAcceptableMaxDistances(screw, connectedWithPlateScrew, screwBrand);

            tooCloseDistanceProblems = PerformMinDistanceCheck(acceptableMinDistance, screw, distances, bones, connectedWithPlateScrews);
            tooFarDistanceProblems = PerformMaxDistanceCheck(acceptableMaxDistances, screw, distances, connectedWithPlateScrews);
        }

        public List<Screw> PerformAllScrewInsertionTrajectoryCheck()
        {
            var constraintMeshQuery = new ConstraintMeshQuery(_objectManager);
            var bones = constraintMeshQuery.GetConstraintMeshesForImplant(false);

            var implantComponent = new ImplantCaseComponent();
            var insertTrajectoryProblems = new List<Screw>();

            object sync = new object();
            var addIntoListThreadSafe = new Func<Screw, bool>(n =>
            {
                lock (sync) { insertTrajectoryProblems.Add(n); }
                return true;
            });

            foreach (var casePreferenceData in _casePrefManager.CasePreferences)
            {
                var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ImplantPreview, casePreferenceData);
                var implantPreview = _objectManager.GetBuildingBlock(buildingBlock);

                var targetMeshes = bones.ToList();
                if (implantPreview != null)
                {
                    targetMeshes.Add((Mesh)implantPreview.Geometry);
                }

                var mesh = MeshUtilities.AppendMeshes(targetMeshes);

                var screws = GetScrews(casePreferenceData);

                screws.ForEach(screw =>
                {
                    if (!IsPerformInsertTrajectoryCheckOk(screw, mesh))
                    {
                        addIntoListThreadSafe(screw);
                    }
                });

            }

            return insertTrajectoryProblems;
        }

        public bool IsPerformInsertionTrajectoryCheckOk(Screw screw)
        {
            var casePreferenceData = _objectManager.GetCasePreference(screw);
            var implantComponent = new ImplantCaseComponent();
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ImplantPreview, casePreferenceData);
            var implantPreview = _objectManager.GetBuildingBlock(buildingBlock);

            var constraintMeshQuery = new ConstraintMeshQuery(_objectManager);
            var bones = constraintMeshQuery.GetConstraintMeshesForImplant(false);

            var targetMeshes = bones.ToList();
            if (implantPreview != null)
            {
                targetMeshes.Add((Mesh)implantPreview.Geometry);
            }

            var mesh = MeshUtilities.AppendMeshes(targetMeshes);
            return IsPerformInsertTrajectoryCheckOk(screw, mesh);
        }

        public double CalculateScrewAngle(Screw screw, Vector3d referenceDirection)
        {
            return MathUtilities.CalculateDegrees(screw.Direction, referenceDirection);
        }

        public Dictionary<RhinoObject, List<Screw>> GroupScrewWithBone(CasePreferenceDataModel casePreferenceData)
        {
            var bonesScrewsData = new Dictionary<RhinoObject, List<Screw>>();
            var screws = GetScrews(casePreferenceData);
            var screwsBonesData = GetScrewBone(screws);

            foreach (var screwBoneData in screwsBonesData)
            {
                var screw = screwBoneData.Key;
                var boneName = screwBoneData.Value;

                if (boneName == null)
                {
                    continue;
                }

                var bone = _implantConstraintRhObjs.First(obj => (obj.Name == boneName));

                if (!bonesScrewsData.ContainsKey(bone))
                {
                    bonesScrewsData.Add(bone, new List<Screw>() {screw});
                    continue;
                }

                bonesScrewsData[bone].Add(screw);
            }

            return bonesScrewsData;
        }

        private List<Screw> GetScrews(CasePreferenceDataModel casePreferenceData)
        {
            var screws = GetScrewsBelongTheCase(casePreferenceData);

            //make sure all screw indexes are assigned
            var indexes = screws.Select(screw => screw.Index).ToArray();
            if (indexes.Distinct().Count() != indexes.Length)
            {
                throw new Exception("Duplicate screw numbers!");
            }

            return screws;
        }

        private List<Screw> GetScrewsBelongTheCase(CasePreferenceDataModel casePreferenceData)
        {
            var implantComponent = new ImplantCaseComponent();
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
            var screws = _objectManager.GetAllBuildingBlocks(buildingBlock).Select(screw => screw as Screw).ToList();
            return screws;
        }

        private Dictionary<Screw, Dictionary<Screw, double>> GetScrewDistances(List<Screw> screws)
        {
            var distances = new Dictionary<Screw, Dictionary<Screw, double>>();
            var screwList = new List<Screw>(screws);

            for (var i = screwList.Count - 1; i >= 0; i--)
            {
                var thisScrew = screwList[i];

                foreach (var otherScrew in screwList)
                {
                    if (otherScrew.Id == thisScrew.Id)
                    {
                        continue;
                    }
                    
                    var calculateDistance = GetDistanceBetweenTwoScrew(thisScrew, otherScrew);
                    HandleScrewDistancesInfo(ref distances, thisScrew, otherScrew, calculateDistance);
                    HandleScrewDistancesInfo(ref distances, otherScrew, thisScrew, calculateDistance);
                }

                screwList.RemoveAt(i);
            }

            return distances;
        }

        private Dictionary<Screw, double> GetScrewDistances(Screw thisScrew, List<Screw> screws)
        {
            var distances = new Dictionary<Screw, double>();

            foreach (var otherScrew in screws)
            {
                if (otherScrew.Id == thisScrew.Id)
                {
                    continue;
                }

                var calculateDistance = GetDistanceBetweenTwoScrew(thisScrew, otherScrew);
                distances.Add(otherScrew, calculateDistance);

#if (INTERNAL)
                if (CMFImplantDirector.IsDebugMode)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"ScrewDistance [{thisScrew.Index}]-[{otherScrew.Index}]: {calculateDistance}");
                }
#endif
            }

            return distances;
        }

        private double GetDistanceBetweenTwoScrew(Screw screwA, Screw screwB)
        {
            var screwACasePref = _objectManager.GetCasePreference(screwA);
            var screwBCasePref = _objectManager.GetCasePreference(screwB);

            var screwARotationCenter =
                ScrewUtilities.FindDotTheScrewBelongsTo(screwA, screwACasePref.ImplantDataModel.DotList);
            var screwBRotationCenter =
                ScrewUtilities.FindDotTheScrewBelongsTo(screwB, screwBCasePref.ImplantDataModel.DotList);

            return (RhinoPoint3dConverter.ToPoint3d(screwBRotationCenter.Location) - RhinoPoint3dConverter.ToPoint3d(screwARotationCenter.Location)).Length;
        }

        private void HandleScrewDistancesInfo(ref Dictionary<Screw, Dictionary<Screw, double>> screwDistances, Screw screwA, Screw screwB, double distance)
        {
            if (screwDistances.ContainsKey(screwA))
            {
                if (!screwDistances[screwA].ContainsKey(screwB))
                {
                    screwDistances[screwA].Add(screwB, distance);
                }
                else if (Math.Abs(screwDistances[screwA][screwB] - distance) < 0.0001)
                {
                    throw new Exception("Mismatched screw distance");
                }
            }
            else
            {
                screwDistances.Add(screwA, new Dictionary<Screw, double>
                {
                    { screwB, distance }
                });
            }

#if (INTERNAL)
            if (CMFImplantDirector.IsDebugMode)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"ScrewDistance [{screwA.Index}]-[{screwB.Index}]: {distance}");
            }
#endif
        }

        private Dictionary<Screw, string> GetScrewBone(List<Screw> screws)
        {
            var screwBones = new Dictionary<Screw, string>();

            foreach (var screw in screws)
            {
                var lowLoD = ScrewUtilities.FindIntersection(implantLowLoDConstraintMeshes, screw);
                var meshName = string.Empty;
                if (lowLoD == null)
                {
                    meshName = null;
                }
                else
                {
                    var meshIdx = implantLowLoDConstraintMeshes.IndexOf(lowLoD);
                    var rhObj = _implantConstraintRhObjs[meshIdx];
                    meshName = rhObj.Name;
                }

                screwBones.Add(screw, meshName);

#if (INTERNAL)
                if (CMFImplantDirector.IsDebugMode)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"ScrewBone [{screw.Index}]: {meshName}");
                }
#endif
            }

            return screwBones;
        }


        private Dictionary<Screw, List<Screw>> GetConnectedWithPlateScrews(CasePreferenceDataModel casePreferenceData, 
            List<Screw> screws, bool needUpdateConnection = true)
        {
            var connectedScrews = new Dictionary<Screw, List<Screw>>();

            var connectionList = casePreferenceData.ImplantDataModel.ConnectionList;
            if (needUpdateConnection)
            {
                _implantManager.InvalidateConnectionBuildingBlock(casePreferenceData);
            }

            var dotPastilles = new List<DotPastille>();
            screws.ForEach(x =>
            {
                dotPastilles.Add(ImplantCreationUtilities.
                    FindClosestDotPastille(casePreferenceData.ImplantDataModel.DotList, x.HeadPoint));
            });

            foreach (var dotPastille in dotPastilles)
            {
                var result = ImplantCreationUtilitiesRhinoFree.FindNeigbouringDotPastilles(dotPastille, connectionList, ConnectionType.EPlate);

                var currentScrew = screws.First(screw => screw.Id == dotPastille.Screw.Id);

                result.ForEach(x =>
                {
                    var otherScrew = screws.First(screw => screw.Id == x.Screw.Id);

                    HandleInfo(ref connectedScrews, currentScrew, otherScrew);
                    HandleInfo(ref connectedScrews, otherScrew, currentScrew);
                });

            }

            return connectedScrews;
        }

        private List<Screw> PerformMinDistanceCheck(double acceptableMinDistance, Screw screw, Dictionary<Screw, double> screwDistances, Dictionary<Screw, string> screwBones, Dictionary<Guid, List<Screw>> connectedWithPlateScrews)
        {
            //1. Measure the minimum distance when the screws are on the same bone.
            //2. Measure the minimum distance when the screws are on the different bone but connected by the plate.
            var tooCloseDistanceProblems = new List<Screw>();

            var thisScrew = screw;
            var thisBone = screwBones.First(s => s.Key.Id == thisScrew.Id).Value;

            foreach (var otherScrew in screwDistances)
            {
                var distance = otherScrew.Value;
                if (distance < acceptableMinDistance)
                {
                    if (screwBones[otherScrew.Key] == thisBone || connectedWithPlateScrews.Keys.Any(s => s == thisScrew.Id) && connectedWithPlateScrews.First(s => s.Key == thisScrew.Id).Value.Contains(otherScrew.Key))
                    {
                        tooCloseDistanceProblems.Add(otherScrew.Key);
                    }
                }
            }

            return tooCloseDistanceProblems;
        }

        private Dictionary<Screw, List<Screw>> PerformMaxDistanceCheck(Dictionary<Screw, Dictionary<Screw, double>> acceptableMaxDistances, Dictionary<Screw, Dictionary<Screw, double>> screwDistances, Dictionary<Screw, List<Screw>> connectedWithPlateScrews)
        {
            //1. Measure the maximum distance when the screws are connected by the plate
            var tooFarDistanceProblems = new Dictionary<Screw, List<Screw>>();
            var connectedWithPlateScrewsList = connectedWithPlateScrews.ToDictionary(c => c.Key, c => new List<Screw>(c.Value));

            foreach (var connection in connectedWithPlateScrewsList)
            {
                var currentScrew = connection.Key;
                var thisDistances = screwDistances[currentScrew];

                foreach (var targetScrew in connection.Value)
                {
                    var distance = thisDistances[targetScrew];
                    var acceptableMaxDistance = acceptableMaxDistances[currentScrew][targetScrew];
                    if (distance > acceptableMaxDistance)
                    {
                        HandleInfo(ref tooFarDistanceProblems, currentScrew, targetScrew);
                        HandleInfo(ref tooFarDistanceProblems, targetScrew, currentScrew);
                    }

                    if (connectedWithPlateScrewsList.ContainsKey(targetScrew))
                    {
                        connectedWithPlateScrewsList[targetScrew].Remove(currentScrew);
                    }
                }
            }

            return tooFarDistanceProblems;
        }

        private List<Screw> PerformMaxDistanceCheck(Dictionary<Screw, double> acceptableMaxDistances, Screw screw, Dictionary<Screw, double> screwDistances, Dictionary<Guid, List<Screw>> connectedWithPlateScrews)
        {
            //1. Measure the maximum distance when the screws are connected by the plate
            var tooFarDistanceProblems = new List<Screw>();

            var thisScrew = screw;

            if (!connectedWithPlateScrews.Keys.Any(s => s == thisScrew.Id))
            {
                return tooFarDistanceProblems;
            }

            var connection = connectedWithPlateScrews.First(s => s.Key == thisScrew.Id).Value;

            foreach (var otherScrew in connection)
            {
                var distance = screwDistances[otherScrew];
                var acceptableMaxDistance = acceptableMaxDistances[otherScrew];
                if (distance > acceptableMaxDistance)
                {
                    tooFarDistanceProblems.Add(otherScrew);
                }
            }

            return tooFarDistanceProblems;
        }

        private void HandleInfo(ref Dictionary<Screw, List<Screw>> info, Screw thisScrew, Screw otherScrew)
        {
            if (info.ContainsKey(thisScrew))
            {
                if (!info[thisScrew].Contains(otherScrew))
                {
                    info[thisScrew].Add(otherScrew);
                }
            }
            else
            {
                info.Add(thisScrew, new List<Screw> { otherScrew });
            }
        }

        private bool IsPerformInsertTrajectoryCheckOk(Screw screw, Mesh mesh)
        {
            var screwHeadRef = screw.GetScrewHeadRef();
            Circle headCircle;
            if (!screwHeadRef.TryGetCircle(out headCircle, 1.0))
            {
                throw new Exception("HeadRef is not a circle");
            }

            var rayDirection = -screw.Direction;

            var cylinder = CylinderUtilities.CreateCylinder(headCircle.Diameter, screw.HeadPoint, rayDirection, Constants.QCValues.InsertionTrajectoryDistance);
            var cylinderBrep = ThreadSafeCommonOperations.BrepCreateFromCylinder(cylinder);

#if (INTERNAL)
            if (CMFImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddObject(cylinderBrep, "TrajectoryCylinder", "Test");
            }
#endif

            var cylinderMesh = ThreadSafeCommonOperations.MeshCreateFromBrep(cylinderBrep).FirstOrDefault();
            var intersection = ThreadSafeCommonOperations.IntersectionMeshMeshFast(cylinderMesh, mesh);
            return !intersection.Any();
        }

        private Dictionary<Screw, Dictionary<Screw, double>> GetAllScrewAcceptableMaxDistances(Dictionary<Screw, List<Screw>> screwsMap, EScrewBrand screwBrand)
        {
            var acceptableMaxDistances = new Dictionary<Screw, Dictionary<Screw, double>>();

            foreach (var screwMap in screwsMap)
            {
                var acceptableMaxDistance = GetScrewAcceptableMaxDistances(screwMap.Key, screwMap.Value, screwBrand);
                acceptableMaxDistances.Add(screwMap.Key, acceptableMaxDistance);
            }

            return acceptableMaxDistances;
        }

        private Dictionary<Screw, double> GetScrewAcceptableMaxDistances(Screw screw, List<Screw> otherScrews, EScrewBrand screwBrand)
        {
            var acceptableMaxDistances = new Dictionary<Screw, double>();

            foreach (var otherScrew in otherScrews)
            {
                var acceptableMaxDistance = GetAcceptableMaxDistanceBetweenTwoScrew(screw, otherScrew, screwBrand);
                acceptableMaxDistances.Add(otherScrew, acceptableMaxDistance);
            }

            return acceptableMaxDistances;
        }

        private double GetAcceptableMaxDistanceBetweenTwoScrew(Screw screwA, Screw screwB, EScrewBrand screwBrand)
        {
            var screwACasePref = _objectManager.GetCasePreference(screwA);
            var screwBCasePref = _objectManager.GetCasePreference(screwB);

            if (screwACasePref != screwBCasePref)
            {
                throw new IDSException("CasePreferenceDataModel for screw A and B is different");
            }

            var screwARotationCenter =
                ScrewUtilities.FindDotTheScrewBelongsTo(screwA, screwACasePref.ImplantDataModel.DotList);
            var screwBRotationCenter =
                ScrewUtilities.FindDotTheScrewBelongsTo(screwB, screwBCasePref.ImplantDataModel.DotList);

            var multiplePathConnections = ImplantCreationUtilitiesRhinoFree.FindConnectionsBelongToTwoDotPastille(
                screwACasePref.ImplantDataModel.ConnectionList, screwARotationCenter, screwBRotationCenter);

            var finalAcceptableMaxDistance = double.MaxValue;
            foreach (var connections in multiplePathConnections)
            {
                if (!ImplantCreationUtilitiesRhinoFree.CheckConnectionsPropertiesIsEqual(connections))
                {
                    throw new Exception($"Connection in between Screw {screwA.Index} and Screw {screwB.Index} have different properties");
                }

                var connection = connections[0];
                var acceptableMaxDistance = CasePreferencesHelper.GetAcceptableMaxScrewDistance(screwBrand, screwACasePref.CasePrefData.ImplantTypeValue, connection.Thickness, connection.Width);

                if (finalAcceptableMaxDistance > acceptableMaxDistance)
                {
                    finalAcceptableMaxDistance = acceptableMaxDistance;
                }
            }

            return finalAcceptableMaxDistance;
        }

        public bool CheckAllConnectionPropertiesAreEqual(List<Screw> screws)
        {
            foreach (var screw in screws)
            {
                var screwCasePreferenceData = _objectManager.GetCasePreference(screw);
                var screwsInCasePreferenceData = GetScrewsBelongTheCase(screwCasePreferenceData);
                var screwWithPlateScrewConnections = 
                    GetConnectedWithPlateScrews(
                            screwCasePreferenceData, 
                            screwsInCasePreferenceData,
                            false)
                        .ToDictionary(kv => kv.Key.Id, 
                            kv => kv.Value);

                var success = screwWithPlateScrewConnections.TryGetValue(screw.Id, 
                    out var plateScrewConnections);
                if (!success)
                {
                    continue;
                }

                foreach (var plateScrew in plateScrewConnections)
                {
                    var screwDotPastille =
                        ScrewUtilities.FindDotTheScrewBelongsTo(screw,
                            screwCasePreferenceData.ImplantDataModel.DotList);
                    var plateScrewDotPastille =
                        ScrewUtilities.FindDotTheScrewBelongsTo(plateScrew,
                            screwCasePreferenceData.ImplantDataModel.DotList);

                    var multiplePathConnections =
                        ImplantCreationUtilitiesRhinoFree.FindConnectionsBelongToTwoDotPastille(
                            screwCasePreferenceData.ImplantDataModel.ConnectionList,
                            screwDotPastille, plateScrewDotPastille);

                    if (!multiplePathConnections.All(
                            ImplantCreationUtilitiesRhinoFree.CheckConnectionsPropertiesIsEqual))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void Dispose()
        {
            implantLowLoDConstraintMeshes.ForEach(x => x.Dispose());
        }
    }
}
