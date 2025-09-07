using IDS.Operations.CupPositioning;
using Rhino;
using Rhino.Collections;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.DocObjects.Custom;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using IDS.Amace.Relations;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.Operations;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
#if (INTERNAL)
using System.Drawing;
using IDS.Core.NonProduction;
#endif

namespace IDS.Amace.ImplantBuildingBlocks
{
    /// <summary>
    /// The solid acetabular cup as NURBS geometry.
    /// </summary>
    /// <seealso cref="Rhino.DocObjects.Custom.CustomBrepObject" />
    /// <seealso cref="IBBinterface{T}" />
    public class Cup : CustomBrepObject, IBBinterface<ImplantDirector>
    {
        private const double Epsilon = 0.0001;
        /// <summary>
        /// The anteversion default
        /// </summary>
        public const double anteversionDefault = 20.0;

        /// <summary>
        /// The aperture angle default
        /// </summary>
        public const double apertureAngleDefault = 170;

        /// <summary>
        /// The aperture angle maximum
        /// </summary>
        public const double apertureAngleMax = 180;

        /// <summary>
        /// The aperture angle minimum
        /// </summary>
        public const double apertureAngleMin = 170;

        private CupType _cupType;

        /// <summary>
        /// The horizontal border width
        /// </summary>
        private static double horizontalBorderWidth = 2;

        /// <summary>
        /// The inclination default
        /// </summary>
        public const double inclinationDefault = 40.0;

        /// <summary>
        /// The inner diameter default
        /// </summary>
        public const double innerDiameterDefault = 54.0;

        /// <summary>
        /// The inner diameter maximum
        /// </summary>
        public const double innerDiameterMax = 80.0;

        /// <summary>
        /// The inner diameter minimum
        /// </summary>
        public const double innerDiameterMin = 40.0;

        /// <summary>
        /// The porous thickness default
        /// </summary>
        public const double porousThicknessDefault = 1.0;

        /// <summary>
        /// The porous thickness maximum
        /// </summary>
        private const double porousThicknessMax = 50.0;

        /// <summary>
        /// The porous thickness minimum
        /// </summary>
        private const double porousThicknessMin = 0.0;

        /// <summary>
        /// The smooth design medial aperture
        /// </summary>
        private const double smoothDesignMedialAperture = 144.0;

        /// <summary>
        /// The thickness default
        /// </summary>
        public const double thicknessDefault = 4.0;

        /// <summary>
        /// The thickness maximum
        /// </summary>
        private const double thicknessMax = 5.0;

        /// <summary>
        /// The thickness minimum
        /// </summary>
        private const double thicknessMin = 1.0;

        /// <summary>
        /// The transition end arc length with polishing offset
        /// </summary>
        private const double referenceEndArcLength = 8.0;

        /// <summary>
        /// The default reaming height
        /// </summary>
        private const double defaultReamingHeight = 80.0;

        /// <summary>
        /// The extended medial aperture
        /// </summary>
        private const double extendedMedialAperture = 153.0;

        /// <summary>
        /// The key anteversion
        /// </summary>
        private const string keyAnteversion = "anteversion";

        /// <summary>
        /// The key aperture angle
        /// </summary>
        private const string keyApertureAngle = "aperture_angle";

        /// <summary>
        /// The key center of rotation
        /// </summary>
        private const string keyCenterOfRotation = "cor";

        /// <summary>
        /// The key inclination
        /// </summary>
        private const string keyInclination = "inclination";

        /// <summary>
        /// The key initial position
        /// </summary>
        private const string keyInitialPosition = "initial_position";

        /// <summary>
        /// The key inner diameter
        /// </summary>
        private const string keyInnerDiameter = "inner_diameter";

        /// <summary>
        /// The key inner sphere plane
        /// </summary>
        private const string keyInnerSpherePlane = "inner_sphere_plane";

        /// <summary>
        /// The key market
        /// </summary>
        private const string keyDesign = "design";

        /// <summary>
        /// The key porous thickness
        /// </summary>
        private const string keyPorousThickness = "porous_shell_thick";

        /// <summary>
        /// The key thickness
        /// </summary>
        private const string keyThickness = "thickness";

        /// <summary>
        /// The porous radius overlap
        /// </summary>
        private const double porousRadiusOverlapSmoothDesign = 0.25;

        /// <summary>
        /// The porous radius overlap
        /// </summary>
        private const double porousRadiusOverlapRingDesign = 0.1;

        /// <summary>
        /// The reaming aperture
        /// </summary>
        private const double reamingAperture = 180.0;

        /// <summary>
        /// The anteversion
        /// </summary>
        private double _anteversion;

        /// <summary>
        /// The aperture angle
        /// </summary>
        private double _apertureAngle;

        /// <summary>
        /// The inclination
        /// </summary>
        private double _inclination;

        /// <summary>
        /// The initial position
        /// </summary>
        private Point3d initialPosition;

        /// <summary>
        /// The coordinate system
        /// </summary>
        public Plane coordinateSystem { get; private set; }

        /// <summary>
        /// The key coordinate system
        /// </summary>
        private string keyCoordinateSystem = "coordinate_system";

        /// <summary>
        /// The defect is left
        /// </summary>
        public bool defectIsLeft { get; private set; }

        /// <summary>
        /// The key defect is left
        /// </summary>
        private string keyDefectIsLeft = "defect_is_left";

        /// <summary>
        /// The center of rotation
        /// </summary>
        private Point3d _centerOfRotation;

        /// <summary>
        /// The inner diameter
        /// </summary>
        private double _innerCupDiameter;

        private Point3d DrawingCircleCenter => GetDrawingCircleCenter(InnerCupRadius);

