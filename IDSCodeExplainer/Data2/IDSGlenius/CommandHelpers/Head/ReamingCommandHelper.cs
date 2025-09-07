using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Geometry;
using System.Collections.Generic;

namespace IDS.Glenius.CommandHelpers
{
    public class ReamingCommandHelper
    {
        private readonly GleniusObjectManager objManager;
        private readonly RhinoDoc doc;

        public ReamingCommandHelper(RhinoDoc doc, GleniusObjectManager objManager)
        {
            this.doc = doc;
            this.objManager = objManager;
        }

        private void UpdateDocumentScapulaDesignReamed(Mesh scapulaDesignReamedMesh, IBB destinationBlock)
        {
            if (objManager.HasBuildingBlock(destinationBlock))
            {
                var oldID = objManager.GetBuildingBlockId(destinationBlock);
                objManager.SetBuildingBlock(destinationBlock, scapulaDesignReamedMesh, oldID);
            }
            else
            {
                objManager.AddNewBuildingBlock(destinationBlock, scapulaDesignReamedMesh);
            }

            doc.Views.Redraw();
        }

        //IBB.Scapula, IBB.ReamingEntity, IBB.RBVExtra, IBB.ScapulaDesignReamed
        public bool PerformReaming(IBB originalBlockToBeReamed, IBB reamingEntityBlock, IBB RBVBlock, IBB destinationBlock)
        {
            Mesh originalMeshToBeReamed = objManager.GetBuildingBlock(originalBlockToBeReamed).Geometry as Mesh;

            Mesh meshToBeReamed = new Mesh();
            meshToBeReamed.CopyFrom(originalMeshToBeReamed);

            List<Mesh> reamingEntity = new List<Mesh>();

            if (objManager.HasBuildingBlock(reamingEntityBlock))
            {
                foreach (var obj in objManager.GetAllBuildingBlocks(reamingEntityBlock))
                {
                    var objBrep = obj.Geometry as Brep;

                    foreach (var m in Mesh.CreateFromBrep(objBrep))
                    {
                        reamingEntity.Add(m);
                    }
                }
            }
            else //If no IBB.ReamingEntity found, delete any IBB.RBVExtra
            {
                objManager.DeleteBuildingBlock(RBVBlock);

                UpdateDocumentScapulaDesignReamed(meshToBeReamed, destinationBlock);
                return true;
            }

            //Do Reaming
            Mesh reamedMesh;
            Mesh reamedPieces;
            if (ReamingHelper.DoReaming(meshToBeReamed, reamingEntity.ToArray(), out reamedMesh, out reamedPieces))
            {
                //Add reamed pieces into document
                if (objManager.HasBuildingBlock(RBVBlock))
                {
                    var oldID = objManager.GetBuildingBlockId(RBVBlock);
                    objManager.SetBuildingBlock(RBVBlock, reamedPieces, oldID);
                }
                else
                {
                    objManager.AddNewBuildingBlock(RBVBlock, reamedPieces);
                }

                //Add reamed mesh into document, checks if there're existing or not
                if (reamedMesh != null)
                {
                    UpdateDocumentScapulaDesignReamed(reamedMesh, destinationBlock);
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
