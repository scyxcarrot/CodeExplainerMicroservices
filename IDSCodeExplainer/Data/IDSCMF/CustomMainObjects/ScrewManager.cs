using IDS.CMF.CasePreferences;
using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.CMF.V2.Utilities;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.RhinoInterfaces.Converter;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class ScrewManager
    {
        private readonly CMFImplantDirector _director;

        public ScrewManager(CMFImplantDirector director)
        {
            _director = director;
        }

        public CMFImplantDirector GetDirector()
        {
            return _director;
        }

        public List<Screw> GetScrews(IEnumerable<Screw> screws, ICaseData caseData, bool isGetGuideFixationScrews)
        {
            var objectManager = new CMFObjectManager(_director);

            return screws.Where(screw =>
            {
                var caseGuid = isGetGuideFixationScrews ? objectManager.GetGuidePreference(screw).CaseGuid :
                    objectManager.GetCasePreference(screw).CaseGuid;
                return caseGuid == caseData.CaseGuid;
            }).ToList();
        }

        //TODO set isGetGuideFixationScrews to false? 
        public List<Screw> GetScrews(ICaseData caseData, bool isGetGuideFixationScrews)
        {
            return GetScrews(GetAllScrews(isGetGuideFixationScrews), caseData, isGetGuideFixationScrews);
        }

        public List<Screw> GetScrews(GuidePreferenceDataModel casePref)
        {
            return GetScrews(casePref, true);
        }

        public List<Screw> GetAllScrews(bool isGetGuideFixationScrews)
        {
            var ibbScrew = isGetGuideFixationScrews ? IBB.GuideFixationScrew : IBB.Screw;

            var screws = new List<Screw>();

            var objManager = new CMFObjectManager(_director);
            var screwIds = objManager.GetAllBuildingBlockIds(ibbScrew).ToList();

            screwIds.ForEach(x =>
            {
                var scr = (Screw)_director.Document.Objects.Find(x);
                screws.Add(scr);
            });

            return screws;
        }

        public bool CalibrateAllImplantScrew(Mesh calibrationMesh, CasePreferenceDataModel casePref)
        {
            var success = true;
            var allScrews = GetScrews(casePref, false);
            allScrews.ForEach(x => success &= CalibrateImplantScrew(x, calibrationMesh));
            return success;
        }

        public bool CalibrateImplantScrew(Screw screw, Mesh calibrationMesh)
        {
            var objectManager = new CMFObjectManager(_director);

            var screwCalibrator = new ScrewCalibrator(calibrationMesh);
            var casePreferenceData = objectManager.GetCasePreference(screw);
            
            var pastille = casePreferenceData.ImplantDataModel.DotList.FirstOrDefault(dot => (dot as DotPastille)?.Screw != null && screw.Id == (dot as DotPastille).Screw.Id);
            var newLocation = RhinoPoint3dConverter.ToPoint3d(pastille.Location);            
            var newTipPoint = newLocation + screw.Direction * screw.Length;
            var referenceScrew = new Screw(screw.Director, newLocation, newTipPoint, screw.ScrewAideDictionary, screw.Index, screw.ScrewType, screw.BarrelType);                      

            if (!screwCalibrator.LevelHeadOnTopOfMesh(referenceScrew, casePreferenceData.CasePrefData.PlateThicknessMm, true))
            {
                return false;
            }

            ScrewPastilleManager.UpdateScrewDataInPastille((DotPastille)pastille, screwCalibrator.CalibratedScrew, false);

            var registeredBarrelId = screw.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel) ? screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] : Guid.Empty;
            var maintainAtOriginalPosition = screwCalibrator.CalibratedScrew.HeadPoint.EpsilonEquals(screw.HeadPoint, 0.001);
            if (maintainAtOriginalPosition && registeredBarrelId != Guid.Empty)
            {
                screw.ScrewGuideAidesInDocument.Remove(IBB.RegisteredBarrel);
            }

            // screw will set to null after running ReplaceExistingScrewInDocument, so keep the screw.Id to avoid null exception
            var oldScrewId = screw.Id;
            ReplaceExistingScrewInDocument(screwCalibrator.CalibratedScrew, ref screw, casePreferenceData, true);

            if (registeredBarrelId != Guid.Empty)
            {
                if (maintainAtOriginalPosition)
                {
                    screwCalibrator.CalibratedScrew.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelId;
                }
                else
                {
                    RegisteredBarrelUtilities.NotifyBuildingBlockHasChanged(_director, oldScrewId);
                }
            }

            return true;
        }

        //It detects the type of screw
        //NewScrew and ScrewInDocument must be of a same type!
        public void ReplaceExistingScrewInDocument(Screw newScrew, ref Screw screwInDocument, ICaseData casePreferenceData, bool isImplantScrew)
        {
            var objManager = new CMFObjectManager(_director);

            var screwInDocumentFound = _director.Document.Objects.Find(screwInDocument.Id);
            if (!(screwInDocumentFound is Screw))
            {
                throw new IDSException("Screw is not found in the document!");
            }

            ExtendedImplantBuildingBlock buildingBlock;

            if (isImplantScrew)
            {
                var implantComponent = new ImplantCaseComponent();
                buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
            }
            else
            {
                var guideComponent = new GuideCaseComponent();
                buildingBlock = guideComponent.GetGuideBuildingBlock(IBB.GuideFixationScrew, casePreferenceData);
            }

            newScrew.ScrewGuideAidesInDocument = screwInDocument.ScrewGuideAidesInDocument;
            newScrew.ScrewImplantAidesInDocument = screwInDocument.ScrewImplantAidesInDocument;
            var attrDict = screwInDocument.Attributes.UserDictionary;
            newScrew.Attributes.UserDictionary.ReplaceContentsWith(attrDict);
            screwInDocument.CommitChanges();
            objManager.SetBuildingBlock(buildingBlock, newScrew, screwInDocument.Id);
            //Implant screw can have both
            newScrew.InvalidateImplantScrewAidesReferencesInDocument();
            newScrew.InvalidateGuideScrewAidesReferencesInDocument();

            var screwsItSharedWith = screwInDocument.GetScrewItSharedWith();
            newScrew.ShareWithScrews(screwsItSharedWith);

            screwInDocument = null;
        }

        public void ReplaceExistingImplantScrewWithoutAnyInvalidation(Screw newScrew, ref Screw screwInDocument, CasePreferenceDataModel casePreferenceData)
        {
            var pastille = ScrewUtilities.FindDotTheScrewBelongsTo(screwInDocument, casePreferenceData.ImplantDataModel.DotList);

            Guid registeredBarrelId;
            RhinoObject registeredBarrelObject;

            var foundRegisteredBarrel = RegisteredBarrelUtilities.GetRegisteredBarrelIdAndObject(_director, screwInDocument, out registeredBarrelId,
                out registeredBarrelObject);

            ReplaceExistingScrewInDocument(newScrew, ref screwInDocument, casePreferenceData, true);

            if (foundRegisteredBarrel)
            {
                newScrew.ScrewGuideAidesInDocument[IBB.RegisteredBarrel] = registeredBarrelId;
                _director.Document.Objects.Undelete(registeredBarrelObject);
            }

            ScrewPastilleManager.UpdateScrewDataInPastille(pastille, newScrew);
        }

        public ImplantPreferenceModel GetImplantPreferenceTheScrewBelongsTo(Screw screw)
        {
            var pastille = ImplantCreationUtilities.GetDotPastille(screw);
            if (pastille == null)
            {
                return null;
            }

            var rotationPoint = RhinoPoint3dConverter.ToPoint3d(pastille.Location);
            return ImplantCreationUtilities.GetNearestImplantCasePreferenceModel(_director, rotationPoint, 1.0);
        }
        
        public GuidePreferenceDataModel GetGuidePreferenceTheScrewBelongsTo(Screw screw)
        {
            var objectManager = new CMFObjectManager(_director);
            var guidePreferenceData = objectManager.GetGuidePreference(screw);
            return guidePreferenceData;
        }

        public string GetScrewNumberWithImplantNumber(Screw screw)
        {
            var casePref = GetImplantPreferenceTheScrewBelongsTo(screw);
            return GetScrewNumberWithImplantNumber(screw.Index, casePref.NCase);
        }

        [Obsolete("Obsolete, please use ScrewUtilitiesV2.GetScrewNumberWithImplantNumber")]
        public static string GetScrewNumberWithImplantNumber(int screwIndex, int numCase)
        {
            return ScrewUtilitiesV2.GetScrewNumberWithImplantNumber(screwIndex, numCase);
        }

        public string GetScrewNumberWithGuideNumber(Screw screw)
        {
            var guidePrefData = GetGuidePreferenceTheScrewBelongsTo(screw);
            return GetScrewNumberWithGuideNumber(screw.Index, guidePrefData.NCase);
        }

        public static string GetScrewNumberWithGuideNumber(int screwIndex, int numCase)
        {
            return $"{screwIndex}.G{numCase}";
        }

        public IEnumerable<Screw> SortScrews(IEnumerable<Screw> screws, bool isGuideFixation)
        {
            var screwNCaseNumber = screws.Select(s =>
            {
                var caseData = isGuideFixation? (ICaseData)GetGuidePreferenceTheScrewBelongsTo(s) 
                    : (ICaseData)GetImplantPreferenceTheScrewBelongsTo(s);
                //if case data is null, a big value will be assigned
                return new Tuple<int, int, Screw>(caseData?.NCase ?? 1000, s.Index, s);
            });
            return screwNCaseNumber.OrderBy(t => t.Item1 * 1000 + t.Item2).Select(t => t.Item3);
        }

        public void UpdateGuideFixationScrewInDocument(Screw newScrew, ref Screw screwInDocument)
        {
            var objectManager = new CMFObjectManager(_director);
            var guidePreferenceData = objectManager.GetGuidePreference(screwInDocument);

            ReplaceExistingScrewInDocument(newScrew, ref screwInDocument, guidePreferenceData, false);
            newScrew.UpdateAidesInDocument();

            guidePreferenceData.Graph.NotifyBuildingBlockHasChanged(new[] { IBB.GuideFixationScrew }, IBB.GuideFixationScrewLabelTag);
        }
        
        public IDictionary<Screw, Tuple<IBB, Brep>> GetAllGuideScrewsEyeOrLabelTag(IEnumerable<Screw> allGuideScrews)
        {
            var guideScrewsAides = new Dictionary<Screw, Tuple<IBB, Brep>>();
            
            foreach (var guideScrew in allGuideScrews)
            {
                if (guideScrew.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewEye))
                {
                    var screwEyeGuid = guideScrew.ScrewGuideAidesInDocument[IBB.GuideFixationScrewEye];
                    guideScrewsAides.Add(guideScrew, new Tuple<IBB, Brep>(
                        IBB.GuideFixationScrewEye, (Brep)_director.Document.Objects.Find(screwEyeGuid).Geometry));
                }
                else if (guideScrew.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewLabelTag))
                {
                    var screwEyeGuid = guideScrew.ScrewGuideAidesInDocument[IBB.GuideFixationScrewLabelTag];
                    guideScrewsAides.Add(guideScrew, new Tuple<IBB, Brep>(
                        IBB.GuideFixationScrewLabelTag, (Brep)_director.Document.Objects.Find(screwEyeGuid).Geometry));
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Abnormal guide screw, {guideScrew.Id} found, the screw haven't contain any screw eye or screw label tag");
                }
            }

            return guideScrewsAides;
        }

        public Tuple<IBB, Brep> GetGuideScrewEyeOrLabelTag(Screw guideScrew)
        {
            if (guideScrew.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewEye))
            {
                var screwEyeGuid = guideScrew.ScrewGuideAidesInDocument[IBB.GuideFixationScrewEye];
                return new Tuple<IBB, Brep>(IBB.GuideFixationScrewEye,
                    (Brep) _director.Document.Objects.Find(screwEyeGuid).Geometry);
            }
            
            if (guideScrew.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewLabelTag))
            {
                var screwEyeGuid = guideScrew.ScrewGuideAidesInDocument[IBB.GuideFixationScrewLabelTag];
                return new Tuple<IBB, Brep>(IBB.GuideFixationScrewLabelTag,
                    (Brep) _director.Document.Objects.Find(screwEyeGuid).Geometry);
            }

            IDSPluginHelper.WriteLine(LogCategory.Error,
                $"Abnormal guide screw, {guideScrew.Id} found, the screw haven't contain any screw eye or screw label tag");

            return null;
        }

        public Brep GetGuideScrewEyeOrLabelTagGeometry(Screw guideScrew)
        {
            return GetGuideScrewEyeOrLabelTag(guideScrew)?.Item2;
        }

        public bool IsAllImplantScrewsCalibrated()
        {
            var allScrews = GetAllScrews(false);
            var isScrewsCalibrated = allScrews.Select(screw => screw.IsCalibrated);
            return isScrewsCalibrated.All(isScrewCalibrated => isScrewCalibrated);
        }

        public List<Screw> GetCalibratedImplantScrews()
        {
            var allScrews = GetAllScrews(false);
            var calibratedScrews = new List<Screw>();
            foreach (var screw in allScrews)
            {
                if (screw.IsCalibrated)
                {
                    calibratedScrews.Add(screw);
                }
            }

            return calibratedScrews;
        }

        public class ScrewGroup : ISerializable<ArchivableDictionary>
        {
            public List<Guid> ScrewGuids { get; set; }

            public static string SerializationLabelConst => "ScrewGroup";
            public string SerializationLabel => SerializationLabelConst;
            private readonly string KeyScrewGuidsInGroup = "ScrewGuidsInGroup";

            public ScrewGroup()
            {
                ScrewGuids = new List<Guid>();
            }

            public ScrewGroup(List<Guid> screwGuids)
            {
                ScrewGuids = screwGuids.ToList();
            }

            public bool DeSerialize(ArchivableDictionary serializer)
            {
                ScrewGuids = ((IEnumerable<Guid>)serializer[KeyScrewGuidsInGroup]).ToList();

                return true;
            }

            public bool Serialize(ArchivableDictionary serializer)
            {
                serializer.Set(KeyScrewGuidsInGroup, ScrewGuids);

                return true;
            }
        }

        public class ScrewGroupManager : ISerializable<ArchivableDictionary>
        {
            public List<ScrewGroup> Groups { get; set; }

            public static string SerializationLabelConst => "ScrewGroupManager";

            public string SerializationLabel => SerializationLabelConst;

            public ScrewGroupManager()
            {
                Groups = new List<ScrewGroup>();
            }

            public int GetScrewGroupIndex(Screw screw)
            {
                for (var i = 0; i < Groups.Count; i++)
                {
                    var currGroup = Groups[i];

                    if (currGroup.ScrewGuids.Contains(screw.Id))
                    {
                        return i;
                    }
                }

                return -1;
            }

            public void RemoveScrew(Guid screwId)
            {
                Groups.ForEach(g =>
                {
                    if (g.ScrewGuids.Contains(screwId))
                    {
                        g.ScrewGuids.Remove(screwId);
                    }
                });
                PurgeGroups();
            }

            public void PurgeGroups()
            {
                var i = 0;

                while ( i < Groups.Count)
                {
                    var currGroup = Groups[i];

                    if (!currGroup.ScrewGuids.Any())
                    {
                        Groups.Remove(currGroup);
                        continue;
                    }

                    i++;
                }
            }

            public bool Serialize(ArchivableDictionary serializer)
            {
                var allScrewGroupsArchive = new ArchivableDictionary();
                Groups.ForEach(x =>
                {
                    var dataDict = SerializationFactory.CreateSerializedArchive(x);
                    allScrewGroupsArchive.Set($"{ScrewGroup.SerializationLabelConst}_{Guid.NewGuid()}", dataDict);
                });

                serializer.Set(SerializationLabel, allScrewGroupsArchive);

                return true;
            }

            public bool DeSerialize(ArchivableDictionary serializer)
            {
                Groups = SerializationFactory.DeserializeScrewGroup(serializer);

                return true;
            }
        }
    }
}
