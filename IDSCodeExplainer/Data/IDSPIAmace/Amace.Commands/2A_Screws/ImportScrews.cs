using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Importers;
using IDS.Amace.Proxies;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.GUI;
using IDS.Core.Importer;
using IDS.Core.Operations;
using Rhino;
using Rhino.Commands;
using System;
using System.Linq;
using System.Windows.Forms;

namespace IDS.Amace.Commands
{
    /**
     * Rhino Command to ...
     */

    [System.Runtime.InteropServices.Guid("75E7E2FD-576F-4E2E-8E59-25DFDFB2348B")]
    [IDSCommandAttributes(true, DesignPhase.Screws, IBB.Cup, IBB.WrapBottom, IBB.WrapTop, IBB.WrapSunkScrew)]
    public class ImportScrews : CommandBase<ImplantDirector>
    {
        public ImportScrews()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        ///<summary>The one and only instance of this command</summary>
        public static ImportScrews TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ImportScrews";

        private readonly Dependencies _dependencies;

        /**
        * RunCommand does .... as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {

            // Import file
            var fd = new Rhino.UI.OpenFileDialog
            {
                Title = "Please select xml file with screw cylinders.",
                Filter = "XML files (*.xml)|*.xml||",
                InitialDirectory = Environment.SpecialFolder.Desktop.ToString()
            };
            var drc = fd.ShowDialog();
            if (drc != DialogResult.OK)
            {
                RhinoApp.WriteLine("Invalid file. Aborting.");
                return Result.Failure;
            }
            var xmlFile = fd.FileName;

            // Loader
            var waitbar = new frmWaitbar();
            waitbar.Title = "Importing screws...";

            try
            {
                waitbar.Show();

                // read xml
                var screwList = GenericScrewImportExport.ReadScrewXml<Screw>(xmlFile, doc, new ReadAMaceScrewXmlComponent());

                // Delete old screws
                var screwManager = new ScrewManager(director.Document);
                var oldScrewList = screwManager.GetAllScrews().ToList();
                foreach (var thisScrew in oldScrewList)
                {
                    thisScrew.Delete();
                }

                waitbar.Increment(20);

                // Add new screws
                foreach (var screw in screwList)
                {
                    // Calibrate
                    screw.CalibrateHeadAndTipGlobally();
                    screw.CalibrateScrewHead();
                    screw.Update(); // adds the screw to the document

                    waitbar.Increment((int)Math.Ceiling(80.0 / screwList.Count));
                }

                // Delete dependencies
                _dependencies.DeleteBlockDependencies(director, IBB.Screw);

                // Lock everything again
                Locking.LockAll(director.Document);

                // Update screw panel
                var screwPanel = ScrewPanel.GetPanel();
                screwPanel?.RefreshPanelInfo();

                // visualisation
                Visibility.ScrewDefault(doc);

                // Update QC conduit
                if (ScrewInfo.Numbers != null)
                {
                    ScrewInfo.Update(doc, false);
                }

                // Close waitbar
                waitbar.Close();
                return Result.Success;
            }
            catch
            {
                // Close waitbar
                waitbar.ReportError("Could not import (all) screws.");
                return Result.Failure;
            }
        }
    }
}