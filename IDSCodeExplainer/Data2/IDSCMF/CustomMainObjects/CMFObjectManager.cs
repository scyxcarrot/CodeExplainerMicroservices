using IDS.CMF.CasePreferences;
using IDS.CMF.Compatibility;
using IDS.CMF.DataModel;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Preferences;
using IDS.CMF.Utilities;
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
using System.Text.RegularExpressions;

namespace IDS.CMF
{
    public class CMFObjectManager : ObjectManager
    {
        private const string KeyTransformationMatrix = Constants.AttributeKeys.KeyTransformationMatrix;
        private const string KeyCoordinateSystem = "coordinate_system";
        private const string KeyGuideSupportDrawnRoI = "guide_support_drawn_roi";
        private const string KeyGuideSupportRemovedMetalIntegrationRoISelection = "guide_support_removed_metal_integration_roi_selection";

        private readonly List<CasePreferenceDataModel> casePreferences;
        private readonly List<GuidePreferenceDataModel> guidePreferences;
        private readonly ImplantCaseComponent implantComponent;
        private readonly GuideCaseComponent guideComponent;
        private readonly Dictionary<string, ExtendedImplantBuildingBlock> eBlocks;

        /// <summary>
        /// Initializes a new instance of the <see cref="CMFObjectManager"/> class.
        /// </summary>
        /// <param name="director"></param>
        public CMFObjectManager(CMFImplantDirector director) : base(director)
        {
            casePreferences = director.CasePrefManager.CasePreferences;
            guidePreferences = director.CasePrefManager.GuidePreferences;
            eBlocks = director.EBlock;
            implantComponent = new ImplantCaseComponent();
            guideComponent = new GuideCaseComponent();
        }

