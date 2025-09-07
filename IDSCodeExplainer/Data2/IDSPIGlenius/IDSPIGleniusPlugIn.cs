using IDS.Common;
using IDS.Common.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using IDS.Core.Utilities;
using IDS.Glenius;
using IDS.Glenius.FileSystem;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.PlugIns;
using Rhino.Runtime;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using PlugInInfo = IDS.Glenius.PlugInInfo;

//TODO: TechDebt 540138
namespace IDSPIGlenius
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class IDSPIGleniusPlugIn : PlugIn, IDisposable
    {
        /// <summary>
        /// IDS main version, leave as is, version check based on this variable is not validated
        /// </summary>
        private const int MajorVersion = 1;

        /// <summary>
        /// IDS subversion, leave as is, version check based on this variable is not validated
        /// </summary>
        private const int MinorVersion = 2;

        /// <summary>
        /// The one and only instance of the plugin
        /// </summary>
        private static IDSPIGleniusPlugIn _sharedInstance;
        public static IDSPIGleniusPlugIn SharedInstance => _sharedInstance;

        private readonly ErrorLogger _errorLogger;

        /// <summary>
        /// Indicates whether a document is being opened
        /// </summary>
        private bool _isOpeningDocument;

        public string FileName { get; private set; }
        public int CaseVersion { get; set; }
        public int CaseDraft { get; set; }

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

        public static bool IsGlenius { get; set; }

        private readonly IDSPanel _panels;

        /// <summary>
        /// The form about
        /// </summary>
        private frmAbout _formAbout;

        /// <summary>
        /// Opens the about window.
        /// </summary>
        private void OpenAboutWindow()
        {
            Thread t = new Thread(new ThreadStart(AboutWindowThread));
            t.Start();
            t.Join(1000); // Show it for one second
        }

        /// <summary>
        /// Abouts the window thread.
        /// </summary>
        private void AboutWindowThread()
        {
            _formAbout = new frmAbout(IDS.Glenius.PlugInInfo.PluginModel);
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

        public IDSPIGleniusPlugIn()
        {
            //OpenAboutWindow();

            // Initialize variables
            IDSPluginHelper.CloseAfterCommand = false;
            ScriptMode = false;
            _panels = new IDSPanel(this);
            _errorLogger = new ErrorLogger();

            IsGlenius = false;
            SubscribeEvents();

            // Make the plugin object accessible for all
            _sharedInstance = this;

            //CloseAboutWindow();
            CaseVersion = -1;
            CaseDraft = -1;
        }

        private void SubscribeEvents()
        {
            // Register event handlers
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
            IDSPluginHelper.AddUserDataEvent += AddUserDataEvent;
        }

        private void AddUserDataEvent(ImplantBuildingBlock block, GeometryBase geom, ObjectAttributes oa)
        {
            if (!IsGlenius)
            {
                return;
            }

            var ud = new IBBUserData(block, geom);
            oa.UserData.Add(ud);
        }

        /// <summary>
        /// Executed when something is changed in the layer panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnLayerEvent(object sender, LayerTableEventArgs e)
        {

        }

        /// <summary>
        /// Executed when an object is deleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDelete(object sender, RhinoObjectEventArgs e)
        {
            if (!IsGlenius)
            {
                return;
            }

            if (_isOpeningDocument)
            {
                return;
            }
        }

        /// <summary>
        /// Executed when an object is undeleted
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUndelete(object sender, RhinoObjectEventArgs e)
        {
            if (!IsGlenius)
            {
                return;
            }

            var director = GetDirector(e.TheObject.Document.DocumentId);
            director?.SetVisibilityByPhase();
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
            if (IsGlenius)
            {
                IDSPluginHelper.WriteLine(LogCategory.Default,
                    $"*** Executing {e.CommandEnglishName.ToUpper()} command ***");
            }

            // Close rhino if the flag was set
            if (!IDSPluginHelper.CloseAfterCommand || e.CommandPluginName !=
                Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title)
            {
                return;
            }

            Dialogs.ShowMessageBox("You tried to run an IDS Glenius command in a file that is not supposed to be edited. Rhino will close.",
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
            if (!IsGlenius)
            {
                return;
            }

            // Lock all objects
            if (e.CommandPluginName == Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>().Title)
            {
                Locking.LockAll(RhinoDoc.ActiveDoc);
            }

            // Report command result
            IDSPluginHelper.WriteLine(LogCategory.Default,
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
        /// Set the director for the given document.
        /// </summary>
        /// <param name="docId"></param>
        /// <param name="director"></param>
        public static void SetDirector(int docId, IImplantDirector director)
        {
            IDSPluginHelper.SetDirector(docId, director);
        }

        /// <summary>
        /// Get the director for the given document.
        /// </summary>
        /// <param name="docId"></param>
        /// <returns></returns>
        public static T GetDirector<T>(int docId) where T : class, IImplantDirector
        {
            return IDSPluginHelper.GetDirector<T>(docId);
        }

        public static IImplantDirector GetDirector(int docId)
        {
            return IDSPluginHelper.GetDirector(docId);
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
        /// Is called when the plug-in is being loaded.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            // Register panels
            _panels.RegisterIDSPanels();

            // Suppress server busy messages
            HostUtils.DisplayOleAlerts(false);

            IDSPluginHelper.WriteLine(LogCategory.Default, "Implant Design Suite (IDS) Glenius successfully loaded");

            IDSPluginHelper.PluginVersion = Version;
            return LoadReturnCode.Success;
        }

        /// <summary>
        /// Called when a new document is created
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNewDocument(object sender, DocumentEventArgs e)
        {
            FileName = string.Empty;
            CaseVersion = -1;
            CaseDraft = -1;
        }

        /// <summary>
        /// Called first when a document is opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBeginOpenDocument(object sender, DocumentOpenEventArgs e)
        {
            IsGlenius = false;
            _isOpeningDocument = true;
            FileName = Path.GetFileName(e.FileName);
        }

        /// <summary>
        /// Called after OnBeginOpenDocument
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEndOpenDocument(object sender, DocumentOpenEventArgs e)
        {
            if (!IsGlenius)
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
            var director = GetDirector(e.DocumentId);
            director?.RestoreCustomRhinoObjects(e.Document);

            if (director != null)
            {
                CaseVersion = director.version;
                CaseDraft = director.draft;
            }

            if (director == null || string.IsNullOrEmpty(director.caseId) || ScriptMode)
            {
                return;
            }

            // Do version check
            var currentDirParts =
                e.FileName.Split(new [] {"\\"}, StringSplitOptions.None).ToList();
            currentDirParts.RemoveAt(currentDirParts.Count - 1);
            var logfile = string.Join("\\", currentDirParts) + "\\" + director.caseId + "_versioncheck.log";
            VersionControl.DoVersionCheck(director, false, false, logfile, IDS.Glenius.PlugInInfo.PluginModel);

            var screwQCfolder = string.Format("2_Draft{0:D}_ScrewQC", director.draft);
            var scaffoldQCfolder = string.Format("2_Draft{0:D}_ScaffoldQC", director.draft);

            DirectoryStructure.CheckWorkFileLocation(director, e.FileName,
                new List<string>() {screwQCfolder, scaffoldQCfolder, "Work"},
                director.InputFiles.Select(Path.GetFileName).ToList(),
                new List<string>() {"3dm", "mat"},
                new List<string>() {screwQCfolder, scaffoldQCfolder});

            director.FileName = FileName;
        }

        /// <summary>
        /// Called when the views have been initialized
        /// Called after OnEndOpenDocument
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnInitialView(object sender, DocumentOpenEventArgs e)
        {
            if (!IsGlenius)
            {
                return;
            }

            var director = IDSPluginHelper.GetDirector(e.DocumentId);
            if (director == null)
            {
                return;
            }

            if (!_isOpeningDocument)
            {
                return;
            }

            // Unset flag
            _isOpeningDocument = false;

            // Skip when stp file is imported into the current document
            if (e.Merge)
            {
                return;
            }


            director.OnInitialView(e.Document);
        }

        /// <summary>
        /// Called when a document is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCloseDocument(object sender, DocumentEventArgs e)
        {
            if (!IsGlenius)
            {
                return;
            }

            // Destruct the associated director
            SetDirector(e.DocumentId, null); // Discard last reference
            IsGlenius = false;
        }

        /// <summary>
        /// Prepare objects for archiving.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBeginSaveDocument(object sender, DocumentSaveEventArgs e)
        {
            if (!IsGlenius)
            {
                return;
            }

            // Archive objects
            var director = GetDirector(e.DocumentId);
            director?.PrepareObjectsForArchive();
        }

        /// <summary>
        /// Perform actions after document has been saved/autosaved.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnEndSaveDocument(object sender, DocumentSaveEventArgs e)
        {
            _errorLogger.FlushErrorLog();
        }

        /// <summary>
        /// Called whenever a Rhino is about to save a .3dm file.
        /// If you want to save plug-in document data when a model is saved in
        /// a version 5 .3dm file, then you must override this function to
        /// return true and you must override WriteDocument().
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        protected override bool ShouldCallWriteDocument(FileWriteOptions options)
        {
            return !options.WriteGeometryOnly && !options.WriteSelectedObjectsOnly && IsGlenius;
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
        protected override void WriteDocument(RhinoDoc doc, BinaryArchiveWriter archive, FileWriteOptions options)
        {
            if (!IsGlenius)
            {
                return;
            }
            // 1. Write the version for this chunk
            /// \todo Remove majorVersion/minorVersion version check
            archive.Write3dmChunkVersion(MajorVersion, MinorVersion);

            // Serialize the director
            var director = GetDirector(doc.DocumentId);

            // 2. Write the ImplantDirector
            director?.WriteToArchive(archive); // WARNING: don't use same keys as in this class!
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
        protected override void ReadDocument(RhinoDoc doc, BinaryArchiveReader archive, FileReadOptions options)
        {
            if (!_isOpeningDocument)
            {
                return;
            }

            // Check plugin version
            /// \todo Remove major, minor version
            int major, minor;
            archive.Read3dmChunkVersion(out major, out minor);
            if (MajorVersion < major || (MajorVersion == major && MinorVersion < minor))
            {
                RhinoApp.Write($"Incompatible IDS Glenius Plugin version: {major}.{minor}");
                return;
            }

            /// \todo If reading a new document: replace the current director if there is one, if merging/importing: preserve the current inspector
            if (options.OpenMode || options.NewMode)
            {
                try
                {
                    var dict = archive.ReadDictionary();
                    if (dict.Name != "GleniusImplantDirector")
                    {
                        return;
                    }

                    IsGlenius = true; 
                    var director = new GleniusImplantDirector(doc, archive, dict, major, minor, IDS.Glenius.PlugInInfo.PluginModel);

                    Msai.Terminate(PlugInInfo.PluginModel, FileName, CaseVersion, CaseDraft);
                    Msai.Initialize(PlugInInfo.PluginModel, FileName, director.version, director.draft);
                    CaseVersion = director.version;
                    CaseDraft = director.draft;
                    // Associate director with this document
                    SetDirector(doc.DocumentId, director);
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
        public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

        /// <summary>
        /// Called on Rhino shutdown.
        /// </summary>
        protected override void OnShutdown()
        {
            base.OnShutdown();
            if (IsGlenius)
            {
                _errorLogger.FlushErrorLog();
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

        public void Dispose()
        {
            _formAbout?.Dispose();
        }
    }
}