namespace RhinoMatSDKOperations
{
    ///<summary>
    /// Every RhinoCommon Plug-In must have one and only one PlugIn derived
    /// class. DO NOT create an instance of this class. It is the responsibility
    /// of Rhino to create an instance of this class.
    ///</summary>
    public class MDCKRhinoPlugin : Rhino.PlugIns.PlugIn
    {
        static MDCKRhinoPlugin m_theplugin;

        public MDCKRhinoPlugin()
        {
            m_theplugin = this;
        }

        public static MDCKRhinoPlugin ThePlugIn
        {
            get { return m_theplugin; }
        }

    }


}
