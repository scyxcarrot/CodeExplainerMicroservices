		//Generated on 2015-12-07 19:31:05.395000
		public static readonly Dictionary<IBB, string> BlockDescriptions = new Dictionary<IBB, string>
		{

			//General
			{ IBB.Generic, "Generic stls loaded into the project" },

			//Preop
			{ IBB.CollisionEntity, "Preop parts that could have an effect on the implant design" },
			{ IBB.ContralateralFemur, "Femur of the contralateral hemi-pelvis" },
			{ IBB.ContralateralPelvis, "Contralateral hemi-pelvis" },
			{ IBB.DefectFemur, "Femur of the defect hemi-pelvis" },
			{ IBB.DefectPelvis, "Defect hemi-pelvis" },
			{ IBB.DefectPelvisTHIBQual, "Bone quality map of the defect hemi-pelvis" },
			{ IBB.DefectPelvisTHICortex, "Cortex thickness map of the defect hemi-pelvis" },
			{ IBB.DefectPelvisTHIWall, "Wall thickness map of the defect hemi-pelvis" },
			{ IBB.OtherContralateralParts, "Other preop parts on the contralateral hemi-pelvis side" },
			{ IBB.OtherDefectParts, "Other preop parts on the defect hemi-pelvis side" },
			{ IBB.Sacrum, "Sacrum" },

			//DesignPelvis
			{ IBB.DesignMeshDifference, "Difference map between the original defect hemi-pelvis and the design hemi-pelvis" },
			{ IBB.DesignPelvis, "Hemi-pelvis used for the design of the implant" },

			//Cup
			{ IBB.Cup, "Cup of the implant" },
			{ IBB.CupPorousLayer, "Porous layer on the outside of the cup" },
			{ IBB.CupRbvPreview, "Hemi-sphere that shows how much cup reaming will be performed" },
			{ IBB.CupReamingEntity, "Entity that is used to subtract from the bone to perform cup reaming" },
			{ IBB.CupStuds, "Studs on the inside of the cup" },
			{ IBB.FilledSolidCup, "Cup with a medial surface flush with the horizontal border" },
			{ IBB.LateralCupSubtractor, "Subtraction entity on the inner surface of the cup" },

			//Reaming
			{ IBB.AdditionalRbv, "Reaming bone volume created by the extra reaming entities" },
			{ IBB.CupRbv, "Reaming bone volume created by the cup" },
			{ IBB.CupReamedPelvis, "Defect hemi-pelvis after cup reaming" },
			{ IBB.ExtraReamingEntity, "Reaming entities used for extra reaming" },
			{ IBB.OriginalReamedPelvis, "Defect hemi-pelvis after cup, extra and screw bump reaming" },
			{ IBB.ReamedPelvis, "Defect hemi-pelvis after cup and extra reaming" },
			{ IBB.TotalRbv, "Reaming bone volume created by cup, extra reaming and screw bumps" },

			//Skirt
			{ IBB.SkirtBoneCurve, "Touchdown curve on the bone that defines where the skirt attaches to the bone" },
			{ IBB.SkirtCupCurve, "Liftoff curve on the cup that defines how flanges attach to the cup" },
			{ IBB.SkirtGuide, "Guiding curve that determines the shape of the skirt" },
			{ IBB.SkirtMesh, "Skirt mesh based on bone curve, cup curve and skirt guides" },

			//Scaffold
			{ IBB.ScaffoldBottom, "Bottom part of the scaffold that fills the defect" },
			{ IBB.ScaffoldFinalized, "Finalized scaffold volume with cup and screw holes subtracted" },
			{ IBB.ScaffoldSupport, "Scaffold support indicated on the bone" },
			{ IBB.ScaffoldTop, "Top part of the scaffold" },
			{ IBB.ScaffoldVolume, "Volume bounded by the top and bottom part of the scaffold" },

			//Wraps
			{ IBB.WrapBottom, "Wrapped offset of reamed bone and scaffold for bottom of plate" },
			{ IBB.WrapScrewBump, "Wrap to trim screw bumps" },
			{ IBB.WrapSunkScrew, "Top wrap with inner cup surface for sunk screw calculations" },
			{ IBB.WrapTop, "Wrapped offset of bottom wrap for top of plate" },

			//Plate
			{ IBB.PlateBumps, "Plate with cup and bumps" },
			{ IBB.PlateClearance, "Plate clearance map on bottom plate" },
			{ IBB.PlateContourBottom, "Bottom plate outline " },
			{ IBB.PlateContourTop, "Top plate outline" },
			{ IBB.PlateFlat, "Plate with cup" },
			{ IBB.PlateHoles, "Plate with cup, bumps and holes" },
			{ IBB.PlateSmoothBumps, "Smooth plate with cup and bumps" },
			{ IBB.PlateSmoothHoles, "Smooth plate with cup, bumps and holes" },
			{ IBB.SolidPlate, "Plate without cup" },
			{ IBB.SolidPlateBottom, "Bottom of plate without cup" },
			{ IBB.SolidPlateRounded, "Smooth plate without cup" },
			{ IBB.SolidPlateSide, "Side of plate without cup" },
			{ IBB.SolidPlateTop, "Top of plate without cup" },

			//Screws
			{ IBB.LateralBump, "Lateral bump of a screw" },
			{ IBB.LateralBumpTrim, "Trimmed lateral bump of a screw" },
			{ IBB.MedialBump, "Medial bump of a screw" },
			{ IBB.MedialBumpTrim, "Trimmed medial bump of a screw" },
			{ IBB.Screw, "Screw" },
			{ IBB.ScrewContainer, "Container of a screw that shows minimal volume necessary around a screw" },
			{ IBB.ScrewCushionSubtractor, "Subtraction entity a screw to create holes in the cushions" },
			{ IBB.ScrewHoleSubtractor, "Subtraction entity of a screw to create holes in the plate" },
			{ IBB.ScrewMbvSubtractor, "Subtraction entity of a screw to create holes in the scaffold" },
			{ IBB.ScrewOutlineEntity, "Entity of a screw to show the minimal plate outline around a screw" },
			{ IBB.ScrewPlasticSubtractor, "Subtraction entity of a screw to create holes in the plastic models" },
			{ IBB.ScrewStudSelector, "Entity of a screw to decide which studs need to be kept in the implant" },
		};
