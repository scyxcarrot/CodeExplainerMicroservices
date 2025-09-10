#if (INTERNAL)

using IDS.Amace;
using IDS.Core.NonProduction;
using Rhino;
using Rhino.Commands;
using System.Drawing;
using System.Linq;

namespace IDS.NonProduction.Commands
{
    [System.Runtime.InteropServices.Guid("05b1a333-a30f-463f-af58-9ede3e146f98")]
    public class AMace_TestCreateScrewPoints : Command
    {
        static AMace_TestCreateScrewPoints _instance;
        public AMace_TestCreateScrewPoints()
        {
            _instance = this;
        }

        ///<summary>The only instance of the AMace_TestCreateScrewPoints command.</summary>
        public static AMace_TestCreateScrewPoints Instance => _instance;

        public override string EnglishName => "AMace_TestCreateScrewPoints";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var screwManager = new ScrewManager(doc);
            var screws = screwManager.GetAllScrews().ToList();

            foreach (var s in screws)
            {
                InternalUtilities.AddPoint(s.HeadPoint, "Head Point", $"Testing::Screws::Screw {s.Index} of {s.screwBrandType}", Color.Blue);
                InternalUtilities.AddPoint(s.HeadCalibrationPoint, "Head Calibration Point", $"Testing::Screws::Screw {s.Index} of {s.screwBrandType}", Color.Red);
                InternalUtilities.AddPoint(s.TipPoint, "Tip Point", $"Testing::Screws::Screw {s.Index} of {s.screwBrandType}", Color.Green);
                InternalUtilities.AddPoint(s.BodyOrigin, "Body Origin Point", $"Testing::Screws::Screw {s.Index} of {s.screwBrandType}", Color.Magenta);

                var resetDebugMode = false;
                if (!ImplantDirector.IsDebugMode)
                {
                    resetDebugMode = true;
                    ImplantDirector.IsDebugMode = true;
                }

                var distInBone = s.GetDistanceInBone(); //This will create the intersection points.
                var distUntilBone = s.GetDistanceUntilBone(); //This will create the intersection points.

                if (resetDebugMode)
                {
                    ImplantDirector.IsDebugMode = false;
                }
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}

#endif