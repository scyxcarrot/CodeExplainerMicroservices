using Rhino;
using Rhino.Geometry;
using Rhino.Input;
using System;
using System.IO;
using MDCK = Materialise.SDK.MDCK;
using RhinoMatSDKOperations.Smooth;
using Rhino.Input.Custom;
using Rhino.Commands;
using System.Collections.Generic;

namespace RhinoMatSDKOperations.Commands
{
    [System.Runtime.InteropServices.Guid("4B84BA77-DEE4-45F8-9F3A-CFAE275805A7")]
    public class MDCKSmoothEdgeCommand : Rhino.Commands.Command
    {
        static MDCKSmoothEdgeCommand m_thecommand;
        public MDCKSmoothEdgeCommand()
        {
            // Rhino only creates one instance of each command class defined in a plug-in, so it is
            // safe to hold on to a static reference.
            m_thecommand = this;
        }

        ///<summary>The one and only instance of this command</summary>
        public static MDCKSmoothEdgeCommand TheCommand
        {
            get { return m_thecommand; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string EnglishName
        {
            get { return "MDCKSmoothEdge"; }
        }

        ///<returns>The command name as it appears on the Rhino command line</returns>
        public override string LocalName
        {
            get 
            {
                return "MDCKSmoothEdge";
            }
        }

        public override Guid Id
        {
            get
            {
                return new Guid("4B84BA77-DEE4-45F8-9F3A-CFAE275805A7");
            }
        }

        /**
        * RunCommand performs a shrinkwrap operation as a Rhino command
        * @param doc        The active Rhino document
        * @param mode       The command runmode
        * @see              Rhino::Commands::Command::RunCommand()
        */
        protected override Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            // Prepare the command
            var go = new Rhino.Input.Custom.GetObject();
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Mesh;
            go.SubObjectSelect = false;
            go.GroupSelect = false;
            go.AcceptNothing(false);

            // Add all the parameters that user can specify
            OptionDouble influenceDistance = new OptionDouble(1.0, true, 0.0);
            OptionDouble minEdgeLength = new OptionDouble(0.125, true, 0.01);
            OptionDouble maxEdgeLength = new OptionDouble(0.25, true, 0.01);
            
            go.AddOptionDouble("influenceDistance", ref influenceDistance);
            go.AddOptionDouble("minEdgeLength", ref minEdgeLength);
            go.AddOptionDouble("maxEdgeLength", ref maxEdgeLength);

            // Ask user to select object
            go.SetCommandPrompt("Select object for smoothing");
            go.GeometryFilter = Rhino.DocObjects.ObjectType.Mesh;
            while (true)
            {
                GetResult get_rc = go.Get(); // prompts the user for input

                if (get_rc == GetResult.Object)
                {
                    break;
                }
            }
            if (go.ObjectCount != 1)
            {
                RhinoApp.WriteLine("[MDCK::Error] Invalid input! Aborting ...");
                return Result.Failure;
            }

            // Ask user to select object
            var gc = new Rhino.Input.Custom.GetObject();
            gc.SetCommandPrompt("Select object curve");
            gc.GeometryFilter = Rhino.DocObjects.ObjectType.Curve;
            while (true)
            {
                GetResult get_rc = gc.Get(); // prompts the user for input

                if (get_rc == GetResult.Object)
                {
                    break;
                }
            }
            if (gc.ObjectCount != 1)
            {
                RhinoApp.WriteLine("[MDCK::Error] Invalid input! Aborting ...");
                return Result.Failure;
            }

            // Set up parameters
            MDCKSmoothEdgeParameters parameters = new MDCKSmoothEdgeParameters();
            parameters.RegionOfInfluence = influenceDistance.CurrentValue;
            //int PointWeight;
            //int Iteration;
            //bool AutoSubdivide;
            parameters.MaxEdgeLength = maxEdgeLength.CurrentValue;
            parameters.MinEdgeLength = minEdgeLength.CurrentValue;
            //double BadThreshold;
            //bool FastCollapse;
            //bool FlipEdges;
            //bool IgnoreSurfaceInfo;
            //bool RemeshLowQuality;
            //bool SkipBorder;
            //SubdivisionMethod SubdivisionMethod;

            Mesh smoothed;
            Mesh unsmoothed = go.Object(0).Object().Geometry as Mesh;

            Curve edgeCurve = gc.Object(0).Object().Geometry as Curve;
            Point3d edgePoint = edgeCurve.PointAtStart;

            bool success = MDCKSmoothEdge.OperatorSmoothEdge(new List<Mesh> { unsmoothed }, out smoothed, edgePoint, parameters);
            if (!success)
            {
                RhinoApp.WriteLine("[MDCK::Error] Smoothing operation failed. Aborting...");
                return Rhino.Commands.Result.Failure;
            }

            // Add the mesh to the document;
            System.Guid mid = doc.Objects.AddMesh(smoothed);
            if (mid == System.Guid.Empty)
            {
                RhinoApp.WriteLine("[MDCK::Error] Could not add the resulting mesh to the document. Aborting...");
                return Rhino.Commands.Result.Failure;
            }
            doc.Views.Redraw();

            // Reached the end
            return Result.Success;
        }
    }

}

