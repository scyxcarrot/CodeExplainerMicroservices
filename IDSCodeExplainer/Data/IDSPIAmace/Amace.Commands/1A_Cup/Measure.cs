using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Drawing;
using System.Linq;


namespace IDS.Amace.Commands
{
    [System.Runtime.InteropServices.Guid("0c490dc2-7166-4489-a11e-db76f7f09fcd")]
    [IDSCommandAttributes(true, DesignPhase.Cup, IBB.Cup)]
    public class Measure : CommandBase<ImplantDirector>
    {
        /// <summary>
        /// The measurement conduit
        /// </summary>
        public static CupPositionConduit MeasurementConduit
        {
            get
            {
                return Proxies.Measure.MeasurementConduit;
            }
            private set
            {
                Proxies.Measure.MeasurementConduit = value;
            }
        }

        public Measure()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
        }

        /** The one and only instance of this command */

        public static Measure TheCommand { get; private set; }

        /** The command name as it appears on the Rhino command line */

        public override string EnglishName => "Measure";

        /**
         * Run the command.
         *
         * The commands shows the contralateral HJC/COR as a sphere and
         * displays the lateralization using measurement arrows and
         * a numerical label on-screen. The user can show and hide
         * the visualizations.
         */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Measurement was on, so disable it
            if (MeasurementConduit != null && MeasurementConduit.Enabled)
            {
                DisableMeasurement(doc);
            }
            // Measurement was off, so enable it
            else if (MeasurementConduit == null || !MeasurementConduit.Enabled)
            {
                EnableMeasurement(director, doc);
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Nothing needs to be done.");
                return Result.Success;
            }

            // Success
            return Result.Success;
        }

        private static void DisableMeasurement(RhinoDoc doc)
        {
            // Disable conduit
            MeasurementConduit.Enabled = false;
            // Set back to perspective projection
            var viewPerspective = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Perspective"];
            doc.Views.ActiveView = viewPerspective;
            doc.Views.ActiveView.ActiveViewport.ChangeToParallelProjection(true);
            // Set acetabular view
            View.SetCupAcetabularView(doc);
            // Set visualization
            var cupPanel = CupPanel.GetPanel();
            HandleDisabledMeasurementVisibility(cupPanel, doc);
        }

        private static void HandleDisabledMeasurementVisibility(CupPanel cupPanel, RhinoDoc doc)
        {
            if (cupPanel == null)
            {
                Visibility.CupDefault(doc);
            }
            else if (cupPanel.chkRbvPreview.Checked)
            {
                Visibility.CupRbvPreview(doc);
            }
            else
            {
                Visibility.CupDefault(doc);
            }
        }

        private static void EnableMeasurement(ImplantDirector director, RhinoDoc doc)
        {
            // Get cup data
            var cup = director.cup;

            var objectManager = new AmaceObjectManager(director);

            // Get relevant meshes
            var def = objectManager.GetBuildingBlock(IBB.DefectPelvis).Geometry as Mesh;
            Mesh clat = null;
            if (objectManager.HasBuildingBlock(IBB.ContralateralPelvis))
            {
                clat = objectManager.GetBuildingBlock(IBB.ContralateralPelvis).Geometry as Mesh;
            }
            Mesh sacrum = null;
            if (objectManager.HasBuildingBlock(IBB.Sacrum))
            {
                sacrum = objectManager.GetBuildingBlock(IBB.Sacrum).Geometry as Mesh;
            }

            // Replace the conduit by a new one, to make sure it is based on the latest cup position
            MeasurementConduit = new CupPositionConduit(cup, director.CenterOfRotationContralateralFemur, director.CenterOfRotationDefectFemur, Color.Black, def, clat, sacrum, true, true, true);
            MeasurementConduit.Enabled = true;
            // Set view to front and parallel projection
            var viewFront = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)["Front"];
            doc.Views.ActiveView = viewFront;
            doc.Views.ActiveView.ActiveViewport.ChangeToParallelProjection(true);
            // Set anterior view
            View.SetPcsAnteriorView(doc);
            // Set visualization
            var cupPanel = CupPanel.GetPanel();
            HandleEnabledMeasurementVisibility(cupPanel, doc);
        }

        private static void HandleEnabledMeasurementVisibility(CupPanel cupPanel, RhinoDoc doc)
        {
            if (cupPanel == null)
            {
                Visibility.CupContralateralMeasurement(doc);
            }
            else if (cupPanel.chkRbvPreview.Checked)
            {
                Visibility.CupContralateralMeasurementRbvPreview(doc);
            }
            else
            {
                Visibility.CupContralateralMeasurement(doc);
            }
        }
    }
}