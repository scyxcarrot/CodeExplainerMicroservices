using IDS.CMF.Factory;
using IDS.CMF.Graph;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.V2.DataModels;
using Rhino.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.CasePreferences
{
    public class CasePreferenceManager
    {
        private const string KeySurgeryInformation = "SurgeryInformation";
        private const string KeyCasePreference = "CasePreference";
        private const string KeyGuidePreference = "GuidePreference";

        public delegate void OnCaseDeactivateDelegate(CasePreferenceDataModel data);
        public delegate void OnCaseActivateDelegate(CasePreferenceDataModel data);
        public delegate void OnDeleteGuidePreferenceDelegate(GuidePreferenceDataModel data);

        public OnCaseActivateDelegate OnCaseActivateEventHandler { get; set; }
        public OnCaseActivateDelegate OnCaseDeactivateEventHandler { get; set; }
        public OnCaseActivateDelegate OnCasePreferenceDeletedEventHandler { get; set; }
        public OnDeleteGuidePreferenceDelegate OnGuidePreferenceDeletedEventHandler { get; set; }

        private List<CasePreferenceDataModel> _casePreferences = new List<CasePreferenceDataModel>();

        public List<CasePreferenceDataModel> CasePreferences
        {
            get { return _casePreferences; }
            set
            {
                _casePreferences = value;
                _casePreferences.ForEach(x =>
                {
                    if (x.Graph == null)
                    {
                        x.Graph = new CMFGraph(Director, x);
                        x.Graph.SubscribeForGraphInvalidation();
                    }
                });
            }
        }

        private List<GuidePreferenceDataModel> _guidePreferences = new List<GuidePreferenceDataModel>();

        public List<GuidePreferenceDataModel> GuidePreferences
        {
            get { return _guidePreferences; }
            set
            {
                _guidePreferences = value;
                _guidePreferences.ForEach(x =>
                {
                    if (x.Graph == null)
                    {
                        x.Graph = new CMFGraph(Director, x);
                        x.Graph.SubscribeForGraphInvalidation();
                    }
                });
            }
        }

        public SurgeryInformationData SurgeryInformation { get; set; }

        public CMFImplantDirector Director { get; private set; }

        public CasePreferenceManager(CMFImplantDirector director)
        {
            CasePreferences = new List<CasePreferenceDataModel>();
            GuidePreferences = new List<GuidePreferenceDataModel>();
            SurgeryInformation = new SurgeryInformationData();
            Director = director;
        }

        public void NotifyBuildingBlockHasChangedToAll(IBB[] ibbs, params IBB[] ibbsToSkip)
        {
            _casePreferences.ForEach(x =>
            {
                x.Graph.NotifyBuildingBlockHasChanged(ibbs, ibbsToSkip);
            });

            _guidePreferences.ForEach(x =>
            {
                x.Graph.NotifyBuildingBlockHasChanged(ibbs, ibbsToSkip);
            });
        }

        public void InitializeGraphs()
        {
            _casePreferences.ForEach(x =>
            {
                x.InvalidateGraph(Director);
            });

            _guidePreferences.ForEach(x =>
            {
                x.InvalidateGraph(Director);
            });
        }

        public void InitializeEvents()
        {
            _casePreferences.ForEach(x =>
            {
                x.InvalidateEvents(Director);
            });
        }

        public void AddCasePreference(CasePreferenceDataModel data)
        {
            data.InvalidateGraph(Director);
            data.InvalidateEvents(Director);
            CasePreferences.Add(data);
            var guidValueData = new GuidValueData(data.CaseGuid,
                new List<Guid>() { IdsDocumentUtilities.RootGuid }, data.CaseGuid);
            Director.IdsDocument.Create(guidValueData);
        }

        public void AddGuidePreference(GuidePreferenceDataModel data)
        {
            data.InvalidateGraph(Director);
            GuidePreferences.Add(data);
        }

        public bool DeleteCasePreference(Guid caseGuid)
        {
            if (!CasePreferences.Exists(x => x.CaseGuid == caseGuid))
            {
                return false;
            }

            var casePref = GetCase(caseGuid);
            return DeleteCasePreference(casePref);
        }

        public bool DeleteCasePreference(CasePreferenceDataModel data)
        {
            if (!CasePreferences.Exists(x => x.CaseGuid == data.CaseGuid))
            {
                return false;
            }

            data.Dispose();
            var success = CasePreferences.Remove(data);
            OnCasePreferenceDeletedEventHandler?.Invoke(data);
            data = null;
            return success;
        }

        public bool DeleteGuidePreference(Guid caseGuid)
        {
            if (!GuidePreferences.Exists(x => x.CaseGuid == caseGuid))
            {
                return false;
            }

            var casePref = GetGuideCase(caseGuid);
            return DeleteGuidePreference(casePref);
        }

        public bool DeleteGuidePreference(GuidePreferenceDataModel data)
        {
            if (!GuidePreferences.Exists(x => x.CaseGuid == data.CaseGuid))
            {
                return false;
            }

            var success = GuidePreferences.Remove(data);
            OnGuidePreferenceDeletedEventHandler?.Invoke(data);
            data = null;
            return success;
        }

        public CasePreferenceDataModel GetActivatedCase()
        {
            return CasePreferences.Find(x => x.IsActive);
        }

        public CasePreferenceDataModel GetCase(Guid guid)
        {
            if (!IsCaseExist(guid))
            {
                return null;
            }

            return CasePreferences.Find(x => x.CaseGuid == guid);
        }

        public CasePreferenceDataModel GetCaseWithCaseIndex(int caseIndex)
        {
            return CasePreferences.FirstOrDefault(x => x.NCase == caseIndex);
        }

        public GuidePreferenceDataModel GetGuideCase(Guid guid)
        {
            if (!IsGuideCaseExist(guid))
            {
                return null;
            }

            return GuidePreferences.Find(x => x.CaseGuid == guid);
        }

        public bool ActivateCase(CasePreferenceDataModel data)
        {
            return ActivateCase(data, null, null);
        }

        public bool ActivateCase(CasePreferenceDataModel data, OnCaseActivateDelegate onCaseActivateDelegate)
        {
            return ActivateCase(data, onCaseActivateDelegate, null);
        }

        public bool ActivateCase(CasePreferenceDataModel data, OnCaseDeactivateDelegate onCaseDeactivateDelegate)
        {
            return ActivateCase(data, null, onCaseDeactivateDelegate);
        }

        public bool ActivateCase(CasePreferenceDataModel data, OnCaseActivateDelegate onCaseActivateDelegate, OnCaseDeactivateDelegate onCaseDeactivateDelegate)
        {
            if (!IsCaseExist(data))
            {
                return false;
            }

            if (data.IsActive)
            {
                return true;
            }

            CasePreferences.ForEach(x => DeActivateCase(x, onCaseDeactivateDelegate));
            data.IsActive = true;
            OnCaseActivateEventHandler?.Invoke(data);
            onCaseActivateDelegate?.Invoke(data);
            return true;
        }

        public void DeActivateCase(CasePreferenceDataModel data, OnCaseDeactivateDelegate onCaseDeactivateDelegate)
        {
            if (!data.IsActive)
            {
                return;
            }

            data.IsActive = false;
            OnCaseDeactivateEventHandler?.Invoke(data);
            onCaseDeactivateDelegate?.Invoke(data);
        }

        public bool IsCaseExist(CasePreferenceDataModel data)
        {
            return CasePreferences.Any(x => x.CaseGuid == data.CaseGuid);
        }

        public bool IsCaseExist(Guid guid)
        {
            return CasePreferences.Any(x => x.CaseGuid == guid);
        }

        public bool IsGuideCaseExist(Guid guid)
        {
            return GuidePreferences.Any(x => x.CaseGuid == guid);
        }

        public bool SaveSurgeryInformationTo3Dm(ArchivableDictionary dict)
        {
            var siArc = SerializationFactory.CreateSerializedArchive(SurgeryInformation);
            return dict.Set(KeySurgeryInformation, siArc);
        }

        public bool LoadSurgeryInformationFrom3Dm(ArchivableDictionary dict)
        {
            var loadedData = new SurgeryInformationData();

            if (!loadedData.DeSerialize((ArchivableDictionary)dict[KeySurgeryInformation]))
            {
                return false;
            }

            SurgeryInformation = loadedData;

            return true;
        }

        public bool SaveCasePreferencesTo3Dm(ArchivableDictionary dict)
        {
            var count = 0;
            foreach (var cp in CasePreferences)
            {
                if (cp.CasePrefData.ImplantTypeValue == null)
                {
                    continue;
                }

                var csArc = SerializationFactory.CreateSerializedArchive(cp);
                if (csArc == null)
                {
                    return false;
                }

                dict.Set(KeyCasePreference + $"_{count}", csArc);
                count++;
            }
            return true;
        }

        public bool SaveGuidePreferencesTo3Dm(ArchivableDictionary dict)
        {
            var count = 0;
            foreach (var cp in GuidePreferences)
            {
                if (cp.GuidePrefData.GuideTypeValue == null)
                {
                    continue;
                }

                var csArc = SerializationFactory.CreateSerializedArchive(cp);
                if (csArc == null)
                {
                    return false;
                }

                dict.Set(KeyGuidePreference + $"_{count}", csArc);
                count++;
            }
            return true;
        }

        public bool LoadCasePreferencesFrom3Dm(ArchivableDictionary dict, SurgeryInformationData surgeryInformation,
            ScrewBrandCasePreferencesInfo screwBrandCasePref, ScrewLengthsData screwLengthsData)
        {
            foreach (var d in dict)
            {
                if (Regex.IsMatch(d.Key, KeyCasePreference + "_\\d+"))
                {
                    var model = SerializationFactory.DeSerializeCasePreferenceAsModel((ArchivableDictionary)dict[d.Key], surgeryInformation, screwBrandCasePref, screwLengthsData);
                    CasePreferences.Add(model);
                }
            }

            return true;
        }

        public bool LoadGuidePreferencesFrom3Dm(ArchivableDictionary dict, ScrewBrandCasePreferencesInfo screwBrandCasePref)
        {
            foreach (var d in dict)
            {
                if (Regex.IsMatch(d.Key, KeyGuidePreference + "_\\d+"))
                {
                    var model = SerializationFactory.DeSerializeGuidePreferenceAsModel((ArchivableDictionary)dict[d.Key], screwBrandCasePref);
                    GuidePreferences.Add(model);
                }
            }

            return true;
        }

        public bool HasUnsetCasePreference()
        {
            return CasePreferences.Exists(x => string.IsNullOrEmpty(x.CasePrefData.ImplantTypeValue));
        }

        public bool HasUnsetGuidePreference()
        {
            return GuidePreferences.Exists(x => string.IsNullOrEmpty(x.GuidePrefData.GuideTypeValue));
        }

        public bool HasInvalidCasePreferencesValues()
        {
            return CasePreferences.Any(x => !x.IsValid());
        }

        public void HandleRenumberCaseNumber(ICaseData model, int newNumber)
        {
            model.SetCaseNumber(newNumber);

            var implantCaseComponent = new ImplantCaseComponent();
            var implantComponents = implantCaseComponent.GetImplantComponents();

            var blocks = implantComponents.Select(component => implantCaseComponent.GetImplantBuildingBlock(component, model).Block).ToList();

            var guideCaseComponent = new GuideCaseComponent();
            var guideComponents = guideCaseComponent.GetGuideComponents();

            blocks.AddRange(guideComponents.Select(component => guideCaseComponent.GetGuideBuildingBlock(component, model).Block));

            UpdateRhinoObjectCaseNumber(blocks);
        }

        private void UpdateRhinoObjectCaseNumber(List<ImplantBuildingBlock> blocks) 
        {
            var objectManager = new CMFObjectManager(Director);

            foreach (var block in blocks)
            {
                var rhinoObjects = objectManager.GetAllBuildingBlocks(block.Name).ToList();

                foreach (var rhinoObject in rhinoObjects)
                {
                    objectManager.ChangeLayer(block, rhinoObject);
                }

                objectManager.UpdateMaterial(block, Director.Document);
            }
        }

        public bool IsContainCasePreference(CasePreferenceDataModel data)
        {
            var objectManager = new CMFObjectManager(Director);
            var buildingBlocks = new List<ExtendedImplantBuildingBlock>();

            var implantCaseComponent = new ImplantCaseComponent();
            buildingBlocks.Add(implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, data));
            buildingBlocks.Add(implantCaseComponent.GetImplantBuildingBlock(IBB.PlanningImplant, data));
            buildingBlocks.Add(implantCaseComponent.GetImplantBuildingBlock(IBB.Connection, data));
            buildingBlocks.Add(implantCaseComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, data));

            var guideCaseComponent = new GuideCaseComponent();
            buildingBlocks.Add(guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, data));
            buildingBlocks.Add(guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFlange, data));
            buildingBlocks.Add(guideCaseComponent.GetGuideBuildingBlock(IBB.GuideBridge, data));


            return buildingBlocks.Any(buildingBlock => objectManager.HasBuildingBlock(buildingBlock));
        }

        public void HandleDeleteCasePreference(CasePreferenceDataModel data, bool deleteImplantSupport = false)
        {
            //delete related entities
            //Planning
            //Screws
            //Connections
            //Implant Preview
            //Registered Barrels
            //Landmarks
            //Guide Outline
            //Guide Secondary Outline
            //Guide Flange
            //Guide Bridge
            //Guide Fixation Screw
            //Guide Fixation Screw Eye
            //Guide Label Tag
            //Guide Preview

            var objectManager = new CMFObjectManager(Director);

            var implantCaseComponent = new ImplantCaseComponent();
            var buildingBlock = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, data);
            var hasScrew = objectManager.HasBuildingBlock(buildingBlock);
            var rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
            rhinoObjects.ForEach(x =>
            {
                objectManager.DeleteScrew(x.Id);
            });

            //eventhough the deletion of IBB.PlanningImplant and IBB.Connection is triggered by
            //IBB.Screw, there are scenarios where IBB.Screw has yet exist in the document
            //Hence, we manually trigger the deletion here
            buildingBlock = implantCaseComponent.GetImplantBuildingBlock(IBB.PlanningImplant, data);
            rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
            rhinoObjects.ForEach(x => objectManager.DeleteObject(x.Id));

            Director.ImplantManager.DeleteAllConnectionsBuildingBlock(data);

            // When placed implant template with existing planning design,
            // the whole implant case will be remove and add new implant case(IDot, IConnection, ...),
            // but since implant support is under implant case now, it not worth to remove the implant support when place implant template
            if (deleteImplantSupport)
            {
                // delete implant support and patch support
                buildingBlock = implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, data);
                rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
                rhinoObjects.ForEach(x => objectManager.DeleteObject(x.Id));

                buildingBlock = implantCaseComponent.GetImplantBuildingBlock(
                    IBB.PatchSupport, data);
                rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
                rhinoObjects.ForEach(x => objectManager.DeleteObject(x.Id));
            }

            var guideCaseComponent = new GuideCaseComponent();
            buildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, data);
            rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
            rhinoObjects.ForEach(x => objectManager.DeleteObject(x.Id));

            buildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFlange, data);
            rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
            rhinoObjects.ForEach(x => objectManager.DeleteObject(x.Id));

            buildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideBridge, data);
            rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
            rhinoObjects.ForEach(x => objectManager.DeleteObject(x.Id));

            var ibbs = new List<IBB> { IBB.GuideFixationScrew };
            if (hasScrew)
            {
                ibbs.Add(IBB.Screw);
            }
            data.Graph.NotifyBuildingBlockHasChanged(ibbs.ToArray());
            data.Graph.InvalidateGraph();
        }

        public void HandleDeleteGuidePreference(GuidePreferenceDataModel data)
        {
            var objectManager = new CMFObjectManager(Director);

            var guideCaseComponent = new GuideCaseComponent();

            var guideScrewEibb = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, data);
            var guideScrews = objectManager.GetAllBuildingBlocks(guideScrewEibb).ToList();

            guideScrews.ForEach(x =>
            {
                var screw = (Screw)x;
                var screwsItSharedWith = screw.GetScrewItSharedWith();
                screwsItSharedWith.ForEach(y =>
                {
                    y.UnshareFromScrew(screw);
                });
            });

            var ibbs = new List<IBB> { IBB.GuideFixationScrew, IBB.GuideFlange, IBB.GuideBridge, IBB.PositiveGuideDrawings, IBB.NegativeGuideDrawing, IBB.GuideLinkSurface, IBB.GuideSolidSurface, IBB.GuideSurface, IBB.SmoothGuideBaseSurface, IBB.TeethBlock, IBB.TeethBaseRegion, IBB.TeethBaseExtrusion };

            foreach (var b in ibbs)
            {
                var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(b, data);
                var rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();
                rhinoObjects.ForEach(x =>
                {
                    var deleteSuccess = Director.IdsDocument.Delete(x.Id);
                    if (!deleteSuccess)
                    {
                        objectManager.DeleteObject(x.Id);
                    }
                });
            }

            data.Graph.NotifyBuildingBlockHasChanged(ibbs.ToArray());
            data.Graph.InvalidateGraph();
        }
    }
}
