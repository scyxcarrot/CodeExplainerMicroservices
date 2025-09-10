using IDS.CMF.GuideBuildingBlocks;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Operations;
using IDS.Core.PluginHelper;
using Rhino.Geometry;
using System;
using System.Linq;

namespace IDS.CMF.Utilities
{
    public class ScrewLabelTagHelper
    {
        //Screw label tag is positioned at Plane.WorldXY and the protruded tag is positioned towards Vector3d.YAxis direction
        //It is not centered at Point3d.Origin because it is aligned based on the screw head point.
        //So, rotation should be along -Vector3d.ZAxis (which represents the screw direction at origin)
        public static readonly Vector3d DefaultLabelTagDirection = Vector3d.YAxis;
        public static readonly Vector3d DefaultScrewDirection = -Vector3d.ZAxis;

        private const string KeyScrewLabelTagOrientation = "ScrewLabelTagOrientation";
        
        private readonly CMFObjectManager _objectManager;

        public Transform LabelTagRotation { get; set; } = Transform.Unset;

        public ScrewLabelTagHelper(CMFImplantDirector director)
        {
            _objectManager = new CMFObjectManager(director);
        }

        public Tuple<Screw, Brep> CreateScrewLabelTagForPyTestApi(Screw screw, double labelTagAngle, Mesh calibrationMesh)
        {
            var newCalibratedScrew = CalibrateScrewBeforeAddLabelTag(screw, labelTagAngle, calibrationMesh);
            var labelTag = CreateScrewLabelTag(screw, labelTagAngle, out _);
            return new Tuple<Screw, Brep>(newCalibratedScrew, labelTag);
        }

        private Screw CalibrateScrewBeforeAddLabelTag(Screw screw, double labelTagAngle, Mesh calibrationMesh)
        {
            var tmpScrew = new Screw(screw.Director,
                screw.HeadPoint,
                screw.TipPoint,
                screw.ScrewAideDictionary, screw.Index, screw.ScrewType, screw.BarrelType);

            var calibrator = new GuideFixationScrewCalibrator();
            return calibrator.LevelScrewWithLabelTag(tmpScrew, calibrationMesh, labelTagAngle);
        }

        public Guid DoAddLabelTagToScrew(Screw screw, double labelTagAngle, Mesh calibrationMesh)
        {
            var newCalibratedScrew = CalibrateScrewBeforeAddLabelTag(screw, labelTagAngle, calibrationMesh);

            var screwManager = new ScrewManager(screw.Director);
            screwManager.UpdateGuideFixationScrewInDocument(newCalibratedScrew, ref screw);

            if (newCalibratedScrew.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewEye))
            {
                _objectManager.DeleteObject(newCalibratedScrew.ScrewGuideAidesInDocument[IBB.GuideFixationScrewEye]);
            }

            var id = AddLabelTagToScrew(newCalibratedScrew, labelTagAngle);
            newCalibratedScrew.ScrewGuideAidesInDocument.Remove(IBB.GuideFixationScrewEye);
            newCalibratedScrew.ScrewGuideAidesInDocument.Add(IBB.GuideFixationScrewLabelTag, id);
            newCalibratedScrew.Attributes.UserDictionary.Set(KeyScrewLabelTagOrientation, labelTagAngle);

            return id;
        }

        private Brep CreateScrewLabelTag(Screw screw, double labelTagAngle, out Transform labelTagRotation)
        {
            labelTagRotation = GetLabelTagRotation(screw, labelTagAngle);
            var labelTag = screw.GetScrewLabelTagWithDefaultOrientation();
            labelTag.Transform(labelTagRotation);
            return labelTag;
        }

        private Guid AddLabelTagToScrew(Screw screw, double labelTagAngle)
        {
            if (!screw.IsThisTypeOfScrew(IBB.GuideFixationScrew))
            {
                throw new IDSException("Only Guide Fixation Screw can have label tag!");
            }

            var labelTag = CreateScrewLabelTag(screw, labelTagAngle, out var labelTagRotation);
            LabelTagRotation = labelTagRotation;

            var guid = AddLabelTagToDocument(screw, labelTag);
            if (screw.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewEye))
            {
                _objectManager.DeleteObject(screw.ScrewGuideAidesInDocument[IBB.GuideFixationScrewEye]);
            }

