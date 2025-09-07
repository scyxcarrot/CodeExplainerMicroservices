using IDS.CMF.CasePreferences;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Relations;
using IDS.CMF.ScrewQc;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.V2.Logics;
using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using IDS.Core.V2.Databases;
using IDS.Core.V2.DataModels;
using IDS.Core.V2.TreeDb.Interface;
using IDS.Core.V2.TreeDb.Model;
using IDS.Core.V2.Utilities;
using IDS.Core.Visualization;
using IDS.Interface.Implant;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using View = IDS.CMF.Visualization.View;

namespace IDS.CMF
{
    /// <summary>
    /// The CMFImplantDirector manages implant building blocks in the document
    /// and coordinates the design flow(transition between design phases).
    /// </summary>
    public class CMFImplantDirector : ImplantDirectorBase
    {
#if (INTERNAL)
        public static bool IsDebugMode { get; set; } = false;
#endif

        private const string KeyCaseId = "CASE_ID";
        private const string KeyGuidePhaseStarted = "GUIDE_PHASE_STARTED";
        private const string KeyNeedToUpdateScrewEntities = "NEED_TO_UPDATE_SCREW_ENTITIES_V2";
        private const string KeyNeedToUpdateMidfaceScrewEntities = "NEED_TO_UPDATE_MIDFACE_SCREW_ENTITIES";
        private const string KeyNeedToRegenerateGuideFlangeGuidingOutlines = "NEED_TO_REGENERATE_GUIDE_FLANGE_GUIDING_OUTLINES_V2";
        private const string KeyOsteotomiespPreop = "OSTEOTOMY_PREOP";
        private const string KeyGeneratedImplantSupportGuidingOutlines = "GENERATED_IMPLANT_SUPPORT_GUIDING_OUTLINES_V2";
        private const string KeyNeedToUpdateGuideEntitiesColor = "NEED_TO_UPDATE_GUIDE_ENTITIES_COLOR";
        private const string KeyNeedToUpdateImplantScrewTypeValue = "NEED_TO_UPDATE_IMPLANT_SCREW_TYPE_VALUE";
        private const string KeyNeedToUpdateGuideScrewTypeValue = "NEED_TO_UPDATE_GUIDE_SCREW_TYPE_VALUE";
        private const string KeyNeedToUpdateImplantScrewStyleValue = "NEED_TO_UPDATE_IMPLANT_SCREW_STYLE_VALUE";
        private const string KeyNeedToRestructureImplantSupportLayer = "NEED_TO_RESTRUCTURE_IMPLANT_LAYER";
        private const string KeyNeedToIntroduceGuideSupportRemovedMetalIntegrationRoI = "NEED_TO_INTRODUCE_GUIDE_SUPPORT_REMOVED_METAL";
        private const string KeyNeedToUpdateReferenceEntityLayer = "NEED_TO_UPDATE_REFERENCE_ENTITY_LAYER";
        private const string KeyMasterDatabase = "IDS_MASTER_DATABASE";
        private const string KeyNeedToSetBarrelType = "NEED_TO_SET_BARREL_TYPE";
        private const string KeyInputFileType = "INPUT_FILE_TYPE";
        private const string KeyIdsDatabase = "IDS_DATABASE";

        //changing screw qc algorithms from cylinder to capsule for ImplantScrewVicinityChecker, ImplantScrewAnatomicalObstacleChecker, OsteotomyIntersectionChecker 
        private const string KeyNeedToClearScrewQcResults = "NEED_TO_CLEAR_SCREWQC_RESULTS"; 

        public Dictionary<string, ExtendedImplantBuildingBlock> EBlock { get; set; }

        public ScrewBrandCasePreferencesInfo ScrewBrandCasePreferences { get; set; }
        public ScrewLengthsData ScrewLengthsPreferences { get; set; }
        public CasePreferenceManager CasePrefManager { get; set; }

        public ImplantManager ImplantManager { get; set; }

        public MedicalCoordinateSystem MedicalCoordinateSystem { get; set; }

        public ScrewManager.ScrewGroupManager ScrewGroups { get; set; }

        public GuideManager GuideManager { get; set; }

        public ScrewQcLiveUpdateHandler ImplantScrewQcLiveUpdateHandler { get; set; }

        public Mesh OsteotomiesPreop { get; set; }// The intersected mesh between osteotomies plane and preop

        public InputFileType CurrentInputFileType { get; set; }

