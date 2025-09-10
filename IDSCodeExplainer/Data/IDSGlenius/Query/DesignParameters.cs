using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Geometry;
using System;

namespace IDS.Glenius.Query
{
    public class DesignParameters
    {
        private GleniusImplantDirector director;
        private GleniusObjectManager objectManager;
        private HeadAlignment headAlignment;
        private HeadPreopMeasurements headPreopMeasurements;

        public DesignParameters(GleniusImplantDirector director)
        {
            this.director = director;
            objectManager = new GleniusObjectManager(director);
            headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document, director.defectIsLeft);
            if (director.PreopCor != null && objectManager.HasBuildingBlock(IBB.Head))
            {
                var head = objectManager.GetBuildingBlock(IBB.Head) as Head;
                headPreopMeasurements = new HeadPreopMeasurements(director.AnatomyMeasurements, head.CoordinateSystem.Origin, director.PreopCor.CenterPoint);
            }
        }

        public string GetIDSVersion()
        {
            return IDSPluginHelper.PluginVersion; 
        }

        public string GetTimeStamp()
        {
            return string.Format("{0:yyyy/MM/dd H:mm:ss}", DateTime.Now);
        }

        public string GetCaseID()
        {
            return director.caseId;
        }

        public string GetVersion()
        {
            return director.version.ToString();
        }

        public string GetDraft()
        {
            return director.draft.ToString();
        }

        public string GetDefectSide()
        {
            return director.defectIsLeft ? "left" : "right";
        }

        #region MCS

        public Point3d GetMCSAxialPlaneCenter()
        {
            return director.AnatomyMeasurements.PlAxial.Origin;
        }

        public Point3d GetMCSCoronalPlaneCenter()
        {
            return director.AnatomyMeasurements.PlCoronal.Origin;
        }

        public Point3d GetMCSSagittalPlaneCenter()
        {
            return director.AnatomyMeasurements.PlSagittal.Origin;
        }

        public Vector3d GetMCSAxialPlaneNormal()
        {
            return director.AnatomyMeasurements.PlAxial.Normal;
        }

        public Vector3d GetMCSCoronalPlaneNormal()
        {
            return director.AnatomyMeasurements.PlCoronal.Normal;
        }

        public Vector3d GetMCSSagittalPlaneNormal()
        {
            return director.AnatomyMeasurements.PlSagittal.Normal;
        }

        public Vector3d GetMCSAPAxis()
        {
            return director.AnatomyMeasurements.AxAp;
        }

        #endregion  

        #region HeadParameters

        public double GetHeadDiameter()
        {
            var head = objectManager.GetBuildingBlock(IBB.Head) as Head;
            return HeadQueries.GetHeadDiameter(head.HeadType);
        }

        public double GetHeadLinerDiameter()
        {
            return GetHeadDiameter();
        }

        public double GetHeadVersion()
        {
            return Math.Round(headAlignment.GetVersionAngle(), 1);
        }

        public double GetHeadInclination()
        {
            return Math.Round(headAlignment.GetInclinationAngle(), 1);
        }

        public double GetHeadAnteroPosteriorDistance()
        {
            return Round(headAlignment.GetAnteriorPosteriorPosition());
        }

        public string GetHeadAnteroPosteriorDescription()
        {
            if (GetHeadAnteroPosteriorDistance() > 0)
            {
                return "Posteriorisation";
            }

            if (GetHeadAnteroPosteriorDistance() < 0)
            {
                return "Anteriorisation";
            }

            return "N/A";
        }

        public double GetLateroMedialDistance()
        {
            return Round(headAlignment.GetMedialLateralPosition());
        }

        public string GetLateroMedialDescription()
        {
            if (GetLateroMedialDistance() > 0)
            {
                return "Lateralisation";
            }

            if (GetLateroMedialDistance() < 0)
            {
                return "Medialisation";
            }

            return "N/A";
        }

        //Returns with 1 decimal point rounding
        public double GetSuperiorInferiorDistance()
        {
            return Round(headAlignment.GetInferiorSuperiorPosition());
        }

        public string GetSuperiorInferiorDescription()
        {
            if (GetSuperiorInferiorDistance() > 0)
            {
                return "Superiorisation";
            }

            if (GetSuperiorInferiorDistance() < 0)
            {
                return "Inferiorisation";
            }

            return "N/A";
        }