            return guid;
        }

        public void DoDeleteLabelTag(Screw screw, Mesh lowLoDCalibrationMesh)
        {
            DeleteLabelTag(screw);

            var tmpScrew = new Screw(screw.Director,
                screw.HeadPoint,
                screw.TipPoint,
                screw.ScrewAideDictionary, screw.Index, screw.ScrewType, screw.BarrelType);

            var calibrator = new GuideFixationScrewCalibrator();
            var newCalibratedScrew = calibrator.LevelScrew(tmpScrew, lowLoDCalibrationMesh, screw);

            var screwManager = new ScrewManager(screw.Director);
            screwManager.UpdateGuideFixationScrewInDocument(newCalibratedScrew, ref screw);
        }

        private void DeleteLabelTag(Screw screw)
        {
            if (!screw.IsThisTypeOfScrew(IBB.GuideFixationScrew))
            {
                throw new IDSException("Only Guide Fixation Screw can have label tag!");
            }

            if (screw.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewLabelTag))
            {
                _objectManager.DeleteObject(screw.ScrewGuideAidesInDocument[IBB.GuideFixationScrewLabelTag]);
            }

            var guideCaseComponent = new GuideCaseComponent();
            var caseData = _objectManager.GetGuidePreference(screw);
            var guideFixationScrewEyeIbb = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrewEye, caseData);
            var guid = _objectManager.AddNewBuildingBlock(guideFixationScrewEyeIbb, screw.GetScrewEye());
            screw.ScrewGuideAidesInDocument[IBB.GuideFixationScrewEye] = guid;

            screw.ScrewGuideAidesInDocument.Remove(IBB.GuideFixationScrewLabelTag);
            screw.Attributes.UserDictionary.Remove(KeyScrewLabelTagOrientation);
        }

        public Screw GetScrewOfLabelTag(Guid labelTagId)
        {
            var screws = _objectManager.GetAllBuildingBlocks(IBB.GuideFixationScrew).Select(o => (Screw) o);
            foreach (var screw in screws)
            {
                if (screw.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewLabelTag) &&
                    screw.ScrewGuideAidesInDocument[IBB.GuideFixationScrewLabelTag] == labelTagId)
                {
                    return screw;
                }
            }

            return null;
        }

        public bool HandleLabelTagUpdate(Screw screw)
        {
            if (screw.Attributes.UserDictionary.ContainsKey(KeyScrewLabelTagOrientation))
            {
                var transform = GetLabelTagTransformFromDefaultOrientationOnScrew(screw);
                var labelTag = screw.GetScrewLabelTagWithDefaultOrientation();
                labelTag.Transform(transform);
                
                if (screw.ScrewGuideAidesInDocument.ContainsKey(IBB.GuideFixationScrewLabelTag))
                {
                    screw.ScrewGuideAidesInDocument[IBB.GuideFixationScrewLabelTag] = _objectManager.SetBuildingBlock(GetLabelTagBuildingBlock(screw), labelTag, screw.ScrewGuideAidesInDocument[IBB.GuideFixationScrewLabelTag]);
                }
                else
                {
                    screw.ScrewGuideAidesInDocument[IBB.GuideFixationScrewLabelTag] = AddLabelTagToDocument(screw, labelTag);
                }

                return true;
            }

            return false;
        }

        public Transform GetLabelTagTransformFromDefaultOrientationOnScrew(Screw screw)
        {
            var orientation = GetLabelTagAngle(screw);
            return GetLabelTagRotation(screw, orientation);
        }

        public void SetNewScrewLabelTagFromOldScrew(Screw oldScrew, Screw newScrew)
        {
            var orientation = GetLabelTagAngle(oldScrew);
            if (!double.IsNaN(orientation))
            {
                newScrew.Attributes.UserDictionary.Set(KeyScrewLabelTagOrientation, orientation);
            }
        }

        public double GetLabelTagAngle(Screw screw)
        {
            if (screw.Attributes.UserDictionary.ContainsKey(KeyScrewLabelTagOrientation))
            {
                return screw.Attributes.UserDictionary.GetDouble(KeyScrewLabelTagOrientation);
            }

            return double.NaN;
        }

        public Brep GetLabelTagRef(Screw screw, double labelTagAngle)
        {
            var labelTagRef = new Brep();
            labelTagRef.Append(screw.ScrewAideDictionary[Constants.ScrewAide.EyeLabelTagRef] as Brep);
            labelTagRef.Transform(screw.AlignmentTransform);
            labelTagRef.Transform(GetLabelTagRotation(screw, labelTagAngle));
            return labelTagRef;
        }

        private Transform GetLabelTagRotation(Screw screw, double labelTagAngle)
        {
            var direction = DefaultScrewDirection;
            direction.Transform(screw.AlignmentTransform);
            var transform = Transform.Rotation(labelTagAngle, direction, screw.HeadPoint);
            return transform;
        }

        private Guid AddLabelTagToDocument(Screw screw, Brep labelTag)
        {
            var buildingBlock = GetLabelTagBuildingBlock(screw);
            return _objectManager.AddNewBuildingBlock(buildingBlock, labelTag);
        }

        private ExtendedImplantBuildingBlock GetLabelTagBuildingBlock(Screw screw)
        {
            var guideCaseComponent = new GuideCaseComponent();
            var caseData = _objectManager.GetGuidePreference(screw);
            var buildingBlock = guideCaseComponent.GetGuideBuildingBlock(IBB.GuideFixationScrewLabelTag, caseData);
            return buildingBlock;
        }
    }
}