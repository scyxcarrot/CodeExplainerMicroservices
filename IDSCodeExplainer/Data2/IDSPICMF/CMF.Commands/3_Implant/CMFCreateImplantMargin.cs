using IDS.CMF;
using IDS.CMF.CommandHelpers;
using IDS.CMF.DataModel;
using IDS.CMF.Enumerators;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Utilities;
using IDS.PICMF.Operations;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.PICMF.Commands
{
    [System.Runtime.InteropServices.Guid("FFFA11BD-02A9-43C8-A47D-532BEEAE5166")]
    [CommandStyle(Style.ScriptRunner)]
    [IDSCMFCommandAttributes(DesignPhase.Implant, IBB.ImplantSupportGuidingOutline)]
    public class CMFCreateImplantMargin : CmfCommandBase
    {
        public CMFCreateImplantMargin()
        {
            TheCommand = this;
            VisualizationComponent = new CMFAddEditImplantMarginVisualization();
        }

        public static CMFCreateImplantMargin TheCommand { get; private set; }

        public override string EnglishName => "CMFCreateImplantMargin";

        public override Result OnCommandExecute(RhinoDoc doc, RunMode mode, CMFImplantDirector director)
        {
            var implantMarginInputGetter = new ImplantMarginInputGetter(director);
            var res = implantMarginInputGetter.GetInputs(out var implantMarginAttributeList);

            if (res != Result.Success)
            {
                return res;
            }

            var marginCreator = new ImplantMarginCreation(director, implantMarginInputGetter.OsteotomyParts);
            var inputGetterHelper = new ImplantMarginInputGetterHelper(director);
            var marginMeshesInfo = new List<KeyValuePair<Mesh, ImplantMarginAttribute>>();
            var marginHelper = new ImplantMarginHelper(director);

            foreach (var implantMarginAttribute in implantMarginAttributeList)
            {
                var transform = inputGetterHelper.GetMarginTransform(implantMarginAttribute.OriginalPart);

                if (!marginCreator.GenerateImplantMargin(implantMarginAttribute.MarginTrimmedCurve,
                    implantMarginInputGetter.MarginThickness, transform, out var marginMesh, out var offsettedCurve)) // Margin thickness is same value for now
                {
                    return Result.Failure;
                }

                if (!marginMesh.Transform(transform))
                {
                    return Result.Failure;
                }

                implantMarginAttribute.OffsettedTrimmedCurve = offsettedCurve;
                marginMeshesInfo.Add(new KeyValuePair<Mesh, ImplantMarginAttribute>(marginMesh, implantMarginAttribute));
            }

            foreach (var marginMeshInfo in marginMeshesInfo)
            {
                var mesh = marginMeshInfo.Key;
                var attribute = marginMeshInfo.Value;
                marginHelper.AddNewMargin(mesh, attribute.MarginCurve.Id, attribute.MarginTrimmedCurve,
                    implantMarginInputGetter.MarginThickness, attribute.OriginalPart.Id, attribute.OffsettedTrimmedCurve); // Margin thickness is same value for now
            }
            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
