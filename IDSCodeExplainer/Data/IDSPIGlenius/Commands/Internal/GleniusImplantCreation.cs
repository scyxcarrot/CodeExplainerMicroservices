
using IDS.Core.PluginHelper;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace IDS.Glenius.Commands.Internal
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("560E0DF5-163F-4B4E-AB0D-92F358600855")]
    [CommandStyle(Style.ScriptRunner)]
    public class GleniusImplantCreation : Command
    {
        public GleniusImplantCreation()
        {
            Instance = this;
        }

        public static GleniusImplantCreation Instance { get; private set; }

        public override string EnglishName => "GleniusImplantCreation";

        private ObjectExporter exporter;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = IDSPluginHelper.GetDirector<GleniusImplantDirector>(doc.DocumentId);
            if (director == null)
            {
                return Result.Failure;
            }

            var success = false;

            //Export location selection
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Please select a folder to export implants";
            var rc = dialog.ShowDialog();
            if (rc != DialogResult.OK)
            {
                return Result.Failure;
            }

            var exportFolderPath = Path.GetFullPath(dialog.SelectedPath);

            //To use chamfer or not
            var getOption = new GetOption();
            getOption.SetCommandPrompt("Type");
            getOption.AcceptNothing(true);
            var productionRodChamfer = new OptionToggle(false, "False", "True");
            getOption.AddOptionToggle("ProductionRodChamfer", ref productionRodChamfer);
            var result = getOption.Get();
            if (result != GetResult.Cancel)
            {
                var useProductionRodChamfer = productionRodChamfer.CurrentValue;
                var solidPartCreator = new SolidPartCreator(director, useProductionRodChamfer);

                exporter = new ObjectExporter(doc);
                exporter.ExportDirectory = exportFolderPath;
                var generator = new ImplantFileNameGenerator(director);

                //Create STLs
                Mesh solidPartForReporting;
                var solidPartForReportingMeshSuccess =
                    solidPartCreator.GetSolidPartForReporting(out solidPartForReporting);
                ExportGeometry(solidPartForReporting, generator.GeneratePlateForReportingFileName(),
                    solidPartForReportingMeshSuccess);

                Mesh solidPartForFinalization;
                var solidPartForFinalizationMeshSuccess =
                    solidPartCreator.GetSolidPartForFinalization(out solidPartForFinalization);
                ExportGeometry(solidPartForFinalization, generator.GeneratePlateForFinalizationFileName(),
                    solidPartForFinalizationMeshSuccess);

                Mesh solidPartForProductionOffset;
                var solidPartForProductionOffsetMeshSuccess =
                    solidPartCreator.GetSolidPartForProductionOffset(out solidPartForProductionOffset);
                ExportGeometry(solidPartForProductionOffset, generator.GeneratePlateForProductionOffsetFileName(),
                    solidPartForProductionOffsetMeshSuccess);

                //Create STPs
                var stpCreator = new SolidPartStpCreator(director, useProductionRodChamfer, exporter.ExportDirectory);
                Brep solidPartForProductioOffset;
                Dictionary<string, Brep> productionOffsetIntermediates;
                var solidPartForProductionOffsetSuccess =
                    stpCreator.CreateForProductionOffset(out solidPartForProductioOffset, out productionOffsetIntermediates);
                ExportGeometry(solidPartForProductioOffset, generator.GeneratePlateForProductionOffsetFileName(),
                    solidPartForProductionOffsetSuccess);
                ExportUtilities.ExportBreps(productionOffsetIntermediates, exporter.ExportDirectory, doc);

                Brep solidPartForProductioReal;
                Dictionary<string, Brep> productionRealIntermediates;
                var solidPartForProductionRealtSuccess =
                    stpCreator.CreateForProductionReal(out solidPartForProductioReal, out productionRealIntermediates);
                ExportGeometry(solidPartForProductioReal, generator.GeneratePlateForProductionFileName(),
                    solidPartForProductionRealtSuccess);
                ExportUtilities.ExportBreps(productionRealIntermediates, exporter.ExportDirectory, doc);

                //Create Scaffold
                var scaffoldCreator = new ImplantScaffoldCreator(director, solidPartCreator);

                Mesh scaffoldForReporting;
                var scaffoldForReportingSuccess = scaffoldCreator.GetScaffoldForReporting(out scaffoldForReporting);
                ExportGeometry(scaffoldForReporting, generator.GenerateScaffoldForReportingFileName(),
                    scaffoldForReportingSuccess);

                Mesh scaffoldForFinalization;
                var scaffoldForFinalizationSuccess =
                    scaffoldCreator.GetScaffoldForFinalization(out scaffoldForFinalization);
                ExportGeometry(scaffoldForFinalization, generator.GenerateScaffoldForFinalizationFileName(),
                    scaffoldForFinalizationSuccess);

                success = solidPartForReportingMeshSuccess && solidPartForFinalizationMeshSuccess &&
                          solidPartForProductionOffsetMeshSuccess &&
                          solidPartForProductionOffsetSuccess && solidPartForProductionRealtSuccess &&
                          scaffoldForReportingSuccess && scaffoldForFinalizationSuccess;

                exporter = null;
            }
            doc.Views.Redraw();

            return success ? Result.Success : Result.Failure;
        }

        private void ExportGeometry(GeometryBase geometry, string fileName, bool success)
        {
            if (success && geometry != null)
            {
                switch (geometry.ObjectType)
                {
                    case ObjectType.Brep:
                        exporter.ExportStp((Brep)geometry, fileName);
                        break;
                    case ObjectType.Mesh:
                        exporter.ExportStl((Mesh)geometry, fileName);
                        break;
                    default:
                        break;
                }
            }
        }
    }

#endif
}