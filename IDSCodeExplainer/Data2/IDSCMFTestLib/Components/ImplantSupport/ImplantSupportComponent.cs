using Rhino.Geometry;
using System;

namespace IDS.CMF.TestLib.Components
{
    public class ImplantSupportComponent
    {
        public Guid CaseGuid { get; set; } = Guid.Empty;

        public MeshComponent MeshConfig { get; set; } = new MeshComponent();

        public Mesh GetImplantSupportMesh(string workDir)
        {
            MeshConfig.ParseFromComponent(workDir, out var partMesh);
            return partMesh;
        }

        public void FillToComponent(string implantSupportName, string workDir, Guid caseGuid,Mesh implantSupportMesh)
        {
            CaseGuid = caseGuid;
            MeshConfig.FillToComponent($"{implantSupportName}.stl", workDir, implantSupportMesh);
        }
    }
}
