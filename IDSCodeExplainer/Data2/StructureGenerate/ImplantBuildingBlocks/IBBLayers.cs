		//Generated on 2015-12-07 19:31:05.405000
		public static readonly Dictionary<IBB, string> BuildingBlockLayers = new Dictionary<IBB, string>
		{

			//General
			{ IBB.Generic, "General" },

			//Preop
			{ IBB.CollisionEntity, "Pre-op::Collision Entity" },
			{ IBB.ContralateralFemur, "Pre-op::Clat Femur" },
			{ IBB.ContralateralPelvis, "Pre-op::Clat Pelvis" },
			{ IBB.DefectFemur, "Pre-op::Def Femur" },
			{ IBB.DefectPelvis, "Pelvis::Original" },
			{ IBB.DefectPelvisTHIBQual, "Analysis::Bone Quality" },
			{ IBB.DefectPelvisTHICortex, "Analysis::Cortex THI" },
			{ IBB.DefectPelvisTHIWall, "Analysis::Wall THI" },
			{ IBB.OtherContralateralParts, "Pre-op::Other Clat Parts" },
			{ IBB.OtherDefectParts, "Pre-op::Other Def Parts" },
			{ IBB.Sacrum, "Pre-op::Sacrum" },

			//DesignPelvis
			{ IBB.DesignMeshDifference, "Analysis::Design Mesh Diff" },
			{ IBB.DesignPelvis, "Pelvis::Design" },

			//Cup
			{ IBB.Cup, "Cup::Cup" },
			{ IBB.CupPorousLayer, "Cup::Porous Layer" },
			{ IBB.CupRbvPreview, "Cup::Reaming preview" },
			{ IBB.CupReamingEntity, "Cup::Reaming Entity" },
			{ IBB.CupStuds, "Cup::Studs" },
			{ IBB.FilledSolidCup, "Constructors::Cup Filled" },
			{ IBB.LateralCupSubtractor, "Constructors::Cup Lateral Subtractor" },

			//Reaming
			{ IBB.AdditionalRbv, "Reaming::RBV By Extra" },
			{ IBB.CupRbv, "Reaming::RBV By Cup" },
			{ IBB.CupReamedPelvis, "Constructors::Pelvis Cup Reamed" },
			{ IBB.ExtraReamingEntity, "Reaming::Extra Reaming Entities" },
			{ IBB.OriginalReamedPelvis, "Pelvis::Original Reamed" },
			{ IBB.ReamedPelvis, "Pelvis::Design Reamed" },
			{ IBB.TotalRbv, "Reaming::RBV Total" },

			//Skirt
			{ IBB.SkirtBoneCurve, "Skirt::Bone Curve" },
			{ IBB.SkirtCupCurve, "Skirt::Cup Curve" },
			{ IBB.SkirtGuide, "Skirt::Guiding Curves" },
			{ IBB.SkirtMesh, "Skirt::Skirt" },

			//Scaffold
			{ IBB.ScaffoldBottom, "Constructors::Scaffold Bottom" },
			{ IBB.ScaffoldFinalized, "Scaffold::Processed" },
			{ IBB.ScaffoldSupport, "Scaffold::Support" },
			{ IBB.ScaffoldTop, "Constructors::Scaffold Top" },
			{ IBB.ScaffoldVolume, "Scaffold::Temp" },

			//Wraps
			{ IBB.WrapBottom, "Constructors::Wrap Bottom" },
			{ IBB.WrapScrewBump, "Constructors::Wrap Bump Trimming" },
			{ IBB.WrapSunkScrew, "Constructors::Wrap Screw Positioning" },
			{ IBB.WrapTop, "Constructors::Wrap Top" },

			//Plate
			{ IBB.PlateBumps, "Constructors::Plate Bumps" },
			{ IBB.PlateClearance, "Analysis::Implant Clearance" },
			{ IBB.PlateContourBottom, "Plate::Bottom Contour" },
			{ IBB.PlateContourTop, "Plate::Top Contour" },
			{ IBB.PlateFlat, "Constructors::Plate" },
			{ IBB.PlateHoles, "Plate::Plate Holes" },
			{ IBB.PlateSmoothBumps, "Constructors::Plate Bumps Rounded" },
			{ IBB.PlateSmoothHoles, "Plate::Plate Holes Rounded" },
			{ IBB.SolidPlate, "Constructors::Plate Flat" },
			{ IBB.SolidPlateBottom, "Constructors::Plate Flat Bottom" },
			{ IBB.SolidPlateRounded, "Plate::Plate Flat Rounded" },
			{ IBB.SolidPlateSide, "Constructors::Plate Flat Side" },
			{ IBB.SolidPlateTop, "Constructors::Plate Flat Top" },

			//Screws
			{ IBB.LateralBump, "Screws::Lateral Augmentations" },
			{ IBB.LateralBumpTrim, "Screws::Lateral Augmentations (Trimmed)" },
			{ IBB.MedialBump, "Screws::Medial Augmentations" },
			{ IBB.MedialBumpTrim, "Screws::Medial Augmentations (Trimmed)" },
			{ IBB.Screw, "Screws::Screw" },
			{ IBB.ScrewContainer, "Screws::Containers" },
			{ IBB.ScrewCushionSubtractor, "Constructors::ScrewAide Cushion Boolean" },
			{ IBB.ScrewHoleSubtractor, "Constructors::ScrewAide Holes Plate" },
			{ IBB.ScrewMbvSubtractor, "Constructors::ScrewAide MBV Boolean" },
			{ IBB.ScrewOutlineEntity, "Constructors::ScrewAide Outline Entities" },
			{ IBB.ScrewPlasticSubtractor, "Constructors::ScrewAide Holes Models" },
			{ IBB.ScrewStudSelector, "Constructors::ScrewAide Stud Selector" },
		};
