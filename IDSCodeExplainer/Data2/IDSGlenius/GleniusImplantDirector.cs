using IDS.Common;
using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.Forms;
using IDS.Glenius.Graph;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Collections;
using Rhino.Commands;
using Rhino.FileIO;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius
{
    public class GleniusImplantDirector : ImplantDirectorBase
    {
        private const string KeyCaseId = "CASE_ID";

        private const string KeyDefectSide = "defect_side";

        private const string KeyBlockToKeywordMapping = "BlockToKeywordMapping";

        private const string DefaultPrefix = "default";

        public GleniusGraph Graph { get; private set; }

        public SolidWallManager SolidWallObjectManager { get; private set; }

        public ScrewManager ScrewObjectManager { get; private set; }

        public DesignPhase CurrentDesignPhase { get; private set; }

        public Dictionary<IBB, string> BlockToKeywordMapping { get; set; }

        public AnalyticSphere PreopCor { get; set; }

        public bool DeleteEmptyLayersOnDelete { get; set; }

        public GleniusImplantDirector(RhinoDoc doc, IPluginInfoModel pluginInfoModel) : base(doc, pluginInfoModel)
        {
            IsTestingMode = false;
            DeleteEmptyLayersOnDelete = true;
            this.CurrentDesignPhase = DesignPhase.Initialization;
            BlockToKeywordMapping = new Dictionary<IBB, string>();

            HandleGleniusGraphInitiation();
            HandleSolidWallManagerInitiation();
            HandleScrewManagerInitiation();
            InitializeCallbackUnSubcription();
        }

        public GleniusImplantDirector(
            RhinoDoc doc, BinaryArchiveReader archive, ArchivableDictionary dict,
            int pluginMajorVersion, int pluginMinorVersion, IPluginInfoModel pluginInfoModel) :
            base(doc, archive, dict, pluginMajorVersion, pluginMinorVersion, pluginInfoModel)
        {
            IsTestingMode = false;
            DeleteEmptyLayersOnDelete = true;

            HandleGleniusGraphInitiation();
            HandleSolidWallManagerInitiation();
            HandleScrewManagerInitiation();
            InitializeCallbackUnSubcription();
        }

        public void HandleGleniusGraphInitiation()
        {
            if (Graph == null)
            {
                Graph = new GleniusGraph(this);
            }
            Graph.SubscribeForGraphInvalidation();
        }

        public void HandleSolidWallManagerInitiation()
        {
            if (SolidWallObjectManager == null)
            {
                SolidWallObjectManager = new SolidWallManager(this);
            }
            SolidWallObjectManager.SubscribeForUndoRedo();
        }

        public void HandleScrewManagerInitiation()
        {
            if (ScrewObjectManager == null)
            {
                ScrewObjectManager = new ScrewManager(Document);
            }
            ScrewObjectManager.SubscribeScrewInvalidation();
        }

        private void InitializeCallbackUnSubcription()
        {
            if (OnUnsubscribeCallback == null)
            {
                OnUnsubscribeCallback += () =>
                {
                    SolidWallObjectManager.UnsubscribeForUndoRedo();
                    ScrewObjectManager.UnSubscribeScrewInvalidation();
                    Graph.UnsubscribeForGraphInvalidation();
                };
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

            caseId = dict.GetString(KeyCaseId, "Unset");
            DefectSide = dict.GetString(KeyDefectSide, null);

            var blockToKeywordMappingArchiver = new DictionaryArchiver<IBB, string>();
            BlockToKeywordMapping = blockToKeywordMappingArchiver.LoadFromArchive(dict, KeyBlockToKeywordMapping) as Dictionary<IBB, string>;

            //Anatomy
            _canTriggerOnAnatomyMeasurementsChanging = false;
            var anatomicalMeasurementsArchiver = new AnatomicalMeasurementsArchiver();
            AnatomyMeasurements = anatomicalMeasurementsArchiver.LoadFromArchive(dict, defectIsLeft);
            DefaultAnatomyMeasurements = anatomicalMeasurementsArchiver.LoadFromArchive(dict, DefaultPrefix, defectIsLeft);
            
            //backward compatibility
            if (DefaultAnatomyMeasurements == null && AnatomyMeasurements != null)
            {
                DefaultAnatomyMeasurements = new AnatomicalMeasurements(AnatomyMeasurements);
            }
            _canTriggerOnAnatomyMeasurementsChanging = true;

            //Solid Walls
            var solidWallManager = new SolidWallManager(this);
            solidWallManager.LoadFromArchive(dict);
            SolidWallObjectManager = solidWallManager;

            var preopCorArchiver = new PreopCorArchiver();
            PreopCor = preopCorArchiver.LoadFromArchive(dict);

            base.RestoreTraceabilityInformation(dict);
        }

        public override DesignPhaseProperty CurrentDesignPhaseProperty => DesignPhases.Phases[CurrentDesignPhase];

        public string DefectSide { get; set; }
        public override bool defectIsLeft => DefectSide.ToLowerInvariant() == "left";

        public override string caseId
        {
            get;
            set;
        }

        private bool _canTriggerOnAnatomyMeasurementsChanging = true;
        private AnatomicalMeasurements _anatomyMeasurements;
        public AnatomicalMeasurements AnatomyMeasurements
        {
            get { return _anatomyMeasurements; }
            set
            {
                var newAnatomyMeasurements = value;
                if (_anatomyMeasurements != newAnatomyMeasurements && _canTriggerOnAnatomyMeasurementsChanging)
                {
                    OnAnatomyMeasurementsChanging(newAnatomyMeasurements);
                }
                _anatomyMeasurements = newAnatomyMeasurements;
            }
        }

        public AnatomicalMeasurements DefaultAnatomyMeasurements { get; set; }

        public Head Head
        {
            get
            {
                var objectManager = new GleniusObjectManager(this);
                return objectManager.GetBuildingBlock(IBB.Head) as Head;
            }
        }

        public override void EnterDesignPhase(DesignPhaseProperty toPhase)
        {
            var toDesignPhase = DesignPhases.Phases.Where(p => p.Value == toPhase).Select(p => p.Key).First();
            EnterDesignPhase(toDesignPhase);
        }
        
        public void EnterDesignPhase(DesignPhase toPhase)
        {
            // Swap current design phase
            CurrentDesignPhase = toPhase;
            ResetVisualizers();
        }

        public override bool IsCommandRunnable(Command command, bool printMessage = false)
        {
            if (IDSPluginHelper.CloseAfterCommand)
            {
                return false;
            }

            var idsAttr = command.GetType().GetCustomAttributes(typeof(IDSGleniusCommandAttribute), false) as IEnumerable<IDSGleniusCommandAttribute>;

            // If no attributes defined: it is runnable
            if (null == idsAttr || !idsAttr.Any())
            {
                return true;
            }

            // Check phase and data
            var cmdAttr = idsAttr.First();
            return IsCommandRunnable(cmdAttr, printMessage);
        }

        private bool IsCommandRunnable(IDSGleniusCommandAttribute attributes, bool printMessage = false)
        {
            // Check the phase (bitwise operator or Enum.HasFlag)
            if (IsAllowedInCurrentPhase(attributes.phasesWhereRunnable) || !printMessage)
            {
                return CheckDataAvailable(printMessage, attributes.requiredBlocks.ToArray());
            }

            IDSPluginHelper.WriteLine(LogCategory.Warning, "The command is not available in the {0} phase", CurrentDesignPhase);
            return false;

            // Only check data if phase is OK
        }

        private bool IsAllowedInCurrentPhase(DesignPhase phaseFlag)
        {
            // Allowable when ALL flags in currentPhase are set to 1 in phaseFlags
            // - when the current phase is None (all zeros), this is always true
            // - when the phaseFlags are Any (all ones) this is always true
            return (CurrentDesignPhase & phaseFlag) == CurrentDesignPhase;
        }

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

        private bool IsDataAvailable(out string message, params IBB[] blocks)
        {
            var missing_msg = new List<string>();
            var available = true;

            var objectManager = new GleniusObjectManager(this);

            // Check all building blocks
            foreach (var block in blocks)
            {
                bool blockAvailable = objectManager.HasBuildingBlock(block);
                if (!blockAvailable)
                {
                    missing_msg.Add(BuildingBlocks.Blocks[block].Name);
                }
                available &= blockAvailable;
            }

            // Construct message
            message = "";
            if (missing_msg.Count > 0)
            {
                string missingString = string.Join(", ", missing_msg);
                message = $"Missing data: {missingString}.";
            }

            return available;
        }

        public override void PrepareObjectsForArchive()
        {
            var rhobjs = GetObjectsForArchive();
            
            foreach (var rhobj in rhobjs)
            {
                var block = rhobj as IBBinterface<GleniusImplantDirector>;
                block?.PrepareForArchiving();
            }
        }

        public override void RestoreCustomRhinoObjects(RhinoDoc doc)
        {
            var isScrewInvalidationSubscribed = ScrewObjectManager.IsScrewInvalidationSubscribed;
            if (isScrewInvalidationSubscribed)
            {
                ScrewObjectManager.UnSubscribeScrewInvalidation();
            }

            var objectManager = new GleniusObjectManager(this);
            objectManager.RestoreCustomRhinoObjects(doc);

            if (isScrewInvalidationSubscribed && !ScrewObjectManager.IsScrewInvalidationSubscribed)
            {
                ScrewObjectManager.SubscribeScrewInvalidation();
            }
        }

        public override void WriteToArchive(BinaryArchiveWriter archive)
        {
            var dict = new ArchivableDictionary(ArchiveVersion, "GleniusImplantDirector") {Version = ArchiveVersion};
            // store version to anticipate future changes

            dict.SetEnumValue<DesignPhase>(KeyPhase, CurrentDesignPhase);
            dict.Set(KeyCaseId, caseId);
            dict.Set(KeyDefectSide, DefectSide);

            var blockToKeywordMappingArchiver = new DictionaryArchiver<IBB, string>();
            dict.Set(KeyBlockToKeywordMapping, blockToKeywordMappingArchiver.CreateArchive(BlockToKeywordMapping));

            //Anatomy
            var anatomicalMeasurementsArchiver = new AnatomicalMeasurementsArchiver();
            var anatomicalArchive = anatomicalMeasurementsArchiver.CreateArchive(AnatomyMeasurements);

            if(anatomicalArchive != null)
            {
                dict.AddContentsFrom(anatomicalArchive);
            }

            var defaultAnatomicalArchive = anatomicalMeasurementsArchiver.CreateArchive(DefaultAnatomyMeasurements, DefaultPrefix);
            if (defaultAnatomicalArchive != null)
            {
                dict.AddContentsFrom(defaultAnatomicalArchive);
            }

            //Solid Walls
            var solidWallInfoArchive = SolidWallObjectManager.CreateArchive();
            if (solidWallInfoArchive != null)
            {
                dict.AddContentsFrom(solidWallInfoArchive);
            }

            var preopCorArchiver = new PreopCorArchiver();
            var archivedPreopCor = preopCorArchiver.CreateArchive(PreopCor);
            if (archivedPreopCor != null)
            {
                dict.AddContentsFrom(archivedPreopCor);
            }

            WriteToArchive(dict);

            archive.WriteDictionary(dict);
        }  

        public override void SetVisibilityByPhase()
        {
            Visibility.SetVisibilityByPhase(this);
        }

        public override string CurrentDesignPhaseName { get { return CurrentDesignPhase.ToString(); } }

        public override void OnInitialView(RhinoDoc openedDoc)
        {
            // Set appropriate view
            View.SetIDSDefaults(openedDoc);
            SetVisibilityByPhase();

            // Refresh the head panel
            if (CurrentDesignPhase == DesignPhase.Head)
            {
                HeadPanel.OpenPanel();
                var headPanel = HeadPanel.GetPanelViewModel();
                if (headPanel != null)
                {
                    headPanel.Director = this;
                    headPanel.AnatomicalMeasurements = AnatomyMeasurements;
                    HeadPanel.SetEnabled(true);
                }
            }
            else
            {
                HeadPanel.SetEnabled(false);
            }

            ResetVisualizers();

            // Set phase to draft to make sure no commands can be executed
            if (documentType == DocumentType.ScrewQC ||
                documentType == DocumentType.ScaffoldQC ||
                documentType == DocumentType.ApprovedQC)
            {
                Relations.PhaseChanger.ChangePhase(this, DesignPhase.Draft, false);
            }
        }

        public override void OnObjectDeleted()
        {

        }

        private void OnAnatomyMeasurementsChanging(AnatomicalMeasurements newAnatomicalMeasurements)
        {
            var objectManager = new GleniusObjectManager(this);
            if (!objectManager.HasBuildingBlock(IBB.Head))
            {
                return;
            }

            var headPanel = HeadPanel.GetPanelViewModel();
            if (headPanel?.Director != null && headPanel.AnatomicalMeasurements != newAnatomicalMeasurements)
            {
                headPanel.AnatomicalMeasurements = newAnatomicalMeasurements;
            }
        }

        public void TransformAnatomicalMeasurements(Transform transform)
        {
            DefaultAnatomyMeasurements.Transform(transform);
            _anatomyMeasurements.Transform(transform);
            OnAnatomyMeasurementsChanging(_anatomyMeasurements);

            ReconstructionMeasurementVisualizer.Get().Reset();
            ReconstructionMeasurementVisualizer.Get().Initialize(this);
            ReconstructionMeasurementVisualizer.Get().ShowAll(false);
        }

        private void ResetVisualizers()
        {
            HeadFullSphereVisualizer.Get().Reset();
            CylindricalOffsetVisualizer.Get().Reset();
            ScapulaReamedOffsetVisualizer.Get().Reset();
            MetalBackingPlaneVisualizer.Get().Reset();
        }
    }
}