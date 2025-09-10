using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using RhinoMtlsCore.Utilities;
using System.Collections.Generic;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("23eececf-ead3-4d5e-80d2-c990ea7630a1")]
    public class MtlsRayIntersections : Command
    {
        public MtlsRayIntersections()
        {
            Instance = this;
        }

        public static MtlsRayIntersections Instance { get; private set; }

        public override string EnglishName => "MtlsRayIntersections";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // Prepare the getObject
            var go = new GetObject();
            Mesh[] meshes;

            if (SelectionUtilities.DoGetMultipleMesh(ref go, SelectionUtilities.GetMultiMeshPromptText, out meshes) != Result.Success)
                return Result.Failure;

            var rayDatas = new List<RayData>();

            while (true)
            {
                //Get Direction
                Line rayDirection;
                var getDirection = new GetLine
                {
                    AcceptZeroLengthLine = false,
                    FirstPointPrompt = "Set ray origin",
                    SecondPointPrompt = "Set ray direction",
                };

                var resDir = getDirection.Get(out rayDirection);

                if(resDir == Result.Success)
                {
                    rayDatas.Add(new RayData(rayDirection.From, rayDirection.Direction));
                    doc.Objects.AddLine(rayDirection);
                }
                else if (resDir == Result.Cancel)
                {
                    break;
                }
                else
                {
                    return Result.Failure;
                }
            }

            if(rayDatas.Count == 0)
            {
                return Result.Cancel;
            }

            //Get intersection points
            var points = RayIntersection.PerformRayIntersection(meshes, rayDatas.ToArray());
            if (null == points) return Result.Failure;
            foreach(var pt in points)
            {
                doc.Objects.AddPoint(pt);
            }

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