        /// <summary>
        /// The archive version
        /// </summary>
        private const int archiveVersion = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="CMFImplantDirector"/> class.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="pluginInfoModel">The plugin info model.</param>
        /// <param name="subscribeToEvents">True if we need to subscribe to document events, put it to false if it is a unit test</param>
        public CMFImplantDirector(
            RhinoDoc doc, IPluginInfoModel pluginInfoModel, bool subscribeToEvents=true) 
            : base(doc, pluginInfoModel, subscribeToEvents)
        {
            // Init
            CurrentDesignPhase = DesignPhase.Initialization;
            CurrentInputFileType = InputFileType.FileTypeNotSet;
            SetupBuildingBlocks();
            CasePrefManager = new CasePreferenceManager(this);
            ImplantManager = new ImplantManager(new CMFObjectManager(this));
            ScrewGroups = new ScrewManager.ScrewGroupManager();
            GuideManager = new GuideManager(CasePrefManager);
            IsTestingMode = false;
            CasePrefManager.InitializeGraphs();
            CasePrefManager.InitializeEvents();
            InitializeCallbackUnSubcription();
            GuidePhaseStarted = false;
            NeedToUpdateScrewEntities = false;
            NeedToUpdateMidfaceScrewEntities = false;
            NeedToRegenerateGuideFlangeGuidingOutlines = false;
            GeneratedImplantSupportGuidingOutlines = true;
            NeedToUpdateGuideEntitiesColor = false;
            NeedToUpdateImplantScrewTypeValue = false;
            NeedToUpdateGuideScrewTypeValue = false;
            NeedToUpdateImplantScrewStyleValue = false;
            NeedToRestructureImplantSupportLayer = false;
            NeedToIntroduceGuideSupportRemovedMetalIntegrationRoI = false;
            NeedToUpdateReferenceEntityLayer = false;
            ImplantScrewQcLiveUpdateHandler = null;
            NeedToSetBarrelType = false;
            InitializeIdsDocument(new MemoryStream());

            IsForUserTesting = CMFPreferences.GetIsForUserTesting();
            UserTestingOverlayConduit.Instance.Enabled = IsForUserTesting;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CMFImplantDirector"/> class.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="archive">The archive.</param>
        /// <param name="pluginMajorVersion">The plugin major version.</param>
        /// <param name="pluginMinorVersion">The plugin minor version.</param>
        /// <param name="pluginInfoModel">The plugin info</param>
        /// <param name="subscribeToEvents">True if we need to subscribe to document events, put it to false if it is a unit test</param>
        public CMFImplantDirector(RhinoDoc doc, BinaryArchiveReader archive, ArchivableDictionary dict, int pluginMajorVersion, int pluginMinorVersion, IPluginInfoModel pluginInfoModel, bool subscribeToEvents=true) :
            base(doc, archive, dict, 
                pluginMajorVersion, pluginMinorVersion, pluginInfoModel, subscribeToEvents)
        {
            CasePrefManager = new CasePreferenceManager(this);
            SetupBuildingBlocks();
            ImplantManager = new ImplantManager(new CMFObjectManager(this));
            ScrewGroups = new ScrewManager.ScrewGroupManager();
            GuideManager = new GuideManager(CasePrefManager);
            ImplantScrewQcLiveUpdateHandler = null;
            IsTestingMode = false;

            RestoreData(dict); //this will set IsForUserTesting

            //If the file is from Live case, but IDS is for testing, this overrides it for safety.
            if (!IsForUserTesting && CMFPreferences.GetIsForUserTesting()) 
            {
                IsForUserTesting = true;
            }
            UserTestingOverlayConduit.Instance.Enabled = IsForUserTesting;

            CasePrefManager.InitializeGraphs();
            CasePrefManager.InitializeEvents();
            InitializeCallbackUnSubcription();
        }

        private void InitializeCallbackUnSubcription()
        {
            if (OnUnsubscribeCallback == null)
            {
                OnUnsubscribeCallback += () =>
                {
                    CasePrefManager.CasePreferences.ForEach(x => x.Graph.UnsubscribeForGraphInvalidation());
                    CasePrefManager.GuidePreferences.ForEach(x => x.Graph.UnsubscribeForGraphInvalidation());
                };
            }
        }

        private void SetupBuildingBlocks()
        {
            EBlock = new Dictionary<string, ExtendedImplantBuildingBlock>();
            var staticBlocks = BuildingBlocks.Blocks;
            foreach (var block in staticBlocks)
            {
                if (!block.Value.Name.Contains("{") && !block.Value.Name.Contains("}"))
                {
                    EBlock.Add(block.Value.Name, new ExtendedImplantBuildingBlock
                    {
                        Block = block.Value,
                        PartOf = block.Key
                    });
                }
            }
        }

        protected override void RestoreTraceabilityInformation(ArchivableDictionary dict)
        {
            // Design Phase
            DesignPhase storedPhase;
            var gotDesignPhase = dict.TryGetEnumValue<DesignPhase>(KeyPhase, out storedPhase);
            if (gotDesignPhase)
            {
                CurrentDesignPhase = storedPhase;
            }

            var gotInputFileType = dict.TryGetEnumValue<InputFileType>(KeyInputFileType, 
                out var storedInputFileType);
            CurrentInputFileType = 
                gotInputFileType ? storedInputFileType : InputFileType.FileTypeNotSet;

            caseId = dict.GetString(KeyCaseId, "Unset");
            GuidePhaseStarted = dict.GetBool(KeyGuidePhaseStarted, false);
            NeedToUpdateScrewEntities = dict.GetBool(KeyNeedToUpdateScrewEntities, true);
            NeedToUpdateMidfaceScrewEntities = dict.GetBool(KeyNeedToUpdateMidfaceScrewEntities, true);
            NeedToRegenerateGuideFlangeGuidingOutlines = dict.GetBool(KeyNeedToRegenerateGuideFlangeGuidingOutlines, true);
            GeneratedImplantSupportGuidingOutlines = dict.GetBool(KeyGeneratedImplantSupportGuidingOutlines, false);
            NeedToUpdateGuideEntitiesColor = dict.GetBool(KeyNeedToUpdateGuideEntitiesColor, true);
            NeedToUpdateImplantScrewTypeValue = dict.GetBool(KeyNeedToUpdateImplantScrewTypeValue, true);
            NeedToUpdateGuideScrewTypeValue = dict.GetBool(KeyNeedToUpdateGuideScrewTypeValue, true);
            NeedToUpdateImplantScrewStyleValue = dict.GetBool(KeyNeedToUpdateImplantScrewStyleValue, true);
            NeedToRestructureImplantSupportLayer = dict.GetBool(KeyNeedToRestructureImplantSupportLayer, true);
            NeedToIntroduceGuideSupportRemovedMetalIntegrationRoI = dict.GetBool(KeyNeedToIntroduceGuideSupportRemovedMetalIntegrationRoI, true);
            NeedToUpdateReferenceEntityLayer = dict.GetBool(KeyNeedToUpdateReferenceEntityLayer, true);
            NeedToSetBarrelType = dict.GetBool(KeyNeedToSetBarrelType, true);

            base.RestoreTraceabilityInformation(dict);
        }

        /// <summary>
        /// Gets the current design phase.
        /// </summary>
        /// <value>
        /// The current design phase.
        /// </value>
        public DesignPhase CurrentDesignPhase { get; private set; }

        public override string caseId { get; set; }

        public bool GuidePhaseStarted { get; set; }

        public bool NeedToUpdateScrewEntities { get; set; }

        public bool NeedToUpdateMidfaceScrewEntities { get; set; }

        public bool NeedToRegenerateGuideFlangeGuidingOutlines { get; set; }

        public bool GeneratedImplantSupportGuidingOutlines { get; set; }

        public bool NeedToUpdateGuideEntitiesColor { get; set; }

        public bool NeedToUpdateImplantScrewTypeValue { get; set; }

        public bool NeedToUpdateGuideScrewTypeValue { get; set; }

        public bool NeedToUpdateImplantScrewStyleValue { get; set; }

        public bool NeedToRestructureImplantSupportLayer { get; set; }

        public bool NeedToIntroduceGuideSupportRemovedMetalIntegrationRoI { get; set; }

        public bool NeedToUpdateReferenceEntityLayer { get; set; }

        public bool NeedToSetBarrelType { get; set; }

        /// <summary>
        /// Checks the data available.
        /// </summary>
        /// <param name="printMessage">if set to <c>true</c> [print message].</param>
        /// <param name="checkInspector">if set to <c>true</c> [check inspector].</param>
        /// <param name="blocks">The blocks.</param>
        /// <returns></returns>
        private bool CheckDataAvailable(bool printMessage, params IBB[] blocks)
        {
            string msg;
            var available = IsDataAvailable(out msg, blocks);
            if (printMessage && msg != "")
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, msg);
            }

