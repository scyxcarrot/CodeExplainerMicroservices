using IDS.CMF.Visualization;
using Rhino.DocObjects;

namespace IDS.CMF.Utilities
{
    public static class GuideCreationUtilities
    {
        public static bool IsLeveledBarrelsNotMeetingSpecs(RhinoObject barrelRhObject)
        {
            var barrelMat = barrelRhObject.GetMaterial(true);
            return barrelMat.DiffuseColor == Colors.BarrelLevelingNotMeetingSpecs; //Got set in GuideScrewRegistration
        }
    }

}
