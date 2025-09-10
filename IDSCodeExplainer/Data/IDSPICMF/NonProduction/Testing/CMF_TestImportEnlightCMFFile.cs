#if (STAGING)

using IDS.CMF.V2.Loader;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.Plugin;
using IDS.Core.Utilities;
using IDS.Interface.Geometry;
using IDS.RhinoInterface.Converter;
using IDS.RhinoInterfaces.Converter;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.NonProduction
{
    [System.Runtime.InteropServices.Guid("EF7C9E55-D950-4E2F-A912-751DF1FF9BFD")]
    public class CMF_TestImportEnlightCMFFile : Command
    {
        public CMF_TestImportEnlightCMFFile()
        {
            TheCommand = this;
        }

        public static CMF_TestImportEnlightCMFFile TheCommand { get; private set; }

        public override string EnglishName => "CMF_TestImportEnlightCMFFile";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var filePath = FileUtilities.GetFileDir("Please select a MCS file", "MCS files (*.mcs)|*.mcs", "Invalid file selected or Canceled.");
            if (filePath == string.Empty)
            {
                return Result.Failure;
            }

            var loader = new EnlightCMFLoader(new IDSRhinoConsole(), filePath);

            if (!loader.LoadAllPreopProperties())
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Retrieving object properties failed.");
                return Result.Failure;
            }
            
            var preopData = loader.Import(out var objectProperties);
            if (preopData == null || objectProperties == null)
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Import failed.");
                return Result.Failure;
            }
            else if (preopData.Count != objectProperties.Count)
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Encountered error while importing.");
                return Result.Failure;
            }

            EnlightCMFLoaderUtilities.UpdatePartNames(objectProperties);
            if (preopData.Count != objectProperties.Count)
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Error while updating names.");
                return Result.Failure;
            }

            IPlane sagittalPlane;
            IPlane axialPlane;
            IPlane coronalPlane;
            IPlane midSagittalPlane;
            if (!loader.GetPlanes(out sagittalPlane, out axialPlane, out coronalPlane, out midSagittalPlane))
            {
                IDSPICMFPlugIn.WriteLine(LogCategory.Error, "Extract Planes failed.");
                return Result.Failure;
            }

            RhinoApp.WriteLine("============================================================================");
            RhinoApp.WriteLine("Refer to Layer Panel for geometry: [#]: [IDSAssignedName]");
            RhinoApp.WriteLine("[#]: [IDSAssignedName], [EnlightCMFAssignedInternalName], [EnlightCMFUiName]");

            for (var i = 0; i < preopData.Count; i++)
            {
                var data = preopData[i];
                var obj = objectProperties[i];
                AddObject(RhinoMeshConverter.ToRhinoMesh(data.Mesh), $"{i}-{obj.Name}", "Objects");
                RhinoApp.WriteLine($"{i}: {obj.Name}, {obj.InternalName}, {obj.UiName}");
            }

            RhinoApp.WriteLine("============================================================================");

            AddPlaneToDocument(sagittalPlane.ToRhinoPlane(), "SagittalPlane");
            AddPlaneToDocument(axialPlane.ToRhinoPlane(), "AxialPlane");
            AddPlaneToDocument(coronalPlane.ToRhinoPlane(), "CoronalPlane");
            AddPlaneToDocument(midSagittalPlane.ToRhinoPlane(), "MidSagittalPlane");

            doc.Views.ActiveView = doc.Views.ToDictionary(v => v.ActiveViewport.Name, v => v)[View.PerspectiveViewName];
            View.SetIDSDefaults(doc);
            RhinoApp.WriteLine("Successfully Imported.");

            return Result.Success;
        }

        private void AddPlaneToDocument(Plane plane, string name)
        {
            var size = 200;
            var span = new Interval(-size / 2.0, size / 2.0);
            var planesurface = new PlaneSurface(plane, span, span);
            var planeBrep = planesurface.ToBrep();
            AddObject(planeBrep, name, "Planes");
        }

        #region Duplicates of InternalUtilities

        private Guid AddObject(Mesh obj, string objName, string layerName, bool replaceIfExists = false)
        {
            if (obj == null)
            {
                return Guid.Empty;
            }

            var doc = RhinoDoc.ActiveDoc;

            if (replaceIfExists)
            {
                var layerIndex = doc.GetLayerWithName(layerName);
                var parentLayer = doc.Layers[layerIndex];
                var objsParent = doc.Objects.FindByLayer(parentLayer).ToList();
                foreach (var x in objsParent)
                {
                    doc.Objects.Unlock(x.Id, true);
                    doc.Objects.Delete(x.Id, true);
                }

                var objectLayers = parentLayer.GetChildren();
                if (objectLayers != null)
                {
                    foreach (var layer in objectLayers)
                    {
                        if (layer.Name == objName)
                        {
                            var objs = doc.Objects.FindByLayer(layer).ToList();
                            foreach (var x in objs)
                            {
                                doc.Objects.Unlock(x.Id, true);
                                doc.Objects.Delete(x.Id, true);
                            }
                        }
                    }
                }
            }

            if (doc.Layers.Find(layerName, false) < 0)
            {
                doc.Layers.Add(layerName, Color.Firebrick);
            }

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layerName + "::" + objName),
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial,
                Name = objName
            };

            return doc.Objects.AddMesh(obj, oa);
        }

        private Guid AddObject(Brep obj, string objName, string layerName)
        {
            if (obj == null)
            {
                return Guid.Empty;
            }

            var doc = RhinoDoc.ActiveDoc;

            if (doc.Layers.Find(layerName, false) < 0)
            {
                doc.Layers.Add(layerName, Color.Firebrick);
            }

            var oa = new ObjectAttributes
            {
                LayerIndex = doc.GetLayerWithPath(layerName + "::" + objName),
                MaterialSource = ObjectMaterialSource.MaterialFromObject,
                ColorSource = ObjectColorSource.ColorFromMaterial,
                Name = objName
            };

            return doc.Objects.AddBrep(obj, oa);
        }

        #endregion
    }
}

#endif
