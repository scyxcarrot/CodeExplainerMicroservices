using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;
using RhinoMatSDKOperations.Curves;
using System;
using System.Collections.Generic;

namespace RhinoMatSDKOperations.Commands
{
    [System.Runtime.InteropServices.Guid("C0B987F2-5FAA-4CEC-9797-19A5CE78D9EB")]
    public class MTLSSplitWithCurve : Command
    {
        /// <summary>
        /// The m_thecommand
        /// </summary>
        private static MTLSSplitWithCurve m_thecommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="MTLSSplitWithCurve"/> class.
        /// </summary>
        public MTLSSplitWithCurve()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        /// <summary>
        /// Gets the command.
        /// </summary>
        /// <value>
        /// The command.
        /// </value>
        public static MTLSSplitWithCurve TheCommand
        {
            get { return m_thecommand; }
        }

        /// <summary>
        /// Gets the name of the command.
        /// This method is abstract.
        /// </summary>
        public override string EnglishName
        {
            get { return "MTLSSplitWithCurve"; }
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="doc">The current document.</param>
        /// <param name="mode">The command running mode.</param>
        /// <returns>
        /// The command result code.
        /// </returns>
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            GetObject go = new GetObject();
            go.GeometryFilter = ObjectType.Mesh;
            go.SubObjectSelect = false;
            go.GroupSelect = false;
            go.AcceptNothing(true);
            go.SetCommandPrompt("Select mesh");
            go.Get();
            if (go.CommandResult() != Result.Success)
                return go.CommandResult();
            var mesh = go.Object(0).Mesh();

            GetObject getCurve = new GetObject();
            getCurve.GeometryFilter = ObjectType.Curve;
            getCurve.SubObjectSelect = false;
            getCurve.GroupSelect = false;
            getCurve.AcceptNothing(true);
            getCurve.SetCommandPrompt("Select curve");
            getCurve.Get();
            if (getCurve.CommandResult() != Result.Success)
                return getCurve.CommandResult();            
            var curve = getCurve.Object(0).Curve();

            List<Mesh> parts;
            if (MDCKSplitWithCurve.OperatorSplitWithCurve(mesh, curve, out parts))
            {
                foreach (var part in parts)
                {
                    if (doc.Objects.AddMesh(part) == Guid.Empty)
                    {
                        return Result.Failure;
                    }
                }

                doc.Views.Redraw();
            }

            return Result.Failure;
        }
    }
}