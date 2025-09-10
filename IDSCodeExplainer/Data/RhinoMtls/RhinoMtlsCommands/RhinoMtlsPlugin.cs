using Rhino;

namespace RhinoMtlsCommands
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
    public class RhinoMtlsPlugin : Rhino.PlugIns.PlugIn
    {
        /// <summary>
        /// The one and only instance of the plugin
        /// </summary>
        private static RhinoMtlsPlugin _sharedInstance;
        /// <summary>
        /// The Constructor assigns the newly created instance (this) to the "Instance" attribute
        /// </summary>
        public RhinoMtlsPlugin()
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
        /// Is called when the plug-in is being loaded.
        /// </summary>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        protected override Rhino.PlugIns.LoadReturnCode OnLoad(ref string errorMessage)
        {
            // Suppress server busy messages
            Rhino.Runtime.HostUtils.DisplayOleAlerts(false);

            RhinoApp.WriteLine("RhinoMtlsCommands successfully loaded");
            
            return Rhino.PlugIns.LoadReturnCode.Success;
        }

        /// <summary>
        /// Override this property to load the plugin at startup (instead of when needed)
        /// </summary>
        public override Rhino.PlugIns.PlugInLoadTime LoadTime => Rhino.PlugIns.PlugInLoadTime.AtStartup;
    }
}