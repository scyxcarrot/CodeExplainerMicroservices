#if DEBUG

using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.Amace.Commands
{
    /**
     * Rhino Command to make an undercut entity
     */

    [System.Runtime.InteropServices.Guid("26FF2C65-C3A6-4C05-B009-78EB1712D022")]
    public class MakeUndercutEntity : Command
    {
        private static MakeUndercutEntity m_thecommand;

        public MakeUndercutEntity()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MakeUndercutEntity TheCommand => m_thecommand;

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "MakeUndercutEntity";

        protected override Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Check input data
            ImplantDirector director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            return Proxies.MakeUndercutEntity.RunCommand(doc);
        }
    }
}

#endif