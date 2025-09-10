using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino;
using Rhino.Geometry;
using System;

namespace IDS.Glenius.Operations
{
    public delegate void ValueChangedEventHandler();

    public class HeadAlignment
    {
        public event ValueChangedEventHandler ValueChanged;

        private const double Epsilon = 0.001;

        private readonly Plane anatomicalCoordinateSystem;
        private readonly double glenoidInclination;

        private Vector3d coronalNormal => anatomicalCoordinateSystem.XAxis;
        private Vector3d axialNormal => anatomicalCoordinateSystem.YAxis;
        private Vector3d sagittalNormal => defectIsLeft ? anatomicalCoordinateSystem.ZAxis : -anatomicalCoordinateSystem.ZAxis;

        private readonly GleniusObjectManager objectManager;
        private readonly RhinoDoc document;
        private readonly bool defectIsLeft;

        private Plane headCoordinateSystem
        {
            get
            {
                var head = objectManager.GetBuildingBlock(IBB.Head) as Head;
                return head.CoordinateSystem;
            }
        }

        private readonly bool transformHead;
        private double localInclination;
        private double localVersion;

        public HeadAlignment(AnatomicalMeasurements anatomicalInfo, GleniusObjectManager objectManager, RhinoDoc document, bool defectIsLeft)
        {
            this.objectManager = objectManager;
            this.document = document;
            
            this.anatomicalCoordinateSystem = new Plane(anatomicalInfo.PlGlenoid.Origin, anatomicalInfo.AxAp, anatomicalInfo.AxIs);
            this.glenoidInclination = anatomicalInfo.GlenoidInclinationValue;
            this.defectIsLeft = defectIsLeft;

            localInclination = 0;
            localVersion = 0;

            //backward compatibility
            this.transformHead = false;
            var head = objectManager.GetBuildingBlock(IBB.Head) as Head;
            if (head.Attributes.UserDictionary.ContainsKey("inclination"))
            {
                var inferiorSuperior = head.Attributes.UserDictionary.GetDouble("inferior_superior");
                var medialLateral = head.Attributes.UserDictionary.GetDouble("medial_lateral");
                var anteriorPosterior = head.Attributes.UserDictionary.GetDouble("anterior_posterior");
                var inclination = head.Attributes.UserDictionary.GetDouble("inclination");
                var version = head.Attributes.UserDictionary.GetDouble("version");
                SetInferiorSuperiorPosition(inferiorSuperior);
                SetMedialLateralPosition(medialLateral);
                SetAnteriorPosteriorPosition(anteriorPosterior);
                SetInclinationAngle(inclination);
                SetVersionAngle(version);
            }
            this.transformHead = true;
        }

        public void AlignHeadToDefaultPosition()
        {
            AlignHeadToInitialPosition();

            //restrict between -10 & 0
            var defaultInclination = glenoidInclination - 10;
            if (defaultInclination < -10)
            {
                defaultInclination = -10;
            }
            else if (defaultInclination > 0)
            {
                defaultInclination = 0;
            }
            else { }

            SetInclinationAngle(defaultInclination);
        }

        public Plane GetHeadCoordinateSystem()
        {
            var coordinateSystem = headCoordinateSystem;
            return coordinateSystem;
        }
        
        public Vector3d GetHeadComponentVectorProjectedToCoronalPlane()
        {
            var projectionPlane = new Plane(anatomicalCoordinateSystem.Origin, coronalNormal);
            return GetProjectedHeadVector(projectionPlane);
        }

        public Vector3d GetHeadComponentVectorProjectedToAxialPlane()
        {
            var projectionPlane = new Plane(anatomicalCoordinateSystem.Origin, axialNormal);
            return GetProjectedHeadVector(projectionPlane);
        }

        #region Position

        #region InferiorSuperior

        public double GetInferiorSuperiorPosition()
        {
            var plane = new Plane(anatomicalCoordinateSystem.Origin, axialNormal);
            return GetPosition(plane);
        }

        public void SetInferiorSuperiorPosition(double value)
        {
            var plane = new Plane(anatomicalCoordinateSystem.Origin, axialNormal);
            SetPosition(plane, value);
        }

        public void IncrementDecrementInferiorSuperiorPosition(double change)
        {
            UpdatePosition(axialNormal, change);
        }

        #endregion

        #region MedialLateral

        public double GetMedialLateralPosition()
        {
            var plane = new Plane(anatomicalCoordinateSystem.Origin, sagittalNormal);
            return GetPosition(plane);
        }

        public void SetMedialLateralPosition(double value)
        {
            var plane = new Plane(anatomicalCoordinateSystem.Origin, sagittalNormal);
            SetPosition(plane, value);
        }

        public void IncrementDecrementMedialLateralPosition(double change)
        {
            UpdatePosition(sagittalNormal, change);
        }

        #endregion

        #region AnteriorPosterior

        public double GetAnteriorPosteriorPosition()
        {
            var plane = new Plane(anatomicalCoordinateSystem.Origin, coronalNormal);
            return GetPosition(plane);
        }     
        
        public void SetAnteriorPosteriorPosition(double value)
        {
            var plane = new Plane(anatomicalCoordinateSystem.Origin, coronalNormal);
            SetPosition(plane, value);
        }

