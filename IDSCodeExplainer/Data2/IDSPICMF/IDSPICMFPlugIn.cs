using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Preferences;
using IDS.CMF.Quality;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using IDS.Core.V2.Utilities;
using IDS.PICMF.Forms;
using IDS.PICMF.Helper;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using BoneThicknessAnalyzableObjectManager = IDS.CMF.Visualization.BoneThicknessAnalyzableObjectManager;
using Locking = IDS.Core.Operations.Locking;
using Visibility = IDS.CMF.Visualization.Visibility;

namespace IDS.PICMF
{

    /// <summary>
    /// Every RhinoCommon Plug-In must have one and only one PlugIn derived class. DO NOT create an
    /// instance of this class. It is the responsibility of Rhino to create an instance of this class.
    /// 
    /// You can override methods here to change the plug-in behavior on loading and shut down, add
    /// options pages to the Rhino _Option command and mantain plug-in wide options in a document.
    /// IDSPlugin is the Rhino Plugin class that derives all necessary plugin functionality. It
    /// contains references to all the Panels and UI controls for creating an implant. The IDSPlugin
    /// also serves as the main access point for all the implant components.
    /// </summary>
    /// <seealso cref="Rhino.PlugIns.PlugIn" />
    public class IDSPICMFPlugIn : Rhino.PlugIns.PlugIn, IDisposable
    {
        /// <summary>
        /// IDS main version
        /// </summary>
        private int ProjectMajorVersion = -1;

        /// <summary>
        /// IDS subversion
        /// </summary>
        private int ProjectMinorVersion = -1;

        /// <summary>
        /// The one and only instance of the plugin
        /// </summary>
        private static IDSPICMFPlugIn _sharedInstance;
        public static IDSPICMFPlugIn SharedInstance => _sharedInstance;

        /// <summary>
        /// Used to log exceptions thrown while running our program
        /// </summary>
        private System.Text.StringBuilder _errorLogger;

        /// <summary>
        /// Indicates wheter exceptions have to be logged
        /// </summary>
        private bool _recordErrorLog;

        /// <summary>
        /// Reference to the error log file
        /// </summary>
        private FileInfo _errorLogFile;

        /// <summary>
        /// Indicates whether a document is being opened
        /// </summary>
        private bool _isOpeningDocument;

        public string FileName { get; private set; }
        public int CaseVersion { get; set; }
        public int CaseDraft { get; set; }

        /// <summary>
        /// Indicates whether executing a command should force Rhino to close.
        /// </summary>
        public static bool CloseAfterCommand { get; set; }

        /// <summary>
        /// Flag to indicate that Rhino is running from a script
        /// </summary>
        public static bool ScriptMode
        {
            get
            {
                return IDSPluginHelper.ScriptMode;
            }
            set
            {
                IDSPluginHelper.ScriptMode = value;
            }
        }

        private readonly IDSPanel _panels;

        public static bool IsCMF { get; set; }

        //NOTE!!
        //Flow tends to be like this.
        //1)OnNewDocument
        //2)OnStartCommand
        //3)OnBeginOpenDocument
        //4)OnLayerEvent
        //5)ReadDocument //Where IsGlenius is set! Because Director is set here!
        //6)OnInitialView
        //7)OnEndCommand

        /// <summary>
        /// The Constructor assigns the newly created instance (this) to the "Instance" attribute
        /// </summary>
        public IDSPICMFPlugIn()
        {
            // Initialize variables
            CloseAfterCommand = false;
            ScriptMode = false;
            _panels = new IDSPanel(this);
            IsCMF = false;

            // Register event handlers
            SubscribeEvents();

            // Error reporting
            StartErrorLogging();

            // Make the plugin object accessible for all
            _sharedInstance = this;
            CaseVersion = -1;
            CaseDraft = -1;

            // Load general preferences
            DimensionConduit.SphereRadius = CMFPreferences.GetMeasurementsSphereRadius();
        }

        private void SubscribeEvents()
        {
            RhinoDoc.BeginOpenDocument += OnBeginOpenDocument;
            RhinoDoc.CloseDocument += OnCloseDocument;
            RhinoDoc.EndOpenDocumentInitialViewUpdate += OnInitialView;
            RhinoDoc.EndOpenDocument += OnEndOpenDocument;
            RhinoDoc.BeginSaveDocument += OnBeginSaveDocument;
            RhinoDoc.EndSaveDocument += OnEndSaveDocument;
            RhinoDoc.NewDocument += OnNewDocument;
            RhinoDoc.AddRhinoObject += OnAddObject;
            RhinoDoc.UndeleteRhinoObject += OnUndelete;
            RhinoDoc.DeleteRhinoObject += OnDelete;
            RhinoDoc.LayerTableEvent += OnLayerEvent;
            Command.BeginCommand += OnStartCommand;
            Command.EndCommand += OnEndCommand;
            Command.UndoRedo += OnUndoRedoCommand;
            RhinoApp.Closing += RhinoAppOnClosing;
            QcBubbleToggleOffUtilities.SubscribeEvent();
        }

