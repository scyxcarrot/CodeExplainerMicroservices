using IDS.Core.ImplantBuildingBlocks;
using IDS.Glenius.Enumerators;
using IDS.Glenius.Operations;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;

namespace IDS.Glenius.ImplantBuildingBlocks
{
    public class Screw : ScrewBase<GleniusImplantDirector, ScrewType, ScrewAideType>, IBBinterface<GleniusImplantDirector>
    {
        private ScrewBrepFactory _screwBrepFactoryinstance;
        private ScrewBrepFactory ScrewBrepFactoryInstance
        {
            get
            {
                if(_screwBrepFactoryinstance == null)
                {
                    _screwBrepFactoryinstance = new ScrewBrepFactory(ScrewType);
                }

                return _screwBrepFactoryinstance;
            }
        }


        public Screw(GleniusImplantDirector director, ScrewType screwType, double axialOffsetInit, int index)
        {
            HeadPoint = Point3d.Unset;
            TipPoint = Point3d.Unset;
            ScrewType = screwType;
            _fixedLength = 0.0; // not set
            Director = director;
            Index = index;
        }

        public Screw(GleniusImplantDirector director, Point3d headPoint, Point3d tipPoint, ScrewType screwType, int newIndex)
            : base(new ScrewBrepFactory(screwType).CreateScrewBrep(headPoint, tipPoint))
        {
            HeadPoint = headPoint;
            TipPoint = tipPoint;
            ScrewType = screwType;
            _fixedLength = 0.0; // not set
            Director = director;
            Index = newIndex;
        }

        public Screw() : base()
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
            : base(other, copyAttributes)
        {
            // Copy member variables (tries to cast to screw)
            OnDuplicate(other);

            // Load member variables from UserDictionary
            if (fromArchive)
            {
                // Load member variables from archive
                ArchivableDictionary udict = other.Attributes.UserDictionary;
                DeArchive(udict);
            }
        }

        /// <summary>
        /// De-serialize member variables from archive.
        /// </summary>
        /// <param name="userDict">The user dictionary.</param>
        public override void DeArchive(ArchivableDictionary userDict)
        {
            base.DeArchive(userDict);

            ScrewType screwTypeTmp;

            if (userDict.TryGetEnumValue<ScrewType>(KeyScrewType, out screwTypeTmp))
            {
                ScrewType = screwTypeTmp;
            }

            // Load aide GUIDs
            foreach (ScrewAideType key in Enum.GetValues(typeof(ScrewAideType)))
            {
                var screwAideID = userDict.GetGuid(key.ToString(), Guid.Empty);
                if (screwAideID != Guid.Empty)
                {
                    ScrewAides.Add(key, screwAideID);
                }
            }
        }

        /// <summary>
        /// Serialize member variables to user dictionary.
        /// </summary>
        public override void PrepareForArchiving()
        {
            base.PrepareForArchiving();

            ArchivableDictionary userDict = this.Attributes.UserDictionary;
            userDict.SetEnumValue<IBB>(ImplantBuildingBlockProperties.KeyBlockType, IBB.Screw);
            userDict.SetEnumValue<ScrewType>(KeyScrewType, ScrewType);
            if (ScrewAides != null)
            {
                foreach (ScrewAideType key in ScrewAides.Keys)
                {
                    userDict.Set(key.ToString(), ScrewAides[key]);
                }
            }

            CommitChanges();
        }

        public override Point3d BodyOrigin
        {
            get
            {
                var screwFactory = new ScrewBrepFactory(ScrewType);
                var headHeight = screwFactory.GetHeadHeight();
                return HeadPoint + Direction * headHeight;
            }
        }

        public override double GetDistanceInBone()
        {
            GleniusObjectManager objectManager = new GleniusObjectManager(Director);
            Mesh target = objectManager.GetBuildingBlock(BuildingBlocks.Blocks[IBB.ScapulaDesignReamed]).GetMeshes(MeshType.Default)[0];
            return CalculateDistanceInBone(target);
        }