        /// <summary>
        /// Adds the new building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="blockGeometry">The block geometry.</param>
        /// <returns></returns>
        public Guid AddNewBuildingBlock(IBB block, GeometryBase blockGeometry)
        {
            var eBlock = new ExtendedImplantBuildingBlock
            {
                Block = BuildingBlocks.Blocks[block],
                PartOf = block,
            };
            return AddNewBuildingBlock(eBlock, blockGeometry);
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

        public Guid AddNewBuildingBlock(ExtendedImplantBuildingBlock eblock, GeometryBase blockGeometry)
        {
            var guid = AddNewBuildingBlock(eblock.Block, blockGeometry);
            HandleEBlock(eblock);
            return guid;
        }

        public Guid AddNewBuildingBlock(ExtendedImplantBuildingBlock eblock, RhinoObject rhobj, bool setattributes = true)
        {
            var guid = AddNewBuildingBlock(eblock.Block, rhobj, setattributes);
            HandleEBlock(eblock);
            return guid;
        }

        public Guid AddNewBuildingBlock(Guid geometryGuid, IBB block, GeometryBase blockGeometry)
        {
            return AddNewBuildingBlock(
                geometryGuid, BuildingBlocks.Blocks[block], blockGeometry);
        }

        public Guid AddNewBuildingBlock(Guid geometryGuid, ExtendedImplantBuildingBlock eblock, GeometryBase blockGeometry)
        {
            var guid = AddNewBuildingBlock(geometryGuid, eblock.Block, blockGeometry);
            HandleEBlock(eblock);
            return guid;
        }

        private IEnumerable<RhinoObject> GetAllBuildingBlocksWithPartOf(IBB block)
        {
            var blocks = new List<RhinoObject>();
            foreach (var eblock in eBlocks.Values)
            {
                if (eblock.PartOf != block)
                {
                    continue;
                }

                blocks.AddRange(GetAllBuildingBlocks(eblock.Block));
            }
            return blocks;
        }
        
        public IEnumerable<ImplantBuildingBlock> GetAllImplantBuildingBlocks(IBB block)
        {
            var blocks = new List<ImplantBuildingBlock>();
            foreach (var eblock in eBlocks.Values)
            {
                if (eblock.PartOf != block)
                {
                    continue;
                }

                blocks.Add(eblock.Block);
            }

            return blocks;
        }

        public IEnumerable<RhinoObject> GetAllBuildingBlocks(string blockName)
        {
            var block = eBlocks.FirstOrDefault(b => b.Key.Equals(blockName)).Value;
            return block != null ? GetAllBuildingBlocks(block.Block) : new List<RhinoObject>();
        }
        
        /// <summary>
        /// Gets all building blocks.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IEnumerable<RhinoObject> GetAllBuildingBlocks(IBB block)
        {
            return GetAllBuildingBlocksWithPartOf(block);
        }

        public IEnumerable<RhinoObject> GetAllBuildingBlocks(ExtendedImplantBuildingBlock eblock)
        {
            return GetAllBuildingBlocks(eblock.Block);
        }

        /// <summary>
        /// Gets all building block ids.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public IEnumerable<Guid> GetAllBuildingBlockIds(IBB block)
        {
            var blocks = GetAllBuildingBlocksWithPartOf(block);
            return blocks.Select(b => b.Id);
        }

        public IEnumerable<Guid> GetAllBuildingBlockIds(ExtendedImplantBuildingBlock eblock)
        {
            return GetAllBuildingBlockIds(eblock.Block);
        }

        /// <summary>
        /// Gets the building block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public RhinoObject GetBuildingBlock(IBB block)
        {
            var rhinoObjects = GetAllBuildingBlocksWithPartOf(block);
            return rhinoObjects.FirstOrDefault();
        }

        public RhinoObject GetBuildingBlock(ExtendedImplantBuildingBlock eblock)
        {
            return GetBuildingBlock(eblock.Block);
        }

        public RhinoObject GetBuildingBlock(string blockName)
        {
            var block = eBlocks.FirstOrDefault(b => b.Key.Equals(blockName)).Value;
            return block != null ? GetBuildingBlock(eBlocks[blockName].Block) : null;
        }

        /// <summary>
        /// Gets the building block identifier.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public Guid GetBuildingBlockId(IBB block)
        {
            var blocks = GetAllBuildingBlocksWithPartOf(block);
            return blocks.Select(b => b.Id).FirstOrDefault();
        }

        public Guid GetBuildingBlockId(ExtendedImplantBuildingBlock eblock)
        {
            return GetBuildingBlockId(eblock.Block);
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
            var hasBlock = false;
            foreach (var eblock in eBlocks.Values)
            {
                if (eblock.PartOf != block)
                {
                    continue;
                }

                hasBlock = HasBuildingBlock(eblock.Block);
                if (hasBlock)
                {
                    break;
                }
            }

            return hasBlock;
        }

        public bool HasBuildingBlock(ExtendedImplantBuildingBlock eblock)
        {
            return HasBuildingBlock(eblock.Block);
        }

        public bool HasEBlock(ExtendedImplantBuildingBlock eblock)
        {
            if (eBlocks.ContainsKey(eblock.Block.Name))
            {
                return true;
            }

            return false;
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
            var id = SetBuildingBlock(BuildingBlocks.Blocks[block], blockGeometry, oldID);

            var doc = Director.Document;
            doc.Objects.Unlock(id, true);

            foreach (var userDictionaryKey in doc.Objects.Find(id).Attributes.UserDictionary.Keys)
            {
                if (userDictionaryKey.Contains(ImplantCreationUtilities.ImplantSupportRoIKeyBaseString))
                {
                    if (doc.Objects.Find(id).Attributes.UserDictionary.ContainsKey(userDictionaryKey))
                    {
                        doc.Objects.Find(id).Attributes.UserDictionary.Remove(userDictionaryKey);
                    }
                }
            }

            doc.Objects.Lock(id, true);
            return id;
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

        public Guid SetBuildingBlock(ExtendedImplantBuildingBlock eblock, RhinoObject rhobj, Guid oldID)
        {
            var guid = SetBuildingBlock(eblock.Block, rhobj, oldID);
            HandleEBlock(eblock);
            return guid;
        }

        public Guid SetBuildingBlock(ExtendedImplantBuildingBlock eblock, GeometryBase blockGeometry, Guid oldID)
        {
            var guid = SetBuildingBlock(eblock.Block, blockGeometry, oldID);
            HandleEBlock(eblock);
            return guid;
        }

        public Guid SetBuildingBlockWithTransform(ExtendedImplantBuildingBlock eblock,
            GeometryBase blockGeometry, Guid oldID, Transform transform)
        {
            var guid = SetBuildingBlock(eblock, blockGeometry, oldID);
            var rhinoObject = Director.Document.Objects.Find(guid);
            rhinoObject.Attributes.UserDictionary.Set(KeyTransformationMatrix, transform);
            return guid;
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
            if (rhobj is IBBinterface<CMFImplantDirector>)
            {
                return true;
            }

            // Check if the object has a block_type key in its UserDictionary
            IBB block_type;
            var rc = rhobj.Attributes.UserDictionary.TryGetEnumValue<IBB>(ImplantBuildingBlockProperties.KeyBlockType, out block_type);

            //workaround to fix IBB-BuildingBlock name mapping for NervesWrapped. This IBB is not an extended building block.
            //1065211 C: Tech Debt - Implement proper fix for IBB.NervesWrapped-building block name mapping
            if (!rc && rhobj.Name == BuildingBlocks.Blocks[IBB.NervesWrapped].Name)
            {
                rc = true;
            }

            if (!rc)
            {
                string block_type_string;
                rc = rhobj.Attributes.UserDictionary.TryGetString(ImplantBuildingBlockProperties.KeyBlockType, out block_type_string)
                    && TryGetIBB(block_type_string, out block_type);
                if (!rc)
                {
                    return false;
                }

                #region Temporary_Until_No_Old_Case
                // TODO: Remove when no more old case
                if (block_type == IBB.ProPlanImport)
                {
                    var proPlanNameCompatibleHelper = new ProPlanNameCompatibleHelper();
                    proPlanNameCompatibleHelper.RenameNewProPlanRhinoObject(rhobj, Director.Document, ref block_type_string);
                }
                #endregion

                RestoreEBlock(block_type, block_type_string);
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
                case IBB.ProPlanImport:
                case IBB.ImplantSupport:
                case IBB.GuideSurfaceWrap:
                    // Make sure facenormals are there
                    ((Mesh) rhobj.Geometry).FaceNormals.ComputeFaceNormals();
                    customObj = rhobj;
                    break;
                case IBB.Screw:
                case IBB.GuideFixationScrew:
                    var screw = CreateScrewFromArchived(rhobj, true);
                    var implantDirector = Director as CMFImplantDirector;
                    screw.Director = implantDirector;
                    customObj = screw;
                    break;
                default:
                    customObj = rhobj;
                    break;
            }

            // Restore original state (e.g re-lock)
            if (oldMode != customObj.Attributes.Mode)
            {
                customObj.Attributes.Mode = oldMode;
                customObj.CommitChanges();
            }

            return true;
        }

        public bool TryGetIBB(string blockType, out IBB block)
        {
            var extract = blockType.Split('_').First();

            //workaround to fix IBB-BuildingBlock name mapping of NervesWrapped
            //1065211 C: Tech Debt - Implement proper fix for IBB.NervesWrapped-building block name mapping
            if (blockType == BuildingBlocks.Blocks[IBB.NervesWrapped].Name)
            {
                extract = IBB.NervesWrapped.ToString();
            }

            return Enum.TryParse<IBB>(extract, true, out block);
        }

        private Screw CreateScrewFromArchived(RhinoObject other, bool replaceInDoc)
        {
            // Restore the screw object from archive
            var restored = new Screw(other, true, true);

            // Replace if necessary
            if (!replaceInDoc)
            {
                return restored;
            }

            var replaced = IDSPluginHelper.ReplaceRhinoObject(other, restored);
            return !replaced ? null : restored;
        }

        private void RestoreEBlock(IBB block, string blockName)
        {
            var restorer = new BuildingBlockRestorer(Director as CMFImplantDirector);
            var eblock = restorer.GetExtendedBuildingBlock(block, blockName);
            HandleEBlock(eblock);
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
                    if (b.Geometry is Brep brep)
                    {
                        var meshes = Mesh.CreateFromBrep(brep);
                        basePartMeshes.AddRange(meshes);
                    }
                    else if (b.Geometry is Mesh mesh)
                    {
                        basePartMeshes.Add(mesh);
                    }
                    else
                    {
                        throw new IDSException($"{b.Name} is not a valid IBB type to convert to mesh!");
                    }
                });
            });

