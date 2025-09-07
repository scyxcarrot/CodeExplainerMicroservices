using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IDS.Core.PluginHelper
{
    public delegate void AddUserDataEventHandler(ImplantBuildingBlock block, GeometryBase geom, ObjectAttributes oa);

    public static class IDSPluginHelper
    {

        private static CancellationTokenSource _loadIndicatorCancellationTokenSource;
        public static CancellationTokenSource LoadIndicatorCancellationTokenSource
        {
            get => _loadIndicatorCancellationTokenSource;
            set
            {
                // Dispose the old CancellationTokenSource if it exists
                _loadIndicatorCancellationTokenSource?.Dispose();
                _loadIndicatorCancellationTokenSource = value;
            }
        }

        //Rhino.DocObjects.Custom.UserDataList.Add will throws NullException 
        //if the UserData class is not defined in the same assembly as PlugIn class
        public static event AddUserDataEventHandler AddUserDataEvent;

        /// <summary>
        /// Dictionary containing the implant director for each document
        /// Keys: RhinoDoc.Id
        /// Values: ImplantDirector for the RhinoDoc with id equal to key
        /// </summary>
        private static readonly Dictionary<int, IImplantDirector> _directors = new Dictionary<int, IImplantDirector>();

        /// <summary>
        /// Indicates whether executing a command should force Rhino to close.
        /// </summary>
        public static bool CloseAfterCommand { get; set; } = false;

        /// <summary>
        /// Flag to indicate that Rhino is running from a script
        /// </summary>
        public static bool ScriptMode { get; set; } = false;
        
        public static string PluginVersion { get; set; }

        /// <summary>
        /// Set the director for the given document.
        /// </summary>
        /// <param name="docId"></param>
        /// <param name="director"></param>
        public static void SetDirector(int docId, IImplantDirector director)
        {
            IImplantDirector old;
            var rc = _directors.TryGetValue(docId, out old);
            if (rc && old != director)
            {
                old.UnsubscribeCallbacks();
            }
            _directors[docId] = director;
        }

        public static IImplantDirector GetDirector(int docId)
        {
            IImplantDirector director;
            var rc = _directors.TryGetValue(docId, out director);
            return rc ? director : null;
        }

        /// <summary>
        /// Get the director for the given document.
        /// </summary>
        /// <param name="docId"></param>
        /// <returns></returns>
        public static T GetDirector<T>(int docId) where T : class, IImplantDirector
        {
            var director = GetDirector(docId);
            return director as T;
        }

        /// <summary>
        /// Replace a RhinoObject in the document
        /// </summary>
        /// <param name="oldObj"></param>
        /// <param name="newObj"></param>
        /// <returns></returns>
        public static bool ReplaceRhinoObject(RhinoObject oldObj, RhinoObject newObj)
        {
            // Replace the old cup/head
            var oldRef = new ObjRef(oldObj);
            return oldObj.Document.Objects.Replace(oldRef, newObj);
        }

        /// <summary>
        /// Check if a command should be allowed
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public static bool CheckIfCommandIsAllowed(Command command)
        {
            var director = GetDirector(RhinoDoc.ActiveDoc.DocumentId);

            if (CloseAfterCommand)
            {
                // flag raised to block command execution
                return false;
            }

            if (director != null)
            {
                // check by design phase and building blocks
                return director.IsCommandRunnable(command, true);
            }

            // allow by default (e.g. for executing IDSHelp, version check, ... without preop data)
            return true;
        }

        //[PN] move to a separate class under Common namespace
        /// <summary>
        /// Write a line to the Rhino command log window
        /// </summary>
        /// <param name="category"></param>
        /// <param name="message"></param>
        /// <param name="formatArgs"></param>
        public static void WriteLine(LogCategory category, string message, params object[] formatArgs)
        {
            IImplantDirector director = null;
            var trackLog = false;

            string WriteLinePostFix = "";
            if (RhinoDoc.ActiveDoc != null)
            {
                director = GetDirector(RhinoDoc.ActiveDoc.DocumentId);
                if (director != null && director.IsForUserTesting)
                {
                    WriteLinePostFix = "::TEST VERSION. NOT FOR LIVE CASES!";
                }
            }

            string prefix;
            switch (category)
            {
                case LogCategory.Warning:
                    prefix = $"[IDS{WriteLinePostFix}::Warning] ";
                    trackLog = true;
                    break;

                case LogCategory.Error:
                    prefix = $"[IDS{WriteLinePostFix}::Error] ";
                    trackLog = true;
                    break;

                case LogCategory.Diagnostic:
                    prefix = $"[IDS{WriteLinePostFix}::Diagnostics] ";
                    break;

                default:
                    prefix = $"[IDS{WriteLinePostFix}] ";
                    break;
            }

            // Write line to Rhino log/command window
            var line = string.Format(prefix + message, formatArgs);
            RhinoApp.WriteLine(line);

            if (trackLog && director != null && director.PluginInfoModel != null && director.PluginInfoModel.ProductName.Any())
            {
                Msai.TrackException(new IDSException(line), director.PluginInfoModel.ProductName);
            }
        }

        public static void AddUserData(ImplantBuildingBlock block, GeometryBase geom, ObjectAttributes oa)
        {
            AddUserDataEvent?.Invoke(block, geom, oa);
        }

        //returns true if Rhino is currently running a command
        public static bool HandleRhinoIsInCommand()
        {
            if (!Command.InCommand())
            {
                return false;
            }
            WriteLine(LogCategory.Error, "Rhino is currently running a command!");
            return true;
        }
    }
}