            return available;
        }

        /// <summary>
        /// Determines whether [is allowed in current phase] [the specified phase flag].
        /// </summary>
        /// <param name="phaseFlag">The phase flag.</param>
        /// <returns>
        ///   <c>true</c> if [is allowed in current phase] [the specified phase flag]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsAllowedInCurrentPhase(DesignPhase phaseFlag)
        {
            // Allowable when ALL flags in currentPhase are set to 1 in phaseFlags
            // - when the current phase is None (all zeros), this is always true
            // - when the phaseFlags are Any (all ones) this is always true
            return (CurrentDesignPhase & phaseFlag) == CurrentDesignPhase;
        }

        /// <summary>
        /// Enters the design phase.
        /// </summary>
        /// <param name="toPhase">To phase.</param>
        public void EnterDesignPhase(DesignPhase toPhase)
        {
            // Swap current design phase
            CurrentDesignPhase = toPhase;
        }

        public override void EnterDesignPhase(DesignPhaseProperty toPhase)
        {
            var toDesignPhase = DesignPhases.Phases.Where(p => p.Value == toPhase).Select(p => p.Key).First();
            EnterDesignPhase(toDesignPhase);
        }

        /// <summary>
        /// Determines whether [is command runnable] [the specified command].
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="printMessage">if set to <c>true</c> [print message].</param>
        /// <returns>
        ///   <c>true</c> if [is command runnable] [the specified command]; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsCommandRunnable(Rhino.Commands.Command command, bool printMessage = false)
        {
            if (IDSPluginHelper.CloseAfterCommand)
            {
                return false;
            }

            var idsAttr = command.GetType().GetCustomAttributes(typeof(IDSCMFCommandAttributes), false) as IEnumerable<IDSCMFCommandAttributes>;

            // If no attributes defined: it is runnable
            if (null == idsAttr || !idsAttr.Any())
            {
                return true;
            }

            // Check phase and data
            var cmdAttr = idsAttr.First();
            return IsCommandRunnable(cmdAttr, printMessage);
        }

