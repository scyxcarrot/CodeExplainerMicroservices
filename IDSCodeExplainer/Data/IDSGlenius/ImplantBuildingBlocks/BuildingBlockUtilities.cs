using IDS.Core.Utilities;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Glenius.ImplantBuildingBlocks
{
    public class BuildingBlockUtilities
    {
        private readonly List<Mesh> disjointPieces;

        public BuildingBlockUtilities(IBB type, GleniusImplantDirector implantDirector)
        {
            IBB buildingBlockType = type;
            GleniusImplantDirector director = implantDirector;

            var objectManager = new GleniusObjectManager(director);
            var block = objectManager.GetBuildingBlock(buildingBlockType);
            if (block != null)
            {
                var mesh = block.Geometry as Mesh;
                disjointPieces = mesh.SplitDisjointPieces().ToList();
            }
            else
            {
                disjointPieces = new List<Mesh>();
            }
        }

        public bool IsDisjointPiece(Mesh pieceToCompare)
        {
            bool found = false;
            foreach (var mesh in disjointPieces)
            {
                if (pieceToCompare.IsEqual(mesh))
                {
                    found = true;
                    break;
                }
            }
            return found;
        }
    }
}