using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.PICMF.NonProduction
{
#if INTERNAL
    public class CMFTrimRemovedMetalVisualization : CMFVisualizationComponentBase
    {
        public override void OnCommandBeginVisualization(RhinoDoc doc)
        {
            SnapshotVisualisation(doc);
            HideAllLayerVisibility(doc);
        }

        public override void OnCommandCanceledVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
        }

        public override void OnCommandFailureVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
        }

        public override void OnCommandSuccessVisualization(RhinoDoc doc)
        {
            RestoreVisualisation(doc);
        }
    }

    [System.Runtime.InteropServices.Guid("0D19256C-3BC9-4634-8FF4-FC5F942BDF4B")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Any, IBB.ImplantSupportRemovedMetalIntegrationRoI)]
    public class CMF_TestTrimmingTools : CmfCommandBase
    {
        static CMF_TestTrimmingTools _instance;
        public CMF_TestTrimmingTools()
        {
            _instance = this;
            VisualizationComponent = new CMFTrimRemovedMetalVisualization();
        }
        
        public static CMF_TestTrimmingTools Instance => _instance;

        public override string EnglishName => "CMF_TestTrimmingTools";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var objectManager = new CMFObjectManager(director);
            var availableParts = new Dictionary<Guid, Mesh>();
            var removedMetals = objectManager.GetAllBuildingBlocks(IBB.ImplantSupportRemovedMetalIntegrationRoI);

            foreach (var removedMetal in removedMetals)
            {
                if (removedMetal.Geometry is Mesh mesh)
                {
                    availableParts.Add(removedMetal.Id, mesh);
                }
            }

            var trimmerTool = new MultipleMeshesTrimmer(availableParts);
            if (!trimmerTool.Execute(doc, "Select the removed metal shell that needs trimming", out var trimmedMeshes))
            {
                return Result.Cancel;
            }

            foreach (var trimmedMesh in trimmedMeshes)
            {
                var rhObject = doc.Objects.Find(trimmedMesh.Key);
                if (rhObject != null)
                {
                    if (trimmedMesh.Value == null)
                    {
                        objectManager.DeleteObject(trimmedMesh.Key);
                    }
                    else
                    {
                        objectManager.SetBuildingBlock(
                            IBB.ImplantSupportRemovedMetalIntegrationRoI, trimmedMesh.Value, trimmedMesh.Key);
                    }
                }
            }
            return Result.Success;
        }
    }
#endif
}