        private void OnCloseDocument(object sender, DocumentEventArgs e)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)e.DocumentSerialNumber);
            director?.IdsDocument?.Dispose();

            // Destruct the associated director
            IDSPluginHelper.SetDirector(e.DocumentId, null); // Discard last reference
            IsCMF = false;
        }

        private void RhinoAppOnClosing(object sender, EventArgs e)
        {
            Msai.Terminate(PlugInInfo.PluginModel, FileName, CaseVersion, CaseDraft);
        }

        /// <summary>
        /// Executed when something is changed in the layer panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLayerEvent(object sender, Rhino.DocObjects.Tables.LayerTableEventArgs e)
        {
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)e.Document.RuntimeSerialNumber);
            if (director != null)
            {
                CMFScrewNumberBubbleConduitProxy.GetInstance().Invalidate(director);
                DimensionVisualizer.Instance.InvalidateConduits(e.Document);
            }

            if (e.EventType != Rhino.DocObjects.Tables.LayerTableEventType.Modified || _isOpeningDocument ||
                IDS.Core.Visualization.Visibility.IsVisibilityAutomaticallyAdjusted ||
                e.OldState.ParentLayerId == e.NewState.ParentLayerId)
            {
                return;
            }

            if (e.OldState.FullPath == e.NewState.FullPath)
            {
                return;
            }

            var parameters = new System.Collections.Generic.Dictionary<string, string>();
            parameters.Add("Old Layer", e.OldState.FullPath);
            parameters.Add("New Layer", e.NewState.FullPath);

            Msai.TrackOpsEvent("LAYER MODIFIED", "CMF", parameters);
        }

        private void OnAddObject(object sender, RhinoObjectEventArgs e)
        {
            if (_isOpeningDocument)
            {
                return;
            }

            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)e.TheObject.Document.RuntimeSerialNumber);

            if (director == null) //Should be null when a .3dm file is just opened
            {
                return;
            }

            HandleAddingExtendedBuildingBlock(director, e);
        }

        /// <summary>
        /// Executed when an object is deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDelete(object sender, RhinoObjectEventArgs e)
        {
            if (_isOpeningDocument || !IsCMF)
            {
                return;
            }

            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)e.TheObject.Document.RuntimeSerialNumber);
            CMFScrewNumberBubbleConduitProxy.GetInstance().Invalidate(director);
            var objectManager = new CMFObjectManager(director);
            if (!string.IsNullOrEmpty(e.TheObject.Name))
            {
                objectManager.RemoveEBlock(e.TheObject.Name);
            }
        }

        /// <summary>
        /// Executed when an object is undeleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUndelete(object sender, RhinoObjectEventArgs e)
        {
            if (_isOpeningDocument)
            {
                return;
            }

            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)e.TheObject.Document.RuntimeSerialNumber);
            if (director == null)
            {
                return;
            }

            CMFScrewNumberBubbleConduitProxy.GetInstance().Invalidate(director);

            HandleAddingExtendedBuildingBlock(director, e);

            InvalidateCasePreferencePanel(e);
        }

        private void InvalidateCasePreferencePanel(RhinoObjectEventArgs e)
        {
            if (e.TheObject != null && !string.IsNullOrEmpty(e.TheObject.Name) 
                                    && e.TheObject.Name.StartsWith(IBB.RegisteredBarrel.ToString()))
            {
                CasePreferencePanel.GetView().InvalidateUI();
            }
        }

        private void HandleAddingExtendedBuildingBlock(CMFImplantDirector director, RhinoObjectEventArgs e)
        {
            var objectManager = new CMFObjectManager(director);
            var casePreferenceData = objectManager.TryGetCasePreference(e.TheObject);
            var guidePreferenceData = objectManager.TryGetGuidePreference(e.TheObject);

            IBB ibb;

            var objectName = e.TheObject.Name ?? "";
            objectManager.TryGetIBB(objectName, out ibb);

            if (casePreferenceData != null)
            {
                var implantCaseComponent = new ImplantCaseComponent();
                if (implantCaseComponent.GetImplantComponents().Contains(ibb))
                {
                    var eBlock = implantCaseComponent.GetImplantBuildingBlock(ibb, casePreferenceData);
                    objectManager.HandleEBlock(eBlock);
                }
            }
            else if (guidePreferenceData != null)
            {
                var guideCaseComponent = new GuideCaseComponent();
                if (guideCaseComponent.GetGuideComponents().Contains(ibb))
                {
                    var eBlock = guideCaseComponent.GetGuideBuildingBlock(ibb, guidePreferenceData);
                    objectManager.HandleEBlock(eBlock);
                }
            }
            else if (ibb == IBB.ProPlanImport)
            {
                var proPlanImportComponent = new ProPlanImportComponent();
                var partName = proPlanImportComponent.GetPartName(objectName);
                var eBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(partName);

                if (!objectManager.HasEBlock(eBlock))
                {
                    objectManager.HandleEBlock(eBlock);
                }
            }
            else if (ibb != IBB.Generic)
            {
                var staticBlocks = BuildingBlocks.Blocks;

                if (staticBlocks.ContainsKey(ibb))
                {
                    var eBlock = new ExtendedImplantBuildingBlock
                    {
                        Block = staticBlocks[ibb],
                        PartOf = ibb
                    };

                    if (!objectManager.HasEBlock(eBlock))
                    {
                        objectManager.HandleEBlock(eBlock);
                    }
                }
            }
        }

        private void DoScrewInfoBubbleVisibility(object sender, CommandEventArgs e)
        {
            if (e.CommandEnglishName.ToLower() != CommandEnglishName.CMFToggleScrewInfoBubble.ToLower() &&
                e.CommandEnglishName.ToLower() != CommandEnglishName.CMFToggleGuideFixationScrewInfoBubble.ToLower())
            {
                var conduitProxyInstance = ScrewInfoConduitProxy.GetInstance();

                if (conduitProxyInstance.IsShowing())
                {
                    conduitProxyInstance.Show(false);
                    conduitProxyInstance.Reset();
                }
            }

            e.Document.Views.Redraw();
        }

        private void DoConnectionInfoBubbleVisibility(object sender, CommandEventArgs e)
        {
            if (e.CommandEnglishName.ToLower() != CommandEnglishName.CMFToggleConnectionInfoBubble.ToLower())
            {
                var conduitProxyInstance = ConnectionInfoConduitProxy.GetInstance();

                if (conduitProxyInstance.IsShowing())
                {
                    conduitProxyInstance.Show(false);
                    conduitProxyInstance.Reset();
                }
            }

            e.Document.Views.Redraw();
        }

        /// <summary>
        /// Executed when a command is started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStartCommand(object sender, CommandEventArgs e)
        {
            var cmdEngName = e.CommandEnglishName.ToLower();
            if (cmdEngName.Equals("delete"))
            {
                Msai.TrackOpsEvent("DELETE", "CMF");
            }

            DoScrewInfoBubbleVisibility(sender, e);
            DoConnectionInfoBubbleVisibility(sender, e);

            // Unlock all layers
            foreach (var layer in RhinoDoc.ActiveDoc.Layers)
            {
                if (!layer.IsLocked)
                {
                    continue;
                }
                layer.IsLocked = false;
            }

            // Report command name
            if (IsCMF)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default,
                    $"*** Executing {e.CommandEnglishName.ToUpper()} command ***");
            }

            // Close rhino if the flag was set
            if (!CloseAfterCommand || e.CommandPluginName !=
                Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title)
            {
                return;
            }
            Dialogs.ShowMessage("You tried to run an IDS command in a file that is not supposed to be edited. Rhino will close.",
                "Rhino will close now", ShowMessageButton.OK, ShowMessageIcon.Error);

            Visibility.HideTheOtherLayer(RhinoDoc.ActiveDoc);

            SystemTools.DiscardChanges();
            Msai.Terminate(PlugInInfo.PluginModel, FileName, CaseVersion, CaseDraft);
            RhinoApp.Exit();
        }

        /// <summary>
        /// Executed when a command is started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEndCommand(object sender, CommandEventArgs e)
        {
            if (!IsCMF)
            {
                return;
            }

            var cmdEngName = e.CommandEnglishName.ToLower();
            if (cmdEngName.Equals("unlock"))
            {
                Msai.TrackOpsEvent("UNLOCK", "CMF");
            }
            
            // Lock all objects
            if (e.CommandPluginName == Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title)
            {
                Locking.LockAll(RhinoDoc.ActiveDoc);
            }

            Visibility.HideTheOtherLayer(RhinoDoc.ActiveDoc);

            // Report command result
            WriteLine(LogCategory.Default,
                e.CommandResult == Result.Success
                    ? $"*** Finished {e.CommandEnglishName.ToUpper()} command ***"
                    : $"*** Aborted {e.CommandEnglishName.ToUpper()} command ***");
        }

        private void OnUndoRedoCommand(object sender, UndoRedoEventArgs e)
        {
            if (!IsCMF || (!e.IsEndUndo && !e.IsEndRedo))
            {
                return;
            }

            var command = GetCommands().FirstOrDefault(c => c.Id == e.CommandId);
            if (command == null)
            {
                return;
            }

            if (e.IsEndUndo)
            {
                Msai.TrackOpsEvent($"UNDO: {command.EnglishName}", "CMF");
            }
            else if (e.IsEndRedo)
            {
                Msai.TrackOpsEvent($"REDO: {command.EnglishName}", "CMF");
            }
        }

        /// <summary>
        /// This method is called by RhinoApp.GetPlugInObject(guid) to get the plugin
        /// </summary>
        /// <returns></returns>
        public override object GetPlugInObject()
        {
            return _sharedInstance;
        }

        /// <summary>
        /// Check if a command should be allowed
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static bool CheckIfCommandIsAllowed(Command command)
        {
            return IDSPluginHelper.CheckIfCommandIsAllowed(command);
        }

        /// <summary>
        /// Start logging errors to a log file.
        /// </summary>
        protected void StartErrorLogging()
        {
            _recordErrorLog = true;

            // Register exception reporter
            Rhino.Runtime.HostUtils.OnExceptionReport -= IdsExceptionReporter;
            Rhino.Runtime.HostUtils.OnExceptionReport += IdsExceptionReporter;

            // Make the error log file
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmm_ss_fff");
            var filename = "IDS_ERROR_LOG_" + timestamp + ".txt";
            _errorLogFile = SystemTools.MakeValidAvailableFilename(System.IO.Path.GetTempPath(), filename);

            // Write first text to error log
            _errorLogger = new System.Text.StringBuilder();
            _errorLogger.AppendLine("### Implant Design Suite Error Log ###\nThis file contains all exceptions thrown while running Rhino with the IDS Plugin loaded.");
        }

        /// <summary>
        /// Write all errors recorded since the last flush to the error log file.
        /// </summary>
        protected void FlushErrorLog()
        {
            // Write error log to file
            var recentErrors = _errorLogger.ToString();
            if (recentErrors.Length > 0)
            {
                // Expensive file write operation
                using (var outfile = new StreamWriter(_errorLogFile.FullName, true))
                    outfile.Write(recentErrors);
                WriteLine(LogCategory.Default, "Updated error log file at <{0}>", _errorLogFile.FullName);
                _errorLogger.Clear(); // Clear flushed content
            }
        }

        /// <summary>
        /// Handle any exceptions that occur while running our plug-in.
        /// </summary>
        /// <param name="source">An exception source text</param>
        /// <param name="exc"></param>
        private void IdsExceptionReporter(string source, Exception exc)
        {
            Msai.TrackException(exc, PlugInInfo.PluginModel.ProductName);
            // to shut down the thread gracefully
            IDSPluginHelper.LoadIndicatorCancellationTokenSource?.Cancel();
            var msg = _recordErrorLog ? "This error was written to the IDS error log" : "";
            WriteLine(LogCategory.Error, "Exception occurred in {0}:\n{1}\n{2}", source, exc.ToString(), msg);

            // Write to our error log
            if (_recordErrorLog)
            {
                _errorLogger.AppendFormat("===== Exception occurred in {0}. Details below. =====\n{1}\n", source, exc);
            }
        }

        private void AutoUpdate()
        {
            var helper = new UpdateHelper();
            helper.AutoUpdate();
        }

        /// <summary>
        /// Is called when the plug-in is being loaded.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        protected override Rhino.PlugIns.LoadReturnCode OnLoad(ref string errorMessage)
        {
            // Register panels
            RegisterIdsPanels();

            AutoUpdate();

            // Suppress server busy messages
            Rhino.Runtime.HostUtils.DisplayOleAlerts(false);

            WriteLine(LogCategory.Default, "You are using IDS version {0}", PlugInInfo.PluginModel.VersionLabel);
            IDSPluginHelper.PluginVersion = Version;

            return Rhino.PlugIns.LoadReturnCode.Success;
        }

        /// <summary>
        /// Register all panels provided by this plug-in with the Rhino application.
        /// </summary>
        protected void RegisterIdsPanels()
        {
            _panels.RegisterIDSPanels();
        }

        /// <summary>
        /// Called when a new document is created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNewDocument(object sender, Rhino.DocumentEventArgs e)
        {
            UserTestingOverlayConduit.Instance.Enabled = false;
            InitializePanels(e.Document);
            FileName = string.Empty;
            CaseVersion = -1;
            CaseDraft = -1;
        }

        private static void InitializePanels(RhinoDoc document)
        {
            //Initialize
        }

        /// <summary>
        /// Called first when a document is opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBeginOpenDocument(object sender, Rhino.DocumentOpenEventArgs e)
        {
            UserTestingOverlayConduit.Instance.Enabled = false;
            _isOpeningDocument = true;

            // Skip when 3dm file is imported into the current document
            if (e.Merge)
            {
                return;
            }

            IsCMF = false;
            InitializePanels(e.Document);
            FileName = Path.GetFileName(e.FileName);
            DimensionVisualizer.Instance.ResetConduits();
        }

        /// <summary>
        /// Called after OnBeginOpenDocument
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEndOpenDocument(object sender, Rhino.DocumentOpenEventArgs e)
        {
            if (!IsCMF)
            {
                return;
            }

            if (!_isOpeningDocument)
            {
                return;
            }

            // Skip when stp file is imported into the current document
            if (e.Merge)
            {
                return;
            }

            RhinoDoc.DeleteRhinoObject -= OnDelete;

            // Restore all Custom Rhino Object types (by default, they become standard Rhino objects
            // of their parent type upon loading)
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)e.DocumentSerialNumber);

            // remove the coarse guide preview before restoring custom rhino objects
            // this is to avoid errors when restoring coarse guide preview that is being phased out
            RemoveCoarseGuidePreview(director);
            director?.RestoreCustomRhinoObjects(e.Document);

            if (director != null)
            {
                CaseVersion = director.version;
                CaseDraft = director.draft;
                director.caseId = StringUtilitiesV2.ExtractCaseId(director.caseId);
            }

            RhinoDoc.DeleteRhinoObject += OnDelete;

            BoneThicknessAnalyzableObjectManager.HandleRemoveAllVertexColor(director);
            director.FileName = FileName;

            //TODO: Subtley pop up, optimizing your .3dm file
            var constraintQuery = new ConstraintMeshQuery(new CMFObjectManager(director));
            constraintQuery.GetConstraintMeshesForImplant(true); //somewhat backward compatibility

            var helper = new OriginalPositionedScrewAnalysisHelper(director);
            helper.GetPlannedBonesLowLoD(true);

            ImplantSupportManager.HandleRestructureImplantSupportLayerBackwardCompatibility(director);

            var screwManager = new ScrewManager(director);
            //If open old file where screw is not grouped, add them into a group with respective case preference panel
            if (!director.ScrewGroups.Groups.Any() && screwManager.GetAllScrews(false).Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Screws are not in groups, potentially this file is from older version of IDS. Grouping the screws by each implant preferences...");

                director.CasePrefManager.CasePreferences.ForEach(cp =>
                {
                    var allScrewsForImplant = screwManager.GetScrews(cp, false);

                    var group = new ScrewManager.ScrewGroup();
                    group.ScrewGuids.AddRange(allScrewsForImplant.Select(x => x.Id));
                    director.ScrewGroups.Groups.Add(group);
                });
                IDSPluginHelper.WriteLine(LogCategory.Default, "Screw grouping done!");

            }

            var converted = RegisteredBarrelUtilities.ConvertRegisteredBarrelIdsToImplantScrewIds(director);

            var screwLengthBackwardCompatibility = new ScrewLengthBackwardCompatibility(director);
            screwLengthBackwardCompatibility.PerformScrewLengthCorrection();

            var screwTypeBackwardsCompatibility = new ScrewTypeBackwardsCompatibility(director); 
            screwTypeBackwardsCompatibility.UpdateImplantScrewTypeBackwardCompatibility();
            screwTypeBackwardsCompatibility.UpdateGuideScrewTypeBackwardCompatibility();
            screwTypeBackwardsCompatibility.ChangeImplantScrewTypeIfScrewTypeContainsMini();

            var screwEntitiesHelper = new ScrewEntitiesHelper(director);
            screwEntitiesHelper.UpdateScrewEntities();

            var propertyHandler = new PropertyConfigurationChangedHandler(director);
            propertyHandler.HandleImplantPropertyValueChanged();

            var guideFlangeHelper = new GuideFlangeObjectHelper(director);
            guideFlangeHelper.InvalidateOldFlanges();

            if (!director.GeneratedImplantSupportGuidingOutlines)
            {
                var objectManager = new CMFObjectManager(director);
                ProPlanImportUtilities.RegenerateImplantSupportGuidingOutlines(objectManager);
                IDSPluginHelper.WriteLine(LogCategory.Default, "Generated margin and transition outline and intersected preop with osteotomies");
            }

            if (converted)
            {
                var message = "Converted linked registered barrels information.";
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, message);
            }

            UpdateGuideComponentColors(director);

            var handled = GuideManager.HandleGuideSupportRemovedMetalIntegrationRoIBackwardCompatibility(director);
            if (handled)
            {
                var message = "Guide support ROI is removed. Please re-indicate the ROI and regenerate the guide support.";
                IDSPluginHelper.WriteLine(LogCategory.Warning, message);
                IDSDialogHelper.ShowSuppressibleMessage(message, "Obsoleted Guide support ROI Found", ShowMessageIcon.Warning);
            }

            PreferencePanelHelper.InvalidateAllLinkedImplantDisplayStringOnGuidePreferences(director);
            PreferencePanelHelper.InvalidateAllLinkedGuideDisplayStringOnImplantPreferences(director);
            RhinoLayerUtilities.DeleteEmptyLayers(director.Document);

            UpdateReferenceEntities(director);
            SetBarrelType(director);
            AddUncalibratedScrew(director);
        }

        /// <summary>
        /// method for backward compatibility. Function will find reference entity layer and split it into multiple layers for each stl
        /// </summary>
        /// <param name="director">Directory class needed to access rhino functions and building blocks</param>
        private void UpdateReferenceEntities(CMFImplantDirector director)
        {
            if (!director.NeedToUpdateReferenceEntityLayer)
            {
                return;
            }
            // try to rename the Reference Entity Layers to different stl names
            // for backward compatibility
            var objManager = new CMFObjectManager(director);
            var oldReferenceEntities = objManager.GetAllBuildingBlocks(IBB.ReferenceEntities);

            // split the old version of reference entity to multipe layers
            var index = 1;
            foreach (RhinoObject oldReferenceEntity in oldReferenceEntities)
            {
                var cloneReferenceBuildingBlock = BuildingBlocks.Blocks[IBB.ReferenceEntities].Clone();
                cloneReferenceBuildingBlock.Layer = string.Format(cloneReferenceBuildingBlock.Layer, $"Reference Entities {index}");
                objManager.AddNewBuildingBlock(cloneReferenceBuildingBlock, oldReferenceEntity.GetMeshes(Rhino.Geometry.MeshType.Default)[0]);
                index += 1;

                director.Document.Objects.Delete(oldReferenceEntity, true, true);
            }

            // delete the layer after deleting the rhino objects inside
            // only delete if there is some old reference entity inside the file
            if (oldReferenceEntities.Any())
            {
                var oldReferenceBuildingBlock = BuildingBlocks.Blocks[IBB.ReferenceEntities].Clone();
                oldReferenceBuildingBlock.Layer = string.Format(oldReferenceBuildingBlock.Layer, $"Reference Entities");
                director.Document.Layers.Delete(director.Document.GetLayerWithPath(oldReferenceBuildingBlock.Layer), true);
            }

            // tell the director there is no need to update reference layer anymore after this since updated
            director.NeedToUpdateReferenceEntityLayer = false;
        }

        /// <summary>
        /// method for backward compatibility. Function will remove barrel name from screw type and register barrelnets
        /// </summary>
        /// <param name="director">Directory class needed to access rhino functions and building blocks</param>
        private void SetBarrelType(CMFImplantDirector director)
        {
            if (!director.NeedToSetBarrelType)
            {
                return;
            }

            var implantComponent = new ImplantCaseComponent();
            var objectManager = new CMFObjectManager(director);

            foreach (var caseData in director.CasePrefManager.CasePreferences)
            {
                var screwBuildingBlock = implantComponent.GetImplantBuildingBlock(IBB.Screw, caseData);
                var screwRhinoObjects = objectManager.GetAllBuildingBlocks(screwBuildingBlock);

                foreach (var screwRhinoObject in screwRhinoObjects)
                {
                    var screw = (Screw)screwRhinoObject;
                    var screwType = caseData.CasePrefData.ScrewTypeValue;
                    var barrelType = caseData.CasePrefData.BarrelTypeValue;
                    screw.Attributes.UserDictionary.Set(screw.KeyScrewType, screwType);
                    screw.ScrewType = screwType;
                    screw.BarrelType = barrelType;
                }
            }

            director.NeedToSetBarrelType = false;
        }

        /// <summary>
        /// method for backward compatibility. Function will remove all coarse guide previews
        /// </summary>
        /// <param name="director">Directory class needed to access rhino functions and building blocks</param>
        private void RemoveCoarseGuidePreview(CMFImplantDirector director)
        {
            var objManager = new CMFObjectManager(director);

            // loop through all guide preferences to delete the coarse guide previews
            foreach (GuidePreferenceDataModel guidePreference in director.CasePrefManager.GuidePreferences)
            {
                // delete the coarse guide previews if they are present
                // if guide preview not present, the GUID returned will be Guid.Empty
                // Delete object will just do nothing and return false
                objManager.DeleteObject(objManager.GetBuildingBlockId($"GuidePreview_{guidePreference.CaseGuid}"));
            }

            // delete layers with no meshes
            RhinoLayerUtilities.DeleteEmptyLayers(director.Document);
        }

        /// <summary>
        /// method for backward compatibility. Function will add uncalibrated screws cases with Implant Planning but no support
        /// </summary>
        /// <param name="director">Directory class needed to access rhino functions and building blocks</param>
        private void AddUncalibratedScrew(CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var implantCaseComponent = new ImplantCaseComponent();

            foreach (var caseData in director.CasePrefManager.CasePreferences)
            {
                var screwBuildingBlock = implantCaseComponent.GetImplantBuildingBlock(IBB.Screw, caseData);
                var screwRhinoObjects = objectManager.GetAllBuildingBlocks(screwBuildingBlock);

                if (screwRhinoObjects.Any() || !caseData.ImplantDataModel.IsHasConstruction())
                {
                    continue;
                }

                var targetLowLoDMeshes = new List<Mesh>();
                var implantSupport = implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, caseData);

                if (!objectManager.HasBuildingBlock(implantSupport))
                {
                    var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
                    targetLowLoDMeshes = constraintMeshQuery.GetVisibleConstraintMeshesForImplant(true).ToList();
                }

                var screwCreator = new ScrewCreator(director);
                if (!screwCreator.CreateAllScrewBuildingBlock(true, caseData, !targetLowLoDMeshes.Any() ? 
                        null : 
                        MeshUtilities.AppendMeshes(targetLowLoDMeshes)))
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"Failed to create screws for CaseID: {caseData.CaseGuid}");
                    return;
                }

                if (targetLowLoDMeshes.Any())
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Uncalibrated screws created on CaseID: {caseData.CaseGuid}");
                }
            }
        }

        private void UpdateGuideComponentColors(CMFImplantDirector director)
        {
            if (!director.NeedToUpdateGuideEntitiesColor)
            {
                return;
            }

            var guideComponent = new GuideCaseComponent();
            var blocks = new List<IBB>
            {
                IBB.GuideSurface,
                IBB.GuideFlange,
                IBB.GuideBridge,
                IBB.GuideFixationScrewLabelTag,
                IBB.GuideFixationScrewEye
            };

            var objectManager = new CMFObjectManager(director);

            foreach (var guidePreference in director.CasePrefManager.GuidePreferences)
            {
                foreach (var block in blocks)
                {
                    var buildingBlock = guideComponent.GetGuideBuildingBlock(block, guidePreference);
                    var doc = director.Document;
                    var rhinoObjects = objectManager.GetAllBuildingBlocks(buildingBlock).ToList();

                    if (doc == null || rhinoObjects == null)
                    {
                        return;
                    }

                    rhinoObjects.ForEach(rhinoObject => { doc.Objects.Unlock(rhinoObject.Id, true); });

                    rhinoObjects.ForEach(rhinoObject => { objectManager.UpdateMaterial(buildingBlock.Block, doc); });

                    rhinoObjects.ForEach(rhinoObject => { doc.Objects.Lock(rhinoObject.Id, true); });
                }
            }

            director.NeedToUpdateGuideEntitiesColor = false;
        }

        /// <summary>
        /// Called when the views have been initialized
        /// Called after OnEndOpenDocument
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInitialView(object sender, Rhino.DocumentOpenEventArgs e)
        {
            if (!IsCMF)
            {
                return;
            }

            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)e.DocumentSerialNumber);

            var doc = director.Document;

            if (!_isOpeningDocument)
            {
                return;
            }          
            
            // Unset flag
            _isOpeningDocument = false;

            if (e.Merge)
            {
                return;
            }


            director.OnInitialView(doc);
        }

        /// <summary>
        /// Prepare objects for archiving.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBeginSaveDocument(object sender, Rhino.DocumentSaveEventArgs e)
        {
            if (!IsCMF)
            {
                return;
            }

            // Archive objects
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)e.DocumentSerialNumber);
            director.PrepareObjectsForArchive();
            var casePrefViewModel = CasePreferencePanel.GetPanelViewModel();
            casePrefViewModel.InfoOnSurgeryControl.PrepareForSaveTo3dm();
            foreach (var listViewItem in casePrefViewModel.ListViewItems)
            {
                var cpc = (ImplantPreferenceControl) listViewItem;
                cpc.PrepareForSaveTo3dm();
            }
            foreach (var listViewItem in casePrefViewModel.GuideListViewItems)
            {
                var gpc = (GuidePreferenceControl)listViewItem;
                gpc.PrepareForSaveTo3dm();
            }
        }

        /// <summary>
        /// Perform actions after document has been saved/autosaved.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEndSaveDocument(object sender, Rhino.DocumentSaveEventArgs e)
        {
            if (_recordErrorLog)
            {
                FlushErrorLog();
            }
        }

        /// <summary>
        /// Called whenever a Rhino is about to save a .3dm file.
        /// If you want to save plug-in document data when a model is saved in
        /// a version 5 .3dm file, then you must override this function to
        /// return true and you must override WriteDocument().
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        protected override bool ShouldCallWriteDocument(Rhino.FileIO.FileWriteOptions options)
        {
            return !options.WriteGeometryOnly && !options.WriteSelectedObjectsOnly;
        }

        /// <summary>
        /// Called when Rhino is saving a .3dm file to allow the plug-in to save document user data.
        /// NOTE: data is stored as a linked list, this means:
        /// * you can call the same write method multiple times on the archive
        /// * you have to call read methods in same order as write methods CONVENTION:
        /// * each object/class stores an entire dictionary
        /// * that way the read/write order is not influenced by adding extra variables
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="archive"></param>
        /// <param name="options"></param>
        protected override void WriteDocument(RhinoDoc doc, Rhino.FileIO.BinaryArchiveWriter archive, Rhino.FileIO.FileWriteOptions options)
        {
            if (!IsCMF)
            {
                return;
            }

            // 1. Write the assembely version for this chunk
            var assembly = Assembly.GetExecutingAssembly();
            var info = FileVersionInfo.GetVersionInfo(assembly.Location);
            archive.Write3dmChunkVersion(info.FileMajorPart, info.FileMinorPart);

            // Serialize the director
            var director = IDSPluginHelper.GetDirector<CMFImplantDirector>((int)doc.RuntimeSerialNumber);

            // 2. Write the ImplantDirector
            director.WriteToArchive(archive); // WARNING: don't use same keys as in this class!
        }

        /// <summary>
        /// Called whenever a Rhino document is being loaded and plug-in user data was encountered written by a plug-in with this plug-in's GUID.
        /// NOTE: data is stored as a linked list, this means:
        /// - you can call the same write method multiple times on the archive
        /// - you have to call read methods in same order as write methods
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="archive"></param>
        /// <param name="options"></param>
        protected override void ReadDocument(RhinoDoc doc, Rhino.FileIO.BinaryArchiveReader archive, Rhino.FileIO.FileReadOptions options)
        {
            if (!_isOpeningDocument)
            {
                return;
            }

            // Check plugin version
            int projMajor, projMinor;
            archive.Read3dmChunkVersion(out projMajor, out projMinor);

            // check 3dm version
            var archiveVersion = Rhino.FileIO.File3dm.ReadArchiveVersion(FileName);
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"The currently open 3DM file has been saved as {archiveVersion / 10.0:0.0}");

            /// \todo If reading a new document: replace the current director if there is one, if merging/importing: preserve the current inspector
            if (options.OpenMode || options.NewMode)
            {
                // Recreate the director
                try
                {
                    var dict = archive.ReadDictionary();

                    if (dict.Name != "CMFImplantDirector")
                    {
                        return;
                    }

                    ProjectMajorVersion = projMajor;
                    ProjectMinorVersion = projMinor;

                    IsCMF = true;
                    var director = new CMFImplantDirector(doc, archive, dict, ProjectMajorVersion, ProjectMinorVersion, PlugInInfo.PluginModel);

                    Msai.Terminate(PlugInInfo.PluginModel, FileName, CaseVersion, CaseDraft);
                    Msai.Initialize(PlugInInfo.PluginModel, FileName, director.version, director.draft);

                    CaseVersion = director.version;
                    CaseDraft = director.draft;

                    // Associate director with this document
                    IDSPluginHelper.SetDirector((int)doc.RuntimeSerialNumber, director);

                    var casePrefViewModel = CasePreferencePanel.GetPanelViewModel();
                    if (casePrefViewModel != null)
                    {
                        //reset director
                        casePrefViewModel.InitializeDirector(null);
                    }

                    director.ImplantManager.SeparateTeethWrappedBuildingBlock();

                    CasePreferencePanel.OpenPanel();

                    UiUtilities.SubscribePanelWidthInvalidation();
                }
                catch (Rhino.FileIO.BinaryArchiveException)
                {

                }
            }
            else // import mode
            {
                /// \todo Check if its the same case ID -> don't allow import if not. Ask to overwrite existing components
                RhinoApp.WriteLine("[IDS:TODO] Implement functionality for importing components into existing document");
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Override this property to load the plugin at startup (instead of when needed)
        /// </summary>
        public override Rhino.PlugIns.PlugInLoadTime LoadTime => Rhino.PlugIns.PlugInLoadTime.AtStartup;

        /// <summary>
        /// Called on Rhino shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();
            if (!IsCMF)
            {
                return;
            }

            if (_recordErrorLog)
            {
                FlushErrorLog();
            }

        }

        /// <summary>
        /// Write a line to the Rhino command log window
        /// </summary>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="formatArgs"></param>
        public static void WriteLine(LogCategory category, string message, params object[] formatArgs)
        {
            IDSPluginHelper.WriteLine(category, message, formatArgs);
        }

        public void Dispose()
        {
        }
    }
}