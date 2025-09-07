namespace IDS.CMF.V2.MTLS.Operation
{
    public class AutoImplantProposalResult
    {
        public long[,] LinkConnections { get; set; }
        public long[,] PlateConnections { get; set; }
        public double[,] ScrewHeads { get; set; }
        public double[,] ScrewTips { get; set; }
        public long[] ScrewNumbers { get; set; }
        public byte[] ScrewIssues { get; set; }
    }
}
