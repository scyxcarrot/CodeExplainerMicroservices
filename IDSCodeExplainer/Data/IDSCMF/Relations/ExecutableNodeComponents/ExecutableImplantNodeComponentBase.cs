using IDS.CMF.CasePreferences;
using IDS.CMF.ImplantBuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Relations
{
    public class ExecutableImplantNodeComponentBase : IExecutableImplantNodeComponent
    {
        protected CMFObjectManager objectManager;
        protected CMFImplantDirector director;

        public ICaseData CaseData { get; set; }
        public List<Guid> Guids { get; set; }

        public ExecutableImplantNodeComponentBase(CMFImplantDirector director)
        {
            this.director = director;
            objectManager = new CMFObjectManager(director);
        }

        public virtual bool Execute(ICaseData data)
        {
            return true;
        }

        public virtual bool Execute()
        {
            Execute(CaseData);
            return true;
        }

        protected bool HandleDeletion(ExtendedImplantBuildingBlock eBlock)
        {
            var deleted = true;
            var blockIds = GetBlockIds(eBlock);

            if (!blockIds.Any())
            {
                return false;
            }

            blockIds.ForEach(guid =>
            {
                if (guid != Guid.Empty)
                {
                    deleted &= objectManager.DeleteObject(guid);
                }
            });

            return deleted;
        }

        protected bool HandleDbDeletion(ExtendedImplantBuildingBlock eBlock)
        {
            var deleted = true;
            var blockIds = GetBlockIds(eBlock);

            if (!blockIds.Any())
            {
                return false;
            }

            blockIds.ForEach(guid =>
            {
                if (guid != Guid.Empty)
                {
                    deleted &= director.IdsDocument.Delete(guid);
                }
            });

            return deleted;
        }

        private List<Guid> GetBlockIds(ExtendedImplantBuildingBlock eBlock)
        {
            var blockIds = objectManager.GetAllBuildingBlockIds(eBlock).ToList();

            if (!blockIds.Any())
            {
                return blockIds;
            }

            if (Guids != null)
            {
                Guids.RemoveAll(id => id == Guid.Empty);

                if (Guids.TrueForAll(id => blockIds.Contains(id)))
                {
                    blockIds = Guids;
                }
            }

            return blockIds;
        }
    }
}
