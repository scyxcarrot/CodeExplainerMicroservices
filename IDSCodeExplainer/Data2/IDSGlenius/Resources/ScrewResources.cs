using System.IO;

namespace IDS.Glenius
{
    public class ScrewResources : Core.PluginHelper.Resources
    {

        private readonly Screw3Dot5LockingResources screw3Dot5LockingResources;
        private readonly Screw4Dot0LockingResources screw4Dot0Locking;
        private readonly Screw4Dot0NonLockingResources screw4Dot0NonLockingResources;
        private readonly ScrewM4ConnectionResources screwM4ConnectionResources;

        public ScrewResources()
        {
            screw3Dot5LockingResources = new Screw3Dot5LockingResources(ScrewAssetFolder);
            screw4Dot0Locking = new Screw4Dot0LockingResources(ScrewAssetFolder);
            screw4Dot0NonLockingResources = new Screw4Dot0NonLockingResources(ScrewAssetFolder);
            screwM4ConnectionResources = new ScrewM4ConnectionResources(ScrewAssetFolder);
        }

        public Screw3Dot5LockingResources Screw3Dot5Locking => screw3Dot5LockingResources; 

        public Screw4Dot0LockingResources Screw4Dot0Locking => screw4Dot0Locking; 

        public Screw4Dot0NonLockingResources Screw4Dot0NonLocking => screw4Dot0NonLockingResources; 

        public ScrewM4ConnectionResources ScrewM4Connection => screwM4ConnectionResources; 

        public string ScrewAssetFolder => Path.Combine(AssetsFolder, "Screws");

        
    }
}
