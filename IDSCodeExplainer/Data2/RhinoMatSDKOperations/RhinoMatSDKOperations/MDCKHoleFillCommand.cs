using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using RhinoMatSDKOperations.Fix;
using RhinoMatSDKOperations.Utilities;
using System;

namespace RhinoMatSDKOperations.Commmands
{
    [System.Runtime.InteropServices.Guid("5ffd09ff-9397-44ed-9061-5d6e4c3b305b")]
    public class MDCKHoleFillCommand : Rhino.Commands.Command
    {
        private static MDCKHoleFillCommand m_thecommand;

        public MDCKHoleFillCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKHoleFillCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKHoleFillNormal"; }
        }

        /**
        * Run the HoleFillNormal operation as a Rhino command
        */

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Select a border edge to get entire border
            int[] vert_ids;
            MeshObject mobj;
            bool res = SelectionUtilities.IndicateNakedMeshEdge(doc, out mobj, out vert_ids);
            if (!res)
                return Rhino.Commands.Result.Failure;

            // Get entire border
            int[] border_ids;
            try
            {
                border_ids = MeshUtilities.GetBorderVertexIndices(mobj.MeshGeometry, vert_ids[0]);
            }
            catch (ArgumentException)
            {
                RhinoApp.WriteLine("[MDCK::Error] Invalid mesh or edge");
                return Rhino.Commands.Result.Failure;
            }

            Mesh filled;
            res = MDCKHoleFill.OperatorHoleFillNormal(mobj.MeshGeometry, border_ids[0], out filled);
            if (!res)
            {
                RhinoApp.WriteLine("[MDCK::Error] Holefilling operation failed.");
                return Rhino.Commands.Result.Failure;
            }
            doc.Objects.AddMesh(filled);
            doc.Views.Redraw();
            return Rhino.Commands.Result.Success;
        }
    }
}