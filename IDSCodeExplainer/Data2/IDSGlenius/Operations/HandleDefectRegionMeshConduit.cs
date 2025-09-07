using IDS.Core.Utilities;
using IDSCore.Glenius.Drawing;
using Rhino;
using Rhino.Geometry;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static Rhino.Input.Custom.PickContext;

namespace IDSCore.Common
{
    public class HandleDefectRegionMeshConduit : GetPoint
    {
        private readonly DefectRegionMeshConduit meshConduit;
        private readonly Mesh mergedMeshes;

        public HandleDefectRegionMeshConduit(DefectRegionMeshConduit meshConduits)
        {
            this.meshConduit = meshConduits;
            SetCommandPrompt("Left-click/Alt+left-click to mark/un-mark Defect Region, or Enter to remove defect region");

            List<Mesh> allMesh = new List<Mesh>();
            allMesh = allMesh.Concat(meshConduits.DefectRegions).
                Concat(meshConduits.NonDefectRegions).ToList();

            Booleans.PerformBooleanUnion(out mergedMeshes, allMesh.ToArray());
        }

        //Defect to Non Defect
        private bool MoveRegion(PickContext picker, List<Mesh> from, List<Mesh> to)
        {
            List<Mesh> allMesh = new List<Mesh>();
            allMesh = allMesh.Concat(from).
                Concat(to).ToList();

            //Handle Defect Region
            double depth, distance;
            Point3d hitPoint;
            MeshHitFlag hitFlag;
            int hitIndex;

            if(mergedMeshes != null)
            {
                //If it hits
                if (picker.PickFrustumTest(mergedMeshes, PickContext.MeshPickStyle.ShadedModePicking, out hitPoint, out depth, out distance, out hitFlag, out hitIndex))
                {
                    var cmesh = MeshUtilities.GetClosestMesh(allMesh.ToArray(), hitPoint);

                    if (cmesh != null && from.Find(x => (x == cmesh)) != null)
                    {
                        from.Remove(cmesh);
                        to.Add(cmesh);
                        return true; //Something has changed
                    }
                }

                return false;
            }

            return false;
        }

        protected override void OnMouseDown(GetPointMouseEventArgs e)
        {
            base.OnMouseDown(e);
            var picker = new PickContext();
            picker.View = e.Viewport.ParentView;
            picker.PickStyle = PickStyle.PointPick;

            var xform = e.Viewport.GetPickTransform(e.WindowPoint);
            picker.SetPickTransform(xform);

            bool update = false;

            if(Control.ModifierKeys == Keys.Alt && e.LeftButtonDown)
            {
                update = MoveRegion(picker, meshConduit.DefectRegions, meshConduit.NonDefectRegions);
            }
            else if (e.LeftButtonDown)
            {
                update = MoveRegion(picker, meshConduit.NonDefectRegions, meshConduit.DefectRegions);
            }

            if(update)
            {
                RhinoDoc.ActiveDoc.Views.Redraw();
            }
            
        }


    }
}
