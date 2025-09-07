using IDS.CMF.FileSystem;
using IDS.CMF.V2.Preferences;
using IDS.Core.Utilities;
using System.IO;
using System.Xml;

namespace IDS.CMF.Preferences
{
    public static class CMFPreferences
    {
        private static readonly XmlDocument PreferencesDoc;

        static CMFPreferences()
        {
            var resources = new CMFResources();
            PreferencesDoc = new XmlDocument();
            PreferencesDoc.Load(resources.CMFPreferenceXmlPath);
        }

        //Keys 
        private const string KeyWrapOperationSmallestDetail = "WrapOperation_SmallestDetail";
        private const string KeyWrapOperationGapClosingDistance = "WrapOperation_GapClosingDistance";
        private const string KeyWrapOperationOffset = "WrapOperation_Offset";
        private const string KeyWrapOperationOffsetInDistanceRatio = "WrapOperation_Offset_InDistanceRatio";
        private const string KeyIsDoPostProcessing = "IsDoPostProcessing";
        private const string KeyFixingIterations = "FixingIterations";
        private const string KeyOffsetOperationInDistanceRatio = "OffsetOperation_InDistanceRatio";
        private const string KeyTubeRadiusModifierRatio = "TubeRadiusModifier";
        private const string KeyPastillePlacementModifier = "PastillePlacementModifier";
        private const string KeySquareExtensionFromPastilleCircumference = "SquareExtensionFromPastilleCircumference";
        private const string KeyCircleCenterRatioWithPastilleRadius = "CircleCenterRatioWithPastilleRadius";
        private const string KeySquareWidthRatioWithPastilleRadius = "SquareWidthRatioWithPastilleRadius";
        private const string KeyCircleRadiusRatioWithPastilleRadius = "CircleRadiusRatioWithPastilleRadius";
        private const string KeyTriangleHeightRatioWithDefault = "TriangleHeightRatioWithDefault";

        private const string KeyIsForUserTesting = "IsForUserTesting";
        private const string KeyMeasurementsSphereRadius = "MeasurementsSphereRadius";
        private const string KeyTrimaticPaths = "TrimaticPaths";
        private const string KeyTrimaticPath = "TrimaticPath";

        private const string KeyAutoDeployUrl = "Url";
        private const string KeyAutoDeployBuildPropertiesUrl = "BuildProperties";
        private const string KeyAutoDeployBuildDownloadUrl = "BuildDownload";
        private const string KeySmartDesignPropertiesUrl = "SmartDesignProperties";
        private const string KeySmartDesignDownloadUrl = "SmartDesignDownload";
        private const string KeyPBAPythonPropertiesUrl = "PBAPythonProperties";
        private const string KeyPBAPythonDownloadUrl = "PBAPythonDownload";
        private const string KeyAutoDeployDownloadTimeOutMin = "DownloadTimeOutMin";
        private const string KeyAutoDeployEnable = "Enable";

        private const string KeyAutoDeployVariableName = "VariableName";
        private const string KeyPluginVersionVariable = "PluginName";
        private const string KeyPythonVersionVariable = "PBAName";
        private const string KeySmartDesignVersionVariable = "SmartDesignName";
        private const string KeyChecksumSha256Variable = "ChecksumSha256";

        private static OverallImplantParams GetOverallImplantParameters(XmlNode nodePath)
        {
            return new OverallImplantParams()
            {
                WrapOperationSmallestDetails = XmlDocumentUtilities.ExtractDouble(nodePath, KeyWrapOperationSmallestDetail),
                WrapOperationGapClosingDistance = XmlDocumentUtilities.ExtractDouble(nodePath, KeyWrapOperationGapClosingDistance),
                WrapOperationOffset = XmlDocumentUtilities.ExtractDouble(nodePath, KeyWrapOperationOffset),
                IsDoPostProcessing = XmlDocumentUtilities.ExtractBoolean(nodePath, KeyIsDoPostProcessing),
                FixingIterations = XmlDocumentUtilities.ExtractInteger(nodePath, KeyFixingIterations)
            };
        }

        private static IndividualImplantParams GetIndividualImplantParameters(XmlNode nodePath)
        {
            return new IndividualImplantParams()
            {
                WrapOperationSmallestDetails = XmlDocumentUtilities.ExtractDouble(nodePath, KeyWrapOperationSmallestDetail),
                WrapOperationGapClosingDistance = XmlDocumentUtilities.ExtractDouble(nodePath, KeyWrapOperationGapClosingDistance),
                WrapOperationOffsetInDistanceRatio = XmlDocumentUtilities.ExtractDouble(nodePath, KeyWrapOperationOffsetInDistanceRatio),
                OffsetOperation_InDistanceRatio = XmlDocumentUtilities.ExtractDouble(nodePath, KeyOffsetOperationInDistanceRatio),
                TubeRadiusModifier = XmlDocumentUtilities.ExtractDouble(nodePath, KeyTubeRadiusModifierRatio),
                PastillePlacementModifier = XmlDocumentUtilities.ExtractDouble(nodePath, KeyPastillePlacementModifier)
            };
        }

