using Rhino.Geometry;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;

namespace IDS.Glenius.Operations
{
    public static class ReamingHelper
    {
        public static bool DoReaming(Mesh meshForReam, Brep[] reamingEntities, out Mesh reamedMesh, out Mesh reamedPieces)
        {
            List<Mesh> reamingEntitiesMesh = new List<Mesh>();
            reamedPieces = null;
            reamedMesh = null;

            foreach (var br in reamingEntities)
            {
                foreach(var m in Mesh.CreateFromBrep(br))
                {
                    reamingEntitiesMesh.Add(m);
                }
            }

            return DoReaming(meshForReam, reamingEntitiesMesh.ToArray(), out reamedMesh, out reamedPieces); 
        }

        public static bool DoReaming(Mesh meshForReam, Mesh[] reamingEntities, out Mesh reamedMesh, out Mesh reamedPieces)
        {
            reamedPieces = null;
            reamedMesh = null;

            Mesh unitedReamingEntities;
            if(Booleans.PerformBooleanUnion(out unitedReamingEntities, reamingEntities))
            {
                reamedMesh = Booleans.PerformBooleanSubtraction(meshForReam, unitedReamingEntities);
                reamedPieces = Booleans.PerformBooleanIntersection(meshForReam, unitedReamingEntities);
                if (reamedMesh.IsValid && reamedPieces.IsValid)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        //////////////////////////////////////////////////////////////////////////
    }
}
