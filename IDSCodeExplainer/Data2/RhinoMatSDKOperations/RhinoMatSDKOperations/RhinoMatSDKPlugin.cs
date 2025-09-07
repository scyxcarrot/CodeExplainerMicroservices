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
using System.Windows.Forms;

namespace RhinoMatSdkOperations
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
    public class RhinoMatSDKPlugin : Rhino.PlugIns.PlugIn
    {
        /// <summary>
        /// The one and only instance of the plugin
        /// </summary>
        private static RhinoMatSDKPlugin sharedInstance;
        /// <summary>
        /// The Constructor assigns the newly created instance (this) to the "Instance" attribute
        /// </summary>
        public RhinoMatSDKPlugin()
        {
            // Make the plugin object accessible for all
            sharedInstance = this;
        }

        /// <summary>
        /// This method is called by RhinoApp.GetPlugInObject(guid) to get the plugin
        /// </summary>
        /// <returns></returns>
        public override object GetPlugInObject()
        {
            return sharedInstance;
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

            Rhino.RhinoApp.WriteLine("Rhino MatSDK plugin successfully loaded");
            
            return Rhino.PlugIns.LoadReturnCode.Success;
        }

        /// <summary>
        /// Override this property to load the plugin at startup (instead of when needed)
        /// </summary>
        public override Rhino.PlugIns.PlugInLoadTime LoadTime
        {
            get
            {
                return Rhino.PlugIns.PlugInLoadTime.AtStartup;
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
        
    }
}