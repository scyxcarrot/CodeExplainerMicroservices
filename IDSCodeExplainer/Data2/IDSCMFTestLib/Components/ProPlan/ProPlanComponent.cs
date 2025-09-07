using IDS.CMF.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.IO;

namespace IDS.CMF.TestLib.Components
{
    public class ProPlanComponent
    {
        public IDSTransform TransformMatrix { get; set; } = new IDSTransform();

        public string PartName { get; set; }

        public MeshComponent MeshConfig { get; set; } = new MeshComponent();

        public void ParseToDirector(CMFImplantDirector director, string workDir)
        {
            var objectManager = new CMFObjectManager(director);
            var proPlanImportComponent = new ProPlanImportComponent();
            var newImplantPlacableBlock = proPlanImportComponent.GetProPlanImportBuildingBlock(PartName);
            var transform = TransformMatrix.ToRhinoTransform();

            MeshConfig.ParseFromComponent(workDir, out var partMesh);
            var guid = objectManager.AddNewBuildingBlockWithTransform(newImplantPlacableBlock, partMesh, transform);
            if (guid == Guid.Empty)
            {
                throw new IDSUnexpectedState($"{PartName} failed to add into director");
            }
        }

        public void FillToComponent(RhinoObject proPlanPart, string workDir)
        {
            if (!CMFObjectManager.GetTransformationMatrixFromPart(proPlanPart, out var transform))
            {
                throw new InvalidDataException("The part didn't contain transformation matrix");
            }

            TransformMatrix = transform.ToIDSTransform();
            var proPlanComponent = new ProPlanImportComponent();
            PartName = proPlanComponent.GetPartName(proPlanPart.Name);
            MeshConfig.FillToComponent($"{PartName}.stl", workDir, (Mesh)proPlanPart.Geometry);
        }
    }
}
