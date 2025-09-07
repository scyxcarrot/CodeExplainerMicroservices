using IDS.Amace.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using System;

namespace IDS.Amace.Relations
{
    public partial class Dependencies
    {
        //[AH] AMace Specific???
        /// <summary>
        /// Deletes the screw dependencies.
        /// </summary>
        /// <param name="screw">The screw.</param>
        /// <returns></returns>
        public bool DeleteScrewDependencies(Screw screw)
        {
            ObjectManager objectManager = new ObjectManager(screw.Director);

            // Remove all screw aides from the document
            foreach (Guid id in screw.ScrewAides.Values)
            {
                objectManager.DeleteObject(id);
            }

            // Empty the screw aide dictionary of the screw
            screw.ScrewAides.Clear();

            return true;
        }

        public void UpdateAdditionalReaming(ImplantDirector director)
        {
            var objManager = new AmaceObjectManager(director);

            var reamingManager = new ReamingManager(director);
            reamingManager.PerformAdditionalReaming(IBB.CupReamedPelvis);
            if (objManager.HasBuildingBlock(IBB.BoneGraft))
            {
                reamingManager.PerformGraftReaming();
            }
        }

        public void UpdateCupAndAdditionalReaming(ImplantDirector director)
        {
            var reamingManager = new ReamingManager(director);
            reamingManager.PerformCupReaming(IBB.DesignPelvis);
            UpdateAdditionalReaming(director);
        }
    }
}
