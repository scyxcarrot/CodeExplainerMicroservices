using IDS.CMF.CasePreferences;
using IDS.CMF.Constants;
using IDS.CMF.Factory;
using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.Operations;
using IDS.CMF.Quality;
using IDS.CMF.Query;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.Enumerators;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.DocObjects.Custom;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Plane = Rhino.Geometry.Plane;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public class Screw : CustomBrepObject, IBBinterface<CMFImplantDirector>
    {
        protected const string KeyHeadPoint = "head_point";
        protected const string KeyIndex = "index";
        protected const string KeyTipPoint = "tip_point";
        // set this to public for backward compatibility changes
        public string KeyScrewType = "screw_type";
        protected const string KeyOriginalDirection = "original_direction";
        protected const string KeyBarrelType = "barrel_type";

        private ScrewBrepFactory _screwBrepFactoryInstance;
        public ScrewBrepFactory ScrewBrepFactoryInstance
        {
            get
            {
                if (_screwBrepFactoryInstance == null && ScrewAideDictionary[Constants.ScrewAide.Head] is Brep)
                {
                    _screwBrepFactoryInstance = new ScrewBrepFactory(ScrewAideDictionary[Constants.ScrewAide.Head] as Brep);
                }

                return _screwBrepFactoryInstance;
            }
        }

        //TODO: Do not set the screw type from outside, this will be private in the future.
        public string ScrewType { get; set; }

        public string BarrelType
        {
            get
            {
                return Attributes.UserDictionary.GetString(KeyBarrelType, string.Empty);
            }
            set
            {
                var barrelType = value;
                if (string.IsNullOrEmpty(barrelType))
                {
                    if (Attributes.UserDictionary.ContainsKey(KeyBarrelType))
                    {
                        Attributes.UserDictionary.Remove(KeyBarrelType);
                    }
                    return;
                }

                UserDictionaryUtilities.ModifyUserDictionary(this, KeyBarrelType, barrelType);
            }
        }

        public string ScrewTypeAndBarrelType
        {
            get
            {
                return $"{ScrewType}_{BarrelType}";
            }
        }

        public Guid RegisteredBarrelId
        {
            get
            {
                return Attributes.UserDictionary.GetGuid(AttributeKeys.KeyRegisteredBarrel, Guid.Empty);
            }
            set
            {
                var registeredBarrelGuid = value;

                // check if barrel guid exists
                if (registeredBarrelGuid == Guid.Empty)
                {
                    if (Attributes.UserDictionary.ContainsKey(AttributeKeys.KeyRegisteredBarrel))
                    {
                        Attributes.UserDictionary.Remove(AttributeKeys.KeyRegisteredBarrel);
                    }
                    return;
                }

                UserDictionaryUtilities.ReplaceContentDictionaryUnsafe(this, AttributeKeys.KeyRegisteredBarrel, registeredBarrelGuid);
            }
        }

        public CMFImplantDirector Director
        {
            get;
            set;
        }

        public Point3d TipPoint
        {
            get;
            protected set;
        }
        public Point3d HeadPoint
        {
            get;
            protected set;
        }

        public bool IsCalibrated
        {
            get
            {
                var objectManager = new CMFObjectManager(Director);
                var implantCaseComponent = new ImplantCaseComponent();

                var casePreference = objectManager.GetCasePreference(this);
                var implantSupport = implantCaseComponent.GetImplantBuildingBlock(IBB.ImplantSupport, casePreference);

                return objectManager.HasBuildingBlock(implantSupport);
            }
        }
        protected Vector3d ScrewVector => TipPoint - HeadPoint;

        //Head to Tip
        public Vector3d Direction
        {
            get
            {
                var screwVector = ScrewVector;
                screwVector.Unitize();
                return screwVector;
            }
        }

        public Point3d BodyOrigin
        {
            get
            {
                var screwFactory = new ScrewBrepFactory(ScrewAideDictionary[Constants.ScrewAide.Head] as Brep);
                var headHeight = screwFactory.GetHeadHeight();
                return HeadPoint + Direction * headHeight;
            }
        }

        public double Length => (HeadPoint - TipPoint).Length;

        //Only works if it is already in the document!
        public bool IsThisTypeOfScrew(IBB ibbType)
        {
            var nameBlocks = BuildingBlocks.Blocks[ibbType].Name.Split('_');
            return Name.Contains(nameBlocks[0]);
        }

        public string GetPositionOnPlannedBone()
        {
            var objectManager = new CMFObjectManager(Director);
            var constraintMeshQuery = new ConstraintMeshQuery(objectManager);
            var plannedBones = constraintMeshQuery.GetConstraintRhinoObjectForImplant().ToList();
            var lowLoDMeshes = new List<Mesh>();
            plannedBones.ToList().ForEach(x =>
            {
                Mesh lowLoD;
                objectManager.GetBuildingBlockLoDLow(x.Id, out lowLoD);
                if (lowLoD == null)
                {
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "Level of Detail - Low failed to obtained, a full detailed mesh is used instead.");
                    lowLoDMeshes.Add(((Mesh)x.Geometry).DuplicateMesh());
                }
                else
                {
                    lowLoDMeshes.Add(lowLoD.DuplicateMesh());
                }

            });

            var plannedMesh = ScrewUtilities.FindIntersection(plannedBones, lowLoDMeshes, this);
            
            lowLoDMeshes.ForEach(x => x.Dispose());

            if (plannedMesh == null)
            {
                return string.Empty;
            }
            
            return BoneNamePreferencesManager.Instance.GetPreferenceBoneName(Director, plannedMesh);
        }

        //Only works if it is already in the document!
        public IBB GetTypeOfScrew()
        {
            var screwPreFix =  BuildingBlocks.Blocks[IBB.Screw].Name.Split('_')[0];
            var guideScrewPreFix = BuildingBlocks.Blocks[IBB.GuideFixationScrew].Name.Split('_')[0];

            if (Name.Contains(screwPreFix))
            {
                return IBB.Screw;
            }

            if (Name.Contains(guideScrewPreFix))
            {
                return IBB.GuideFixationScrew;
            }

            throw new Exception("Screw Type is Invalid!");
        }

        private string GenerateScrewSharingKey(Screw screw)
        {
            return $"screw_sharing_{screw.Id.ToString()}";
        }

        private Guid ExtractScrewIdFromScrewSharingKey(string key)
        {
            var guidString = key.Replace("screw_sharing_", "");
            return new Guid(guidString);
        }

        public void ShareWithScrews(IEnumerable<Screw> screws)
        {
            foreach (var screw in screws)
            {
                ShareWithScrew(screw);
            }
        }

        //[AH] Can optimize to exclude own self, but need to be careful of the dependencies with other screws
        public void ShareWithScrew(Screw screw)
        {
            if (this.Id == Guid.Empty)
            {
                throw new IDSException("[DEV] The screw must be added into the document first before sharing/referencing with other screws!");
            }

            var key = GenerateScrewSharingKey(screw);
            if (Attributes.UserDictionary.ContainsKey(key))
            {
                return;
            }

            Attributes.UserDictionary.Set(key, screw.Id);
        }

        public void UnshareFromScrews(IEnumerable<Screw> screws)
        {
            foreach (var screw in screws)
            {
                UnshareFromScrew(screw);
            }
        }

        public void UnshareFromScrew(Screw screw)
        {
            var key = GenerateScrewSharingKey(screw);
            if (!Attributes.UserDictionary.ContainsKey(key))
            {
                return;
            }

            Attributes.UserDictionary.Remove(key);
        }

        public List<Screw> GetScrewItSharedWith()
        {
            var res = new List<Screw>();

            foreach (var keyValuePair in Attributes.UserDictionary)
            {
                if (keyValuePair.Key.Contains("screw_sharing_"))
                {
                    var screwGuid = ExtractScrewIdFromScrewSharingKey(keyValuePair.Key);

                    var screw = (Screw)Director.Document.Objects.Find(screwGuid);
                    res.Add(screw);
                }
            }

            return res;
        }
        
        //This Screw MUST already be in the DOCUMENT if GuidePreferenceDataModel is not explicitly set! refGPref is where this screw belongs to.
        public List<KeyValuePair<GuidePreferenceDataModel, Screw>> GetGuideAndScrewItSharedWith(GuidePreferenceDataModel refGPref = null)
        {
            var res = new List<KeyValuePair<GuidePreferenceDataModel, Screw>>();
            var objManager = new CMFObjectManager(Director);

            var ScrewsItSharedWith = GetScrewItSharedWith();
            ScrewsItSharedWith.ForEach(x =>
            {
                var cp = objManager.GetGuidePreference(x);
                res.Add(new KeyValuePair<GuidePreferenceDataModel, Screw>(cp, x));
            });

            return res;
        }

        /// <summary>
        /// The screw aides
        /// </summary>
        public Dictionary<IBB, Guid> ScrewImplantAidesInDocument { get; set; } = new Dictionary<IBB, Guid>();
        public Dictionary<IBB, Guid> ScrewGuideAidesInDocument { get; set; } = new Dictionary<IBB, Guid>();

        private Dictionary<string, GeometryBase> _screwAideDictionary;
        public Dictionary<string, GeometryBase> ScrewAideDictionary
        {
            get
            {
                if (_screwAideDictionary == null || _screwAideDictionary.ContainsValue(null))
                {
                    var objectManager = new CMFObjectManager(Director);

                    if (IsThisTypeOfScrew(IBB.GuideFixationScrew))
                    {
                        var guidePreferenceData = objectManager.GetGuidePreference(this);
                        _screwAideDictionary = guidePreferenceData.GuideScrewAideData.GenerateScrewAideDictionary();
                    }
                    else if(IsThisTypeOfScrew(IBB.Screw))
                    {
                        var casePreferenceData = objectManager.GetCasePreference(this);
                        _screwAideDictionary = casePreferenceData.ScrewAideData.GenerateScrewAideDictionary();
                    }
                    else
                    {
                        throw new IDSException("Type of screw is not valid!");
                    }
                }

                return _screwAideDictionary;
            }
            set { _screwAideDictionary = value; }
        }
    
        public int Index
        {
            get;
            set;
        }


        public Transform AlignmentTransform
        {
            get
            {
                var rotation = Transform.Rotation(-Plane.WorldXY.ZAxis, Direction, Plane.WorldXY.Origin);
                var translation = Transform.Translation(HeadPoint - Plane.WorldXY.Origin);
                return Transform.Multiply(translation, rotation);
            }
        }

        public Screw() : base()
        {
        }

        protected Screw(Brep brep) : base(brep)
        { }

        // constructor for guide screw
        public Screw(CMFImplantDirector director, Point3d headPoint, Point3d tipPoint, Dictionary<string, GeometryBase> screwAideDict, int newIndex, string screwType)
            : base(new ScrewBrepFactory(screwAideDict[Constants.ScrewAide.Head] as Brep).CreateScrewBrep(headPoint, tipPoint))
        {
            HeadPoint = headPoint;
            TipPoint = tipPoint;
            Director = director;
            Index = newIndex;
            ScrewType = screwType;
            
            ScrewAideDictionary = new Dictionary<string, GeometryBase>()
            {
                { Constants.ScrewAide.Head, screwAideDict[Constants.ScrewAide.Head]},
                { Constants.ScrewAide.HeadRef, screwAideDict[Constants.ScrewAide.HeadRef]},
                { Constants.ScrewAide.Eye, screwAideDict[Constants.ScrewAide.Eye]},
                { Constants.ScrewAide.Container, screwAideDict[Constants.ScrewAide.Container]},
                { Constants.ScrewAide.Stamp, screwAideDict[Constants.ScrewAide.Stamp]},
                { Constants.ScrewAide.EyeShape, screwAideDict[Constants.ScrewAide.EyeShape]},
                { Constants.ScrewAide.EyeSubtractor, screwAideDict[Constants.ScrewAide.EyeSubtractor]},
                { Constants.ScrewAide.EyeLabelTag, screwAideDict[Constants.ScrewAide.EyeLabelTag]},
                { Constants.ScrewAide.EyeLabelTagShape, screwAideDict[Constants.ScrewAide.EyeLabelTagShape]},
                { Constants.ScrewAide.EyeRef, screwAideDict[Constants.ScrewAide.EyeRef]},
                { Constants.ScrewAide.EyeLabelTagRef, screwAideDict[Constants.ScrewAide.EyeLabelTagRef]},
            };
        }

        //screwAide dictionary key string same name as steps file {Head, Container, Eye, Barrel, Stamp, Headref}
        public Screw(CMFImplantDirector director, Point3d headPoint, Point3d tipPoint, Dictionary<string, GeometryBase> screwAideDict, int newIndex, string screwType, string barrelType)
                      : this(director, headPoint, tipPoint, screwAideDict, newIndex, screwType)
        {
            BarrelType = barrelType;
        }

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

        //Must be called when screw is already in the document
        public void UpdateAidesInDocument()
        {
            // Disable undo recording so that Ctrl-Z does not restore the screw aides Screw aide
            // creation is controlled by OnAddToDocument (which is also triggered when Ctrl-Z is pressed)
            if (Director != null && Director.Document.UndoRecordingEnabled)
            {
                Director.Document.UndoRecordingEnabled = false;
            }

            var objectManager = new CMFObjectManager(Director);

            if (IsThisTypeOfScrew(IBB.GuideFixationScrew))
            {
                var guideCaseComponent = new GuideCaseComponent();

                var guidePreferenceData = objectManager.GetGuidePreference(this);
                var guideFixationScrewEyeIbb = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrewEye, guidePreferenceData);

                if (ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewEye))
                {
                    ScrewGuideAidesInDocument[IBB.GuideFixationScrewEye] =
                        objectManager.SetBuildingBlock(guideFixationScrewEyeIbb, GetScrewEye(), ScrewGuideAidesInDocument[IBB.GuideFixationScrewEye]);
                }
                else 
                {
                    //guide fixation screw must have an eye (either a default eye or an eye with label tag)
                    var labelTagHelper = new ScrewLabelTagHelper(Director);
                    if (!labelTagHelper.HandleLabelTagUpdate(this))
                    {
                        ScrewGuideAidesInDocument[IBB.GuideFixationScrewEye] = objectManager.AddNewBuildingBlock(guideFixationScrewEyeIbb, GetScrewEye());
                    }
                }
            }
            else if (IsThisTypeOfScrew(IBB.Screw))
            {

            }
            else
            {
                if (Director != null && !Director.Document.UndoRecordingEnabled)
                {
                    Director.Document.UndoRecordingEnabled = true;
                }

                throw new IDSException("Type of screw is not valid!");
            }

            // Restart recording actions for Ctrl-Z
            if (Director != null && !Director.Document.UndoRecordingEnabled)
            {
                Director.Document.UndoRecordingEnabled = true;
            }
        }

        protected override void OnAddToDocument(RhinoDoc doc)
        {
            base.OnAddToDocument(doc);

            ScrewAideDocumentOperation.OnAddToDocument(doc, this);

            //When a screw is first created (before adding to document), it's Name is empty.
            //This block of codes will be valid when undoing a deleted screw (by re-adding the deleted screw back to the document)
            //The deleted screw will have value to it's Name
            if (Director != null && !string.IsNullOrEmpty(Name))
            {
                if (IsThisTypeOfScrew(IBB.GuideFixationScrew))
                {
                    _screwAideDictionary = null;
                    UpdateAidesInDocument();

                    if (AllGuideFixationScrewGaugesProxy.Instance.IsEnabled)
                    {
                        AllGuideFixationScrewGaugesProxy.Instance.Invalidate();
                    }
                }
                else if (IsThisTypeOfScrew(IBB.Screw) && (AllScrewGaugesProxy.Instance.IsEnabled))
                {
                    AllScrewGaugesProxy.Instance.Invalidate();
                }
            }
        }

        protected override void OnDeleteFromDocument(RhinoDoc doc)
        {
            base.OnDeleteFromDocument(doc);

            ScrewAideDocumentOperation.OnDeleteFromDocument(doc, this);

            HandleAidesRemoval(ScrewImplantAidesInDocument);
            HandleAidesRemoval(ScrewGuideAidesInDocument);
        }

        private void HandleAidesRemoval(Dictionary<IBB, Guid> aidesDictionary)
        {
            // Disable undo recording so that Ctrl-Z does not restore the screw aides Screw aide
            // creation is controlled by OnAddToDocument (which is also triggered when Ctrl-Z is pressed)
            if (Director != null)
            {
                Director.Document.UndoRecordingEnabled = false;
            }

            var objectManager = new CMFObjectManager(Director);

            foreach (var id in aidesDictionary.Values)
            {
                objectManager.DeleteObject(id);
            }

            // Empty the screw aide dictionary of the screw
            aidesDictionary.Clear();

            if (Director != null)
            {
                Director.Document.UndoRecordingEnabled = true;
            }
        }

        //TODO: Should be in a factory or Creator
        public Brep GetScrewContainer()
        {
            var screwContainer = new Brep();
            screwContainer.Append(ScrewAideDictionary[Constants.ScrewAide.Container] as Brep);
            screwContainer.Transform(AlignmentTransform);
            return screwContainer;
        }
        public Brep GetScrewHeadAtOrigin()
        {
            var screwHead = new Brep();
            screwHead.Append(ScrewAideDictionary[Constants.ScrewAide.Head] as Brep);
            return screwHead;
        }

        public Brep GetScrewEye()
        {
            return GetScrewComponentInAlignment(Constants.ScrewAide.Eye);
        }

        public Brep GetScrewEyeShape()
        {
            return GetScrewComponentInAlignment(Constants.ScrewAide.EyeShape);
        }

        public Brep GetScrewLabelTagWithDefaultOrientation()
        {
            return GetScrewComponentInAlignment(Constants.ScrewAide.EyeLabelTag);
        }

        public Brep GetScrewLabelTagShapeAtOrigin()
        {
            var component = new Brep();
            component.Append(ScrewAideDictionary[Constants.ScrewAide.EyeLabelTagShape] as Brep);
            return component;
        }

        public Brep GetScrewLabelTagShapeInlabelTagAlignment()
        {
            var screwLabelTagHelper = new ScrewLabelTagHelper(Director);
            var transform = screwLabelTagHelper.GetLabelTagTransformFromDefaultOrientationOnScrew(this);

            var tag = GetScrewComponentInAlignment(Constants.ScrewAide.EyeLabelTagShape);
            tag.Transform(transform);

            return tag;
        }

        private Brep GetScrewComponentInAlignment(string componentName)
        {
            var component = new Brep();
            component.Append(ScrewAideDictionary[componentName] as Brep);
            component.Transform(AlignmentTransform);
            return component;
        }

        public Brep GetScrewStamp()
        {
            var screwStamp = new Brep();
            screwStamp.Append(ScrewAideDictionary[Constants.ScrewAide.Stamp] as Brep);
            screwStamp.Transform(AlignmentTransform);
            return screwStamp;
        }

        public Curve GetScrewHeadRef()
        {
            var screwHeadRefOri = ScrewAideDictionary[Constants.ScrewAide.HeadRef] as Curve;
            var screwHeadRef = screwHeadRefOri.DuplicateCurve();
            screwHeadRef.Transform(AlignmentTransform);
            return screwHeadRef;
        }

        public Brep GetScrewEyeRef()
        {
            var screwEyeRef = new Brep();
            screwEyeRef.Append(ScrewAideDictionary[Constants.ScrewAide.EyeRef] as Brep);
            screwEyeRef.Transform(AlignmentTransform);
            return screwEyeRef;
        }

        public void HandleAddGuideAides(IBB buildingBlock, Guid id)
        {
            var objectManager = new CMFObjectManager(Director);

            if (ScrewGuideAidesInDocument.ContainsKey(buildingBlock) && ScrewGuideAidesInDocument[buildingBlock] != id)
            {
                objectManager.DeleteObject(ScrewGuideAidesInDocument[buildingBlock]);
            }

            ScrewGuideAidesInDocument[buildingBlock] = id;
        }

        public void DeleteGuideAides(IBB buildingBlock)
        {
            var objectManager = new CMFObjectManager(Director);

            objectManager.DeleteObject(ScrewGuideAidesInDocument[buildingBlock]);
            ScrewGuideAidesInDocument.Remove(buildingBlock);
        }

        public void PrepareForArchiving()
        {
            var userDict = Attributes.UserDictionary;
            userDict.Set(KeyHeadPoint, HeadPoint);
            userDict.Set(KeyTipPoint, TipPoint);
            userDict.Set(KeyIndex, Index);

            userDict.Set(KeyScrewType, ScrewType);

            if (ScrewImplantAidesInDocument != null)
            {
                foreach (IBB key in ScrewImplantAidesInDocument.Keys)
                {
                    userDict.Set(key.ToString(), ScrewImplantAidesInDocument[key]);
                }
            }
            
            userDict.Remove(IBB.GuideFixationScrewEye.ToString());
            userDict.Remove(IBB.GuideFixationScrewLabelTag.ToString());
            userDict.Remove(AttributeKeys.KeyRegisteredBarrel);

            if (ScrewGuideAidesInDocument != null)
            {
                foreach (IBB key in ScrewGuideAidesInDocument.Keys)
                {
                    userDict.Set(key.ToString(), ScrewGuideAidesInDocument[key]);
                }
            }

            CommitChanges();
        }
       
        public void DeArchive(ArchivableDictionary userDict)
        {
            // Load position parameters
            HeadPoint = userDict.GetPoint3d(KeyHeadPoint, Point3d.Unset);
            TipPoint = userDict.GetPoint3d(KeyTipPoint, Point3d.Unset);
            Index = userDict.GetInteger(KeyIndex, 0);
            ScrewType = userDict.GetString(KeyScrewType,string.Empty);

            #region Backward_Compatibility

            ScrewType = BackwardCompatibilityUtilities.RenameScrewTypeFrom_Before_C3_dot_0(ScrewType);

            #endregion

            if (ScrewType == string.Empty)
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, $"ScrewType property for screw {Name} is missing!");
            }

            var guideCaseComponent = new GuideCaseComponent();
            var guideComponents = guideCaseComponent.GetGuideComponents().ToList();

            foreach (IBB key in Enum.GetValues(typeof(IBB)))
            {
                var screwAideID = userDict.GetGuid(key.ToString(), Guid.Empty);
                if (screwAideID != Guid.Empty)
                {
                    if (guideComponents.Contains(key) || key.ToString() == AttributeKeys.KeyRegisteredBarrel)
                    {
                        ScrewGuideAidesInDocument.Add(key, screwAideID);
                        continue;
                    }

                    ScrewImplantAidesInDocument.Add(key, screwAideID);
                }
            }
        }

        private void InvalidateAidesReferencesInDocument(Dictionary<IBB, Guid> aidesDictionary)
        {
            var missingIbb = new List<IBB>();
            foreach (var keyValuePair in aidesDictionary)
            {
                var found = Director.Document.Objects.Find(keyValuePair.Value);
                if (found == null)
                {
                    missingIbb.Add(keyValuePair.Key);
                }
            }

            foreach (var ibb in missingIbb)
            {
                aidesDictionary.Remove(ibb);
            }
        }

        public void InvalidateImplantScrewAidesReferencesInDocument()
        {
            InvalidateAidesReferencesInDocument(ScrewImplantAidesInDocument);
        }

        public void InvalidateGuideScrewAidesReferencesInDocument()
        {
            InvalidateAidesReferencesInDocument(ScrewGuideAidesInDocument);
        }

        public bool Delete()
        {
            var objectManager = new CMFObjectManager(Director);
            objectManager.DeleteObject(this.Id);

            return true;
        }
    }
}
