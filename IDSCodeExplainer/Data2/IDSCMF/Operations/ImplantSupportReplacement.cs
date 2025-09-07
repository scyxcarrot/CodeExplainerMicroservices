using IDS.CMF.AttentionPointer;
using IDS.CMF.CasePreferences;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Interface.Implant;
using IDS.RhinoInterfaces.Converter;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IDS.CMF.Operations
{
    public class ImplantSupportReplacement
    {
        private readonly CMFImplantDirector _director;

        public ImplantSupportReplacement(CMFImplantDirector director)
        {
            _director = director;
        }

        public bool ReplaceImplantSupport(CasePreferenceDataModel casePref, Mesh implantSupport, bool invalidateRoI, out Guid supportGuid)
        {
            supportGuid = Guid.Empty;
            
            if (implantSupport.SolidOrientation() != 1)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "ImplantSupport is not solid!");
            }

            Mesh dummyLowLoDSupport = null;

            IDSPluginHelper.WriteLine(LogCategory.Default, "Level of Detail - Low will be generated on the background...");
            var threadStart = new ThreadStart(() =>
            {
                var tmpMesh = implantSupport.DuplicateMesh();

                var backgroundObjectManager = new CMFObjectManager(_director);
                dummyLowLoDSupport = backgroundObjectManager.GenerateLoDLow(tmpMesh, false);

                tmpMesh.Dispose();
            });

            var thread = new Thread(threadStart)
            {
                IsBackground = true
            };
            thread.Start();

            var attracted = AttractConnections(casePref, implantSupport);
            if (!attracted)
            {
                return false;
            }

            RegenerateAllScrews(casePref, implantSupport);

            RegenerateBuildingBlocks(casePref);

            var objectManager = new CMFObjectManager(_director);
            var implantSupportManager = new ImplantSupportManager(objectManager);
            supportGuid = implantSupportManager.AddImplantSupportRhObj(casePref, implantSupport, _director);
            var rhSupport = _director.Document.Objects.Find(supportGuid);

            thread.Join();

            if (dummyLowLoDSupport != null)
            {
                objectManager.SetBuildingBlockLoDLow(supportGuid, dummyLowLoDSupport);
                ImplantCreationUtilities.GetImplantRoIVolume(objectManager, casePref, ref rhSupport);
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Implant RoIs are not generated as LoDLow failed to generate!");
            }
            
            // It will be invalidate all the metal and teeth integration RoI when any single implant support been imported
            if (invalidateRoI)
            {
                if (objectManager.HasBuildingBlock(IBB.ImplantSupportTeethIntegrationRoI))
                {
                    var roiId = objectManager.GetBuildingBlockId(IBB.ImplantSupportTeethIntegrationRoI);
                    if (objectManager.DeleteObject(roiId))
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Implant Teeth RoI has been removed.");
                    }
                }

                if (objectManager.HasBuildingBlock(IBB.ImplantSupportRemovedMetalIntegrationRoI))
                {
                    var roiId = objectManager.GetBuildingBlockId(IBB.ImplantSupportRemovedMetalIntegrationRoI);
                    if (objectManager.DeleteObject(roiId))
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Implant Removed Metal RoI has been removed.");
                    }
                }

                if (objectManager.HasBuildingBlock(IBB.ImplantSupportRemainedMetalIntegrationRoI))
                {
                    var roiId = objectManager.GetBuildingBlockId(IBB.ImplantSupportRemainedMetalIntegrationRoI);
                    if (objectManager.DeleteObject(roiId))
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Implant Remained Metal RoI has been removed.");
                    }
                }

                _director.ImplantManager.ResetImplantSupportRoICreationInformation();
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Implant RoI Information has been reset.");
            }

            PastilleAttentionPointer.Instance.HideAndClearDeformedPastille(_director);

            //invalidate patch support if exist
            InvalidatePatchSupport(casePref, objectManager);

            return true;
        }

        private bool AttractConnections(CasePreferenceDataModel casePref, Mesh support)
        {
            var maximumDistanceAllowed = DotUtilities.MaximumDistanceAllowed;

            var list = new List<Tuple<IDot, IDot>>();

            var implantDataModel = casePref.ImplantDataModel;

            if (implantDataModel != null)
            {
                foreach (var dot in implantDataModel.DotList)
                {
                    var duplicateDot = DotUtilities.FindDotOnDifferentMesh(dot, support, maximumDistanceAllowed);
                    if (duplicateDot == null)
                    {
                        return false;
                    }
                    list.Add(new Tuple<IDot, IDot>(dot, duplicateDot));
                }
            }

            foreach (var tuple in list)
            {
                tuple.Item1.Location = tuple.Item2.Location;
                tuple.Item1.Direction = tuple.Item2.Direction;
            }

            return true;
        }

        private DotPastille GetPastille(CasePreferenceDataModel casePref, CMFImplantDirector director, Screw screw)
        {
            var implant = casePref.ImplantDataModel;
            foreach (var dot in implant.DotList)
            {
                var pastille = dot as DotPastille;
                if (pastille?.Screw != null && pastille.Screw.Id == screw.Id)
                {
                    return pastille;
                }
            }
            throw new Exception("Unable to find associated pastille of selected screw!");
        }

        private class ScrewRefData
        {
            public Vector3d ScrewDir { get; set; }
            public int Index { get; set; }
            public int GroupIndex { get; set; }
            public double ScrewLength { get; set; }
            public string BarrelType { get; set; }

            public ScrewRefData(Vector3d screwDir, int index, int groupIndex, double screwLength, string barrelType)
            {
                ScrewDir = screwDir;
                Index = index;
                GroupIndex = groupIndex;
                ScrewLength = screwLength;
                BarrelType = barrelType;
            }
        }

        private void RegenerateAllScrews(CasePreferenceDataModel casePref, Mesh support)
        {
            var objectManager = new CMFObjectManager(_director);
            var screwManager = new ScrewManager(_director);
            var implantScrews = screwManager.GetScrews(casePref, false);

            var screwRefs = new List<KeyValuePair<DotPastille, ScrewRefData>>();
            var regenerateScrewGroups = new List<ScrewManager.ScrewGroup>();
            _director.ScrewGroups.Groups.ForEach(x => regenerateScrewGroups.Add(new ScrewManager.ScrewGroup(x.ScrewGuids)));

            foreach (var screw in implantScrews)
            {
                var castedScrew = (Screw)screw;
                var pastille = GetPastille(casePref, _director, castedScrew);
                var screwGroupIndex = _director.ScrewGroups.GetScrewGroupIndex(castedScrew);
                screwRefs.Add(new KeyValuePair<DotPastille, ScrewRefData>(pastille, new ScrewRefData(castedScrew.Direction, castedScrew.Index, screwGroupIndex, castedScrew.Length, castedScrew.BarrelType)));
                
                if (screwGroupIndex != -1)
                {
                    regenerateScrewGroups[screwGroupIndex].ScrewGuids.Remove(screw.Id);
                }
            }

            var implantComponent = new ImplantCaseComponent();
            var screwCreator = new ScrewCreator(_director);
            var implant = casePref.ImplantDataModel;
            if (implant != null)
            {
                var implant_point_list = implant.DotList;

                var availableLengths = Queries.GetAvailableScrewLengths(casePref.CasePrefData.ScrewTypeValue, casePref.CasePrefData.ScrewStyle);

                foreach (var connection_pt in implant_point_list)
                {
                    var pastille = connection_pt as DotPastille;
                    if (pastille != null)
                    {
                        var screwAideDict = casePref.ScrewAideData.GenerateScrewAideDictionary();
                        var screw = screwCreator.CreateScrewObjectOnPastille(
                            RhinoPoint3dConverter.ToPoint3d(pastille.Location),
                            -RhinoVector3dConverter.ToVector3d(pastille.Direction), screwAideDict,
                            casePref.CasePrefData.ScrewLengthMm, casePref.CasePrefData.ScrewTypeValue,
                            casePref.CasePrefData.BarrelTypeValue);

                        var screwCalibrator = new ScrewCalibrator(support);

                        //failure in screw calibration will not block implant support import operation
                        //  => original positioned screw will be used

                        var oldScrewId = Guid.Empty;
                        var screwGroupIndex = -1;
                        if (screwRefs.Any(x => x.Key == pastille))
                        {
                            var oldPastille = screwRefs.FirstOrDefault(x => x.Key == pastille);
                            var length = oldPastille.Value.ScrewLength;
                            length = Queries.GetNearestAvailableScrewLength(availableLengths, length);
                            screw = screwCreator.CreateScrewObjectOnPastille(screw.HeadPoint,
                                oldPastille.Value.ScrewDir,
                                screwAideDict, length, casePref.CasePrefData.ScrewTypeValue,
                                casePref.CasePrefData.BarrelTypeValue);
                            screw.Index = oldPastille.Value.Index;
                            screwGroupIndex = oldPastille.Value.GroupIndex;

                            if (screwCalibrator.LevelHeadOnTopOfMesh(screw, casePref.CasePrefData.PlateThicknessMm, true))
                            {
                                screw = screwCalibrator.CalibratedScrew;
                            }

                            oldScrewId = oldPastille.Key.Screw.Id;
                        }
                        else //if first time import support, there were no screw on pastille yet
                        {
                            if (screwCalibrator.LevelHeadOnTopOfMesh(screw, casePref.CasePrefData.PlateThicknessMm, true))
                            {
                                screw = screwCalibrator.CalibratedScrew;
                            }
                        }

                        var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePref);

                        var parentGuid = pastille.Id;
                        var screwId = 
                            IdsDocumentUtilities.AddNewRhinoObjectBuildingBlock(
                                objectManager, 
                                _director.IdsDocument, 
                                buildingBlock, 
                                parentGuid, 
                                screw);

                        if (oldScrewId != Guid.Empty)
                        {
                            RegisteredBarrelUtilities.ReplaceLinkedImplantScrew(_director, oldScrewId, screwId);

                            // remove the old screws for targeted invalidation
                            _director.IdsDocument.Delete(oldScrewId);
                        }

                        if (screwGroupIndex != -1)
                        {
                            regenerateScrewGroups[screwGroupIndex].ScrewGuids.Add(screwId);
                        }

                        ScrewPastilleManager.UpdateScrewDataInPastille(pastille, screw);
                        pastille.CreationAlgoMethod = DotPastille.CreationAlgoMethods[0];
                    }
                }
            }

            _director.ScrewGroups.Groups = regenerateScrewGroups;
        }

        private void RegenerateBuildingBlocks(CasePreferenceDataModel casePref)
        {
            //regenerate connections, landmarks, implant previews
            //failure in implant preview generation will not block implant support import operation
            //  => user will need to regenerate the implant preview manually
            _director.ImplantManager.InvalidateConnectionBuildingBlock(casePref);
            _director.ImplantManager.InvalidateLandmarkBuildingBlock(casePref);

            casePref.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.PlanningImplant });
        }

        private void InvalidatePatchSupport(CasePreferenceDataModel casePref, CMFObjectManager objectManager)
        {
            var implantComponent = new ImplantCaseComponent();
            var patchSupportBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.PatchSupport, casePref);

            if (objectManager.HasBuildingBlock(patchSupportBuildingBlock))
            {
                var patchSupportIds = objectManager.GetAllImplantExtendedImplantBuildingBlocksIDs(IBB.PatchSupport, casePref);
                foreach (var patchSupportId in patchSupportIds)
                {
                    _director.IdsDocument.Delete(patchSupportId);

                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Patch Support: {patchSupportId} has been removed.");
                }
                
            }
        }
    }
}
