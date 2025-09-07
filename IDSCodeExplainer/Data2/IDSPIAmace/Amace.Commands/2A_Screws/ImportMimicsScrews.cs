using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Proxies;
using IDS.Amace.Relations;
using IDS.Amace.Visualization;
using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.GUI;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Locking = IDS.Core.Operations.Locking;

namespace IDS.Amace.Commands
{
    /**
     * Rhino Command to ...
     */

    [System.Runtime.InteropServices.Guid("4919AC31-C9AE-422B-987C-A25997E3C1C2")]
    [IDSCommandAttributes(true, DesignPhase.Screws, IBB.Cup, IBB.WrapBottom, IBB.WrapTop, IBB.WrapSunkScrew)]
    public class ImportMimicsScrews : CommandBase<ImplantDirector>
    {
        public ImportMimicsScrews()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            TheCommand = this;
            _dependencies = new Dependencies();
        }

        ///<summary>The one and only instance of this command</summary>
        public static ImportMimicsScrews TheCommand { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName => "ImportMimicsScrews";

        private readonly Dependencies _dependencies;

        /**
        * RunCommand does .... as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, ImplantDirector director)
        {
            // Loader
            var waitbar = new frmWaitbar { Title = "Importing screws..." };

            try
            {
                waitbar.Show();

                // Run python command to get Mimics clipboard
                var resources = new Resources();
                var cyloutpath = Path.Combine(Path.GetTempPath(),
                    $"IDS_MimicsCylinders_{Guid.NewGuid().ToString()}.txt");
                var cPythonCommand = $"\"{resources.GetCPythonScriptPath("GetMimicsClipBoard")}\" \"{cyloutpath}\"";

                waitbar.Increment(10);
                var importedFromMimics = ExternalToolInterop.RunCPythonScript(cPythonCommand);
                if (!importedFromMimics)
                {
                    return Result.Failure;
                }

                // here we need to read the data !
                var lines = File.ReadAllLines(cyloutpath);
                // Remove the temp cylinder text file
                File.Delete(cyloutpath);

                // Delete old screws
                var screwManager = new ScrewManager(director.Document);
                var oldScrewList = screwManager.GetAllScrews().ToList();
                foreach (var thisScrew in oldScrewList)
                {
                    thisScrew.Delete();
                }

                waitbar.Increment(10);

                // Add from file
                foreach (var theLine in lines)
                {
                    if (!ProcessLineAndAddScrew(theLine, director))
                    {
                        continue;
                    }

                    waitbar.Increment((int)Math.Ceiling(80.0 / lines.Count()));
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
                waitbar.ReportError("Could not import (all) screws.");
                return Result.Failure;
            }
        }

        private static bool ProcessLineAndAddScrew(string theLine, ImplantDirector director)
        {
            RhinoApp.WriteLine(theLine);
            ScrewBrandType screwBrandType;
            ScrewAlignment screwAlignment;
            Point3d head;
            Point3d tip;
            try
            {
                string screwCaseId;
                if (!ParseScrewLine(theLine, out screwCaseId, out screwAlignment, out screwBrandType, out head, out tip))
                {
                    RhinoApp.WriteLine("[IDS]: Screw import of a screw did not work, check naming in mimics...");
                    return false;
                }
            }
            catch
            {
                RhinoApp.WriteLine("[IDS]: Screw import of a screw did not work, check naming in mimics...");
                return false;
            }
            var screw = new Screw(director, head, tip, screwBrandType, screwAlignment);
            // Figure out if the screw is bicortical or not to decide when to keep its length
            // or when to change it to default positioning
            screw.FixedLength = screw.IsBicortical ? 0.0 : (tip - head).Length;

            // Calibrate
            screw.CalibrateHeadAndTipGlobally();
            screw.CalibrateScrewHead();
            screw.Update(); // adds the screw to the document

            return true;
        }

        private static bool ParseScrewLine(string screwLine, out string caseID, out ScrewAlignment SA, out ScrewBrandType SBT, out Point3d head, out Point3d tip)
        {
            head = new Point3d();
            tip = new Point3d();
            int ind;
            var csv = screwLine.Split(',');
            var nameParts = csv[0].Split('_');

            // Case id
            if (nameParts[0].Equals("Unset"))
            {
                ind = 0;
                caseID = "Unset";
            }
            else
            {
                ind = 1;
                caseID = nameParts[0] + "_" + nameParts[1];
            }

            // Screw type
            SBT = FindScrewBrandType(nameParts[ind + 1] + '_' + nameParts[ind + 2]);

            // Screw alignment
            SA = FindScrewAlignment(nameParts[ind + 3]);

            var provider = new NumberFormatInfo
            {
                NumberDecimalSeparator = ".",
                NumberGroupSeparator = ","
            };

            // Screw head
            head.X = Convert.ToDouble(csv[5], provider);
            head.Y = Convert.ToDouble(csv[6], provider);
            head.Z = Convert.ToDouble(csv[7], provider);

            // Screw tip
            tip.X = Convert.ToDouble(csv[2], provider);
            tip.Y = Convert.ToDouble(csv[3], provider);
            tip.Z = Convert.ToDouble(csv[4], provider);

            return true;
        }

        private static ScrewAlignment FindScrewAlignment(string findString)
        {
            ScrewAlignment screwAlignment;
            try
            {
                screwAlignment = (ScrewAlignment)Enum.Parse(typeof(ScrewAlignment), findString);
            }
            catch
            {
                return ScrewAlignment.Invalid;
            }
            return screwAlignment;
        }

        private static ScrewBrandType FindScrewBrandType(string findString)
        {
            ScrewBrandType screwBrandType;
            ScrewBrandType.TryParse(findString, out screwBrandType);
            return screwBrandType;
        }
    }
}