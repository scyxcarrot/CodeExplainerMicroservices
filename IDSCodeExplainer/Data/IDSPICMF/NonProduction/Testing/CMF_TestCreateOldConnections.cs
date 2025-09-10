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
    [System.Runtime.InteropServices.Guid("B0E4B8CC-EE48-4E95-93B3-1BA479C8BFFB")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.Screw)]
    public class CMF_TestCreateOldConnections : CmfCommandBase
    {
        public CMF_TestCreateOldConnections()
        {
            TheCommand = this;
        }

        public static CMF_TestCreateOldConnections TheCommand { get; private set; }

        public override string EnglishName => "CMF_TestCreateOldConnections";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var parameter = ImplantCreatorHelper.CreateImplantCreatorParams(director);

            var connectionCreator = new ConnectionCreator(director)
            {
                IsCreateActualConnection = false,
                IsUsingV2Creator = false
            };
            var success = connectionCreator
                .GenerateMissingConnectionPreviews(parameter);

            if (connectionCreator.SuccessfulConnections.Any())
            {
                var message = "Created connections : " + string.Join(",", connectionCreator.SuccessfulConnections);
                IDSPluginHelper.WriteLine(LogCategory.Default, message);
            }
            if (connectionCreator.UnsuccessfulConnections.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Something went wrong during connection creation. Please refer Help website (https://home.materialise.net/sites/Materialise%20Software/Implant%20Design%20Suite/General%20Documents/Website/IDS/FAQ.html) on how to proceed.");

                var message = "Failed connections : " + string.Join(",", connectionCreator.UnsuccessfulConnections);
                IDSPluginHelper.WriteLine(LogCategory.Error, message);
            }
            if (connectionCreator.SkippedConnections.Any())
            {
                var message = "Skipped connections : " + string.Join(",", connectionCreator.SkippedConnections) + ", because it is already created.";
                IDSPluginHelper.WriteLine(LogCategory.Default, message);
            }

            var generatedPreviews = 
                connectionCreator.GeneratedConnections;
            
            if (generatedPreviews.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "Connections Preview Summary");
                var errorSummary = new List<string>();

                // Generate summary for processing time summary
                IDSPluginHelper.WriteLine(LogCategory.Default, "> Processing Time");
                generatedPreviews.ForEach(x =>
                {
                    var layerIndex = doc.GetLayerWithPath($"Old Connections {x.Key.CaseName}" + "::" + "Connections Preview");

                    foreach (var result in x.Value.Item1)
                    {
                        var oa = new ObjectAttributes
                        {
                            LayerIndex = layerIndex,
                            Name = $"Old Connection Preview {x.Key.CaseGuid}"
                        };

                        doc.Objects.AddMesh(result.FinalConnection, oa);
                    }

                    IDSPluginHelper.WriteLine(LogCategory.Default, $"  > {x.Key.CaseName} took {x.Value.Item2.ToInvariantCultureString()} seconds to create");

                    if (connectionCreator.ErrorMessages.Any(y => y.Key == x.Key))
                    {
                        var status = $"Connection creation for {x.Key.CaseName} success with error messages:\n";
                        var errorMessages = connectionCreator.ErrorMessages.First(y => y.Key == x.Key).Value;
                        errorSummary.Add($"{status}{string.Join("\n", errorMessages)}");
                    }
                });

                // Generate summary for error
                if (errorSummary.Any())
                {
                    IDSPluginHelper.WriteLine(LogCategory.Default, "> Failed Connection");
                    errorSummary.ForEach(errSumm =>
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Default, $"  > {errSumm}");
                    });
                }
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No Connection preview summary has been generated");
            }

            doc.Views.Redraw();
            return success ? Result.Success : Result.Failure;
        }
    }
#endif
}
