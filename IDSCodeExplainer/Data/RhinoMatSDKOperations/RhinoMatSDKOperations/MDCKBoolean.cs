using Materialise.SDK.MDCK.Model.Objects;
using Rhino;
using Rhino.Geometry;
using RhinoMatSDKOperations.IO;
using System.Collections.Generic;
using System.Linq;
using MDCK = Materialise.SDK.MDCK;

namespace RhinoMatSDKOperations.Boolean
{
    public class MDCKBoolean
    {
        /// <summary>
        /// Perform a sequence of boolean operators using the given meshes.
        /// </summary>
        /// <param name="meshes">The meshes.</param>
        /// <param name="startMeshIdx">Start index of the mesh.</param>
        /// <param name="operationSequence">The operation sequence.</param>
        /// <param name="sequenceResult">The sequence result.</param>
        /// <returns></returns>
        public static bool BooleanSequence(IEnumerable<Mesh> meshes, int startMeshIdx, IEnumerable<MDCKBooleanParameters> operationSequence, out Mesh sequenceResult)
        {
            // Defaults
            sequenceResult = null;
            if (operationSequence.Any(op => op.MeshIndices.Any(idx => (idx < 0 || idx >= meshes.Count()))))
                return false;

            // Convert each mesh to an MDCK model
            var input_models = new List<MDCK.Model.Objects.Model>();
            foreach (Mesh inmesh in meshes)
            {
                MDCK.Model.Objects.Model _model;
                bool rc = MDCKConversion.Rhino2MDCKMeshStl(inmesh, out _model);
                if (!rc)
                    return false;
                input_models.Add(_model);
            }
            // Intermediate results
            MDCK.Model.Objects.Model dest_model = input_models[startMeshIdx];
            MDCK.Model.Objects.Model intermed_model;

            // Perform sequence of operations
            foreach (MDCKBooleanParameters b_args in operationSequence)
            {
                // make a new destination model for current operation
                // TODO: check if intermediate models are correctly disposed after following assignment
                intermed_model = dest_model;
                dest_model = new MDCK.Model.Objects.Model();

                // Make an output model
                if (b_args.Operation == MDCKBooleanOperations.Union)
                {
                    using (var bop = new MDCK.Operators.BooleanUnite())
                    {
                        // Set up operation
                        bop.DestinationModel = dest_model;
                        bop.AddModel(intermed_model);
                        foreach (int model_idx in b_args.MeshIndices)
                        {
                            bop.AddModel(input_models[model_idx]);
                        }

                        // Perform operation
                        try
                        {
                            bop.Operate();
                        }
                        catch (MDCK.Operators.BooleanUnite.Exception)
                        {
                            return false;
                        }
                    }
                }
                else if (b_args.Operation == MDCKBooleanOperations.Subtract)
                {
                    using (var bop = new MDCK.Operators.BooleanSubtract())
                    {
                        // Set up operation
                        bop.DestinationModel = dest_model;
                        bop.AddSourceModel(intermed_model);
                        foreach (int model_idx in b_args.MeshIndices)
                        {
                            bop.AddModelToBeSubtracted(input_models[model_idx]);
                        }

                        // Perform operation
                        try
                        {
                            bop.Operate();
                        }
                        catch (MDCK.Operators.BooleanSubtract.Exception)
                        {
                            return false;
                        }
                    }
                }
                else if (b_args.Operation == MDCKBooleanOperations.Intersect)
                {
                    using (var bop = new MDCK.Operators.BooleanIntersect())
                    {
                        // Set up operation
                        bop.DestinationModel = dest_model;
                        bop.AddModel(intermed_model);
                        foreach (int model_idx in b_args.MeshIndices)
                        {
                            bop.AddModel(input_models[model_idx]);
                        }

                        // Perform operation
                        try
                        {
                            bop.Operate();
                        }
                        catch (MDCK.Operators.BooleanIntersect.Exception)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }

            // Convert result
            bool ok = MDCKConversion.MDCK2RhinoMeshStl(dest_model, out sequenceResult);
            return ok;
        }

        /// <summary>
        /// Do a boolean union.
        /// </summary>
        /// <param name="unioned">The unioned.</param>
        /// <param name="inmeshes">The inmeshes.</param>
        /// <returns></returns>
        public static bool OperatorBooleanUnion(out Mesh unioned, params Mesh[] inmeshes)
        {
            unioned = null;
            MDCK.Operators.BooleanUnite bop = new MDCK.Operators.BooleanUnite();
            List<MDCK.Model.Objects.Model> lstModels = new List<MDCK.Model.Objects.Model>();
            // Add all models to be unioned
            foreach (Mesh part in inmeshes)
            {
                // only if the part contains something
                if (null == part || part.Vertices.Count == 0)
                    continue;

                Model addmesh;
                bool rc = MDCKConversion.Rhino2MDCKMeshStl(part, out addmesh);
                if (!rc)
                    return false;
                lstModels.Add(addmesh);
            }

            // Perform operation
            Model outmodel = new Model();
            bop.DestinationModel = outmodel;
            try
            {
                foreach (Model mdl in lstModels)
                    bop.AddModel(mdl);
                bop.Operate();
            }
            catch (MDCK.Operators.BooleanUnite.Exception)
            {
                return false;
            }

            // Convert restult
            bool result = MDCKConversion.MDCK2RhinoMeshStl(outmodel, out unioned);
            return result;
        }

        /**
         * Compute Boolean difference between input mesh and
         * given subtracting meshes.
         */

        public static bool OperatorBooleanDifference(Mesh inmesh, out Mesh subtracted, params Mesh[] subtracting)
        {
            // Defaults
            subtracted = null;

            // create new MDCK model for input mesh
            if (inmesh.Faces.QuadCount > 0)
                inmesh.Faces.ConvertQuadsToTriangles();
            MDCK.Model.Objects.Model matmesh;
            bool res = MDCKConversion.Rhino2MDCKMeshStl(inmesh, out matmesh);
            if (!res)
                return false;

            // Set up Boolean difference operator
            bool easyOperate = true;// this is being used to see if you even need an operation
            using (var bop = new MDCK.Operators.BooleanSubtract())
            {
                bop.AddSourceModel(matmesh);
                foreach (Mesh subtractor in subtracting)
                {
                    if (subtractor.Vertices.Count > 0)
                    {
                        easyOperate = false;
                        // Convert the mesh
                        if (subtractor.Faces.QuadCount > 0)
                            subtractor.Faces.ConvertQuadsToTriangles();

                        MDCK.Model.Objects.Model submesh;
                        bool rc = MDCKConversion.Rhino2MDCKMeshStl(subtractor, out submesh);
                        if (!rc)
                            return false;

                        // Ad as subtracting entity
                        bop.AddModelToBeSubtracted(submesh);
                    }
                }

                if (easyOperate)
                {
                    subtracted = inmesh;
                    return true;
                }

                // Execute operator
                var outmodel = new MDCK.Model.Objects.Model();
                bop.DestinationModel = outmodel;
                try
                {
                    bop.Operate();
                }
                catch (MDCK.Operators.BooleanSubtract.Exception)
                {
                    return false;
                }

                // Convert restult
                bool result = MDCKConversion.MDCK2RhinoMeshStl(outmodel, out subtracted);
                return result;
            }
        }

        /**
         * Compute Boolean intersection of a number of meshes
         */

        public static bool OperatorBooleanIntersection(out Mesh unioned, params Mesh[] inmeshes)
        {
            unioned = null;
            using (var bop = new MDCK.Operators.BooleanIntersect())
            {
                // Add all models to be unioned
                foreach (Mesh part in inmeshes)
                {
                    // Convert the mesh
                    MDCK.Model.Objects.Model addmesh;
                    bool rc = MDCKConversion.Rhino2MDCKMeshStl(part, out addmesh);
                    if (!rc)
                        return false;

                    // Ad an intersection entity
                    bop.AddModel(addmesh);
                }

                // Perform operation
                var outmodel = new MDCK.Model.Objects.Model();
                bop.DestinationModel = outmodel;
                try
                {
                    bop.Operate();
                }
                catch (MDCK.Operators.BooleanUnite.Exception)
                {
                    return false;
                }

                // Convert restult
                bool result = MDCKConversion.MDCK2RhinoMeshStl(outmodel, out unioned);
                return result;
            }
        }

        /**
         * Intersect a set of meshes with a target mesh. Robust to
         * meshes consisting of disjoint submeshes.
         *
         * @note        if you don't split input meshes in disjoint meshes,
         *              it will discard unaffected pieces.
         * @return      Intersection between target mesh and reaming meshes,
         *              joined into a single mesh
         */

        public static bool BooleanSubtractAndIntersect(IEnumerable<Mesh> reaming_targets, IEnumerable<Mesh> reamers, out Mesh[] reamed_targets, out Mesh[] reamed_pieces)
        {
            // Defaults
            reamed_targets = null;
            reamed_pieces = null;

            // Append meshes into single mesh for STL export
            Mesh target = reaming_targets.First();
            for (int i = 1; i < reaming_targets.Count(); i++)
            {
                target.Append(reaming_targets.ElementAt(i));
            }
            Mesh reamer = reamers.First();
            for (int i = 1; i < reamers.Count(); i++)
            {
                reamer.Append(reamers.ElementAt(i));
            }

            // Convert to MDCK
            MDCK.Model.Objects.Model target_mdck, reamer_mdck;
            bool res = MDCKConversion.Rhino2MDCKMeshStl(target, out target_mdck);
            res = res & MDCKConversion.Rhino2MDCKMeshStl(reamer, out reamer_mdck);
            if (!res)
                return false;

            // Perform boolean difference operation
            using (var bop = new MDCK.Operators.BooleanSubtract())
            using (var iop = new MDCK.Operators.BooleanIntersect())
            {
                // Perform subtraction
                var reamed_mdck = new MDCK.Model.Objects.Model();
                bop.AddSourceModel(target_mdck);
                bop.AddModelToBeSubtracted(reamer_mdck);
                bop.DestinationModel = reamed_mdck;
                try
                {
                    bop.Operate();
                }
                catch (MDCK.Operators.BooleanSubtract.Exception)
                {
                    return false;
                }

                // Perform intersection
                var rbv_mdck = new MDCK.Model.Objects.Model();
                iop.AddModel(target_mdck);
                iop.AddModel(reamer_mdck);
                iop.DestinationModel = rbv_mdck;
                try
                {
                    iop.Operate();
                }
                catch (MDCK.Operators.BooleanIntersect.Exception)
                {
                    return false;
                }

                // Convert result
                Mesh reamed_all, rbv_all;
                bool rc = MDCKConversion.MDCK2RhinoMeshStl(reamed_mdck, out reamed_all);
                rc = rc & MDCKConversion.MDCK2RhinoMeshStl(rbv_mdck, out rbv_all);
                if (!rc)
                    return false;
                reamed_targets = reamed_all.SplitDisjointPieces();
                reamed_pieces = rbv_all.SplitDisjointPieces();
            }

            // Reached end: success!
            return true;
        }

        /**
     * Intersect a set of meshes with a target mesh. Robust to
     * meshes consisting of disjoint submeshes.
     *
     * @note        if you don't split input meshes in disjoint meshes,
     *              it will discard unaffected pieces.
     * @return      Intersection between target mesh and reaming meshes,
     *              joined into a single mesh
     */

        public static bool BooleanSubtractAndIntersect(Mesh target, IEnumerable<Mesh> reamers, out Mesh reamedTargets, out Mesh[] reamedPieces)
        {
            // init
            reamedTargets = null;
            reamedPieces = null;
            Mesh rbvAll;

            // Call the main function
            bool success = BooleanSubtractAndIntersect(target, reamers, out reamedTargets, out rbvAll);
            if (!success)
                return false;

            reamedPieces = rbvAll.SplitDisjointPieces();

            // Reached end: success!
            return true;
        }

        public static bool BooleanSubtractAndIntersect(Mesh target, IEnumerable<Mesh> reamers, out Mesh reamedTargets, out Mesh reamedPieces)
        {
            // Defaults
            reamedTargets = null;
            reamedPieces = null;

            // Append meshes into single mesh for STL export
            Mesh reamer = reamers.First();
            for (int i = 1; i < reamers.Count(); i++)
            {
                reamer.Append(reamers.ElementAt(i));
            }

            // Convert to MDCK
            MDCK.Model.Objects.Model target_mdck, reamer_mdck;
            bool res = MDCKConversion.Rhino2MDCKMeshStl(target, out target_mdck);
            res = res & MDCKConversion.Rhino2MDCKMeshStl(reamer, out reamer_mdck);
            if (!res)
                return false;

            // Perform boolean difference operation
            using (var bop = new MDCK.Operators.BooleanSubtract())
            using (var iop = new MDCK.Operators.BooleanIntersect())
            {
                // Perform subtraction
                var reamed_mdck = new MDCK.Model.Objects.Model();
                bop.AddSourceModel(target_mdck);
                bop.AddModelToBeSubtracted(reamer_mdck);
                bop.DestinationModel = reamed_mdck;
                try
                {
                    bop.Operate();
                }
                catch (MDCK.Operators.BooleanSubtract.Exception)
                {
                    return false;
                }

                // Perform intersection
                var rbv_mdck = new MDCK.Model.Objects.Model();
                iop.AddModel(target_mdck);
                iop.AddModel(reamer_mdck);
                iop.DestinationModel = rbv_mdck;
                try
                {
                    iop.Operate();
                }
                catch (MDCK.Operators.BooleanIntersect.Exception)
                {
                    return false;
                }

                // Convert result
                bool rc = MDCKConversion.MDCK2RhinoMeshStl(reamed_mdck, out reamedTargets);
                if (!rc)
                    return false;

                rc = MDCKConversion.MDCK2RhinoMeshStl(rbv_mdck, out reamedPieces);
                if (!rc)
                    return false;
            }

            // Reached end: success!
            return true;
        }
    }
}