using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
#if (STAGING)
    [System.Runtime.InteropServices.Guid("B0E4B8CC-EE48-4E95-93B3-1BA479C8AFFB")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestCreateOldPastilles : CmfCommandBase
    {
        public CMF_TestCreateOldPastilles()
        {
            TheCommand = this;
        }

        public static CMF_TestCreateOldPastilles TheCommand { get; private set; }

        public override string EnglishName => "CMF_TestCreateOldPastilles";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var parameter = ImplantCreatorHelper.CreateImplantCreatorParams(director);

            var pastilleCreator = new PastilleCreator(director)
            {
                IsCreateActualPastille = false,
                IsUsingV2Creator = false
            };
            var success = pastilleCreator.GenerateMissingPastillePreviews(parameter);

            if (pastilleCreator.SuccessfulPastilles.Any())
            {
                var message = "Created pastilles : " + string.Join(",", pastilleCreator.SuccessfulPastilles);
                IDSPluginHelper.WriteLine(LogCategory.Default, message);
            }
            if (pastilleCreator.UnsuccessfulPastilles.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong during pastilles creation. Please refer Help website (https://home.materialise.net/sites/Materialise%20Software/Implant%20Design%20Suite/General%20Documents/Website/IDS/FAQ.html) on how to proceed.");

                var message = "Failed pastilles : " + string.Join(",", pastilleCreator.UnsuccessfulPastilles);
                IDSPluginHelper.WriteLine(LogCategory.Error, message);
            }
            if (pastilleCreator.SkippedPastilles.Any())
            {
                var message = "Skipped pastilles : " + string.Join(",", pastilleCreator.SkippedPastilles) + ", because it is already created.";
                IDSPluginHelper.WriteLine(LogCategory.Default, message);
            }

            var generatedPreviews = pastilleCreator.GeneratedPastilles;
            
            if (generatedPreviews.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Pastille Preview Summary");
                var errorSummary = new List<string>();

                // Generate summary for processing time summary
                IDSPluginHelper.WriteLine(LogCategory.Default, "> Processing Time");
                generatedPreviews.ForEach(x =>
                {
                    var layerIndex = doc.GetLayerWithPath($"Old Pastille {x.Key.CaseName}" + "::" + $"Pastille Preview");

                    foreach (var result in x.Value.Item1)
                    {
                        var oa = new ObjectAttributes
                        {
                            LayerIndex = layerIndex,
                            Name = $"{result.DotPastilleId}"
                        };

                        doc.Objects.AddMesh(result.FinalPastille, oa);
                    }

                    IDSPluginHelper.WriteLine(LogCategory.Default, $"  > {x.Key.CaseName} took {x.Value.Item2.ToInvariantCultureString()} seconds to create");

                    if (pastilleCreator.ErrorMessages.Any(y => y.Key == x.Key))
                    {
                        var status = $"Pastille creation for {x.Key.CaseName} success with error messages:\n";
                        var errorMessages = pastilleCreator.ErrorMessages.First(y => y.Key == x.Key).Value;
                        errorSummary.Add($"{status}{string.Join("\n", errorMessages)}");
                    }
                });

                // Generate summary for error
                if (errorSummary.Any())
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "> Failed Pastille");
                    errorSummary.ForEach(errSumm =>
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"  > {errSumm}");
                    });
                }
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No pastille preview summary has been generated");
            }

            doc.Views.Redraw();

            return success ? Result.Success : Result.Failure;
        }
    }
#endif
}
