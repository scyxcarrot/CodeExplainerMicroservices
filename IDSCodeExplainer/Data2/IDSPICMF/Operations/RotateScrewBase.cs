using IDS.CMF;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.CMF.Preferences;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Operations
{
    public abstract class RotateScrewBase : IDisposable
    {
        protected double maxScrewAngulationInDegrees;
        protected double length;
        protected readonly CMFImplantDirector director;
        protected Screw referenceScrew;
        protected Point3d fixedPoint;
        protected Point3d movingPoint;
        protected ScrewGaugeConduit _gaugeConduit;

        protected Mesh screwPreviewMesh;
        private Brep _screwPreview;
        protected Brep conePreview;
        protected Plane projectionPlane;
        protected double radius;
        protected Vector3d referenceDirection;
        protected double _lengthCompensated;
        protected Point3d _centerOfRotation;

        protected Brep screwPreview
        {
            get { return _screwPreview; }
            set
            {
                _screwPreview = value;
                screwPreviewMesh = MeshUtilities.AppendMeshes(Mesh.CreateFromBrep(value));
            }
        }

        protected Brep constraintPreview;
        protected readonly DisplayMaterial screwMaterial;
        protected readonly DisplayMaterial constraintMaterial;
        protected readonly bool _minimalPreviews;

        public Mesh ConstraintMesh { get; set; }

        public ImplantDataModel OldImplantDataModel { get; set; }

        protected RotateScrewBase(Screw screw, Point3d centerOfRotation, Vector3d referenceDirection) : this(screw, centerOfRotation, referenceDirection, false)
        {

        }

        protected RotateScrewBase(Screw screw, Point3d centerOfRotation, Vector3d referenceDirection, bool minimalPreviews)
        {
            director = screw.Director;
            referenceScrew = screw;
            fixedPoint = screw.HeadPoint;
            movingPoint = screw.TipPoint;
            length = (screw.HeadPoint - screw.TipPoint).Length;

            screwPreview = screw.Geometry.Duplicate() as Brep;
            screwMaterial = new DisplayMaterial(Colors.ScrewTemporary, 0.75);
            constraintMaterial = new DisplayMaterial(Color.Red, 0.5);

            this.referenceDirection = referenceDirection;

            fixedPoint = centerOfRotation;
            _centerOfRotation = centerOfRotation;

            var compensateLength = (fixedPoint - referenceScrew.HeadPoint).Length;
            _lengthCompensated = length - compensateLength;

            _minimalPreviews = minimalPreviews;
        }

        protected virtual void SetupBeforeRotate(GetPoint getPoint)
        {
            _gaugeConduit.Enabled = true;
            ConduitUtilities.RefeshConduit();
            getPoint.DynamicDraw += DynamicDraw;
            SetupConstraintPreview();
        }

        protected virtual void TeardownAfterRotated(GetPoint getPoint)
        {
            getPoint.DynamicDraw -= DynamicDraw;
            _gaugeConduit.Enabled = false;
        }

        public virtual Result Rotate(bool isImplantScrew)
        {
            return RotateToPoint(isImplantScrew);
        }

        protected virtual Result RotateToPoint(bool isImplantScrew)
        {
            var get = new GetPoint();
            var parameters = CMFPreferences.GetScrewAspectParameters().ScrewAngulationParams;
            maxScrewAngulationInDegrees = parameters.StandardAngleInDegrees;

            /*
             *  The following blank spaces is used to avoid an existing bug in Rhino 6.23
             *  Please refer to REQUIREMENT 1053130 for more information.
             */
            RhinoApp.SetCommandPromptMessage("                                                                                                                          ");
            get.SetCommandPrompt("Click on a point to rotate screw");
            get.PermitObjectSnap(false);
            get.AcceptNothing(true); // accept ENTER to confirm
            get.EnableTransparentCommands(false);
            var cancelled = false;
            OptionToggle optionToggle = new OptionToggle(false, null, null);
            int screwAngulationIndex = 0;
            while (true)
            {
                if (isImplantScrew)
                {
                    get.ClearCommandOptions();
                    optionToggle = new OptionToggle(maxScrewAngulationInDegrees == parameters.StandardAngleInDegrees,
                        parameters.MaximumAngleInDegrees.ToString(), parameters.StandardAngleInDegrees.ToString());
                    screwAngulationIndex = get.AddOptionToggle("maxScrewAngulationInDegrees", ref optionToggle);
                }

                SetupBeforeRotate(get);

                var getRes = get.Get(); // function only returns after clicking
                if (getRes == GetResult.Cancel)
                {
                    cancelled = true;
                    break;
                }

                if (getRes == GetResult.Point)
                {
                    if (UpdateScrew(get.Point()))
                    {
                        break;
                    }
                }

                if (isImplantScrew && getRes == GetResult.Option)
                {
                    if (get.OptionIndex() == screwAngulationIndex)
                    {
                        maxScrewAngulationInDegrees = optionToggle.CurrentValue
                            ? parameters.StandardAngleInDegrees
                            : parameters.MaximumAngleInDegrees;
                    }

                    TeardownAfterRotated(get);
                    continue;
                }
            }

            TeardownAfterRotated(get);
            return cancelled ? Result.Cancel : Result.Success;
        }

        protected virtual void SetupConstraintPreview()
        {
            var plane = new Plane(fixedPoint, referenceDirection);
            var trigoAngle = RhinoMath.ToRadians(maxScrewAngulationInDegrees);
            radius = Math.Tan(trigoAngle) * _lengthCompensated;
            var cone = new Cone(plane, _lengthCompensated, radius);
            var coneBrep = cone.ToBrep(true);

            var sphere = new Sphere(plane, _lengthCompensated);
            var sphereBrep = sphere.ToBrep();

            var tolerance = 0.001;
            conePreview = coneBrep.Trim(sphereBrep, tolerance)[0]; //revolved face
            constraintPreview = sphereBrep.Trim(coneBrep, tolerance)[0]; //rounded cap face

            projectionPlane = new Plane(fixedPoint + referenceDirection * _lengthCompensated, referenceDirection);
        }

        protected virtual Point3d GetPointOnConstraint(Point3d currentPoint, Point3d cameraLocation, Vector3d cameraDirection)
        {
            var points = Intersection.ProjectPointsToBreps(new List<Brep> { constraintPreview }, new List<Point3d> { currentPoint }, cameraDirection, 0.0);
            if (points != null && points.Any())
            {
                //get the nearest point to camera
                var projectedPoint = points.OrderBy(point => point.DistanceTo(cameraLocation)).First();
                return projectedPoint;
            }

            double lineParameter;
            var line = new Line(currentPoint, cameraDirection, double.MaxValue);
            if (Intersection.LinePlane(line, projectionPlane, out lineParameter))
            {
                var projectedPoint = line.PointAt(lineParameter);
                var direction = projectedPoint - projectionPlane.Origin;
                if (direction.Length < radius)
                {
                    return projectedPoint;
                }

                direction.Unitize();
                return projectionPlane.Origin + direction * radius;
            }

            return movingPoint;
        }

        protected virtual void DynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            var pointOnConstraint = GetPointOnConstraint(e.CurrentPoint, e.Viewport.CameraLocation, e.Viewport.CameraDirection);
            if (pointOnConstraint != Point3d.Unset)
            {
                var liveScrewTransform = Transform.Rotation(movingPoint - fixedPoint, pointOnConstraint - fixedPoint, fixedPoint);
                movingPoint.Transform(liveScrewTransform);
                screwPreview.Transform(liveScrewTransform);
                screwPreviewMesh.Transform(liveScrewTransform);
                OnDynamicDrawPointOnConstraint(sender, e, liveScrewTransform);
            }

            constraintPreview.DuplicateNakedEdgeCurves(true, false).ToList().ForEach(x =>
            {
                e.Display.DrawCurve(x.ToNurbsCurve(), Color.Magenta, 3);
            });

            if (_minimalPreviews)
            {
                e.Display.DrawBrepWires(screwPreview, Color.GreenYellow);
            }
            else
            {
                var silhouettes = Silhouette.Compute(screwPreviewMesh, SilhouetteType.Projecting, e.Viewport.CameraLocation, 0.1, 0.1).ToList();
                silhouettes.ForEach(x =>
                {
                    if (x.Curve != null)
                    {
                        e.Display.DrawCurve(x.Curve, Color.GreenYellow, 2);
                    }
                });
            }

            e.Display.DrawBrepShaded(screwPreview, screwMaterial);
            e.Display.DrawBrepWires(constraintPreview, Color.Magenta, 2);
            e.Display.DrawBrepWires(conePreview, Color.Magenta, 2);
        }

        protected void OnDynamicDrawPointOnConstraint(object sender, GetPointDrawEventArgs e,
            Transform transform)
        {
            _gaugeConduit.GaugesData.ForEach(x =>
            {
                if (transform != Transform.Unset)
                {
                    x.Gauge.Transform(transform);
                }
            });

            if (transform != Transform.Unset)
            {
                ConduitUtilities.RefeshConduit();
            }
        }

        protected virtual bool UpdateScrew(Point3d toPoint)
        {
            return true;
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
                screwMaterial.Dispose();
                constraintMaterial.Dispose();
            }
        }
    }
}
