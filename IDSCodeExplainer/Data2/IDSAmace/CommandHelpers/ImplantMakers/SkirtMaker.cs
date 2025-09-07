using IDS.Amace.ImplantBuildingBlocks;
using IDS.Amace.Relations;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Amace.Operations
{
    public class SkirtMaker
    {
        public static bool CreateSkirt(ImplantDirector director)
        {
            // Create the skirt
            Mesh skirt;
            Mesh transitionMesh;
            Mesh filled;
            bool success = CreateSkirt(director, out skirt, out transitionMesh, out filled);
            if (!success)
            {
                return false;
            }
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Replace old skirt by new skirt
            Guid oldSkirtMeshID = objectManager.GetBuildingBlockId(IBB.SkirtMesh);
            objectManager.SetBuildingBlock(IBB.SkirtMesh, skirt, oldSkirtMeshID);

            // Delete dependencies
            Dependencies dependencies = new Dependencies();
            dependencies.DeleteBlockDependencies(director, IBB.SkirtMesh);
            dependencies.DeleteDisconnectedSkirtGuides(director);

            // Success
            return true;
        }

        public static bool CreateSkirt(ImplantDirector director, out Mesh skirt, out Mesh transitionMesh, out Mesh filled)
        {
            // init output meshes
            transitionMesh = new Mesh();
            filled = new Mesh();
            skirt = new Mesh();
            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Gather everything from director
            CurveObject liftoffObj = objectManager.GetBuildingBlock(IBB.SkirtCupCurve) as CurveObject;
            CurveObject touchdownObj = objectManager.GetBuildingBlock(IBB.SkirtBoneCurve) as CurveObject;
            if (null == touchdownObj || null == liftoffObj)
            {
                return false;
            }
            Curve liftoff = liftoffObj.CurveGeometry;
            Curve touchdown = touchdownObj.CurveGeometry;

            // Create skirt
            bool success = CreateSkirt(liftoff, touchdown, out skirt, out transitionMesh, out filled);

            // Success
            return success;
        }

        public static bool CreateSkirt(Curve liftoff, Curve touchdown, out Mesh skirt, out Mesh transitionMesh, out Mesh filled)
        {
            // init output meshes
            transitionMesh = new Mesh();
            filled = new Mesh();
            skirt = new Mesh();

            // alternative skirt
            bool rc = MeshOperations.StitchCurvesAndFillTop(liftoff, touchdown, out transitionMesh, out filled, numberOfPoints: 200);
            if (!rc)
            {
                return false;
            }

            // Merge transition mesh and filled
            skirt.Append(transitionMesh);
            skirt.Append(filled);

            // Success
            return true;
        }

        public static bool CreateSweepSkirt(RhinoDoc doc)
        {
            // Check if all needed data is available
            ImplantDirector director = IDSPluginHelper.GetDirector<ImplantDirector>(doc.DocumentId);
            if (director == null)
            {
                return false;
            }

            AmaceObjectManager objectManager = new AmaceObjectManager(director);

            // Get curves
            RhinoObject boneCurve = objectManager.GetBuildingBlock(IBB.SkirtBoneCurve);
            RhinoObject cupCurve = objectManager.GetBuildingBlock(IBB.SkirtCupCurve);
            IEnumerable<RhinoObject> guides = objectManager.GetAllBuildingBlocks(IBB.SkirtGuide);

            // Do sweep2
            Locking.UnlockSkirtCurves(doc);
            Mesh skirtMesh = BrepUtilities.CreateSweepMesh(doc, cupCurve, boneCurve, guides);
            if (skirtMesh == null)
            {
                return false;
            }

            // Set skirtMesh building block
            Guid oldSkirtMesh = objectManager.GetBuildingBlockId(IBB.SkirtMesh);
            objectManager.SetBuildingBlock(IBB.SkirtMesh, skirtMesh, oldSkirtMesh);
            Locking.LockAll(doc);

            // Success
            return true;
        }
    }
}