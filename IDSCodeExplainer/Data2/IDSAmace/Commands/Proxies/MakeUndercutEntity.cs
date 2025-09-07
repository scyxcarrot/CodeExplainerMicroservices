using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using RhinoMtlsCore.Utilities;
using System;

namespace IDS.Amace.Proxies
{
    public static class MakeUndercutEntity
    {       
        public static Result RunCommand(RhinoDoc doc)
        {
            ////////// Select mesh for rotational undercut //////////
#region
            GetObject go = new GetObject();
            go.SetCommandPrompt("Select a mesh");
            const ObjectType geometryFilter = ObjectType.Mesh;
            go.GeometryFilter = geometryFilter;
            go.DisablePreSelect();
            go.SubObjectSelect = false;

            Mesh opmesh;
            while (true)
            {
                GetResult res = go.Get();
                if (res == GetResult.Nothing)
                {
                    return Result.Failure;
                }
                else if (null == go.Object(0).Mesh())
                {
                    return Result.Failure;
                }
                else
                {
                    opmesh = go.Object(0).Mesh(); //.Duplicate()
                    break;
                }
            }
            RhinoApp.WriteLine("[IDS] Mesh selected.");
#endregion

            ////////// Select axis for rotational undercut //////////
#region
            GetObject go2 = new GetObject();
            go2.SetCommandPrompt("Select a Line for rotational undercut.");
            const ObjectType lineFilter = ObjectType.Curve;
            go2.GeometryFilter = lineFilter;
            go2.DisablePreSelect();
            go2.SubObjectSelect = false;

            LineCurve ucrotax;
            while (true)
            {
                GetResult res = go2.Get();
                if (res == GetResult.Nothing)
                {
                    return Result.Failure;
                }
                else if (null == go2.Object(0).Geometry())
                {
                    return Result.Failure;
                }
                else
                {
                    ucrotax = go2.Object(0).Geometry() as LineCurve;
                    break;
                }
            }
#endregion

            ////////// Rotational undercut parameters //////////
#region
            Point3d rotax_origin = ucrotax.Line.From;
            Vector3d rotax_dir = ucrotax.Line.Direction;
            rotax_dir.Unitize();

            double rotUC_angle = 30.0; // total rotation angle
            double preview_rotUC_step = 5.0; // preview angle step
            double rotUC_step = 1.0; // uc step
            int rotUC_nstep = 5; // nstep combination

            // init transform, targetmesh and tempmesh
            Transform rotuc_trans;
            Mesh targetmesh = opmesh.DuplicateMesh();
            Mesh tempmesh;

            // do the rotation copies and redraw
            for (int i = 1; i <= Math.Ceiling(Math.Abs(rotUC_angle) / preview_rotUC_step); i++)
            {
                rotuc_trans = Transform.Rotation(RhinoMath.ToRadians(i * preview_rotUC_step) * Math.Sign(rotUC_angle), rotax_dir, rotax_origin);
                tempmesh = opmesh.DuplicateMesh();
                tempmesh.Transform(rotuc_trans);
                targetmesh.Append(tempmesh);
            }
            Guid uce_forvis = doc.Objects.AddMesh(targetmesh);
            doc.Views.Redraw();

            // Modify options for rotation undercut
            GetOption getopt = new GetOption();
            OptionDouble totalangle = new OptionDouble(rotUC_angle, -60.0, 60.0);
            getopt.AddOptionDouble("TotalAngle", ref totalangle);
            string prompt = "Change the parameter values and press enter.";
            getopt.SetCommandPrompt(prompt);
            while (true)
            {
                GetResult get_rc = getopt.Get();

                if (get_rc == Rhino.Input.GetResult.Option)
                {
                    // delete preview and recalculate with updated params, then show preview
                    doc.Objects.Delete(uce_forvis, false);
                    targetmesh = opmesh.DuplicateMesh();
                    for (int i = 1; i <= Math.Ceiling(Math.Abs(totalangle.CurrentValue) / preview_rotUC_step); i++)
                    {
                        rotuc_trans = Transform.Rotation(RhinoMath.ToRadians(i * preview_rotUC_step * Math.Sign(totalangle.CurrentValue)), rotax_dir, rotax_origin);
                        tempmesh = opmesh.DuplicateMesh();
                        tempmesh.Transform(rotuc_trans);
                        targetmesh.Append(tempmesh);
                    }
                    uce_forvis = doc.Objects.AddMesh(targetmesh);
                    doc.Views.Redraw();
                    continue;
                }

                if (get_rc == GetResult.Nothing || get_rc == GetResult.Cancel || get_rc == GetResult.NoResult)
                {
                    break;
                }
            }

            // final param values
            rotUC_angle = totalangle.CurrentValue;

            // delete preview of rotational undercut
            doc.Objects.Delete(uce_forvis, false);
            doc.Views.Redraw();
#endregion

            RhinoApp.WriteLine("[IDS] rot uc preview done.");

            ////////// Select axis for linear undercut //////////
#region
            GetObject go3 = new GetObject();
            go3.SetCommandPrompt("Select a Line for linear undercut.");
            go3.GeometryFilter = lineFilter;
            go3.DisablePreSelect();
            go3.SubObjectSelect = false;

            LineCurve uclinax;
            while (true)
            {
                GetResult res = go3.Get();
                if (res == GetResult.Nothing)
                {
                    return Result.Failure;
                }
                else if (null == go3.Object(0).Geometry())
                {
                    return Result.Failure;
                }
                else
                {
                    uclinax = go3.Object(0).Geometry() as LineCurve;
                    break;
                }
            }
#endregion

            ////////// Linear undercut parameters //////////
#region
            Vector3d linax_dir = uclinax.Line.Direction;
            linax_dir.Unitize();

            double linUC_dist = 30.0; // total undercut distance
            double preview_linUC_step = 5.0; // preview dist step
            double linUC_step = 1.0; // uc step
            int linUC_nstep = 5; // nstep combination

            // init transform, targetmesh and tempmesh
            Transform linuc_trans;
            targetmesh = opmesh.DuplicateMesh();

            // do the rotation copies and redraw
            for (int i = 1; i <= Math.Ceiling(Math.Abs(linUC_dist) / preview_linUC_step); i++)
            {
                linuc_trans = Transform.Translation(linax_dir * i * preview_linUC_step * Math.Sign(linUC_dist));
                tempmesh = opmesh.DuplicateMesh();
                tempmesh.Transform(linuc_trans);
                targetmesh.Append(tempmesh);
            }
            uce_forvis = doc.Objects.AddMesh(targetmesh);
            doc.Views.Redraw();

            // Modify options for rotation undercut
            GetOption getopt2 = new GetOption();
            OptionDouble totaldist = new OptionDouble(linUC_dist, -100.0, 100.0);
            getopt2.AddOptionDouble("TotalDist", ref totaldist);
            getopt2.SetCommandPrompt("Change the parameter values and press enter.");
            while (true)
            {
                GetResult get_rc = getopt2.Get();

                if (get_rc == Rhino.Input.GetResult.Option)
                {
                    // delete preview and recalculate with updated params, then show preview
                    doc.Objects.Delete(uce_forvis, false);
                    targetmesh = opmesh.DuplicateMesh();
                    for (int i = 1; i <= Math.Ceiling(Math.Abs(totaldist.CurrentValue) / preview_linUC_step); i++)
                    {
                        linuc_trans = Transform.Translation(linax_dir * i * preview_linUC_step * Math.Sign(totaldist.CurrentValue));
                        tempmesh = opmesh.DuplicateMesh();
                        tempmesh.Transform(linuc_trans);
                        targetmesh.Append(tempmesh);
                    }
                    uce_forvis = doc.Objects.AddMesh(targetmesh);
                    doc.Views.Redraw();
                    continue;
                }

                if (get_rc == GetResult.Nothing || get_rc == GetResult.Cancel || get_rc == GetResult.NoResult)
                {
                    break;
                }
            }

            // final param values
            linUC_dist = totaldist.CurrentValue;

            // delete preview of rotational undercut
            doc.Objects.Delete(uce_forvis, false);
            doc.Views.Redraw();
#endregion

            // Prep for undercut entity creation
#region

            // reduce opmesh
            RhinoApp.WriteLine("[IDS] reducing input mesh...");
            //MDCKReduceParameters opparams = new MDCKReduceParameters(0.05, 15.0, 3, true);
            var redopmesh = Reduce.PerformReduce(opmesh.DuplicateMesh(), 3);
            if (!redopmesh.IsValid)
            {
                RhinoApp.WriteLine("[IDS] reduce failed, aborting...");
                return Result.Failure;
            }

#endregion

            ///// lin uc
#region

            // copying small batch
            RhinoApp.WriteLine("[IDS] linuc: copying small batch...");
            targetmesh = redopmesh.DuplicateMesh();
            for (int i = 0; i < linUC_nstep; i++)
            {
                linuc_trans = Transform.Translation(linax_dir * i * linUC_step * Math.Sign(linUC_dist));
                tempmesh = opmesh.DuplicateMesh();
                tempmesh.Transform(linuc_trans);
                targetmesh.Append(tempmesh);
            }

            // Unify the small batch
            RhinoApp.WriteLine("[IDS] linuc: unifying small batch copies...");
            var linbatchresult = AutoFix.PerformUnify(targetmesh);
            if (!linbatchresult.IsValid)
            {
                RhinoApp.WriteLine("[IDS] linuc: unify failed, aborting...");
                return Result.Failure;
            }

            // Copy large batch
            RhinoApp.WriteLine("[IDS] linuc: copying small batch...");
            targetmesh = linbatchresult.DuplicateMesh();
            for (int i = 0; i <= Math.Ceiling(Math.Abs(linUC_dist) / (linUC_nstep * linUC_step)); i++)
            {
                linuc_trans = Transform.Translation(linax_dir * i * (linUC_nstep * linUC_step) * Math.Sign(linUC_dist));
                tempmesh = linbatchresult.DuplicateMesh();
                tempmesh.Transform(linuc_trans);
                targetmesh.Append(tempmesh);
            }

            // Unify the large batch
            RhinoApp.WriteLine("[IDS] linuc: unifying large batch copies...");
            linbatchresult = AutoFix.PerformUnify(targetmesh);
            if (!linbatchresult.IsValid)
            {
                RhinoApp.WriteLine("[IDS] linuc: unify failed, aborting...");
                return Result.Failure;
            }

#endregion

            ///// rot uc
            // rotUC_angle preview_rotUC_step rotUC_step rotUC_nstep
#region

            // copying small batch
            RhinoApp.WriteLine("[IDS] rotuc: copying small batch...");
            targetmesh = redopmesh.DuplicateMesh();
            for (int i = 0; i < rotUC_nstep; i++)
            {
                rotuc_trans = Transform.Rotation(RhinoMath.ToRadians(i * rotUC_step * Math.Sign(rotUC_angle)), rotax_dir, rotax_origin);
                tempmesh = opmesh.DuplicateMesh();
                tempmesh.Transform(rotuc_trans);
                targetmesh.Append(tempmesh);
            }

            // Unify the small batch
            RhinoApp.WriteLine("[IDS] rotuc: unifying small batch copies...");
            var rotbatchresult = AutoFix.PerformUnify(targetmesh);
            if (!rotbatchresult.IsValid)
            {
                RhinoApp.WriteLine("[IDS] rotuc: unify failed, aborting...");
                return Result.Failure;
            }

            // Copy large batch
            RhinoApp.WriteLine("[IDS] rotuc: copying small batch...");
            targetmesh = rotbatchresult.DuplicateMesh();
            for (int i = 0; i <= Math.Ceiling(Math.Abs(rotUC_angle) / (rotUC_nstep * rotUC_step)); i++)
            {
                rotuc_trans = Transform.Rotation(RhinoMath.ToRadians(i * rotUC_nstep * rotUC_step * Math.Sign(rotUC_angle)), rotax_dir, rotax_origin);
                tempmesh = rotbatchresult.DuplicateMesh();
                tempmesh.Transform(rotuc_trans);
                targetmesh.Append(tempmesh);
            }

            // Unify the large batch
            RhinoApp.WriteLine("[IDS] rotuc: unifying large batch copies...");
            rotbatchresult = AutoFix.PerformUnify(targetmesh);
            if (!rotbatchresult.IsValid)
            {
                RhinoApp.WriteLine("[IDS] rotuc: unify failed, aborting...");
                return Result.Failure;
            }

#endregion

#region Unify lin entity and rot entity

            // Unify linentity and rotentity
            RhinoApp.WriteLine("[IDS] unifying linear and rotational uc entity...");
            var mergedMeshes = MeshUtilities.MergeMeshes(new Mesh[] { linbatchresult, rotbatchresult });
            var combiresult = AutoFix.PerformUnify(mergedMeshes);
            if (!combiresult.IsValid)
            {
                RhinoApp.WriteLine("[IDS] combi unify failed, aborting...");
                return Result.Failure;
            }

#endregion

#region Final wrap

            // wrap combined undercut entity with gap closing distance
            //MDCKShrinkWrapParameters wrap_params = new MDCKShrinkWrapParameters(0.5, 3.0, 0.0, false, true, false, false);
            Mesh undercutentity;
            var success = Wrap.PerformWrap(new Mesh[] { combiresult }, 0.5, 3.0, 0.0, false, true, false, false, out undercutentity);
            if (!success)
            {
                RhinoApp.WriteLine("[IDS] combi wrap failed, aborting...");
                return Result.Failure;
            }

#endregion

            // add to document
            doc.Objects.AddMesh(undercutentity);
            doc.Views.Redraw();

            // Reached the end: success!
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}