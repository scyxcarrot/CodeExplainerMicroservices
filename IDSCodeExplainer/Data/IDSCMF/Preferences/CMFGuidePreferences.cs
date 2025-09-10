using IDS.Core.Utilities;
using System.Collections.Generic;
using System.Xml;

namespace IDS.CMF.Preferences
{
    public static class CMFGuidePreferences
    {
        private const string KeyRemeshOperationCount = "OperationCount";
        private const string KeyRemeshQualityThreshold = "QualityThreshold";
        private const string KeyRemeshMaximalGeometricError = "MaximalGeometricError";
        private const string KeyRemeshCheckMaximalEdgeLength = "CheckMaximalEdgeLength";
        private const string KeyRemeshMaximalEdgeLength = "MaximalEdgeLength";
        private const string KeyRemeshNumberOfIterations = "NumberOfIterations";
        private const string KeyRemeshSkipBadEdges = "SkipBadEdges";
        private const string KeyRemeshPreserveSurfaceBorders = "PreserveSurfaceBorders";

        private const string KeyLightweightSegmentRadius = "SegmentRadius";
        private const string KeyOctagonalBridgeCompensation = "OctagonalBridgeCompensation";
        private const string KeyLightweightFractionalTriangleEdgeLength = "FractionalTriangleEdgeLength";

        private const string KeyNonMeshHeight = "NonMeshHeight";
        private const string KeyNonMeshIsoCurveDistance = "NonMeshIsoCurveDistance";

        private const string KeyBooleanUnionBuildingBlocks = "UnionBuildingBlocks";
        private const string KeyBooleanSubtractWithScrewEntities = "SubtractWithScrewEntities";
        private const string KeyBooleanSubtractWithSupport = "SubtractWithSupport";

        private const string KeyAdditionalOffset = "AdditonalOffset";
        private const string KeyDefaultFrance = "DefaultFrance";
        private const string KeyDefaultUsCanada = "DefaultUsCanada";
        private const string KeyDefaultRoW = "DefaultRoW";       
        
        private const string KeyType = "BarrelType";
        private const string KeyDefault = "Default";

        private const string KeyBridgeDefaultDiameter = "DefaultDiameter";
        private const string KeyBridgeMinimumDiameter = "MinimumDiameter";
        private const string KeyBridgeMaximumDiameter = "MaximumDiameter";

        private const string KeyWrapOperationSmallestDetail = "WrapOperation_SmallestDetail";
        private const string KeyWrapOperationGapClosingDistance = "WrapOperation_GapClosingDistance";
        private const string KeyWrapOperationOffset = "WrapOperation_Offset";

        private static RemeshParams GetRemeshParameters(XmlNode nodePath)
        {
            return new RemeshParams()
            {
                OperationCount = XmlDocumentUtilities.ExtractInteger(nodePath, KeyRemeshOperationCount),
                QualityThreshold = XmlDocumentUtilities.ExtractDouble(nodePath, KeyRemeshQualityThreshold),
                MaximalGeometricError = XmlDocumentUtilities.ExtractDouble(nodePath, KeyRemeshMaximalGeometricError),
                CheckMaximalEdgeLength = XmlDocumentUtilities.ExtractBoolean(nodePath, KeyRemeshCheckMaximalEdgeLength),
                MaximalEdgeLength = XmlDocumentUtilities.ExtractDouble(nodePath, KeyRemeshMaximalEdgeLength),
                NumberOfIterations = XmlDocumentUtilities.ExtractInteger(nodePath, KeyRemeshNumberOfIterations),
                SkipBadEdges = XmlDocumentUtilities.ExtractBoolean(nodePath, KeyRemeshSkipBadEdges),
                PreserveSurfaceBorders = XmlDocumentUtilities.ExtractBoolean(nodePath, KeyRemeshPreserveSurfaceBorders)
            };
        }

        private static LightweightParams GetLightweightParameters(XmlNode nodePath)
        {
            return new LightweightParams()
            {
                SegmentRadius = XmlDocumentUtilities.ExtractDouble(nodePath, KeyLightweightSegmentRadius),
                OctagonalBridgeCompensation = XmlDocumentUtilities.ExtractDouble(nodePath, KeyOctagonalBridgeCompensation),
                FractionalTriangleEdgeLength = XmlDocumentUtilities.ExtractDouble(nodePath, KeyLightweightFractionalTriangleEdgeLength)
            };
        }

