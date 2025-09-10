using IDS.Amace;
using IDS.Amace.Enumerators;
using IDS.Amace.ImplantBuildingBlocks;
using IDS.Common;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace IDS.NonProduction.Commands
{
#if (INTERNAL)

    [System.Runtime.InteropServices.Guid("C2E049E2-8FAB-44EA-BF5C-434825932698")]
    [IDSCommandAttributes(true, DesignPhase.Any)]
    public class AMace_TestEditScrewDatabase : Command
    {
        public AMace_TestEditScrewDatabase()
        {
            Instance = this;
        }

        public static AMace_TestEditScrewDatabase Instance { get; private set; }

        public override string EnglishName => "AMace_TestEditScrewDatabase";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var director = new ImplantDirector(doc, PlugInInfo.PluginModel);

            //Get Brand
            var getBrandOption = new GetOption();
            getBrandOption.SetCommandPrompt("Choose screw brand.");
            getBrandOption.AcceptNothing(true);

            var selectedBrand = director.GetCurrentScrewBrand();
            var screwDatabaseQuery = new ScrewDatabaseQuery();
            var availableScrewBrands = screwDatabaseQuery.GetAvailableScrewBrands().ToList();
            var currentScrewBrandIndex = availableScrewBrands.IndexOf(selectedBrand);
            getBrandOption.AddOptionList("Brand", availableScrewBrands, currentScrewBrandIndex);

            while (true)
            {
                var res = getBrandOption.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Option)
                {
                    selectedBrand = availableScrewBrands[getBrandOption.Option().CurrentListOptionIndex];
                }
            }
            var rc = getBrandOption.CommandResult();
            if (rc != Result.Success)
            {
                return Result.Cancel;
            }

            //Get Type
            var getTypeOption = new GetOption();
            getTypeOption.SetCommandPrompt("Choose screw type.");
            getTypeOption.AcceptNothing(true);

            var selectedType = screwDatabaseQuery.GetDefaultScrewType(selectedBrand);
            var availableScrewTypes = screwDatabaseQuery.GetAvailableScrewTypes(selectedBrand).ToList();
            var currentScrewTypeIndex = availableScrewTypes.IndexOf(selectedType);
            getTypeOption.AddOptionList("Type", availableScrewTypes, currentScrewTypeIndex);

            while (true)
            {
                var res = getTypeOption.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Option)
                {
                    selectedType = availableScrewTypes[getTypeOption.Option().CurrentListOptionIndex];
                }
            }
            rc = getTypeOption.CommandResult();
            if (rc != Result.Success)
            {
                return Result.Cancel;
            }

            var screwBrandType = ScrewBrandType.Parse(selectedBrand, selectedType);

            //Get Diameter & Calibration offset
            var diameter = screwBrandType.Diameter;
            var calibrationOffset =
                ScrewAideManager.GetHeadAndHeadCalibrationOffset(director.ScrewDatabase, screwBrandType);
            
            var getParametersOption = new GetOption();
            getParametersOption.SetCommandPrompt("Choose screw diameter and calibration offset");
            getParametersOption.AcceptNothing(true);
            var optionDiameter = new OptionDouble(diameter, 0.1, 9.9);
            var optionDiameterId = getParametersOption.AddOptionDouble("Diameter", ref optionDiameter);
            var optionCalibrationOffset = new OptionDouble(calibrationOffset, 0.0, 9.9);
            var optionCalibrationOffsetId = getParametersOption.AddOptionDouble("CalibrationOffset", ref optionCalibrationOffset);

            while (true)
            {
                var res = getParametersOption.Get();
                if (res == GetResult.Cancel)
                {
                    return Result.Cancel;
                }

                if (res == GetResult.Nothing)
                {
                    break;
                }
                if (res == GetResult.Option)
                {
                    var optId = getParametersOption.OptionIndex();
                    if (optId == optionDiameterId)
                    {
                        diameter = optionDiameter.CurrentValue;
                    }
                    else if (optId == optionCalibrationOffsetId)
                    {
                        calibrationOffset = optionCalibrationOffset.CurrentValue;
                    }
                }
            }
            rc = getParametersOption.CommandResult();
            if (rc != Result.Success)
            {
                return Result.Cancel;
            }

            //Get output folder
            var dialog = new FolderBrowserDialog();
            dialog.Description = "Please select an output folder for screw database";
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Cancel;
            }

            var folderPath = Path.GetFullPath(dialog.SelectedPath);

            var resources = new AmaceResources();
            var screwDatabase3dm = File3dm.Read(resources.ScrewDatabasePath);
            var screwDatabaseXml = new XmlDocument();
            screwDatabaseXml.Load(resources.ScrewDatabaseXmlPath);

            //Save a copy
            var screwDatabase3dmPath = $"{folderPath}\\IDS_Screw_Database_Full.3dm";
            var screwDatabaseXmlPath = $"{folderPath}\\Screw_Database.xml";
            screwDatabase3dm.Write(screwDatabase3dmPath, new File3dmWriteOptions());
            screwDatabaseXml.Save(screwDatabaseXmlPath);
            screwDatabase3dm.Dispose();

            //Edit
            var newScrewBrandType = new ScrewBrandType(screwBrandType.Brand, diameter, screwBrandType.Locking);
            EditDiameterInXml(screwDatabaseXmlPath, screwBrandType, newScrewBrandType);
            EditValuesIn3dm(screwDatabase3dmPath, screwBrandType, newScrewBrandType, calibrationOffset);

            SystemTools.OpenExplorerInFolder(folderPath);

            RhinoApp.WriteLine("Screw Databases were generated to the following folder:");
            RhinoApp.WriteLine("{0}", folderPath);
            return Result.Success;
        }

        private void EditDiameterInXml(string path, ScrewBrandType oldScrewBrandType, ScrewBrandType newScrewBrandType)
        {
            if (Math.Abs(oldScrewBrandType.Diameter - newScrewBrandType.Diameter) > 0.0001)
            {
                var screwDatabaseXml = new XmlDocument();
                screwDatabaseXml.Load(path);

                //update default type (if applicable) and type name
                var brandNode =
                    screwDatabaseXml.SelectSingleNode($"/screws/brands/brand[name='{newScrewBrandType.Brand}']");
                if (brandNode != null)
                {
                    var defaultTypeNode = brandNode.SelectSingleNode($"./defaultType[.='{oldScrewBrandType.Type}']");
                    if (defaultTypeNode != null)
                    {
                        defaultTypeNode.InnerXml = newScrewBrandType.Type;
                    }

                    var typeNode = brandNode.SelectSingleNode($"./types/type/name[.='{oldScrewBrandType.Type}']");
                    if (typeNode != null)
                    {
                        typeNode.InnerXml = newScrewBrandType.Type;
                    }
                    screwDatabaseXml.Save(path);
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error,
                        $"Could not find brand {newScrewBrandType.Brand} in {path}.");
                }
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"No change to diameter in {path}.");
            }
        }

        private void EditValuesIn3dm(string path, ScrewBrandType oldScrewBrandType, ScrewBrandType newScrewBrandType, double calibrationOffset)
        {
            var screwDatabase3dm = File3dm.Read(path);

            var screwBrandTypeFile3dmObject = GetObject(screwDatabase3dm, oldScrewBrandType);

            if (screwBrandTypeFile3dmObject.IsValid())
            {
                var updateHeadRelatedEntities = false;
                var diameterChanged = false;
                var headCurve = screwBrandTypeFile3dmObject.Head.EntityCurve;
                var headCalibrationCurve = screwBrandTypeFile3dmObject.HeadCalibration.EntityCurve;

                if (Math.Abs(oldScrewBrandType.Diameter - newScrewBrandType.Diameter) > 0.0001)
                {
                    //update type, curve, brep, mesh of head and head calibration
                    var brandTypeLayer = screwDatabase3dm.Layers.FirstOrDefault(layer => string.Equals(layer.Name,
                        oldScrewBrandType.ToString(), StringComparison.InvariantCultureIgnoreCase));
                    if (brandTypeLayer != null)
                    {
                        brandTypeLayer.Name = newScrewBrandType.ToString();
                        brandTypeLayer.CommitChanges();
                    }

                    headCurve = ChangeCurveDiameter(headCurve, newScrewBrandType.Diameter);
                    headCalibrationCurve = ChangeCurveDiameter(headCalibrationCurve, newScrewBrandType.Diameter);

                    updateHeadRelatedEntities = true;
                    diameterChanged = true;
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"No change to diameter.");
                }

                var headOrigin = ScrewDatabaseDataRepository.Get().GetHeadPoint(oldScrewBrandType);
                var headCalibrationOrigin = ScrewDatabaseDataRepository.Get().GetHeadCalibrationPoint(oldScrewBrandType);
                var currentOffset = headOrigin.Z - headCalibrationOrigin.Z;
                if (Math.Abs(Math.Abs(currentOffset) - calibrationOffset) > 0.0001)
                {
                    var newOffset = (0 - currentOffset) - calibrationOffset;
                    headCurve = ChangeCurveOffset(headCurve, newOffset, headCalibrationCurve.PointAtEnd);
                    updateHeadRelatedEntities = true;
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"No change to calibration offset.");
                }

                var listToDelete = new List<Guid>();
                if (updateHeadRelatedEntities)
                {
                    AddFile3dmObjects(screwDatabase3dm, screwBrandTypeFile3dmObject.Head, headCurve, $"{newScrewBrandType}", "HEAD");
                    listToDelete.Add(screwBrandTypeFile3dmObject.Head.EntityCurveObject.Attributes.ObjectId);
                    listToDelete.Add(screwBrandTypeFile3dmObject.Head.EntityBrepObject.Attributes.ObjectId);
                    listToDelete.Add(screwBrandTypeFile3dmObject.Head.EntityMeshObject.Attributes.ObjectId);
                }

                if (diameterChanged)
                {
                    AddFile3dmObjects(screwDatabase3dm, screwBrandTypeFile3dmObject.HeadCalibration, headCalibrationCurve, $"{newScrewBrandType}", "HEAD_CALIBRATION");
                    listToDelete.Add(screwBrandTypeFile3dmObject.HeadCalibration.EntityCurveObject.Attributes.ObjectId);
                    listToDelete.Add(screwBrandTypeFile3dmObject.HeadCalibration.EntityBrepObject.Attributes.ObjectId);
                    listToDelete.Add(screwBrandTypeFile3dmObject.HeadCalibration.EntityMeshObject.Attributes.ObjectId);
                }

                if (updateHeadRelatedEntities)
                {
                    var deleted = screwDatabase3dm.Objects.Delete(listToDelete);
                    if (deleted != listToDelete.Count)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Error,
                            $"There are {listToDelete.Count - deleted} old head/head calibration object(s) not deleted.");
                    }

                    if (diameterChanged)
                    {
                        UpdateScrewAidesName(screwDatabase3dm, $"{oldScrewBrandType}", $"{newScrewBrandType}");
                    }

                    screwDatabase3dm.Write(path, new File3dmWriteOptions());
                }
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"Could not find objects in database.");
            }

            screwDatabase3dm.Dispose();
        }

        private ScrewBrandTypeFile3dmObject GetObject(File3dm database, ScrewBrandType screwBrandType)
        {
            var headCurveTag = $"{screwBrandType}_CONTOUR_HEAD";
            var headCurveObj = database.Objects.FirstOrDefault(rhobj => string.Equals(rhobj.Attributes.Name,
                headCurveTag,
                StringComparison.InvariantCultureIgnoreCase));
            var headCurve = headCurveObj?.Geometry as Curve;

            var headBrepTag = $"{screwBrandType}_SURFACE_HEAD";
            var headBrepObj = database.Objects.FirstOrDefault(rhobj => string.Equals(
                rhobj.Attributes.Name,
                headBrepTag, StringComparison.InvariantCultureIgnoreCase));
            var headBrep = headBrepObj?.Geometry as Brep;

            var headMeshTag = $"{screwBrandType}_MESH_HEAD";
            var headMeshObj = database.Objects.FirstOrDefault(rhobj => string.Equals(
                rhobj.Attributes.Name,
                headMeshTag, StringComparison.InvariantCultureIgnoreCase));
            var headMesh = headMeshObj?.Geometry as Mesh;

            var headCalibrationCurveTag = $"{screwBrandType}_CONTOUR_HEAD_CALIBRATION";
            var headCalibrationCurveObj = database.Objects.FirstOrDefault(rhobj => string.Equals(
                rhobj.Attributes.Name,
                headCalibrationCurveTag, StringComparison.InvariantCultureIgnoreCase));
            var headCalibrationCurve = headCalibrationCurveObj?.Geometry as Curve;

            var headCalibrationBrepTag = $"{screwBrandType}_SURFACE_HEAD_CALIBRATION";
            var headCalibrationBrepObj = database.Objects.FirstOrDefault(rhobj => string.Equals(
                rhobj.Attributes.Name,
                headCalibrationBrepTag, StringComparison.InvariantCultureIgnoreCase));
            var headCalibrationBrep = headCalibrationBrepObj?.Geometry as Brep;

            var headCalibrationMeshTag = $"{screwBrandType}_MESH_HEAD_CALIBRATION";
            var headCalibrationMeshObj = database.Objects.FirstOrDefault(rhobj => string.Equals(
                rhobj.Attributes.Name,
                headCalibrationMeshTag, StringComparison.InvariantCultureIgnoreCase));
            var headCalibrationMesh = headCalibrationMeshObj?.Geometry as Mesh;

            var screwBrandTypeObject = new ScrewBrandTypeFile3dmObject
            {
                Head = new EntityFile3dmObject
                {
                    EntityCurveObject = headCurveObj,
                    EntityBrepObject = headBrepObj,
                    EntityMeshObject = headMeshObj,
                    EntityCurve = headCurve,
                    EntityBrep = headBrep,
                    EntityMesh = headMesh,
                },
                HeadCalibration = new EntityFile3dmObject
                {
                    EntityCurveObject = headCalibrationCurveObj,
                    EntityBrepObject = headCalibrationBrepObj,
                    EntityMeshObject = headCalibrationMeshObj,
                    EntityCurve = headCalibrationCurve,
                    EntityBrep = headCalibrationBrep,
                    EntityMesh = headCalibrationMesh
                }
            };
            return screwBrandTypeObject;
        }

        private Curve ChangeCurveDiameter(Curve curve, double diameter)
        {
            var radius = diameter / 2;
            var nurbsCurve = curve.ToNurbsCurve();
            var points = nurbsCurve.Points.Select(point => point.Location).ToList();
            var index = 0;
            var furthestXPoint = Point3d.Origin;
            for (var i = 0; i < points.Count; i++)
            {
                var point = points[i];
                if (point.X > furthestXPoint.X)
                {
                    furthestXPoint = point;
                    index = i;
                }
            }

            var controlPoints = new List<Point3d>(points);
            if (index == points.Count - 1)
            {
                controlPoints.RemoveAt(points.Count - 1);
            }
            else
            {
                controlPoints.RemoveRange(index, points.Count - index);
            }
            var lastPoint = points.Last();
            controlPoints.Add(new Point3d(radius, lastPoint.Y, lastPoint.Z));

            var newCurve = Curve.CreateControlPointCurve(controlPoints);
            return newCurve;
        }

        private Curve ChangeCurveOffset(Curve curve, double offset, Point3d lastPoint)
        {
            var nurbsCurve = curve.ToNurbsCurve();
            nurbsCurve.Transform(Transform.Translation(new Vector3d(0, 0, offset)));

            var points = nurbsCurve.Points.Select(point => point.Location).ToList();
            points.RemoveAt(points.Count - 1);
            points.Add(lastPoint);

            var controlPoints = new List<Point3d>(points);
            var newCurve = Curve.CreateControlPointCurve(controlPoints);
            return newCurve;
        }

        private void AddFile3dmObjects(File3dm database, EntityFile3dmObject entityFileObj, Curve curve, string screwBrandType, string suffix)
        {
            var curveAttr = entityFileObj.EntityCurveObject.Attributes.Duplicate();
            var brepAttr = entityFileObj.EntityBrepObject.Attributes.Duplicate();
            var meshAttr = entityFileObj.EntityMeshObject.Attributes.Duplicate();
            
            curveAttr.Name = $"{screwBrandType}_CONTOUR_{suffix}";
            database.Objects.AddCurve(curve, curveAttr);

            var brep = CreateBrepFromCurve(curve);
            brepAttr.Name = $"{screwBrandType}_SURFACE_{suffix}";
            database.Objects.AddBrep(brep, brepAttr);

            var mesh = CreateMeshFromBrep(brep);
            meshAttr.Name = $"{screwBrandType}_MESH_{suffix}";
            database.Objects.AddMesh(mesh, meshAttr);
        }

        private Brep CreateBrepFromCurve(Curve curve)
        {
            var screwAxis = -Vector3d.ZAxis;
            var rotationAxis = new Line(Point3d.Origin, (Point3d)screwAxis);
            var revSurf = RevSurface.Create(curve, rotationAxis);
            var brep = Brep.CreateFromRevSurface(revSurf, true, true);
            return brep;
        }

        private Mesh CreateMeshFromBrep(Brep brep)
        {
            var meshparameters = MeshParameters.IDS();
            var meshParts = Mesh.CreateFromBrep(brep, meshparameters);
            var mesh = new Mesh();
            foreach (var part in meshParts)
            {
                mesh.Append(part);
            }
            return mesh;
        }

        private void UpdateScrewAidesName(File3dm database, string oldScrewBrandType, string newScrewBrandType)
        {
            var listToDelete = new List<Guid>();

            var objects = database.Objects.Where(rhobj => rhobj.Attributes.Name.StartsWith(
                oldScrewBrandType.ToString(),
                StringComparison.InvariantCultureIgnoreCase)).ToList();

            //update names
            foreach (var obj in objects)
            {
                var attr = obj.Attributes.Duplicate();
                attr.Name = attr.Name.Replace($"{oldScrewBrandType}", $"{newScrewBrandType}");
                switch (obj.Geometry.ObjectType)
                {
                    case ObjectType.Brep:
                        database.Objects.AddBrep(obj.Geometry.Duplicate() as Brep, attr);
                        listToDelete.Add(obj.Attributes.ObjectId);
                        break;
                    case ObjectType.Curve:
                        database.Objects.AddCurve(obj.Geometry.Duplicate() as Curve, attr);
                        listToDelete.Add(obj.Attributes.ObjectId);
                        break;
                    case ObjectType.Mesh:
                        database.Objects.AddMesh(obj.Geometry.Duplicate() as Mesh, attr);
                        listToDelete.Add(obj.Attributes.ObjectId);
                        break;
                }
            }

            var aidesDeleted = database.Objects.Delete(listToDelete);
            if (aidesDeleted != listToDelete.Count)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error,
                    $"There are {listToDelete.Count - aidesDeleted} old screw aide object(s) not deleted.");
            }
        }

        public class ScrewBrandTypeFile3dmObject
        {
            public EntityFile3dmObject Head;
            public EntityFile3dmObject HeadCalibration;

            public bool IsValid()
            {
                return Head.IsValid() && HeadCalibration.IsValid();
            }
        }

        public class EntityFile3dmObject
        {
            public File3dmObject EntityCurveObject;
            public File3dmObject EntityBrepObject;
            public File3dmObject EntityMeshObject;

            public Curve EntityCurve;
            public Brep EntityBrep;
            public Mesh EntityMesh;

            public bool IsValid()
            {
                return EntityCurveObject != null && EntityBrepObject != null && EntityMeshObject != null
                       && EntityCurve != null && EntityBrep != null && EntityMesh != null;
            }
        }
    }

#endif
}
