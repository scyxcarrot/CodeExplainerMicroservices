using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using System;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("33E12F3D-42F8-46C8-B6E9-8CDDB465FA54")]
    public class MtlsFillHoleFreeform : Command
    {
        public MtlsFillHoleFreeform()
        {
            TheCommand = this;
        }

        public static MtlsFillHoleFreeform TheCommand { get; private set; }
        
        public override string EnglishName => "MtlsFillHoleFreeform";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            int edgesCount;
            FillHole.GetHoleFillParameters(out edgesCount);

            Guid referenceMeshId;
            Mesh mesh;
            long[,] borderSegments;
            FillHole.GetHoleFillSegments(doc, edgesCount, out referenceMeshId, out mesh, out borderSegments);

            if (mesh == null || borderSegments == null)
            {
                return Result.Failure;
            }

            Mesh filled;
            bool filledHole;
            double gridSize;
            bool tangent;
            bool treatAsOneHole;
            var gotFillHoleFreeFormParameters = FillHole.GetHoleFillFreeformParameters(out gridSize, out tangent, out treatAsOneHole);
            if (gotFillHoleFreeFormParameters)
            {
                filledHole = HoleFill.PerformFreeformHoleFill(mesh, borderSegments, tangent, treatAsOneHole, gridSize, out filled);
            }
            else
            {
                return Result.Cancel;
            }

            if (filledHole)
            {
                doc.Objects.Add(filled);
                doc.Views.Redraw();
                return Result.Success;
            }
            RhinoApp.WriteLine("[MDCK::Error] Holefilling operation failed.");
            return Result.Failure;
        }
    }
}