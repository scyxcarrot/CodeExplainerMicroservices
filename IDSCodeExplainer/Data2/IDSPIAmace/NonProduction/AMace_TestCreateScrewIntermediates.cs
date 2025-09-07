#if (INTERNAL)

using IDS.Amace;
using IDS.Core.NonProduction;
using Rhino;
using Rhino.Commands;
using System.Linq;

namespace IDS.NonProduction.Commands
{
    [System.Runtime.InteropServices.Guid("c0837314-0dc2-4ee9-9977-7dad2a2ae423")]
    public class AMace_TestCreateScrewIntermediates : Command
    {
        static AMace_TestCreateScrewIntermediates _instance;
        public AMace_TestCreateScrewIntermediates()
        {
            _instance = this;
        }

        ///<summary>The only instance of the AMace_TestCreateScrewIntermediates command.</summary>
        public static AMace_TestCreateScrewIntermediates Instance => _instance;

        public override string EnglishName => "AMace_TestCreateScrewIntermediates";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var screwManager = new ScrewManager(doc);
            var screws = screwManager.GetAllScrews().ToList();

            foreach (var s in screws)
            {
                var collisionMesh = s.GetCalibrationCollisionMesh();
                var targetEntity = s.GetCalibrationTargetEntity(true);

                InternalUtilities.AddObject(collisionMesh, "Collision Mesh", $"Testing::Screws::Screw {s.Index} of {s.screwBrandType}::CalibrationMesh - {s.screwAlignment}");
                InternalUtilities.AddObject(targetEntity, "Target Entity", $"Testing::Screws::Screw {s.Index} of {s.screwBrandType}::CalibrationMesh - {s.screwAlignment}");
                InternalUtilities.AddObject(s.Head, "Head", $"Testing::Screws::Screw {s.Index} of {s.screwBrandType}::CalibrationMesh - {s.screwAlignment}");
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}

#endif