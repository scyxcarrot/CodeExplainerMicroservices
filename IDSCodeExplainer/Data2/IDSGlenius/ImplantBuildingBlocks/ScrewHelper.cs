using IDS.Core.PluginHelper;
using Rhino.DocObjects;

namespace IDS.Glenius.ImplantBuildingBlocks
{
    public static class ScrewHelper
    {
        public static Screw CreateFromArchived(RhinoObject other, bool replaceInDoc)
        {
            // Restore the screw object from archive
            Screw restored = new Screw(other, true, true);

            // Replace if necessary
            return ReplaceRhinoObject(replaceInDoc, other, restored) ? restored : null;
        }

        public static ScrewMantle CreateScrewMantleFromArchived(RhinoObject other, bool replaceInDoc)
        {
            var restored = new ScrewMantle(other);
            return ReplaceRhinoObject(replaceInDoc, other, restored) ? restored : null;
        }

        private static bool ReplaceRhinoObject(bool replaceInDoc, RhinoObject other, RhinoObject restored)
        {
            return !replaceInDoc || IDSPluginHelper.ReplaceRhinoObject(other, restored);
        }
    }
}
