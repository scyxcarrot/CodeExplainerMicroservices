using IDS.Glenius.Enumerators;
using Rhino.Geometry;
using IDS.Glenius.Operations;

namespace IDS.Glenius
{
    //How it works: If the component variable is null, it will load it in a file, else return whatever that has been loaded.
    public static class ScrewBrepComponentDatabase
    {
        private static readonly ScrewComponentImporter ComponentImporter = new ScrewComponentImporter();

        public static void PreLoadScrewHead(ScrewType type)
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        if (_screw3Dot5Head == null)
                        {
                            _screw3Dot5Head = ComponentImporter.ImportScrewHeadBrep(type);
                        }
                        break;
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                    {
                        if (_screw4Dot0LockingHead == null)
                        {
                            _screw4Dot0LockingHead = ComponentImporter.ImportScrewHeadBrep(type);
                        }
                        break;
                    }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        if (_screw4Dot0NonLockingHead == null)
                        {
                            _screw4Dot0NonLockingHead = ComponentImporter.ImportScrewHeadBrep(type);
                        }
                        break;
                    }
                default:
                    break;
            }
        }

        public static void PreLoadScrewAides(ScrewType type)
        {
            switch (type)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                {
                    if (_screw3Dot5Mantle == null)
                    {
                        _screw3Dot5Mantle = ComponentImporter.ImportScrewMantleBrep(type);
                    }
                    if (_screw3Dot5SafetyZone == null)
                    {
                        _screw3Dot5SafetyZone = ComponentImporter.ImportScrewSafetyZoneBrep(type);
                    }
                    if (_screw3Dot5DrillGuideCylinder == null)
                    {
                        _screw3Dot5DrillGuideCylinder = ComponentImporter.ImportScrewDrillGuideCylinderBrep(type);
                    }
                    break;
                }
                case ScrewType.TYPE_4Dot0_LOCKING:
                {
                    if (_screw4Dot0LockingMantle == null)
                    {
                        _screw4Dot0LockingMantle = ComponentImporter.ImportScrewMantleBrep(type);
                    }
                    if (_screw4Dot0LockingSafetyZone == null)
                    {
                        _screw4Dot0LockingSafetyZone = ComponentImporter.ImportScrewSafetyZoneBrep(type);
                    }
                    if (_screw4Dot0LockingDrillGuideCylinder == null)
                    {
                        _screw4Dot0LockingDrillGuideCylinder = ComponentImporter.ImportScrewDrillGuideCylinderBrep(type);
                    }
                    break;
                }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                {
                    if (_screw4Dot0NonLockingMantle == null)
                    {
                        _screw4Dot0NonLockingMantle = ComponentImporter.ImportScrewMantleBrep(type);
                    }
                    if (_screw4Dot0NonLockingSafetyZone == null)
                    {
                        _screw4Dot0NonLockingSafetyZone = ComponentImporter.ImportScrewSafetyZoneBrep(type);
                    }
                    if (_screw4Dot0NonLockingDrillGuideCylinder == null)
                    {
                        _screw4Dot0NonLockingDrillGuideCylinder = ComponentImporter.ImportScrewDrillGuideCylinderBrep(type);
                    }
                    break;
                }
                default:
                    break;
            }
        }

        #region ScrewHeads

        private static Brep _screw3Dot5Head;
        public static Brep Screw3Dot5Head => 
            _screw3Dot5Head ?? (_screw3Dot5Head = ComponentImporter.ImportScrewHeadBrep(ScrewType.TYPE_3Dot5_LOCKING));

        private static Brep _screw4Dot0LockingHead;
        public static Brep Screw4Dot0LockingHead =>
            _screw4Dot0LockingHead ?? (_screw4Dot0LockingHead = ComponentImporter.ImportScrewHeadBrep(ScrewType.TYPE_4Dot0_LOCKING)); 

        private static Brep _screw4Dot0NonLockingHead;
        public static Brep Screw4Dot0NonLockingHead =>
            _screw4Dot0NonLockingHead ?? (_screw4Dot0NonLockingHead = ComponentImporter.ImportScrewHeadBrep(ScrewType.TYPE_4Dot0_NONLOCKING)); 

        #endregion

        #region Mantle

        private static Brep _screw3Dot5Mantle;
        public static Brep Screw3Dot5Mantle => 
            _screw3Dot5Mantle ?? (_screw3Dot5Mantle = ComponentImporter.ImportScrewMantleBrep(ScrewType.TYPE_3Dot5_LOCKING));

        private static Brep _screw4Dot0LockingMantle;
        public static Brep Screw4Dot0LockingMantle => 
            _screw4Dot0LockingMantle ?? (_screw4Dot0LockingMantle = ComponentImporter.ImportScrewMantleBrep(ScrewType.TYPE_4Dot0_LOCKING));

        private static Brep _screw4Dot0NonLockingMantle;
        public static Brep Screw4Dot0NonLockingMantle => 
            _screw4Dot0NonLockingMantle ?? (_screw4Dot0NonLockingMantle = ComponentImporter.ImportScrewMantleBrep(ScrewType.TYPE_4Dot0_NONLOCKING));

        #endregion

        #region SafetyZone

        private static Brep _screw3Dot5SafetyZone;

        public static Brep Screw3Dot5SafetyZone =>
            _screw3Dot5SafetyZone ?? (_screw3Dot5SafetyZone = ComponentImporter.ImportScrewSafetyZoneBrep(ScrewType.TYPE_3Dot5_LOCKING));

        private static Brep _screw4Dot0LockingSafetyZone;
        public static Brep Screw4Dot0LockingSafetyZone => 
            _screw4Dot0LockingSafetyZone ?? (_screw4Dot0LockingSafetyZone = ComponentImporter.ImportScrewSafetyZoneBrep(ScrewType.TYPE_4Dot0_LOCKING));

        private static Brep _screw4Dot0NonLockingSafetyZone;
        public static Brep Screw4Dot0NonLockingSafetyZone => 
            _screw4Dot0NonLockingSafetyZone ?? (_screw4Dot0NonLockingSafetyZone = ComponentImporter.ImportScrewSafetyZoneBrep(ScrewType.TYPE_4Dot0_NONLOCKING));

        #endregion

        #region DrillGuideCylinder

        private static Brep _screw3Dot5DrillGuideCylinder;

        public static Brep Screw3Dot5DrillGuideCylinder => 
            _screw3Dot5DrillGuideCylinder ?? (_screw3Dot5DrillGuideCylinder = ComponentImporter.ImportScrewDrillGuideCylinderBrep(ScrewType.TYPE_3Dot5_LOCKING));

        private static Brep _screw4Dot0LockingDrillGuideCylinder;
        public static Brep Screw4Dot0LockingDrillGuideCylinder => 
            _screw4Dot0LockingDrillGuideCylinder ?? (_screw4Dot0LockingDrillGuideCylinder = ComponentImporter.ImportScrewDrillGuideCylinderBrep(ScrewType.TYPE_4Dot0_LOCKING));

        private static Brep _screw4Dot0NonLockingDrillGuideCylinder;
        public static Brep Screw4Dot0NonLockingDrillGuideCylinder => 
            _screw4Dot0NonLockingDrillGuideCylinder ?? (_screw4Dot0NonLockingDrillGuideCylinder = ComponentImporter.ImportScrewDrillGuideCylinderBrep(ScrewType.TYPE_4Dot0_NONLOCKING));

        #endregion
    }
}