        /// <summary>
        /// Determines whether [is command runnable] [the specified attributes].
        /// </summary>
        /// <param name="attributes">The attributes.</param>
        /// <param name="printMessage">if set to <c>true</c> [print message].</param>
        /// <returns>
        ///   <c>true</c> if [is command runnable] [the specified attributes]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsCommandRunnable(IDSCMFCommandAttributes attributes, bool printMessage = false)
        {
            // Check the phase (bitwise operator or Enum.HasFlag)
            var phaseOk = IsAllowedInCurrentPhase(attributes.PhasesWhereRunnable);
            if (phaseOk)
            {
                return CheckDataAvailable(printMessage, attributes.RequiredBlocks.ToArray());
            }

            if (printMessage)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "The command is not available in the {0} phase", CurrentDesignPhase);
            }

            return false;
        }

        /// <summary>
        /// Determines whether [is data available] [the specified message].
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="blocks">The blocks.</param>
        /// <returns>
        ///   <c>true</c> if [is data available] [the specified message]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsDataAvailable(out string message, params IBB[] blocks)
        {
            var missingMsg = new List<string>();
            var available = true;

            // Check pre-op data

            var objManager = new CMFObjectManager(this);

            // Check all building blocks
            foreach (var block in blocks)
            {
                var blockAvailable = objManager.HasBuildingBlock(block);
                if (!blockAvailable)
                {
                    missingMsg.Add(block.ToString());
                }

                available &= blockAvailable;
            }

            // Construct message
            message = "";
            if (missingMsg.Count <= 0)
            {
                return available;
            }

            var missingString = string.Join(", ", missingMsg);
            message = $"Missing data: {missingString}.";

            return available;
        }

        /// <summary>
        /// Prepares the objects for archive.
        /// </summary>
        public override void PrepareObjectsForArchive()
        {
            // Prepare all custom RhinoObject objects for serialization
            var settings = new ObjectEnumeratorSettings
            {
                HiddenObjects = true
            };
            var rhobjs = Document.Objects.FindByFilter(settings);

            foreach (var rhobj in rhobjs)
            {
                var block = rhobj as IBBinterface<CMFImplantDirector>;
                block?.PrepareForArchiving();
            }
        }

        /// <summary>
        /// Restores the custom rhino objects.
        /// </summary>
        /// <param name="doc">The document.</param>
        public override void RestoreCustomRhinoObjects(RhinoDoc doc)
        {
            var objManager = new CMFObjectManager(this);
            objManager.RestoreCustomRhinoObjects(doc);
        }

        /// <summary>
        /// Restore master database from ArchivableDictionary
        /// </summary>
        /// <param name="dict">The archive dictionary.</param>
        private void RestoreMasterDatabase(ArchivableDictionary dict)
        {
            var bytes = dict.GetBytes(KeyMasterDatabase, null);
            if (bytes == null)
            {
                return;
            }

            try
            {
                var masterDatabase = BsonUtilities.Deserialize<IDSCMFMasterDatabase>(bytes);
                if (masterDatabase?.ImplantScrewQcDatabase == null)
                {
                    return;
                }

                var needToClearScrewQcResults = dict.GetBool(KeyNeedToClearScrewQcResults, true);
                if (needToClearScrewQcResults)
                {
                    masterDatabase.ImplantScrewQcDatabase = new ImplantScrewQcDatabase();
                }

                ImplantScrewQcLiveUpdateHandler = ImplantScrewQcDatabaseUtilities.GetImplantScrewLiveUpdateHandler(
                    masterDatabase.ImplantScrewQcDatabase);
            }
            catch (Exception ex)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to parse the master database due to: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes master database to ArchivableDictionary
        /// </summary>
        /// <param name="dict">The archive dictionary.</param>
        private void WriteMasterDatabase(ArchivableDictionary dict)
        {
            var masterDatabase = new IDSCMFMasterDatabase
            {
                ImplantScrewQcDatabase = ImplantScrewQcDatabaseUtilities.GetImplantScrewQcDatabaseFromDirector(this)
            };

            var bytes = BsonUtilities.Serialize(masterDatabase);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, 
                $"Memory consumption of Screw QC results : {StringUtilitiesV2.MemorySizeFormat(bytes.Length)}");
            dict.Set(KeyMasterDatabase, bytes);
            dict.Set(KeyNeedToClearScrewQcResults, false);
        }

        private void RestoreIdsDatabase(ArchivableDictionary dict)
        {
            var idsDatabaseBytes = dict.GetBytes(KeyIdsDatabase, null);
                
            var idsMemoryStream = new MemoryStream();
            if (idsDatabaseBytes != null)
            {
                idsMemoryStream.Write(idsDatabaseBytes, 0, idsDatabaseBytes.Length);
            }

            InitializeIdsDocument(idsMemoryStream);
        }

        private void InitializeIdsDocument(MemoryStream memoryStream)
        {
            var idsDatabase = new LiteDbDatabase(
                memoryStream,
                AppDomain.CurrentDomain.GetAssemblies());

            var idsConsole = new IDSRhinoConsole();
            IdsDocument = new IDSDocument(idsConsole, idsDatabase);

            TreeBackwardCompatibilityUtilities.CreateTree(this);

            //subscribe to event AFTER backward compatibility is done
            idsDatabase.OnDeleted += IdsDatabase_OnDeleted;
        }

        private void IdsDatabase_OnDeleted(IData data)
        {
            var objectManager = new CMFObjectManager(this);
            objectManager.DeleteObject(data.Id);
        }

        private void WriteIdsDatabase(ArchivableDictionary dictionary)
        {
            var idsDatabaseByteArray = WriteIdsDatabaseToByteArray();
            dictionary.Set(KeyIdsDatabase, idsDatabaseByteArray);
        }

        public byte[] WriteIdsDatabaseToByteArray()
        {
            return IdsDocument != null ? 
                IdsDocument.GetDatabaseBytes() : 
                Array.Empty<byte>();
        }

        /// <summary>
        /// Writes to archive.
        /// </summary>
        /// <param name="archive">The archive.</param>
        public override void WriteToArchive(BinaryArchiveWriter archive)
        {
            var dict = new ArchivableDictionary(archiveVersion, "CMFImplantDirector")
            {
                Version = archiveVersion
            };
            // store version to anticipate future changes

            dict.SetEnumValue(KeyPhase, CurrentDesignPhase);
            dict.SetEnumValue(KeyInputFileType, CurrentInputFileType);
            dict.Set(KeyCaseId, caseId);
            dict.Set(KeyGuidePhaseStarted, GuidePhaseStarted);
            dict.Set(KeyNeedToUpdateScrewEntities, NeedToUpdateScrewEntities);
            dict.Set(KeyNeedToUpdateMidfaceScrewEntities, NeedToUpdateMidfaceScrewEntities);
            dict.Set(KeyNeedToRegenerateGuideFlangeGuidingOutlines, NeedToRegenerateGuideFlangeGuidingOutlines);
            dict.Set(KeyGeneratedImplantSupportGuidingOutlines, GeneratedImplantSupportGuidingOutlines);
            dict.Set(KeyNeedToUpdateGuideEntitiesColor, NeedToUpdateGuideEntitiesColor);
            dict.Set(KeyNeedToUpdateImplantScrewTypeValue, NeedToUpdateImplantScrewTypeValue);
            dict.Set(KeyNeedToUpdateGuideScrewTypeValue, NeedToUpdateGuideScrewTypeValue);
            dict.Set(KeyNeedToUpdateImplantScrewStyleValue, NeedToUpdateImplantScrewStyleValue);
            dict.Set(KeyNeedToRestructureImplantSupportLayer, NeedToRestructureImplantSupportLayer);
            dict.Set(KeyNeedToIntroduceGuideSupportRemovedMetalIntegrationRoI, NeedToIntroduceGuideSupportRemovedMetalIntegrationRoI);
            dict.Set(KeyNeedToUpdateReferenceEntityLayer, NeedToUpdateReferenceEntityLayer);
            dict.Set(KeyNeedToSetBarrelType, NeedToSetBarrelType);
            CasePrefManager.SaveSurgeryInformationTo3Dm(dict);
            CasePrefManager.SaveCasePreferencesTo3Dm(dict);
            CasePrefManager.SaveGuidePreferencesTo3Dm(dict);
            MedicalCoordinateSystem.SaveMedicalCoordinateSystem(dict);
            dict.Set(KeyIsForTesting, IsForUserTesting);
            ScrewGroups.Serialize(dict);
            GuideManager.SaveGuideInformationTo3Dm(dict);
            dict.Set(KeyOsteotomiespPreop, OsteotomiesPreop);
            ImplantManager.SaveImplantInformationTo3Dm(dict);
            WriteMasterDatabase(dict);
            WriteIdsDatabase(dict);
            WriteToArchive(dict);

            archive.WriteDictionary(dict);
        }

        public void RestoreData(ArchivableDictionary dict)
        {
            CasePrefManager.LoadSurgeryInformationFrom3Dm(dict);
            ScrewBrandCasePreferences = CasePreferencesHelper.LoadScrewBrandCasePreferencesInfo(CasePrefManager.SurgeryInformation.ScrewBrand);
            ScrewLengthsPreferences = CasePreferencesHelper.LoadScrewLengthData();
            CasePrefManager.LoadCasePreferencesFrom3Dm(dict, CasePrefManager.SurgeryInformation, ScrewBrandCasePreferences, ScrewLengthsPreferences);
            CasePrefManager.LoadGuidePreferencesFrom3Dm(dict, ScrewBrandCasePreferences);
            MedicalCoordinateSystem = new MedicalCoordinateSystem(dict);

            if (dict.ContainsKey(KeyIsForTesting))
            {
                IsForUserTesting = dict.GetBool(KeyIsForTesting);
            }

            if (dict.ContainsKey(ScrewManager.ScrewGroupManager.SerializationLabelConst))
            {
                ScrewGroups = new ScrewManager.ScrewGroupManager();
                ScrewGroups.DeSerialize(dict);
            }

            GuideManager.LoadGuideInformationFrom3Dm(dict);

            if (dict.ContainsKey(KeyOsteotomiespPreop))
            {
                OsteotomiesPreop = (Mesh)dict[KeyOsteotomiespPreop];
            }

            ImplantManager.LoadImplantInformationFrom3Dm(dict);
            RestoreMasterDatabase(dict);
            RestoreIdsDatabase(dict);
        }

        public override void SetVisibilityByPhase()
        {
            Visualization.Visibility.SetVisibilityByPhase(this);
        }

        public override void OnInitialView(RhinoDoc openedDoc)
        {
            View.ResetLayouts(openedDoc);
            // Set appropriate view
            View.SetIDSDefaults(openedDoc);
            SetVisibilityByPhase();

            // Set phase to draft to make sure no commands can be executed
            if (GeneralUtilities.IsDraft(this))
            {
                PhaseChanger.ChangePhase(this, DesignPhase.Draft, false);
            }
        }

        public override void OnObjectDeleted()
        {
            //Empty
        }

        public override DesignPhaseProperty CurrentDesignPhaseProperty => DesignPhases.Phases[CurrentDesignPhase];

        public override string CurrentDesignPhaseName => CurrentDesignPhase.ToString();
    }
}
