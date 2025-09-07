using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Collections;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Amace
{
    /// <summary>
    /// PreOpInspector provides access to the data provided by the Pre-operative analysis step.
    /// This includes, anatomical planes that define the pelvic coordinate system and the
    /// centers of rotation(COR) for the hip.
    /// WARNING, ImportPreOpData.py WOULD ACCESS THE ATTRIBUTES HERE BY NAME! IF NAME HERE AND IN PYTHON SCRIPT IS NOT THE SAME,
    /// IMPORT PREOP WILL FAIL!
    /// </summary>
    /// <seealso cref="Rhino.Collections.ArchivableDictionary" />
    public class PreOpInspector : ArchivableDictionary
    {
        /// <summary>
        /// The key axial plane
        /// </summary>
        private const string KeyAxialPlane = "axial_plane";

        /// <summary>
        /// The key axial plane identifier
        /// </summary>
        private const string KeyAxialPlaneId = "axial_plane_id";

        /// <summary>
        /// The key bone quality
        /// </summary>
        private const string KeyBoneQuality = "DEF_BONE_QUALITY";

        /// <summary>
        /// The key case identifier
        /// </summary>
        private const string KeyCaseId = "CASE_ID";

        /// <summary>
        /// The key collidables
        /// </summary>
        private const string KeyCollidables = "collidable_components";

        /// <summary>
        /// The key contralateral cup rim
        /// </summary>
        private const string KeyContralateralCupRim = "clat_cup_rim";

        /// <summary>
        /// The key contralateral dislocated
        /// </summary>
        private const string KeyContralateralDislocated = "clat_dislocated";

        /// <summary>
        /// The key contralateral femur
        /// </summary>
        private const string KeyContralateralFemur = "clat_femur";

        /// <summary>
        /// The key contralateral femur center of rotation
        /// </summary>
        private const string KeyContralateralFemurCenterOfRotation = "clat_femur_cor";

        /// <summary>
        /// The key contralateral femur center of rotation ahjc
        /// </summary>
        public const string KeyContralateralFemurCenterOfRotationAhjc = "clat_femur_cor_ahjc";

        /// <summary>
        /// The key contralateral mesh
        /// </summary>
        private const string KeyContralateralMesh = "clat_mesh";

        /// <summary>
        /// The key contralateral pelvis center of rotation
        /// </summary>
        private const string KeyContralateralPelvisCenterOfRotation = "clat_pelvis_cor";

        /// <summary>
        /// The key contralateral pelvis center of rotation ahjc
        /// </summary>
        private const string KeyContralateralPelvisCenterOfRotationAhjc = "clat_pelvis_cor_ahjc";

        /// <summary>
        /// The key contralateral side
        /// </summary>
        private const string KeyContralateralSide = "clat_side";

        /// <summary>
        /// The key contralateral SSM center of rotation
        /// </summary>
        private const string KeyContralateralSsmCenterOfRotation = "clat_ssm_cor";

        /// <summary>
        /// The key contralateral SSM radius
        /// </summary>
        private const string KeyContralateralSsmRadius = "clat_ssm_radius";

        /// <summary>
        /// The key contralateral SSM rim
        /// </summary>
        private const string KeyContralateralSsmRim = "clat_ssm_rim";

        /// <summary>
        /// The key coronal plane
        /// </summary>
        private const string KeyCoronalPlane = "frontal_plane";

        /// <summary>
        /// The key coronal plane identifier
        /// </summary>
        private const string KeyCoronalPlaneId = "frontal_plane_id";

        /// <summary>
        /// The key cortex thickness
        /// </summary>
        private const string KeyCortexThickness = "DEF_CORTEX_THICK";

        /// <summary>
        /// The key cortex thickness maxiumum
        /// </summary>
        private const string KeyCortexThicknessMaxiumum = "DEF_CORTEX_THICK_MAX";

        /// <summary>
        /// The key cortex thickness minimum
        /// </summary>
        private const string KeyCortexThicknessMinimum = "DEF_CORTEX_THICK_MIN";

        /// <summary>
        /// The key cortex thickness threshold
        /// </summary>
        private const string KeyCortexThicknessThreshold = "DEF_CORTEX_THICK_THRESH";

        /// <summary>
        /// The key defect cup rim
        /// </summary>
        private const string KeyDefectCupRim = "defect_cup_rim";

        /// <summary>
        /// The key defect dislocated
        /// </summary>
        private const string KeyDefectDislocated = "defect_dislocated";

        /// <summary>
        /// The key defect femur
        /// </summary>
        private const string KeyDefectFemur = "defect_femur";

        /// <summary>
        /// The key defect femur center of rotation
        /// </summary>
        private const string KeyDefectFemurCenterOfRotation = "defect_femur_cor";

        /// <summary>
        /// The key defect mesh
        /// </summary>
        private const string KeyDefectMesh = "defect_mesh";

        /// <summary>
        /// The key defect pelvis center of rotation
        /// </summary>
        private const string KeyDefectPelvisCenterOfRotation = "defect_pelvis_cor";

        /// <summary>
        /// The key defect side
        /// </summary>
        private const string KeyDefectSide = "defect_side";

        /// <summary>
        /// The key defect SSM center of rotation
        /// </summary>
        private const string KeyDefectSsmCenterOfRotation = "ssm_cor";

        /// <summary>
        /// The key defect SSM radius
        /// </summary>
        private const string KeyDefectSsmRadius = "ssm_radius";

        /// <summary>
        /// The key defect SSM rim
        /// </summary>
        private const string KeyDefectSsmRim = "ssm_rim";

        /// <summary>
        /// The key pre operative identifier
        /// </summary>
        private const string KeyPreOperativeId = "PREOP_ID";

        /// <summary>
        /// The key sacrum
        /// </summary>
        private const string KeySacrum = "sacrum";

        /// <summary>
        /// The key sagittal plane
        /// </summary>
        private const string KeySagittalPlane = "mid_plane";

        /// <summary>
        /// The key sagittal plane identifier
        /// </summary>
        private const string KeySagittalPlaneId = "mid_plane_id";

        /// <summary>
        /// The key wall thickness
        /// </summary>
        private const string KeyWallThickness = "DEF_WALL_THICK";

        /// <summary>
        /// The key wall thickness maximum
        /// </summary>
        private const string KeyWallThicknessMaximum = "DEF_WALL_THICK_MAX";

        /// <summary>
        /// The key wall thickness minimum
        /// </summary>
        private const string KeyWallThicknessMinimum = "DEF_WALL_THICK_MIN";

        /// <summary>
        /// The pelvic coordinate system plane size
        /// </summary>
        private const double PelvicCoordinateSystemPlaneSize = 214.0;

        /// <summary>
        /// The document
        /// </summary>
        private readonly RhinoDoc _document;

        /// <summary>
        /// The pelvic coordinate system axial index
        /// </summary>
        private int _pelvicCoordinateSystemAxialIndex = -1;

        /// <summary>
        /// The pelvic coordinate system coronal index
        /// </summary>
        private int _pelvicCoordinateSystemCoronalIndex = -1;

        /// <summary>
        /// The pelvic coordinate system sagittal index
        /// </summary>
        private int _pelvicCoordinateSystemSagittalIndex = -1;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreOpInspector"/> class.
        /// </summary>
        /// <param name="doc">The document.</param>
        public PreOpInspector(RhinoDoc doc)
        {
            _document = doc;
            Name = "PreOpInspector";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreOpInspector"/> class.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="archive">The archive.</param>
        public PreOpInspector(RhinoDoc doc, Rhino.FileIO.BinaryArchiveReader archive) : this(doc)
        {
            var preopDict = archive.ReadDictionary();
            AddContentsFrom(preopDict);

            // Notify everyone that pre-op data is loaded
            PreOpDataLoaded?.Invoke(this, new PreOpDataLoadedArgs(this));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PreOpInspector"/> class.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="pickleDict">The pickle dictionary.</param>
        public PreOpInspector(RhinoDoc doc, dynamic pickleDict) : this(doc)
        {
            var dict = pickleDict as IDictionary<object, object>;
            AddContentsFrom(dict);

            // Notify everyone that pre-op data is loaded
            PreOpDataLoaded?.Invoke(this, new PreOpDataLoadedArgs(this));
        }

        /// <summary>
        /// Event fired when pre-op data is loaded, and a new PreOpInspector object is created.
        /// </summary>
        public static event EventHandler<PreOpDataLoadedArgs> PreOpDataLoaded;

        /// <summary>
        /// Gets or sets the axial plane.
        /// </summary>
        /// <value>
        /// The axial plane.
        /// </value>
        public Plane AxialPlane
        {
            get
            {
                return GetPlane(KeyAxialPlane, Plane.Unset);
            }
            set
            {
                Set(KeyAxialPlane, value);
                _pelvicCoordinateSystemAxialIndex = _document.NamedConstructionPlanes.Add("PCS Axial", value);
            }
        }

        /// <summary>
        /// Gets or sets the axial plane identifier.
        /// </summary>
        /// <value>
        /// The axial plane identifier.
        /// </value>
        private Guid AxialPlaneId
        {
            get
            {
                return GetGuid(KeyAxialPlaneId, Guid.Empty);
            }
            set
            {
                Set(KeyAxialPlaneId, value);
            }
        }

        /// <summary>
        /// Gets or sets the case identifier.
        /// </summary>
        /// <value>
        /// The case identifier.
        /// </value>
        public string CaseId
        {
            get
            {
                return GetString(KeyCaseId, "Unset");
            }
            set
            {
                Set(KeyCaseId, value);
            }
        }

        /// <summary>
        /// Gets the collidable components.
        /// </summary>
        /// <value>
        /// The collidable components.
        /// </value>
        private List<Guid> CollidableComponents
        {
            get
            {
                object comps;
                var rc = TryGetValue(KeyCollidables, out comps);
                if (!rc)
                {
                    return new List<Guid>(); // empty list
                }
                var collidables = comps as IEnumerable<Guid>;

                return collidables?.ToList() ?? new List<Guid>();
            }
        }

        /// <summary>
        /// Gets or sets the contralateral cup rim.
        /// </summary>
        /// <value>
        /// The contralateral cup rim.
        /// </value>
        public Plane ContralateralCupRim
        {
            get
            {
                return GetPlane(KeyContralateralCupRim, Plane.Unset);
            }
            set
            {
                Set(KeyContralateralCupRim, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [contralateral dislocated].
        /// </summary>
        /// <value>
        /// <c>true</c> if [contralateral dislocated]; otherwise, <c>false</c>.
        /// </value>
        public bool ContralateralDislocated
        {
            get
            {
                return GetBool(KeyContralateralDislocated, false);
            }
            set
            {
                Set(KeyContralateralDislocated, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral femur.
        /// </summary>
        /// <value>
        /// The contralateral femur.
        /// </value>
        public Mesh ContralateralFemur
        {
            get
            {
                if (Guid.Empty == ContralateralFemurId)
                {
                    return null;
                }

                return _document.Objects.Find(ContralateralFemurId).Geometry as Mesh;
            }
            set
            {
                var director = IDSPluginHelper.GetDirector(_document.DocumentId);
                var objectManager = new ObjectManager(director);
                ContralateralFemurId = objectManager.AddNewBuildingBlock(BuildingBlocks.Blocks[IBB.ContralateralFemur], value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral femur center of rotation.
        /// </summary>
        /// <value>
        /// The contralateral femur center of rotation.
        /// </value>
        public Point3d ContralateralFemurCenterOfRotation
        {
            get
            {
                return GetPoint3d(KeyContralateralFemurCenterOfRotation, Point3d.Unset);
            }
            set
            {
                Set(KeyContralateralFemurCenterOfRotation, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral femur center of rotation ahjc.
        /// </summary>
        /// <value>
        /// The contralateral femur center of rotation ahjc.
        /// </value>
        public Point3d ContralateralFemurCenterOfRotationAhjc
        {
            get
            {
                return GetPoint3d(KeyContralateralFemurCenterOfRotationAhjc, Point3d.Unset);
            }
            set
            {
                Set(KeyContralateralFemurCenterOfRotationAhjc, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral femur identifier.
        /// </summary>
        /// <value>
        /// The contralateral femur identifier.
        /// </value>
        public Guid ContralateralFemurId
        {
            get
            {
                return GetGuid(KeyContralateralFemur, Guid.Empty);
            }
            private set
            {
                Set(KeyContralateralFemur, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral mesh.
        /// </summary>
        /// <value>
        /// The contralateral mesh.
        /// </value>
        public Mesh ContralateralMesh
        {
            get
            {
                if (Guid.Empty == ContralateralMeshId)
                {
                    return null;
                }
                return _document.Objects.Find(ContralateralMeshId).Geometry as Mesh;
            }
            set
            {
                var director = IDSPluginHelper.GetDirector(this._document.DocumentId);
                var objectManager = new ObjectManager(director);
                ContralateralMeshId = objectManager.AddNewBuildingBlock(BuildingBlocks.Blocks[IBB.ContralateralPelvis], value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral mesh identifier.
        /// </summary>
        /// <value>
        /// The contralateral mesh identifier.
        /// </value>
        public Guid ContralateralMeshId
        {
            get
            {
                return GetGuid(KeyContralateralMesh, Guid.Empty);
            }
            private set
            {
                Set(KeyContralateralMesh, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral pelvis center of rotation.
        /// </summary>
        /// <value>
        /// The contralateral pelvis center of rotation.
        /// </value>
        public Point3d ContralateralPelvisCenterOfRotation
        {
            get
            {
                return GetPoint3d(KeyContralateralPelvisCenterOfRotation, Point3d.Unset);
            }
            set
            {
                Set(KeyContralateralPelvisCenterOfRotation, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral pelvis center of rotation ahjc.
        /// </summary>
        /// <value>
        /// The contralateral pelvis center of rotation ahjc.
        /// </value>
        public Point3d ContralateralPelvisCenterOfRotationAhjc
        {
            get
            {
                return GetPoint3d(KeyContralateralPelvisCenterOfRotationAhjc, Point3d.Unset);
            }
            set
            {
                Set(KeyContralateralPelvisCenterOfRotationAhjc, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral side.
        /// </summary>
        /// <value>
        /// The contralateral side.
        /// </value>
        public string ContralateralSide
        {
            get
            {
                return GetString(KeyContralateralSide, null);
            }
            set
            {
                Set(KeyContralateralSide, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral SSM center of rotation.
        /// </summary>
        /// <value>
        /// The contralateral SSM center of rotation.
        /// </value>
        public Point3d ContralateralSsmCenterOfRotation
        {
            get
            {
                return GetPoint3d(KeyContralateralSsmCenterOfRotation, Point3d.Unset);
            }
            set
            {
                Set(KeyContralateralSsmCenterOfRotation, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral SSM radius.
        /// </summary>
        /// <value>
        /// The contralateral SSM radius.
        /// </value>
        public double ContralateralSsmRadius
        {
            get
            {
                return GetDouble(KeyContralateralSsmRadius, -1.0);
            }
            set
            {
                Set(KeyContralateralSsmRadius, value);
            }
        }

        /// <summary>
        /// Gets or sets the contralateral SSM rim.
        /// </summary>
        /// <value>
        /// The contralateral SSM rim.
        /// </value>
        public Plane ContralateralSsmRim
        {
            get
            {
                return GetPlane(KeyContralateralSsmRim, Plane.Unset);
            }
            set
            {
                Set(KeyContralateralSsmRim, value);
            }
        }

        /// <summary>
        /// Gets or sets the coronal plane.
        /// </summary>
        /// <value>
        /// The coronal plane.
        /// </value>
        public Plane CoronalPlane
        {
            get
            {
                return GetPlane(KeyCoronalPlane, Plane.Unset);
            }
            set
            {
                Set(KeyCoronalPlane, value);
                _pelvicCoordinateSystemCoronalIndex = _document.NamedConstructionPlanes.Add("PCS Front", value);
            }
        }

        /// <summary>
        /// Gets or sets the coronal plane identifier.
        /// </summary>
        /// <value>
        /// The coronal plane identifier.
        /// </value>
        private Guid CoronalPlaneId
        {
            get
            {
                return GetGuid(KeyCoronalPlaneId, Guid.Empty);
            }
            set
            {
                Set(KeyCoronalPlaneId, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect bone quality.
        /// </summary>
        /// <value>
        /// The defect bone quality.
        /// </value>
        public IEnumerable<double> DefectBoneQuality
        {
            get
            {
                object qualMeasure;
                var rc = TryGetValue(KeyBoneQuality, out qualMeasure);
                if (rc)
                {
                    return qualMeasure as IEnumerable<double>;
                }

                return null;
            }
            set { Set(KeyBoneQuality, value); }
        }

        /// <summary>
        /// Gets or sets the defect cortex thickness.
        /// </summary>
        /// <value>
        /// The defect cortex thickness.
        /// </value>
        public IEnumerable<double> DefectCortexThickness
        {
            get
            {
                object qualMeasure;
                var rc = TryGetValue(KeyCortexThickness, out qualMeasure);
                if (rc)
                {
                    return qualMeasure as IEnumerable<double>;
                }
                return null;
            }
            set { Set(KeyCortexThickness, value); }
        }

        /// <summary>
        /// Gets or sets the defect cortex thickness maximum.
        /// </summary>
        /// <value>
        /// The defect cortex thickness maximum.
        /// </value>
        public double DefectCortexThicknessMaximum
        {
            get
            {
                return GetDouble(KeyCortexThicknessMaxiumum, -1.0);
            }
            set
            {
                Set(KeyCortexThicknessMaxiumum, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect cortex thickness minimum.
        /// </summary>
        /// <value>
        /// The defect cortex thickness minimum.
        /// </value>
        public double DefectCortexThicknessMinimum
        {
            get
            {
                return GetDouble(KeyCortexThicknessMinimum, -1.0);
            }
            set
            {
                Set(KeyCortexThicknessMinimum, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect cortex thickness threshold.
        /// </summary>
        /// <value>
        /// The defect cortex thickness threshold.
        /// </value>
        public double DefectCortexThicknessThreshold
        {
            get
            {
                return GetDouble(KeyCortexThicknessThreshold, -1.0);
            }
            set
            {
                Set(KeyCortexThicknessThreshold, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect cup rim.
        /// </summary>
        /// <value>
        /// The defect cup rim.
        /// </value>
        public Plane DefectCupRim
        {
            get
            {
                return GetPlane(KeyDefectCupRim, Plane.Unset);
            }
            set
            {
                Set(KeyDefectCupRim, value);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [defect dislocated].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [defect dislocated]; otherwise, <c>false</c>.
        /// </value>
        public bool DefectDislocated
        {
            get
            {
                return GetBool(KeyDefectDislocated, false);
            }
            set
            {
                Set(KeyDefectDislocated, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect femur.
        /// </summary>
        /// <value>
        /// The defect femur.
        /// </value>
        public Mesh DefectFemur
        {
            get
            {
                if (Guid.Empty == DefectFemurId)
                {
                    return null;
                }

                return _document.Objects.Find(DefectFemurId).Geometry as Mesh;
            }
            set
            {
                var director = IDSPluginHelper.GetDirector(this._document.DocumentId);
                var objectManager = new ObjectManager(director);
                DefectFemurId = objectManager.AddNewBuildingBlock(BuildingBlocks.Blocks[IBB.DefectFemur], value);
            }
        }

        /// <summary>
        /// Gets or sets the defect femur center of rotation.
        /// </summary>
        /// <value>
        /// The defect femur center of rotation.
        /// </value>
        public Point3d DefectFemurCenterOfRotation
        {
            get
            {
                return GetPoint3d(KeyDefectFemurCenterOfRotation, Point3d.Unset);
            }
            set
            {
                Set(KeyDefectFemurCenterOfRotation, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect femur identifier.
        /// </summary>
        /// <value>
        /// The defect femur identifier.
        /// </value>
        public Guid DefectFemurId
        {
            get
            {
                return GetGuid(KeyDefectFemur, Guid.Empty);
            }
            private set
            {
                Set(KeyDefectFemur, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect mesh.
        /// </summary>
        /// <value>
        /// The defect mesh.
        /// </value>
        public Mesh DefectMesh
        {
            get
            {
                if (Guid.Empty == DefectMeshId)
                {
                    return null;
                }

                return _document.Objects.Find(DefectMeshId).Geometry as Mesh;
            }
            set
            {
                var director = IDSPluginHelper.GetDirector(this._document.DocumentId);
                var objectManager = new ObjectManager(director);
                DefectMeshId = objectManager.AddNewBuildingBlock(BuildingBlocks.Blocks[IBB.DefectPelvis], value);
            }
        }

        /// <summary>
        /// Gets or sets the defect mesh identifier.
        /// </summary>
        /// <value>
        /// The defect mesh identifier.
        /// </value>
        private Guid DefectMeshId
        {
            get
            {
                return GetGuid(KeyDefectMesh, Guid.Empty);
            }
            set
            {
                Set(KeyDefectMesh, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect pelvis center of rotation.
        /// </summary>
        /// <value>
        /// The defect pelvis center of rotation.
        /// </value>
        public Point3d DefectPelvisCenterOfRotation
        {
            get
            {
                return GetPoint3d(KeyDefectPelvisCenterOfRotation, Point3d.Unset);
            }
            set
            {
                Set(KeyDefectPelvisCenterOfRotation, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect side.
        /// </summary>
        /// <value>
        /// The defect side.
        /// </value>
        public string DefectSide
        {
            get
            {
                return GetString(KeyDefectSide, null);
            }
            set
            {
                Set(KeyDefectSide, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect SSM center of rotation.
        /// </summary>
        /// <value>
        /// The defect SSM center of rotation.
        /// </value>
        public Point3d DefectSsmCenterOfRotation
        {
            get
            {
                return GetPoint3d(KeyDefectSsmCenterOfRotation, Point3d.Unset);
            }
            set
            {
                Set(KeyDefectSsmCenterOfRotation, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect SSM radius.
        /// </summary>
        /// <value>
        /// The defect SSM radius.
        /// </value>
        public double DefectSsmRadius
        {
            get
            {
                return GetDouble(KeyDefectSsmRadius, -1.0);
            }
            set
            {
                Set(KeyDefectSsmRadius, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect SSM rim.
        /// </summary>
        /// <value>
        /// The defect SSM rim.
        /// </value>
        public Plane DefectSsmRim
        {
            get
            {
                return GetPlane(KeyDefectSsmRim, Plane.Unset);
            }
            set
            {
                Set(KeyDefectSsmRim, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect wall thickness.
        /// </summary>
        /// <value>
        /// The defect wall thickness.
        /// </value>
        public IEnumerable<double> DefectWallThickness
        {
            get
            {
                object qualMeasure;
                var rc = TryGetValue(KeyWallThickness, out qualMeasure);
                if (rc)
                {
                    return qualMeasure as IEnumerable<double>;
                }

                return null;
            }
            set { Set(KeyWallThickness, value); }
        }

        /// <summary>
        /// Gets or sets the defect wall thickness maximum.
        /// </summary>
        /// <value>
        /// The defect wall thickness maximum.
        /// </value>
        public double DefectWallThicknessMaximum
        {
            get
            {
                return GetDouble(KeyWallThicknessMaximum, -1.0);
            }
            set
            {
                Set(KeyWallThicknessMaximum, value);
            }
        }

        /// <summary>
        /// Gets or sets the defect wall thickness minimum.
        /// </summary>
        /// <value>
        /// The defect wall thickness minimum.
        /// </value>
        public double DefectWallThicknessMinimum
        {
            get
            {
                return GetDouble(KeyWallThicknessMinimum, -1.0);
            }
            set
            {
                Set(KeyWallThicknessMinimum, value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is defect left.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is defect left; otherwise, <c>false</c>.
        /// </value>
        private bool IsDefectLeft => null != DefectSide && DefectSide.Equals("left", StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Gets the pelvic coordinate system.
        /// </summary>
        /// <value>
        /// The pelvic coordinate system.
        /// </value>
        public Plane pelvicCoordinateSystem => AxialPlane;

        /// <summary>
        /// Gets or sets the pre operative identifier.
        /// </summary>
        /// <value>
        /// The pre operative identifier.
        /// </value>
        public string PreOperativeId
        {
            get
            {
                return GetString(KeyPreOperativeId, "Unknown");
            }
            set
            {
                Set(KeyPreOperativeId, value);
            }
        }

        /// <summary>
        /// Gets or sets the sacrum.
        /// </summary>
        /// <value>
        /// The sacrum.
        /// </value>
        public Mesh Sacrum
        {
            get
            {
                if (Guid.Empty == SacrumId)
                {
                    return null;
                }

                return _document.Objects.Find(SacrumId).Geometry as Mesh;
            }
            set
            {
                var director = IDSPluginHelper.GetDirector(this._document.DocumentId);
                var objectManager = new ObjectManager(director);
                SacrumId = objectManager.AddNewBuildingBlock(BuildingBlocks.Blocks[IBB.Sacrum], value);
            }
        }

        /// <summary>
        /// Gets or sets the sacrum identifier.
        /// </summary>
        /// <value>
        /// The sacrum identifier.
        /// </value>
        public Guid SacrumId
        {
            get
            {
                return GetGuid(KeySacrum, Guid.Empty);
            }
            set
            {
                Set(KeySacrum, value);
            }
        }

        /// <summary>
        /// Gets or sets the sagittal plane.
        /// </summary>
        /// <value>
        /// The sagittal plane.
        /// </value>
        public Plane SagittalPlane
        {
            get
            {
                return GetPlane(KeySagittalPlane, Plane.Unset);
            }
            set
            {
                Set(KeySagittalPlane, value);
                _pelvicCoordinateSystemSagittalIndex = this._document.NamedConstructionPlanes.Add("PCS Mid", value);
            }
        }

        /// <summary>
        /// Gets or sets the sagittal plane identifier.
        /// </summary>
        /// <value>
        /// The sagittal plane identifier.
        /// </value>
        private Guid SagittalPlaneId
        {
            get
            {
                return GetGuid(KeySagittalPlaneId, Guid.Empty);
            }
            set
            {
                Set(KeySagittalPlaneId, value);
            }
        }

        /// <summary>
        /// Adds the collidable component.
        /// </summary>
        /// <param name="comp">The comp.</param>
        /// <param name="defectPart">if set to <c>true</c> [defect part].</param>
        /// <returns></returns>
        public Guid AddCollidableComponent(Mesh comp, bool defectPart)
        {
            var collidables = CollidableComponents;
            var director = IDSPluginHelper.GetDirector(_document.DocumentId);
            var objectManager = new ObjectManager(director);
            var compId = Guid.Empty;

            compId = objectManager.AddNewBuildingBlock(defectPart ? 
                BuildingBlocks.Blocks[IBB.OtherDefectParts] : BuildingBlocks.Blocks[IBB.OtherContralateralParts], comp);

            collidables.Add(compId);
            Set(KeyCollidables, collidables);
            return compId;
        }

        /// <summary>
        /// Adds the contents from.
        /// </summary>
        /// <param name="preopdict">The preopdict.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">Required data not found in pickle file</exception>
        private static bool AddContentsFrom(IDictionary<object, object> preopdict)
        {
            // Get the top-level dictionaries
            var PCS_data = preopdict.Get("pcs", null) as IDictionary<object, object>;
            var HJC_data = preopdict.Get("HJC", null) as IDictionary<object, object>;
            var INPUT_data = preopdict.Get("input", null) as IDictionary<object, object>;
            var PATHS_data = preopdict.Get("input_paths", null) as IDictionary<object, object>;
            var PREOPID_data = preopdict.Get("preopid", null) as String;
            if (null == PCS_data || null == HJC_data || null == INPUT_data)
            {
                throw new ArgumentNullException("Required data not found in pickle file");
            }

            return true;
        }

        /// <summary>
        /// Gets the plane.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        private Plane GetPlane(string key, Plane defaultValue)
        {
            object plane_obj;
            var rc = TryGetValue(key, out plane_obj);
            if (rc && plane_obj is Plane)
            {
                return (Plane)plane_obj;
            }

            return defaultValue;
        }

        /// <summary>
        /// Writes to archive.
        /// </summary>
        /// <param name="archive">The archive.</param>
        public void WriteToArchive(Rhino.FileIO.BinaryArchiveWriter archive)
        {
            archive.WriteDictionary(this);
        }
    }
}