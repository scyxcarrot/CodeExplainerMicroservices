using IDS.Core.ImplantDirector;
using IDS.Core.Utilities;
using IDS.Core.Visualization;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Operations
{
    public class MeasurementCreator
    {
        public class MeasurementsDataModel
        {
            public Point3d Pt1 = Point3d.Unset;
            public Point3d Pt2 = Point3d.Unset;
        }

        private class PickFrustumTestDataModel
        {
            public RhinoObject RhinoObjectTest;
            public BoundingBox BoundingBoxTest;
            public Mesh MeshTest;
        }
        
        private readonly IImplantDirector _director;
        private List<MeasurementsDataModel> _measurementPts = new List<MeasurementsDataModel>();
        private readonly Dictionary<Guid, PickFrustumTestDataModel> _pickFrustumTestDataModel;

        private MeasurementsDataModel _activeMeasurementDataModel = null;

        private bool _updatableLayerTable = true;
        private Point3d _ptTemporary = Point3d.Unset;

        public MeasurementCreator(IImplantDirector director)
        {
            _director = director;
            _pickFrustumTestDataModel = new Dictionary<Guid, PickFrustumTestDataModel>();
            // Initial dictionary, add without check if exist
            foreach (var rhinoObject in _director.Document.Objects)
            {
                AddNewMesh(rhinoObject);
                AddNewBrep(rhinoObject);
            }
        }

        private void AddNewMesh(RhinoObject rhinoObject)
        {
            if (rhinoObject.Geometry is Mesh)
            {
                _pickFrustumTestDataModel.Add(rhinoObject.Id, new PickFrustumTestDataModel()
                {
                    RhinoObjectTest = rhinoObject,
                    BoundingBoxTest = rhinoObject.Geometry.GetBoundingBox(true),
                    MeshTest = rhinoObject.Geometry as Mesh,
                });
            }
        }

        private void AddNewBrep(RhinoObject rhinoObject)
        {
            if (rhinoObject.Geometry is Brep)
            {
                _pickFrustumTestDataModel.Add(rhinoObject.Id, new PickFrustumTestDataModel()
                {
                    RhinoObjectTest = rhinoObject,
                    BoundingBoxTest = rhinoObject.Geometry.GetBoundingBox(true),
                    MeshTest = null
                });
            }
        }

        private Point3d GetPickedPoint(RhinoViewport activeViewport, System.Drawing.Point selectedPoint)
        {
            var picker = new PickContext();
            picker.View = activeViewport.ParentView;
            picker.PickStyle = PickStyle.PointPick;
            var xform = activeViewport.GetPickTransform(selectedPoint);
            picker.SetPickTransform(xform);

            var pointPicked = Point3d.Unset;
            var refDepth = double.MinValue;

            foreach (var pickDataModelValue in _pickFrustumTestDataModel.Values)
            {
                if (!pickDataModelValue.RhinoObjectTest.Visible)
                {
                    continue;
                }

                bool boxCompletelyInFrustum;
                if (!picker.PickFrustumTest(pickDataModelValue.BoundingBoxTest, out boxCompletelyInFrustum))
                {
                    continue;
                }

                if ((pickDataModelValue.MeshTest == null) &&
                    (pickDataModelValue.RhinoObjectTest.Geometry is Brep))
                {
                    var brep = pickDataModelValue.RhinoObjectTest.Geometry as Brep;
                    var brepMesh = ConvertBrepToMesh(brep);
                    if (brep != null)
                    {
                        pickDataModelValue.MeshTest = brepMesh;
                    }
                }

                double distance;
                Point3d hitPoint;
                PickContext.MeshHitFlag hitFlag;
                int hitIndex;
                double depth;
                var mesh = pickDataModelValue.MeshTest;

                var t1 = !picker.PickFrustumTest(mesh, PickContext.MeshPickStyle.ShadedModePicking, out hitPoint,
                    out depth, out distance, out hitFlag, out hitIndex);
                var t2 = !(Math.Abs(distance) < double.Epsilon);
                var t3 = !(depth > refDepth);

                //depth returned here for point picks LARGER values are NEARER to the camera. SMALLER values are FARTHER from the camera.
                if (t1 || t2 || t3)
                {
                    continue;
                }

                refDepth = depth;
                pointPicked = hitPoint;
            }

            return pointPicked;
        }

        private Mesh ConvertBrepToMesh(Brep brep)
        {
            return MeshUtilities.AppendMeshes(Mesh.CreateFromBrep(brep, MeshParameters.IDS()));
        }

        private Mesh GetConstraintMesh()
        {
            var visibleMeshObjects = _director.Document.Objects.
                Where(x => x.Visible && x.Geometry is Mesh).Select(x => (Mesh)x.Geometry).ToList();

            var visibleBrepMeshedObjects = _director.Document.Objects.
                Where(x => x.Visible && x.Geometry is Brep).Select(x =>
                    MeshUtilities.AppendMeshes(Mesh.CreateFromBrep((Brep)x.Geometry, MeshParameters.IDS()))).ToList();

            visibleMeshObjects.AddRange(visibleBrepMeshedObjects);

            var res = MeshUtilities.AppendMeshes(visibleMeshObjects);

            return res ?? new Mesh();
        }

        public bool Execute()
        {
            _measurementPts = new List<MeasurementsDataModel>();

            var isFirstPoint = true;

            var getPt = new GetPoint();
            
            getPt.AcceptNothing(true);
            getPt.DynamicDraw += GetPt_DynamicDraw;
            getPt.MouseMove += GetPt_MouseMove;

            RhinoDoc.LayerTableEvent += LayerTableEvent;

            while (true)
            {
                if (_measurementPts.Any())
                {
                    _activeMeasurementDataModel = isFirstPoint ? new MeasurementsDataModel() : _measurementPts.Last();
                }
                else
                {
                    _activeMeasurementDataModel = new MeasurementsDataModel();
                }

                var res1 = getPt.Get(true);

                if (res1 != GetResult.Point)
                {
                    RemoveIncompleteMeasurements();
                    _activeMeasurementDataModel = null;
                    break;
                }

                var pt = GetPickedPoint(getPt.View().ActiveViewport, getPt.Point2d());
                if(pt == Point3d.Unset)
                    continue;

                if (isFirstPoint)
                {
                    _activeMeasurementDataModel.Pt1 = pt;
                    _measurementPts.Add(_activeMeasurementDataModel);
                    isFirstPoint = false;
                }
                else
                {
                    _activeMeasurementDataModel.Pt2 = pt;
                    isFirstPoint = true;


                    if (_activeMeasurementDataModel.Pt1.DistanceTo(_activeMeasurementDataModel.Pt2) > 0.0001)
                    {
                        _updatableLayerTable = false;
                        var dim = CreateDimension(_activeMeasurementDataModel.Pt1, _activeMeasurementDataModel.Pt2);

                        var objName =
                            $"Distance {StringUtilities.DoubleStringify(_activeMeasurementDataModel.Pt1.DistanceTo(_activeMeasurementDataModel.Pt2), 2)}mm";
                        var layering = $"Measurements - {_director.CurrentDesignPhaseName}" + "::" + objName;

                        var oa = new ObjectAttributes
                        {
                            LayerIndex = _director.Document.GetLayerWithPath(layering),
                            ObjectColor = Color.Black,
                            Name = objName,
                        };

                        var guid = _director.Document.Objects.Add(dim, oa);

                        DimensionVisualizer.Instance.InvalidateConduits(_director.Document);

                        _director.Document.Views.Redraw();

                        _updatableLayerTable = true;
                    }
                }
            }

            RhinoDoc.LayerTableEvent -= LayerTableEvent;
            getPt.DynamicDraw -= GetPt_DynamicDraw;
            _activeMeasurementDataModel = null;

            return true;
        }

        private void GetPt_DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            if (_activeMeasurementDataModel != null && _activeMeasurementDataModel.Pt1 != Point3d.Unset &&
                _ptTemporary != Point3d.Unset && _activeMeasurementDataModel.Pt1.DistanceTo(_ptTemporary) > 0.0001)
            {
                var dim = CreateDimension(_activeMeasurementDataModel.Pt1, _ptTemporary);
                e.Display.DrawAnnotation(dim, Color.BlueViolet);
            }
        }

        private void GetPt_MouseMove(object sender, GetPointMouseEventArgs e)
        {
            if (_activeMeasurementDataModel != null && _activeMeasurementDataModel.Pt1 != Point3d.Unset)
            {
                var pointPicked = GetPickedPoint(e.Viewport, e.WindowPoint);
                if (pointPicked != Point3d.Unset)
                {
                    _ptTemporary = pointPicked;
                }
            }
        }

        private void LayerTableEvent(object sender, LayerTableEventArgs e)
        {
            if (_updatableLayerTable)
            {
                foreach (var rhinoObject in _director.Document.Objects)
                {
                    if (!_pickFrustumTestDataModel.ContainsKey(rhinoObject.Id))
                    {
                        AddNewMesh(rhinoObject);
                        AddNewBrep(rhinoObject);
                    }
                }
            }
        }

        private LinearDimension CreateDimension(Point3d pt1, Point3d pt2)
        {
            return RhinoObjectUtilities.CreateDimension(pt1, pt2, _director.Document);
        }

        public List<MeasurementsDataModel> GetMeasurements()
        {
            return _measurementPts;
        }

        private void RemoveIncompleteMeasurements()
        {
            _measurementPts.RemoveAll(x => x == null);
            _measurementPts.RemoveAll(x => x.Pt1 == Point3d.Unset || x.Pt2 == Point3d.Unset);
            _measurementPts.RemoveAll(x => x.Pt1.DistanceTo(x.Pt2) < 0.0001);
        }
    }
}
