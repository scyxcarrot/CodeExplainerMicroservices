using IDS.Glenius.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Windows.Input;
using System;

namespace IDS.Glenius.Operations
{
    public abstract class TranslateObjectAlongHeadPlaneXY: IDisposable
    {
        protected readonly string objectName;

        protected GleniusImplantDirector director;
        protected GleniusObjectManager objectManager;
        protected GeometryBase objectPreview;
        protected Plane objectCoordinateSystem;
        protected Point3d objectPreviewOrigin;

        protected DisplayMaterial material;
        private readonly Plane translationPlane;

        public TranslateObjectAlongHeadPlaneXY(GleniusImplantDirector director, string name)
        {
            objectName = name;

            this.director = director;
            objectManager = new GleniusObjectManager(director);
            material = new DisplayMaterial(Colors.Metal, 0.75);

            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document, director.defectIsLeft);
            var headCoordinateSystem = headAlignment.GetHeadCoordinateSystem();
            translationPlane = new Plane(headCoordinateSystem.Origin, headCoordinateSystem.XAxis, headCoordinateSystem.YAxis);

            objectCoordinateSystem = new Plane(headCoordinateSystem.Origin, headCoordinateSystem.XAxis, headCoordinateSystem.YAxis);
        }

        public Result Translate()
        {
            return TranslateToPoint();
        }

        protected abstract bool IsWithinRangeOfMovement(double offsetFromOrigin);

        protected abstract void TransformBuildingBlocks(Transform transform);

        private Result TranslateToPoint()
        {
            var get = new GetPoint();
            get.SetCommandPrompt(string.Format("Click on a point to move {0}", objectName));
            get.DynamicDraw += DynamicDraw;
            get.AcceptNothing(true); // accept ENTER to confirm
            get.EnableTransparentCommands(false);
            var cancelled = false;
            while (true)
            {
                GetResult get_res = get.Get(); // function only returns after clicking
                if (get_res == GetResult.Cancel)
                {
                    cancelled = true;
                    break;
                }

                if (get_res == GetResult.Point)
                {
                    TransformObject(get.Point());
                    break;
                }
            }
            get.DynamicDraw -= DynamicDraw;

            return cancelled ? Result.Cancel : Result.Success;
        }

        private void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            var transform = GetTransform(objectPreviewOrigin, e.CurrentPoint, e.Viewport.CameraDirection);
            objectPreviewOrigin.Transform(transform);
            objectPreview.Transform(transform);
            switch (objectPreview.ObjectType)
            {
                case ObjectType.Mesh:
                    e.Display.DrawMeshShaded(objectPreview as Mesh, material);
                    break;
                case ObjectType.Brep:
                    e.Display.DrawBrepShaded(objectPreview as Brep, material);
                    break;
                default:
                    break;
            }
        }

        private void TransformObject(Point3d toPoint)
        {
            var transform = GetTransform(objectCoordinateSystem.Origin, toPoint, RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.CameraDirection);

            TransformBuildingBlocks(transform);

            director.Document.Views.Redraw();
        }

        private Transform GetTransform(Point3d referencePoint, Point3d currentPoint, Vector3d cameraDirection)
        {
            var cameraPlane = new Plane(translationPlane.Origin, cameraDirection);

            var projectedOrigin = translationPlane.ClosestPoint(referencePoint);

            var projectedPoint = cameraPlane.ClosestPoint(currentPoint);
            projectedPoint = translationPlane.ClosestPoint(projectedPoint);

            if (!CanTransform(projectedPoint))
            {
                return Transform.Identity;
            }

            return Transform.Translation(projectedPoint - projectedOrigin);
        }

        private bool CanTransform(Point3d currentPoint)
        {
            var motion = currentPoint - translationPlane.Origin;
            var withinRange = IsWithinRangeOfMovement(motion.Length);
            if (!withinRange)
            {
                Mouse.SetCursor(Cursors.No);
            }

            return withinRange;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                material.Dispose();
            }
        }
    }
}