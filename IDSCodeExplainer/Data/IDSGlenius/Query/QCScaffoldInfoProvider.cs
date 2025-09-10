using IDS.Core.Utilities;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Glenius.Query
{
    public class QCScaffoldInfoProvider
    {
        private readonly GleniusObjectManager objectManager;
        private readonly ImplantDerivedEntities implantDerivedEntities;

        public QCScaffoldInfoProvider(GleniusImplantDirector director)
        {
            objectManager = new GleniusObjectManager(director);
            implantDerivedEntities = new ImplantDerivedEntities(director);
        }

        public double GetScaffoldVolumeInCC()
        {
            var scaffold = implantDerivedEntities.GetScaffoldShape();
            return Volume.MeshVolume(scaffold, true);
        }

        public double GetHeadRBVVolumeInCC()
        {
            double volume = 0;

            var rbvHeads = objectManager.GetAllBuildingBlocks(IBB.RBVHead);
            rbvHeads?.ToList().ForEach(x => volume += Volume.MeshVolume(x.Geometry as Mesh, true));

            return volume;
        }

        public double GetScaffoldRBVVolumeInCC()
        {
            double volume = 0;

            var rbvScaffolds = objectManager.GetAllBuildingBlocks(IBB.RbvScaffold);
            rbvScaffolds?.ToList().ForEach(x => volume += Volume.MeshVolume(x.Geometry as Mesh, true));

            return volume;
        }
        public double GetTotalRBVVolumeInCC()
        {
            return GetHeadRBVVolumeInCC() + GetScaffoldRBVVolumeInCC();
        }

    }
}
