using IDS.Glenius.Enumerators;
using IDS.Glenius.Operations;
using Rhino.DocObjects;
using Rhino.DocObjects.Custom;
using Rhino.Geometry;

namespace IDS.Glenius.ImplantBuildingBlocks
{
    public class ScrewMantle : CustomBrepObject
    {
        #region Properties derived from Attributes

        private const string KeyStartExtension = "StartExtension";
        private const string KeyScrewType = "ScrewType";
        private const string KeyExtensionLength = "ExtensionLength";
        private const string KeyExtensionDirection = "ExtensionDirection";

        public Point3d StartExtension
        {
            get
            {
                return Attributes.UserDictionary.GetPoint3d(KeyStartExtension);
            }
            private set
            {
                Attributes.UserDictionary.Set(KeyStartExtension, value);
            }
        }

        public ScrewType ScrewType
        {
            get
            {
                return Attributes.UserDictionary.GetEnumValue<ScrewType>(KeyScrewType);
            }
            private set
            {
                Attributes.UserDictionary.SetEnumValue(KeyScrewType, value);
            }
        }

        public double ExtensionLength
        {
            get
            {
                return Attributes.UserDictionary.GetDouble(KeyExtensionLength, 0.0);
            }
            private set
            {
                Attributes.UserDictionary.Set(KeyExtensionLength, value);
            }
        }

        public Vector3d ExtensionDirection
        {
            get
            {
                return Attributes.UserDictionary.GetVector3d(KeyExtensionDirection);
            }
            private set
            {
                Attributes.UserDictionary.Set(KeyExtensionDirection, value);
            }
        }

        #endregion

        public ScrewMantle() : base()
        {
        }
        
        public ScrewMantle(Brep brep) : base(brep)
        {
        }

        public ScrewMantle(RhinoObject other) : base(other.Geometry as Brep)
        {
            Attributes = other.Attributes;
        }

        public ScrewMantle(Brep brep, ScrewType screwType, Point3d startExtensionPoint, Vector3d extensionDirection, double extensionLength)
            : base(brep)
        {
            ScrewType = screwType;
            StartExtension = startExtensionPoint;
            ExtensionDirection = extensionDirection;
            ExtensionLength = extensionLength;
        }

        public ScrewMantle(ScrewType screwType, Point3d startExtensionPoint, Vector3d extensionDirection, double extensionLength)
            : this(new ScrewMantleBrepFactory(screwType).
                  CreateScrewMantleBrep(startExtensionPoint, extensionDirection, extensionLength),
                  screwType, startExtensionPoint, extensionDirection, extensionLength)
        {
        }

        protected override void OnTransform(Transform xform)
        {
            base.OnTransform(xform);
            StartExtension.Transform(xform);
            ExtensionDirection.Transform(xform);
        }

        public static ScrewMantle CreateScrewMantleFromScrewHeadPoint(Brep brep, ScrewType screwType, Point3d screwHeadPoint, Vector3d extensionDirection)
        {
            var startExtension = GetStartExtension(screwHeadPoint, extensionDirection, screwType);
            return new ScrewMantle(brep, screwType, startExtension, extensionDirection, 0.0);
        }

        public bool IsDataValid()
        {
            if (!Attributes.UserDictionary.ContainsKey(KeyStartExtension) || !Attributes.UserDictionary.ContainsKey(KeyScrewType) ||
                !Attributes.UserDictionary.ContainsKey(KeyExtensionLength) || !Attributes.UserDictionary.ContainsKey(KeyExtensionDirection))
            {
                return false;
            }
            return true;
        }

        public void ConstructData(Screw screw)
        {
            ScrewType = screw.ScrewType;
            ExtensionDirection = new Vector3d(screw.Direction);
            StartExtension = GetStartExtension(screw.HeadPoint, ExtensionDirection, screw.ScrewType);
            ExtensionLength = 0;
        }

        private static Point3d GetStartExtension(Point3d screwHeadPoint, Vector3d extensionDirection, ScrewType type)
        {
            var screwFactory = new ScrewBrepFactory(type);
            var headHeight = screwFactory.GetHeadHeight();

            var startExtension = new Point3d(0, 0, -headHeight);
            startExtension.Transform(ScrewBrepFactory.GetAlignmentTransform(extensionDirection, screwHeadPoint));
            return startExtension;
        }

        public void SetLength(double length, GleniusObjectManager objManager)
        {
            var mantle = new ScrewMantle(ScrewType, StartExtension, ExtensionDirection, length);
            objManager.SetBuildingBlock(IBB.ScrewMantle, mantle, this.Id);
        }
    }
}
