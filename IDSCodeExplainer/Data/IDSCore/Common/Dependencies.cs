using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Core.Relations
{
    /// <summary>
    /// Dependency handling functions
    /// </summary>
    public abstract class Dependencies<T> where T : IImplantDirector
    {
        /// <summary>
        /// Dictionary that summarizes which blocks are directly dependent on a specific block and should
        /// be deleted if something changes in the latter.
        /// \dotfile dependencies.dot "Dependency graph. Black arrows indicate automatic deletions, red arrows indicate dependency handled by other functions"
        /// </summary>
        protected Dictionary<string, string[]> deleteableDependencies;

        /// <summary>
        /// Deletes the block dependencies.
        /// Public access method for private implementation.
        /// The public version does not require you to type the same block name for block and rootblock.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        protected bool DeleteBlockDependencies(T director, string block)
        {
            return DeleteBlockDependencies(director, block, block);
        }

        /// <summary>
        /// Recursively delete dependencies of a building block.
        /// In most cases, block and rootblock will be the same buidling block. Rootblock is used to
        /// keep top level information in the lower level of the recursion.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="block">The block.</param>
        /// <param name="rootblock">The rootblock.</param>
        /// <returns></returns>
        private bool DeleteBlockDependencies(T director, string block, string rootblock)
        {
            bool deleted = false;
            bool childrendeleted = true;
            string[] dependentblocks = null;
            // Check if the block has dependencies
            if (deleteableDependencies.ContainsKey(block))
            {
                dependentblocks = deleteableDependencies[block];
            }
            // If it does
            if (dependentblocks != null && dependentblocks.Length != 0)
            {
                // Go deeper
                foreach (string subblock in dependentblocks)
                {
                    childrendeleted = DeleteBlockDependencies(director, subblock, rootblock);
                }
            }
            // If it does not and all dependent entities have been deleted
            // Note: the root block is never deleted
            if (!block.Equals(rootblock) && (dependentblocks == null || dependentblocks.Length == 0 || childrendeleted))
            {
                ObjectManager objectManager = new ObjectManager(director);

                // Get block ID
                Guid blockID = objectManager.GetBuildingBlockId(block);
                if (blockID == Guid.Empty)
                {
                    // The block is already deleted or never existed
                    deleted = true;
                }
                while (blockID != Guid.Empty)
                {
                    // Delete block
                    deleted = objectManager.DeleteObject(blockID);
                    // Try to get the next one
                    blockID = objectManager.GetBuildingBlockId(block);
                }
            }

            // See if the destruction of the block also requires objects to be destroyed
            DeleteBlockObjectDependencies(director, block);

            return deleted;
        }

        /// <summary>
        /// Function to delete layers that no longer contain objects.
        /// Useful to clean up after deleting dependencies
        /// </summary>
        /// <param name="director">The director.</param>
        public void DeleteEmptyLayers(T director)
        {
            RhinoObject[] layerObjects;
            foreach (Layer lyr in director.Document.Layers)
            {
                if (lyr.FullPath == "Default")
                {
                    continue;
                }

                layerObjects = director.Document.Objects.FindByLayer(lyr.FullPath);
                if ((lyr.LayerIndex != director.Document.Layers.CurrentLayer.LayerIndex)
                        && (layerObjects != null)
                        && (layerObjects.Length == 0)) // empty layer, not the current one
                {
                    director.Document.Layers.Delete(director.Document.GetLayerWithPath(lyr.FullPath), true);
                }
            }
        }

        protected abstract void DeleteBlockObjectDependencies(T director, string block);



        /// <summary>
        /// Delete disconnected guides
        /// </summary>
        /// <param name="director">The director.</param>
        /// <returns></returns>
        protected bool DeleteDisconnectedGuides(T director, ImplantBuildingBlock topCurveBlock, ImplantBuildingBlock bottomCurveBlock, ImplantBuildingBlock guidesBlock)
        {
            // Parameters
            double threshold = 0.1;
            var objectManager = new ObjectManager(director);

            // top and bottom curve
            if (null == objectManager.GetBuildingBlock(topCurveBlock) || null == objectManager.GetBuildingBlock(bottomCurveBlock))
            {
                return true;
            }
            Curve topCurve = objectManager.GetBuildingBlock(topCurveBlock).Geometry as Curve;
            Curve bottomCurve = objectManager.GetBuildingBlock(bottomCurveBlock).Geometry as Curve;

            var rhobjs = objectManager.GetAllBuildingBlocks(guidesBlock);
            foreach (RhinoObject rhobj in rhobjs)
            {
                double tTop, tBottom;
                Curve guide = rhobj.Geometry as Curve;
                // Check for disconnect on top curve
                topCurve.ClosestPoint(guide.PointAtStart, out tTop);
                if ((guide.PointAtStart - topCurve.PointAt(tTop)).Length > threshold)
                {
                    objectManager.DeleteObject(rhobj.Id);
                    continue;
                }
                // Check for disconnect on bottom curve
                bottomCurve.ClosestPoint(guide.PointAtEnd, out tBottom);
                if ((guide.PointAtEnd - bottomCurve.PointAt(tBottom)).Length > threshold)
                {
                    objectManager.DeleteObject(rhobj.Id);
                }
            }

            return true;
        }

    }
}