        public void IncrementDecrementAnteriorPosteriorPosition(double change)
        {
            UpdatePosition(coronalNormal, change);
        }

        #endregion

        private double GetPosition(Plane plane)
        {
            var value = plane.DistanceTo(headCoordinateSystem.Origin);
            return value;
        }

        private void SetPosition(Plane plane, double value)
        {
            var currentValue = GetPosition(plane);
            var diff = value - currentValue;
            UpdatePosition(plane.Normal, diff);
        }

        private void UpdatePosition(Vector3d normal, double diff)
        {
            var direction = Vector3d.Multiply(normal, diff);
            var transform = Transform.Translation(direction);
            TransformHead(transform);
        }

        #endregion

        #region Orientation

        public double GetInclinationAngle()
        {
            var projectionPlane = new Plane(anatomicalCoordinateSystem.Origin, coronalNormal);
            var positivePlane = new Plane(anatomicalCoordinateSystem.Origin, axialNormal);
            double orientation;
            if (GetOrientation(projectionPlane, positivePlane, out orientation))
            {
                localInclination = orientation;
            }
            return localInclination;
        }

        public double GetVersionAngle()
        {
            var projectionPlane = new Plane(anatomicalCoordinateSystem.Origin, axialNormal);
            var positivePlane = new Plane(anatomicalCoordinateSystem.Origin, -coronalNormal);
            double orientation;
            if (GetOrientation(projectionPlane, positivePlane, out orientation))
            {
                localVersion = orientation;
            }
            return localVersion;
        }

        public void SetInclinationAngle(double value)
        {
            var versionValue = GetVersionAngle();
            var transform = SetOrientations(value, versionValue);
            TransformHead(transform);
        }

        public void IncrementDecrementInclinationAngle(double change)
        {
            var currentValue = GetInclinationAngle();
            var newValue = currentValue + change;
            SetInclinationAngle(newValue);
        }

        public void SetVersionAngle(double value)
        {
            var inclinationValue = GetInclinationAngle();
            var transform = SetOrientations(inclinationValue, value);
            TransformHead(transform);
        }

        public void IncrementDecrementVersionAngle(double change)
        {
            var currentValue = GetVersionAngle();
            var newValue = currentValue + change;
            SetVersionAngle(newValue);
        }

        private bool GetOrientation(Plane projectionPlane, Plane positivePlane, out double orientation)
        {
            var projectedHeadVector = GetProjectedHeadVector(projectionPlane);
            if (projectedHeadVector == Vector3d.Unset)
            {
                orientation = double.NaN;
                return false;
            }

            var valueInRadian = Vector3d.VectorAngle(projectedHeadVector, sagittalNormal);
            var value = RhinoMath.ToDegrees(valueInRadian);

            if (Math.Abs(value) < Epsilon)
            {
                value = 0;
            }

            var endHead = Point3d.Add(anatomicalCoordinateSystem.Origin, -headCoordinateSystem.ZAxis);
            var positive = (positivePlane.DistanceTo(endHead) >= 0);
            orientation = positive ? value : -value;
            return true;
        }

        private Transform SetOrientations(double inclinationValue, double versionValue)
        {
            var oldOrientation = MathUtilities.GleniusAnteversionInclinationToVector(GetVersionAngle(), GetInclinationAngle(), anatomicalCoordinateSystem, defectIsLeft);
            var orientation = MathUtilities.GleniusAnteversionInclinationToVector(versionValue, inclinationValue, anatomicalCoordinateSystem, defectIsLeft);
            localInclination = inclinationValue;
            localVersion = versionValue;
            return Transform.Rotation(oldOrientation, orientation, headCoordinateSystem.Origin);
        }

        private Vector3d GetProjectedHeadVector(Plane projectionPlane)
        {
            if (headCoordinateSystem.ZAxis.IsParallelTo(projectionPlane.Normal) != 0)
            {
                //return Vector3d.Unset if parallel
                return Vector3d.Unset;
            }

            var projectedHeadVector = -headCoordinateSystem.ZAxis - (Vector3d.Multiply(-headCoordinateSystem.ZAxis, projectionPlane.Normal) * projectionPlane.Normal);
            if (!projectedHeadVector.IsUnitVector)
            {
                projectedHeadVector.Unitize();
            }
            return projectedHeadVector;
        }

        #endregion

        private void AlignHeadToInitialPosition()
        {
            var changeBasisTransform = Transform.ChangeBasis(anatomicalCoordinateSystem, Plane.WorldXY);
            var zRotationTransform = Transform.Identity;
            if (defectIsLeft)
            {
                zRotationTransform = Transform.Rotation(RhinoMath.ToRadians(180), Plane.WorldXY.YAxis, Plane.WorldXY.Origin);
            }

            var transform = changeBasisTransform * zRotationTransform;
            TransformHead(transform);
        }

        private void TransformHead(Transform transform, bool realignment = false)
        {
            var head = objectManager.GetBuildingBlock(IBB.Head) as Head;
            if (head != null)
            {
                head.IsRealignment = realignment;
                if (transformHead)
                {
                    objectManager.TransformBuildingBlock(IBB.Head, transform);
                }
                ValueChanged?.Invoke();
            }

            document.Views.Redraw();
        }
    }
}