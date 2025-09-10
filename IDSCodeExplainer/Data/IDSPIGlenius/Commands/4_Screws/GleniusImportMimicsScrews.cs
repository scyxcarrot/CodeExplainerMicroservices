using IDS.Common;
using IDS.Core.CommandBase;
using IDS.Core.GUI;
using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Locking = IDS.Core.Operations.Locking;

namespace IDS.Glenius.Commands
{
    [System.Runtime.InteropServices.Guid("F60AE06F-DA28-4C66-B5C6-3A5F053013FD")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSGleniusCommand(DesignPhase.Screws, IBB.Head)]
    public class GleniusImportMimicsScrews : CommandBase<GleniusImplantDirector>
    {
        public GleniusImportMimicsScrews()
        {
            TheCommand = this;
        }

        public static GleniusImportMimicsScrews TheCommand { get; private set; }

        public override string EnglishName => "GleniusImportMimicsScrews";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, GleniusImplantDirector director)
        {

            if (!director.IsCommandRunnable(this, true))
            {
                return Result.Failure;
            }

            // Loader
            frmWaitbar waitbar = new frmWaitbar();
            waitbar.Title = "Importing screws...";

            try
            {
                waitbar.Show();

                // Run python command to get Mimics clipboard
                var resources = new Core.PluginHelper.Resources();
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
                director.ScrewObjectManager.DeleteAllScrews();

                waitbar.Increment(10);

                // Add from file
                foreach (var theLine in lines)
                {
                    RhinoApp.WriteLine(theLine);
                    ScrewType screwType;
                    Point3d head;
                    Point3d tip;
                    try
                    {
                        var success = ParseScrewLine(theLine, out screwType, out head, out tip);
                        if (!success)
                        {
                            RhinoApp.WriteLine("[IDS]: Screw import of a screw did not work, check naming in mimics...");
                            continue;
                        }
                    }
                    catch
                    {
                        RhinoApp.WriteLine("[IDS]: Screw import of a screw did not work, check naming in mimics...");
                        continue;
                    }

                    var screw = new Screw(director, head, tip, screwType, -1);
                    director.ScrewObjectManager.HandleIndexAssignment(ref screw);
                    screw.Update(); // adds the screw to the document

                    waitbar.Increment((int)Math.Ceiling(80.0 / lines.Count()));
                }

                // Lock everything again
                Locking.LockAll(director.Document);

                // Update screw panel

                // visualisation
                Visibility.ScrewsDefault(doc);

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

        public static bool ParseScrewLine(string screwLine, out ScrewType ST, out Point3d head, out Point3d tip)
        {
            head = new Point3d();
            tip = new Point3d();
            var csv = screwLine.Split(',');
            var nameParts = csv[0].Split('_');

            // Case id
            var ind = nameParts[0].Equals("Unset") ? 0 : 1;

            // Screw type
            ST = TryFindScrewType(nameParts[ind + 1] + '_' + nameParts[ind + 2] + '_' + nameParts[ind + 3]);

            var provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            provider.NumberGroupSeparator = ",";

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

        public static ScrewType TryFindScrewType(string findString)
        {
            return (ScrewType)Enum.Parse(typeof(ScrewType), findString);
        }

        public override void OnCommandExecuteSuccess(RhinoDoc doc, GleniusImplantDirector director)
        {
            GlobalScrewIndexVisualizer.Initialize(director);
        }

        public override void OnCommandExecuteFailed(RhinoDoc doc, GleniusImplantDirector director)
        {
            GlobalScrewIndexVisualizer.Initialize(director);
        }
    }
}