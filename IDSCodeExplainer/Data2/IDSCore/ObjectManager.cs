using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.ImplantDirector;
using IDS.Core.Utilities;
using Rhino;
using Rhino.DocObjects;
using Rhino.DocObjects.Custom;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.PluginHelper
{
    public class ObjectManager
    {
        protected readonly string LoDLowKey = "LoDLow";
        protected readonly string ThicknessDataKey = "ThicknessData";
        protected readonly string MinThicknessKey = "MinThickness";
        protected readonly string MaxThicknessKey = "MaxThickness";

        /// <summary>
        /// The document
        /// </summary>
        protected IImplantDirector Director;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectManager"/> class.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="director"></param>
        public ObjectManager(IImplantDirector director)
        {
            Director = director;
        }

        /// <summary>
        /// Adds the new building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="blockGeometry">The block geometry.</param>
        /// <returns></returns>
        public Guid AddNewBuildingBlock(ImplantBuildingBlock block, GeometryBase blockGeometry)
        {
            if (!CheckBlockGeometryIsValid(block, blockGeometry))
            {
                return Guid.Empty;
            }

            // Create attributes
            ObjectAttributes objectAttributes = null;
            ImplantBuildingBlockProperties.GetBlockAttributes(block, Director.Document, blockGeometry, ref objectAttributes);

            // Add it to the document
            return AddBlockGeometryToDocument(block, objectAttributes, blockGeometry);
        }

        /// <summary>
        /// Adds the new building block and Guid.
        /// </summary>
        /// <param name="geometryGuid">Guid you want the object to be created in</param>
        /// <param name="block">The block.</param>
        /// <param name="blockGeometry">The block geometry.</param>
        /// <returns></returns>
        public Guid AddNewBuildingBlock(Guid geometryGuid, ImplantBuildingBlock block, GeometryBase blockGeometry)
        {
            if (!CheckBlockGeometryIsValid(block, blockGeometry))
            {
                return Guid.Empty;
            }

            // Create attributes
            ObjectAttributes objectAttributes = null;
            ImplantBuildingBlockProperties.GetBlockAttributes(block, Director.Document, blockGeometry, ref objectAttributes);
            objectAttributes.ObjectId = geometryGuid;

            // Add it to the document
            return AddBlockGeometryToDocument(block, objectAttributes, blockGeometry);
        }

        private bool CheckBlockGeometryIsValid(
            ImplantBuildingBlock block, GeometryBase blockGeometry)
        {
            if (blockGeometry == null)
            {
                return false;
            }

            // Check if the geometry provided is of the correct type
            var geomType = block.GeometryType;
            if (!geomType.HasFlag(blockGeometry.ObjectType))
            {
                return false;
            }

            return true;
        }

        private Guid AddBlockGeometryToDocument(ImplantBuildingBlock block, ObjectAttributes objectAttributes, GeometryBase blockGeometry)
        {
            if (blockGeometry is Mesh mesh)
            {
                mesh.Faces.CullDegenerateFaces();
                if (!mesh.IsValid)
                {
                    mesh.Vertices.UseDoublePrecisionVertices = false;
                }
            }

            var blockId = Director.Document.Objects.Add(blockGeometry, objectAttributes);
            LogBlockAdded(block.Name);

            return blockId;
        }

        /// <summary>
        /// Adds the new building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="rhobj">The rhobj.</param>
        /// <param name="setattributes">if set to <c>true</c> [setattributes].</param>
        /// <returns></returns>
        public Guid AddNewBuildingBlock(ImplantBuildingBlock block, RhinoObject rhobj, bool setattributes = true)
        {
            if (rhobj == null)
            {
                return Guid.Empty;
            }

            // Add to document
            if (rhobj.ObjectType == ObjectType.Brep)
            {
                Director.Document.Objects.AddRhinoObject((CustomBrepObject)rhobj);
                LogBlockAdded(block.Name);
            }
            else if (rhobj.ObjectType == ObjectType.Mesh)
            {

                var m = ((CustomMeshObject) rhobj);
                m.MeshGeometry.Vertices.UseDoublePrecisionVertices = false;
                m.MeshGeometry.Faces.CullDegenerateFaces();
                Director.Document.Objects.AddRhinoObject(m);

                LogBlockAdded(block.Name);
            }


            // Check input
            var blockObj = rhobj as IBBinterface<IImplantDirector>;
            if (null != blockObj)
            {
                blockObj.Director = Director;
            }

            // Create attributes
            if (setattributes)
            {
                ImplantBuildingBlockProperties.AddBlockAttributes(block, rhobj, true);
            }

            return rhobj.Id;
        }

        /// <summary>
        /// Deletes the object.
        /// </summary>
        /// <param name="blockID">The block identifier.</param>
        /// <returns></returns>
        public bool DeleteObject(Guid blockID)
        {
            if (!IsObjectPresent(blockID))
            {
                return false;
            }

            // Unlock the old object
            Director.Document.Objects.Unlock(blockID, true);
            var blockName = Director.Document.Objects.Find(blockID).Attributes.Name;
            // Remove it
            var deleted = Director.Document.Objects.Delete(blockID, true);
            LogBlockDeleted(blockName);
            return deleted;
        }

        public bool IsObjectPresent(Guid id)
        {
            return Director.Document.Objects.Find(id) != null;
        }

        /// <summary>
        /// Gets all building block ids.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IEnumerable<Guid> GetAllBuildingBlockIds(ImplantBuildingBlock block)
        {
            var settings = new ObjectEnumeratorSettings
            {
                NameFilter = block.Name,
                HiddenObjects = true
            };
            var rhobjs = Director.Document.Objects.FindByFilter(settings);
            return rhobjs.Select(rhobj => rhobj.Id).ToList();
        }

        /// <summary>
        /// Gets all building blocks.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IEnumerable<RhinoObject> GetAllBuildingBlocks(ImplantBuildingBlock block)
        {
            var settings = new ObjectEnumeratorSettings
            {
                NameFilter = block.Name,
                HiddenObjects = true
            };
            var rhobjs = Director.Document.Objects.FindByFilter(settings);
            return rhobjs.ToList();
        }

        /// <summary>
        /// Gets the building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public RhinoObject GetBuildingBlock(ImplantBuildingBlock block)
        {
            var blockId = GetBuildingBlockId(block);
            return blockId != Guid.Empty ? Director.Document.Objects.Find(blockId) : null;
        }

        /// <summary>
        /// Gets the building block identifier.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public Guid GetBuildingBlockId(ImplantBuildingBlock block)
        {
            return GetBuildingBlockId(block.Name);
        }

        /// <summary>                                                                                                                                                                                              
        /// Gets the building block identifier.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="blockName"></param>
        /// <returns></returns>
        public Guid GetBuildingBlockId(string blockName)
        {
            var settings = new ObjectEnumeratorSettings
            {
                NameFilter = blockName,
                HiddenObjects = true
            };

            if (Director.Document == null)
            {
                return Guid.Empty;
            }
            var rhobjs = Director.Document.Objects.FindByFilter(settings);

            return rhobjs.Length != 0 ? rhobjs[0].Id : Guid.Empty;
        }

        /// <summary>
        /// Determines whether [has building block] [the specified block].
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns>
        ///   <c>true</c> if [has building block] [the specified block]; otherwise, <c>false</c>.
        /// </returns>
        public bool HasBuildingBlock(ImplantBuildingBlock block)
        {
            var settings = new ObjectEnumeratorSettings
            {
                NameFilter = block.Name,
                HiddenObjects = true
            };
            var has = Director.Document.Objects.FindByFilter(settings).Length != 0;
            return has;
        }

        /// <summary>
        /// Sets the building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="blockGeometry">The block geometry.</param>
        /// <param name="oldId">The old identifier.</param>
        /// <returns></returns>
        public Guid SetBuildingBlock(ImplantBuildingBlock block, GeometryBase blockGeometry, Guid oldId)
        {
            if (blockGeometry == null)
            {
                return Guid.Empty;
            }

            if (oldId == Guid.Empty)
            {
                return AddNewBuildingBlock(block, blockGeometry);
            }

            var doc = Director.Document;
            if (doc == null)
            {
                return Guid.Empty;
            }

            // Unlock the old object
            doc.Objects.Unlock(oldId, true);

            // Use the appropriate replace function variety
            switch (blockGeometry.ObjectType)
            {
                case ObjectType.Mesh:
                    doc.Objects.Replace(oldId, (Mesh)blockGeometry);
                    LogBlockReplaced(block.Name);
                    break;
                case ObjectType.Brep:
                    doc.Objects.Replace(oldId, (Brep)blockGeometry);
                    LogBlockReplaced(block.Name);
                    break;
                case ObjectType.Curve:
                    doc.Objects.Replace(oldId, (Curve)blockGeometry);
                    LogBlockReplaced(block.Name);
                    break;
                case ObjectType.Surface:
                    doc.Objects.Replace(oldId, (Surface)blockGeometry);
                    LogBlockReplaced(block.Name);
                    break;
                default:
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Cannot replace object of type .");
                    break;
            }

            // Move to correct layer if original layer was deleted (due to automatic deletion of
            // empty layers)
            var layerIndex = ImplantBuildingBlockProperties.GetLayer(block, doc);
            doc.Objects.Find(oldId).Attributes.LayerIndex = layerIndex;

            RemoveItemFromUserDictionary(oldId, LoDLowKey);
            RemoveItemFromUserDictionary(oldId, ThicknessDataKey);

            // Lock the replaced object again
            doc.Objects.Lock(oldId, true);

            return oldId;
        }

        /// <summary>
        /// Sets the building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="rhobj">The rhobj.</param>
        /// <param name="oldID">The old identifier.</param>
        /// <returns></returns>
        public Guid SetBuildingBlock(ImplantBuildingBlock block, RhinoObject rhobj, Guid oldID)
        {
            if (rhobj == null)
            {
                return Guid.Empty;
            }

            if (oldID == Guid.Empty)
            {
                return AddNewBuildingBlock(block, rhobj);
            }

            var doc = Director.Document;
            if (doc == null)
            {
                return Guid.Empty;
            }

            // Unlock the old object
            doc.Objects.Unlock(oldID, true);

            // Replace in document
            var oldRef = new ObjRef(oldID);
            doc.Objects.Replace(oldRef, rhobj);
            LogBlockReplaced(block.Name);

            // Move to correct layer if original layer was deleted (due to automatic deletion of
            // empty layers on object removal)
            var layerIndex = ImplantBuildingBlockProperties.GetLayer(block, doc);
            doc.Objects.Find(oldID).Attributes.LayerIndex = layerIndex;

            RemoveItemFromUserDictionary(oldID, LoDLowKey);
            RemoveItemFromUserDictionary(oldID, ThicknessDataKey);

            // Lock the replaced object again
            doc.Objects.Lock(oldID, true);

            return oldID;
        }
        
        /// <summary>
        /// Restores the custom rhino object.
        /// </summary>
        /// <param name="rhobj">The rhobj.</param>
        /// <returns></returns>
        public virtual bool RestoreCustomRhinoObject(RhinoObject rhobj)
        {
            //does nothing here
            return true;
        }

        /// <summary>
        /// Restores the custom rhino objects.
        /// </summary>
        /// <param name="doc">The document.</param>
        public virtual void RestoreCustomRhinoObjects(RhinoDoc doc)
        {
            // Types of objects to find
            var rhobjFilter = new ObjectEnumeratorSettings
            {
                NormalObjects = true,
                LockedObjects = true,
                HiddenObjects = true,
                ActiveObjects = true,
                ReferenceObjects = true,
                SelectedObjectsFilter = false,
                IncludeLights = false,
                IncludeGrips = false
            };
            var allIdsObj = doc.Objects.GetObjectList(rhobjFilter);

            // Restore each object to its custom version
            foreach (var rhobj in allIdsObj)
            {
                RestoreCustomRhinoObject(rhobj);
            }
        }

        private void LogBlockAdded(string blockName)
        {
            Log("ADDED", blockName);
        }

        private void LogBlockReplaced(string blockName)
        {
            Log("UPDATED", blockName);
        }

        private void LogBlockDeleted(string blockName)
        {
            Log("DELETED", blockName);
        }

        private void Log(string category, string blockName)
        {
            if (Director.IsTestingMode && !string.IsNullOrEmpty(blockName))
            {
                RhinoApp.WriteLine($"[IDS::Log] *BLOCK {category}:: {blockName}");
            }
        }

        public bool GetBuildingBlockLoDLow(Guid guid, out Mesh lowLoD, bool returnWithoutGenerate = false)
        {
            lowLoD = null;

            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning,
                    "Level of Detail - Low failed to generate due to rhino object not found.");
                return false;
            }

            if (rhinoObject.Attributes.UserDictionary.ContainsKey(LoDLowKey))
            {
                lowLoD = (Mesh)rhinoObject.Attributes.UserDictionary[LoDLowKey];
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Level of Detail - Low was used.");
                return true;
            }

            if (returnWithoutGenerate)
            {
                return false;
            }

            IDSPluginHelper.WriteLine(LogCategory.Default,
                "Level of Detail - Low needs to be generated (only needed once, unless model is removed/updated). Generating...");

            var tmpMesh = ((Mesh)rhinoObject.Geometry).DuplicateMesh();
            var tmpLoD = GenerateLoDLow(tmpMesh);
            if (tmpLoD != null)
            {
                rhinoObject.Attributes.UserDictionary.Set(LoDLowKey, tmpLoD);
                lowLoD = (Mesh)rhinoObject.Attributes.UserDictionary[LoDLowKey];
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Level of Detail - Low was generated successfully and been used.");
            }

            tmpMesh.Dispose();

            return true;
        }

        public bool GetBuildingBlockThicknessData(Guid guid, out double[] thicknessData)
        {
            thicknessData = null;

            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }

            if (!rhinoObject.Attributes.UserDictionary.ContainsKey(ThicknessDataKey))
            {
                return false;
            }

            thicknessData = (double[])rhinoObject.Attributes.UserDictionary[ThicknessDataKey];

            return true;
        }

        public bool GetBuildingBlockThicknessMinMax(Guid guid, ref double minThickness, ref double maxThickness)
        {
            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }

            if (!rhinoObject.Attributes.UserDictionary.ContainsKey(MinThicknessKey))
            {
                return false;
            }

            if (!rhinoObject.Attributes.UserDictionary.ContainsKey(MaxThicknessKey))
            {
                return false;
            }

            minThickness = (double)rhinoObject.Attributes.UserDictionary[MinThicknessKey];
            maxThickness = (double)rhinoObject.Attributes.UserDictionary[MaxThicknessKey];

            return true;
        }

        public bool SetBuildingBlockLoDLow(Guid guid, Mesh lowLoD)
        {
            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }

            RemoveItemFromUserDictionary(guid, LoDLowKey);

            rhinoObject.Attributes.UserDictionary.Set(LoDLowKey, lowLoD);

            return true;
        }

        public bool SetBuildingBlockThicknessData(Guid guid, double[] thicknessData)
        {
            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }

            var isRecording = RhinoDoc.ActiveDoc.UndoRecordingEnabled;
            RhinoDoc.ActiveDoc.UndoRecordingEnabled = false;

            RemoveItemFromUserDictionary(guid, ThicknessDataKey);
            rhinoObject.Attributes.UserDictionary.Set(ThicknessDataKey, thicknessData);

            RhinoDoc.ActiveDoc.UndoRecordingEnabled = isRecording;

            return true;
        }

        public bool SetBuildingBlockThicknessMinMax(Guid guid, double minThicknessKey, double maxThicknessKey)
        {
            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }

            var isRecording = RhinoDoc.ActiveDoc.UndoRecordingEnabled;
            RhinoDoc.ActiveDoc.UndoRecordingEnabled = false;

            RemoveItemFromUserDictionary(guid, MinThicknessKey);
            rhinoObject.Attributes.UserDictionary.Set(MinThicknessKey, minThicknessKey);

            RemoveItemFromUserDictionary(guid, MaxThicknessKey);
            rhinoObject.Attributes.UserDictionary.Set(MaxThicknessKey, maxThicknessKey);

            RhinoDoc.ActiveDoc.UndoRecordingEnabled = isRecording;

            return true;
        }


        public virtual Mesh GenerateLoDLow(Mesh m)
        {
            var meshRemeshed = m.DuplicateMesh();
            for (var i = 0; i < 2; ++i)
            {
                meshRemeshed = ExternalToolInterop.PerformQualityPreservingReduceTriangles(
                    meshRemeshed, 0.3, 0.5, true, 5.0, 3, false, false);
            }

            if (meshRemeshed.DisjointMeshCount > 0)
            {
                MeshUtilities.FixDisjointedClosedMeshNormals(ref meshRemeshed);
            }

            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Level of Detail - Low is generating and ready to used.");
            return meshRemeshed;
        }

        private void RemoveItemFromUserDictionary(Guid guid, string key)
        {

            if (Director.Document.Objects.Find(guid).Attributes.UserDictionary.ContainsKey(key))
            {
                Director.Document.Objects.Find(guid).Attributes.UserDictionary.Remove(key);
            }
        }

        public List<RhinoObject> FindLayerObjectsByFullPath(ImplantBuildingBlock block)
        {
            foreach (var layer in Director.Document.Layers)
            {
                if (!layer.IsDeleted && layer.FullPath.Equals(block.Layer))
                {
                    return Director.Document.Objects.FindByLayer(layer).ToList();
                }
            }

            return null;
        }
    }
}
