using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.FileSystem;
using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Proxies;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Locking = IDS.Core.Operations.Locking;

namespace IDS
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
    public class IDSPIAmacePlugIn : Rhino.PlugIns.PlugIn, IDisposable
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
        private static IDSPIAmacePlugIn _sharedInstance;
        public static IDSPIAmacePlugIn SharedInstance => _sharedInstance;

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

        /// <summary>
        /// Indicates whether executing a command should force Rhino to close.
        /// </summary>
        public static bool CloseAfterCommand { get; set; }

        /// <summary>
        /// Flag to indicate that Rhino is running from a script
        /// </summary>
        public static bool ScriptMode { get; set; }

        /// <summary>
        /// The form about
        /// </summary>
        private frmAbout _formAbout;

        public static bool IsAmace { get; set; }

        public string FileName { get; private set; }
        public int CaseVersion { get; set; }
        public int CaseDraft { get; set; }

        /// <summary>
        /// Opens the about window.
        /// </summary>
        private void OpenAboutWindow()
        {
            System.Threading.Thread t = new System.Threading.Thread(new System.Threading.ThreadStart(AboutWindowThread));
            t.Start();
            t.Join(1000); // Show it for one second
        }

        /// <summary>
        /// Abouts the window thread.
        /// </summary>
        private void AboutWindowThread()
        {
            _formAbout = new frmAbout(PlugInInfo.PluginModel);
            _formAbout.ShowDialog(); // To make sure it stays shown
        }

        /// <summary>
        /// Closes the about window.
        /// </summary>
        private void CloseAboutWindow()
        {
            _formAbout.ThreadSafeClose();
        }

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
        public IDSPIAmacePlugIn()
        {
            //OpenAboutWindow();
            // Initialize variables
            CloseAfterCommand = false;
            ScriptMode = false;
            IsAmace = false;

            // Register event handlers
            SubscribeEvents();

            // Error reporting
            StartErrorLogging();

            // Make the plugin object accessible for all
            _sharedInstance = this;
            CaseVersion = -1;
            CaseDraft = -1;
        }

        private void SubscribeEvents()
        {
            RhinoDoc.BeginOpenDocument += OnBeginOpenDocument;
            RhinoDoc.CloseDocument += OnCloseDocument;
            RhinoDoc.EndOpenDocumentInitialiViewUpdate += OnInitialView;
            RhinoDoc.EndOpenDocument += OnEndOpenDocument;
            RhinoDoc.BeginSaveDocument += OnBeginSaveDocument;
            RhinoDoc.EndSaveDocument += OnEndSaveDocument;
            RhinoDoc.NewDocument += OnNewDocument;
            RhinoDoc.UndeleteRhinoObject += OnUndelete;
            RhinoDoc.DeleteRhinoObject += OnDelete;
            RhinoDoc.LayerTableEvent += OnLayerEvent;
            Command.BeginCommand += OnStartCommand;
            Command.EndCommand += OnEndCommand;
        }

        /// <summary>
        /// Executed when something is changed in the layer panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLayerEvent(object sender, Rhino.DocObjects.Tables.LayerTableEventArgs e)
        {

        }

        /// <summary>
        /// Executed when an object is deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDelete(object sender, RhinoObjectEventArgs e)
        {
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(e.TheObject.Document.DocumentId);
            if (director == null)
            {
                return;
            }

            Screw.OnDeleteFromDocumentComplete(director.Document);
        }

        /// <summary>
        /// Executed when an object is undeleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUndelete(object sender, RhinoObjectEventArgs e)
        {
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(e.TheObject.Document.DocumentId);
            if (director == null)
            {
                return;
            }

            Visibility.SetVisibilityByPhase(director);
        }

        /// <summary>
        /// Executed when a command is started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnStartCommand(object sender, CommandEventArgs e)
        {
            // Unlock all layers
            foreach (var layer in RhinoDoc.ActiveDoc.Layers)
            {
                if (!layer.IsLocked)
                {
                    continue;
                }
                layer.IsLocked = false;
                layer.CommitChanges();
            }

            // Report command name
            if (IsAmace)
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
            Dialogs.ShowMessageBox("You tried to run an IDS command in a file that is not supposed to be edited. Rhino will close.",
                "Rhino will close now",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

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
            if (!IsAmace)
            {
                return;
            }

            // Lock all objects
            if (e.CommandPluginName == Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title)
            {
                Locking.LockAll(RhinoDoc.ActiveDoc);
            }

            // Report command result
            WriteLine(LogCategory.Default,
                e.CommandResult == Result.Success
                    ? $"*** Finished {e.CommandEnglishName.ToUpper()} command ***"
                    : $"*** Aborted {e.CommandEnglishName.ToUpper()} command ***");
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
            var msg = _recordErrorLog ? "This error was written to the IDS error log" : "";
            WriteLine(LogCategory.Error, "Exception occurred in {0}:\n{1}\n{2}", source, exc.ToString(), msg);

            // Write to our error log
            if (_recordErrorLog)
            {
                _errorLogger.AppendFormat("===== Exception occurred in {0}. Details below. =====\n{1}\n", source, exc);
            }
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

            // Suppress server busy messages
            Rhino.Runtime.HostUtils.DisplayOleAlerts(false);

            WriteLine(LogCategory.Default, "You are using IDS version {0}", PlugInInfo.PluginModel.VersionLabel);

            return Rhino.PlugIns.LoadReturnCode.Success;
        }

        /// <summary>
        /// Register all panels provided by this plug-in with the Rhino application.
        /// </summary>
        protected void RegisterIdsPanels()
        {
            // Register Cup Positioning Panel
            RegisterCupPanel();

            // Register screw panel
            RegisterScrewPanel();
        }

        private void RegisterScrewPanel()
        {
            var screwPanelType = typeof(ScrewPanel);
            var resources = new AmaceResources();
            Panels.RegisterPanel(this, screwPanelType, "Screws", new System.Drawing.Icon(resources.ScrewPanelIconFile));
        }

        private void RegisterCupPanel()
        {
            var cupPanelType = typeof(CupPanel);
            var resources = new AmaceResources();
            Panels.RegisterPanel(this, cupPanelType, "Cup", new System.Drawing.Icon(resources.CupPanelIconFile));
        }

        /// <summary>
        /// Called when a new document is created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNewDocument(object sender, Rhino.DocumentEventArgs e)
        {
            InitializePanels(e.Document);
            FileName = string.Empty;
            CaseVersion = -1;
            CaseDraft = -1;
        }

        private static void InitializePanels(RhinoDoc document)
        {
            InitializeCupPanel(document);
            InitializeScrewPanel(document);
        }

        private static void InitializeScrewPanel(RhinoDoc document)
        {
            // Screw Panel
            ScrewPanel screwPanel = ScrewPanel.GetPanel();
            if (screwPanel != null)
            {
                screwPanel.doc = document;
            }
        }

        private static void InitializeCupPanel(RhinoDoc document)
        {
            // Cup Panel
            var cupPanel = CupPanel.GetPanel();
            if (cupPanel != null)
            {
                cupPanel.document = document;
            }
        }

        /// <summary>
        /// Called first when a document is opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBeginOpenDocument(object sender, Rhino.DocumentOpenEventArgs e)
        {
            _isOpeningDocument = true;

            // Skip when 3dm file is imported into the current document
            if (e.Merge)
            {
                return;
            }

            IsAmace = false;
            InitializePanels(e.Document);
            FileName = Path.GetFileName(e.FileName);
        }

        /// <summary>
        /// Called after OnBeginOpenDocument
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEndOpenDocument(object sender, Rhino.DocumentOpenEventArgs e)
        {
            if (!IsAmace)
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

            // Restore all Custom Rhino Object types (by default, they become standard Rhino objects
            // of their parent type upon loading)
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(e.DocumentId);
            director?.RestoreCustomRhinoObjects(e.Document);

            if (director != null)
            {
                CaseVersion = director.version;
                CaseDraft = director.draft;
            }

            if (director?.Inspector == null || ScriptMode)
            {
                return;
            }

            // Do version check
            var currentDirParts = e.FileName.Split(new [] { "\\" }, StringSplitOptions.None).ToList();
            currentDirParts.RemoveAt(currentDirParts.Count - 1);
            var logfile = string.Join("\\", currentDirParts) + "\\" + director.Inspector.CaseId + "_versioncheck.log";
            VersionControl.DoVersionCheck(director, false, false, logfile, PlugInInfo.PluginModel);

            //Show warning if loaded case was from before transition creation was implemented
            const int transitionCreationImplementationMajorVersion = 3;
            const int transitionCreationImplementationMinorVersion = 2;
            var objectManager = new AmaceObjectManager(director);

            if (ProjectMajorVersion < transitionCreationImplementationMajorVersion || 
                  (ProjectMajorVersion == transitionCreationImplementationMajorVersion &&
                   ProjectMinorVersion < transitionCreationImplementationMinorVersion) ||
                !objectManager.HasBuildingBlock(IBB.IntersectionEntity))
            {
                Dialogs.ShowMessageBox("This case is from the previous version of IDS. Intersection Entity is not created." +
                                       " Please go back to Scaffold Phase.", "Intersection Entity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

            var cupQCfolder = string.Format("2_Draft{0:D}_CupQC", director.draft);
            var implantQCfolder = string.Format("2_Draft{0:D}_ImplantQC", director.draft);

            DirectoryStructure.CheckWorkFileLocation(director, e.FileName,
                new List<string>() { cupQCfolder, implantQCfolder, "work", "inputs", "extrainputs", "extra_inputs" },
                director.InputFiles.Select(Path.GetFileName).ToList(),
                new List<string>() { "3dm", "mat" },
                new List<string>() { cupQCfolder, implantQCfolder });

            director.FileName = FileName;
        }

        /// <summary>
        /// Called when the views have been initialized
        /// Called after OnEndOpenDocument
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInitialView(object sender, Rhino.DocumentOpenEventArgs e)
        {
            if (!IsAmace)
            {
                return;
            }

            var director = IDSPluginHelper.GetDirector<ImplantDirector>(e.DocumentId);
            if (director?.Inspector == null)
            {
                return;
            }

            var doc = director.Document;

            if (!_isOpeningDocument)
            {
                return;
            }

            if (e.Merge)
            {
                return;
            }

            // Set appropriate view
            Amace.Visualization.View.SetIDSDefaults(doc);
            Visibility.SetVisibilityByPhase(director);

            // Disable conduits
            TogglePlateAnglesVisualisation.Disable(director);

            // Unset flag
            _isOpeningDocument = false;

            // Refresh the cup panel
            var cupPanel = CupPanel.GetPanel();
            if (null != cupPanel)
            {
                cupPanel.document = e.Document;
                {
                    cupPanel.UpdatePanelWithCup(director.cup);
                    var panelId = CupPanel.panelId;
                    if (director.CurrentDesignPhase == DesignPhase.Cup)
                    {
                        Panels.OpenPanel(panelId);
                        cupPanel.Enabled = true;
                    }
                    else
                    {
                        cupPanel.Enabled = false;
                    }
                }
            }

            // Refresh screw panel
            var screwPanel = ScrewPanel.GetPanel();
            if (screwPanel != null)
            {
                screwPanel.doc = e.Document;
                screwPanel.RefreshPanelInfo();
            }

            // Set phase to draft to make sure no commands can be executed
            if (director.documentType == DocumentType.CupQC ||
                director.documentType == DocumentType.ImplantQC ||
                director.documentType == DocumentType.Export)
            {
                PhaseChanger.ChangePhase(director, DesignPhase.Draft, false);
            }
        }

        /// <summary>
        /// Called when a document is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCloseDocument(object sender, Rhino.DocumentEventArgs e)
        {
            if (!IsAmace)
            {
                return;
            }

            // Destruct the associated director
            IDSPluginHelper.SetDirector(e.DocumentId, null); // Discard last reference
            IsAmace = false;
        }

        /// <summary>
        /// Prepare objects for archiving.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBeginSaveDocument(object sender, Rhino.DocumentSaveEventArgs e)
        {
            if (!IsAmace)
            {
                return;
            }

            // Disable the Fea Conduit
            if (PerformFea.FeaConduit != null && PerformFea.FeaConduit.Enabled)
            {
                PerformFea.DisableConduit(e.Document);
            }

            // Archive objects
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(e.DocumentId);
            director.PrepareObjectsForArchive();
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
            if (!IsAmace)
            {
                return;
            }

            // 1. Write the assembely version for this chunk
            var assembly = Assembly.GetExecutingAssembly();
            var info = FileVersionInfo.GetVersionInfo(assembly.Location);
            archive.Write3dmChunkVersion(info.FileMajorPart, info.FileMinorPart);

            // Serialize the director
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);

            // 2. Write the ImplantDirector
            director.WriteToArchive(archive); // WARNING: don't use same keys as in this class!

            // 3. Write the PreOpInspector
            var inspector = director.Inspector;
            if (null != inspector) /// \todo Unnecessary check?
            {
                director.Inspector.WriteToArchive(archive); // idem   
            }
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

            /// \todo If reading a new document: replace the current director if there is one, if merging/importing: preserve the current inspector
            if (options.OpenMode || options.NewMode)
            {
                // Recreate the director
                try
                {
                    var dict = archive.ReadDictionary();
                    if (dict.Name != "ImplantDirector")
                    {
                        return;
                    }

                    ProjectMajorVersion = projMajor;
                    ProjectMinorVersion = projMinor;

                    IsAmace = true;
                    var director = new ImplantDirector(doc, archive, dict, ProjectMajorVersion, ProjectMinorVersion, PlugInInfo.PluginModel);

                    Msai.Terminate(PlugInInfo.PluginModel, FileName, CaseVersion, CaseDraft);
                    Msai.Initialize(PlugInInfo.PluginModel, FileName, director.version, director.draft);
                    CaseVersion = director.version;
                    CaseDraft = director.draft;

                    var inspector = new PreOpInspector(doc, archive);
                    director.Inspector = inspector;
                    // Associate director with this document
                    IDSPluginHelper.SetDirector(doc.DocumentId, director);
                }
                catch (Rhino.FileIO.BinaryArchiveException e)
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
            if (!IsAmace)
            {
                return;
            }

            if (_recordErrorLog)
            {
                FlushErrorLog();
            }

        }

        /// <summary>
        /// Called right after plug-in is created and is responsible
        /// for creating all of the commands in a given plug-in.
        /// The base class implementation Constructs an instance
        /// of every publicly exported command class in your plug-in's assembly.
        /// </summary>
        protected override void CreateCommands()
        {
            base.CreateCommands();
        }

        /// <summary>
        /// Write a line to the Rhino command log window
        /// </summary>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="formatArgs"></param>
        public static void WriteLine(LogCategory category, string message, params object[] formatArgs)
        {
            string prefix;
            switch (category)
            {
                case LogCategory.Warning:
                    {
                        prefix = "[IDS::Warning] ";
                        break;
                    }
                case LogCategory.Error:
                    {
                        prefix = "[IDS::Error] ";
                        break;
                    }
                case LogCategory.Diagnostic:
                    {
                        prefix = "[IDS::Diagnostics] ";
                        break;
                    }
                default:
                    {
                        prefix = "[IDS] ";
                        break;
                    }
            }

            // Write line to Rhino log/command window
            var line = string.Format(prefix + message, formatArgs);
            RhinoApp.WriteLine(line);
        }

        public void Dispose()
        {
            _formAbout?.Dispose();
        }
    }
}