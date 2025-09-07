namespace IDS.CMF.ScrewQc
{
    public interface IContainNonSharedScrewCheckResult
    {
        // Use for update the non-shared-screw-check such as barrel vicinity since it will just check with the barrels in same case
        void UpdateResult(object nonSharedScrewCheckResult);

        // Use for merge the non-shared-screw-check done by a group of shared screws and shown in bubble
        IContainNonSharedScrewCheckResult Merge(IContainNonSharedScrewCheckResult otherResult);
    }
}
