using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;

namespace IDS.Core.Operations
{
    public class Locking
    {
        public static void ManageUnlocked(RhinoDoc doc, List<ImplantBuildingBlock> blocks)
        {
            // Lock all
            foreach (RhinoObject obj in doc.Objects)
                doc.Objects.Lock(obj.Id, true);
            // Unlock required building blocks (if any)
            if (blocks != null)
            {
                var director = IDSPluginHelper.GetDirector(doc.DocumentId);
                var objectManager = new ObjectManager(director);

                foreach (ImplantBuildingBlock block in blocks)
                {
                    IEnumerable<Guid> blockIDs = objectManager.GetAllBuildingBlockIds(block);
                    foreach (Guid blockID in blockIDs)
                        doc.Objects.Unlock(blockID, true);
                }
            }
        }

        //// Lock functions ////

        // Lock all
        public static void LockAll(RhinoDoc doc)
        {
            ManageUnlocked(doc, null);
        }

    }
}