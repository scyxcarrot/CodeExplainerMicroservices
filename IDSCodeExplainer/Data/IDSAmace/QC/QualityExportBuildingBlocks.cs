using System.Collections.Generic;

namespace IDS.Amace.ImplantBuildingBlocks
{
    public class ExportBuildingBlocks
    {
        private static readonly List<IBB> CupQcExportBlocks = new List<IBB>{ IBB.DefectPelvis, IBB.DesignPelvis, IBB.ContralateralPelvis, IBB.Sacrum,
                                                                    IBB.Cup, IBB.CupPorousLayer, IBB.CupReamingEntity,
                                                                    IBB.CollisionEntity,
                                                                    IBB.CupRbv, IBB.ExtraReamingEntity, IBB.AdditionalRbv, IBB.ReamedPelvis,
                                                                    IBB.SkirtMesh,
                                                                    IBB.ScaffoldVolume,
                                                                    IBB.BoneGraft, IBB.BoneGraftRemaining, IBB.PreopPelvis };

        private static readonly List<IBB> ImplantQcExportBlocks = new List<IBB>{ IBB.DefectPelvis, IBB.DesignPelvis, IBB.ContralateralPelvis, IBB.Sacrum,
                                                                        IBB.Cup, IBB.CupStuds, IBB.CupPorousLayer,
                                                                        IBB.CollisionEntity,
                                                                        IBB.CupRbv, IBB.ExtraReamingEntity, IBB.AdditionalRbv, IBB.ReamedPelvis,
                                                                        IBB.OriginalReamedPelvis, IBB.TotalRbv, IBB.AdditionalRbv, IBB.ReamedPelvis,
                                                                        IBB.ScaffoldVolume,IBB.ScaffoldFinalized,
                                                                        IBB.Screw, IBB.ScrewContainer, IBB.ScrewHoleSubtractor, IBB.ScrewOutlineEntity, IBB.LateralBumpTrim,
                                                                        IBB.PlateHoles, IBB.SkirtMesh, IBB.SolidPlateBottom,
                                                                        IBB.BoneGraft, IBB.BoneGraftRemaining, IBB.PreopPelvis };

        private static readonly List<IBB> DesignPelvisExportBlocks = new List<IBB> { IBB.Cup, IBB.DesignPelvis, IBB.ExtraReamingEntity };

        private static readonly List<IBB> ScrewsExportBlocks = new List<IBB> { IBB.Cup };

        private static readonly List<IBB> ReportingBuildingBlocks = new List<IBB>{   IBB.CupStuds,
                                                                            IBB.TotalRbv,
                                                                            IBB.OriginalReamedPelvis,
                                                                            IBB.Screw,
                                                                            IBB.ScaffoldFinalized,
                                                                            IBB.PlateSmoothHoles,
                                                                            IBB.CollisionEntity,
                                                                            IBB.BoneGraft, IBB.BoneGraftRemaining };

        private static readonly List<IBB> PostProcessingBlocks = new List<IBB>{IBB.DefectPelvis, IBB.CupStuds, IBB.CupPorousLayer,
                                                                IBB.ReamedPelvis, IBB.DesignPelvis, IBB.OriginalReamedPelvis,
                                                                IBB.Cup,
                                                                IBB.TotalRbv, IBB.CupReamingEntity, IBB.ExtraReamingEntity,
                                                                IBB.CollisionEntity,
                                                                IBB.ScaffoldVolume, IBB.ScaffoldFinalized, IBB.SkirtMesh,
                                                                IBB.MedialBump,IBB.MedialBumpTrim,IBB.LateralBump,IBB.LateralBumpTrim,
                                                                IBB.Screw, IBB.ScrewContainer, IBB.ScrewHoleSubtractor,
                                                                IBB.LateralCupSubtractor, IBB.ScrewMbvSubtractor,
                                                                IBB.ScrewCushionSubtractor, IBB.ScrewOutlineEntity,
                                                                IBB.PlateSmoothHoles, IBB.SolidPlateBottom,
                                                                IBB.FilledSolidCup,IBB.SolidPlateRounded,
                                                                IBB.BoneGraft, IBB.BoneGraftRemaining };

        private static readonly List<IBB> PlastiModelsBlocks = new List<IBB> { IBB.ScrewPlasticSubtractor, IBB.BoneGraftRemaining };

        public static List<IBB> GetExportBuildingBlockListCupQc()
        {
            return CupQcExportBlocks;
        }

        public static List<IBB> GetExportBuildingBlockListImplantQc()
        {
            return ImplantQcExportBlocks;
        }

        public static List<IBB> GetExportBuildingBlockListDesignPelvis()
        {
            return DesignPelvisExportBlocks;
        }

        public static List<IBB> GetExportBuildingBlockListScrews()
        {
            return ScrewsExportBlocks;
        }

        public static List<IBB> GetExportBuildingBlockListReporting()
        {
            return ReportingBuildingBlocks;
        }

        public static List<IBB> GetExportBuildingBlockListPostProcessing()
        {
            return PostProcessingBlocks;
        }

        public static List<IBB> GetExportBuildingBlockListPlasticModels()
        {
            return PlastiModelsBlocks;
        }
    }
}
