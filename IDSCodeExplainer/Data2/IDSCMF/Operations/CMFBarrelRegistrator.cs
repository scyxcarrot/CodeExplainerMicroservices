using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Quality;
using IDS.CMF.Utilities;
using IDS.CMF.V2.CasePreferences;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Operations
{
    public class CMFBarrelRegistrator : IDisposable
    {
        private readonly CMFImplantDirector director;
        private readonly ScrewRegistration screwRegistration;
        private readonly List<MeshObject> originalMeshes;
        private readonly Dictionary<string, Dictionary<string, GeometryBase>> _barrelAidesDictionary;

        public List<MeshObject> OriginalMeshes
        {
            get { return originalMeshes; }
        }

        public CMFBarrelRegistrator(CMFImplantDirector director)
        {
            this.director = director;
            screwRegistration = new ScrewRegistration(director, true);
            originalMeshes = new List<MeshObject>();
            _barrelAidesDictionary = new Dictionary<string, Dictionary<string, GeometryBase>>();
        }

        public bool RegisterAllGuideRegisteredBarrel(Mesh guideSupport, out bool areAllBarrelsMeetingSpecs)
        {
            return RegisterAllGuideRegisteredBarrel(false, guideSupport, out areAllBarrelsMeetingSpecs);
        }

        public bool RegisterOnlyNewGuideRegisteredBarrel(Mesh guideSupport, out bool areAllBarrelsMeetingSpecs)
        {
            return RegisterAllGuideRegisteredBarrel(true, guideSupport, out areAllBarrelsMeetingSpecs);
        }

        private bool RegisterAllGuideRegisteredBarrel(bool newOnly, Mesh guideSupport, out bool areAllBarrelsMeetingSpecs)
        {
            areAllBarrelsMeetingSpecs = true;

            originalMeshes.Clear();

            foreach (var casePreferenceData in director.CasePrefManager.CasePreferences)
            {
                bool barrelsMeetingSpecs;
                RegisterScrewsBarrel(casePreferenceData, guideSupport, newOnly, out barrelsMeetingSpecs);
                areAllBarrelsMeetingSpecs = areAllBarrelsMeetingSpecs && barrelsMeetingSpecs;
            }

            return true;
        }

        public bool RegisterScrewsBarrel(CasePreferenceDataModel casePreferenceData, Mesh guideSupport, bool newOnly, out bool areAllBarrelsMeetingSpecs)
        {
            areAllBarrelsMeetingSpecs = true;

            var objectManager = new CMFObjectManager(director);
            var implantComponent = new ImplantCaseComponent();
            var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, casePreferenceData);
            var screwsObj = objectManager.GetAllBuildingBlocks(screwBuildingBlock);

            originalMeshes.Clear();

            var allExistingRegisteredBarrel = objectManager.GetAllImplantExtendedImplantBuildingBlocksIDs(IBB.RegisteredBarrel, casePreferenceData);
            var skippedLevelingScrewBarrels = new List<Screw>();

            foreach (var screwObj in screwsObj)
            {
                var screw = (Screw)screwObj;

                if (newOnly && screw.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
                {
                    continue;
                }

                bool isBarrelMeetingSpecs;
                bool isBarrelLevelingSkipped;
                Guid id;
                if (!RegisterGuideRegisteredBarrel(screw, guideSupport, casePreferenceData, out isBarrelMeetingSpecs, out id, out isBarrelLevelingSkipped))
                {
                    areAllBarrelsMeetingSpecs = false;
                    continue;
                }

                allExistingRegisteredBarrel.Remove(id);

                if (!isBarrelMeetingSpecs)
                {
                    areAllBarrelsMeetingSpecs = false;
                }

                if (isBarrelLevelingSkipped)
                {
                    skippedLevelingScrewBarrels.Add(screw);
                }
            }

            if (skippedLevelingScrewBarrels.Any())
            {
                BarrelLevelingErrorReporter.ReportGuideBarrelLevelingError(guideSupport,
                    skippedLevelingScrewBarrels);
            }

            if (!newOnly)
            {
                allExistingRegisteredBarrel.ForEach(x => { objectManager.DeleteObject(x); });
            }

            return true;
        }

        public Guid RegisterSingleScrewBarrel(Screw screw, Mesh guideSupportMesh, out bool isBarrelLevelingSkipped)
        {
            var objectManager = new CMFObjectManager(director);
            var casePreferenceData = objectManager.GetCasePreference(screw);

            originalMeshes.Clear();

            bool isBarrelMeetingSpecs;
            Guid id;
            if (!RegisterGuideRegisteredBarrel(screw, guideSupportMesh, casePreferenceData, out isBarrelMeetingSpecs, out id, out isBarrelLevelingSkipped))
            {
                if (screw.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
                {
                    objectManager.DeleteObject(screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel]);
                }
            }

            return id;
        }

        private bool RegisterGuideRegisteredBarrel(Screw screw, Mesh guideSupportMesh, CasePreferenceDataModel casePreferenceData, 
            out bool isMeetingSpecs, out Guid barrelId, out bool isBarrelLevelingSkipped)
        {
            isBarrelLevelingSkipped = false;
            isMeetingSpecs = false;
            barrelId = Guid.Empty;

            MeshObject plannedMesh;
            MeshObject originalMesh;
            var isBarrelMeetingSpecs = GetScrewBarrelRegistrationMeshes(screw, casePreferenceData.NCase, out plannedMesh, out originalMesh);
            if (!isBarrelMeetingSpecs)
            {
                return false;
            }

            if (!originalMeshes.Any(mesh => mesh.Name == originalMesh.Name))
            {
                originalMeshes.Add(originalMesh);
            }

            Transform alignmentTransform;
            PointUtilities.PointDistance distance;
            var screwBarrel = RegisterAndCalibrateBarrelOnOriginalPosition(originalMesh, plannedMesh, guideSupportMesh, screw, out alignmentTransform,
                out isMeetingSpecs, out distance, out isBarrelLevelingSkipped);
            if (screwBarrel == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Registration skipped: [Screw {screw.Index}.I{casePreferenceData.NCase}] - Invalid transformation matrix!");
                return false;
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Registration performed: [Screw {screw.Index}.I{casePreferenceData.NCase}] - Planned part: {plannedMesh.Name} to Original part: {originalMesh.Name}");

            var implantComponent = new ImplantCaseComponent();
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.RegisteredBarrel, casePreferenceData);
            var objectManager = new CMFObjectManager(director);

            var createNew = true;

            if (screw.ScrewGuideAidesInDocument.ContainsKey(IBB.RegisteredBarrel))
            {
                barrelId = screw.ScrewGuideAidesInDocument[IBB.RegisteredBarrel];

                if (director.Document.Objects.Find(barrelId) != null)
                {
                    barrelId = objectManager.SetBuildingBlockWithTransform(buildingBlock, screwBarrel, barrelId, alignmentTransform);
                    createNew = false;
                }
                else
                {
                    Msai.TrackException(new IDSException($"[INTERNAL] Screw {screw.Id} ScrewGuideAidesInDocument contains RegisteredBarrel but it didn't exist. " +
                                                         $"Please monitor if this still happening in newer test cases."), "CMF");
                }
            }

            if (createNew)
            {
                barrelId = IdsDocumentUtilities.AddNewGeometryBuildingBlockWithTransform(
                    objectManager,
                    director.IdsDocument,
                    buildingBlock,
                    screw.Id,
                    screwBarrel,
                    alignmentTransform);
            }

            screw.InvalidateGuideScrewAidesReferencesInDocument();
            screw.HandleAddGuideAides(IBB.RegisteredBarrel, barrelId);
            screw.RegisteredBarrelId = barrelId;

            var barrelObj = director.Document.Objects.Find(barrelId);
            var barrelMat = barrelObj.GetMaterial(true);

            var color = GetBarrelColor(director.CasePrefManager.SurgeryInformation.ScrewBrand, screw.ScrewType, isMeetingSpecs, distance.Distance);
            barrelMat.DiffuseColor = color;
            barrelMat.SpecularColor = color;
            barrelMat.AmbientColor = color;
            barrelMat.CommitChanges();

            return true;
        }

        private Brep RegisterAndCalibrateBarrelOnOriginalPosition(MeshObject originalMesh, MeshObject plannedMesh, Mesh guideSupport,
            Screw screw, out Transform alignmentTransform, out bool isMeetingSpecs, out PointUtilities.PointDistance distance, out bool isLevelingSkipped)
        {
            isLevelingSkipped = false;
            distance = new PointUtilities.PointDistance();
            isMeetingSpecs = false;

            var originalTransformation = originalMesh.Transform;
            var plannedTransformation = plannedMesh.Transform;
            
            Transform inverseTrans;
            if (!plannedTransformation.TryGetInverse(out inverseTrans))
            {
                alignmentTransform = Transform.Identity;
                return null;
            }

            var registrationTrans = Transform.Multiply(originalTransformation, inverseTrans);

            Curve leveledBarrelRef;
            var leveledBarrel = CalibrateBarrel(guideSupport, screw, registrationTrans,
                false, out alignmentTransform, out isMeetingSpecs, out distance, out leveledBarrelRef, out isLevelingSkipped);
            
            return leveledBarrel;
        }

        public Brep CalibrateBarrel(Mesh supportMesh, Screw screw, Transform registrationTrans, bool isPlannedBarrel, out Transform alignmentTransform, 
            out bool isMeetingSpecs, out PointUtilities.PointDistance distance, out Curve leveledBarrelRef, out bool isLevelingSkipped)
        {
            isLevelingSkipped = false;
            distance = new PointUtilities.PointDistance();
            isMeetingSpecs = false;
           
            var screwTrans = screw.AlignmentTransform;
            Dictionary<string, GeometryBase> dictionary;
            if (_barrelAidesDictionary.ContainsKey(screw.ScrewTypeAndBarrelType))
            {
                dictionary = _barrelAidesDictionary[screw.ScrewTypeAndBarrelType];
            }
            else
            {
                var barrelAideDataModel = new BarrelAideDataModel(screw.ScrewType, screw.BarrelType);
                dictionary = barrelAideDataModel.GenerateBarrelAideDictionary();
                _barrelAidesDictionary.Add(screw.ScrewTypeAndBarrelType, dictionary);
            }

            var screwbarrel = (dictionary[BarrelAide.Barrel] as Brep).DuplicateBrep();
            var screwBarrelRegistered = new Brep();
            screwBarrelRegistered.Append(screwbarrel);
            screwBarrelRegistered.Transform(screwTrans);
            screwBarrelRegistered.Transform(registrationTrans);

            alignmentTransform = Transform.Multiply(registrationTrans, screwTrans);

            var barrelRef = (dictionary[BarrelAide.BarrelRef] as Curve).DuplicateCurve();
            var screwBarrelRefRegistered = barrelRef;
            screwBarrelRefRegistered.Transform(screwTrans);
            screwBarrelRefRegistered.Transform(registrationTrans);

            var nameToDisplay = isPlannedBarrel ? "Planned" : "Reg.";

            var screwManager = new ScrewManager(director);
            var pref = screwManager.GetImplantPreferenceTheScrewBelongsTo(screw);

            if (supportMesh == null) //TODO: Ideally not to be placed here but as a responsibility of the caller. Need to be evaluated again its code design.
            {
                isLevelingSkipped = true;

                leveledBarrelRef = screwBarrelRefRegistered;
                return screwBarrelRegistered;
            }

            var registeredScrewDir = screw.Direction;
            registeredScrewDir.Transform(registrationTrans);

            var registeredScrewHead = screw.HeadPoint;
            registeredScrewHead.Transform(registrationTrans);

            Brep leveledBarrel;
            Transform levelingTransform;
            var barrelCalibrator = new BarrelCalibrator(supportMesh);

            var acceptableLimit = BarrelHelper.GetLevelingLimit(director.CasePrefManager.SurgeryInformation.ScrewBrand, screw.BarrelType);
            var defaultAcceptable = acceptableLimit.Default;
            var additonalOffset = acceptableLimit.AdditonalOffset;

            isMeetingSpecs = barrelCalibrator.CalibrateBarrel(registeredScrewHead, screwBarrelRegistered, screwBarrelRefRegistered,
                registeredScrewDir, additonalOffset, defaultAcceptable, out leveledBarrel, out leveledBarrelRef,
                out levelingTransform, out distance);

            if (!isMeetingSpecs)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Leveling {nameToDisplay} Barrel can't meet ideal specs for screw {screw.Index} for {pref.CaseNumber} " +
                                                             $"(Height: {StringUtilities.DoubleStringify(distance.Distance, 3)}mm)," +
                                                               $" Ideal Height: {StringUtilities.DoubleStringify(defaultAcceptable, 3)}mm." +
                                                             $" Please reposition/reorient your screw, or level it manually.");
            }

            alignmentTransform = Transform.Multiply(levelingTransform, alignmentTransform);

#if INTERNAL
            InternalUtilities.AddObject(screwBarrelRegistered, $"TEST {nameToDisplay} Barrel for Screw {screw.Index}.I{pref.CaseNumber}");
            InternalUtilities.AddObject(leveledBarrel, $"TEST Leveled {nameToDisplay} Barrel for Screw {screw.Index}.I{pref.CaseNumber}");
#endif
            return leveledBarrel;
        }
        
        public bool GetScrewBarrelRegistrationMeshes(Screw screw, int implantNum, out MeshObject plannedMesh, out MeshObject originalMesh)
        {
            plannedMesh = null;
            originalMesh = null;

            var result = screwRegistration.PerformImplantScrewRegistrationToOriginalPosition(screw);

            if (result.IsScrewOnGraft)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Registration skipped: [Screw {screw.Index}.I{implantNum}] - On graft!");
                return false;
            }

            if (result.IntersectedWithPlannedMeshObject == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Registration skipped: [Screw {screw.Index}.I{implantNum}] - No intersection with planned part!");
                return false; // if no implant mesh, skip registration
            }

            plannedMesh = result.IntersectedWithPlannedMeshObject;

            if (result.IsFloatingScrew)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, $"Registration skipped: [Screw {screw.Index}.I{implantNum}] - Floating screw!");
            }

            var dist = result.PastillePointToPlannedMeshDistance;

            //TODO: Remove when Necessary
            try
            {
                var screwManager = new ScrewManager(director);
                var pref = screwManager.GetImplantPreferenceTheScrewBelongsTo(screw);

                var trackingParameter = new Dictionary<string, string>
                {
                    { "Implant", pref != null ? pref.SelectedImplantType : "ERROR" },
                    {"Distance from Support To Planned", dist.ToString(CultureInfo.InvariantCulture)},
                    {
                        "Distance Tolerance (Max Acceptable)",
                        QCValues.FloatingScrewCheckTolerance.ToString(CultureInfo.InvariantCulture)
                    },
                    {"Registration Result", result.RegistrationSuccessful ? "Success" : "Failed"}
                };

                Msai.TrackDevEvent("Registered Barrel reg.dist.chk", "CMF", trackingParameter);
            }
            catch (Exception e)
            {
                Msai.TrackException(new IDSException("[DEV] MSAI ERROR!", e), "CMF");
            }

            originalMesh = result.RegisteredOnOriginalMeshObject;
            if (originalMesh == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Registration skipped: [Screw {screw.Index}.I{implantNum}] - No original part for {plannedMesh.Name}!");
                return false;
            }

            return result.RegistrationSuccessful;
        }

        public static Color GetBarrelColor(EScrewBrand screwBrand, string screwType, bool isMeetingSpecs, double distance)
        {
            //distance and acceptableLimit are not used as there is no longer BarrelLevelingMinWithinRange (Yellow/Orange)
            if (!isMeetingSpecs)
            {
                return Colors.BarrelLevelingNotMeetingSpecs;
            }
            else
            {
                return BuildingBlocks.Blocks[IBB.RegisteredBarrel].Color;
            }
        }

        public void Dispose()
        {
            screwRegistration.Dispose();
        }
    }
}
