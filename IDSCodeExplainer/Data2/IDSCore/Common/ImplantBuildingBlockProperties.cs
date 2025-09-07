using IDS.Core.Enumerators;
using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Render;
using System.Drawing;
using System.Linq;

namespace IDS.Core.ImplantBuildingBlocks
{
    public static class ImplantBuildingBlockProperties
    {
        /// <summary>
        /// The key of the block type
        /// </summary>
        public const string KeyBlockType = "block_type";

        /// <summary>
        /// The Generic layer name
        /// </summary>
        public const string GenericLayerName = "General";
        
        /// <summary>
        /// Adds the block attributes.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="rhobj">The rhobj.</param>
        /// <param name="creationTime">if set to <c>true</c> [creation time].</param>
        public static void AddBlockAttributes(ImplantBuildingBlock block, RhinoObject rhobj, bool creationTime = false)
        {
            // Always exit in normal way
            if (null == rhobj)
            {
                return;
            }

            // Get a reference to the object's attributes
            var oa = rhobj.Attributes;
            GetBlockAttributes(block, rhobj.Document, rhobj.Geometry, ref oa);
            rhobj.CommitChanges();
        }

        /// <summary>
        /// Gets the block attributes.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="doc">The document.</param>
        /// <param name="geom">The geom.</param>
        /// <param name="oa">The oa.</param>
        public static void GetBlockAttributes(ImplantBuildingBlock block, RhinoDoc doc, GeometryBase geom, ref ObjectAttributes oa)
        {
            // Get a reference to the object's attributes
            if (null == oa)
            {
                oa = doc.CreateDefaultAttributes();
            }

            // Add custom user data
            oa.UserDictionary.Set(KeyBlockType, block.Name);
            IDSPluginHelper.AddUserData(block, geom, oa);

            // Assign it to the correct layer
            oa.LayerIndex = GetLayer(block, doc);
            oa.Name = block.Name;

            // Locked or unlocked mode
            oa.Mode = ObjectMode.Locked;

            // Materials and plot colors
            var midx = GetMaterial(block, doc);
            oa.MaterialIndex = midx;
            var blockColor = block.Color;
            oa.MaterialSource = ObjectMaterialSource.MaterialFromObject;
            oa.ColorSource = ObjectColorSource.ColorFromMaterial;
            switch (block.Name)
            {
                case "DefectPelvis":
                    break;

                case "ReamedPelvis":
                    break;

                case "CupReamedPelvis":
                    break;

                case "DesignPelvis":
                    break;

                default:
                    // Default mode is "Normal" (not locked)
                    oa.ObjectColor = blockColor;
                    break;
            }
        }

        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public static Color GetColor(ImplantBuildingBlock block)
        {
            return !block.Color.IsEmpty ? block.Color : Color.Crimson;
        }

        /// <summary>
        /// Gets the export ready.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="director">The director.</param>
        /// <param name="exportDir">The export dir.</param>
        /// <param name="filePrefix">The file prefix.</param>
        /// <param name="fileSuffix">The file suffix.</param>
        /// <param name="exportMesh">The export mesh.</param>
        /// <param name="exportFilename">The export filename.</param>
        /// <param name="meshColor">Color of the mesh.</param>
        /// <returns></returns>
        public static bool GetExportReady(ImplantBuildingBlock block, IImplantDirector director, string exportDir, string filePrefix, string fileSuffix, out Mesh exportMesh, out string exportFilename, out int[] meshColor)
        {
            // init
            exportFilename = string.Empty;
            var description = string.Empty;

            if (!GetExportReady(block, director, out exportMesh, out description, out meshColor))
            {
                return false;
            }

            // Filename
            exportFilename = GetExportFilename(filePrefix, fileSuffix, description);
            if (exportDir != "")
            {
                exportFilename = exportDir + "\\" + exportFilename;
            }            
            
            return true;
        }

