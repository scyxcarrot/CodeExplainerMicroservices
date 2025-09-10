using System;
using Rhino;
using Rhino.Commands;
using RhinoMtlsCore.Operations;
using Rhino.Geometry;
using RhinoMtlsCommands.Utilities;
using System.Collections.Generic;
using System.Drawing;
using IDS.RhinoMtlsCore.NonProduction;

namespace RhinoMtlsCommands.Commands
{
    [System.Runtime.InteropServices.Guid("d6dabf17-efba-441c-b1d0-9b307e6e01da")]
    public class MtlsShortestPath : Command
    {
        static MtlsShortestPath _instance;
        public MtlsShortestPath()
        {
            _instance = this;
        }

        ///<summary>The only instance of the MtlsShortestPath command.</summary>
        public static MtlsShortestPath Instance => _instance;

        public override string EnglishName => "MtlsShortestPath";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            Mesh mesh;
            Getter.GetMesh("Select mesh", out mesh);

            if (mesh == null)
            {
                return Result.Cancel;
            }

            var startPoint = Getter.GetPoint3d("Start Point", mesh);

            if (startPoint == Point3d.Unset)
            {
                return Result.Cancel;
            }

            var endPoint = Getter.GetPoint3d("End Point", mesh);

            if (endPoint == Point3d.Unset)
            {
                return Result.Cancel;
            }

            List<Point3d> path;
            ShortestPath.FindShortestPath(mesh, startPoint, endPoint, out path);

            var curve = new PolylineCurve(path);
            InternalUtilities.AddCurve(curve, "Shortest Path Curve", "Curve", Color.Blue);

            return Result.Success;
        }
    }
}
