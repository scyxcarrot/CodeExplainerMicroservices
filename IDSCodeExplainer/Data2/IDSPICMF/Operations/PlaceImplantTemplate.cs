using IDS.CMF;
using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.CustomMainObjects;
using IDS.CMF.DataModel;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.CMF.Visualization;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Point = System.Drawing.Point;

namespace IDS.PICMF.Operations
{
    public class PlaceImplantTemplate
    {
        private DisplayBitmap _displayBitmap;

        private int _centreX;
        private int _centreY;

        private double _scaleX = 1;
        private double _scaleY = 1;
        private double _rotationAngle = 0;

        private const double ScaleInterval = 0.1;
        private const double MinScale = 0.5;
        private const double MaxScale = 3;
        private const double AngleInterval = 1;

        private readonly ImplantTemplateDataModel _implantTemplateDataModel;
        private readonly CMFImplantDirector _director;
        private readonly CasePreferenceData _casePreferenceData;
        // The constraint meshes can be either low LoD meshes or support meshes
        private List<Mesh> _constraintMeshes;

        public ImplantDataModel ImplantDataModel { get; private set; }

        public PlaceImplantTemplate(CMFImplantDirector director,
            CasePreferenceData casePreferenceData, ImplantTemplateDataModel implantTemplateDataModel)
        {
            _director = director;
            _casePreferenceData = casePreferenceData;
            _implantTemplateDataModel = implantTemplateDataModel;
        }

        public Result Place(CasePreferenceDataModel casePreferenceDataModel)
        {
            var result = Result.Cancel;
            ImplantDataModel = null;
            var get = new GetPoint();
            get.SetCommandPrompt("Place implant template (Press <ESC>, <Enter> or <Space> to cancel the operation; Press <W/Z> to scale UP Or <S> to scale DOWN vertically; Press <D> to scale UP Or <A/Q> to scale DOWN horizontally; Press <R> or <T> to rotate)");
            get.PermitObjectSnap(false);
            get.AcceptNothing(false);
            get.EnableTransparentCommands(false);
            get.DynamicDraw += DynamicDraw;

            var mouseCallback = new IDSMouseCallback { Enabled = true };
            mouseCallback.MouseEnter += OnMouseEnter;
            mouseCallback.MouseLeave += OnMouseLeave;
            RhinoApp.KeyboardEvent += OnKeyboard;

            _constraintMeshes = GetConstraintMeshes(casePreferenceDataModel);
            _displayBitmap = DrawAllComponentOnRhinoDisplayBitmap();

            while (true)
            {
                var res = get.Get();
                if (res == GetResult.Point)
                {
                    var pointOnScreen = get.Point2d();
                    var view = RhinoDoc.ActiveDoc.Views.ActiveView;

                    if (pointOnScreen.IsEmpty)
                    {
                        IDSPluginHelper.WriteLine(LogCategory.Warning, "Not able to get the point on screen, please replace the implant template again");
                        continue;
                    }

                    ImplantDataModel = GetImplantDataModelFromImplantTemplate(_director, _casePreferenceData, view, pointOnScreen);

                    if (ImplantDataModel == null)
                    {
                        continue;
                    }

                    result = Result.Success;
                }
                break;
            }

            get.DynamicDraw -= DynamicDraw;

            RhinoApp.KeyboardEvent -= OnKeyboard;
            mouseCallback.Enabled = false;
            mouseCallback.MouseEnter -= OnMouseEnter;
            mouseCallback.MouseLeave -= OnMouseLeave;

            return result;
        }

        private DisplayBitmap DrawAllComponentOnRhinoDisplayBitmap()
        {
            var implantTemplateBitmapDrawer = new ImplantTemplateBitmapDrawer(_scaleX, _scaleY, _rotationAngle);
            implantTemplateBitmapDrawer.DrawFromImplantTemplateDataModel(_implantTemplateDataModel, false);
            var bitmap = implantTemplateBitmapDrawer.LatestBitmap;

            _centreX = bitmap.Width / 2;
            _centreY = bitmap.Height / 2;

            return new DisplayBitmap(bitmap);
        }

        private void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            var view = RhinoDoc.ActiveDoc.Views.ActiveView;

