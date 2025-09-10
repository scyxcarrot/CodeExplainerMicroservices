using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Operations;
using IDS.Common;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace IDS.NonProduction.Commands
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("B1113F73-8941-401B-80BE-4B6C52588043")]
    [IDSCommandAttributes(true, DesignPhase.Any, IBB.PlateFlat)]
    public class AMace_TestExportTransitions : Command
    {
        public AMace_TestExportTransitions()
        {
            Instance = this;
        }

        public static AMace_TestExportTransitions Instance { get; private set; }

        public override string EnglishName => "AMace_TestExportTransitions";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            if (director == null || !director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            var dialog = new FolderBrowserDialog
            {
                Description = "Select Destination to Export Transitions"
            };

            if (dialog.ShowDialog() != DialogResult.OK)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Aborted.");
                return Result.Failure;
            }
            
            var objectManager = new AmaceObjectManager(director);
            if (objectManager.HasBuildingBlock(IBB.PlateSmoothHoles))
            {
                objectManager.DeleteObject(objectManager.GetBuildingBlockId(IBB.PlateSmoothHoles));
            }

            var qcApprovedPlateCreated = PlateMaker.CreateQcApprovedPlate(director);
            if (!qcApprovedPlateCreated)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Error while creating plate.");
                return Result.Failure;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);

            var smoothPlateHoles = (Mesh)objectManager.GetBuildingBlock(IBB.PlateSmoothHoles).Geometry;
            ExportMesh(folderPath, BuildingBlocks.Blocks[IBB.PlateSmoothHoles].ExportName, smoothPlateHoles, Color.Gray);

            // Export Acetabular plane
            CupExporter.ExportAcetabularPlane(folderPath, director);

            var cup = director.cup;
            const double offset = 10.0;
            var radius = cup.cupType.CupThickness + (cup.innerCupDiameter / 2) + offset;
            var cupOffset = new Sphere(cup.centerOfRotation, radius);
            ExportMesh(folderPath, "CupOffset", Mesh.CreateFromSphere(cupOffset, 150, 150), Color.Red);

            RhinoApp.WriteLine("Transitions exported to the following folder:");
            RhinoApp.WriteLine("{0}", folderPath);
            return Result.Success;
        }

        private void ExportMesh(string exportDir, string name, Mesh mesh, Color color)
        {
            var filePath = $"{exportDir}\\{name}.stl";
            var meshColor = new int[3] { color.R, color.G, color.B };
            StlUtilities.RhinoMesh2StlBinary(mesh, filePath, meshColor);
        }
    }

#endif
}
