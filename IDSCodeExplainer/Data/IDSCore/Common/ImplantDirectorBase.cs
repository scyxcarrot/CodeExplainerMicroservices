using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.SplashScreen;
using IDS.Core.V2.TreeDb.Model;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.ImplantDirector
{
    public abstract class ImplantDirectorBase : IImplantDirector
    {
        public IPluginInfoModel PluginInfoModel { get; set; }

        /// <summary>
        /// The archive version
        /// </summary>
        protected const int ArchiveVersion = 1;

        /// <summary>
        /// The document type key
        /// </summary>
        protected const string KeyDocType = "document_type";

        /// <summary>
        /// The draft key
        /// </summary>
        protected const string KeyDraft = "draft";

        /// <summary>
        /// The input file key
        /// </summary>
        protected const string KeyInputFile = "input_fiie";

        /// <summary>
        /// The phase key
        /// </summary>
        protected const string KeyPhase = "design_phase";

        /// <summary>
        /// The version key
        /// </summary>
        protected const string KeyVersion = "version";

        protected const string KeyIsForTesting = "IsForUserTesting";

        /// <summary>
        /// The document type
        /// </summary>
        public DocumentType documentType { get; set; }

        public delegate void UnSubscribeCallbackDelegate();

        public UnSubscribeCallbackDelegate OnUnsubscribeCallback { get; set; }
        
        public bool IsTestingMode { get; set; }

        public string FileName { get; set; } //Set it when plugin even document open ends!

        public bool IsForUserTesting { get; set; } = false;

        protected ImplantDirectorBase(RhinoDoc doc, IPluginInfoModel pluginInfoModel, bool subscribeToEvents=true)
        {
            documentType = DocumentType.Work;
            ComponentVersions = new Dictionary<string, Dictionary<string, string>>();
            ComponentDateTimes = new Dictionary<string, Dictionary<string, DateTime>>();

            // The document where this director manages objects
            Document = doc;

            PluginInfoModel = pluginInfoModel;

            // Init
            version = 0;
            draft = 0;
            InputFiles = new List<string>();

            // Subscribe to Document events
            if (subscribeToEvents)
            {
                SubscribeCallbacks();
            }
        }

        protected ImplantDirectorBase(RhinoDoc doc, Rhino.FileIO.BinaryArchiveReader archive, ArchivableDictionary dict, 
            int pluginMajorVersion, int pluginMinorVersion, IPluginInfoModel pluginInfoModel, bool subscribeToEvents=true) : this(doc, pluginInfoModel, subscribeToEvents)
        {
            if (dict.Version > ArchiveVersion)
            {
                throw new NotImplementedException("The archive version for the ImplantDirector is larger than the one supported.");
            }

            RestoreArchive(dict);
        }

        protected virtual void RestoreArchive(ArchivableDictionary dict)
        {
            RestoreSoftwareVersions(dict);
            RestoreTraceabilityInformation(dict);
        }

        protected void RestoreSoftwareVersions(ArchivableDictionary dict)
        {            
            // (Git) versions & dates
            Dictionary<string, Dictionary<string, DateTime>> dummyObject;
            Dictionary<string, Dictionary<string, string>> comps = VersionControl.GetVersionDictionaries(out dummyObject);
            Dictionary<string, Dictionary<string, string>> componentVersionsTemp = new Dictionary<string, Dictionary<string, string>>();
            Dictionary<string, Dictionary<string, DateTime>> componentDateTimesTemp = new Dictionary<string, Dictionary<string, DateTime>>();
            foreach (string componentName in comps.Keys)
            {
                Dictionary<string, string> versions = new Dictionary<string, string>();
                Dictionary<string, DateTime> datetimes = new Dictionary<string, DateTime>();
                foreach (string versiontype in new List<string>() { "commit", "build" })
                {
                    if (!comps[componentName].ContainsKey(versiontype))
                    {
                        continue;
                    }

                    try
                    {
                        versions.Add(versiontype, dict.GetString(componentName + "_" + versiontype + "_version"));
                    }
                    catch { } // do nothing 
                    try
                    {
                        datetimes.Add(versiontype, DateTime.ParseExact(dict.GetString(componentName + "_" + versiontype + "_datetime", "").Substring(0, 10), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch { } // do nothing 
                }
                // Write to temporary dictionary (to avoid having to check if a key exists)
                componentVersionsTemp.Add(componentName, versions);
                componentDateTimesTemp.Add(componentName, datetimes);
            }
            // Copy temporary to actual dictionary
            ComponentVersions = componentVersionsTemp;
            ComponentDateTimes = componentDateTimesTemp;
        }

        protected virtual void RestoreTraceabilityInformation(ArchivableDictionary dict)
        {
            // Draft / version / input file
            draft = dict.Getint(KeyDraft, 0);
            version = dict.Getint(KeyVersion, 0);
            var files = dict[KeyInputFile] as string[];
            if (files == null)
            {
                InputFiles = new List<string> { dict.GetString(KeyInputFile, "") };
            }
            else
            {
                InputFiles = files.ToList();
            }

            // Document Type
            DocumentType storedDocType;
            bool rc = dict.TryGetEnumValue<DocumentType>(KeyDocType, out storedDocType);
            if (rc)
            {
                documentType = storedDocType;
            }
        }

        /// <summary>
        /// Gets the component date times.
        /// </summary>
        /// <value>
        /// The component date times.
        /// </value>
        public Dictionary<string, Dictionary<string, DateTime>> ComponentDateTimes { get; private set; }
        /// <summary>
        /// Gets the component versions.
        /// </summary>
        /// <value>
        /// The component versions.
        /// </value>
        public Dictionary<string, Dictionary<string, string>> ComponentVersions { get; private set; }

        public virtual DesignPhaseProperty CurrentDesignPhaseProperty { get; }

        /// <summary>
        /// Gets a value indicating whether [defect is left].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [defect is left]; otherwise, <c>false</c>.
        /// </value>
        public virtual bool defectIsLeft { get; }

        /// <summary>
        /// Gets the document.
        /// </summary>
        /// <value>
        /// The document.
        /// </value>
        public RhinoDoc Document { get; protected set; }

        public IDSDocument IdsDocument { get; protected set; }

        /// <summary>
        /// Gets or sets the draft.
        /// </summary>
        /// <value>
        /// The draft.
        /// </value>
        public int draft { get; set; }
        /// <summary>
        /// Gets or sets the input file.
        /// </summary>
        /// <value>
        /// The input file.
        /// </value>
        public List<string> InputFiles { get; set; }

        public virtual string caseId  { get; set; }       

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public int version { get; set; }
        public abstract void EnterDesignPhase(DesignPhaseProperty toPhase);

        public abstract bool IsCommandRunnable(Rhino.Commands.Command command, bool printMessage = false);
        
        /// <summary>
        /// Called when [close document].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DocumentEventArgs"/> instance containing the event data.</param>
        private void OnCloseDocument(object sender, DocumentEventArgs e)
        {
            UnsubscribeCallbacks();
            Document = null;
        }

        public abstract void PrepareObjectsForArchive();
        
        protected RhinoObject[] GetObjectsForArchive()
        {
            // Prepare all custom RhinoObject objects for serialization
            ObjectEnumeratorSettings settings = new Rhino.DocObjects.ObjectEnumeratorSettings();
            settings.HiddenObjects = true;
            RhinoObject[] rhobjs = Document.Objects.FindByFilter(settings);
            return rhobjs;
        }

        /// <summary>
        /// Restores the custom rhino objects.
        /// </summary>
        /// <param name="doc">The document.</param>
        public abstract void RestoreCustomRhinoObjects(RhinoDoc doc);

        /// <summary>
        /// Unsubscribes the callbacks.
        /// </summary>
        public void UnsubscribeCallbacks()
        {
            OnUnsubscribeCallback?.Invoke();
            RhinoDoc.CloseDocument -= this.OnCloseDocument;
        }

        /// <summary>
        /// Updates the component versions.
        /// </summary>
        public void UpdateComponentVersions()
        {
            Dictionary<string, Dictionary<string, DateTime>> componentDateTimesTemp;
            ComponentVersions = VersionControl.GetVersionDictionaries(out componentDateTimesTemp);
            ComponentDateTimes = componentDateTimesTemp;
        }

        /// <summary>
        /// Writes to archive.
        /// </summary>
        /// <param name="archive">The archive.</param>
        public abstract void WriteToArchive(Rhino.FileIO.BinaryArchiveWriter archive);

        protected void WriteToArchive(ArchivableDictionary dict)
        {
            WriteTraceabilityInformationToArchive(dict);
            WriteSoftwareVersionsToArchive(dict);
        }

        private void WriteSoftwareVersionsToArchive(ArchivableDictionary dict)
        {
            foreach (string componentName in ComponentVersions.Keys)
            {
                foreach (string versiontype in ComponentVersions[componentName].Keys)
                {
                    // Version
                    string keyCommit = componentName + "_" + versiontype + "_version";
                    string valVersion = ComponentVersions[componentName][versiontype];
                    dict.Set(keyCommit, valVersion);
                    // Date
                    string keyDate = componentName + "_" + versiontype + "_datetime";
                    string valDate = ComponentDateTimes[componentName][versiontype].ToString("yyyy-MM-dd");
                    dict.Set(keyDate, valDate);
                }
            }
        }

        private void WriteTraceabilityInformationToArchive(ArchivableDictionary dict)
        {            
            dict.SetEnumValue<DocumentType>(KeyDocType, documentType);
            dict.Set(KeyDraft, draft);
            dict.Set(KeyVersion, version);
            dict.Set(KeyInputFile, InputFiles);
        }

        /// <summary>
        /// Subscribes the callbacks.
        /// </summary>
        private void SubscribeCallbacks()
        {
            RhinoDoc.CloseDocument += this.OnCloseDocument;
        }

        public abstract void SetVisibilityByPhase();

        public virtual string CurrentDesignPhaseName { get; }

        public abstract void OnInitialView(RhinoDoc openedDoc);

        public abstract void OnObjectDeleted();
    }
}