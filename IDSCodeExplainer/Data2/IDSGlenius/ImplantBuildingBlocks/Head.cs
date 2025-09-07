using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Glenius.Enumerators;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.DocObjects.Custom;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.Glenius.ImplantBuildingBlocks
{
    public class Head : CustomBrepObject, IBBinterface<GleniusImplantDirector>
    {
        private const string keyCoordinateSystem = "coordinate_system";

        private const string KeyHeadType = "HeadType";

        public Head() : base()
        {
            // empty
        }

        public Head(Brep brep) : base(brep)
        {
            // empty
        }

        public Head(GleniusImplantDirector director, Brep brep, HeadType type) : this(brep)
        {
            Director = director;
            HeadType = type;
        }

        private Head(RhinoObject other, bool fromArchive) : this(other.Geometry as Brep)
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

        public Head(GleniusImplantDirector director, Brep otherHeadBrep, HeadType type, Head propertiesAndAttributesBasedOn) : this(otherHeadBrep)
        {
            // Replace the object in the document or create new one
            if(propertiesAndAttributesBasedOn != null)
            {
                Attributes = propertiesAndAttributesBasedOn.Attributes;
                DuplicateProperties(propertiesAndAttributesBasedOn);
            }
            Director = director;
            HeadType = type;
        }

        public GleniusImplantDirector Director { get; set; }

        public Plane CoordinateSystem
        {
            get
            {
                if (Attributes.UserDictionary.ContainsKey(keyCoordinateSystem))
                {
                    return (Plane) Attributes.UserDictionary[keyCoordinateSystem];
                }
                else
                {
                    var cs = Plane.WorldXY;
                    Attributes.UserDictionary.Set(keyCoordinateSystem, cs);
                    return cs;
                }
            }
            private set
            {
                Attributes.UserDictionary.Set(keyCoordinateSystem, value);
            }
        }

        public HeadType HeadType { get; set; }

        public bool IsRealignment { get; set; }

        public static Head CreateFromArchived(RhinoObject other, bool replaceInDoc)
        {
            // Restore the head object from archive
            Head restored = new Head(other, true);

            // Replace if necessary
            if (replaceInDoc && !IDSPluginHelper.ReplaceRhinoObject(other, restored))
            {
                return null;
            }

            return restored;
        }

        public void DeArchive(ArchivableDictionary udict)
        {
            // Load data
            HeadType = udict.GetEnumValue<HeadType>(KeyHeadType);
        }

        public void PrepareForArchiving()
        {
            Attributes.UserDictionary.SetEnumValue<IBB>(ImplantBuildingBlockProperties.KeyBlockType, IBB.Head);
            Attributes.UserDictionary.SetEnumValue<HeadType>(KeyHeadType, HeadType);
            CommitChanges();
        }

        protected override void OnDuplicate(RhinoObject source)
        {
            base.OnDuplicate(source);
            DuplicateProperties(source);
        }
        
        protected override void OnTransform(Transform xform)
        {
            base.OnTransform(xform);
            TransformHeadDependentComponents(xform);
        }

        private void DuplicateProperties(RhinoObject source)
        {
            // Convert the Rhino object to a Head
            var sourceHead = source as Head;

            // Copy properties
            Director = sourceHead.Director;
            CoordinateSystem = sourceHead.CoordinateSystem;
            HeadType = sourceHead.HeadType;
            IsRealignment = sourceHead.IsRealignment;
        }

        private void TransformHeadDependentComponents(Transform xform)
        {
            var objectManager = new GleniusObjectManager(Director);
            var components = BuildingBlocks.GetHeadComponents().ToList();
            if (!IsRealignment)
            {
                components.AddRange(BuildingBlocks.GetM4ConnectionScrewComponents());
                components.AddRange(BuildingBlocks.GetBasePlateComponents());
            }
            
            foreach (var ibb in components)
            {
                var guid = objectManager.GetBuildingBlockId(ibb);
                if (guid != Guid.Empty)
                {
                    objectManager.TransformBuildingBlock(ibb, xform);
                }
            }

            if (!IsRealignment)
            {
                Director.Graph.NotifyBuildingBlockHasChanged(IBB.BasePlateBottomContour);
            }
            IsRealignment = false;
        }
    }
}