        public string GetHeadPreopAnteroPosteriorDistanceInString()
        {
            return GetHeadPreopDistanceInString(GetHeadPreopAnteroPosteriorDistance());
        }

        public string GetHeadPreopAnteroPosteriorDescription()
        {
            var distance = GetHeadPreopAnteroPosteriorDistance();

            if (distance > 0)
            {
                return "Posteriorisation";
            }

            return distance < 0 ? "Anteriorisation" : "N/A";
        }

        private double GetHeadPreopAnteroPosteriorDistance()
        {
            return headPreopMeasurements != null ? Round(headPreopMeasurements.GetAnteriorPosteriorPosition()) : double.NaN;
        }
        
        public string GetHeadPreopLateroMedialDistanceInString()
        {
            return GetHeadPreopDistanceInString(GetHeadPreopLateroMedialDistance());
        }

        public string GetHeadPreopLateroMedialDescription()
        {
            var distance = GetHeadPreopLateroMedialDistance();

            if (distance > 0)
            {
                return "Lateralisation";
            }

            return distance < 0 ? "Medialisation" : "N/A";
        }

        private double GetHeadPreopLateroMedialDistance()
        {
            return headPreopMeasurements != null ? Round(headPreopMeasurements.GetMedialLateralPosition()) : double.NaN;
        }

        public string GetHeadPreopSuperiorInferiorDistanceInString()
        {
            return GetHeadPreopDistanceInString(GetHeadPreopSuperiorInferiorDistance());
        }

        public string GetHeadPreopSuperiorInferiorDescription()
        {
            var distance = GetHeadPreopSuperiorInferiorDistance();

            if (distance > 0)
            {
                return "Superiorisation";
            }

            return distance < 0 ? "Inferiorisation" : "N/A";
        }

        private double GetHeadPreopSuperiorInferiorDistance()
        {
            return headPreopMeasurements != null ? Round(headPreopMeasurements.GetInferiorSuperiorPosition()) : double.NaN;
        }
        
        //Example, distance = 0.5 but in memory is 0.4999, it will round to 0.5, and later round to 1 during stringification
        private static string GetHeadPreopDistanceInString(double distance)
        {
            return double.IsNaN(distance) ? "N/A" : StringUtilities.DoubleStringify(Math.Abs(Round(distance)), 0);
        }

        private static double Round(double value)
        {
            return Math.Round(value, 1, MidpointRounding.AwayFromZero);
        }

        #endregion

        #region RBV 

        public double GetRBVHeadVolumeCC()
        {
            var provider = new QCScaffoldInfoProvider(director);
            return provider.GetHeadRBVVolumeInCC();
        }

        public double GetRBVImplantVolumeCC()
        {
            var provider = new QCScaffoldInfoProvider(director);
            return provider.GetScaffoldRBVVolumeInCC();
        }

        public double GetTotalRBVVolumeCC()
        {
            var provider = new QCScaffoldInfoProvider(director);
            return provider.GetTotalRBVVolumeInCC();
        }

        #endregion

        #region Screw

        public struct ScrewInfo
        {
            public int index;
            public double diameter;
            public string screwLockingType;
            public Point3d headPoint;
            public Point3d tipPoint;
            public double length;
            public double inBone;
            public double untilBone;
            public string fixation;
            public double offset;
            public string brand;
            public string referenceNumber;
        }

        public ScrewInfo GetScrewInfo(Screw screw)
        {
            var provider = new ScrewInfoProvider();

            var info = new ScrewInfo
            {
                index = screw.Index,
                screwLockingType = provider.GetScrewLockingType(screw),
                diameter = Math.Round(screw.Diameter,1),
                headPoint = screw.HeadPoint,
                tipPoint = screw.TipPoint,
                length = Math.Round(screw.TotalLength,4),
                inBone = Math.Round(screw.GetDistanceInBone(),4),
                untilBone = Math.Round(screw.GetDistanceUntilBone(),4),
                fixation = screw.IsBicortical ? "BI" : "UNI",
                offset = Math.Round(screw.GetOffsetFromIdealPlacementPlane(),4),
                brand = provider.GetScrewBrand(screw),
                referenceNumber = provider.GetScrewReferenceNumber(screw)
            };

            return info;
        }


        #endregion
    }
}