        public override Point3d HeadPoint
        {
            get;
            protected set;
        }

        public Point3d headCenter
        {
            get
            {
                var screwFactory = new ScrewBrepFactory(ScrewType);
                var headCenterOffsetFromHeadPoint = screwFactory.GetHeadCenterOffsetFromHeadPoint();
                var center = new Point3d(0, 0, headCenterOffsetFromHeadPoint);
                center.Transform(AlignmentTransform);
                return center;
            }
        }

        //used by base class to perform intersection; requires a big number to make sure that projection goes through the target 
        public override double MaximumBodyLength => 500.0;

        public override bool IsBicortical
        {
            get
            {
                var objectManager = new GleniusObjectManager(Director);
                var target = objectManager.GetBuildingBlock(BuildingBlocks.Blocks[IBB.ScapulaDesignReamed]).GetMeshes(MeshType.Default)[0];
                return GetCorticalBites(target, BodyOrigin, BodyLength) > 1;
            }
        }

        public override Point3d TipPoint
        {
            get;
            protected set;
        }

        public override string GenerateNameForMimics()
        {
            return string.Format("{0}_{1}_{2:D}", Director.caseId, ScrewType, Index);
        }

        public override void Set(Guid oldScrewId, bool recalibrate, bool update)
        {
            var isScrewInvalidationSubscribed = Director.ScrewObjectManager.IsScrewInvalidationSubscribed;
            if (isScrewInvalidationSubscribed)
            {
                Director.ScrewObjectManager.UnSubscribeScrewInvalidation();
            }

            GleniusObjectManager objManager = new GleniusObjectManager(Director);

            objManager.SetBuildingBlock(IBB.Screw, this, oldScrewId);

            if (isScrewInvalidationSubscribed && !Director.ScrewObjectManager.IsScrewInvalidationSubscribed)
            {
                Director.ScrewObjectManager.SubscribeScrewInvalidation();
            }
        }

        protected override void CreateAides()
        {
            GleniusObjectManager objectManager = new GleniusObjectManager(Director);

            Action<Brep, ScrewAideType, IBB> addAide = (inBrep, aideType, buildingBlockInto) =>
            {
                var brep = new Brep();
                brep.Append(inBrep);
                brep.Transform(AlignmentTransform);

                if (aideType == ScrewAideType.Mantle)
                {
                    ScrewAides[aideType] = objectManager.AddNewBuildingBlock(buildingBlockInto, ScrewMantle.CreateScrewMantleFromScrewHeadPoint(brep, ScrewType,HeadPoint, Direction), true);
                }
                else
                {
                    ScrewAides[aideType] = objectManager.AddNewBuildingBlock(buildingBlockInto, brep);
                }
                ImplantBuildingBlockProperties.SetTransparency(BuildingBlocks.Blocks[buildingBlockInto], Director.Document, 0.5);
            };

            var factory = new ScrewMantleBrepFactory(ScrewType);
            addAide(factory.CreateScrewMantleBrep(0.0), ScrewAideType.Mantle, IBB.ScrewMantle);
            switch (ScrewType)
            {
                case ScrewType.TYPE_3Dot5_LOCKING:
                    {
                        addAide(ScrewBrepComponentDatabase.Screw3Dot5SafetyZone, ScrewAideType.SafetyZone, IBB.ScrewSafetyZone);
                        addAide(ScrewBrepComponentDatabase.Screw3Dot5DrillGuideCylinder, ScrewAideType.GuideDrillCylinder, IBB.ScrewDrillGuideCylinder);
                        break;
                    }
                case ScrewType.TYPE_4Dot0_LOCKING:
                    {
                        addAide(ScrewBrepComponentDatabase.Screw4Dot0LockingSafetyZone, ScrewAideType.SafetyZone, IBB.ScrewSafetyZone);
                        addAide(ScrewBrepComponentDatabase.Screw4Dot0LockingDrillGuideCylinder, ScrewAideType.GuideDrillCylinder, IBB.ScrewDrillGuideCylinder); 
                        break;
                    }
                case ScrewType.TYPE_4Dot0_NONLOCKING:
                    {
                        addAide(ScrewBrepComponentDatabase.Screw4Dot0NonLockingSafetyZone, ScrewAideType.SafetyZone, IBB.ScrewSafetyZone);
                        addAide(ScrewBrepComponentDatabase.Screw4Dot0NonLockingDrillGuideCylinder, ScrewAideType.GuideDrillCylinder, IBB.ScrewDrillGuideCylinder);
                        break;
                    }
                default:
                    break;
            }
        }

