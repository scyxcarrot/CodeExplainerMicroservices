using IDS.Core.Enumerators;
using Rhino;
using Rhino.DocObjects;

namespace IDS.Testing
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
    public class IDSAmaceTestsPlugin : Rhino.PlugIns.PlugIn
    {
        /// <summary>
        /// The one and only instance of the plugin
        /// </summary>
        private static IDSAmaceTestsPlugin _sharedInstance;

        /// <summary>
        /// The Constructor assigns the newly created instance (this) to the "Instance" attribute
        /// </summary>
        public IDSAmaceTestsPlugin()
        {
            // Make the plugin object accessible for all
            _sharedInstance = this;
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
        /// Replace a RhinoObject in the document
        /// </summary>
        /// <param name="oldObj"></param>
        /// <param name="newObj"></param>
        /// <returns></returns>
        public static bool ReplaceRhinoObject(RhinoObject oldObj, RhinoObject newObj)
        {
            // Replace the old cup
            var oldRef = new ObjRef(oldObj);
            var replaced = oldObj.Document.Objects.Replace(oldRef, newObj);
            return replaced;
        }

        /// <summary>
        /// Is called when the plug-in is being loaded.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        protected override Rhino.PlugIns.LoadReturnCode OnLoad(ref string errorMessage)
        {
            // Suppress server busy messages
            Rhino.Runtime.HostUtils.DisplayOleAlerts(false);

            WriteLine(LogCategory.Default, "IDS Tests Plugin successfully loaded");
            
            return Rhino.PlugIns.LoadReturnCode.Success;
        }

        /// <summary>
        /// Override this property to load the plugin at startup (instead of when needed)
        /// </summary>
        public override Rhino.PlugIns.PlugInLoadTime LoadTime => Rhino.PlugIns.PlugInLoadTime.AtStartup;

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
                    prefix = "[IDS::Warning] ";
                    break;

                case LogCategory.Error:
                    prefix = "[IDS::Error] ";
                    break;

                case LogCategory.Diagnostic:
                    prefix = "[IDS::Diagnostics] ";
                    break;

                default:
                    prefix = "[IDS] ";
                    break;
            }

            // Write line to Rhino log/command window
            var line = string.Format(prefix + message, formatArgs);
            RhinoApp.WriteLine(line);
        }
    }
}