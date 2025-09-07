using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Rhino.Geometry;
using System;
using System.IO;
using JsonUtilities = IDS.Core.V2.Utilities.JsonUtilities;

namespace IDS.CMF.TestLib.Components
{
    public class MeshComponent
    {
        public class ConfigFromStl
        {
            public string StlFileName { get; set; }

            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public string Dir { get; set; } = null;

            private string GetFolderDirectory(string workDir)
            {
                return $"{workDir}\\Meshes";
            }

            public void ReadFromStl(string workDir, out Mesh mesh)
            {
                var directory = !string.IsNullOrEmpty(Dir) ?
                    Dir : GetFolderDirectory(workDir);

                if (!StlUtilities.StlBinary2RhinoMesh($"{directory}\\{StlFileName}", out mesh))
                {
                    throw new IDSException($"Failed to load {StlFileName}");
                }
            }

            public void WriteToStl(string workDir, Mesh mesh)
            {
                var directory = !string.IsNullOrEmpty(Dir)? 
                    Dir : GetFolderDirectory(workDir);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                StlUtilities.RhinoMesh2StlBinary(mesh, $"{directory}\\{StlFileName}");
            }
        }

        public class ConfigFromBox
        {
            public IDSPoint3D MinPoint3d { get; set; }

            public IDSPoint3D MaxPoint3d { get; set; }

            public double Resolution { get; set; }

            public void ReadFromConfig(out Mesh mesh)
            {
                var min = RhinoPoint3dConverter.ToPoint3d(MinPoint3d);
                var max = RhinoPoint3dConverter.ToPoint3d(MaxPoint3d);
                var xLength = Math.Abs(max.X - min.X);
                var yLength = Math.Abs(max.Y - min.Y);
                var zLength = Math.Abs(max.Z - min.Z);
                var xCount = Convert.ToInt32(xLength / Resolution);
                var yCount = Convert.ToInt32(yLength / Resolution);
                var zCount = Convert.ToInt32(zLength / Resolution);
                mesh = Mesh.CreateFromBox(new BoundingBox(min, max), xCount, yCount, zCount);
            }
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public MeshType Type { get; set; } = MeshType.FromStl;

        public string Config { get; set; }

        public void ParseFromComponent(string workDir, out Mesh mesh)
        {
            mesh = null;

            switch (Type)
            {
                case MeshType.FromStl:
                    var configFromStl = JsonUtilities.Deserialize<ConfigFromStl>(Config);
                    if (configFromStl == null)
                    {
                        throw new IDSException("Failed to deserialize ConfigFromStl");
                    }
                    configFromStl.ReadFromStl(workDir, out mesh);
                    break;
                case MeshType.FromBox:
                    var configFromBox = JsonUtilities.Deserialize<ConfigFromBox>(Config);
                    if (configFromBox == null)
                    {
                        throw new IDSException("Failed to deserialize ConfigFromBox");
                    }
                    configFromBox.ReadFromConfig(out mesh);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        // If load from existing implant director, it will keep as stl
        public void FillToComponent(string fileName, string workDir, Mesh mesh)
        {
            Type = MeshType.FromStl;
            var config = new ConfigFromStl()
            {
                StlFileName = fileName
            };
            config.WriteToStl(workDir, mesh);
            Config = JsonUtilities.Serialize(config);
        }
    }
}