        protected override double GetDiameter() => ScrewBrepFactoryInstance.GetScrewBodyRadius() * 2;

        protected override bool Update(Guid oldID)
        {
            var objectManager = new GleniusObjectManager(Director);

            // Update length to an available one
            //** unable to use base.SetAvailableLength(); because adjustment should be done on the head instead of the tip
            bool exceeded;
            var nearestLength = ScrewCalibrator.AdjustLengthToAvailableScrewLength(ScrewType, TotalLength, out exceeded);
            HeadPoint = TipPoint - Direction * nearestLength;

            Screw newScrew = new Screw(Director, HeadPoint, TipPoint, ScrewType, 0);
            newScrew.Index = Index; // Keep screw index
            newScrew._fixedLength = _fixedLength; // Keep fixed length (if any)

            // Update geometry in the document (also creates the screw aides in OnAddToDocument)
            Guid ID = objectManager.SetBuildingBlock(IBB.Screw, newScrew, oldID);
            if (ID == Guid.Empty)
            {
                return false;
            }

            // Write the properties of the new screw back to the current entity
            Attributes = newScrew.Attributes;
            ScrewAides = newScrew.ScrewAides;

            return true;
        }

        protected override double[] ScrewLengths => ScrewBrepFactory.GetAvailableScrewLengths(ScrewType);

        /// <summary>
        /// Distances the until bone.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="screwDatabase">The screw database.</param>
        /// <returns></returns>
        public override double GetDistanceUntilBone()
        {
            GleniusObjectManager objectManager = new GleniusObjectManager(Director);
            Mesh target = objectManager.GetBuildingBlock(BuildingBlocks.Blocks[IBB.ScapulaDesignReamed]).GetMeshes(MeshType.Default)[0];

            return CalculateDistanceUntilBone(target);
        }

        //Please check the returned value before use. Is it the expected value?
        public override double GetAvailableLength() => BodyLength;

        public void UpdateScrewMantleGuid(Guid newScrewMantleId)
        {
            if (ScrewAides.ContainsKey(ScrewAideType.Mantle))
            {
                ScrewAides[ScrewAideType.Mantle] = newScrewMantleId;
            }
        }

        public string GetScrewLockingType()
        {
            return ScrewBrepFactoryInstance.GetScrewLockingType();
        }

        public double GetOffsetFromIdealPlacementPlane()
        {
            var placementPlaneGenerator = new ScrewPlacementPlaneGenerator(Director);
            var idealPlacementPlane = placementPlaneGenerator.GenerateHeadConstraintPlane();

            var offsetDistance = idealPlacementPlane.DistanceTo(headCenter);

            //Rhino Plane's DistanceTo method returns a signed distance value. If the point is below the plane, a negative distance is returned.
            //Negate the distance because the idealPlacementPlane's Normal is pointing to the medial side but the offset should be + when headCenter is on the lateral side
            return offsetDistance * -1;
        }

        public ScrewMantle GetScrewMantle()
        {
            if (!ScrewAides.ContainsKey(ScrewAideType.Mantle))
                return null;

            var mantle = Director.Document.Objects.Find(ScrewAides[ScrewAideType.Mantle]) as ScrewMantle;
            return mantle;
        }
    }
}
