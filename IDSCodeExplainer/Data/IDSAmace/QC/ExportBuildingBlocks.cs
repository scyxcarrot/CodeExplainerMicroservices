using System;
using Rhino;
using Rhino.Commands;
using System.Collections.Generic;
using IDS.ImplantBuildingBlocks;
using IDS.Enumerators;

namespace IDS.Quality
{
    public class ExportBuildingBlocks
    {
        static private List<IBB> cupQcExportBlocks = new List<IBB>{ IBB.DefectPelvis, IBB.DesignPelvis, IBB.ContralateralPelvis, IBB.Sacrum,
                                                                    IBB.Cup, IBB.CupPorousLayer, IBB.CupReamingEntity,
                                                                    IBB.CollisionEntity,
                                                                    IBB.CupRbv, IBB.ExtraReamingEntity, IBB.AdditionalRbv, IBB.ReamedPelvis,
                                                                    IBB.SkirtMesh,
                                                                    IBB.ScaffoldVolume,
                                                                    };

        static private List<IBB> implantQcExportBlocks = new List<IBB>{ IBB.DefectPelvis, IBB.DesignPelvis, IBB.ContralateralPelvis, IBB.Sacrum,
                                                                        IBB.Cup, IBB.CupStuds, IBB.CupPorousLayer,
                                                                        IBB.CollisionEntity,
                                                                        IBB.CupRbv, IBB.ExtraReamingEntity, IBB.AdditionalRbv, IBB.ReamedPelvis, IBB.OriginalReamedPelvis, IBB.TotalRbv, IBB.AdditionalRbv, IBB.ReamedPelvis,
                                                                        IBB.ScaffoldVolume,IBB.ScaffoldFinalized,
                                                                        IBB.Screw, IBB.ScrewContainer, IBB.ScrewHoleSubtractor, IBB.ScrewOutlineEntity, IBB.LateralBumpTrim,
                                                                        IBB.PlateHoles, IBB.SkirtMesh, IBB.SolidPlateBottom,
                                                                        };

        static private List<IBB> designPelvisExportBlocks = new List<IBB> { IBB.Cup, IBB.DesignPelvis, IBB.ExtraReamingEntity };

        static private List<IBB> screwsExportBlocks = new List<IBB> { IBB.Cup };

        public static List<IBB> GetExportBuildingBlockListCupQc()
        {
            return cupQcExportBlocks;
        }

        public static List<IBB> GetExportBuildingBlockListImplantQc()
        {
            return implantQcExportBlocks;
        }

        public static List<IBB> GetExportBuildingBlockListDesignPelvis()
        {
            return designPelvisExportBlocks;
        }

        public static List<IBB> GetExportBuildingBlockListScrews()
        {
            return screwsExportBlocks;
        }
    }
}