        private static bool GetExportReady(ImplantBuildingBlock block, IImplantDirector director, out Mesh exportMesh, out string description, out int[] meshColor)
        {
            // init
            meshColor = new int[3];
            exportMesh = new Mesh();
            description = "";
            var mp = MeshParameters.IDS();

            // Get all building blocks in document
            description = GetExportName(block);
            var objectManager = new ObjectManager(director);
            var blockObjs = objectManager.GetAllBuildingBlocks(block);
            if (!blockObjs.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Block <{0}> not found in document. Skipping.", description);
                return false;
            }

            // Mesh color
            var blockColor = GetColor(block);
            meshColor = new int[] { blockColor.R, blockColor.G, blockColor.B };

            // Get the meshes for export
            foreach (var theBlockObj in blockObjs)
            {
                if (theBlockObj is MeshObject)
                {
                    exportMesh.Append((Mesh)theBlockObj.Geometry);
                }
                else if (theBlockObj is BrepObject)
                {
                    var tempBrep = (Brep)theBlockObj.Geometry;
                    exportMesh.Append(tempBrep.GetCollisionMesh(mp));
                }
                else
                {
                    // not supported yet
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Block <{0}> could not be exported. Unsupported format. Skipping.", description);
                }
            }

            // Check if mesh actually contains something
            if (exportMesh.Vertices.Count > 0)
            {
                return true;
            }

            meshColor = new int[3];
            description = "";
            return false;

            // Success
        }

        private static string GetExportFilename(string filePrefix, string fileSuffix, string description)
        {
            var exportFilename = $"{description.Replace(' ', '_')}";

            if (!string.IsNullOrEmpty(filePrefix))
            {
                exportFilename = $"{filePrefix}_{exportFilename}";
            }

            if (!string.IsNullOrEmpty(fileSuffix))
            {
                exportFilename = $"{exportFilename}_{fileSuffix}";
            }

            return $"{exportFilename}.stl";
        }

        /// <summary>
        /// Gets the layer.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        public static int GetLayer(ImplantBuildingBlock block, RhinoDoc doc)
        {
            if (string.IsNullOrEmpty(block.Layer))
            {
                return doc.GetLayerWithName(GenericLayerName);
            }

            var layerpath = block.Layer;
            return doc.GetLayerWithPath(layerpath);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        public static string GetName(ImplantBuildingBlock block)
        {
            return block.Name;
        }

        /// <summary>
        /// Gets the name of the export.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <returns></returns>
        private static string GetExportName(ImplantBuildingBlock block)
        {
            return !string.IsNullOrEmpty(block.ExportName) ? block.ExportName : "Unknown_ExportName";
        }

        /// <summary>
        /// Gets the material.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        private static int GetMaterial(ImplantBuildingBlock block, RhinoDoc doc)
        {
            var midx = doc.Materials.Find(block.Name, true);
            if (midx >= 0)
            {
                return midx;
            }

            midx = doc.Materials.Add();
            var mat = doc.Materials[midx];

            // Set material properties
            mat.Name = block.Name;
            var bcol = GetColor(block);
            switch (block.Name)
            {
                default:
                    mat.DiffuseColor = bcol;
                    mat.SpecularColor = bcol;
                    mat.AmbientColor = bcol;
                    mat.Reflectivity = 0.0;
                    mat.Transparency = 0.0;
                    mat.Shine = 0.0;
                    break;
            }
            mat.CommitChanges();

            // Create a render material associated with this material
            RenderMaterial rmat = RenderMaterial.CreateBasicMaterial(mat);
            rmat.Name = block.Name;
            return midx;
        }

        /// <summary>
        /// Resets the transparencies.
        /// </summary>
        /// <param name="doc">The document.</param>
        public static void ResetTransparencies(RhinoDoc doc)
        {
            foreach (var mat in doc.Materials)
            {
                mat.Transparency = 0.0;
                mat.CommitChanges();
            }
        }

        /// <summary>
        /// Sets the transparency.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="doc">The document.</param>
        /// <param name="transp">The transp.</param>
        public static void SetTransparency(ImplantBuildingBlock block, RhinoDoc doc, double transp = 0.0)
        {
            foreach (var mat in doc.Materials)
            {
                if (mat.Name != block.Name)
                {
                    continue;
                }

                mat.Transparency = transp;
                mat.CommitChanges();
            }
        }
    }
}