        private static Point3d GetDrawingCircleCenter(double innerCupRadius)
        {
            return new Point3d(0, innerCupRadius, 0);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cup" /> class.
        /// </summary>
        /// <param name="hipJointCenter">The hip joint center.</param>
        /// <param name="anteversion">The anteversion.</param>
        /// <param name="inclination">The inclination.</param>
        /// <param name="apertureAngle">The aperture angle.</param>
        /// <param name="innerDiameter">The inner diameter.</param>
        /// <param name="thickness">The thickness.</param>
        /// <param name="porous">The porous.</param>
        /// <param name="coordinateSystem">The coordinate system.</param>
        /// <param name="defectIsLeft">if set to <c>true</c> [defect is left].</param>
        public Cup(Point3d centerOfRotation,
            CupType cupType,
            double anteversion,
            double inclination,
            double apertureAngle,
            double innerDiameter,
            Plane coordinateSystem,
            bool defectIsLeft)
            : this(ComputeBrep(centerOfRotation, cupType, anteversion, inclination, apertureAngle, innerDiameter, coordinateSystem, defectIsLeft))
        {
            InitializeParameters(centerOfRotation, anteversion, inclination, apertureAngle, innerDiameter, cupType, coordinateSystem, defectIsLeft);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cup"/> class.
        /// New cup at default position
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="hipJointCenter">The hip joint center.</param>
        /// <param name="anteversion">The anteversion.</param>
        /// <param name="inclination">The inclination.</param>
        /// <param name="apertureAngle">The aperture angle.</param>
        /// <param name="innerDiameter">The inner diameter.</param>
        /// <param name="thickness">The thickness.</param>
        /// <param name="porous">The porous.</param>
        public Cup(ImplantDirector director,
            Point3d hipJointCenter,
            CupType cupType,
            double anteversion = anteversionDefault,
            double inclination = inclinationDefault,
            double apertureAngle = apertureAngleDefault,
            double innerDiameter = innerDiameterDefault)
            : this(ComputeBrep(hipJointCenter, cupType, anteversion, inclination, apertureAngle, innerDiameter, director.Pcs, director.defectIsLeft))
        {
            InitializeParameters(hipJointCenter, anteversion, inclination, apertureAngle, innerDiameter, cupType, director.Pcs, director.defectIsLeft);

            this.Director = director;
            initialPosition = hipJointCenter;

            ManageDependencies();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cup" /> class. Creates a new Cup from the geometrical
        /// parameters of an existing cup. This is useful when the other cup's parameters are not consistent
        /// anymore with its Geometry.
        /// </summary>
        /// <param name="otherCup">The other cup.</param>
        /// <param name="director">The director.</param>
        /// <param name="replaceInDocument">if set to <c>true</c> [replace in document].</param>
        private Cup(Cup otherCup, ImplantDirector director)
            : base(ComputeBrep(otherCup.centerOfRotation,
                otherCup.cupType,
                otherCup.anteversion,
                otherCup.inclination,
                otherCup.apertureAngle,
                otherCup.innerCupDiameter,
                director.Pcs,
                director.defectIsLeft))
        {
            // Copy member variables from the other object
            DuplicateProperties(otherCup);
            // Manage dependencies
            ManageDependencies();
            // Replace the object in the document
            RhinoDoc doc = otherCup.Document;
            ObjRef otherRef = new ObjRef(otherCup);

            var objManager = new AmaceObjectManager(director);
            objManager.SetBuildingBlock(IBB.Cup, this, otherCup.Id);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cup"/> class. Create a new Cup object from a Brep object
        /// representing a cup. The objects Attributes dictionary and member variables can be copies from the 
        /// source object if specified.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <param name="fromArchive">if set to <c>true</c> [from archive].</param>
        /// <param name="copyAttributes">if set to <c>true</c> [copy attributes].</param>
        private Cup(RhinoObject other, bool fromArchive) : this(other.Geometry as Brep)
        {
            // Replace the object in the document or create new one
            Attributes = other.Attributes;

            // Load member variables from UserDictionary
            if (fromArchive)
            {
                // Load member variables from archive
                ArchivableDictionary udict = other.Attributes.UserDictionary;
                DeArchive(udict);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cup" /> class. This constructor is called during copying, which
        /// happens on all transformations/modifications to the object in the document.
        /// HAS TO EXIST AND HAS TO BE PUBLIC
        /// </summary>
        public Cup() : base()
        {
            // empty
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cup"/> class.
        /// HAS TO EXIST AND HAS TO BE PUBLIC
        /// </summary>
        /// <param name="brep">The brep.</param>
        public Cup(Brep brep) : base(brep)
        {
            // empty
        }

        /// <summary>
        /// Initializes the parameters.
        /// </summary>
        /// <param name="centerOfRotation">The center of rotation.</param>
        /// <param name="anteversion">The anteversion.</param>
        /// <param name="inclination">The inclination.</param>
        /// <param name="apertureAngle">The aperture angle.</param>
        /// <param name="innerDiameter">The inner diameter.</param>
        /// <param name="thickness">The thickness.</param>
        /// <param name="porous">The porous.</param>
        /// <param name="coordinateSystem">The coordinate system.</param>
        /// <param name="defectIsLeft">if set to <c>true</c> [defect is left].</param>
        private void InitializeParameters(Point3d centerOfRotation, double anteversion, double inclination, double apertureAngle, double innerDiameter, CupType cupType, Plane coordinateSystem, bool defectIsLeft)
        {
            // Parameters
            _cupType = cupType;
            _apertureAngle = apertureAngle;
            _anteversion = anteversion;
            _inclination = inclination;
            _innerCupDiameter = innerDiameter;
            this.coordinateSystem = coordinateSystem;
            this.defectIsLeft = defectIsLeft;
            _centerOfRotation = centerOfRotation;
            initialPosition = centerOfRotation;
        }

        /// <summary>
        /// Gets or sets the anteversion.
        /// </summary>
        /// <value>
        /// The anteversion.
        /// </value>
        public double anteversion
        {
            get
            {
                return _anteversion;
            }
            set
            {
                // Store old value
                double oldAnteversion = _anteversion;
                // Set value
                _anteversion = value;
                // Update geometry
                UpdateOrientation(oldAnteversion, inclination);
            }
        }

        /// <summary>
        /// Gets or sets the aperture angle.
        /// </summary>
        /// <value>
        /// The aperture angle.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">Invalid aperture angle</exception>
        public double apertureAngle
        {
            get
            {
                return _apertureAngle;
            }
            set
            {
                if (CanHaveAsApertureAngle(value))
                {
                    // Set value
                    _apertureAngle = value;
                    // Replace this cup in the document by a new one with correct parameters
                    Cup alteredCup = new Cup(this, Director);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Invalid aperture angle");
                }
            }
        }

        /// <summary>
        /// Gets or sets the center of rotation.
        /// </summary>
        /// <value>
        /// The center of rotation.
        /// </value>
        public Point3d centerOfRotation
        {
            get
            {
                return _centerOfRotation;
            }
            set
            {
                // Old value
                Point3d oldCenterOfRotation = centerOfRotation;
                // Set value
                _centerOfRotation = value;
                // Transform
                Transform xform = Transform.Translation(centerOfRotation - oldCenterOfRotation);
                Document.Objects.Transform(this, xform, true);
            }
        }

        /// <summary>
        /// The center of the circle corresponding to the cup rim.
        /// </summary>
        /// <value>
        /// The cup rim center.
        /// </value>
        public Point3d cupRimCenter
        {
            get
            {
                // Compute plane center relative to midplane (equitorial plane)
                double offsetangle = (180.0 - apertureAngle) / 2.0;
                Vector3d offsetvector = orientation * (innerCupDiameter / 2.0 * Math.Sin(RhinoMath.ToRadians(offsetangle)));
                return (centerOfRotation - offsetvector);
            }
        }

        /// <summary>
        /// Gets the cup rim plane.
        /// </summary>
        /// <value>
        /// The cup rim plane.
        /// </value>
        public Plane cupRimPlane
        {
            get
            {
                // Copy the inner sphere equitorial plane put origin on cuprim center
                Plane rim_plane = new Plane(centerOfRotation, orientation);  // The equatorial plane
                rim_plane.Origin = cupRimCenter;
                return rim_plane;
            }
        }

        /// <summary>
        /// Gets the cup skirt inner curve.
        /// </summary>
        /// <value>
        /// The cup skirt inner curve.
        /// </value>
        public Curve cupSkirtInnerCurve
        {
            get
            {
                return GetRimCurveAtAxialOffset(Director.PlateThickness + Director.PlateClearance, 0.0);
            }
        }

        /// <summary>
        /// Gets the cup skirt inner radius.
        /// </summary>
        /// <value>
        /// The cup skirt inner radius.
        /// </value>
        public double cupSkirtInnerRadius
        {
            get
            {
                return GetRimRadiusAtAxialOffset(Director.PlateThickness + Director.PlateClearance, 0.0);
            }
        }

        /// <summary>
        /// Gets the cup skirt outer curve.
        /// </summary>
        /// <value>
        /// The cup skirt outer curve.
        /// </value>
        public Curve cupSkirtOuterCurve
        {
            get
            {
                return GetRimCurveAtAxialOffset(Director.PlateThickness + Director.PlateClearance, Director.PlateThickness + Director.PlateClearance);
            }
        }

        /// <summary>
        /// Gets the cup skirt outer radius.
        /// </summary>
        /// <value>
        /// The cup skirt outer radius.
        /// </value>
        private double cupSkirtOuterRadius
        {
            get
            {
                return GetRimRadiusAtAxialOffset(Director.PlateThickness + Director.PlateClearance, 0.0);
            }
        }

        /// <summary>
        /// Gets the cup skirt plane.
        /// </summary>
        /// <value>
        /// The cup skirt plane.
        /// </value>
        public Plane cupSkirtPlane
        {
            get
            {
                return GetRimPlaneAtAxialOffset(Director.PlateThickness + Director.PlateClearance);
            }
        }

        /// <summary>
        /// The implant director managing this object.
        /// </summary>
        public ImplantDirector Director { get; set; }

        /// <summary>
        /// Gets the filled cup mesh.
        /// </summary>
        /// <value>
        /// The filled cup mesh.
        /// </value>
        public Mesh filledCupMesh
        {
            get
            {
                return MeshUtilities.ConvertBrepToMesh(filledCup);
            }
        }

        /// <summary>
        /// Gets or sets the inclination.
        /// </summary>
        /// <value>
        /// The inclination.
        /// </value>
        public double inclination
        {
            get
            {
                return _inclination;
            }
            set
            {
                // Store old value
                double oldInclination = inclination;
                // Set value
                _inclination = value;
                // Update geometry
                UpdateOrientation(anteversion, oldInclination);
            }
        }

        /// <summary>
        /// Updates the orientation.
        /// </summary>
        /// <param name="newAnteversion">The new anteversion.</param>
        /// <param name="newInclination">The new inclination.</param>
        private void UpdateOrientation(double oldAnteversion, double oldInclination)
        {
            Vector3d oldOrientation = MathUtilities.AnteversionInclinationToVector(oldAnteversion, oldInclination, coordinateSystem, defectIsLeft);
            Transform xform = Transform.Rotation(oldOrientation, orientation, centerOfRotation);
            Document.Objects.Transform(Id, xform, true);
        }

        /// <summary>
        /// Gets or sets the inner diameter.
        /// </summary>
        /// <value>
        /// The inner diameter.
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">Invalid inner diameter</exception>
        public double innerCupDiameter
        {
            get
            {
                return _innerCupDiameter;
            }
            set
            {
                if (CanHaveAsInnerDiameter(value))
                {
                    // Set value
                    _innerCupDiameter = value;
                    // Replace this cup in the document by a new one with correct parameters
                    Cup alteredCup = new Cup(this, Director);
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Invalid inner diameter");
                }
            }
        }

        public double InnerCupRadius => innerCupDiameter / 2;

        /// <summary>
        /// Gets the inner reaming diameter.
        /// </summary>
        /// <value>
        /// The inner reaming diameter.
        /// </value>
        public double innerReamingDiameter
        {
            get { return innerCupDiameter; }
        }

        /// <summary>
        /// Gets the inner reaming volume.
        /// </summary>
        /// <value>
        /// The inner reaming volume.
        /// </value>
        public Brep innerReamingVolume
        {
            get
            {
                return GetReamingVolume(innerReamingDiameter, defaultReamingHeight);
            }
        }

        /// <summary>
        /// Gets the inner reaming volume mesh.
        /// </summary>
        /// <value>
        /// The inner reaming volume mesh.
        /// </value>
        public Mesh innerReamingVolumeMesh
        {
            get
            {
                return GetReamingVolumeMesh(innerReamingDiameter);
            }
        }

        /// <summary>
        /// Gets the lateral cup.
        /// </summary>
        /// <value>
        /// The lateral cup.
        /// </value>
        private Brep lateralCup
        {
            get
            {
                return GetLateralCup(coordinateSystem, defectIsLeft);
            }
        }

        /// <summary>
        /// Gets the lateral cup mesh.
        /// </summary>
        /// <value>
        /// The lateral cup mesh.
        /// </value>
        public Mesh lateralCupMesh
        {
            get
            {
                return GetLateralCupMesh(coordinateSystem, defectIsLeft);
            }
        }

        /// <summary>
        /// Gets the liner diameter maximum.
        /// </summary>
        /// <value>
        /// The liner diameter maximum.
        /// </value>
        public double linerDiameterMax
        {
            get { return (innerCupDiameter - 4); }
        }

        /// <summary>
        /// Gets the orientation as the normal vector of the cup rim plane.
        /// </summary>
        /// <value>
        /// The orientation.
        /// </value>
        public Vector3d orientation
        {
            get
            {
                return MathUtilities.AnteversionInclinationToVector(anteversion, inclination, coordinateSystem, defectIsLeft);
            }
        }

        /// <summary>
        /// Gets the outer cup rim.
        /// </summary>
        /// <value>
        /// The outer cup rim.
        /// </value>
        private Curve outerCupRim
        {
            get
            {
                return GetRimCurveAtAxialOffset(0.0, cupType.CupThickness);
            }
        }

        public double CupRingPolishingOffset=> GetPolishingOffsetValue(_cupType.CupDesign);

        /// <summary>
        /// Gets the outer diameter.
        /// </summary>
        /// <value>
        /// The outer diameter.
        /// </value>
        public double outerCupDiameter
        {
            get { return outerCupRadius * 2; }
        }

        /// <summary>
        /// Gets the outer radius.
        /// </summary>
        /// <value>
        /// The outer radius.
        /// </value>
        public double outerCupRadius
        {
            get { return InnerCupRadius + cupType.CupThickness; }
        }

        /// <summary>
        /// Gets the outer reaming volume mesh.
        /// </summary>
        /// <value>
        /// The outer reaming volume mesh.
        /// </value>
        public Mesh outerReamingVolumeMesh
        {
            get
            {
                return GetOuterReamingVolumeMesh();
            }
        }

        /// <summary>
        /// Gets the reaming diameter.
        /// </summary>
        /// <value>
        /// The reaming diameter.
        /// </value>
        public double outerReamingDiameter
        {
            get { return (outerCupRadius + cupType.PorousThickness) * 2; }
        }

        /// <summary>
        /// Gets the cup closing curve.
        /// </summary>
        /// <value>
        /// The cup closing curve.
        /// </value>
        private Curve cupClosingCurve
        {
            get
            {
                return new Line(horizontalBorderCurve.PointAtEnd, new Point3d(0, horizontalBorderCurve.PointAtEnd.Y, 0)).ToNurbsCurve();
            }
        }

        public Brep GetCupRing()
        {
            if (cupType.CupDesign == CupDesign.v2)
            {
                var transitionToMedialCupCurvePointStart = Point3d.Unset;

                var transitionCurveSegments = new List<Curve>();
                GetRingDesignTransitionCurve(horizontalBorderCurve, AngleHorizontalBorder, InnerCupRadius,
                    cupType.CupThickness, horizontalBorderWidth, CupRingPolishingOffset, out transitionToMedialCupCurvePointStart, out transitionCurveSegments);

                //We do not want the last segment of the curve
                transitionCurveSegments.Remove(transitionCurveSegments.LastOrDefault());
                var transitionCurve = Curve.JoinCurves(transitionCurveSegments)[0];

                var drawingPlaneXAxis = new Vector3d(1, 0, 0);
                var vecCenterToTip = transitionToMedialCupCurvePointStart - DrawingCircleCenter;
                vecCenterToTip.Unitize();

                var angleStart = 90 - RhinoMath.ToDegrees(Vector3d.VectorAngle(vecCenterToTip, drawingPlaneXAxis));
                var lateralCupRingCurve = CreateLateralCupCurve(DrawingCircleCenter, innerCupDiameter, apertureAngle, CupRingPolishingOffset, angleStart);

                var closingCurve = GetGenericCurve(transitionToMedialCupCurvePointStart,
                    lateralCupRingCurve.PointAtStart);

                var allCurves = new List<Curve>();
                allCurves.Add(lateralCupRingCurve);
                allCurves.Add(horizontalBorderCurve);
                allCurves.Add(transitionCurve);
                allCurves.Add(closingCurve);
                
#if (INTERNAL)
                if (ImplantDirector.IsDebugMode)
                {
                    var cupRingCutOffLine = new Line(DrawingCircleCenter, transitionToMedialCupCurvePointStart);

                    InternalUtilities.AddPoint(DrawingCircleCenter, "Center", "Testing::CupRing",
                        Color.Crimson);
                    InternalUtilities.AddCurve(Curve.JoinCurves(allCurves)[0], "cupRingCurve", "Testing::CupRing", Color.Crimson);
                    InternalUtilities.AddLine(cupRingCutOffLine, "cupRingCutOffLine", "Testing::CupRing", Color.Crimson);
                }
#endif

                return MakeCupEntityFromCurves(allCurves);
            }

            throw new IDSException("Cup Ring not implemented (only for v2)");
        }

        public Brep filledCup
        {
            get
            {
                List<Curve> allCurves = new List<Curve>();
                Curve transitionCurve = null;
                if (cupType.CupDesign == CupDesign.v1)
                    transitionCurve = GetSmoothDesignTransitionCurve(innerCupDiameter, cupType.CupThickness, medialCupCurve, horizontalBorderCurve);
                else if (cupType.CupDesign == CupDesign.v2)
                {
                    transitionCurve = GetRingDesignTransitionCurve(horizontalBorderCurve, AngleHorizontalBorder, InnerCupRadius, cupType.CupThickness, horizontalBorderWidth, CupRingPolishingOffset);
                }

                else
                    throw new Exception("Cup design type not implemented");

                allCurves.Add(medialCupCurve);
                allCurves.Add(transitionCurve);
                allCurves.Add(cupClosingCurve);

                Brep filledCupBrep = MakeCupEntityFromCurves(allCurves);

                return filledCupBrep;
            }
        }

        /// <summary>
        /// Gets the horizontal border.
        /// </summary>
        /// <value>
        /// The horizontal border.
        /// </value>
        private Brep horizontalBorder
        {
            get
            {
                // Create entity from curve
                List<Curve> allCurves = new List<Curve>() { horizontalBorderCurve };
                Brep horBorSurfaceTransf = MakeCupEntityFromCurves(allCurves);
                // Flip normals
                horBorSurfaceTransf.Flip();

                return horBorSurfaceTransf;
            }
        }

        /// <summary>
        /// Gets the horizontal border curve.
        /// </summary>
        /// <value>
        /// The horizontal border curve.
        /// </value>
        private Curve horizontalBorderCurve
        {
            get
            {
                return cupType.CupDesign == CupDesign.v2 ? GetHorizontalBorderCurve(CupRingPolishingOffset, cupType.CupThickness) : GetHorizontalBorderCurve(0, cupType.CupThickness);
            }
        }

        /// <summary>
        /// Gets the horizontal inner reaming curve.
        /// </summary>
        /// <value>
        /// The horizontal inner reaming curve.
        /// </value>
        private Curve horizontalInnerReamingCurve
        {
            get
            {
                return GetHorizontalInnerReamingCurve();
            }
        }

        /// <summary>
        /// Gets the horizontal outer reaming curve.
        /// </summary>
        /// <value>
        /// The horizontal outer reaming curve.
        /// </value>
        private Curve horizontalOuterReamingCurve => GetHorizontalOuterReamingCurve(defaultReamingHeight);

        /// <summary>
        /// Gets the lateral cup curve.
        /// </summary>
        /// <value>
        /// The lateral cup curve.
        /// </value>
        private Curve lateralCupCurve => CreateLateralCupCurve(DrawingCircleCenter, innerCupDiameter, apertureAngle,
            CupRingPolishingOffset);

        /// <summary>
        /// Gets the lateral porous curve.
        /// </summary>
        /// <value>
        /// The lateral porous curve.
        /// </value>
        private Curve lateralPorousCurve
        {
            get
            {
                double overlap = cupType.CupDesign == CupDesign.v2 ? porousRadiusOverlapRingDesign : porousRadiusOverlapSmoothDesign;

                var circleDesigner = new CircleDesigner(DrawingCircleCenter);
                Curve theCurve = circleDesigner.CreateCurveOnCircle(outerCupDiameter / 2 - overlap, 90);
                return theCurve;
            }
        }

        /// <summary>
        /// Gets the medial cup.
        /// </summary>
        /// <value>
        /// The medial cup.
        /// </value>
        private Brep medialCup
        {
            get
            {
                List<Curve> allCurves = new List<Curve>() { medialCupCurve };
                return MakeCupEntityFromCurves(allCurves);
            }
        }

        private double medialAperture
        {
            get
            {
                if (cupType.CupDesign == CupDesign.v1)
                    return GetSmoothDesignMedialAperture();
                else if (cupType.CupDesign == CupDesign.v2)
                    return GetRingDesignMedialCupAperture(AngleHorizontalBorder, outerCupRadius);
                else
                    throw new Exception("Cup design type not implemented");
            }
        }

        /// <summary>
        /// Gets the medial cup curve.
        /// </summary>
        /// <value>
        /// The medial cup curve.
        /// </value>
        private Curve medialCupCurve
        {
            get
            {
                return GetMedialCupCurve(innerCupDiameter, cupType.CupThickness, medialAperture);
            }
        }

        /// <summary>
        /// Gets the medial porous curve.
        /// </summary>
        /// <value>
        /// The medial porous curve.
        /// </value>
        private Curve medialPorousCurve
        {
            get
            {
                var circleDesigner = new CircleDesigner(DrawingCircleCenter);
                var theCurve = circleDesigner.CreateCurveOnCircle(outerReamingDiameter / 2, 90);
                return theCurve;
            }
        }

        private static double GetRingThickness(double cupThickness)
        {
            if (Math.Abs(cupThickness - 2) < Epsilon || Math.Abs(cupThickness - 3) < Epsilon) //cupThickness = 2 OR 3
                return cupThickness + 1;
            if (Math.Abs(cupThickness - 4) < Epsilon) //cupThickness = 4
                return cupThickness;

            throw new Exception("Ring thickness not set for selected ring design cup thickness.");
        }

        /// <summary>
        /// Gets the porous shell.
        /// </summary>
        /// <value>
        /// The porous shell.
        /// </value>
        public Brep porousShell
        {
            get
            {
                // No porous shell
                Brep shell = null;
                if (cupType.PorousThickness > 0)
                {
                    if (cupType.CupDesign == CupDesign.v1)
                        shell = GetSmoothDesignPorousShell();
                    else if (cupType.CupDesign == CupDesign.v2)
                    {
                        shell = GetRingDesignPorousShell();
                    }
                    else
                        throw new Exception("Cup design type not implemented");

                }
                else
                {
                    shell = null;
                }
                return shell;
            }
        }

        public double AngleHorizontalBorder => GetAngleHorizontalBorder(apertureAngle);

        private static double GetAngleHorizontalBorder(double apertureAngle)
        {
            return -(180.0 - apertureAngle) / 2.0;
        }

        /// <summary>
        /// Gets the ring design porous shell for v2 Cup.
        /// </summary>
        /// <param name="ringThickness">The ring thickness.</param>
        /// <param name="overlap">The overlap.</param>
        /// <returns></returns>
        private Brep GetRingDesignPorousShell()
        {
            List<Curve> shellCurves = GetRingDesignPorousShellCurves();
            return MakeCupEntityFromCurves(shellCurves);
        }

        //Create the ring design porous shell for v2 Cup.
        public List<Curve> GetRingDesignPorousShellCurves()
        {
            double ringThickness = GetRingThickness(cupType.CupThickness);
            if (ringThickness > cupType.CupThickness) //3+1 and 2+1 v2 
                return GetRingDesignPorousShellUnderRingCurves();
            else
                return GetRingDesignPorousShellsOnCupCurves(); //4+1 v2
        }

        private List<Curve> GetRingDesignPorousShellsOnCupCurves()
        {
            // Arc lengths
            const double anglePorousLayerTop = 5.0;
            const double anglePorousLayerSmoothnessControl = 6.0;
            const double anglePorousLayerRoundingEnd = 7.0;

            // Precalculate some parameters
            const double angleStart = -90.0;
            var innerCupRadius = innerCupDiameter / 2.0;
            var outerCupRadius = innerCupRadius + cupType.CupThickness;
            var innerPorousRadius = outerCupRadius - porousRadiusOverlapRingDesign;
            var radiusOuterPorous = outerCupRadius + cupType.PorousThickness;

            var circleDesigner = new CircleDesigner(DrawingCircleCenter);

            // Lateral curve
            var angleLateralCurveEnd = AngleHorizontalBorder - MathUtilities.CalculateArcAngle(radiusOuterPorous, anglePorousLayerTop);
            var apertureLateralCurve = 180 + 2 * angleLateralCurveEnd;
            var lateralCurve = circleDesigner.CreateCurveOnCircle(innerPorousRadius, apertureLateralCurve / 2);

            // Medial curve
            var angleMedialCurve = AngleHorizontalBorder - MathUtilities.CalculateArcAngle(radiusOuterPorous, anglePorousLayerRoundingEnd);
            var aperatureMedialCurve = 180 + 2 * angleMedialCurve;
            var medialCurve = circleDesigner.CreateCurveOnCircle(radiusOuterPorous, aperatureMedialCurve / 2);

            // Create Points
            var p5 = circleDesigner.CreateDesignReferencePoint(angleStart, AngleHorizontalBorder, radiusOuterPorous, anglePorousLayerTop, innerPorousRadius);
            var p6 = circleDesigner.CreateDesignReferencePoint(angleStart, AngleHorizontalBorder, radiusOuterPorous, anglePorousLayerTop, outerCupRadius);
            var p7 = circleDesigner.CreateDesignReferencePoint(angleStart, AngleHorizontalBorder, radiusOuterPorous, anglePorousLayerSmoothnessControl, radiusOuterPorous);
            var p8 = circleDesigner.CreateDesignReferencePoint(angleStart, AngleHorizontalBorder, radiusOuterPorous, anglePorousLayerRoundingEnd, radiusOuterPorous);

            // Transition curves
            var straightTransition = GetGenericCurve(p5, p6);
            var smoothTransition = GetGenericCurve(p6, p7, p8);

#if (INTERNAL)
            if (ImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddCurve(medialCurve, "Porous medialCurve", "Testing::Porous", Color.Aqua);
                InternalUtilities.AddCurve(smoothTransition, "Porous smoothTransition", "Testing::Porous", Color.Aqua);
                InternalUtilities.AddCurve(lateralCurve, "Porous lateralCurve", "Testing::Porous", Color.Aqua);
                InternalUtilities.AddCurve(straightTransition, "Porous straightTransition", "Testing::Porous", Color.Aqua);
            }
#endif

            // Combine curves
            return new List<Curve> { medialCurve, straightTransition, smoothTransition, lateralCurve };
        }

        //Creation of Porous Shell when cup thickness is smaller than ring thickness of v2 cup
        private List<Curve> GetRingDesignPorousShellUnderRingCurves()
        {
            // Precalculate some parameters
            const double angleStart = -90.0;
            var ringThickness = GetRingThickness(cupType.CupThickness);
            var solidRadius = InnerCupRadius + cupType.CupThickness;
            var innerPorousRadius = InnerCupRadius + cupType.CupThickness - porousRadiusOverlapRingDesign; 
            var outerPorousRadius = InnerCupRadius + ringThickness; 
            var outerOverlapRadius = InnerCupRadius + ringThickness - porousRadiusOverlapRingDesign;

            bool isTwoPlusOneVersion2Cup = Math.Abs(cupType.CupThickness - 2) < Epsilon &&
                                           Math.Abs(ringThickness - 3) < Epsilon;

            // Arc lengths
            double anglePorousLayerTopAngleAdjustmentControl = isTwoPlusOneVersion2Cup ? 0.2 : 0.0; //Bigger length the higher it will be, 
            double anglePorousLayerTop = 5 - MathUtilities.CalculateArcLength(outerPorousRadius, anglePorousLayerTopAngleAdjustmentControl);
            const double angleTransitionSmoothnessControl = 6.0; //need to adjust this
            const double angleSmoothTransitionEnd = 8.0;

            var circleDesigner = new CircleDesigner(DrawingCircleCenter);

            // Lateral curve
            var lateralAngle = AngleHorizontalBorder - MathUtilities.CalculateArcAngle(outerPorousRadius, angleSmoothTransitionEnd);
            var lateralAperture = 180 + 2 * lateralAngle;
            var lateralCurve = circleDesigner.CreateCurveOnCircle(innerPorousRadius, lateralAperture / 2);

            // Medial curve
            var medialAngle = AngleHorizontalBorder - MathUtilities.CalculateArcAngle(outerPorousRadius, anglePorousLayerTop);
            var medialAperture = 180 + 2 * medialAngle;
            var medialCurve = circleDesigner.CreateCurveOnCircle(outerPorousRadius, medialAperture / 2);

            // Create Points
            var p5 = circleDesigner.CreateDesignReferencePoint(angleStart, AngleHorizontalBorder, outerPorousRadius, anglePorousLayerTop, outerPorousRadius);
            var p6 = circleDesigner.CreateDesignReferencePoint(angleStart, AngleHorizontalBorder, outerPorousRadius, angleTransitionSmoothnessControl, innerPorousRadius);
            var p7 = circleDesigner.CreateDesignReferencePoint(angleStart, AngleHorizontalBorder, outerPorousRadius, angleSmoothTransitionEnd, solidRadius);
            var p8 = circleDesigner.CreateDesignReferencePoint(angleStart, AngleHorizontalBorder, outerPorousRadius, anglePorousLayerTop, outerOverlapRadius);
            var p10 = circleDesigner.CreateDesignReferencePoint(angleStart, AngleHorizontalBorder, outerPorousRadius, angleSmoothTransitionEnd, innerPorousRadius);
            var p9 = p6 + ((p10 - p7) + (p8 - p5)) / 2;

            // Transition curves
            var straightTransition = isTwoPlusOneVersion2Cup ? null : GetGenericCurve(p5, p8);
            var smoothTransition = isTwoPlusOneVersion2Cup ? GetGenericCurve(medialCurve.PointAtEnd, p6, p10) : GetGenericCurve(p8, p9, p10);

#if (INTERNAL)
            if (ImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddCurve(medialCurve, "Porous medialCurve", "Testing::Porous", Color.Aqua);
                InternalUtilities.AddCurve(smoothTransition, "Porous smoothTransition", "Testing::Porous", Color.Aqua);
                InternalUtilities.AddCurve(lateralCurve, "Porous lateralCurve", "Testing::Porous", Color.Aqua);

                if (!isTwoPlusOneVersion2Cup)
                {
                    InternalUtilities.AddCurve(straightTransition, "Porous straightTransition", "Testing::Porous", Color.Aqua);
                }
            }
#endif

            // Combine curves
            return isTwoPlusOneVersion2Cup ? new List<Curve> { medialCurve, smoothTransition, lateralCurve } :
                new List<Curve> { medialCurve, smoothTransition, straightTransition, lateralCurve };
        }

        /// <summary>
        /// Gets the smooth design porous shell.
        /// </summary>
        /// <returns></returns>
        private Brep GetSmoothDesignPorousShell()
        {
            List<Curve> allCurves = GetSmoothDesignPorousShellCurves();
            return MakeCupEntityFromCurves(allCurves);
        }

        public List<Curve> GetSmoothDesignPorousShellCurves()
        {
            Curve transition = GetGenericCurve(lateralPorousCurve.PointAtEnd, medialPorousCurve.PointAtEnd);
            List<Curve> allCurves = new List<Curve>() { lateralPorousCurve, transition, medialPorousCurve };
            return allCurves;
        }

        /// <summary>
        /// Gets the RBV preview.
        /// </summary>
        /// <value>
        /// The RBV preview.
        /// </value>
        private Brep rbvPreview
        {
            get
            {
                Curve curveAtOrigin;
                var reamerBrep = CreateCupReamer(20.0, false, out curveAtOrigin);

#if (INTERNAL)
                if (ImplantDirector.IsDebugMode)
                {
                    InternalUtilities.AddCurve(curveAtOrigin, "cupRbvPreviewCurveAtOrigin", "Testing::Cup Reaming", Color.Magenta);
                }
#endif

                return reamerBrep;
            }
        }

        /// <summary>
        /// Gets the revolve axis to revolve curve around to create cup entities
        /// </summary>
        /// <value>
        /// The revolve axis.
        /// </value>
        private Line RevolveAxis => GetRevolveAxis();

        /// <summary>
        /// Gets the vertical inner reaming curve.
        /// </summary>
        /// <value>
        /// The vertical inner reaming curve.
        /// </value>
        private Curve verticalInnerReamingCurve
        {
            get
            {
                return GetVerticalInnerReamingCurve();
            }
        }

        /// <summary>
        /// Gets the vertical outer reaming curve.
        /// </summary>
        /// <value>
        /// The vertical outer reaming curve.
        /// </value>
        private Curve verticalOuterReamingCurve
        {
            get
            {
                return GetVerticalOuterReamingCurve(defaultReamingHeight);
            }
        }

        /// <summary>
        /// Determines whether this instance [can have as aperture angle] the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can have as aperture angle] the specified value; otherwise, <c>false</c>.
        /// </returns>
        private static bool CanHaveAsApertureAngle(double value)
        {
            return value <= apertureAngleMax && value >= apertureAngleMin;
        }

        /// <summary>
        /// Determines whether this instance [can have as inner diameter] the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can have as inner diameter] the specified value; otherwise, <c>false</c>.
        /// </returns>
        private static bool CanHaveAsInnerDiameter(double value)
        {
            return value >= innerDiameterMin && value <= innerDiameterMax;
        }

        /// <summary>
        /// Determines whether this instance [can have as thickness] the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can have as thickness] the specified value; otherwise, <c>false</c>.
        /// </returns>
        private static bool CanHaveAsThickness(double value)
        {
            return value >= thicknessMin && value <= thicknessMax;
        }

        /// <summary>
        /// Determines whether this instance [can have as porous thickness] the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if this instance [can have as porous thickness] the specified value; otherwise, <c>false</c>.
        /// </returns>
        private bool CanHaveAsPorousThickness(double value)
        {
            return value <= porousThicknessMax && value >= porousThicknessMin;
        }

        /// <summary>
        /// Creates from archived. Replacement constructor as a static factory method.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <param name="replaceInDoc">if set to <c>true</c> [replace in document].</param>
        /// <returns></returns>
        public static Cup CreateFromArchived(RhinoObject other, bool replaceInDoc)
        {
            // Restore the cup object from archive
            Cup restored = new Cup(other, true);

            // Replace if necessary
            if (replaceInDoc)
            {
                bool replaced = AmaceObjectManager.ReplaceRhinoObject(other, restored);
                if (!replaced)
                    return null;
            }

            return restored;
        }
        /// <summary>
        /// De-serialize member variables from archive.
        /// </summary>
        /// <param name="udict">The udict.</param>
        public void DeArchive(ArchivableDictionary udict)
        {
            // Use private variable to avoid the cup being updated

            // Load data
            _apertureAngle = udict.GetDouble(keyApertureAngle, apertureAngleDefault);
            double cupThickness = udict.GetDouble(keyThickness, thicknessDefault);
            double porousThickness = udict.GetDouble(keyPorousThickness, porousThicknessDefault);
            initialPosition = (Point3d)udict[keyInitialPosition];
            _anteversion = udict.GetDouble(keyAnteversion, anteversionDefault);
            _inclination = udict.GetDouble(keyInclination, inclinationDefault);
            _innerCupDiameter = udict.GetDouble(keyInnerDiameter, innerDiameterDefault);

            CupDesign design = CupDesign.v1;
            if (udict.ContainsKey(keyCoordinateSystem))
            {
                // IDS 2.0.0 and later
                coordinateSystem = (Plane)udict[keyCoordinateSystem];
                defectIsLeft = udict.GetBool(keyDefectIsLeft, false);
                _centerOfRotation = (Point3d)udict[keyCenterOfRotation];
                string designString = udict.GetString(keyDesign, CupDesign.v1.ToString());
                // Conversion of Smooth/Ring to v1/v2 between 2.0.0 and 2.0.1
                if (designString.ToLower() == "smooth")
                    design = CupDesign.v1;
                else if (designString.ToLower() == "ring")
                    design = CupDesign.v2;
                else
                    design = (CupDesign)Enum.Parse(typeof(CupDesign), designString);
            }
            else
            {
                // Backwards compatibility for pre-2.0.0 cases
                // Load sphere data
                Plane eq_plane = (Plane)udict[keyInnerSpherePlane];
                ImplantDirector temporaryDirector = IDSPluginHelper.GetDirector<ImplantDirector>(RhinoDoc.ActiveDoc.DocumentId);

                coordinateSystem = temporaryDirector.Pcs;
                defectIsLeft = temporaryDirector.defectIsLeft;
                _centerOfRotation = eq_plane.Origin;
            }

            _cupType = new CupType(cupThickness, porousThickness, design);
        }

        /// <summary>
        /// Gets the horizontal border mesh.
        /// </summary>
        /// <returns></returns>
        public Mesh horizontalBorderMesh
        {
            get
            {
                MeshingParameters meshparameters = MeshParameters.IDS();
                Mesh[] parts = Mesh.CreateFromBrep(horizontalBorder, meshparameters);
                Mesh mesh = new Mesh();
                foreach (Mesh part in parts)
                    mesh.Append(part);
                return mesh;
            }

        }

        /// <summary>
        /// Gets the reaming volume mesh.
        /// </summary>
        /// <param name="diameter">The diameter.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        public Mesh GetReamingVolumeMesh(double diameter, double height = defaultReamingHeight)
        {
            MeshingParameters meshparameters = MeshParameters.IDS();
            Mesh[] parts = Mesh.CreateFromBrep(GetReamingVolume(diameter, height), meshparameters);
            Mesh mesh = new Mesh();
            foreach (Mesh part in parts)
                mesh.Append(part);
            return mesh;
        }

        public Mesh GetOuterReamingVolumeMesh()
        {
            Curve curveAtOrigin;
            var reamerBrep = CreateCupReamer(defaultReamingHeight, true, out curveAtOrigin);

#if (INTERNAL)
            if (ImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddCurve(curveAtOrigin, "cupReamingEntityCurveAtOrigin", "Testing::Cup Reaming", Color.Magenta);
            }
#endif

            var reamer = MeshUtilities.ConvertBrepToMesh(reamerBrep);
            return reamer;
        }

        private Brep CreateCupReamer(double height, bool cappedTop, out Curve curveAtOrigin)
        {
            var cupReamerMaker = new CupReamerMaker(cupType.CupThickness, cupType.PorousThickness, InnerCupRadius, AngleHorizontalBorder, referenceEndArcLength);
            curveAtOrigin = cupReamerMaker.CreateCupReamerCurve(height, cappedTop);
            var reamerBrep = RevSurface.Create(curveAtOrigin, GetRevolveAxis()).ToBrep();
            TransformToCurrentPosition(reamerBrep, new Point3d(0, 0, 0), orientation, centerOfRotation);
            return reamerBrep;
        }

        /// <summary>
        /// Gets the rim center at axial offset.
        /// </summary>
        /// <param name="rimOffsetAxial">The rim offset axial.</param>
        /// <returns></returns>
        public Point3d GetRimCenterAtAxialOffset(double rimOffsetAxial)
        {
            // Create a cutting plane at the height corresponding to the aperture angle
            double offsetAngle = (180.0 - apertureAngle) / 2.0;
            Vector3d offsetVector = orientation * (innerCupDiameter / 2.0 * Math.Sin(RhinoMath.ToRadians(offsetAngle)) + rimOffsetAxial);
            Point3d rimCenter = centerOfRotation - offsetVector;

            return rimCenter;
        }

        /// <summary>
        /// Gets the rim curve at axial offset.
        /// </summary>
        /// <param name="rimOffsetAxial">The rim offset axial.</param>
        /// <param name="rimOffsetRadius">The rim offset radius.</param>
        /// <returns></returns>
        private Curve GetRimCurveAtAxialOffset(double rimOffsetAxial, double rimOffsetRadius)
        {
            Plane cutPlane = GetRimPlaneAtAxialOffset(rimOffsetAxial);
            double radius = GetRimRadiusAtAxialOffset(rimOffsetAxial, rimOffsetRadius);
            Circle rimCircle = new Circle(cutPlane, radius);

            return rimCircle.ToNurbsCurve().Rebuild(16, 2, true);
        }

        /// <summary>
        /// Gets the rim plane at axial offset.
        /// </summary>
        /// <param name="rimOffsetAxial">The rim offset axial.</param>
        /// <returns></returns>
        public Plane GetRimPlaneAtAxialOffset(double rimOffsetAxial)
        {
            Point3d rimCenter = GetRimCenterAtAxialOffset(rimOffsetAxial);
            Plane cutPlane = new Plane(rimCenter, orientation);

            return cutPlane;
        }

        /// <summary>
        /// Gets the rim radius at axial offset.
        /// </summary>
        /// <param name="rimOffsetAxial">The rim offset axial.</param>
        /// <param name="rimOffsetRadius">The rim offset radius.</param>
        /// <returns></returns>
        private double GetRimRadiusAtAxialOffset(double rimOffsetAxial, double rimOffsetRadius)
        {
            double R = InnerCupRadius + rimOffsetRadius;
            double alpha = (180.0 - apertureAngle) / 2;
            double alphaRad = alpha / 180.0 * Math.PI;
            double cB = 2 * R * Math.Sin(alphaRad / 2);
            double Doffset = Math.Cos(alphaRad) * cB;
            double Toffset = Doffset + rimOffsetAxial;
            double Rc = Math.Sqrt(Math.Pow(R, 2) - Math.Pow(Toffset, 2));

            return Rc;
        }

        /// <summary>
        /// Laterals the cup.
        /// </summary>
        /// <param name="PCS">The PCS.</param>
        /// <param name="defectIsLeft">if set to <c>true</c> [defect is left].</param>
        /// <returns></returns>
        private Brep GetLateralCup(Plane PCS, bool defectIsLeft)
        {
            // Create entity from curve
            List<Curve> allCurves = new List<Curve>() { lateralCupCurve, horizontalBorderCurve };
            Brep lateralCupSurfaceTransf = MakeCupEntityFromCurves(allCurves, PCS, defectIsLeft);
            // Flip normals
            lateralCupSurfaceTransf.Flip();

            return lateralCupSurfaceTransf;
        }

        /// <summary>
        /// Laterals the cup mesh.
        /// </summary>
        /// <param name="PCS">The PCS.</param>
        /// <param name="defectIsLeft">if set to <c>true</c> [defect is left].</param>
        /// <returns></returns>
        private Mesh GetLateralCupMesh(Plane PCS, bool defectIsLeft)
        {
            Mesh[] lateralCupParts = Mesh.CreateFromBrep(this.GetLateralCup(PCS, defectIsLeft));
            Mesh lateralCup = new Mesh();
            foreach (Mesh part in lateralCupParts)
                lateralCup.Append(part);
            return lateralCup;
        }

        /// <summary>
        /// Serialize member variables to user dictionary.
        /// </summary>
        public void PrepareForArchiving()
        {
            Attributes.UserDictionary.SetEnumValue<IBB>(ImplantBuildingBlockProperties.KeyBlockType, IBB.Cup);
            // Save parameters
            Attributes.UserDictionary.Set(keyApertureAngle, apertureAngle);
            Attributes.UserDictionary.Set(keyThickness, cupType.CupThickness);
            Attributes.UserDictionary.Set(keyPorousThickness, cupType.PorousThickness);
            Attributes.UserDictionary.Set(keyInitialPosition, initialPosition);
            Attributes.UserDictionary.Set(keyAnteversion, anteversion);
            Attributes.UserDictionary.Set(keyInclination, inclination);

            Attributes.UserDictionary.Set(keyInnerDiameter, innerCupDiameter);
            Attributes.UserDictionary.Set(keyCoordinateSystem, coordinateSystem);
            Attributes.UserDictionary.Set(keyDefectIsLeft, defectIsLeft);
            Attributes.UserDictionary.Set(keyCenterOfRotation, centerOfRotation);
            Attributes.UserDictionary.Set(keyDesign, cupType.CupDesign.ToString());

            CommitChanges();
        }

        /// <summary>
        /// Sets the axial offset.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="reference">The reference.</param>
        public void MoveAlongAxis(double offset, Vector3d axis, Point3d reference)
        {
            axis.Unitize();
            double own_offset = MathUtilities.GetOffset(axis, reference, centerOfRotation);
            Vector3d translation = (offset - own_offset) * axis;
            centerOfRotation = centerOfRotation + translation;
        }

        /// <summary>
        /// Updates the helper blocks.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns></returns>
        private void ManageDependencies()
        {
            // Delete dependencies
            var dep = new Dependencies();
            dep.DeleteBlockDependencies(Director, IBB.Cup);

            // Update all related entities
            UpdateInnerReamingEntity();
            UpdateReamingEntity();
            UpdatePorousLayer();
            UpdateRbvPreview();
        }

        /// <summary>
        /// Updates the RBV preview.
        /// </summary>
        private void UpdateRbvPreview()
        {
            // RBV Preview
            var objManager = new AmaceObjectManager(Director);
            Guid idRbvPreview = objManager.GetBuildingBlockId(IBB.CupRbvPreview);
            Brep rbvPreviewShell = rbvPreview;
            objManager.SetBuildingBlock(IBB.CupRbvPreview, rbvPreviewShell, idRbvPreview);
        }

        /// <summary>
        /// Updates the porous layer.
        /// </summary>
        private void UpdatePorousLayer()
        {
            // Porous shell
            var objManager = new AmaceObjectManager(Director);
            Guid idPorous = objManager.GetBuildingBlockId(IBB.CupPorousLayer);
            if (porousShell == null && idPorous != Guid.Empty)
            {
                // Porous layer can be deleted
                objManager.DeleteObject(idPorous);
            }
            else if (porousShell != null)
            {
                // Replace the building block
                objManager.SetBuildingBlock(IBB.CupPorousLayer, porousShell, idPorous);
            }
        }

        /// <summary>
        /// Updates the reaming entity.
        /// </summary>
        private void UpdateReamingEntity()
        {
            // Reaming Entity
            var objManager = new AmaceObjectManager(Director);
            Guid idReamingOuter = objManager.GetBuildingBlockId(IBB.CupReamingEntity);
            objManager.SetBuildingBlock(IBB.CupReamingEntity, outerReamingVolumeMesh, idReamingOuter);
            // Delete existing dependencies
            var dep = new Dependencies();
            dep.DeleteBlockDependencies(Director, IBB.CupReamingEntity);
        }

        /// <summary>
        /// Updates the inner reaming entity.
        /// </summary>
        private void UpdateInnerReamingEntity()
        {
            // Inner Reaming Entity
            var objManager = new AmaceObjectManager(Director);
            Guid idReamingInner = objManager.GetBuildingBlockId(IBB.LateralCupSubtractor);
            objManager.SetBuildingBlock(IBB.LateralCupSubtractor, innerReamingVolumeMesh, idReamingInner);
            // Delete existing dependencies
            var dep = new Dependencies();
            dep.DeleteBlockDependencies(Director, IBB.LateralCupSubtractor);
        }

        /// <summary>
        /// This call informs an object it is about to be added to the list of
        /// active objects in the document.
        /// </summary>
        /// <param name="doc"></param>
        protected override void OnAddToDocument(RhinoDoc doc)
        {
            base.OnAddToDocument(doc);
        }

        /// <summary>
        /// This call informs an object it is about to be deleted.
        /// Some objects, like clipping planes, need to do a little extra cleanup
        /// before they are deleted.
        /// </summary>
        /// <param name="doc"></param>
        protected override void OnDeleteFromDocument(RhinoDoc doc)
        {
            base.OnDeleteFromDocument(doc);
            // TODO: move clean-up operation from ImplantDirector to here
        }

        /// <summary>
        /// Called when Rhino wants to draw this object
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDraw(DrawEventArgs e)
        {
            base.OnDraw(e);
        }

        /// <summary>
        /// Called when a new instance of this object is created and copied from
        /// an existing object
        /// </summary>
        /// <param name="source"></param>
        protected override void OnDuplicate(RhinoObject source)
        {
            base.OnDuplicate(source);
            DuplicateProperties(source);
            ManageDependencies();
        }

        /// <summary>
        /// Duplicates the properties.
        /// </summary>
        /// <param name="source">The source.</param>
        private void DuplicateProperties(RhinoObject source)
        {
            // Convert the Rhino object to a Cup
            Cup sourceCup = source as Cup;

            // Copy properties
            initialPosition = sourceCup.initialPosition;
            _cupType = sourceCup.cupType;
            _apertureAngle = sourceCup._apertureAngle;
            _anteversion = sourceCup._anteversion;
            _inclination = sourceCup._inclination;
            Director = sourceCup.Director;
            _innerCupDiameter = sourceCup.innerCupDiameter;
            coordinateSystem = sourceCup.coordinateSystem;
            defectIsLeft = sourceCup.defectIsLeft;
            _centerOfRotation = sourceCup.centerOfRotation;
        }

        /// <summary>
        /// Called when this object has been picked
        /// </summary>
        /// <param name="context"></param>
        /// <param name="pickedItems">Items that were picked. This parameter is enumerable because there may
        /// have been multiple sub-objects picked</param>
        protected override void OnPicked(PickContext context, IEnumerable<ObjRef> pickedItems)
        {
            base.OnPicked(context, pickedItems);
        }

        /// <summary>
        /// Called when the selection state of this object has changed
        /// </summary>
        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();
        }

        /// <summary>
        /// Called when [transform].
        /// </summary>
        /// <param name="xform">The xform.</param>
        protected override void OnTransform(Transform xform)
        {
            // call superclass method
            base.OnTransform(xform);

            // Move skirt-bone contact curve (if available)
            TransformCupSkirtCurve(xform);

            // Update insertion direction if unset
            if (Director.InsertionDirection == Vector3d.Unset)
                Director.InsertionDirection = -orientation;
        }

        /// <summary>
        /// Transforms the cup skirt curve.
        /// </summary>
        /// <param name="xform">The xform.</param>
        private void TransformCupSkirtCurve(Transform xform)
        {
            // Transform the curve if it exists
            var objManager = new AmaceObjectManager(Director);
            Guid SkirtCupCurveID = objManager.GetBuildingBlockId(IBB.SkirtCupCurve);
            if (Guid.Empty != SkirtCupCurveID)
            {
                Curve SkirtCupCurve = objManager.GetBuildingBlock(IBB.SkirtCupCurve).Geometry as Curve;
                SkirtCupCurve.Transform(xform);
                objManager.SetBuildingBlock(IBB.SkirtCupCurve, SkirtCupCurve, SkirtCupCurveID);
            }

            // Remove dependent blocks
            var dep = new Dependencies();
            dep.DeleteBlockDependencies(Director, IBB.SkirtCupCurve);
        }

        /// <summary>
        /// Computes the brep.
        /// </summary>
        /// <param name="COR">The cor.</param>
        /// <param name="anteversion">The av.</param>
        /// <param name="inclination">The incl.</param>
        /// <param name="apertureAngle">The aperture angle.</param>
        /// <param name="innerDiameter">The inner diameter.</param>
        /// <param name="thickness">The thickness.</param>
        /// <param name="PCS">The PCS.</param>
        /// <param name="defectIsLeft">if set to <c>true</c> [defect is left].</param>
        /// <returns></returns>
        private static Brep ComputeBrep(Point3d COR, CupType cupType, double anteversion, double inclination, double apertureAngle, double innerDiameter, Plane PCS, bool defectIsLeft)
        {
            // NOTE: Has to be static

            List<Curve> allCurves = new List<Curve>();
            if (cupType.CupDesign == CupDesign.v2)
            {
                allCurves = GetRingDesignCupCurves(apertureAngle, innerDiameter, cupType.CupThickness, horizontalBorderWidth, GetPolishingOffsetValue(cupType.CupDesign));
            }
            else
            {
                allCurves = GetSmoothDesignCupCurves(apertureAngle, innerDiameter, cupType.CupThickness);
            }

            return CupEntityFromCurves(allCurves, innerDiameter, anteversion, inclination, COR, PCS, defectIsLeft);
        }

        private static double CalculateHorizontalBorderAngle(double cupInnerRadius, double apertureAngle, double polishingOffset)
        {
            return -(180.0 - apertureAngle) / 2.0 + MathUtilities.CalculateArcAngle(cupInnerRadius, polishingOffset);
        }

        private static double CalculateHorizontalBorderWidthWithPolishingOffset(double horizontalBorderWidth, double polishingOffset, double cupThickness)
        {
            if (Math.Abs(polishingOffset) < Epsilon)
            {
                return horizontalBorderWidth;
            }

            var degrees = 60;
            if (Math.Abs(cupThickness - 2) < Epsilon) //cupThickness = 2
            {
                degrees = 75;
            }

            return horizontalBorderWidth + polishingOffset / Math.Tan(RhinoMath.ToRadians(degrees));
        }

        /// <summary>
        /// Gets the ring design cup curves.
        /// </summary>
        /// <param name="apertureAngle">The aperture angle.</param>
        /// <param name="innerDiameter">The inner diameter.</param>
        /// <param name="thickness">The thickness.</param>
        /// <param name="ringThickness">The ring thickness.</param>
        /// <param name="horizontalBorderWidth">Width of the horizontal border.</param>
        /// <param name="polishingOffset">Polishing offset of cup ring.</param>
        /// <returns></returns>
        public static List<Curve> GetRingDesignCupCurves(double apertureAngle, double innerDiameter, double cupThickness, double horizontalBorderWidth, double polishingOffset)
        {
            // Precalculate some parameters
            var innerRadius = innerDiameter / 2.0;
            var outerRadius = innerRadius + cupThickness;

            var drawingCircleCenter = GetDrawingCircleCenter(innerRadius);

            // Lateral curve
            var lateralCupCurve = CreateLateralCupCurve(drawingCircleCenter, innerDiameter, apertureAngle,
                polishingOffset);

            // Medial curve
            var horizontalBorderAngle = CalculateHorizontalBorderAngle(innerRadius, apertureAngle, 0);
            var medialCupAperture = GetRingDesignMedialCupAperture(horizontalBorderAngle, outerRadius);
            var medialCupCurve = GetMedialCupCurve(innerDiameter, cupThickness, medialCupAperture);

            // Horizontal border curve
            var horizontalBorderCurve = GetHorizontalBorderCurve(lateralCupCurve.PointAtEnd, horizontalBorderWidth, polishingOffset, cupThickness);

            // Create Transition Curve
            var transitionCurve = GetRingDesignTransitionCurve(horizontalBorderCurve, horizontalBorderAngle, innerRadius, cupThickness, horizontalBorderWidth, polishingOffset);

#if (INTERNAL)
            if (ImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddCurve(medialCupCurve, "medialCupCurve", "Testing::Cup Curves V2", Color.Magenta);
                InternalUtilities.AddCurve(transitionCurve, "transitionCurve", "Testing::Cup Curves V2", Color.Magenta);
                InternalUtilities.AddCurve(horizontalBorderCurve, "horizontalBorderCurve", "Testing::Cup Curves V2", Color.Magenta);
                InternalUtilities.AddCurve(lateralCupCurve, "lateralCupCurve", "Testing::Cup Curves V2", Color.Magenta);
            }
#endif

            // Combine curves
            return new List<Curve>() { medialCupCurve, transitionCurve, horizontalBorderCurve, lateralCupCurve };
        }

        /// <summary>
        /// Gets the ring design medial cup aperture.
        /// </summary>
        /// <param name="horizontalBorderAngle">The horizontal border angle.</param>
        /// <param name="outerRadius">The outer radius.</param>
        /// <returns></returns>
        private static double GetRingDesignMedialCupAperture(double horizontalBorderAngle, double outerRadius)
        {
            double medialCupAngle = horizontalBorderAngle - MathUtilities.CalculateArcAngle(outerRadius, referenceEndArcLength);
            double medialCupAperture = 180 + 2 * medialCupAngle;
            return medialCupAperture;
        }

        /// <summary>
        /// Gets the ring design transition curve.
        /// </summary>
        /// <param name="horizontalBorder">The horiozntal border.</param>
        /// <param name="horizontalBorderAngle">The horizontal border angle.</param>
        /// <param name="innerCupRadius">The inner cup radius.</param>
        /// <param name="cupThickness">The cup thickness.</param>
        /// <param name="transitionEndArcLength">Length of the transition end arc.</param>
        /// <param name="horizontalBorderWidth">Width of horizontal border.</param>
        /// <param name="polishingOffset">Polishing offset.</param>
        /// <returns></returns>
        private static Curve GetRingDesignTransitionCurve(Curve horizontalBorder, double horizontalBorderAngle,
            double innerCupRadius, double cupThickness, double horizontalBorderWidth, double polishingOffset)
        {
            var dummyPt = Point3d.Unset;
            var segmentsTmp = new List<Curve>();
            return GetRingDesignTransitionCurve(horizontalBorder, horizontalBorderAngle, innerCupRadius, cupThickness,
                horizontalBorderWidth, polishingOffset, out dummyPt, out segmentsTmp);
        }

        /// <summary>
        /// Gets the ring design transition curve.
        /// </summary>
        /// <param name="horizontalBorder">The horiozntal border.</param>
        /// <param name="horizontalBorderAngle">The horizontal border angle.</param>
        /// <param name="innerCupRadius">The inner cup radius.</param>
        /// <param name="cupThickness">The cup thickness.</param>
        /// <param name="transitionEndArcLength">Length of the transition end arc.</param>
        /// <param name="horizontalBorderWidth">Width of horizontal border.</param>
        /// <param name="polishingOffset">Polishing offset.</param>
        /// <param name="bottomTransitionStartPoint">The point of transition to medial cup curve starts</param>
        /// <returns></returns>
        private static Curve GetRingDesignTransitionCurve(Curve horizontalBorder, double horizontalBorderAngle, double innerCupRadius, double cupThickness,
            double horizontalBorderWidth, double polishingOffset, out Point3d transitionToMedialCupCurveStartPoint, out List<Curve> segments)
        {
            var horizontalBorderAngleWPolishingOffset = horizontalBorderAngle + MathUtilities.CalculateArcAngle(innerCupRadius, polishingOffset);
            var horizontalBorderWidthWPolishingOffset = CalculateHorizontalBorderWidthWithPolishingOffset(horizontalBorderWidth, polishingOffset, cupThickness);
            const double startAngle = -90.0;
            var cupRingThickness = GetRingThickness(cupThickness);
            var outerCupRadius = innerCupRadius + cupThickness;
            var outerRingRadius = innerCupRadius + cupRingThickness + polishingOffset;

            // Arc lengths
            var a2 = 1.25 / referenceEndArcLength * GetTransitionEndArcLength(polishingOffset);
            var a3 = 3.0 / referenceEndArcLength * GetTransitionEndArcLength(polishingOffset);
            var a4 = 4.0 / referenceEndArcLength * GetTransitionEndArcLength(polishingOffset);
            var a5 = 5.0 / referenceEndArcLength * GetTransitionEndArcLength(polishingOffset);
            var a6 = 6.0 / referenceEndArcLength * GetTransitionEndArcLength(polishingOffset);
            
            // Get design points
            var circleDesigner = new CircleDesigner(GetDrawingCircleCenter(innerCupRadius));
            double transitionMiddleCircleRadius = innerCupRadius + ((horizontalBorderWidthWPolishingOffset + cupRingThickness + polishingOffset) / 2); //4 is ringWidth (3+1 and 4+1 both are 4, 2+1 will be 3)
            var p2 = circleDesigner.CreateDesignReferencePoint(startAngle, horizontalBorderAngleWPolishingOffset, outerRingRadius, a2, transitionMiddleCircleRadius);
            var p3 = circleDesigner.CreateDesignReferencePoint(startAngle, horizontalBorderAngleWPolishingOffset, outerRingRadius, a3, outerRingRadius);
            var p4 = circleDesigner.CreateDesignReferencePoint(startAngle, horizontalBorderAngleWPolishingOffset, outerRingRadius, a4, outerRingRadius);
            var p5 = circleDesigner.CreateDesignReferencePoint(startAngle, horizontalBorderAngleWPolishingOffset, outerRingRadius, a5, outerRingRadius);
            transitionToMedialCupCurveStartPoint = p5;

#if (INTERNAL)
            if (ImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddPoint(transitionToMedialCupCurveStartPoint, "transitionToMedialCupCurveStartPoint", "Testing::CupRing", Color.Magenta);
            }
#endif

            var p6 = circleDesigner.CreateDesignReferencePoint(startAngle, horizontalBorderAngleWPolishingOffset, outerRingRadius, a6, outerCupRadius);

            var medialCupAperture = GetRingDesignMedialCupAperture(horizontalBorderAngle, outerCupRadius);
            var medialCupCurve = GetMedialCupCurve(innerCupRadius*2, cupThickness, medialCupAperture);
            var p7 = medialCupCurve.PointAtEnd;

            // Transition Curves
            var topTransition = GetGenericCurve(horizontalBorder.PointAtEnd, p2);
            var topSmooth = GetGenericCurve(p2, p3, p4);
            var bottomTransition = GetGenericCurve(p4, p5);
            var bottomSmooth = GetGenericCurve(p5, p6, p7);

            //Order is important! check its user before change!
            segments = new List<Curve>() { topTransition, topSmooth, bottomTransition, bottomSmooth };
            var transitionCurve = Curve.JoinCurves(segments)[0];
            
            return transitionCurve;
        }

        /// <summary>
        /// Gets the smooth design cup curves.
        /// </summary>
        /// <param name="apertureAngle">The aperture angle.</param>
        /// <param name="innerDiameter">The inner diameter.</param>
        /// <param name="thickness">The thickness.</param>
        /// <returns></returns>
        public static List<Curve> GetSmoothDesignCupCurves(double apertureAngle, double innerDiameter, double thickness)
        {
            var circleDesigner = new CircleDesigner(GetDrawingCircleCenter(innerDiameter / 2));
            // Medial curve
            var medialCupCurve = circleDesigner.CreateCurveOnCircle(innerDiameter / 2 + thickness, GetSmoothDesignMedialAperture() / 2);
            // Lateral curve
            var lateralCupCurve = circleDesigner.CreateCurveOnCircle(innerDiameter / 2, apertureAngle / 2);
            // Horizontal border
            var horizontalBorderCurve = GetGenericCurve(lateralCupCurve.PointAtEnd, lateralCupCurve.PointAtEnd + new Point3d(horizontalBorderWidth, 0, 0));
            // Transition
            var transitionCurve = GetSmoothDesignTransitionCurve(innerDiameter, thickness, medialCupCurve, horizontalBorderCurve);

#if (INTERNAL)
            if (ImplantDirector.IsDebugMode)
            {
                InternalUtilities.AddCurve(medialCupCurve, "medialCupCurve", "Testing::Cup Curves V1", Color.Magenta);
                InternalUtilities.AddCurve(transitionCurve, "transitionCurve", "Testing::Cup Curves V1", Color.Magenta);
                InternalUtilities.AddCurve(horizontalBorderCurve, "horizontalBorderCurve", "Testing::Cup Curves V1", Color.Magenta);
                InternalUtilities.AddCurve(lateralCupCurve, "lateralCupCurve", "Testing::Cup Curves V1", Color.Magenta);
            }
#endif

            // Combine curves
            return new List<Curve>() { medialCupCurve, transitionCurve, horizontalBorderCurve, lateralCupCurve };
        }

        /// <summary>
        /// Gets the smooth design medial aperture.
        /// </summary>
        /// <returns></returns>
        private static double GetSmoothDesignMedialAperture()
        {
            return smoothDesignMedialAperture;
        }

        private static Curve GetSmoothDesignTransitionCurve(double innerDiameter, double thickness, Curve medialCupCurve, Curve horizontalBorderCurve)
        {
            var circleDesigner = new CircleDesigner(GetDrawingCircleCenter(innerDiameter / 2));
            var medialCupCurveExtended = circleDesigner.CreateCurveOnCircle(innerDiameter / 2 + thickness, extendedMedialAperture / 2);
            var transitionCurve = GetGenericCurve(medialCupCurve.PointAtEnd, medialCupCurveExtended.PointAtEnd, horizontalBorderCurve.PointAtEnd);
            return transitionCurve;
        }

        /// <summary>
        /// Cups the entity from curves.
        /// </summary>
        /// <param name="curves">The curves.</param>
        /// <param name="innerDiameter">The inner diameter.</param>
        /// <param name="anteversion">The anteversion.</param>
        /// <param name="inclination">The inclination.</param>
        /// <param name="COR">The cor.</param>
        /// <param name="PCS">The PCS.</param>
        /// <param name="defectIsLeft">if set to <c>true</c> [defect is left].</param>
        /// <returns></returns>
        private static Brep CupEntityFromCurves(List<Curve> curves, double innerDiameter, double anteversion, double inclination, Point3d COR, Plane PCS, bool defectIsLeft)
        {
            // Calculate orientation
            Vector3d orientation = MathUtilities.AnteversionInclinationToVector(anteversion, inclination, PCS, defectIsLeft);
            // Join curves
            Curve combinedCurves = Curve.JoinCurves(curves)[0];
            // Revolve curve to create surface
            Brep fullSurface = RevSurface.Create(combinedCurves, GetRevolveAxis()).ToBrep();

            if (fullSurface.SolidOrientation == BrepSolidOrientation.Inward)
            {
                fullSurface.Flip();
            }

            // Transform to current cup position
            return TransformBrepToCurrentPosition(fullSurface, innerDiameter, orientation, COR);
        }

        /// <summary>
        /// Gets the revolve axis.
        /// </summary>
        /// <returns></returns>
        private static Line GetRevolveAxis()
        {
            return new Line(0, 0, 0, 0, 1, 0); // Y-axis
        }

        /// <summary>
        /// Gets the transition curve.
        /// </summary>
        /// <param name="pt1">The p1.</param>
        /// <param name="pt2">The p2.</param>
        /// <param name="pt3">The p3.</param>
        /// <returns></returns>
        private static Curve GetGenericCurve(Point3d pt1, Point3d pt2, Point3d pt3)
        {
            return new BezierCurve(new List<Point3d>() { pt1, pt2, pt3 }).ToNurbsCurve();
        }

        /// <summary>
        /// Gets the transition curve.
        /// </summary>
        /// <param name="p1">The p1.</param>
        /// <param name="p2">The p2.</param>
        /// <returns></returns>
        private static Curve GetGenericCurve(Point3d p1, Point3d p2)
        {
            return new PolylineCurve(new List<Point3d>() { p1, p2 });
        }

        /// <summary>
        /// Transforms to current position.
        /// </summary>
        /// <param name="brepAtOrigin">The brep at origin.</param>
        /// <param name="innerDiameter">The inner diameter.</param>
        /// <param name="orientation">The orientation.</param>
        /// <param name="COR">The cor.</param>
        /// <returns></returns>
        private static Brep TransformBrepToCurrentPosition(Brep brepAtOrigin, double innerDiameter, Vector3d orientation, Point3d COR)
        {
            // Rotation center
            Point3d rotationcenter = new Point3d(0, innerDiameter / 2, 0);
            Brep transformedBrep = brepAtOrigin.DuplicateBrep();
            TransformToCurrentPosition(transformedBrep, rotationcenter, orientation, COR);
            return transformedBrep;
        }

        private static void TransformToCurrentPosition(GeometryBase geometryAtOrigin, Point3d rotationcenter, Vector3d orientation, Point3d COR)
        {
            // Transformations
            Transform rotation_global = Transform.Rotation(Vector3d.YAxis, orientation, rotationcenter);
            Transform translation_global = Transform.Translation(COR - rotationcenter);

            // Apply transform
            geometryAtOrigin.Transform(rotation_global);
            geometryAtOrigin.Transform(translation_global);
        }

        /// <summary>
        /// Cups the entity from curves.
        /// </summary>
        /// <param name="curves">The curves.</param>
        /// <returns></returns>
        private Brep MakeCupEntityFromCurves(List<Curve> curves)
        {
            return CupEntityFromCurves(curves, this.innerCupDiameter, this.anteversion, this.inclination, this.centerOfRotation, coordinateSystem, defectIsLeft);
        }

        /// <summary>
        /// Cups the entity from curves.
        /// </summary>
        /// <param name="curves">The curves.</param>
        /// <param name="PCS">The PCS.</param>
        /// <param name="defectIsLeft">if set to <c>true</c> [defect is left].</param>
        /// <returns></returns>
        private Brep MakeCupEntityFromCurves(List<Curve> curves, Plane PCS, bool defectIsLeft)
        {
            return CupEntityFromCurves(curves, this.innerCupDiameter, this.anteversion, this.inclination, this.centerOfRotation, PCS, defectIsLeft);
        }

        /// <summary>
        /// Sets the type of the cup.
        /// </summary>
        /// <param name="thickness">The thickness.</param>
        /// <param name="porousThickness">The porous thickness.</param>
        /// <param name="design">The design.</param>
        public CupType cupType
        {
            get
            {
                return _cupType;
            }
            set
            {
                // Do sanity checks before setting the cup type
                if (!CanHaveAsThickness(value.CupThickness))
                {
                    throw new ArgumentOutOfRangeException("Invalid thickness!");
                }
                else if (!CanHaveAsPorousThickness(value.PorousThickness))
                {
                    throw new ArgumentOutOfRangeException("Invalid porous thickness!");
                }
                else
                {
                    _cupType = value;

                    // Replace this cup in the document by a new one with correct parameters
                    Cup alteredCup = new Cup(this, Director);
                }
            }
        }

        /// <summary>
        /// Gets the horizontal border curve.
        /// </summary>
        /// <returns></returns>
        private Curve GetHorizontalBorderCurve(double polishingOffset, double cupThickness)
        {
            return GetHorizontalBorderCurve(lateralCupCurve.PointAtEnd, horizontalBorderWidth, polishingOffset, cupThickness);
        }

        /// <summary>
        /// Gets the horizontal border curve.
        /// </summary>
        /// <param name="lateralCurveEndPoint">The lateral curve end point.</param>
        /// <param name="horizontalBorderWidth">Width of the horizontal border.</param>
        /// <returns></returns>
        private static Curve GetHorizontalBorderCurve(Point3d lateralCurveEndPoint, double horizontalBorderWidth, double polishingOffset, double cupThickness)
        {
            var horWidthPolishOffset = CalculateHorizontalBorderWidthWithPolishingOffset(horizontalBorderWidth, polishingOffset, cupThickness);
            return GetGenericCurve(lateralCurveEndPoint, lateralCurveEndPoint + new Point3d(horWidthPolishOffset, 0, 0));
        }

        /// <summary>
        /// Gets the horizontal inner reaming curve.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        private Curve GetHorizontalInnerReamingCurve(double height = defaultReamingHeight)
        {
            return GetHorizontalReamingCurve(innerCupDiameter, height);
        }

        /// <summary>
        /// Gets the horizontal outer reaming curve.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        private Curve GetHorizontalOuterReamingCurve(double height)
        {
            return GetHorizontalReamingCurve(outerReamingDiameter, height);
        }

        /// <summary>
        /// Gets the horizontal reaming curve.
        /// </summary>
        /// <param name="diameter">The diameter.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        private Curve GetHorizontalReamingCurve(double diameter, double height)
        {
            // Spherical curve to start from
            var circleDesigner = new CircleDesigner(DrawingCircleCenter);
            // Spherical curve should ends at 90 degrees (reamingAperture / 2)
            var sphericalCurve = circleDesigner.CreateCurveOnCircle(diameter / 2, reamingAperture / 2);

            // The line
            var linePoints = new List<Point3d>(2)
            {
                sphericalCurve.PointAtEnd + new Point3d(0, height, 0),
                new Point3d(0, sphericalCurve.PointAtEnd.Y + height, 0)
            };

            return new PolylineCurve(linePoints);
        }

        /// <summary>
        /// Gets the medial cup curve.
        /// </summary>
        /// <param name="medialApertureAngle">The medial aperture angle.</param>
        /// <returns></returns>
        private static Curve GetMedialCupCurve(double innerDiameter, double thickness, double medialApertureAngle)
        {
            var circleDesigner = new CircleDesigner(GetDrawingCircleCenter(innerDiameter / 2));
            return circleDesigner.CreateCurveOnCircle(innerDiameter / 2 + thickness, medialApertureAngle / 2);
        }
        /// <summary>
        /// Gets the reaming volume.
        /// </summary>
        /// <param name="diameter">The diameter.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        private Brep GetReamingVolume(double diameter, double height)
        {
            var circleDesigner = new CircleDesigner(DrawingCircleCenter);
            var sphericalCurve = circleDesigner.CreateCurveOnCircle(diameter / 2, reamingAperture / 2);
            var verticalCurve = GetVerticalReamingCurve(diameter, height);
            var horizontalCurve = GetHorizontalReamingCurve(diameter, height);

            var allCurves = new List<Curve>() { sphericalCurve, verticalCurve, horizontalCurve };
            return MakeCupEntityFromCurves(allCurves);
        }

        /// <summary>
        /// Gets the vertical inner reaming curve.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        private Curve GetVerticalInnerReamingCurve(double height = defaultReamingHeight)
        {
            return GetVerticalReamingCurve(innerCupDiameter, height);
        }

        /// <summary>
        /// Gets the vertical outer reaming curve.
        /// </summary>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        private Curve GetVerticalOuterReamingCurve(double height)
        {
            return GetVerticalReamingCurve(outerReamingDiameter, height);
        }

        /// <summary>
        /// Gets the vertical reaming curve.
        /// </summary>
        /// <param name="diameter">The diameter.</param>
        /// <param name="height">The height.</param>
        /// <returns></returns>
        private Curve GetVerticalReamingCurve(double diameter, double height)
        {
            // Spherical curve to start from
            var circleDesigner = new CircleDesigner(DrawingCircleCenter);
            Curve sphericalCurve = circleDesigner.CreateCurveOnCircle(diameter / 2, reamingAperture / 2);

            // The line
            List<Point3d> linePoints = new List<Point3d>(2);
            linePoints.Add(sphericalCurve.PointAtEnd);
            linePoints.Add(sphericalCurve.PointAtEnd + new Point3d(0, height, 0));
            return new PolylineCurve(linePoints);
        }

        /// <summary>
        /// Gets the inner cup surface without horizontal border as a Brep
        /// </summary>
        public Brep innerCupSurface
        {
            get
            {
                return GetInnerCupSurface(Director.Inspector.pelvicCoordinateSystem, Director.defectIsLeft);
            }
        }

        /// <summary>
        /// Gets the inner cup surface without horizontal border as a Mehs
        /// </summary>
        public Mesh innerCupSurfaceMesh
        {
            get
            {
                return GetInnerCupSurfaceMesh(Director.Inspector.pelvicCoordinateSystem, Director.defectIsLeft);
            }
        }

        /// <summary>
        /// Gets the inner cup surface without horizontal border as a Brep
        /// </summary>
        private Brep GetInnerCupSurface(Plane PCS, bool defectIsLeft)
        {
            // Create entity from curve
            List<Curve> allCurves = new List<Curve>() { lateralCupCurve };
            Brep innerCup = MakeCupEntityFromCurves(allCurves, PCS, defectIsLeft);
            // Flip normals
            innerCup.Flip();

            return innerCup;
        }

        /// <summary>
        /// Gets the inner cup surface without horizontal border as a Mesh
        /// </summary>
        private Mesh GetInnerCupSurfaceMesh(Plane PCS, bool defectIsLeft)
        {
            MeshingParameters meshParamaters = MeshParameters.IDS();
            Mesh innerCupMesh = BrepUtilities.GetCollisionMesh(GetInnerCupSurface(PCS, defectIsLeft), meshParamaters);

            return innerCupMesh;
        }

        public static double GetPolishingOffsetValue(CupDesign design)
        {
            return design == CupDesign.v2 ? 0.2 : 0.0;
        }

        private static Curve CreateLateralCupCurve(Point3d drawingCircleCenter, double innerCupDiameter, double apertureAngle, double polishingOffset)
        {
            return CreateLateralCupCurve(drawingCircleCenter, innerCupDiameter, apertureAngle, polishingOffset, 0);
        }

        private static Curve CreateLateralCupCurve(Point3d drawingCircleCenter, double innerCupDiameter, double apertureAngle, double polishingOffset, double startAngle)
        {
            var innerRadius = innerCupDiameter / 2;
            var circleDesigner = new CircleDesigner(drawingCircleCenter);
            return circleDesigner.CreateCurveOnCircle(innerRadius, apertureAngle / 2 + MathUtilities.CalculateArcAngle(innerRadius, polishingOffset), startAngle);
        }

        /// <summary>
        /// The transition end arc length with polishing offset
        /// </summary>
        private static double GetTransitionEndArcLength(double polishingOffset)
        {
            return referenceEndArcLength + polishingOffset;
        }
    }
}