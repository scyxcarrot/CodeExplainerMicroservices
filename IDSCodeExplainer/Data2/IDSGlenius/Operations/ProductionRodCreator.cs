using IDS.Glenius.ImplantBuildingBlocks;
using Rhino.Geometry;
using System.Linq;

namespace IDS.Glenius.Operations
{
    public class ProductionRodCreator
    {
        private GleniusImplantDirector director;

        public ProductionRodCreator(GleniusImplantDirector director)
        {
            this.director = director;
        }

        public Brep CreateProductionRod(bool withChamfer)
        {
            var objectManager = new GleniusObjectManager(director);
            var productionRod = objectManager.GetBuildingBlock(IBB.ProductionRod).Geometry as Brep;

            if (withChamfer)
            {
                var chamfer = CreateProductionRodChamferPart();
                var newProductionRodWithChamfer = Brep.CreateBooleanUnion(new[] { chamfer, productionRod.DuplicateBrep() }, 0.01);
                if (!newProductionRodWithChamfer.Any())
                {
                    return null;
                }
                productionRod = newProductionRodWithChamfer.First();
            }

            return productionRod.DuplicateBrep();
        }

        public Brep CreateProductionRodChamferPart()
        {
            var objectManager = new GleniusObjectManager(director);
            var headAlignment = new HeadAlignment(director.AnatomyMeasurements, objectManager, director.Document, director.defectIsLeft);
            var productionRodCreator = new ProductionRodChamferCreator(objectManager, headAlignment);
            var productionRodChamfer = productionRodCreator.Create();
            return productionRodChamfer;
        }
    }
}