        private static LandmarkImplantParams GetLandmarkImplantParameters(XmlNode nodePath)
        {
            return new LandmarkImplantParams()
            {
                SquareExtensionFromPastilleCircumference = XmlDocumentUtilities.ExtractDouble(nodePath, KeySquareExtensionFromPastilleCircumference),
                CircleCenterRatioWithPastilleRadius = XmlDocumentUtilities.ExtractDouble(nodePath, KeyCircleCenterRatioWithPastilleRadius),
                SquareWidthRatioWithPastilleRadius = XmlDocumentUtilities.ExtractDouble(nodePath, KeySquareWidthRatioWithPastilleRadius),
                CircleRadiusRatioWithPastilleRadius = XmlDocumentUtilities.ExtractDouble(nodePath, KeyCircleRadiusRatioWithPastilleRadius),
                TriangleHeightRatioWithDefault = XmlDocumentUtilities.ExtractDouble(nodePath, KeyTriangleHeightRatioWithDefault)
            };
        }

        public static GenioAutoImplantParams GetGenioAutoImplantParams()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences/AutoImplantProposal/Genio");
            return CMFAutoImplantPreferences.GetGenioAutoImplantParameters(nodePath);
        }

        public static bool GetIsForUserTesting()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences");
            return XmlDocumentUtilities.ExtractBoolean(nodePath, KeyIsForUserTesting);
        }

        public static double GetMeasurementsSphereRadius()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences");
            return XmlDocumentUtilities.ExtractDouble(nodePath, KeyMeasurementsSphereRadius);
        }

        public static bool GetTrimaticPath(out string trimaticPath)
        {
            trimaticPath = null;
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences");
            var trimaticPathsNode =  nodePath.SelectSingleNode(KeyTrimaticPaths);
            if (trimaticPathsNode == null)
            {
                return false;
            }

            var found = false;
            foreach (XmlNode trimaticPathNode in trimaticPathsNode.ChildNodes)
            {
                var tmpTrimaticPath  = trimaticPathNode.InnerText;
                if (!File.Exists(tmpTrimaticPath))
                {
                    continue;
                }

                found = true;
                trimaticPath = tmpTrimaticPath;
                break;
            }

            return found;
        }

        public static ActualImplantParams GetActualImplantParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences/ActualImplant");
            var parameter =  new ActualImplantParams()
            {
                OverallImplantParams = GetOverallImplantParameters(nodePath.SelectSingleNode("Overall")),
                IndividualImplantParams = GetIndividualImplantParameters(nodePath.SelectSingleNode("Individual")),
                LandmarkImplantParams = GetLandmarkImplantParameters(nodePath.SelectSingleNode("Landmark"))
            };
            return parameter;
        }

        public static GuideParams GetGuidePreviewParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences/GuidePreview");
            return CMFGuidePreferences.GetGuidePreviewParameters(nodePath);
        }

        public static GuideParams GetActualGuideParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences/ActualGuide");
            return CMFGuidePreferences.GetActualGuideParameters(nodePath);
        }


        public static GuideBarrelLevelingParams GetGuideBarrelLevelingParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences/GuideBarrelLeveling");
            return CMFGuidePreferences.GetBarrelLevelingParameters(nodePath);
        }

        public static ImplantSupportParams GetImplantSupportParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences/ImplantSupport");
            return CMFImplantSupportPreferences.GetImplantSupportParameters(nodePath);
        }

        public static ScrewAspectParams GetScrewAspectParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences/ScrewAspect");
            return CMFScrewAspectPreferences.GetScrewAspectParameters(nodePath);
        }

        public static GuideBridgeParams GetGuideBridgeParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences/GuideBridge");
            return CMFGuidePreferences.GetBridgeParameters(nodePath);
        }

        public static AutoDeploymentParams GetAutoDeploymentParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Preferences/AutoDeploy");
            var urlNode = nodePath.SelectSingleNode(KeyAutoDeployUrl);
            var variableNameNode = nodePath.SelectSingleNode(KeyAutoDeployVariableName);
            var parameter = new AutoDeploymentParams()
            {
                AutoDeployBuildPropertiesUrl = urlNode.SelectSingleNode(KeyAutoDeployBuildPropertiesUrl)?.InnerText,
                AutoDeployBuildDownloadUrl = urlNode.SelectSingleNode(KeyAutoDeployBuildDownloadUrl)?.InnerText,
                SmartDesignPropertiesUrl = urlNode.SelectSingleNode(KeySmartDesignPropertiesUrl)?.InnerText,
                SmartDesignDownloadUrl = urlNode.SelectSingleNode(KeySmartDesignDownloadUrl)?.InnerText,
                PBAPythonPropertiesUrl = urlNode.SelectSingleNode(KeyPBAPythonPropertiesUrl)?.InnerText,
                PBAPythonDownloadUrl = urlNode.SelectSingleNode(KeyPBAPythonDownloadUrl)?.InnerText,
                PluginVariableName = variableNameNode.SelectSingleNode(KeyPluginVersionVariable)?.InnerText,
                PBAPythonVariableName = variableNameNode.SelectSingleNode(KeyPythonVersionVariable)?.InnerText,
                SmartDesignVariableName = variableNameNode.SelectSingleNode(KeySmartDesignVersionVariable)?.InnerText,
                ChecksumSha256VariableName = variableNameNode.SelectSingleNode(KeyChecksumSha256Variable)?.InnerText,
                DownloadTimeOutMin = XmlDocumentUtilities.ExtractDouble(nodePath, KeyAutoDeployDownloadTimeOutMin),
                Enable = XmlDocumentUtilities.ExtractBoolean(nodePath, KeyAutoDeployEnable),
            };

            return parameter;
        }
    }
}