            if (view == null)
            {
                return;
            }

            if (!GetCursorPos(out var pointOnScreen) || !ScreenToClient(view.Handle, ref pointOnScreen))
            {
                return;
            }

            e.Display.DrawBitmap(_displayBitmap, pointOnScreen.X - _centreX, pointOnScreen.Y - _centreY);
        }

        private ImplantDataModel GetImplantDataModelFromImplantTemplate(CMFImplantDirector director,
            CasePreferenceData casePreferenceData, RhinoView view, Point mousePointOnScreen)
        {
            var implantDataModel = new ImplantDataModel();
            var dotList = new List<IDot>();

            foreach (var implantTemplateScrew in _implantTemplateDataModel.Screws)
            {
                var pointOnBitmap = new Point(implantTemplateScrew.X, implantTemplateScrew.Y);
                var transformedPoint = PointUtilities.ScaleThenRotate2dPoint(pointOnBitmap, _scaleX, _scaleY, _rotationAngle);
                transformedPoint.Offset(mousePointOnScreen);

                var pointOnWorld = GetPickedPoint(view.ActiveViewport, transformedPoint, _constraintMeshes, out var lowLoDConstraintMesh);
                if (!pointOnWorld.IsValid)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Some point in implant template failed to project on the meshes, please replace the implant template again");
                    return null;
                }

                var averageNormal = VectorUtilities.FindAverageNormal(lowLoDConstraintMesh, pointOnWorld, ScrewAngulationConstants.AverageNormalRadiusPastille);
                var dot = CreateDotPastille(casePreferenceData, pointOnWorld, averageNormal);

                if (dot == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to create dot, please replace the implant template again");
                    return null;
                }
                dotList.Add(dot);
            }

            foreach (var implantTemplateConnection in _implantTemplateDataModel.Connections)
            {
                var connection = CreateConnection(casePreferenceData, dotList[implantTemplateConnection.A],
                    dotList[implantTemplateConnection.B], implantTemplateConnection.Type);
                if (connection == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to create connection, please replace the implant template again");
                    return null;
                }

                implantDataModel.ConnectionList.Add(connection);
            }

