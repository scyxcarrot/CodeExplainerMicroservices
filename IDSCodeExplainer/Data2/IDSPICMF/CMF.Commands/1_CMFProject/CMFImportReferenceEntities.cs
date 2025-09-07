using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.Logics;
using IDS.Core.Plugin;
using IDS.Interface.Logic;
using IDS.PICMF.Visualization;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("77EADF14-E2CB-4B6D-880C-B08259244406")]
    [IDSCMFCommandAttributes(~DesignPhase.Draft)]
    public class CMFImportReferenceEntities : CmfCommandBase
    {
        public CMFImportReferenceEntities()
        {
            TheCommand = this;
            VisualizationComponent = new CMFImportReferenceEntitiesVisualization();
        }

        public static CMFImportReferenceEntities TheCommand { get; private set; }

        public override string EnglishName => "CMFImportReferenceEntities";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var idsConsole = new IDSRhinoConsole();
            var logicHelper = new ImportReferenceEntitiesHelper(director, mode, idsConsole);
            var logic = new ImportReferenceEntitiesLogic(idsConsole, logicHelper);

            var hiddenPaths = GetReferenceEntitiesHiddenPaths(doc);
            var status = logic.Execute(out _);

            if (status == LogicStatus.Success)
            {
                Core.Visualization.Visibility.SetHidden(doc, hiddenPaths);
                Core.Visualization.Visibility.SetVisible(doc, ParseForMainLayer(BuildingBlocks.Blocks[IBB.ReferenceEntities].Layer));
            }

            return status.ToResultStatus();
        }

        private List<string> GetReferenceEntitiesHiddenPaths(RhinoDoc doc)
        {
            var hiddenPaths = new List<string>();

            var referenceEntitiesLayerString = ParseForMainLayer(
                BuildingBlocks.Blocks[IBB.ReferenceEntities].Layer);
            var referenceEntitiesLayer = doc.Layers.FindName(referenceEntitiesLayerString);
            if (referenceEntitiesLayer == null)
            {
                return hiddenPaths;
            }

            var referenceEntityLayerChildren = referenceEntitiesLayer.GetChildren();
            foreach (var referenceEntityLayerChild in referenceEntityLayerChildren)
            {
                hiddenPaths.Add(referenceEntityLayerChild.FullPath);
            }

            return hiddenPaths;
        }

        private static string ParseForMainLayer(string input)
        {
            var index = input.IndexOf("::");
            if (index >= 0)
            {
                return input.Substring(0, index);
            }
            return input;
        }
    }
}