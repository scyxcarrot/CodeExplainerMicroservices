using IDS.CMF.Visualization;
using IDS.Core.ImplantBuildingBlocks;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.ImplantBuildingBlocks
{
    public static class BuildingBlocks
    {
        #region List of blocks

        public static readonly Dictionary<IBB, ImplantBuildingBlock> Blocks = new Dictionary<IBB, ImplantBuildingBlock>
        {
            {
                IBB.Generic, new ImplantBuildingBlock
                {
                    ID = (int) IBB.Generic,
                    Name = IBB.Generic.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = ImplantBuildingBlockProperties.GenericLayerName,
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.ProPlanImport, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ProPlanImport,
                    Name = $"{Constants.ProPlanImport.ObjectPrefix}"+"{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}",
                    Color = Colors.Bone
                }
            },
            {
                IBB.PlanningImplant, new ImplantBuildingBlock
                {
                    ID = (int) IBB.PlanningImplant,
                    Name = "PlanningImplant_{0}",
                    GeometryType = ObjectType.Brep,
                    Layer = "{0}::Planning",
                    Color = Colors.PlateTemporary
                }
            },
            {
                IBB.ImplantPreview, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ImplantPreview,
                    Name = "ImplantPreview_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Preview",
                    Color = Colors.Implant
                }
            },
            {
                IBB.Screw, new ImplantBuildingBlock
                {
                    ID = (int) IBB.Screw,
                    Name = "Screw_{0}",
                    GeometryType = ObjectType.Brep,
                    Layer = "{0}::Screws",
                    Color = Colors.Screw
                }
            },
            {
                IBB.Connection, new ImplantBuildingBlock
                {
                    ID = (int) IBB.Connection,
                    Name = "Connection_{0}",
                    GeometryType = ObjectType.Curve,
                    Layer = "{0}::Connections",
                    Color = Colors.Connection
                }
            },
            {
                IBB.NervesWrapped, new ImplantBuildingBlock
                {
                    ID = (int) IBB.NervesWrapped,
                    Name = "Nerves_Wrapped",
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Nerves_Wrapped",
                    Color = Colors.NerveWrapped
                }
            },
            {
                IBB.Landmark, new ImplantBuildingBlock
                {
                    ID = (int) IBB.Landmark,
                    Name = "Landmark_{0}",
                    GeometryType = ObjectType.Brep,
                    Layer = "{0}::Landmark",
                    Color = Colors.Landmark
                }
            },
            {
                IBB.ImplantSupport, new ImplantBuildingBlock()
                {
                    ID = (int) IBB.ImplantSupport,
                    Name = "ImplantSupport_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Implant Support Mesh",
                    Color = Colors.ImplantSupport
                }
            },
            {
                IBB.RegisteredBarrel, new ImplantBuildingBlock
                {
                    ID = (int) IBB.RegisteredBarrel,
                    Name = "RegisteredBarrel_{0}",
                    GeometryType = ObjectType.Brep,
                    Layer = "{0}::Registered Barrel",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.GuideSupport, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideSupport,
                    Name = IBB.GuideSupport.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.OriginalLayer}::Guide Support",
                    Color = Colors.GuideSupport
                }
            },
            {
                IBB.GuideSurfaceWrap, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideSurfaceWrap,
                    Name = IBB.GuideSurfaceWrap.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.OriginalLayer}::Guide Surface Wrap",
                    Color = Colors.GuideSurfaceWrap
                }
            },
              {
                IBB.GuideFlange, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideFlange,
                    Name = "GuideFlange_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Guide Flange",
                    Color = Colors.GuideFlange
                }
            },
            {
                IBB.GuideFixationScrew, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideFixationScrew,
                    Name = "GuideFixationScrew_{0}",
                    GeometryType = ObjectType.Brep,
                    Layer = "{0}::Screw",
                    Color = Colors.GuideScrewFixation
                }
            },
            {
                IBB.GuideFixationScrewEye, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideFixationScrewEye,
                    Name = "GuideFixationScrewEye_{0}",
                    GeometryType = ObjectType.Brep,
                    Layer = "{0}::Screw Eyes",
                    Color = Colors.GuideScrewFixationEye
                }
            },
            {
                IBB.GuideFlangeGuidingOutline, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideFlangeGuidingOutline,
                    Name = IBB.GuideFlangeGuidingOutline.ToString(),
                    GeometryType = ObjectType.Curve,
                    Layer = $"{Constants.ProPlanImport.OriginalLayer}::Flange Guiding Outline",
                    Color = Colors.GuideFlangeGuidingOutline
                }
            },
            {
                IBB.GuideBridge, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideBridge,
                    Name = "GuideBridge_{0}",
                    GeometryType = ObjectType.Brep,
                    Layer = "{0}::Bridge(s)",
                    Color = Colors.GuideBridge
                }
            },
            {
                IBB.ReferenceEntities, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ReferenceEntities,
                    Name = IBB.ReferenceEntities.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = "Reference Entities::{0}",
                    Color = Colors.ReferenceEntities
                }
            },
            {
                IBB.AnatomicalObstacles, new ImplantBuildingBlock
                {
                    ID = (int) IBB.AnatomicalObstacles,
                    Name = IBB.AnatomicalObstacles.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = "Anatomical Obstacles::Anatomical Obstacles",
                    Color = Colors.AnatomicalObstacles

                }
            },
            {
                IBB.GuideFixationScrewLabelTag, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideFixationScrewLabelTag,
                    Name = "GuideFixationScrewLabelTag_{0}",
                    GeometryType = ObjectType.Brep,
                    Layer = "{0}::Screw Label Tags",
                    Color = Colors.GuideScrewFixationLabelTag
                }
            },
            {
                IBB.OriginalTeethWrapped, new ImplantBuildingBlock
                {
                    ID = (int) IBB.OriginalTeethWrapped,
                    Name = "OriginalTeethWrapped",
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.OriginalLayer}::Teeth_Wrapped",
                    Color = Colors.TeethWrapped
                }
            },
            {
                IBB.PlannedTeethWrapped, new ImplantBuildingBlock
                {
                    ID = (int) IBB.PlannedTeethWrapped,
                    Name = "PlannedTeethWrapped",
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Teeth_Wrapped",
                    Color = Colors.TeethWrapped
                }
            },
            {
                IBB.OriginalMaxillaTeethWrapped, new ImplantBuildingBlock
                {
                    ID = (int) IBB.OriginalMaxillaTeethWrapped,
                    Name = "OriginalMaxillaTeethWrapped",
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.OriginalLayer}::Maxilla_Teeth_Wrapped",
                    Color = Colors.TeethWrapped
                }
            },
            {
                IBB.OriginalMandibleTeethWrapped, new ImplantBuildingBlock
                {
                    ID = (int) IBB.OriginalMandibleTeethWrapped,
                    Name = "OriginalMandibleTeethWrapped",
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.OriginalLayer}::Mandible_Teeth_Wrapped",
                    Color = Colors.TeethWrapped
                }
            },
            {
                IBB.PlannedMaxillaTeethWrapped, new ImplantBuildingBlock
                {
                    ID = (int) IBB.PlannedMaxillaTeethWrapped,
                    Name = "PlannedMaxillaTeethWrapped",
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Maxilla_Teeth_Wrapped",
                    Color = Colors.TeethWrapped
                }
            },
            {
                IBB.PlannedMandibleTeethWrapped, new ImplantBuildingBlock
                {
                    ID = (int) IBB.PlannedMandibleTeethWrapped,
                    Name = "PlannedMandibleTeethWrapped",
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Mandible_Teeth_Wrapped",
                    Color = Colors.TeethWrapped
                }
            },
            {
                IBB.OriginalNervesWrapped, new ImplantBuildingBlock
                {
                    ID = (int) IBB.OriginalNervesWrapped,
                    Name = "OriginalNervesWrapped",
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.OriginalLayer}::Nerves_Wrapped",
                    Color = Colors.NerveWrapped
                }
            },
            {
                IBB.GuideSurface, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideSurface,
                    Name = "GuideSurface_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::GuideSurface",
                    Color = Color.Yellow
                }
            },
            {
                IBB.PositiveGuideDrawings, new ImplantBuildingBlock
                {
                    ID = (int) IBB.PositiveGuideDrawings,
                    Name = "PositiveGuideDrawings_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::PositiveGuideDrawings",
                    Color = Colors.GuidePositiveSurfaces
                }
            },
            {
                IBB.NegativeGuideDrawing, new ImplantBuildingBlock
                {
                    ID = (int) IBB.NegativeGuideDrawing,
                    Name = "NegativeGuideDrawing_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::NegativeGuideDrawings",
                    Color = Colors.GuideNegativeSurfaces
                }
            },
            {
                IBB.GuideLinkSurface, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideLinkSurface,
                    Name = "GuideLinkSurface_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::GuideLinkSurfaces",
                    Color = Color.Yellow
                }
            },
            {
                IBB.GuideSolidSurface, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideSolidSurface,
                    Name = "GuideSolidSurface_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::GuideSolidSurfaces",
                    Color = Colors.GuideSolidPatch
                }
            },
            {
                IBB.ActualImplant, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ActualImplant,
                    Name = "ActualImplant_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Actual",
                    Color = Colors.Implant
                }
            },
            {
                IBB.ActualImplantWithoutStampSubtraction, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ActualImplantWithoutStampSubtraction,
                    Name = "ActualImplantWithoutStampSubtraction_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::ActualImplantWithoutStampSubtraction",
                    Color = Colors.Implant
                }
            },
            {
                IBB.ActualImplantSurfaces, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ActualImplantSurfaces,
                    Name = "ActualImplantSurfaces_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::ActualImplantSurfaces",
                    Color = Colors.Implant
                }
            },
            {
                IBB.GuidePreviewSmoothen, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuidePreviewSmoothen,
                    Name = "GuidePreviewSmoothen_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Guide Preview Smoothen",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.ActualGuide, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ActualGuide,
                    Name = "ActualGuide_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Actual Guide",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.GuideBaseWithLightweight, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideBaseWithLightweight,
                    Name = "GuideBaseWithLightweight_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Guide Base With Lightweight",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.PastillePreview, new ImplantBuildingBlock
                {
                    ID = (int) IBB.PastillePreview,
                    Name = "PastillePreview_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Pastille Preview",
                    Color = Colors.Implant
                }
            },
            {
                IBB.SmoothGuideBaseSurface, new ImplantBuildingBlock
                {
                    ID = (int) IBB.SmoothGuideBaseSurface,
                    Name = "SmoothGuideBaseSurface_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Smooth Guide Base Surface",
                    Color = Color.Orange
                }
            },
            {
                IBB.GuideSupportRoI, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideSupportRoI,
                    Name = IBB.GuideSupportRoI.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PreopLayer}::Guide Support RoI",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.ImplantMarginGuidingOutline, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ImplantMarginGuidingOutline,
                    Name = IBB.ImplantMarginGuidingOutline.ToString(),
                    GeometryType = ObjectType.Curve,
                    Layer = $"{Constants.ProPlanImport.OriginalLayer}::Implant Margin Guiding Outline",
                    Color = Colors.ImplantMarginGuidingOutline
                }
            },
            {
                IBB.ImplantMargin, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ImplantMargin,
                    Name = IBB.ImplantMargin.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Implant Margin",
                    Color = Colors.ImplantMargin
                }
            },
            {
                IBB.ImplantSupportTeethIntegrationRoI, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ImplantSupportTeethIntegrationRoI,
                    Name = IBB.ImplantSupportTeethIntegrationRoI.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Implant Support Teeth Integration RoI",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.ImplantSupportRemovedMetalIntegrationRoI, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ImplantSupportRemovedMetalIntegrationRoI,
                    Name = IBB.ImplantSupportRemovedMetalIntegrationRoI.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Implant Support Removed Metal Integration RoI",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.ImplantSupportRemainedMetalIntegrationRoI, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ImplantSupportRemainedMetalIntegrationRoI,
                    Name = IBB.ImplantSupportRemainedMetalIntegrationRoI.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Implant Support Remained Metal Integration RoI",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.ImplantSupportGuidingOutline, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ImplantSupportGuidingOutline,
                    Name = IBB.ImplantSupportGuidingOutline.ToString(),
                    GeometryType = ObjectType.Curve,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Implant Support Guiding Outline",
                    Color = Colors.ImplantSupportGuidingOutline
                }
            },
            {
                IBB.ImplantTransition, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ImplantTransition,
                    Name = IBB.ImplantTransition.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PlannedLayer}::Implant Transition",
                    Color = Colors.ImplantTransition
                }
            },
            {
                IBB.ActualImplantImprintSubtractEntity, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ActualImplantImprintSubtractEntity,
                    Name = "ActualImplantImprintSubtractEntity_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::ActualImplantImprintSubtractEntity",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.ImplantScrewIndentationSubtractEntity, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ImplantScrewIndentationSubtractEntity,
                    Name = "ImplantScrewIndentationSubtractEntity_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::ImplantScrewIndentationSubtractEntity",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.ActualGuideImprintSubtractEntity, new ImplantBuildingBlock()
                {
                    ID = (int) IBB.ActualGuideImprintSubtractEntity,
                    Name = "ActualGuideImprintSubtractEntity_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::ActualGuideImprintSubtractEntity",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.GuideScrewIndentationSubtractEntity, new ImplantBuildingBlock()
                {
                    ID = (int) IBB.GuideScrewIndentationSubtractEntity,
                    Name = "GuideScrewIndentationSubtractEntity_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::GuideScrewIndentationSubtractEntity",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.GuideSupportRemovedMetalIntegrationRoI, new ImplantBuildingBlock
                {
                    ID = (int) IBB.GuideSupportRemovedMetalIntegrationRoI,
                    Name = IBB.GuideSupportRemovedMetalIntegrationRoI.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.ProPlanImport.PreopLayer}::Guide Support Removed Metal Integration RoI",
                    Color = Colors.GeneralGrey
                }
            },
            {
                IBB.TeethBlock, new ImplantBuildingBlock
                {
                    ID = (int) IBB.TeethBlock,
                    Name = "TeethBlock_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::TeethBlock",
                    Color = Colors.TeethBlock
                }
            },
            {
                IBB.ConnectionPreview, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ConnectionPreview,
                    Name = "ConnectionPreview_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Connection Preview",
                    Color = Colors.Implant
                }
            },
            {
                IBB.PatchSupport, new ImplantBuildingBlock
                {
                    ID = (int) IBB.PatchSupport,
                    Name = "PatchSupport_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Patch Support",
                    Color = Colors.PatchSupport
                }
            },
            {
                IBB.LimitingSurfaceMandible, new ImplantBuildingBlock
                {
                    ID = (int) IBB.LimitingSurfaceMandible,
                    Name = IBB.LimitingSurfaceMandible.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Limiting Surface Mandible",
                    Color = Colors.LimitingSurface
                }
            },
            {
                IBB.LimitingSurfaceMaxilla, new ImplantBuildingBlock
                {
                    ID = (int) IBB.LimitingSurfaceMaxilla,
                    Name = IBB.LimitingSurfaceMaxilla.ToString(),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Limiting Surface Maxilla",
                    Color = Colors.LimitingSurface
                }
            },
            {
                IBB.TeethBaseRegion, new ImplantBuildingBlock
                {
                    ID = (int) IBB.TeethBaseRegion,
                    Name = "TeethBaseRegion_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Teeth Base Region",
                    Color = Colors.TeethBaseRegion
                }
            },
            {
                IBB.ReinforcementRegionMandible, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ReinforcementRegionMandible,
                    Name = nameof(IBB.ReinforcementRegionMandible),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Reinforcement Region Mandible",
                    Color = Colors.ReinforcementRegion
                }
            },
            {
                IBB.ReinforcementRegionMaxilla, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ReinforcementRegionMaxilla,
                    Name = nameof(IBB.ReinforcementRegionMaxilla),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Reinforcement Region Maxilla",
                    Color = Colors.ReinforcementRegion
                }
            },
            {
                IBB.BracketRegionMandible, new ImplantBuildingBlock
                {
                    ID = (int) IBB.BracketRegionMandible,
                    Name = nameof(IBB.BracketRegionMandible),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Bracket Region Mandible",
                    Color = Colors.BracketRegion
                }
            },
            {
                IBB.BracketRegionMaxilla, new ImplantBuildingBlock
                {
                    ID = (int) IBB.BracketRegionMaxilla,
                    Name = nameof(IBB.BracketRegionMaxilla),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Bracket Region Maxilla",
                    Color = Colors.BracketRegion
                }
            },
            {
                IBB.LimitingSurfaceExtrusionMandible, new ImplantBuildingBlock
                {
                    ID = (int) IBB.LimitingSurfaceExtrusionMandible,
                    Name = nameof(IBB.LimitingSurfaceExtrusionMandible),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Limiting Surface Extrusion Mandible",
                    Color = Colors.LimitingSurfaceExtrusion
                }
            },
            {
                IBB.LimitingSurfaceExtrusionMaxilla, new ImplantBuildingBlock
                {
                    ID = (int) IBB.LimitingSurfaceExtrusionMaxilla,
                    Name = nameof(IBB.LimitingSurfaceExtrusionMaxilla),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Limiting Surface Extrusion Maxilla",
                    Color = Colors.LimitingSurfaceExtrusion
                }
            },
            {
                IBB.BracketExtrusionMaxilla, new ImplantBuildingBlock
                {
                    ID = (int) IBB.BracketExtrusionMaxilla,
                    Name = nameof(IBB.BracketExtrusionMaxilla),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Bracket Extrusion Maxilla",
                    Color = Colors.BracketExtrusion
                }
            },
            {
                IBB.BracketExtrusionMandible, new ImplantBuildingBlock
                {
                    ID = (int) IBB.BracketExtrusionMandible,
                    Name = nameof(IBB.BracketExtrusionMandible),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Bracket Extrusion Mandible",
                    Color = Colors.BracketExtrusion
                }
            },
            {
                IBB.TeethBlockROIMaxilla, new ImplantBuildingBlock
                {
                    ID = (int) IBB.TeethBlockROIMaxilla,
                    Name = nameof(IBB.TeethBlockROIMaxilla),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Teeth Block ROI Maxilla",
                    Color = Colors.TeethBlockROI
                }
            },
            {
                IBB.TeethBlockROIMandible, new ImplantBuildingBlock
                {
                    ID = (int) IBB.TeethBlockROIMandible,
                    Name = nameof(IBB.TeethBlockROIMandible),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Teeth Block ROI Mandible",
                    Color = Colors.TeethBlockROI
                }
            },
            {
                IBB.FinalSupportMaxilla, new ImplantBuildingBlock
                {
                    ID = (int) IBB.FinalSupportMaxilla,
                    Name = nameof(IBB.FinalSupportMaxilla),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Final Support Maxilla",
                    Color = Colors.FinalSupport
                }
            },
            {
                IBB.FinalSupportMandible, new ImplantBuildingBlock
                {
                    ID = (int) IBB.FinalSupportMandible,
                    Name = nameof(IBB.FinalSupportMandible),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Final Support Mandible",
                    Color = Colors.FinalSupport
                }
            },
            {
                IBB.TeethBaseExtrusion, new ImplantBuildingBlock
                {
                    ID = (int) IBB.TeethBaseExtrusion,
                    Name = "TeethBaseExtrusion_{0}",
                    GeometryType = ObjectType.Mesh,
                    Layer = "{0}::Teeth Base Extrusion",
                    Color = Colors.TeethBaseExtrusion
                }
            },
            {
                IBB.ReinforcementExtrusionMaxilla, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ReinforcementExtrusionMaxilla,
                    Name = nameof(IBB.ReinforcementExtrusionMaxilla),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Reinforcement Extrusion Maxilla",
                    Color = Colors.ReinforcementExtrusion
                }
            },
            {
                IBB.ReinforcementExtrusionMandible, new ImplantBuildingBlock
                {
                    ID = (int) IBB.ReinforcementExtrusionMandible,
                    Name = nameof(IBB.ReinforcementExtrusionMandible),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Reinforcement Extrusion Mandible",
                    Color = Colors.ReinforcementExtrusion
                }
            },
            {
                IBB.FinalSupportWrappedMaxilla, new ImplantBuildingBlock
                {
                    ID = (int) IBB.FinalSupportWrappedMaxilla,
                    Name = nameof(IBB.FinalSupportWrappedMaxilla),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Final Support Wrapped Maxilla",
                    Color = Colors.FinalSupportWrapped
                }
            },
            {
                IBB.FinalSupportWrappedMandible, new ImplantBuildingBlock
                {
                    ID = (int) IBB.FinalSupportWrappedMandible,
                    Name = nameof(IBB.FinalSupportWrappedMandible),
                    GeometryType = ObjectType.Mesh,
                    Layer = $"{Constants.LayerName.TeethSupportedGuide}::Final Support Wrapped Mandible",
                    Color = Colors.FinalSupportWrapped
                }
            }
        };

        #endregion

        /// <summary>
        /// Gets all building blocks.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IBB> GetAllBuildingBlocks()
        {
            return Enum.GetValues(typeof(IBB)).Cast<IBB>();
        }
    }
}
