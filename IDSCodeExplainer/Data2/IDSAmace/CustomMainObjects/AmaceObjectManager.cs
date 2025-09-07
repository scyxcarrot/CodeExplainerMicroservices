using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;


namespace IDS.Amace
{
    public class AmaceObjectManager : ObjectManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AmaceObjectManager"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="director"></param>
        public AmaceObjectManager(ImplantDirector director) : base(director)
        {

        }

        /// <summary>
        /// Adds the new building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="blockGeometry">The block geometry.</param>
        /// <returns></returns>
        public Guid AddNewBuildingBlock(IBB block, GeometryBase blockGeometry)
        {
            return AddNewBuildingBlock(BuildingBlocks.Blocks[block], blockGeometry);
        }

        /// <summary>
        /// Adds the new building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="rhobj">The rhobj.</param>
        /// <param name="setattributes">if set to <c>true</c> [setattributes].</param>
        /// <returns></returns>
        public Guid AddNewBuildingBlock(IBB block, RhinoObject rhobj, bool setattributes = true)
        {
            return AddNewBuildingBlock(BuildingBlocks.Blocks[block], rhobj, setattributes);
        }

        /// <summary>
        /// Gets all building block ids.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IEnumerable<Guid> GetAllBuildingBlockIds(IBB block)
        {
            return GetAllBuildingBlockIds(BuildingBlocks.Blocks[block]);
        }

        /// <summary>
        /// Gets all building blocks.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IEnumerable<RhinoObject> GetAllBuildingBlocks(IBB block)
        {
            return GetAllBuildingBlocks(BuildingBlocks.Blocks[block]);
        }
        
        /// <summary>
        /// Gets the building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public RhinoObject GetBuildingBlock(IBB block)
        {
            return GetBuildingBlock(BuildingBlocks.Blocks[block]);
        }

        /// <summary>
        /// Gets the building block identifier.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public Guid GetBuildingBlockId(IBB block)
        {
            return GetBuildingBlockId(BuildingBlocks.Blocks[block]);
        }

        /// <summary>
        /// Determines whether [has building block] [the specified block].
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns>
        ///   <c>true</c> if [has building block] [the specified block]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasBuildingBlock(IBB block)
        {
            return HasBuildingBlock(BuildingBlocks.Blocks[block]);
        }

        /// <summary>
        /// Sets the building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="blockGeometry">The block geometry.</param>
        /// <param name="oldID">The old identifier.</param>
        /// <returns></returns>
        public Guid SetBuildingBlock(IBB block, GeometryBase blockGeometry, Guid oldID)
        {
            return SetBuildingBlock(BuildingBlocks.Blocks[block], blockGeometry, oldID);
        }

        /// <summary>
        /// Sets the building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="rhobj">The rhobj.</param>
        /// <param name="oldID">The old identifier.</param>
        /// <returns></returns>
        public Guid SetBuildingBlock(IBB block, RhinoObject rhobj, Guid oldID)
        {
            return SetBuildingBlock(BuildingBlocks.Blocks[block], rhobj, oldID);
        }

        /// <summary>
        /// Restores the custom rhino object.
        /// </summary>
        /// <param name="rhobj">The rhobj.</param>
        /// <returns></returns>
        public override bool RestoreCustomRhinoObject(RhinoObject rhobj)
        {
            if (rhobj == null)
            {
                return false;
            }

            // Make sure we are not re-intializing already initialized objects
            if (rhobj is IBBinterface<ImplantDirector>)
            {
                return true;
            }

            // Check if the object has a block_type key in its UserDictionary
            IBB block_type;
            var rc = rhobj.Attributes.UserDictionary.TryGetEnumValue<IBB>(ImplantBuildingBlockProperties.KeyBlockType, out block_type);
            if (!rc)
            {
                return false;
            }

            // Unlock the object or it cannot be replaced/deleted
            var oldMode = rhobj.Attributes.Mode;
            if (oldMode != ObjectMode.Normal)
            {
                rhobj.Attributes.Mode = ObjectMode.Normal;
                rhobj.CommitChanges();
            }

            // Restore
            RhinoObject customObj = null;
            switch (block_type)
            {
                case IBB.Cup:
                    customObj = Cup.CreateFromArchived(rhobj, true);
                    break;
                case IBB.Screw:
                    customObj = ScrewHelper.CreateFromArchived(rhobj, true);
                    break;
                default:
                    customObj = rhobj;
                    break;
            }

            // Restore Mode attribute
            if (null == customObj)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "[IDS] Error: the object <{0}> could not be restored from its attached archive file", block_type.ToString());
                return false;
            }

            // Restore original state (e.g re-lock)
            if (oldMode != customObj.Attributes.Mode)
            {
                customObj.Attributes.Mode = oldMode;
                customObj.CommitChanges();
            }

            return true;
        }

        /// <summary>
        /// Restores the custom rhino objects.
        /// </summary>
        /// <param name="doc">The document.</param>
        public override void RestoreCustomRhinoObjects(RhinoDoc doc)
        {
            base.RestoreCustomRhinoObjects(doc);

            var implantDirector = Director as ImplantDirector;

            // Set director for custom objects
            var screwManager = new ScrewManager(doc);
            foreach (var screw in screwManager.GetAllScrews())
            {
                screw.Director = implantDirector;
            }

            if (implantDirector?.cup != null)
            {
                implantDirector.cup.Director = implantDirector;
            }
        }

        /// <summary>
        /// Replace a RhinoObject in the document
        /// </summary>
        /// <param name="oldObj"></param>
        /// <param name="newObj"></param>
        /// <returns></returns>
        public static bool ReplaceRhinoObject(RhinoObject oldObj, RhinoObject newObj)
        {
            // Replace the old cup
            var oldRef = new ObjRef(oldObj);
            return oldObj.Document.Objects.Replace(oldRef, newObj);
        }

        public Mesh GetAllIBBInAMeshHelper(bool displayWarningForMissingIbb, params IBB[] ibbs)
        {
            var basePartMeshes = new List<Mesh>();

            ibbs.ToList().ForEach(ibb =>
            {
                var blocks = GetAllBuildingBlocks(ibb).ToList();

                if (!blocks.Any() && displayWarningForMissingIbb)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"{ibb.ToString()} is missing!");
                }

                blocks.ForEach(b =>
                {
                    if (b.Geometry is Brep)
                    {
                        var brep = (Brep)b.Geometry;
                        var meshes = Mesh.CreateFromBrep(brep);
                        basePartMeshes.AddRange(meshes);
                    }
                    else if (b.Geometry is Mesh)
                    {
                        basePartMeshes.Add((Mesh)b.Geometry);
                    }
                    else
                    {
                        throw new IDSException($"{b.Name} is not a valid IBB type to convert to mesh!");
                    }
                });
            });

            return MeshUtilities.UnionMeshes(basePartMeshes);
        }

        public bool IsTransitionPreviewAvailable()
        {
            if (HasBuildingBlock(IBB.TransitionPreview))
            {
                return true;
            }
            IDSPluginHelper.WriteLine(LogCategory.Warning, "Transition not created. Please go back to Plate phase");
            return false;
        }
    }
}