            return MeshUtilities.UnionMeshes(basePartMeshes);
        }

        public ImplantDataModel GetImplantDataModel(RhinoObject implantObject)
        {
            return GetCasePreference(implantObject)?.ImplantDataModel;
        }

        public List<ImplantDataModel> GetAllImplantDataModel()
        {
            return casePreferences.Select(x => x.ImplantDataModel).ToList();
        }

        public List<CasePreferenceDataModel> GetAllCasePreferenceData()
        {
            return casePreferences.ToList();
        }

        public List<GuidePreferenceDataModel> GetAllGuidePreferenceData()
        {
            return guidePreferences.ToList();
        }

        //Returns Null on failure
        public CasePreferenceDataModel TryGetCasePreference(RhinoObject rhObject)
        {
            try
            {
                return GetCasePreference(rhObject);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public GuidePreferenceDataModel GetGuidePreference(RhinoObject rhObject)
        {
            GuidePreferenceDataModel guidePreferenceData = null;

            IBB block;
            if (TryGetIBB(rhObject.Name, out block))
            {
                if (guideComponent.GetGuideComponents().Contains(block))
                {
                    guideComponent.AssertBlockIsNotGuideComponent(block);

                    Guid caseGuid;
                    if (guideComponent.TryGetCaseGuid(rhObject.Name, out caseGuid))
                    {
                        guidePreferenceData = guidePreferences.FirstOrDefault(data => data.CaseGuid == caseGuid);
                    }
                }
            }

            if (guidePreferenceData == null)
            {
                throw new Exception($"Unable to get GuidePreferenceData from {rhObject.Name}!");
            }

            return guidePreferenceData;
        }

        //Returns Null on failure
        public GuidePreferenceDataModel TryGetGuidePreference(RhinoObject rhObject)
        {
            try
            {
                return GetGuidePreference(rhObject);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public GuidePreferenceDataModel GetGuidePreference(Guid prefId)
        {
            GuidePreferenceDataModel guidePreferenceData = null;

            guidePreferenceData = guidePreferences.FirstOrDefault(data => data.CaseGuid == prefId);

            if (guidePreferenceData == null)
            {
                throw new Exception($"Unable to get GuidePreferenceData for GUID {prefId}!");
            }

            return guidePreferenceData;
        }

        public void HandleEBlock(ExtendedImplantBuildingBlock eblock)
        {
            if (eBlocks.ContainsKey(eblock.Block.Name))
            {
                //if information is different, throw exception
                var existing = eBlocks[eblock.Block.Name];
                if (!eblock.Equals(existing))
                {
                    throw new Exception($"{eblock.Block.Name}'s properties are not equal!");
                }
            }
            else if (HasBuildingBlock(eblock))
            {
                eBlocks.Add(eblock.Block.Name, eblock);
            }
        }

        public void ChangeLayer(ImplantBuildingBlock block, RhinoObject rhobj)
        {
            if (rhobj == null)
            {
                return;
            }

            var doc = Director.Document;
            if (doc == null)
            {
                return;
            }

            // Unlock the object
            doc.Objects.Unlock(rhobj.Id, true);

            // Move to correct layer if original layer was deleted (due to automatic deletion of
            // empty layers on object removal)
            var layerIndex = ImplantBuildingBlockProperties.GetLayer(block, doc);
            rhobj.Attributes.LayerIndex = layerIndex;
            rhobj.CommitChanges();

            // Lock the object again
            doc.Objects.Lock(rhobj.Id, true);

            if (eBlocks.ContainsKey(block.Name))
            {
                var existing = eBlocks[block.Name];
                existing.Block.Layer = block.Layer;
            }
        }

        public void UpdateMaterial(ImplantBuildingBlock block, RhinoDoc doc)
        {
            foreach (var docMaterial in doc.Materials)
            {
                if (!docMaterial.Name.Equals(block.Name))
                {
                    continue;
                }

                var bcol = block.Color;

                if (docMaterial.DiffuseColor != bcol || 
                    docMaterial.SpecularColor != bcol ||
                    docMaterial.AmbientColor != bcol)
                {
                    docMaterial.DiffuseColor = bcol;
                    docMaterial.SpecularColor = bcol;
                    docMaterial.AmbientColor = bcol;
                    docMaterial.CommitChanges();
                }

                if (eBlocks.ContainsKey(block.Name))
                {
                    var existing = eBlocks[block.Name];
                    existing.Block.Color = block.Color;
                }
            }

        }

        public CasePreferenceDataModel GetCasePreference(RhinoObject rhObject, bool throwException = true)
        {
            CasePreferenceDataModel casePreferenceData = null;

            IBB block;
            if (TryGetIBB(rhObject.Name, out block))
            {
                if (implantComponent.GetImplantComponents().Contains(block))
                {
                    implantComponent.AssertBlockIsNotImplantComponent(block);

                    Guid caseGuid;
                    if (implantComponent.TryGetCaseGuid(rhObject.Name, out caseGuid))
                    {
                        casePreferenceData = casePreferences.FirstOrDefault(data => data.CaseGuid == caseGuid);
                    }
                }
            }

            if (casePreferenceData == null && throwException)
            {
                throw new Exception($"Unable to get CasePreferenceData from {rhObject.Name}!");
            }

            return casePreferenceData;
        }

        public CasePreferenceDataModel GetCasePreference(IScrew screw)
        {
            CasePreferenceDataModel casePreferenceDataModel;
            GetDotPastilleBasedOn(screw.Id, out casePreferenceDataModel);
            return casePreferenceDataModel;
        }

        public DotPastille GetDotPastilleBasedOn(Guid screwId, out CasePreferenceDataModel casePreferenceDataModel)
        {
            var X = GetAllCasePreferenceData();
            foreach (var x in X)
            {
                foreach (var dot in x.ImplantDataModel.DotList)
                {
                    var pastille = dot as DotPastille;

                    if (pastille?.Screw == null || pastille.Screw.Id != screwId)
                    {
                        continue;
                    }

                    casePreferenceDataModel = x;
                    return (DotPastille)dot;
                }
            }

            casePreferenceDataModel = null;
            return null;
        }

        public void DeleteScrew(Guid screwId)
        {
            CasePreferenceDataModel dummyDataModel;
            var pastille = GetDotPastilleBasedOn(screwId, out dummyDataModel);
            if (pastille != null)
            {
                pastille.Screw = null;
            }

            var screwObj = (Screw)Director.Document.Objects.Find(screwId);
            //Deleted screw can't be re-do-ed back so far
            ((CMFImplantDirector)Director).ScrewGroups.RemoveScrew(screwId);

            RegisteredBarrelUtilities.UnlinkImplantScrew(screwObj.Director, screwId);

            Director.IdsDocument.Delete(screwId);
        }

        public RhinoObject GetImplantObject(CasePreferenceDataModel data)
        {
            var objManager = new CMFObjectManager((CMFImplantDirector)Director);
            var buildingBlock = implantComponent.GetImplantBuildingBlock(IBB.ImplantPreview, data);
            return buildingBlock == null ? null : objManager.GetBuildingBlock(buildingBlock);
        }
        public CasePreferenceDataModel GetImplantCasePreferenceData(RhinoObject implantObject)
        {
            return GetCasePreference(implantObject);
        }

        public Guid AddNewBuildingBlockWithTransform(ExtendedImplantBuildingBlock eblock, 
            GeometryBase blockGeometry, Transform transform)
        {
            var guid = AddNewBuildingBlock(eblock, blockGeometry);
            var rhinoObject = Director.Document.Objects.Find(guid);
            rhinoObject.Attributes.UserDictionary.Set(KeyTransformationMatrix, transform);
            return guid;
        }

        public Guid AddNewBuildingBlockWithCoordinateSystem(ExtendedImplantBuildingBlock eblock,
            GeometryBase blockGeometry, Plane coordinateSystem)
        {
            var guid = AddNewBuildingBlock(eblock, blockGeometry);
            var rhinoObject = Director.Document.Objects.Find(guid);
            rhinoObject.Attributes.UserDictionary.Set(KeyCoordinateSystem, coordinateSystem);
            return guid;
        }

        public override Mesh GenerateLoDLow(Mesh m)
        {
            return GenerateLoDLow(m, true);
        }

        public Mesh GenerateLoDLow(Mesh m, bool enableLogging)
        {
            //QPRT 2 times
            var secondRemeshParams = CMFPreferences.GetActualGuideParameters().RemeshParams;
            var guideSurfaceSecondRemeshed = m.DuplicateMesh();
            var remeshed = ExternalToolInterop.PerformQualityPreservingReduceTriangles(guideSurfaceSecondRemeshed,
                    secondRemeshParams.QualityThreshold, secondRemeshParams.MaximalGeometricError,
                    secondRemeshParams.CheckMaximalEdgeLength, secondRemeshParams.MaximalEdgeLength,
                    secondRemeshParams.NumberOfIterations, secondRemeshParams.SkipBadEdges,
                    secondRemeshParams.PreserveSurfaceBorders, secondRemeshParams.OperationCount, enableLogging);

            if (remeshed == null)
            {
                //retry 3 times max
                for (var j = 0; j < 3; ++j)
                {
                    if (enableLogging)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, $"Lod Low generation retry: {j + 1}...");
                    }

                    var retry = ExternalToolInterop.PerformQualityPreservingReduceTriangles(guideSurfaceSecondRemeshed,
                        secondRemeshParams.QualityThreshold, secondRemeshParams.MaximalGeometricError,
                        secondRemeshParams.CheckMaximalEdgeLength, secondRemeshParams.MaximalEdgeLength,
                        secondRemeshParams.NumberOfIterations, secondRemeshParams.SkipBadEdges,
                        secondRemeshParams.PreserveSurfaceBorders, secondRemeshParams.OperationCount, enableLogging);

                    if (retry != null)
                    {
                        remeshed = retry;
                        break;
                    }
                }
            }

            if (remeshed == null)
            {
                if (enableLogging)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, $"Lod Low generation failed!");
                    Msai.TrackException(new IDSException($"[INTERNAL] Implant support Lod Low generation failed"), "CMF");
                }
                return null;
            }

            guideSurfaceSecondRemeshed = remeshed;

            if (guideSurfaceSecondRemeshed.DisjointMeshCount > 0)
            {
                MeshUtilities.FixDisjointedClosedMeshNormals(ref guideSurfaceSecondRemeshed);
            }

            if (!guideSurfaceSecondRemeshed.IsValid)
            {
                guideSurfaceSecondRemeshed = StlUtilities.RebuildMesh(guideSurfaceSecondRemeshed);
            }

            if (enableLogging)
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Level of Detail - Low was generated successfully and been used.");
            }
            return guideSurfaceSecondRemeshed;
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

            return false;
        }

        public bool SetBuildingBlockCoordinateSystem(Guid guid, Plane coordinateSystem)
        {
            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }

            rhinoObject.Attributes.UserDictionary.Set(KeyCoordinateSystem, coordinateSystem);
            rhinoObject.CommitChanges();

            return true;
        }

        public bool GetBuildingBlockGuideSupportDrawnRoI(Guid guid, out Mesh drawnRoI)
        {
            drawnRoI = null;

            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }
            if (rhinoObject.Attributes.UserDictionary.ContainsKey(KeyGuideSupportDrawnRoI))
            {
                drawnRoI = (Mesh)rhinoObject.Attributes.UserDictionary[KeyGuideSupportDrawnRoI];
                return true;
            }

            return false;
        }

        public bool SetBuildingBlockGuideSupportDrawnRoI(Guid guid, Mesh drawnRoI)
        {
            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }

            rhinoObject.Attributes.UserDictionary.Set(KeyGuideSupportDrawnRoI, drawnRoI);
            rhinoObject.CommitChanges();

            return true;
        }

        public bool RemoveBuildingBlockGuideSupportDrawnRoI(Guid guid)
        {
            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }

            rhinoObject.Attributes.UserDictionary.Remove(KeyGuideSupportDrawnRoI);
            rhinoObject.CommitChanges();

            return true;
        }

        public bool SetGuideSupportRemovedMetalIntegrationRoISelection(Guid guid, List<Guid> selectedIds)
        {
            var rhinoObject = Director.Document.Objects.Find(guid);
            if (rhinoObject == null)
            {
                return false;
            }

            rhinoObject.Attributes.UserDictionary.Set(KeyGuideSupportRemovedMetalIntegrationRoISelection, selectedIds);
            rhinoObject.CommitChanges();

            return true;
        }

        public List<RhinoObject> GetObjectsByParentLayerPathRecursively(string path)
        {
            var layerIndex = Director.Document.GetLayerWithPath(path);
            var layer = Director.Document.Layers[layerIndex];

            var res = new List<RhinoObject>();

            if (layer.GetChildren() == null)
            {
                return res;
            }

            foreach (var child in layer.GetChildren())
            {
                var rhObjs = Director.Document.Objects.FindByLayer(child).ToList();
                res.AddRange(rhObjs);
            }

            return res;
        }

        public List<RhinoObject> GetAllObjectsByRhinoObject(RhinoObject rhObject)
        {
            var path = Director.Document.Layers[rhObject.Attributes.LayerIndex].FullPath;
            return GetAllObjectsByLayerPath(path);
        }

        public List<RhinoObject> GetAllObjectsByLayerPath(string path)
        {
            var layerIndex = Director.Document.GetLayerWithPath(path);
            var layer = Director.Document.Layers[layerIndex];
            return Director.Document.Objects.FindByLayer(layer).ToList();
        }

        public List<RhinoObject> GetAllBuildingBlockRhinoObjectByMatchingName(IBB block, string regex)
        {
            var found = new List<RhinoObject>();

            var ibbs = GetAllBuildingBlocks(block).ToList();
            ibbs.ForEach(x =>
            {
                if (Regex.IsMatch(x.Name, regex, RegexOptions.IgnoreCase))
                {
                    found.Add(x);
                }
            });

            return found;
        }

        public List<RhinoObject> GetAllBuildingBlockRhinoObjectByMatchingNames(IBB block, IEnumerable<string> regexList)
        {
            var regex = string.Join("|", regexList.Select(r => $"({r})"));
            return GetAllBuildingBlockRhinoObjectByMatchingName(block, regex);
        }

        public new bool DeleteObject(Guid blockID)
        {
            var rhinoObject = Director.Document.Objects.Find(blockID);
            if (rhinoObject == null)
            {
                return false;
            }

            var blockName = rhinoObject.Name;
            var deleted = base.DeleteObject(blockID);

            RemoveEBlock(blockName);

            return deleted;
        }

        public void RemoveEBlock(string blockName)
        {
            if (eBlocks.ContainsKey(blockName) && !IsBlockObjectFound(blockName))
            {
                eBlocks.Remove(blockName);
            }
        }

        private bool IsBlockObjectFound(string blockName)
        {
            var settings = new ObjectEnumeratorSettings
            {
                NameFilter = blockName,
                HiddenObjects = true
            };

            var rhobjs = Director.Document.Objects.FindByFilter(settings);
            return rhobjs.Any();
        }

        public List<ExtendedImplantBuildingBlock> GetAllGuideExtendedImplantBuildingBlocks()
        {
            var res = new List<ExtendedImplantBuildingBlock>();

            var director = (CMFImplantDirector)Director;
            director.CasePrefManager.GuidePreferences.ForEach(cp =>
            {
                res.AddRange(GetGuideExtendedImplantBuildingBlocks(cp));
            });

            return res;
        }

        public List<ExtendedImplantBuildingBlock> GetAllGuideExtendedImplantBuildingBlocks(IBB ibb, List<GuidePreferenceDataModel> prefModels)
        {
            var res = new List<ExtendedImplantBuildingBlock>();

            prefModels.ForEach(cp =>
            {
                res.Add(guideComponent.GetGuideBuildingBlock(ibb, cp));
            });

            return res;
        }

        public List<ExtendedImplantBuildingBlock> GetAllImplantExtendedImplantBuildingBlocks()
        {
            var res = new List<ExtendedImplantBuildingBlock>();

            var director = (CMFImplantDirector)Director;
            director.CasePrefManager.CasePreferences.ForEach(cp =>
            {
                res.AddRange(GetImplantExtendedImplantBuildingBlocks(cp));
            });

            return res;
        }

        public List<ExtendedImplantBuildingBlock> GetAllImplantExtendedImplantBuildingBlocks(IBB ibb)
        {
            var res = new List<ExtendedImplantBuildingBlock>();

            var director = (CMFImplantDirector)Director;
            director.CasePrefManager.CasePreferences.ForEach(cp =>
            {
                res.Add(implantComponent.GetImplantBuildingBlock(ibb, cp));
            });

            return res;
        }

        public List<ExtendedImplantBuildingBlock> GetAllImplantExtendedImplantBuildingBlocks(IBB ibb, List<CasePreferenceDataModel> prefModels)
        {
            var res = new List<ExtendedImplantBuildingBlock>();

            prefModels.ForEach(cp =>
            {
                res.Add(implantComponent.GetImplantBuildingBlock(ibb, cp));
            });

            return res;
        }

        public List<Guid> GetAllImplantExtendedImplantBuildingBlocksIDs(IBB ibb)
        {
            var res = new List<Guid>();
            
            var extended = GetAllImplantBuildingBlocks(ibb).ToList();

            extended.ForEach(x =>
            {
                var ids = GetAllBuildingBlockIds(x);
                res.AddRange(ids);
            });

            return res;
        }

        public List<Guid> GetAllImplantExtendedImplantBuildingBlocksIDs(IBB ibb, CasePreferenceDataModel cp)
        {
            var res = new List<Guid>();
            
            var extended = GetAllImplantExtendedImplantBuildingBlocks(ibb, new List<CasePreferenceDataModel>() {cp}).ToList();

            extended.ForEach(x =>
            {
                var ids = GetAllBuildingBlockIds(x);
                res.AddRange(ids);
            });

            return res;
        }

        public List<RhinoObject> GetAllImplantExtendedImplantBuildingBlocks(IBB ibb, CasePreferenceDataModel cp)
        {
            var res = new List<RhinoObject>();
            
            var extended = GetAllImplantExtendedImplantBuildingBlocks(ibb, new List<CasePreferenceDataModel>() { cp }).ToList();

            extended.ForEach(x =>
            {
                var ids = GetAllBuildingBlockIds(x).ToList();
                ids.ForEach(ibbId =>
                {
                    var rhinoObject = Director.Document.Objects.Find(ibbId);
                    res.Add(rhinoObject);
                });

            });

            return res;
        }

        public List<Guid> GetAllGuideExtendedImplantBuildingBlocksIDs(IBB ibb)
        {
            var res = new List<Guid>();

            var director = (CMFImplantDirector)Director;
            var extended = GetAllGuideExtendedImplantBuildingBlocks(ibb,director.CasePrefManager.GuidePreferences);

            extended.ForEach(x =>
            {
                var ids = GetAllBuildingBlockIds(x.Block);
                res.AddRange(ids);
            });

            return res;
        }

        public List<Guid> GetAllGuideExtendedImplantBuildingBlocksIDs(IBB ibb, GuidePreferenceDataModel prefModel)
        {
            var res = new List<Guid>();

            var extended = GetAllGuideExtendedImplantBuildingBlocks(ibb, new List<GuidePreferenceDataModel>() { prefModel });

            extended.ForEach(x =>
            {
                var ids = GetAllBuildingBlockIds(x.Block);
                res.AddRange(ids);
            });

            return res;
        }

        public List<ExtendedImplantBuildingBlock> GetImplantExtendedImplantBuildingBlocks(ICaseData casePreference)
        {
            var components = new List<ExtendedImplantBuildingBlock>();

            var allComponents = implantComponent.GetImplantComponents();

            allComponents.ToList().ForEach(ibb =>
            {
                components.Add(implantComponent.GetImplantBuildingBlock(ibb, casePreference));
            });

            return components;
        }

        public List<ExtendedImplantBuildingBlock> GetGuideExtendedImplantBuildingBlocks(ICaseData casePreference)
        {
            var components = new List<ExtendedImplantBuildingBlock>();

            var allComponents = guideComponent.GetGuideComponents();

            allComponents.ToList().ForEach(ibb =>
            {
                components.Add(guideComponent.GetGuideBuildingBlock(ibb, casePreference));
            });

            return components;
        }

        public string FindLayerNameWithRhinoObject(RhinoObject rhObject)
        {
            return Director.Document.Layers[rhObject.Attributes.LayerIndex].Name;
        }

        public CMFImplantDirector GetDirector() => (CMFImplantDirector) Director;

        public bool IsImplantComponent(RhinoObject rhinoObject)
        {
            try
            {
                var casePreferenceDataModel = GetCasePreference(rhinoObject, false);
                return (casePreferenceDataModel != null);
            }
            catch
            {
                return false;
            }
        }

        public bool IsGuideComponent(RhinoObject rhinoObject)
        {
            try
            {
                var guidePreferenceDataModel = GetGuidePreference(rhinoObject);
                return (guidePreferenceDataModel != null);
            }
            catch
            {
                return false;
            }
        }

        public static bool GetTransformationMatrixFromPart(RhinoObject rhObject, out Transform transform)
        {
            transform = Transform.Unset;

            if (rhObject == null)
            {
                return false;
            }

            transform = new Transform((Transform)rhObject.Attributes.UserDictionary[KeyTransformationMatrix]);
            return true;
        }
    }
}
