using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using IDS.Amace.Enumerators;
using IDS.Amace.GUI;
using IDS.Amace.Relations;
using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.Quality;
using IDS.Core.Utilities;
using RhinoMtlsCore.Operations;
#if(INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.Amace.ImplantBuildingBlocks
{

    public class Screw : ScrewBase<ImplantDirector, ScrewType, ScrewAideType>, IBBinterface<ImplantDirector>
    {
        /// <summary>
        /// The key qc cup zone
        /// </summary>
        private const string KeyQcCupZone = "qc_cupzone";

        /// <summary>
        /// The key screw align
        /// </summary>
        private const string KeyScrewAlign = "screw_align";

        /// <summary>
        /// The key screw brand type
        /// </summary>
        private const string KeyScrewBrandType = "screw_brand_type";

        /// <summary>
        /// The maximum body length
        /// </summary>
        public override double MaximumBodyLength => 500.0;

        /// <summary>
        /// The outline entity margin
        /// </summary>
        private const double OutlineEntityMargin = 4.0;

        /// <summary>
        /// The cup rim angle threshold
        /// </summary>
        public const double CupRimAngleThresholdDegrees = 15.0;

        /// <summary>
        /// The minimum bone penetration
        /// </summary>
        private const double MinimumBonePenetration = 10.0;

        /// <summary>
        /// The quality cup zone
        /// </summary>
        private QualityCheckResult _qualityCupZone = QualityCheckResult.OK;

        /// <summary>
        /// The axial offset
        /// </summary>
        protected double _axialOffset;

        /// <summary>
        /// Gets or sets the axial offset.
        /// </summary>
        /// <value>
        /// The axial offset.
        /// </value>
        public double AxialOffset
        {
            get
            {
                return _axialOffset;
            }
            set
            {
                var axialDiff = _axialOffset - value;
                _axialOffset = value;
                // Recalculate head/tip point
                HeadPoint += Direction * axialDiff;
                TipPoint += Direction * axialDiff;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Screw"/> class.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="screwBrandType">BrandType of the screw.</param>
        /// <param name="screwAlignment">The screw alignment.</param>
        /// <param name="newIndex">The new index.</param>
        /// <param name="axialOffsetInit">The axial offset initialize.</param>
        public Screw(ImplantDirector director, ScrewBrandType screwBrandType, ScrewAlignment screwAlignment, int newIndex = 0, double axialOffsetInit = 0.0)
        {
            HeadPoint = Point3d.Unset;
            TipPoint = Point3d.Unset;
            this.screwBrandType = screwBrandType;
            _axialOffset = axialOffsetInit;
            _fixedLength = 0.0; // not set
            this.screwAlignment = screwAlignment;
            Director = director;

            // Setting screw index
            CalculateScrewIndex(newIndex);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Screw"/> class.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="headPoint">The head point.</param>
        /// <param name="tipPoint">The tip point.</param>
        /// <param name="screwType">Type of the screw.</param>
        /// <param name="screwAlignment">The screw alignment.</param>
        /// <param name="newIndex">The new index.</param>
        /// <param name="axialOffsetInit">The axial offset initialize.</param>
        /// <exception cref="ArgumentException">Tip Point too close to Head Point (less than 0.1mm)</exception>
        public Screw(ImplantDirector director, Point3d headPoint, Point3d tipPoint, ScrewType screwType, ScrewAlignment screwAlignment, int newIndex = 0, double axialOffsetInit = 0.0) :
            this(director, headPoint, tipPoint, ScrewBrandTypeConverter.ConvertFromScrewType(screwType), screwAlignment, newIndex, axialOffsetInit)
        {

        }

        public Screw(ImplantDirector director, Point3d headPoint, Point3d tipPoint, ScrewBrandType screwBrandType, ScrewAlignment screwAlignment, int newIndex = 0, double axialOffsetInit = 0.0) :
            base(ComputeBrep(director, headPoint, tipPoint, screwBrandType))
        {
            // Set headpoint
            HeadPoint = headPoint;
            // Set tippoint
            if ((tipPoint - headPoint).Length <= 0.1) // ill defined screw
            {
                throw new ArgumentException("Tip Point too close to Head Point (less than 0.1mm)");
            }

            if ((tipPoint - headPoint).Length < GetScrewLengths(screwBrandType).Min()) // smaller than minimum
            {
                Vector3d direction = tipPoint - headPoint;
                direction.Unitize();
                TipPoint = HeadPoint + GetScrewLengths(screwBrandType).Min() * direction;
            }
            else
            {
                TipPoint = tipPoint;
            }

            // Other properties
            this.screwBrandType = screwBrandType;
            _axialOffset = axialOffsetInit;
            _fixedLength = 0.0; // not set
            this.screwAlignment = screwAlignment;
            Director = director;

            // Setting screw index
            CalculateScrewIndex(newIndex);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Screw"/> class.
        /// </summary>
        public Screw()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Screw"/> class.
        /// </summary>
        /// <param name="brep">The brep.</param>
        public Screw(Brep brep) : base(brep)
        {
            // This is the default constructor called during object copy
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Screw"/> class.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <param name="fromArchive">if set to <c>true</c> [from archive].</param>
        /// <param name="copyAttributes">if set to <c>true</c> [copy attributes].</param>
        public Screw(RhinoObject other, bool fromArchive, bool copyAttributes)
            : this(other.Geometry as Brep)
        {
            // Replace the object in the document or create new one
            if (copyAttributes)
                this.Attributes = other.Attributes;

            // Copy member variables (tries to cast to screw)
            OnDuplicate(other);

            // Load member variables from UserDictionary
            if (!fromArchive)
            {
                return;
            }

            // Load member variables from archive
            var udict = other.Attributes.UserDictionary;
            DeArchive(udict);
        }

        /// <summary>
        /// Gets the augments.
        /// \todo this belongs somewhere else, not in the screw class
        /// </summary>
        /// <value>
        /// The augments.
        /// </value>
        public string AugmentsText
        {
            get
            {
                var augm = "";
                if (ScrewAides.ContainsKey(ScrewAideType.LateralBump))
                {
                    augm = augm + "L";
                }
                if (ScrewAides.ContainsKey(ScrewAideType.MedialBump))
                {
                    augm = augm + "M";
                }
                return augm;
            }
        }

        /// <summary>
        /// Gets the body origin.
        /// </summary>
        /// <value>
        /// The body origin.
        /// </value>
        public override Point3d BodyOrigin => ScrewDatabaseBodyOrigin(Director.ScrewDatabase);

        /// <summary>
        /// Gets the container.
        /// </summary>
        /// <value>
        /// The container.
        /// </value>
        private void CreateContainer()
        {
            CreateAideMeshIfNecessary(ScrewAideType.ScrewContainer, IBB.ScrewContainer, ScrewAideManager.SuffixContainerMesh);
        }

        /// <summary>
        /// Gets the cortical bites.
        /// </summary>
        /// <value>
        /// The cortical bites.
        /// </value>
        public int CorticalBites
        {
            get
            {
                var objManager = new AmaceObjectManager(Director);
                var target = objManager.GetBuildingBlock(IBB.OriginalReamedPelvis).GetMeshes(MeshType.Default)[0];
                return GetCorticalBites(target, ScrewDatabaseBodyOrigin(Director.ScrewDatabase), BodyLength(Director.ScrewDatabase));
            }
        }

        /// <summary>
        /// Gets the cup rim angle.
        /// </summary>
        /// <value>
        /// The cup rim angle.
        /// </value>
        public double CupRimAngleRadians
        {
            get
            {
                var directionAngle = Vector3d.VectorAngle(Direction, Director.cup.orientation);
                return directionAngle - Math.PI / 2;
            }
        }

        public double CupRimAngleDegrees => CupRimAngleRadians / Math.PI * 180;

        /// <summary>
        /// Gets the cushion boolean.
        /// </summary>
        /// <value>
        /// The cushion boolean.
        /// </value>
        private void CreateCushionBoolean()
        {
            CreateAideMeshIfNecessary(ScrewAideType.ScrewCushionSubtractor, IBB.ScrewCushionSubtractor, ScrewAideManager.SuffixCushionBooleanMesh);
        }

        private void CreateAideMeshIfNecessary(ScrewAideType screwAideType, IBB screwAideBuildingBlock, string suffix)
        {
            // If it does not exist in the document, create it
            var screwAideManager = new ScrewAideManager(this, Director.ScrewDatabase);

            var objManager = new AmaceObjectManager(Director);
            if (!ScrewAides.ContainsKey(screwAideType))
                ScrewAides[screwAideType] = objManager.AddNewBuildingBlock(screwAideBuildingBlock, screwAideManager.GetScrewAideMeshGeometryAligned(suffix));
        }

        private Mesh GetAideMesh(ScrewAideType screwAideType)
        {
            // Now you are sure it exists, so return it
            return Document.Objects.Find(ScrewAides[screwAideType]).Geometry as Mesh;
        }

        public override double GetDistanceInBone()
        {
            var objManager = new AmaceObjectManager(Director);
            Mesh target = objManager.GetBuildingBlock(IBB.OriginalReamedPelvis).GetMeshes(MeshType.Default)[0];
            return DistanceInBone(target, Director.ScrewDatabase);
        }

        public override double GetDistanceUntilBone()
        {
            // Get reamed pelvis if available, fixed otherwise
            var objManager = new AmaceObjectManager(Director);
            Mesh target = objManager.GetBuildingBlock(IBB.OriginalReamedPelvis).GetMeshes(MeshType.Default)[0];
            return DistanceUntilBone(target, Director.ScrewDatabase);
        }

        /// <summary>
        /// Gets the fixation.
        /// </summary>
        /// <value>
        /// The fixation.
        /// </value>
        public string Fixation
        {
            get
            {
                var objManager = new AmaceObjectManager(Director);
                var target = objManager.GetBuildingBlock(IBB.OriginalReamedPelvis).GetMeshes(MeshType.Default)[0];
                return GetFixation(target, ScrewDatabaseBodyOrigin(Director.ScrewDatabase), BodyLength(Director.ScrewDatabase));
            }
        }

        /// <summary>
        /// Gets the head.
        /// </summary>
        /// <value>
        /// The head.
        /// </value>
        public Mesh Head
        {
            get
            {
                // Get head contour
                var headContour = GetScrewHeadContour(Director.ScrewDatabase, screwBrandType);
                return GenerateHeadMeshFromContour(headContour);
            }
        }

        /// <summary>
        /// Gets the head calibration mesh.
        /// </summary>
        /// <value>
        /// The head calibration.
        /// </value>
        private Mesh HeadCalibrationMesh
        {
            get
            {
                // Get head contour
                var headContour = ScrewAideManager.GetHeadCalibrationCurve(Director.ScrewDatabase, screwBrandType);
                return GenerateHeadMeshFromContour(headContour);
            }
        }

        /// <summary>
        /// Gets the head center.
        /// </summary>
        /// <value>
        /// The head center.
        /// </value>
        public Point3d HeadCenter
        {
            get
            {
                var screwAideManager = new ScrewAideManager(this, Director.ScrewDatabase);
                return screwAideManager.GetHeadCenter();
            }
        }

        private Point3d _headPoint;
        /// <summary>
        /// Gets the head point.
        /// </summary>
        /// <value>
        /// The head point.
        /// </value>
        public override Point3d HeadPoint
        {
            get { return _headPoint; }
            protected set
            {
                _headPoint = value;
                ResetHeadCalibrationPoint();
            }
        }


        private Point3d _headCalibrationPoint;
        /// <summary>
        /// Gets the head calibration point.
        /// </summary>
        /// <value>
        /// The head calibration point. AUTOSET BY SETTING headPoint!
        /// </value>
        public Point3d HeadCalibrationPoint
        {
            get { return _headCalibrationPoint; }
            set
            {
                _headCalibrationPoint = value;
                ResetHeadPoint();
            }
        }

        private void ResetHeadPoint()
        {
            //It is possible the type is not yet set, but setting the type will invoke this
            if (screwBrandType == null)
            {
                return;
            }

            var orientation = TipPoint - _headCalibrationPoint;
            _headPoint = ScrewAideManager.GetHeadPointTransformed(screwBrandType, orientation, _headCalibrationPoint);
        }

        private void ResetHeadCalibrationPoint()
        {
            //It is possible to invoke this method when screwBrandType is not set when headPoint is being set.
            //ScrewBrandType can be set after headPoint, which will invoke this method as well.
            if (screwBrandType == null)
            {
                return;
            }

            var orientation = TipPoint - _headPoint;
            _headCalibrationPoint = ScrewAideManager.GetHeadCalibrationPointTransformed(screwBrandType, orientation, _headPoint);
        }

        /// <summary>
        /// Gets the head radius.
        /// </summary>
        /// <value>
        /// The head radius.
        /// </value>
        public double HeadRadius
        {
            get
            {
                // Get head contour
                var headContour = GetScrewHeadContour(Director.ScrewDatabase, screwBrandType);
                return headContour.GetBoundingBox(true).Max.X;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is bicortical.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is bicortical; otherwise, <c>false</c>.
        /// </value>
        public override bool IsBicortical => CorticalBites > 1;

        public Mesh CreateLateralBump()
        {
            // If it does not exist yet
            if (ScrewAides.ContainsKey(ScrewAideType.LateralBump))
            {
                return null;
            }

            // Flange screws : Should it exist and it doesnt, create it
            if (positioning == ScrewPosition.Flange)
            {
                CreateLateralFlangeBump();
            }

            return null;
        }

        public Mesh LateralBump
        {
            get
            {
                Mesh bumpMesh = null;

                // If it exists already, return it
                if (!ScrewAides.ContainsKey(ScrewAideType.LateralBump))
                {
                    return null;
                }

                var theObj = Document.Objects.Find(ScrewAides[ScrewAideType.LateralBump]);
                // This will make sure the error in the file gets fixed, but it does not fix the
                // root cause See bug Frederik_2016/01/06
                if (theObj != null)
                {
                    bumpMesh = theObj.Geometry as Mesh;
                }
                else
                {
                    IDSPluginHelper.WriteLine(LogCategory.Error, "!!!!!!!!!!!!! Contact Innovation !!!!!!!!!!!!!!");
                    IDSPluginHelper.WriteLine(LogCategory.Error, "A screw has a lateral bump container dictionary entry without having an existing rhino object");
                    IDSPluginHelper.WriteLine(LogCategory.Error, "The entry will be removed");
                    IDSPluginHelper.WriteLine(LogCategory.Error, "Try to remember your last actions and take screenshots if necessary");
                    IDSPluginHelper.WriteLine(LogCategory.Error, "!!!!!!!!!!!!! Contact Innovation !!!!!!!!!!!!!!");
                    ScrewAides.Remove(ScrewAideType.LateralBump);
                }

                return bumpMesh;
            }
        }

        private void CreateLateralFlangeBump()
        {
            // Create mesh
            var screwAideManager = new ScrewAideManager(this, Director.ScrewDatabase);
            var bump = screwAideManager.GetLateralBumpMesh();
            // Add building block
            var objManager = new AmaceObjectManager(Director);
            ScrewAides[ScrewAideType.LateralBump] = objManager.AddNewBuildingBlock(IBB.LateralBump, bump);
        }

        /// <summary>
        /// Gets the lateral bump trimmed.
        /// </summary>
        /// <value>
        /// The lateral bump trimmed.
        /// </value>
        public void CreateTrimmedLateralBump()
        {
            // try to create lateral bump first, this is to make sure the lateral bump needs to
            // exist See bug Frederik_2016/01/06
            // \todo not sure if this is still necessary
            CreateLateralBump();

            // Create trimmed bump if necessary and return
            CreateTrimmedBump(ScrewAideType.LateralBumpTrim, IBB.LateralBumpTrim, ScrewAideType.LateralBump);
        }

        public Mesh lateralTrimmedBump => GetTrimmedBump(ScrewAideType.LateralBumpTrim);

        private Mesh GetTrimmedBump(ScrewAideType trimmedBumpAideType)
        {
            Mesh trimmedBump;

            if (!ScrewAides.ContainsKey(trimmedBumpAideType))
            {
                return null;
            }

            // The bump can be empty when completely eaten up by the plate or outside the plate
            if (Document.Objects.Find(ScrewAides[trimmedBumpAideType]) != null)
            {
                trimmedBump = Document.Objects.Find(ScrewAides[trimmedBumpAideType]).Geometry as Mesh;
            }
            else if (ScrewAides[trimmedBumpAideType] == Guid.Empty)
            {
                trimmedBump = new Mesh(); // return empty mesh
            }
            else
            {
                trimmedBump = null;
            }

            return trimmedBump;
        }

        private void CreateTrimmedBump(ScrewAideType trimmedBumpAideType, IBB trimmedBumpBlockType, ScrewAideType bumpAideType)
        {

            // Trimmed bump should be created if untrimmed bump exists
            var untrimmedBumpExists = ScrewAides.ContainsKey(bumpAideType);
            var trimmedBumpExists = ScrewAides.ContainsKey(trimmedBumpAideType);
            if (!untrimmedBumpExists || trimmedBumpExists)
            {
                return;
            }

#if DEBUG
            var timer = new Stopwatch();
            timer.Start();
#endif
            var objManager = new AmaceObjectManager(Director);
            if (trimmedBumpBlockType == IBB.LateralBumpTrim)
            {
                ScrewAides[trimmedBumpAideType] = objManager.AddNewBuildingBlock(trimmedBumpBlockType, CreateLateralBumpTrimmed());
            }
            else if (trimmedBumpBlockType == IBB.MedialBumpTrim)
            {
                ScrewAides[trimmedBumpAideType] = objManager.AddNewBuildingBlock(trimmedBumpBlockType, CreateMedialBumpTrimmed());
            }
            else
            {
                throw new Exception("Trimmed bump type not supported.");
            }
#if DEBUG
            timer.Stop();
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, "Created {0} in {1:mm\\:ss\\.fffff}", trimmedBumpBlockType.ToString(), timer.Elapsed);
#endif
        }

        /// <summary>
        /// Gets the medial bump.
        /// </summary>
        /// <value>
        /// The medial bump.
        /// </value>
        public void CreateMedialBump()
        {
            // If it does not exist
            if (ScrewAides.ContainsKey(ScrewAideType.MedialBump))
            {
                return;
            }

            var creator = new ScrewMedialBumpCreator(Director.ScrewDatabase, Director.cup);
            if (!creator.ScrewShouldHaveMedialBump(this))
            {
                return;
            }

            var bump = creator.CreateMedialBumpForScrewWithMedialBump(this);
            // Add building block
            var objManager = new AmaceObjectManager(Director);
            ScrewAides[ScrewAideType.MedialBump] = objManager.AddNewBuildingBlock(IBB.MedialBump, bump);
        }

        public void CreateTrimmedMedialBump()
        {
            CreateTrimmedBump(ScrewAideType.MedialBumpTrim, IBB.MedialBumpTrim, ScrewAideType.MedialBump);
        }

        public Mesh MedialTrimmedBump => GetTrimmedBump(ScrewAideType.MedialBumpTrim);

        /// <summary>
        /// Gets the outline entity.
        /// </summary>
        /// <value>
        /// The outline entity.
        /// </value>
        private void CreateOutlineEntity()
        {
            CreateAideMeshIfNecessary(ScrewAideType.OutlineEntity, IBB.ScrewOutlineEntity, ScrewAideManager.SuffixOutlineMesh);
        }

        /// <summary>
        /// Gets the outline radius.
        /// </summary>
        /// <value>
        /// The outline radius.
        /// </value>
        public double OutlineRadius => Radius + OutlineEntityMargin;

        /// <summary>
        /// Gets the plastic boolean.
        /// </summary>
        /// <value>
        /// The plastic boolean.
        /// </value>
        private void CreatePlasticBoolean()
        {
            CreateAideMeshIfNecessary(ScrewAideType.ScrewPlasticSubtractor, IBB.ScrewPlasticSubtractor, ScrewAideManager.SuffixPlasticBooleanMesh);
        }

        /// <summary>
        /// Gets the positioning.
        /// </summary>
        /// <value>
        /// The positioning.
        /// </value>
        public virtual ScrewPosition positioning
        {
            get
            {
                var objManager = new AmaceObjectManager(Director);
                // Get necessary parts
                var wrapSunkScrew = objManager.GetBuildingBlock(IBB.WrapSunkScrew).Geometry as Mesh;
                var lateralCup = Director.cup.lateralCupMesh;
                // Calculate position
                return Positioning(wrapSunkScrew, lateralCup);
            }
        }

        /// <summary>
        /// Gets the scaffold boolean.
        /// </summary>
        /// <value>
        /// The scaffold boolean.
        /// </value>
        public void CreateScaffoldBoolean()
        {
            CreateAideMeshIfNecessary(ScrewAideType.ScrewMbvSubtractor, IBB.ScrewMbvSubtractor, ScrewAideManager.SuffixScaffoldBooleanMesh);
        }

        /// <summary>
        /// Gets or sets the screw alignment.
        /// </summary>
        /// <value>
        /// The screw alignment.
        /// </value>
        public ScrewAlignment screwAlignment
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the screwhole bottom radius.
        /// </summary>
        /// <value>
        /// The screwhole bottom radius.
        /// </value>
        public double ScrewHoleBottomRadius
        {
            get
            {
                // Get head contour
                var screwAideManager = new ScrewAideManager(this, Director.ScrewDatabase);
                var screwholeContour = screwAideManager.GetSubtractorCurve();
                var bottomZ = screwholeContour.GetBoundingBox(true).Min.Z;
                var maxX = double.MinValue;
                foreach (var knot in screwholeContour.ToNurbsCurve().Knots)
                {
                    var pointX = screwholeContour.PointAt(knot).X;
                    if (Math.Abs(screwholeContour.PointAt(knot).Z - bottomZ) < 0.00001 && pointX > maxX)
                    {
                        maxX = pointX;
                    }
                }
                return maxX;
            }
        }

        /// <summary>
        /// Gets the screw hole subtractor.
        /// </summary>
        /// <value>
        /// The screw hole subtractor.
        /// </value>
        public void CreateScrewHoleSubtractor()
        {
            CreateAideMeshIfNecessary(ScrewAideType.ScrewHoleSubtractor, IBB.ScrewHoleSubtractor, ScrewAideManager.SuffixSubtractorMesh);
        }

        /// <summary>
        /// Gets the screwhole top radius.
        /// </summary>
        /// <value>
        /// The screwhole top radius.
        /// </value>
        public double ScrewHoleTopRadius
        {
            get
            {
                // Get head contour
                var screwAideManager = new ScrewAideManager(this, Director.ScrewDatabase);
                var screwholeContour = screwAideManager.GetSubtractorCurve();
                return screwholeContour.GetBoundingBox(true).Max.X;
            }
        }

        /// <summary>
        /// Gets the screw lengths.
        /// </summary>
        /// <value>
        /// The screw lengths.
        /// </value>
        protected override double[] ScrewLengths => GetScrewLengths(screwBrandType);

        private ScrewBrandType _screwBrandType;
        /// <summary>
        /// Gets or sets the brand and type of the screw.
        /// </summary>
        /// <value>
        /// The brand and type of the screw.
        /// </value>
        public ScrewBrandType screwBrandType
        {
            get { return _screwBrandType; }
            set
            {
                _screwBrandType = value;
                //Calibrate the HeadCalibration first because when screw is positioned the head point
                //is defined first on mouse cursor position where calibration point is at (0,0,0).
                ResetHeadCalibrationPoint();
                ResetHeadPoint();
            }
        }

        /// <summary>
        /// Gets the stud selector.
        /// </summary>
        /// <value>
        /// The stud selector.
        /// </value>
        public void CreateStudSelector()
        {
            CreateAideMeshIfNecessary(ScrewAideType.StudSelector, IBB.ScrewStudSelector, ScrewAideManager.SuffixStudSelectorMesh);
        }

        /// <summary>
        /// Gets the tip point.
        /// </summary>
        /// <value>
        /// The tip point.
        /// </value>
        public override Point3d TipPoint
        {
            get;
            protected set;
        }

        /// <summary>
        /// Calculates the default length of the screw.
        /// </summary>
        /// <param name="screwType">Type of the screw.</param>
        /// <param name="targetMeshTip">The target mesh tip.</param>
        /// <param name="headPoint">The head point.</param>
        /// <param name="rayDir">The ray dir.</param>
        /// <param name="dist">The dist.</param>
        /// <param name="maxRayDist">The maximum ray dist.</param>
        /// <returns></returns>
        private static bool CalculateDefaultScrewLength(ScrewBrandType screwBrandType, Mesh targetMeshTip, Point3d headPoint, Vector3d rayDir, out double dist, double maxRayDist = 200.0)
        {
            // init
            dist = 0;
            int[] face_ids;

            // Make sure facenormals are there
            targetMeshTip.FaceNormals.ComputeFaceNormals();

            // Create ray
            rayDir.Unitize();
            var rayLine = new Line(headPoint, rayDir, maxRayDist);

            // Intersect mesh with line
            var hitPts = Intersection.MeshLine(targetMeshTip, rayLine, out face_ids);
            if (face_ids == null)
            {
                return false;
            }

            // Loop over all intersection points and select the last intersection point going out of
            // the bone
            var hitsAndFaceIds = hitPts.Zip(face_ids, (h, f) => new { HitPt = h, FaceId = f });
            foreach (var hf in hitsAndFaceIds)
            {
                Vector3d theNormal = targetMeshTip.FaceNormals[hf.FaceId];
                // if ray is going into the bone, do not consider the point
                if (theNormal * rayLine.Direction <= 0)
                {
                    continue;
                }

                // if ray is going out of the bone, check for max length
                var raylength = (rayLine.From - hf.HitPt).Length;
                if (raylength > dist)
                {
                    dist = raylength;
                }
            }

            // Add the radius to the length to make sure the screw sticks out of the bone
            dist = dist + GetDiameter(screwBrandType) / 2.0;

            // Success
            return true;
        }

        /// <summary>
        /// Computes the brep.
        /// </summary>
        /// <param name="director">The director.</param>
        /// <param name="headPoint">The head point.</param>
        /// <param name="tipPoint">The tip point.</param>
        /// <param name="screwType">Type of the screw.</param>
        /// <returns></returns>
        private static Brep ComputeBrep(ImplantDirector director, Point3d headPoint, Point3d tipPoint, ScrewBrandType screwBrandType)
        {
            // Get head contour
            var headContour = GetScrewHeadContour(director.ScrewDatabase, screwBrandType);

            // Screw parameters
            if (headContour.PointAtStart.X > headContour.PointAtEnd.X)
            {
                headContour.Reverse();
            }

            var orientation = tipPoint - headPoint;
            var totalLength = orientation.Length;
            orientation.Unitize();
            var headLength = Math.Abs(headContour.PointAtStart.Z - headContour.PointAtEnd.Z);
            var bodyRadius = Math.Abs(headContour.PointAtEnd.X);
            var bodyLength = totalLength - headLength;

            // Create screw Brep from contour and revolve
            var screwAxis = -Vector3d.ZAxis;
            var headOrigin = headContour.PointAtStart;
            var bodyStart = headContour.PointAtEnd;
            var bodyOrigin = bodyStart;
            bodyOrigin.X = 0.0; // highest center point of screw body

            // Create full contour and add head contour as first part
            var fullContour = new PolyCurve();
            fullContour.Append(headContour);
            fullContour.RemoveNesting(); // In case contour already was polycurve

            // Make part of contour representing the body
            var bodyLineEnd = bodyStart + (screwAxis * (bodyLength - bodyRadius));
            var bodyLine = new Line(bodyStart, bodyLineEnd);
            fullContour.Append(bodyLine);

            // Make part of contour representing the tip
            var tipEnd = bodyOrigin + (screwAxis * bodyLength);
            var tipLine = new Line(bodyLineEnd, tipEnd);
            fullContour.Append(tipLine);

            // Create revolution surface (closed surface = solid)
            var revAxis = new Line(headOrigin, tipEnd);
            var revSurf = RevSurface.Create(fullContour, revAxis);
            var solidScrew = Brep.CreateFromRevSurface(revSurf, true, true);

            // Subtract hexagon from head
            var offset = ScrewAideManager.GetHeadAndHeadCalibrationOffset(director.ScrewDatabase, screwBrandType);
            var hexaPoints = new List<Point3d>();
            var r = 1.75; // temp
            var rd = Math.Sqrt(3) / 2 * r;
            var depth = -1.5 - offset;
            hexaPoints.Add(new Point3d(r / 2, rd, depth));
            hexaPoints.Add(new Point3d(r, 0, depth));
            hexaPoints.Add(new Point3d(r / 2, -rd, depth));
            hexaPoints.Add(new Point3d(-r / 2, -rd, depth));
            hexaPoints.Add(new Point3d(-r, 0, depth));
            hexaPoints.Add(new Point3d(-r / 2, rd, depth));
            hexaPoints.Add(new Point3d(r / 2, rd, depth));
            var hexaLine = new Polyline(hexaPoints);
            var hexagon = Surface.CreateExtrusion(hexaLine.ToNurbsCurve(), (-depth + 1) * (-screwAxis)).ToBrep().CapPlanarHoles(0.01);
            hexagon.Flip();

            try
            {
                // Subtract screw head hexagon from screw
                solidScrew = Brep.CreateBooleanDifference(solidScrew, hexagon, 0.01)[0];
            }
            catch
            {
                // Do not subtract screw head hexagon, this is sometimes impossible in dynamic screw drawing
            }

            // Transform to align with screw
            solidScrew.Transform(ScrewAideManager.GetAlignmentTransform(orientation, headPoint, screwBrandType));

            return solidScrew;
        }

        /// <summary>
        /// Creates from archived.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <param name="replaceInDoc">if set to <c>true</c> [replace in document].</param>
        /// <returns></returns>
        public static Screw CreateFromArchived(RhinoObject other, bool replaceInDoc)
        {
            // Restore the screw object from archive
            var restored = new Screw(other, true, true);

            // Replace if necessary
            if (!replaceInDoc)
            {
                return restored;
            }

            var replaced = IDSPluginHelper.ReplaceRhinoObject(other, restored);
            if (!replaced)
            {
                return null;
            }

            return restored;
        }

        /// <summary>
        /// Gets the diameter.
        /// </summary>
        /// <param name="screwType">BrandType of the screw.</param>
        /// <returns></returns>
        private static double GetDiameter(ScrewBrandType screwType)
        {
            var query = new ScrewQuery();
            return query.GetDiameter(screwType);
        }

        /// <summary>
        /// Gets the screw lengths.
        /// </summary>
        /// <param name="screwBrandType">BrandType of the screw.</param>
        /// <returns></returns>
        public static double[] GetScrewLengths(ScrewBrandType screwBrandType)
        {
            var query = new ScrewQuery();
            return query.GetScrewLengths(screwBrandType);
        }

        /// <summary>
        /// Bodies the length.
        /// </summary>
        /// <param name="screwDatabase">The screw database.</param>
        /// <returns></returns>
        private double BodyLength(File3dm screwDatabase)
        {
            return (TipPoint - ScrewDatabaseBodyOrigin(screwDatabase)).Length;
        }

        /// <summary>
        /// Bodies the origin.
        /// </summary>
        /// <param name="screwDatabase">The screw database.</param>
        /// <returns></returns>
        private Point3d ScrewDatabaseBodyOrigin(File3dm screwDatabase)
        {
            return this.HeadPoint + this.Direction * HeadHeight(screwDatabase);
        }

        public Mesh GetCalibrationCollisionMesh()
        {
            var collisionMesh = new Mesh();
            if (screwAlignment == ScrewAlignment.Sunk)
            {
                // Screw head mesh for collision detection
                collisionMesh = HeadCalibrationMesh;
            }
            else // if (screwAlignment == ScrewAlignment.Floating)
            {
                // Container mesh for collision detection
                var screwAideManager = new ScrewAideManager(this, Director.ScrewDatabase);
                var collisionMeshParts = new [] { screwAideManager.GetContainerMesh() };
                foreach (var part in collisionMeshParts)
                {
                    collisionMesh.Append(part);
                }
            }

            return collisionMesh;
        }

        public Mesh GetCalibrationTargetEntity(bool takePositionIntoAccount)
        {
            var objManager = new AmaceObjectManager(Director);

            var targetEntity = new Mesh();
            if (screwAlignment == ScrewAlignment.Sunk)
            {
                // Check collison with WrapSunkScrew
                if (takePositionIntoAccount && positioning == ScrewPosition.Cup)
                {
                    targetEntity = (Mesh)objManager.GetBuildingBlock(IBB.LateralCupSubtractor).Geometry;
                }
                else
                {
                    targetEntity = (Mesh)objManager.GetBuildingBlock(IBB.WrapSunkScrew).Geometry;
                }

            }
            else // if (screwAlignment == ScrewAlignment.Floating)
            {
                // Check collison with WrapBottom
                targetEntity = (Mesh)objManager.GetBuildingBlock(IBB.WrapBottom).Geometry;
            }

            return targetEntity;
        }

        private bool CalibrateHeadPoint(Vector3d initialDirection, Mesh collisionMesh, Mesh targetEntity, out Point3d calibratedHeadPoint)
        {
            // Set initial step size and shrink factor
            var step = 5.0;
            const double alpha = 0.5;
            const double stepMin = 0.01;
            // Stop condition on iterations per step size
            const int stepMaxIters = 25;
            var stepIters = 0;

            // Initialize
            var collisionDetected = true;
            var findCollision = false;
            var moveDirection = initialDirection;
            calibratedHeadPoint = HeadCalibrationPoint; // Initial position moving head point
            while (step > stepMin || collisionDetected)
            {
                // Move in direction until colliding or free
                stepIters = 0;
                while (findCollision != collisionDetected && stepIters < stepMaxIters)
                {
                    // Move head and container
                    calibratedHeadPoint += step * moveDirection;
                    collisionMesh.Translate(step * moveDirection);

                    // Check collision
                    var intersAcc = Intersection.MeshMeshAccurate(targetEntity, collisionMesh, 0.001);
                    if (intersAcc == null)
                    {
                        collisionDetected = false;
                    }
                    else
                    {
                        collisionDetected = intersAcc.Length != 0;
                    }
                    
                    stepIters++;
                }

                if (stepIters == stepMaxIters)
                {
                    break;
                }

                // Flip direction
                moveDirection = -moveDirection;
                // Decrease step size
                step *= alpha;
                // Switch search for collision or no collision
                findCollision = !findCollision;
            }

            return stepIters != stepMaxIters;
        }

        /// <summary>
        /// Calibrates this instance.
        /// </summary>
        /// <returns></returns>
        public bool CalibrateScrewHead()
        {
            // Sunk screw: move forward first, otherwise: move backward first
            var moveDirection = screwAlignment == ScrewAlignment.Sunk ? Direction : -Direction;

            // Collision detection mesh
            var collisionMesh = GetCalibrationCollisionMesh();

            // Target entity for collision detection
            var targetEntity = GetCalibrationTargetEntity(true);

            // Move back and forth and detect collision
            Point3d calibratedHeadPoint;
            var calibrated = CalibrateHeadPoint(moveDirection, collisionMesh, targetEntity, out calibratedHeadPoint);

            // Set head point
            if (calibrated)
            {
                HeadCalibrationPoint = calibratedHeadPoint - Direction * AxialOffset;
            }
            else
            {
                IDSPluginHelper.WriteLine(LogCategory.Warning, "Screw {0:D} could not be initialized.", Index);
            }

            return true;
        }

        /// <summary>
        /// Checks the bone penetration.
        /// </summary>
        /// <returns></returns>
        public QualityCheckResult CheckBonePenetration()
        {
            // Return value indicates if enough bone is penetrated (true is good)
            return GetDistanceInBone() >= MinimumBonePenetration ? QualityCheckResult.OK : QualityCheckResult.NotOK;
        }

        /// <summary>
        /// Checks the cup rim angle.
        /// </summary>
        /// <returns></returns>
        public QualityCheckResult CheckCupRimAngle()
        {
            return CupRimAngleDegrees >= CupRimAngleThresholdDegrees ? QualityCheckResult.OK : QualityCheckResult.NotOK;
        }

        /// <summary>
        /// Checks the cup zone.
        /// </summary>
        /// <returns></returns>
        public QualityCheckResult CheckCupZone()
        {
            // Do not check for sunk screws with axialOffset = 0
            if (screwAlignment == ScrewAlignment.Sunk && Math.Abs(AxialOffset) < 0.00001)
            {
                return QualityCheckResult.OK;
            }


            // Calculate
            return _qualityCupZone;
        }

        /// <summary>
        /// Checks the length of the screw.
        /// </summary>
        /// <returns></returns>
        public QualityCheckResult CheckScrewLength()
        {
            return TotalLength < 14 ? QualityCheckResult.NotOK : QualityCheckResult.OK;
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />.
        /// </returns>
        public int CompareTo(Screw other)
        {
            return Index - other.Index;
        }

        /// <summary>
        /// Creates the aides.
        /// </summary>
        /// <returns></returns>
        protected override void CreateAides()
        {
            CreateLateralBump();
            CreateMedialBump();
            CreateContainer();
            CreateScrewHoleSubtractor();
            CreateScaffoldBoolean();
            CreateCushionBoolean();
            CreatePlasticBoolean();
            CreateOutlineEntity();
            CreateStudSelector();
        }

        public Mesh StudSelector => GetAideMesh(ScrewAideType.StudSelector);

        public Mesh ScrewHoleSubtractor => GetAideMesh(ScrewAideType.ScrewHoleSubtractor);

        public Mesh ScaffoldBoolean => GetAideMesh(ScrewAideType.ScrewMbvSubtractor);

        public Mesh MedialBump => GetAideMesh(ScrewAideType.MedialBump);

        /// <summary>
        /// De-serialize member variables from archive.
        /// </summary>
        /// <param name="userDict">The user dictionary.</param>
        public void DeArchive(ArchivableDictionary userDict)
        {
            // Load dimension parameters
            var res = false;

            ScrewAlignment scrAlignment;
            res = userDict.TryGetEnumValue<ScrewAlignment>(KeyScrewAlign, out scrAlignment);
            if (res)
            {
                screwAlignment = scrAlignment;
            }

            // Load position parameters
            HeadPoint = userDict.GetPoint3d(KeyHeadPoint, Point3d.Unset);
            TipPoint = userDict.GetPoint3d(KeyTipPoint, Point3d.Unset);
            _axialOffset = userDict.GetDouble(KeyAxialOffset, 0.0);
            Index = userDict.GetInteger(KeyIndex, 0);
            _fixedLength = userDict.GetDouble(KeyFixedLength, 0.0);

            if (userDict.ContainsKey(KeyScrewBrandType))
            {
                screwBrandType = ScrewBrandTypeConverter.ConvertFromArchivableDictionary(userDict.GetDictionary(KeyScrewBrandType));
            }
            else
            {
                ScrewType screwType;
                res = userDict.TryGetEnumValue<ScrewType>(KeyScrewType, out screwType);
                if (res)
                {
                    screwBrandType = ScrewBrandTypeConverter.ConvertFromScrewType(screwType);
                }
                else
                {
                    screwBrandType = ScrewBrandTypeConverter.ConvertFromScrewType(ScrewType.AO_D45);
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Failed to get ScrewType. ScrewBrandType will be set to AO_D45");
                }
            }

            // Quality checks
            QualityCheckResult qualityCupZoneValue;
            res = userDict.TryGetEnumValue<QualityCheckResult>(KeyQcCupZone, out qualityCupZoneValue);
            if (res)
            {
                _qualityCupZone = qualityCupZoneValue;
            }

            // Load aide GUIDs
            foreach (ScrewAideType key in Enum.GetValues(typeof(ScrewAideType)))
            {
                var screwAideId = userDict.GetGuid(key.ToString(), Guid.Empty);
                if (screwAideId != Guid.Empty)
                {
                    ScrewAides.Add(key, screwAideId);
                }
            }
        }

        /// <summary>
        /// Distances the in bone.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="screwDatabase">The screw database.</param>
        /// <returns></returns>
        private double DistanceInBone(Mesh target, File3dm screwDatabase)
        {
            // Shoot rays along screw direction to find intersection with bone
            var rayorigin = new List<Point3d>
            {
                ScrewDatabaseBodyOrigin(screwDatabase)
            };
            List<Point3d[]> rayHits;
            List<double[]> hitDists;
            List<int[]> hitFaceIdx;
            var bodyLength = BodyLength(screwDatabase);
            target.IntersectWithRays(rayorigin, Direction, MaximumBodyLength, 20.0, out rayHits, out hitDists, out hitFaceIdx);
            var bonedist = 0.0;
            if (hitDists.Count <= 0 || hitDists[0].Length <= 0)
            {
                return bonedist;
            }

            Array.Sort(hitDists[0]);
            // Cycle through all the hit points and sum bone penetration distances Depends on
            // whether first point is inside or outside, so use
            var startInside = false;
            var pos_dists = new List<double>();
            for (var i = 0; i < hitDists[0].Length; i++)
            {
                if (hitDists[0][i] < 0)
                {
                    startInside = true;
                }
                else if (hitDists[0][i] >= 0 && hitDists[0][i] <= bodyLength)
                {
                    pos_dists.Add(hitDists[0][i]);
                }
                else if (hitDists[0][i] >= 0 && hitDists[0][i] > bodyLength && pos_dists.Count != 0 &&
                         Math.Abs(pos_dists.Last() - bodyLength) > 0.00001)
                {
                    pos_dists.Add(bodyLength);
                    break;
                }
                else
                {
                    return 0.0; // something went wrong 
                }
            }
            if (startInside)
            {
                pos_dists.Insert(0, 0.0);
            }

            bonedist = (pos_dists[pos_dists.Count - 1] - pos_dists[0]);

#if (INTERNAL)
            if (!ImplantDirector.IsDebugMode)
            {
                return bonedist;
            }

            foreach (var hits in rayHits)
            {
                hits.ToList().ForEach(p => InternalUtilities.AddPoint(p, "Penetration Points DistanceInBone", $"Testing::Screws::Screw {Index} of {screwBrandType}", Color.Aqua));
            }
#endif
            return bonedist;
        }

        /// <summary>
        /// Distances the until bone.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="screwDatabase">The screw database.</param>
        /// <returns></returns>
        private double DistanceUntilBone(Mesh target, File3dm screwDatabase)
        {
            // Shoot rays along screw direction to find intersection with bone
            var rayorigin = new List<Point3d>();
            rayorigin.Add(ScrewDatabaseBodyOrigin(screwDatabase));
            List<Point3d[]> rayHits;
            List<double[]> hitDists;
            List<int[]> hitFaceIdx;
            var bodyLength = BodyLength(screwDatabase);
            target.IntersectWithRays(rayorigin, Direction, MaximumBodyLength, 20.0, out rayHits, out hitDists, out hitFaceIdx);
            var bonedist = 0.0;
            if (hitDists.Count > 0 && hitDists[0].Length > 0)
            {
                Array.Sort(hitDists[0]);
                // Cycle through all the hit points and sum bone penetration distances Depends on
                // whether first point is inside or outside, so use
                var posDists = new List<double>();
                for (var i = 0; i < hitDists[0].Length; i++)
                {
                    if (hitDists[0][i] < 0)
                    {
                        return 0;
                    }

                    if (hitDists[0][i] >= 0 && hitDists[0][i] <= bodyLength)
                    {
                        posDists.Add(hitDists[0][i]);
                        break;
                    }

                    if (!(hitDists[0][i] >= 0) || !(hitDists[0][i] > bodyLength) || posDists.Count == 0 ||
                        Math.Abs(posDists.Last() - bodyLength) < 0.00001)
                    {
                        return double.MaxValue; // something went wrong

                    }

                    posDists.Add(bodyLength);
                    break;
                }
                bonedist = (posDists[0]);
            }

#if (INTERNAL)
            if (!ImplantDirector.IsDebugMode)
            { return bonedist; }

            foreach (var hits in rayHits)
            {
                hits.ToList().ForEach(p => InternalUtilities.AddPoint(p, "Penetration Points DistanceUntilBone", $"Testing::Screws::Screw {Index} of {screwBrandType}", Color.Gold));
            }
#endif

            return bonedist;
        }

        /// <summary>
        /// Gets the length of the available.
        /// </summary>
        /// <returns></returns>
        public override double GetAvailableLength()
        {
            // init

            // If a fixed length is set, get it, if not, calculate from head and tip
            var theLength = Math.Abs(_fixedLength) > 0.00001 ? _fixedLength : Math.Round((TipPoint - HeadPoint).Length);

            // get available screw lengths for the current screw type
            var availableLengths = GetScrewLengths(screwBrandType);

            // find the closest available length
            var nearestLength = availableLengths.OrderBy(x => Math.Abs((long)x - theLength)).First();

            // success
            return nearestLength;
        }

        private Mesh CreateLateralBumpTrimmed()
        {
            var objManager = new AmaceObjectManager(Director);
            var trimmer = objManager.GetBuildingBlock(IBB.WrapScrewBump).Geometry as Mesh;
            var plateTop = objManager.GetBuildingBlock(IBB.SolidPlateTop).Geometry as Mesh;

            // Mesh the lateral augmentation
            var untrimmed = LateralBump;

            // Compute boolean difference with trimming mesh This may yield redundant pieces: detect
            // ones that are touching/intersecting true plate top surface (cut out)
            var cutRems = Booleans.PerformBooleanSubtraction(untrimmed, trimmer);
            var trimmed = MeshUtilities.UnifyMeshParts(cutRems);

            var augForbool = MeshUtilities.DiscardNonColliding(trimmed, plateTop);
            return augForbool;
        }

        /// <summary>
        /// Makes the medial bump trimmed.
        /// </summary>
        /// <returns></returns>
        private Mesh CreateMedialBumpTrimmed()
        {
            var objManager = new AmaceObjectManager(Director);
            var trimmerWrap = (Mesh)objManager.GetBuildingBlock(IBB.WrapScrewBump).Geometry;
            var trimmerCup = Director.cup.innerReamingVolumeMesh;
            var integratedPlate = (Mesh)objManager.GetBuildingBlock(IBB.PlateFlat).Geometry;

            // Mesh the medial augmentation
            var untrimmed = MedialBump; // get

            var trimmedTemp = untrimmed;
            // If flange screw with medial augmentation, also trim with trimmerWrap mesh
            if (positioning == ScrewPosition.Flange)
            {
                trimmedTemp = Booleans.PerformBooleanIntersection(trimmedTemp, trimmerWrap);
                trimmedTemp = MeshUtilities.UnifyMeshParts(trimmedTemp);
            }

            var trimmed = trimmedTemp.DuplicateMesh();
            // Compute boolean intersection with trimmerCup mesh
            if (Intersection.MeshMeshFast(trimmedTemp, trimmerCup).Length > 0)
            {
                trimmed = Booleans.PerformBooleanSubtraction(trimmedTemp, trimmerCup);
            }
            trimmed = MeshUtilities.UnifyMeshParts(trimmed);

            // Discard augmentation pieces that are not colliding with the plate
            var augForBool = MeshUtilities.DiscardNonColliding(trimmed, integratedPlate);
            return augForBool;
        }

        /// <summary>
        /// Plates the outline.
        /// </summary>
        /// <param name="targetMesh">The target mesh.</param>
        /// <returns></returns>
        public PolylineCurve PlateOutline(Mesh targetMesh)
        {
            const double haloHeight = 15.0;
            var haloRadius = OutlineRadius + 0.1; // margin
            const double distThreshold = 100.0; // ray shooting distance
            const int numberRays = 50;
            List<Point3d[]> hits;
            List<int[]> hitFaceIds;
            List<double[]> hitDists;
            var snapPoints = new List<Point3d>();

            // Sample a circle above the screw head ("halo")
            var haloPlane = new Plane(this.HeadPoint - (haloHeight * Direction), Direction);
            var halo = new Circle(haloPlane, haloRadius);
            Point3d[] haloPoints;
            halo.ToNurbsCurve().DivideByCount(numberRays, true, out haloPoints);

            // Shoot rays
            targetMesh.IntersectWithRays(haloPoints, Direction, distThreshold, distThreshold, out hits, out hitDists, out hitFaceIds);

            // Head snap points are closest ray intersections
            for (var i = 0; i < hits.Count; i++)
            {
                var minDist = double.MaxValue;
                var minHit = Point3d.Unset;
                for (var j = 0; j < hits[i].Length; j++)
                {
                    if (!(hitDists[i][j] < minDist))
                    {
                        continue;
                    }

                    minDist = hitDists[i][j];
                    minHit = hits[i][j];
                }
                snapPoints.Add(minHit);
            }
            snapPoints.Add(snapPoints[0]); // close the curve
            var snapCurve = new PolylineCurve(snapPoints);
            snapCurve.PullToMesh(targetMesh, 0.1);

            return snapCurve;
        }

        /// <summary>
        /// Positionings the specified wrap sunk screw.
        /// </summary>
        /// <param name="wrapSunkScrew">The wrap sunk screw.</param>
        /// <param name="lateralCup">The lateral cup.</param>
        /// <returns></returns>
        private ScrewPosition Positioning(Mesh wrapSunkScrew, Mesh lateralCup)
        {
            // Parameter
            const double tolerance = 0.25; // threshold for distance comparison
            // Do ray intersection
            var initialHeadPoint = HeadPoint - AxialOffset * Direction;
            var ray = new Ray3d(initialHeadPoint, -Direction);
            var intersection1 = Intersection.MeshRay(wrapSunkScrew, ray);
            if (intersection1 < 0)
            {
                return ScrewPosition.Flange;
            }
            var intersection2 = Intersection.MeshRay(lateralCup, ray);

            if (intersection2 < 0)
            {
                return ScrewPosition.Flange;
            }

            // Compare intersection distances
            if (screwAlignment == ScrewAlignment.Sunk && Math.Abs(intersection1 - intersection2) < tolerance)
            {
                return ScrewPosition.Cup;
            }

            return ScrewPosition.Flange;
        }


        /// <summary>
        /// Calibrates the head and tip globally.
        /// </summary>
        /// <returns></returns>
        public bool CalibrateHeadAndTipGlobally()
        {
            // Get target mesh for intersection
            var targetMesh = GetCalibrationTargetEntity(false);

            // Set up rays for intersection
            var origins = new List<Point3d>();
            var rays = new List<Vector3d>();

            // ray from screw head to screw tip
            origins.Add(HeadCalibrationPoint);
            rays.Add(Direction);
            // ray from screw head away from screw tip
            origins.Add(HeadCalibrationPoint);
            rays.Add(-Direction);

            // Perform ray intersection
            List<int> faceIds;
            List<double> hitDistances;
            targetMesh.IntersectWithRaysOnlyFirst(origins, rays, out hitDistances, out faceIds);
            var intersectionTowardsTip = !double.IsNaN(hitDistances[0]);
            var intersectionAwayFromTip = !double.IsNaN(hitDistances[1]);

            // Move screw head depending on intersection result
            var calibrated = true;
            if (!intersectionTowardsTip && !intersectionAwayFromTip)
            {
                calibrated = false;
            }
            else if (intersectionTowardsTip && !intersectionAwayFromTip)
            {
                MoveScrewHeadDown(hitDistances[0]);
            }
            else if (!intersectionTowardsTip)
            {
                MoveScrewHeadUp(hitDistances[1]);
            }
            else
            {
                // Calculate mesh normals
                targetMesh.FaceNormals.ComputeFaceNormals();
                var theNormals = targetMesh.FaceNormals;

                // Check if the direction of the intersection is opposite to the direction of the screw
                // One
                var normal1 = theNormals.ElementAt(faceIds[0]);
                var intersection1OppositeDirection = normal1 * Direction < 0;
                // Two
                var normal2 = theNormals.ElementAt(faceIds[1]);
                var intersection2OppositeDirection = normal2 * Direction < 0;

                if (intersection1OppositeDirection)
                {
                    MoveScrewHeadDown(hitDistances[0]);
                }
                else if (intersection2OppositeDirection)
                {
                    MoveScrewHeadUp(hitDistances[1]);
                }
                else
                {
                    calibrated = false;
                }
            }

            return calibrated;
        }

        private void MoveScrewHeadDown(double distance)
        {
            // move screw head down
            var oldAxis = Direction;
            HeadPoint = HeadPoint + distance * oldAxis;
            // check if screw tip did not flip
            if (oldAxis * Direction < 0)
            {
                TipPoint = TipPoint + distance * oldAxis;
            }
        }

        private void MoveScrewHeadUp(double distance)
        {
            HeadPoint = HeadPoint + distance * -Direction;
        }

        /// <summary>
        /// Serialize member variables to user dictionary.
        /// </summary>
        public new void PrepareForArchiving()
        {
            var userDict = Attributes.UserDictionary;
            userDict.SetEnumValue<IBB>(ImplantBuildingBlockProperties.KeyBlockType, IBB.Screw);
            userDict.SetEnumValue<ScrewAlignment>(KeyScrewAlign, screwAlignment);
            userDict.Set(KeyHeadPoint, HeadPoint);
            userDict.Set(KeyTipPoint, TipPoint);
            userDict.Set(KeyAxialOffset, AxialOffset);
            userDict.Set(KeyIndex, Index);
            userDict.Set(KeyFixedLength, FixedLength);
            userDict.SetEnumValue<QualityCheckResult>(KeyQcCupZone, _qualityCupZone);

            //remove all ScrewAideType from userDict
            foreach (var key in Enum.GetValues(typeof(ScrewAideType)))
            {
                userDict.Remove(key.ToString());
            }

            if (ScrewAides != null)
            {
                foreach (var key in ScrewAides.Keys)
                {
                    userDict.Set(key.ToString(), ScrewAides[key]);
                }
            }

            userDict.Set(KeyScrewBrandType, ScrewBrandTypeConverter.ConvertToArchivableDictionary(screwBrandType));
            CommitChanges();
        }

        /// <summary>
        /// Sets the specified old screw identifier.
        /// </summary>
        /// <param name="oldScrewId">The old screw identifier.</param>
        /// <param name="recalibrate">if set to <c>true</c> [recalibrate].</param>
        /// <param name="update">if set to <c>true</c> [update].</param>
        /// <returns></returns>
        public override void Set(Guid inputOldScrewId, bool recalibrate = true, bool update = true)
        {
            var oldScrewId = inputOldScrewId;

            // Add to document if it is a new screw
            if (oldScrewId == Guid.Empty)
            {
                var objManager = new AmaceObjectManager(Director);
                oldScrewId = objManager.SetBuildingBlock(IBB.Screw, this, oldScrewId);
            }
            // Calibrate screw
            if (recalibrate)
            {
                CalibrateScrewHead();
            }

            // Update geometry
            if (update)
            {
                Update(oldScrewId);
            }

        }

        /// <summary>
        /// This call informs an object it is about to be added to the list of
        /// active objects in the document.
        /// </summary>
        /// <param name="doc"></param>
        protected override void OnAddToDocument(RhinoDoc doc)
        {
            base.OnAddToDocument(doc);

            // Disable undo recording so that Ctrl-Z does not restore the screw aides Screw aide
            // creation is controlled by OnAddToDocument (which is also triggered when Ctrl-Z is pressed)
            if (Director != null)
            {
                Director.Document.UndoRecordingEnabled = false;
            }

            // Create screw aides
            if (Director != null)
            {
                CreateAides();
            }

            // Restart recording actions for Ctrl-Z
            if (Director != null)
            {
                Director.Document.UndoRecordingEnabled = true;
            }

            // Update all visualisations
            UpdateGUI(doc);
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

            // Disable undo recording so that Ctrl-Z does not restore the screw aides Screw aide
            // creation is controlled by OnAddToDocument (which is also triggered when Ctrl-Z is pressed)

            if (Director != null)
            {
                Director.Document.UndoRecordingEnabled = false;
            }

            // Delete dependencies of screw (screw aides)
            var dependency = new Dependencies();
            dependency.DeleteScrewDependencies(this);

            // Restart recording actions for Ctrl-Z
            if (Director != null)
            {
                Director.Document.UndoRecordingEnabled = true;
            }

            // Update all visualisations
            UpdateGUI(doc);
        }

        /// <summary>
        /// Called when this a new instance of this object is created and copied from
        /// an existing object
        /// </summary>
        /// <param name="source"></param>
        protected override void OnDuplicate(RhinoObject source)
        {
            base.OnDuplicate(source);
            
            var other = source as Screw;

            if (other == null)
            {
                return;
            }

            Director = other.Director;
            Index = other.Index;
            HeadPoint = other.HeadPoint;
            TipPoint = other.TipPoint;
            screwBrandType = other.screwBrandType;
            _axialOffset = other._axialOffset;
            screwAlignment = other.screwAlignment;
            _fixedLength = other._fixedLength;
        }

        /// <summary>
        /// Gets the screw head contour.
        /// </summary>
        /// <param name="screwDatabase">The screw database.</param>
        /// <param name="screwType">Type of the screw.</param>
        /// <param name="screwBrandType"></param>
        /// <returns></returns>
        private static Curve GetScrewHeadContour(File3dm screwDatabase, ScrewBrandType screwBrandType)
        {
            return ScrewAideManager.GetHeadCurve(screwDatabase, screwBrandType);
        }

        /// <summary>
        /// Calculates the index of the screw (1-based)
        /// \todo This should be handled by the screw manager
        /// </summary>
        /// <param name="givenIndex">Index of the given.</param>
        private void CalculateScrewIndex(int givenIndex)
        {
            // Index was given
            if (givenIndex != 0)
            {
                Index = givenIndex;
            }
            // Index needs to be calculated
            else
            {
                var screwManager = new ScrewManager(Director.Document);
                var screws = screwManager.GetAllScrews();
                var indMax = 0;
                foreach (var screw in screws)
                {
                    if (screw.Index > indMax)
                    {
                        indMax = screw.Index;
                    }
                }
                Index = indMax + 1;
            }
        }

        /// <summary>
        /// Heads the height.
        /// </summary>
        /// <param name="screwDatabase">The screw database.</param>
        /// <returns></returns>
        private double HeadHeight(File3dm screwDatabase)
        {
            var headContour = GetScrewHeadContour(screwDatabase, screwBrandType);
            return Math.Abs(headContour.PointAtStart.Z - headContour.PointAtEnd.Z);
        }

        /// <summary>
        /// Updates the specified old identifier.
        /// </summary>
        /// <param name="oldId">The old identifier.</param>
        /// <returns></returns>
        protected override bool Update(Guid oldId)
        {
            // Get director
            var doc = Director.Document;
            var objManager = new AmaceObjectManager(Director);

            // if fixed length is unset, the screw needs to be set to the default tip position
            if (Math.Abs(_fixedLength) < 0.00001 && Math.Abs(_axialOffset) < 0.00001)
            {
                var reamedPelvis = objManager.GetBuildingBlock(IBB.OriginalReamedPelvis).Geometry as Mesh;
                double dist;
                var screwAxis = ScrewVector;
                screwAxis.Unitize();
                var success = CalculateDefaultScrewLength(screwBrandType, reamedPelvis, HeadPoint, screwAxis, out dist);
                if (success)
                {
                    // set tip point corresponding to default length
                    TipPoint = HeadPoint + screwAxis * dist;
                }
                else
                {
                    // default length definition is not possible, use a screw of 10.0 mm and set it fixed
                    TipPoint = HeadPoint + screwAxis * 10.0;
                    _fixedLength = 10.0;
                }
            }

            // Update length to an available one
            SetAvailableLength();

            // Create a new screw to update the geometry
            var newScrew = new Screw(Director, HeadPoint, TipPoint, screwBrandType, screwAlignment, 0,
                _axialOffset)
            {
                Index = Index,
                _fixedLength = _fixedLength
            };
            // Keep screw index
            // Keep fixed length (if any)

            // Update geometry in the document (also creates the screw aides in OnAddToDocument)
            var id = objManager.SetBuildingBlock(IBB.Screw, newScrew, oldId);
            if (id == Guid.Empty)
            {
                return false;
            }

            // Write the properties of the new screw back to the current entity
            Attributes = newScrew.Attributes;
            ScrewAides = newScrew.ScrewAides;

            // Update quality checks
            newScrew.UpdateCheckCupZone();
            _qualityCupZone = newScrew._qualityCupZone;

            // Update all visualisations
            UpdateGUI(doc);

            // success
            return true;
        }

        /// <summary>
        /// Updates the check cup zone.
        /// </summary>
        private void UpdateCheckCupZone()
        {
            // A sunk cup screw with no axial offset is always ok
            if (positioning == ScrewPosition.Cup && screwAlignment == ScrewAlignment.Sunk && AxialOffset <= 0)
            {
                _qualityCupZone = QualityCheckResult.OK; // should always be OK if calibration works correctly
                return;
            }

            var reamingVolume = Director.cup.innerReamingVolume;

            const double tolerance = 1e-6;
            Curve[] intersectionCurves;
            Point3d[] intersectionPoints;
            var success = Intersection.BrepBrep(BrepGeometry, reamingVolume, tolerance, out intersectionCurves, out intersectionPoints);

            if (!success)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Could not update cup zone check for screw {0}", Index);
            }

            // Set private variable
            if (intersectionCurves.Count() != 0 || intersectionPoints.Count() != 0)
            {
                _qualityCupZone = QualityCheckResult.NotOK;
            }
            else
            {
                _qualityCupZone = QualityCheckResult.OK;
            }
        }

        /// <summary>
        /// Updates the GUI.
        /// \todo this does not belong in the screw class!
        /// </summary>
        /// <param name="doc">The document.</param>
        private static void UpdateGUI(RhinoDoc doc)
        {
            // Update QC conduit
            if (Proxies.ScrewInfo.Numbers != null)
            {
                Proxies.ScrewInfo.Update(doc, false);
            }

            // Refresh panel
            var screwPanel = ScrewPanel.GetPanel();
            screwPanel?.RefreshPanelInfo();
        }

        private Mesh GenerateHeadMeshFromContour(Curve contour)
        {
            // Screw parameters
            if (contour.PointAtStart.X > contour.PointAtEnd.X)
            {
                contour.Reverse();
            }

            var orientation = TipPoint - HeadPoint;
            orientation.Unitize();

            // Create screw Brep from contour and revolve
            var screwAxis = -Vector3d.ZAxis;
            var rotationAxis = new Line(Point3d.Origin, (Point3d)screwAxis);
            var bodyStart = contour.PointAtEnd;
            var bodyOrigin = bodyStart;
            bodyOrigin.X = 0.0; // highest center point of screw body

            // Create revolution surface (closed surface = solid)
            var revSurf = RevSurface.Create(contour, rotationAxis);
            var screwHead = Brep.CreateFromRevSurface(revSurf, true, true);

            // Transform to align with screw
            screwHead.Transform(ScrewAideManager.GetAlignmentTransform(orientation, HeadPoint, screwBrandType));

            // Create Mesh
            var meshparameters = MeshParameters.IDS();
            var meshParts = Mesh.CreateFromBrep(screwHead, meshparameters);
            var screwHeadMesh = new Mesh();
            foreach (var part in meshParts)
            {
                screwHeadMesh.Append(part);
            }
           
            return screwHeadMesh;
        }

        public override string GenerateNameForMimics()
        {
            return $"{Director.Inspector.CaseId}_{screwBrandType}_{screwAlignment}_{Index:D}";
        }

        protected override double GetDiameter()
        { 
            return GetDiameter(screwBrandType);
        }

        //OnDeleteFromDocument is called when the screw is deleting but still persist in the document.
        //This event is called when deletion is complete and no longer in the document.
        public static void OnDeleteFromDocumentComplete(RhinoDoc doc)
        {
            var mgr = new ScrewManager(doc);
            if (mgr.GetAllScrews().Any())
            {
                UpdateGUI(doc);
            }
        }
    }
}