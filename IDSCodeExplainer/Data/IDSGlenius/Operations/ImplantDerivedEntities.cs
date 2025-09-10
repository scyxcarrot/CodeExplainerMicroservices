using IDS.Core.Operations;
using IDS.Core.Utilities;
using IDS.Glenius.Enumerators;
using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class ImplantDerivedEntities
    {
        private readonly GleniusImplantDirector director;
        private readonly GleniusObjectManager objectManager;
        private readonly ImporterViaRunScript importer;
        private readonly Resources gleniusResources;
        private readonly ScrewResources screwResources;
        private readonly Transform headAlignment;
        private readonly Transform m4ScrewConnectionAlignment;
        private readonly HeadType headType;

        public ImplantDerivedEntities(GleniusImplantDirector director)
        {
            this.director = director;
            objectManager = new GleniusObjectManager(director);
            importer = new ImporterViaRunScript();
            gleniusResources = new Resources();
            screwResources = new ScrewResources();
            headAlignment = GetHeadAlignmentTransform();
            m4ScrewConnectionAlignment = GetM4ConnectionScrewTransform();
            headType = GetHeadType();
        }

        public Mesh GetScapulaReamedWithWrap()
        {
            var scapulaReamedObject = objectManager.GetBuildingBlock(IBB.ScapulaReamed);
            var scapulaReamed = scapulaReamedObject?.Geometry.Duplicate() as Mesh;
            if (scapulaReamed != null)
            {
                //var wrapParams = new MDCKShrinkWrapParameters(0.3, 0.0, 0.3, false, true, false, false);
                Mesh scapulaReamedWithWrap;
                if (Wrap.PerformWrap(new[] {scapulaReamed}, 0.3, 0.0, 0.3, false, true, false, false, out scapulaReamedWithWrap))
                {
                    return scapulaReamedWithWrap;
                }
            }
            return null;
        }

        public Brep GetTaperMantle()
        {
            return ImportBrepAndTransform(gleniusResources.TaperMantle, headAlignment);
        }

        public Brep GetCylinder()
        {
            return ImportBrepAndTransform(gleniusResources.CommonCylinderStepFile, headAlignment);
        }

        public Brep GetCylinderWithTransition()
        {
            return ImportBrepAndTransform(gleniusResources.CommonCylinderWithTransitionStepFile, headAlignment);
        }

        public Brep GetUnroundedCylinder()
        {
            return ImportBrepAndTransform(gleniusResources.UnroundedCylinder, headAlignment);
        }

        public Brep GetTaperBooleanReal()
        {
            return ImportBrepAndTransform(gleniusResources.TaperBooleanReal, headAlignment);
        }

        public Brep GetTaperBooleanProduction()
        {
            return ImportBrepAndTransform(gleniusResources.TaperBooleanProduction, headAlignment);
        }

        public Brep GetTaperBooleanPlastic()
        {
            return ImportBrepAndTransform(gleniusResources.TaperBooleanPlastic, headAlignment);
        }

        public Brep GetCylinderOffset()
        {
            return ImportBrepAndTransform(gleniusResources.CylinderOffset, headAlignment);
        }

        public Brep GetReferenceBlock()
        {
            return ImportBrepAndTransform(gleniusResources.ReferenceBlock, headAlignment);
        }

        public Brep GetM4ConnectionScrewHoleProduction()
        {
            return ImportBrepAndTransform(screwResources.ScrewM4Connection.HoleProduction, m4ScrewConnectionAlignment);
        }

        public Brep GetM4ConnectionScrewHoleReal()
        {
            return ImportBrepAndTransform(screwResources.ScrewM4Connection.HoleReal, m4ScrewConnectionAlignment);
        }

        public Brep GetM4ConnectionHoleGuide()
        {
            return ImportBrepAndTransform(screwResources.ScrewM4Connection.GuideHole, m4ScrewConnectionAlignment);
        }

        public Brep GetM4ConnectionMantle()
        {
            return ImportBrepAndTransform(screwResources.ScrewM4Connection.ScrewMantle, m4ScrewConnectionAlignment);
        }

        public Brep GetScrewHoleReal(Screw screw)
        {
            var filePath = string.Empty;
            switch (screw.ScrewType)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    filePath = screwResources.Screw3Dot5Locking.HoleReal;
                    break;
                case ScrewType.TYPE_4Dot0_LOCKING:
                    filePath = screwResources.Screw4Dot0Locking.HoleReal;
                    break;
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    filePath = screwResources.Screw4Dot0NonLocking.HoleReal;
                    break;
                default:
                    throw new Exception("Unknown screw type");
            }
            return ImportBrepAndTransform(filePath, GetScrewCoordinateSystemTransform(screw));
        }

        public Brep GetScrewHoleProduction(Screw screw)
        {
            var filePath = string.Empty;
            switch (screw.ScrewType)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    filePath = screwResources.Screw3Dot5Locking.HoleProduction;
                    break;
                case ScrewType.TYPE_4Dot0_LOCKING:
                    filePath = screwResources.Screw4Dot0Locking.HoleProduction;
                    break;
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    filePath = screwResources.Screw4Dot0NonLocking.HoleProduction;
                    break;
                default:
                    throw new Exception("Unknown screw type");
            }
            return ImportBrepAndTransform(filePath, GetScrewCoordinateSystemTransform(screw));
        }

        public Brep GetScrewHoleProductionOffset(Screw screw)
        {
            var filePath = string.Empty;
            switch (screw.ScrewType)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    filePath = screwResources.Screw3Dot5Locking.HoleProductionOffset;
                    break;
                case ScrewType.TYPE_4Dot0_LOCKING:
                    filePath = screwResources.Screw4Dot0Locking.HoleProductionOffset;
                    break;
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    filePath = screwResources.Screw4Dot0NonLocking.HoleProductionOffset;
                    break;
                default:
                    throw new Exception("Unknown screw type");
            }
            return ImportBrepAndTransform(filePath, GetScrewCoordinateSystemTransform(screw));
        }

        public Brep GetScrewHoleScaffold(Screw screw)
        {
            var filePath = string.Empty;
            switch (screw.ScrewType)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    filePath = screwResources.Screw3Dot5Locking.HoleScaffold;
                    break;
                case ScrewType.TYPE_4Dot0_LOCKING:
                    filePath = screwResources.Screw4Dot0Locking.HoleScaffold;
                    break;
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    filePath = screwResources.Screw4Dot0NonLocking.HoleScaffold;
                    break;
                default:
                    throw new Exception("Unknown screw type");
            }
            return ImportBrepAndTransform(filePath, GetScrewCoordinateSystemTransform(screw));
        }

        public Mesh GetScaffoldShape()
        {
            var scaffoldTop = objectManager.GetBuildingBlock(IBB.ScaffoldTop)?.Geometry.Duplicate() as Mesh;
            var scaffoldSide = objectManager.GetBuildingBlock(IBB.ScaffoldSide)?.Geometry.Duplicate() as Mesh;
            var scaffoldBottom = objectManager.GetBuildingBlock(IBB.ScaffoldBottom)?.Geometry.Duplicate() as Mesh;
            var scaffoldShape = MeshUtilities.AppendMeshes(new List<Mesh> { scaffoldTop , scaffoldSide, scaffoldBottom});
            return MeshUtilities.FixMesh(scaffoldShape);
        }

        public Plane GetMetalBackingPlane()
        {
            var plateDrawingGenerator = new PlateDrawingPlaneGenerator(director);
            return plateDrawingGenerator.GenerateTopPlane();
        }

        public Mesh GetGuideHandle()
        {
            return ImportMeshAndTransform(gleniusResources.GuideHandle, headAlignment);
        }

        public Mesh GetPlasticHead()
        {
            var filePath = string.Empty;
            switch (headType)
            {
                case HeadType.TYPE_36_MM:
                    filePath = gleniusResources.PlasticHead36;
                    break;
                case HeadType.TYPE_38_MM:
                    filePath = gleniusResources.PlasticHead38;
                    break;
                case HeadType.TYPE_42_MM:
                    filePath = gleniusResources.PlasticHead42;
                    break;
                default:
                    filePath = gleniusResources.PlasticHead36;
                    break;
            }
            return ImportMeshAndTransform(filePath, headAlignment);
        }

        private Transform GetHeadAlignmentTransform()
        {
            var alignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document, director.defectIsLeft);
            var headCoordinateSystem = alignment.GetHeadCoordinateSystem();
            var transform = Transform.ChangeBasis(headCoordinateSystem, new Plane(new Point3d(0, 0, 0), new Vector3d(1, 0, 0), new Vector3d(0, 1, 0)));
            return transform;
        }

        private Transform GetM4ConnectionScrewTransform()
        {
            var transform = Transform.Identity;
            Plane objectCoordinateSystem;
            if (objectManager.GetBuildingBlockCoordinateSystem(IBB.M4ConnectionScrew, out objectCoordinateSystem))
            {
                transform = Transform.ChangeBasis(objectCoordinateSystem, new Plane(new Point3d(0, 0, 0), new Vector3d(1, 0, 0), new Vector3d(0, 1, 0)));
            }
            return transform;
        }

        private Transform GetScrewCoordinateSystemTransform(Screw screw)
        {
            return ScrewBrepFactory.GetAlignmentTransform(screw.Direction, screw.HeadPoint);
        }

        private Brep ImportBrepAndTransform(string filePath, Transform transform)
        {
            return ImportAndTransform(filePath, transform) as Brep;
        }

        private Mesh ImportMeshAndTransform(string filePath, Transform transform)
        {
            return ImportAndTransform(filePath, transform) as Mesh;
        }

        private GeometryBase ImportAndTransform(string filePath, Transform transform)
        {
            GeometryBase geometry;
            if (Import(filePath, out geometry))
            {
                geometry.Transform(transform);
            }
            return geometry;
        }

        private bool Import(string filePath, out GeometryBase geometry)
        {
            geometry = null;
            var guid = importer.Import(filePath).FirstOrDefault();
            if (guid != Guid.Empty)
            {
                var rhObj = director.Document.Objects.Find(guid);
                objectManager.DeleteObject(guid);
                geometry = rhObj.Geometry;
                return true;
            }

            return false;
        }

        private HeadType GetHeadType()
        {
            var head = objectManager.GetBuildingBlock(IBB.Head) as Head;
            return head.HeadType;
        }
    }
}