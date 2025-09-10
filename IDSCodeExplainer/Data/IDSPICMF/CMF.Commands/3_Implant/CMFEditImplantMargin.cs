using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.Core.Utilities;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;
using Style = Rhino.Commands.Style;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("0F29BF7A-B3F1-4B1A-AFF9-63CCAF1CED1B")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.ImplantSupportGuidingOutline, IBB.ImplantMargin)]
    public class CMFEditImplantMargin : CmfCommandBase
    {
        public override string EnglishName => "CMFEditImplantMargin";
        public static CMFEditImplantMargin TheCommand { get; private set; }

        public CMFEditImplantMargin()
        {
            TheCommand = this;
            VisualizationComponent = new CMFAddEditImplantMarginVisualization();
        }
        
        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var editMarginCurveInputGetter = new EditImplantMarginInputGetter(director);
            var res = editMarginCurveInputGetter.GetInputs(out var implantMarginAttributeList);

            if (res != Result.Success)
            {
                doc.Views.Redraw();
                return res;
            }

            var osteotomyParts = ProPlanImportUtilities.GetAllOriginalOsteotomyParts(director.Document);
            var osteotomyPartMerged = MeshUtilities.AppendMeshes(osteotomyParts.Select(mesh => mesh.DuplicateMesh()));
            var inputGetterHelper = new ImplantMarginInputGetterHelper(director);
            var marginHelper = new ImplantMarginHelper(director);
            var marginCreator = new ImplantMarginCreation(director, osteotomyPartMerged);
            var marginMeshesInfo = new List<KeyValuePair<Mesh, ImplantMarginAttribute>>();

            foreach (var implantMarginAttribute in implantMarginAttributeList)
            {
                var transform = inputGetterHelper.GetMarginTransform(implantMarginAttribute.OriginalPart);
                if (!marginCreator.GenerateImplantMargin(implantMarginAttribute.MarginTrimmedCurve,
                    implantMarginAttribute.MarginThickness, transform, out var marginMesh, out var offsettedCurve))
                {
                    return Result.Failure;
                }

                implantMarginAttribute.OffsettedTrimmedCurve = offsettedCurve;
                
                if (!marginMesh.Transform(transform))
                {
                    return Result.Failure;
                }

                marginMeshesInfo.Add(new KeyValuePair<Mesh, ImplantMarginAttribute>(marginMesh, implantMarginAttribute));
            }

            foreach (var marginMeshInfo in marginMeshesInfo)
            {
                var mesh = marginMeshInfo.Key;
                var attribute = marginMeshInfo.Value;
                marginHelper.ReplaceExistingMargin(attribute.MarginGuid, mesh, attribute.MarginTrimmedCurve, attribute.OffsettedTrimmedCurve);
            }

            doc.ClearUndoRecords(true);
            doc.ClearRedoRecords();

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
