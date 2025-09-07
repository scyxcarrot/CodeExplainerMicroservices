using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using RhinoMtlsCommands.Utilities;
using RhinoMtlsCore.Operations;
using System;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("5ffd09ff-9397-44ed-9061-5d6e4c3b305b")]
    public class MtlsFillHoleNormal : Command
    {
        public MtlsFillHoleNormal()
        {
            TheCommand = this;
        }

        public static MtlsFillHoleNormal TheCommand { get; private set; }
        
        public override string EnglishName => "MtlsFillHoleNormal";
        
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
            var filledHole = HoleFill.PerformNormalHoleFill(mesh, borderSegments, out filled);

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