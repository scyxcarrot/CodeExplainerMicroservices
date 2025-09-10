using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;

namespace IDS.Glenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("1f8076f3-8f20-4dca-8792-b3fa2e89025a")]
    public class DisplayGleniusDependencyGraph : Command
    {
        static DisplayGleniusDependencyGraph _instance;
        public DisplayGleniusDependencyGraph()
        {
            _instance = this;
        }

        ///<summary>The only instance of the DisplayGleniusDependencyGraph command.</summary>
        public static DisplayGleniusDependencyGraph Instance => _instance;
        public override string EnglishName => "DisplayGleniusDependencyGraph";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);

            var infos = director.Graph.GetGraphIBBInfo();
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "DISPLAYING CURRENT GRAPH INFO:: TOTAL OF " + infos.Count + " NODES");
            infos.ForEach(x => IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "DEPENDENCY GRAPH CONTAINS NODE:: " + x));

            return Result.Success;
        }
    }

#endif
}
