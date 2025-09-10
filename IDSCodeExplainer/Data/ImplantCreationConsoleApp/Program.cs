using IDS.CMFImplantCreation.DTO;
using IDS.CMFImplantCreation.Helpers;
using IDS.Core.V2.Geometries;
using IDS.Core.V2.Geometry;
using IDS.Interface.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ImplantCreationConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();

            Console.WriteLine("ENTER to finish");
            Console.ReadLine();
        }

        static async Task Run()
        {
            try
            {
                var factory = new ImplantFactory(new LoggingConsole("Factory"));

                var rootFolderPath = $@"<RootFolder>";
                var directory = new DirectoryInfo(rootFolderPath);
                var implantFolders = directory.GetDirectories();

                var messages = new List<string>();
                var timeTakenInSecondsFromCreators = 0.0;

                var componentTimeTakenInSeconds = new Dictionary<string, Dictionary<string, double>>();

                var timer = new Stopwatch();
                timer.Start();

                foreach (var implantFolder in implantFolders)
                {
                    messages.Add($"{implantFolder.Name}");

                    var subFolders = implantFolder.GetDirectories();

                    foreach (var pastilleFolder in subFolders)
                    {
                        if (ExtractPastilleComponentInfo($@"{pastilleFolder.FullName}\PastilleComponentInfo.json", out var componentInfo))
                        {
                            var task = Task.Run(() => factory.CreateImplantAsync(componentInfo));

                            var taskResult = await task;

                            StlUtilitiesV2.IDSMeshToStlBinary(taskResult.FinalComponentMesh, $@"{pastilleFolder.FullName}\{componentInfo.DisplayName}.stl");

                            messages.Add($"{pastilleFolder.Name}: {taskResult.TimeTakenInSeconds}s");
                            timeTakenInSecondsFromCreators += taskResult.TimeTakenInSeconds;

                            componentTimeTakenInSeconds.Add(componentInfo.DisplayName, taskResult.ComponentTimeTakenInSeconds);
                        }
                    }
                }

                timer.Stop();
                var timeTakenInSeconds = timer.ElapsedMilliseconds * 0.001;

                Console.WriteLine("Completed");
                foreach (var message in messages)
                {
                    Console.WriteLine(message);
                }

                foreach (var keyValuePair in componentTimeTakenInSeconds)
                {
                    Console.WriteLine(keyValuePair.Key);

                    foreach (var dictionary in keyValuePair.Value)
                    {
                        Console.WriteLine($"{dictionary.Key}: {dictionary.Value}s");
                    }

                    Console.WriteLine();
                }

                Console.WriteLine($"TimeTakenInSecondsFromCreators: {timeTakenInSecondsFromCreators}s");
                Console.WriteLine($"TimeTakenInSeconds: {timeTakenInSeconds}s");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception thrown: {e.Message}");
            }
        }

        private static PastilleFileIOComponentInfo CreatePastilleComponentInfo(string name)
        {
            var outputLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var folderPath = Path.Combine(outputLocation, "Stls");

            var component = new PastilleFileIOComponentInfo
            {
                DisplayName = $"{name}.1.Genioplasty",
                IsActual = true,
                ScrewHeadPoint = new IDSPoint3D(8.5687476805783742, 6.1171395091486929, 0.0),
                ScrewDirection = new IDSVector3D(0.0, 0.0, 1.0),
                Location = new IDSPoint3D(8.5687476805783742, 6.1171395091486929, 0.0),
                Direction = new IDSVector3D(0.0, 0.0, 1.0),
                Diameter = 5.2,
                Thickness = 1.0,
                ScrewType = "Matrix Orthognathic Ø1.85",
                ComponentMeshesSTLFilePaths = new List<string>
                {
                    $@"{folderPath}\IntermediateLandmark.stl"
                },
                ClearanceMeshSTLFilePath = $@"{folderPath}\ImplantSupport_I1.stl",
                SupportRoIMeshSTLFilePath = $@"{folderPath}\ImplantSupport_I1.stl",
                SubtractorsSTLFilePaths = new List<string>
                {
                    $@"{folderPath}\Stamp-I1_2.stl"
                },
                NeedToFinalize = true
            };

            return component;
        }

        private static bool ExtractPastilleComponentInfo(string filePath, out PastilleFileIOComponentInfo componentInfo)
        {
            var parsed = true;
            componentInfo = null;

            try
            {
                var jsonText = File.ReadAllText(filePath);
                componentInfo = JsonConvert.DeserializeObject<PastilleFileIOComponentInfo>(jsonText, new JsonSerializerSettings()
                {
                    Converters = new List<JsonConverter>
                    {
                        new Point3DConverter(),
                        new Vector3DConverter()
                    }
                });
            }
            catch
            {
                parsed = false;
            }

            return parsed;
        }

        //Below is the reference code snippets to export PastilleComponentInfo information to be used by Run() method
        /*private void WriteToDisk(PastilleComponentInfo componentInfo)
        {
            var folderPath = $@"<RootFolder>\<CaseName>\{componentInfo.DisplayName}";

            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }

            var fileIOComponent = new PastilleFileIOComponentInfo
            {
                DisplayName = componentInfo.DisplayName,
                IsActual = componentInfo.IsActual,
                ScrewType = componentInfo.ScrewType,
                Direction = componentInfo.Direction,
                Thickness = componentInfo.Thickness,
                Location = componentInfo.Location,
                Diameter = componentInfo.Diameter,
                ScrewHeadPoint = componentInfo.ScrewHeadPoint,
                ScrewDirection = componentInfo.ScrewDirection,
                NeedToFinalize = componentInfo.NeedToFinalize
            };

            if (componentInfo.ComponentMeshes != null && componentInfo.ComponentMeshes.Any())
            {
                var sTLFilePaths = new List<string>();

                for (var i = 0; i < componentInfo.ComponentMeshes.Count; i++)
                {
                    var mesh = componentInfo.ComponentMeshes[i];
                    var filePath = $@"{folderPath}\ComponentMeshes-{i}.stl";
                    Core.V2.Geometry.StlUtilitiesV2.IDSMeshToStlBinary(mesh, filePath);
                    sTLFilePaths.Add(filePath);
                }

                fileIOComponent.ComponentMeshesSTLFilePaths = sTLFilePaths;
            }

            if (componentInfo.Subtractors != null && componentInfo.Subtractors.Any())
            {
                var sTLFilePaths = new List<string>();

                for (var i = 0; i < componentInfo.Subtractors.Count; i++)
                {
                    var mesh = componentInfo.Subtractors[i];
                    var filePath = $@"{folderPath}\Subtractors-{i}.stl";
                    Core.V2.Geometry.StlUtilitiesV2.IDSMeshToStlBinary(mesh, filePath);
                    sTLFilePaths.Add(filePath);
                }

                fileIOComponent.SubtractorsSTLFilePaths = sTLFilePaths;
            }

            if (componentInfo.ClearanceMesh != null)
            {
                var filePath = $@"{folderPath}\ClearanceMesh.stl";
                Core.V2.Geometry.StlUtilitiesV2.IDSMeshToStlBinary(componentInfo.ClearanceMesh, filePath);
                fileIOComponent.ClearanceMeshSTLFilePath = filePath;
            }

            if (componentInfo.SupportRoIMesh != null)
            {
                var filePath = $@"{folderPath}\SupportRoIMesh.stl";
                Core.V2.Geometry.StlUtilitiesV2.IDSMeshToStlBinary(componentInfo.SupportRoIMesh, filePath);
                fileIOComponent.SupportRoIMeshSTLFilePath = filePath;
            }

            using (var file = File.CreateText($@"{folderPath}\PastilleComponentInfo.json"))
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(fileIOComponent, Newtonsoft.Json.Formatting.Indented);
                file.Write(json);
            }
        }*/
    }

    public class Point3DConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IPoint3D));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, typeof(IDSPoint3D));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value, typeof(IDSPoint3D));
        }
    }

    public class Vector3DConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(IVector3D));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, typeof(IDSVector3D));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value, typeof(IDSVector3D));
        }
    }
}
