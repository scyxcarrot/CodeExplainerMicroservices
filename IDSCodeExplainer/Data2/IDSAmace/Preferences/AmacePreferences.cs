using IDS.Core.Utilities;
using System.Xml;

namespace IDS.Amace.Preferences
{

    public static class AmacePreferences
    {
        private static readonly XmlDocument PreferencesDoc;

        static AmacePreferences()
        {
            var resources = new AmaceResources();
            PreferencesDoc = new XmlDocument();
            PreferencesDoc.Load(resources.AMacePreferenceXmlPath);
        }

        //Keys 
        private static string KeyTransitionWrapOperationSmallestDetail =>
            "Transition_WrapOperation_SmallestDetail";
        private static string KeyTransitionWrapOperationGapClosingDistance =>
            "Transition_WrapOperation_GapClosingDistance";
        private static string KeyTransitionWrapOperationOffset =>
            "Transition_WrapOperation_Offset";

        private static string KeyTransitionPreviewIsDoPostProcessing => "IsDoPostProcessing";

        public static TransitionIntermediatesParams GetTransitionIntermediatesParams()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Paths/PlateTransitionIntermediates");
            return new TransitionIntermediatesParams()
            {
                IntersectionEntityWrapResolution = XmlDocumentUtilities.ExtractDouble(nodePath, "IntersectionEntityWrapResolution")
            };
        }

        //Extract Info
        private static TransitionParams GetTransitionParams(XmlNode nodePath)
        {
            return new TransitionParams()
            {
                WrapOperationSmallestDetails =
                    XmlDocumentUtilities.ExtractDouble(nodePath, KeyTransitionWrapOperationSmallestDetail),
                WrapOperationGapClosingDistance =
                    XmlDocumentUtilities.ExtractDouble(nodePath, KeyTransitionWrapOperationGapClosingDistance),
                WrapOperationOffset =
                    XmlDocumentUtilities.ExtractDouble(nodePath, KeyTransitionWrapOperationOffset),

            };
        }

        public static TransitionPreviewParams GetTransitionPreviewParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Paths/PlateTransitionPreview/Flanges");

            return new TransitionPreviewParams()
            {
                IsDoPostProcessing = XmlDocumentUtilities.ExtractBoolean(nodePath, KeyTransitionPreviewIsDoPostProcessing),
                FlangesTransitionParams = GetTransitionParams(nodePath)
            };
        }

        public static TransitionActualParams GetTransitionActualParameters()
        {
            var nodePath = PreferencesDoc.SelectSingleNode("/Paths/PlateTransitionActual/Flanges");
            var flangesParams = GetTransitionParams(nodePath);

            nodePath = PreferencesDoc.SelectSingleNode("/Paths/PlateTransitionActual/ScrewBumps");

            var res =  new TransitionActualParams()
            {
                FlangesTransitionParams = flangesParams,
            };

            var screwBumpsParams = new ScrewBumpTransitionParams()
            {
                Parameters = GetTransitionParams(nodePath),
                RoiOffset = XmlDocumentUtilities.ExtractDouble(nodePath, "RoiOffset")
            };

            res.ScrewBumpsTransitionParams = screwBumpsParams;

            return res;
        }

    }
}