        private static NonMeshParams GetNonMeshParameters(XmlNode nodePath)
        {
            return new NonMeshParams()
            {
                NonMeshHeight = XmlDocumentUtilities.ExtractDouble(nodePath, KeyNonMeshHeight),
                NonMeshIsoCurveDistance = XmlDocumentUtilities.ExtractDouble(nodePath, KeyNonMeshIsoCurveDistance)
            };
        }

        private static GuideBooleanParams GetGuideBooleanParameters(XmlNode nodePath)
        {
            return new GuideBooleanParams()
            {
                UnionBuildingBlocks = XmlDocumentUtilities.ExtractBoolean(nodePath, KeyBooleanUnionBuildingBlocks),
                SubtractWithScrewEntities = XmlDocumentUtilities.ExtractBoolean(nodePath, KeyBooleanSubtractWithScrewEntities),
                SubtractWithSupport = XmlDocumentUtilities.ExtractBoolean(nodePath, KeyBooleanSubtractWithSupport)
            };
        }

        public static GuideParams GetGuidePreviewParameters(XmlNode nodePath)
        {
            var parameter = new GuideParams()
            {
                RemeshParams = GetRemeshParameters(nodePath.SelectSingleNode("Remesh")),
                LightweightParams = GetLightweightParameters(nodePath.SelectSingleNode("Lightweight")),
                GuideBooleanParams = GetGuideBooleanParameters(nodePath.SelectSingleNode("Boolean"))
            };
            return parameter;
        }

        public static GuideParams GetActualGuideParameters(XmlNode nodePath)
        {
            var parameter = new GuideParams()
            {
                GuideSurfaceOffset = XmlDocumentUtilities.ExtractDouble(nodePath, "GuideSurfaceOffset"),
                GuideSurfaceIsoCurveDistance = XmlDocumentUtilities.ExtractDouble(nodePath, "GuideSurfaceIsoCurveDistance"),
                FirstRemeshParams = GetRemeshParameters(nodePath.SelectSingleNode("FirstRemesh")),
                RemeshParams = GetRemeshParameters(nodePath.SelectSingleNode("Remesh")),
                LightweightParams = GetLightweightParameters(nodePath.SelectSingleNode("Lightweight")),
                NonMeshParams = GetNonMeshParameters(nodePath.SelectSingleNode("NonMesh")),
                GuideBooleanParams = GetGuideBooleanParameters(nodePath.SelectSingleNode("Boolean")),
            };
            return parameter;
        }

        public static GuideBarrelLevelingParams GetBarrelLevelingParameters(XmlNode nodePath)
        {
            var parameter = new GuideBarrelLevelingParams()
            {
                AdditonalOffset = XmlDocumentUtilities.ExtractDouble(nodePath, KeyAdditionalOffset),
                DefaultFrance = XmlDocumentUtilities.ExtractDouble(nodePath, KeyDefaultFrance),
                DefaultUsCanada = XmlDocumentUtilities.ExtractDouble(nodePath, KeyDefaultUsCanada),
                DefaultRoW = XmlDocumentUtilities.ExtractDouble(nodePath, KeyDefaultRoW),               
                AdditionalRanges = GetBarrelLevelingBarrelTypeParamsList(nodePath.SelectSingleNode("AdditionalRanges"))
            };
            return parameter;
        }

        private static List<BarrelLevelingBarrelTypeParams> GetBarrelLevelingBarrelTypeParamsList(XmlNode nodePath)
        {
            var list = new List<BarrelLevelingBarrelTypeParams>();

            var screwTypes = nodePath.SelectNodes("Barrel");
            foreach (XmlNode child in screwTypes)
            {
                var parameter = new BarrelLevelingBarrelTypeParams()
                {
                    Type = child.SelectSingleNode(KeyType)?.InnerText,
                    Default = XmlDocumentUtilities.ExtractDouble(child, KeyDefault)
                };

                list.Add(parameter);
            }

            return list;
        }

        public static GuideBridgeParams GetBridgeParameters(XmlNode nodePath)
        {
            var parameter = new GuideBridgeParams()
            {
                DefaultDiameter = XmlDocumentUtilities.ExtractDouble(nodePath, KeyBridgeDefaultDiameter),
                MinimumDiameter = XmlDocumentUtilities.ExtractDouble(nodePath, KeyBridgeMinimumDiameter),
                MaximumDiameter = XmlDocumentUtilities.ExtractDouble(nodePath, KeyBridgeMaximumDiameter)
            };
            return parameter;
        }
    }
}