            return implantDataModel;
        }

        private List<Mesh> GetConstraintMeshes(CasePreferenceDataModel casePreferenceDataModel)
        {
            var objectManager = new CMFObjectManager(_director);
            var implantSupportManager = new ImplantSupportManager(objectManager);
            var implantSupportRhObj = implantSupportManager.GetImplantSupportRhObj(casePreferenceDataModel);

            var targetMeshes = new List<Mesh>();

            if (implantSupportRhObj != null)
            {
                const double transparencyValue = 0.75;
               
                var material = implantSupportRhObj.GetMaterial(true);
                material.Transparency = transparencyValue;
                material.CommitChanges();

                var layerIndex = implantSupportRhObj.Attributes.LayerIndex;
                var layer = _director.Document.Layers[layerIndex];
                layer.CommitChanges();

                if (!layer.IsVisible && layer.IsValid)
                {
                    _director.Document.Layers.ForceLayerVisible(layer.Id);
                }

                targetMeshes.Add((Mesh)implantSupportRhObj.Geometry);
            }
            else
            {
                var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
                targetMeshes = constraintMeshQuery.GetVisibleConstraintMeshesForImplant(true).ToList();
            }

            return targetMeshes;
        }

        private Point3d GetPickedPoint(RhinoViewport viewPort, Point pointOnScreen, IEnumerable<Mesh> constraintMeshes, out Mesh lowLoDConstraintMesh)
        {
            var picker = new PickContext();
            picker.View = viewPort.ParentView;
            picker.PickStyle = PickStyle.PointPick;
            var xform = viewPort.GetPickTransform(pointOnScreen);
            picker.SetPickTransform(xform);

            var pointOnWorld = Point3d.Unset;
            lowLoDConstraintMesh = null;
            var refDepth = double.MinValue;

            foreach (var mesh in constraintMeshes)
            {
                double distance;
                Point3d hitPoint;
                PickContext.MeshHitFlag hitFlag;
                int hitIndex;
                double depth;

                var cond1 = !picker.PickFrustumTest(mesh, PickContext.MeshPickStyle.ShadedModePicking, out hitPoint,
                    out depth, out distance, out hitFlag, out hitIndex);
                var cond2 = !(Math.Abs(distance) < double.Epsilon);
                var t3 = !(depth > refDepth);
                
                if (cond1 || cond2 || t3)
                {
                    continue;
                }

                refDepth = depth;
                pointOnWorld = hitPoint;
                lowLoDConstraintMesh = mesh;
            }

            return pointOnWorld;
        }

        protected IDot CreateDotPastille(CasePreferenceData casePreferenceData, Point3d point, Vector3d normal)
        {
            normal.Unitize();

            return DataModelUtilities.CreateDotPastille(point, normal, 
                casePreferenceData.PlateThicknessMm, casePreferenceData.PastilleDiameter);
        }

        protected IConnection CreateConnection(CasePreferenceData casePreferenceData, 
            IDot dotA, IDot dotB, ImplantTemplateConnectionType type)
        {
            switch (type)
            {
                case ImplantTemplateConnectionType.Plate:
                    return new ConnectionPlate
                    {
                        A = dotA,
                        B = dotB,
                        Thickness = casePreferenceData.PlateThicknessMm,
                        Width = casePreferenceData.PlateWidthMm,
                        Id = Guid.NewGuid(),
                    };
                case ImplantTemplateConnectionType.Link:
                    return new ConnectionLink
                    {
                        A = dotA,
                        B = dotB,
                        Thickness = casePreferenceData.PlateThicknessMm,
                        Width = casePreferenceData.LinkWidthMm,
                        Id = Guid.NewGuid(),
                    };
                default:
                    return null;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern short GetKeyState(int keyCode);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point point);

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Point point);

        private static bool IsKeyDown(int key)
        {
            short retVal = GetKeyState(key);

            //If the high-order bit is 1, the key is down
            //otherwise, it is up.
            if ((retVal & 0x8000) == 0x8000)
            {
                return true;
            }
            //If the low-order bit is 1, the key is toggled.
            return false;
        }

        protected void OnKeyboard(int key)
        {
            if (!IsKeyDown(key))
                return;

            switch ((Keys)key)
            {
                case Keys.W:
                case Keys.Z:
                    _scaleY += ScaleInterval;
                    break;
                case Keys.S:
                    _scaleY -= ScaleInterval;
                    break;
                case Keys.D:
                    _scaleX += ScaleInterval;
                    break;
                case Keys.A:
                case Keys.Q:
                    _scaleX -= ScaleInterval;
                    break;
                case Keys.R:
                    _rotationAngle += AngleInterval;
                    break;
                case Keys.T:
                    _rotationAngle -= AngleInterval;
                    break;
                default:
                    return;
            }

            _scaleX = _scaleX >= MaxScale ? MaxScale : _scaleX <= MinScale ? MinScale : _scaleX;
            _scaleY = _scaleY >= MaxScale ? MaxScale : _scaleY <= MinScale ? MinScale : _scaleY;
            _rotationAngle = _rotationAngle > 360 ? _rotationAngle - 360 :
                _rotationAngle < 0 ? 360 + _rotationAngle : _rotationAngle;

            _displayBitmap = DrawAllComponentOnRhinoDisplayBitmap();
            RhinoDoc.ActiveDoc.Views.Redraw();
        }

        private void OnMouseEnter(MouseCallbackEventArgs e)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
            RhinoApp.KeyboardEvent += OnKeyboard;
        }

        private void OnMouseLeave(MouseCallbackEventArgs e)
        {
            RhinoApp.KeyboardEvent -= OnKeyboard;
        }

        private Point3d TransformPointOnScreenToWorld(RhinoView view, Point pointOnScreen)
        {
            var screenToWorldTransform = view.ActiveViewport.GetTransform(CoordinateSystem.Screen, CoordinateSystem.World);
            var pointOnWorld = new Point3d(pointOnScreen.X, pointOnScreen.Y, 0.0);
            pointOnWorld.Transform(screenToWorldTransform);
            return pointOnWorld;
        }
    }
}