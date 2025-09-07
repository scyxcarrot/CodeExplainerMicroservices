using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace
{
    public class ReamingManager
    {
        private readonly ImplantDirector _director;

        public ReamingManager(ImplantDirector director)
        {
            _director = director;
        }

        /// <summary>
        /// Gets all extra reaming entities.
        /// </summary>
        /// <returns></returns>
        private List<Mesh> GetAllExtraReamingEntities()
        {
            // Brep meshing parameters
            var mp = MeshParameters.IDS();

            var objManager = new AmaceObjectManager(_director);

            // Get all extra reaming entities as blocks
            var blockObjs = objManager.GetAllBuildingBlocks(IBB.ExtraReamingEntity);

            // Loop and calculate meshes

            return blockObjs.Select(theBlockObj => (Brep) theBlockObj.Geometry).
                Select(tempBrep => tempBrep.GetCollisionMesh(mp)).ToList();
        }

        /// <summary>
        /// Performs the reaming.
        /// </summary>
        /// <param name="targetBuildingBlock">The target building block.</param>
        /// <param name="reamingEntities">The reaming entities.</param>
        /// <param name="reamedBuildingBlocks">The reamed building blocks.</param>
        /// <param name="rbvPiecesBuildingBlock">The RBV pieces building block.</param>
        /// <returns></returns>
        private bool PerformReaming(IBB targetBuildingBlock, Mesh[] reamingEntities, IBB[] reamedBuildingBlocks, IBB rbvPiecesBuildingBlock, string reamingType)
        {
            Mesh reamedPelvis;
            var objManager = new AmaceObjectManager(_director);

            if (!reamingEntities.Any())
            {
                // Reamed equals target building block
                IDSPluginHelper.WriteLine(LogCategory.Default, $"{reamingType}: No reaming entities available.");
                reamedPelvis = objManager.GetBuildingBlock(targetBuildingBlock).Geometry as Mesh;
            }
            else
            {
                // Do reaming
                Mesh[] rbvPieces;
                var reamingUpdateSuccesful = Ream(targetBuildingBlock, reamingEntities.ToArray(), out reamedPelvis, out rbvPieces);

                if (!reamingUpdateSuccesful)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, $"{ reamingType}: Error during reaming");
                    return false;
                }

                var reamedVolume = CalculateTotalVolume(rbvPieces);
                IDSPluginHelper.WriteLine(LogCategory.Default, $"{reamingType}: {reamedVolume:F0}mm³");
                if (Math.Abs(reamedVolume) > 1e-8)
                {
                    // Add to document
                    foreach (var rbvPiece in rbvPieces)
                    {
                        objManager.AddNewBuildingBlock(rbvPiecesBuildingBlock, rbvPiece);
                    }
                }
            }

            foreach (var reamedBuildingBlock in reamedBuildingBlocks)
            {
                objManager.SetBuildingBlock(reamedBuildingBlock, reamedPelvis, objManager.GetBuildingBlockId(reamedBuildingBlock));
            }

            return true;
        }

        /// <summary>
        /// Performs the graft reaming.
        /// </summary>
        /// <returns></returns>
        public bool PerformGraftReaming()
        {
            var cupGraftReamingSuccesful = PerformCupGraftReaming(IBB.BoneGraft);
            if (!cupGraftReamingSuccesful)
            {
                return false;
            }

            var additionalGraftReamingSuccesful = PerformAdditionalGraftReaming(IBB.BoneGraftRemaining);

            return additionalGraftReamingSuccesful;
        }

        /// <summary>
        /// Performs the additional graft reaming.
        /// </summary>
        /// <param name="targetBuildingBlock">The target building block.</param>
        /// <returns></returns>
        private bool PerformAdditionalGraftReaming(IBB targetBuildingBlock)
        {
            return PerformReaming(targetBuildingBlock, GetAllExtraReamingEntities().ToArray(), new[] { IBB.BoneGraftRemaining }, IBB.AdditionalRbvGraft, "Additional Graft Reaming");
        }

        /// <summary>
        /// Performs the cup graft reaming.
        /// </summary>
        /// <param name="targetBuildingBlock">The target building block.</param>
        /// <returns></returns>
        private bool PerformCupGraftReaming(IBB targetBuildingBlock)
        {
            return PerformReaming(targetBuildingBlock, new [] { _director.cup.outerReamingVolumeMesh }, new[] { IBB.BoneGraftRemaining }, IBB.CupRbvGraft, "Cup Graft Reaming");
        }

        /// <summary>
        /// Performs the additional reaming.
        /// </summary>
        /// <param name="targetBuildingBlock">The target building block.</param>
        /// <returns></returns>
        public bool PerformAdditionalReaming(IBB targetBuildingBlock)
        {
            return PerformReaming(targetBuildingBlock, GetAllExtraReamingEntities().ToArray(), new[] {IBB.ReamedPelvis}, IBB.AdditionalRbv, "Additional Reaming");
        }

        /// <summary>
        /// Performs the cup reaming.
        /// </summary>
        /// <returns></returns>
        public bool PerformCupReaming(IBB targetBuildingBlock)
        {
            return PerformReaming(targetBuildingBlock, new[] { _director.cup.outerReamingVolumeMesh }, new[] { IBB.CupReamedPelvis, IBB.ReamedPelvis }, IBB.CupRbv, "Cup Reaming");
        }

        /// <summary>
        /// Reams the specified target building block.
        /// </summary>
        /// <param name="targetBuildingBlock">The target building block.</param>
        /// <param name="reamingMeshes">The reaming meshes.</param>
        /// <param name="reamedTarget">The reamed target.</param>
        /// <param name="rbvPieces">The RBV pieces.</param>
        /// <returns></returns>
        private bool Ream(IBB targetBuildingBlock, Mesh[] reamingMeshes, out Mesh reamedTarget, out Mesh[] rbvPieces)
        {
            var objManager = new AmaceObjectManager(_director);

            reamedTarget = null;
            rbvPieces = null;

            // Only ream if reaming entities are available
            if (reamingMeshes.Length == 0)
            {
                return false;
            }

            // Set the mesh to be reamed
            var targetObject = objManager.GetBuildingBlock(targetBuildingBlock);
            if (targetObject == null)
            {
                return false;
            }
            var target = objManager.GetBuildingBlock(targetBuildingBlock).Geometry as Mesh;

            // Union all reaming pieces
            Mesh reamingMeshesUnion;
            var unionedReamers = Booleans.PerformBooleanUnion(out reamingMeshesUnion, reamingMeshes);
            if (!unionedReamers)
            {
                return false;
            }

            // Subtract and intersect
            var subtracted = Booleans.PerformBooleanSubtraction(target, reamingMeshesUnion);
            var intersected = Booleans.PerformBooleanIntersection(reamingMeshesUnion, target);
            reamedTarget = subtracted;

            var intersectedFixed = intersected.IsValid ? AutoFix.RemoveNoiseShells(intersected) : intersected;
            rbvPieces = intersectedFixed.SplitDisjointPieces();

            //Atleast the reamedTarget is valid as there's some chance rbv is not possible
            return reamedTarget.IsValid; 
        }

        /// <summary>
        /// Calculates the total volume of an array of Mesh objects
        /// </summary>
        /// <param name="reamedPieces">The reamed pieces.</param>
        /// <returns></returns>
        private static double CalculateTotalVolume(Mesh[] reamedPieces)
        {
            return (from imesh in reamedPieces where imesh.Vertices.Count > 0 select VolumeMassProperties.Compute(imesh) into vm select vm.Volume).Sum();
        }

        /// <summary>
        /// Generates the final reaming.
        /// </summary>
        /// <returns></returns>
        public bool PerformFinalReaming()
        {
            var objManager = new AmaceObjectManager(_director);
            // Get all stuff from director
            var allReamingEntities = new List<Mesh>();
            var screwManager = new ScrewManager(_director.Document);
            allReamingEntities.AddRange(screwManager.GetMedialBumps(fillEmpty: false));
            allReamingEntities.Add(_director.cup.outerReamingVolumeMesh);
            allReamingEntities.AddRange(GetAllExtraReamingEntities());

            // Remove existing Total RBV
            var buildingBlockIds = objManager.GetAllBuildingBlockIds(IBB.TotalRbv);
            foreach (var buildingBlockId in buildingBlockIds)
            {
                objManager.DeleteObject(buildingBlockId);
            }

            return PerformReaming(IBB.DefectPelvis, allReamingEntities.ToArray(),
                new[] {IBB.OriginalReamedPelvis}, IBB.TotalRbv,"Final Reaming");
        }
    }
}
