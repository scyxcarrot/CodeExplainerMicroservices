using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius
{
    public class GleniusObjectManager : ObjectManager
    {
        private const string KeyCoordinateSystem = "coordinate_system";

        private GleniusImplantDirector GleniusImplantDirector => (GleniusImplantDirector) Director;

        /// <summary>
        /// Initializes a new instance of the <see cref="GleniusObjectManager"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="director"></param>
        public GleniusObjectManager(GleniusImplantDirector director) : base(director)
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
        public Guid AddNewBuildingBlock(IBB block, RhinoObject rhobj, bool setattributes)
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
            var deleteEmptyLayersOnDelete = GleniusImplantDirector.DeleteEmptyLayersOnDelete;
            GleniusImplantDirector.DeleteEmptyLayersOnDelete = false;

            var iD = SetBuildingBlock(BuildingBlocks.Blocks[block], blockGeometry, oldID);

            GleniusImplantDirector.DeleteEmptyLayersOnDelete = deleteEmptyLayersOnDelete;
            return iD;
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
            var deleteEmptyLayersOnDelete = GleniusImplantDirector.DeleteEmptyLayersOnDelete;
            GleniusImplantDirector.DeleteEmptyLayersOnDelete = false;

            var iD = SetBuildingBlock(BuildingBlocks.Blocks[block], rhobj, oldID);

            GleniusImplantDirector.DeleteEmptyLayersOnDelete = deleteEmptyLayersOnDelete;
            return iD;
        }

        /// <summary>
        /// Delete the building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public void DeleteBuildingBlock(IBB block)
        {
            if (!HasBuildingBlock(block))
            {
                return;
            }

            foreach (var id in GetAllBuildingBlockIds(block))
            {
                DeleteObject(id);
            }
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
            if (rhobj is IBBinterface<GleniusImplantDirector>)
            {
                return true;
            }

            // Check if the object has a block_type key in its UserDictionary
            IBB blockType;
            var rc = rhobj.Attributes.UserDictionary.TryGetEnumValue<IBB>(ImplantBuildingBlockProperties.KeyBlockType, out blockType);
            if (!rc)
            {
                return false;
            }

            // Unlock the object or it cannot be replaced/deleted
            ObjectMode oldMode = rhobj.Attributes.Mode;
            if (oldMode != ObjectMode.Normal)
            {
                rhobj.Attributes.Mode = ObjectMode.Normal;
                rhobj.CommitChanges();
            }

            // Restore
            RhinoObject customObj;
            if (blockType == IBB.Head)
            {
                customObj = Head.CreateFromArchived(rhobj, true);
            }
            else if (blockType == IBB.Screw)
            {
                customObj = ScrewHelper.CreateFromArchived(rhobj, true);
            }
            else if (blockType == IBB.ScrewMantle)
            {
                customObj = ScrewHelper.CreateScrewMantleFromArchived(rhobj, true);
            }
            else
            {
                customObj = rhobj;
            }

            // Restore Mode attribute
            if (null == customObj)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "[IDS] Error: the object <{0}> could not be restored from its attached archive file", blockType.ToString());
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

        public override void RestoreCustomRhinoObjects(RhinoDoc doc)
        {
            base.RestoreCustomRhinoObjects(doc);

            var implantDirector = (GleniusImplantDirector)Director;

            // Set director for custom objects
            var screwManager = implantDirector.ScrewObjectManager;
            var screws = screwManager.GetAllScrews().ToList();
            foreach (var screw in screws)
            {
                screw.Director = implantDirector;
            }
            if (screws.Select(sc => sc.Index).Distinct().Count() != screws.Count)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "[IDS] Error: File contains screws with duplicate indexes");
            }

            if (implantDirector.Head != null)
            {
                implantDirector.Head.Director = implantDirector;
            }
        }

        public bool GetBuildingBlockCoordinateSystem(IBB block, out Plane coordinateSystem)
        {
            return GetBuildingBlockCoordinateSystem(GetBuildingBlockId(block), out coordinateSystem);
        }

        public bool GetBuildingBlockCoordinateSystem(Guid guid, out Plane coordinateSystem)
        {
            coordinateSystem = Plane.Unset;

            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }
            if (rhinoObject.Attributes.UserDictionary.ContainsKey(KeyCoordinateSystem))
            {
                coordinateSystem = (Plane)rhinoObject.Attributes.UserDictionary[KeyCoordinateSystem];
                return true;
            }

            //backward compatibility
            var index = Director.Document.NamedConstructionPlanes.Find(rhinoObject.Attributes.Name);
            if (index < 0)
            {
                return false;
            }
            coordinateSystem = Director.Document.NamedConstructionPlanes[index].Plane;
            return true;
        }

        public bool SetBuildingBlockCoordinateSystem(IBB block, Plane coordinateSystem)
        {
            var rhinoObject = GetBuildingBlock(block);
            if (rhinoObject == null)
            {
                return false;
            }
            rhinoObject.Attributes.UserDictionary.Set(KeyCoordinateSystem, coordinateSystem);
            return true;
        }
        
        public void TransformBuildingBlock(IBB block, Transform transform)
        {
            var deleteEmptyLayersOnDelete = GleniusImplantDirector.DeleteEmptyLayersOnDelete;
            GleniusImplantDirector.DeleteEmptyLayersOnDelete = false;

            var objects = Director.Document.Objects;

            var guids = GetAllBuildingBlockIds(block).ToList();
            foreach (var guid in guids)
            {
                if (guid == Guid.Empty)
                {
                    continue;
                }

                objects.Unlock(guid, true);
                //Transform the object
                objects.Transform(guid, transform, true);

                //Transform the coordinate System if present
                var rhinoObject = objects.Find(guid);
                Plane ibbCs;
                if (GetBuildingBlockCoordinateSystem(guid, out ibbCs))
                {
                    ibbCs.Transform(transform);
                    rhinoObject.Attributes.UserDictionary.Set(KeyCoordinateSystem, ibbCs);
                }
                    
                objects.Lock(guid, true);
            }

            GleniusImplantDirector.DeleteEmptyLayersOnDelete = deleteEmptyLayersOnDelete;
        }

        public bool ResetBuildingBlockCoordinateSystemToWcs(IBB block)
        {
            var rhinoObjects = GetAllBuildingBlocks(block).ToList();

            foreach (var rhinoObject in rhinoObjects)
            {
                Plane tmpCs;
                if (!GetBuildingBlockCoordinateSystem(block, out tmpCs))
                {
                    continue;
                }

                var wcs = new Plane(new Point3d(0, 0, 0), new Vector3d(1, 0, 0), new Vector3d(0, 1, 0));
                rhinoObject.Attributes.UserDictionary.Set(KeyCoordinateSystem, wcs);
                return true;
            }

            return false;
        }
    }
}
