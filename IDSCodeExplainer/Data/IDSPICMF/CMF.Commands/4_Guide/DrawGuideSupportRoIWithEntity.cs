using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using IDS.Core.Drawing;
using IDS.Core.Operations;
using IDS.Core.Utilities;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.PICMF.Helper
{
    public class DrawGuideSupportRoIWithEntity
    {
        private readonly DrawGuideWithEntityDataContext _dataContext;

        public Mesh RoIMesh { get; set; }

        public Mesh OperationConstraintMesh { get; set; }

        public List<Mesh> HighDefinitionMeshes { get; set; }

        private readonly RhinoDoc _doc;

        public DrawGuideSupportRoIWithEntity(RhinoDoc doc, Mesh constraintMesh)
        {
            _dataContext = new DrawGuideWithEntityDataContext();
            _doc = doc;

            OperationConstraintMesh = constraintMesh;
        }

        public bool Execute()
        {
            if (OperationConstraintMesh == null)
            {
                return false;
            }

            RoIMesh = null;

            var conduit = new DrawGuideWithEntityConduit(_dataContext);
            conduit.Enabled = true;

            while (true)
            {
                var getNext = new GetPoint();
                getNext.SetCommandPrompt("Press <Enter> to finalize, <Esc> to cancel, LMB to start.");
                getNext.AcceptNothing(true); // accept ENTER to confirm

                var result = getNext.Get();
                //GetResult.Nothing - user pressed enter 
                //GetResult.Cancel - user cancel string getting
                if (result == GetResult.Nothing)
                {
                    break;
                }
                else if (result == GetResult.Cancel)
                {
                    if (_dataContext.ContainsDrawing())
                    {
                        var dlgRes = MessageBox.Show(
                            "Pressing Esc will delete the drawings. Do you want to proceed?",
                            "Drawing Surface", MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation);

                        if (dlgRes == DialogResult.OK)
                        {
                            conduit.Enabled = false;
                            conduit.CleanUp();
                            return false;
                        }

                        continue;
                    }

                    conduit.Enabled = false;
                    conduit.CleanUp();
                    return false;
                }

                Plane reamingPlane;
                var pd = new PlaneDrawer();
                if (!pd.ThreePointPlane(OperationConstraintMesh, out reamingPlane))
                {
                    continue;
                }

                // Allow for plane rotation/translation with a gumball
                var planeSpan = 25;
                var xspan = new Interval(-planeSpan, planeSpan);
                var yspan = new Interval(-planeSpan, planeSpan);
                var gTransform = new GumballTransformPlane(_doc, false);
                Transform planeTransform; // this will save the rotation/translation done to the plane
                reamingPlane = gTransform.TransformPlane(reamingPlane, xspan, yspan, out planeTransform);

                // Enlarge the reamingPlane for drawing of the curve
                var largeSpan = new Interval(-200, 200); // Oversize so user can resize along edges
                var reamingSurface = new PlaneSurface(reamingPlane, largeSpan, largeSpan);

                var oa = _doc.CreateDefaultAttributes();
                oa.Visible = true;
                var midx = _doc.Materials.Find("TemporaryPlane", true);
                if (midx == -1)
                {
                    midx = _doc.Materials.Add();
                    var mat = _doc.Materials[midx];
                    mat.Transparency = 0.5;
                    mat.CommitChanges();
                }
                oa.MaterialIndex = midx;
                oa.MaterialSource = ObjectMaterialSource.MaterialFromObject;
                oa.ColorSource = ObjectColorSource.ColorFromMaterial;
                var reamingSurfaceId = _doc.Objects.AddSurface(reamingSurface, oa);
                _doc.Views.Redraw();

                // Draw and extrude curve
                var ce = new CurveExtruder();
                Brep patchExtruded;
                ce.SetExistingCurveId(Guid.Empty);
                var success = ce.ExtrudeCurve(_doc, reamingSurface, planeTransform, out patchExtruded);
                _doc.Objects.Delete(reamingSurfaceId, true);
                if (!success)
                {
                    continue;
                }

                _dataContext.Entities.Add(patchExtruded);
            }

            if (_dataContext.ContainsDrawing())
            {
                PrepareResult();
            }
            else
            {
                conduit.Enabled = false;
                conduit.CleanUp(); 
                return false;
            }

            conduit.Enabled = false;
            conduit.CleanUp(); 
            return true;
        }

        private void PrepareResult()
        {
            if (_dataContext.Entities.Any())
            {
                var entities = MeshUtilities.AppendMeshes(_dataContext.Entities.Select(e => MeshUtilities.ConvertBrepToMesh(e, true)));
                RoIMesh = Booleans.PerformBooleanIntersection(entities, MeshUtilities.AppendMeshes(HighDefinitionMeshes));
#if (INTERNAL)
                InternalUtilities.ReplaceObject(entities, "Intermediate - ReamingEntities"); //temporary
                InternalUtilities.ReplaceObject(RoIMesh, "Intermediate - RoIMesh from ReamingEntities"); //temporary
#endif
            }
        }
    }

    public class DrawGuideWithEntityDataContext
    {
        public List<Brep> Entities { get; private set; } = new List<Brep>();

        public bool ContainsDrawing()
        {
            var hasDrawing = false;
            hasDrawing |= Entities.Any();
            return hasDrawing;
        }
    }

    public class DrawGuideWithEntityConduit : DisplayConduit, IDisposable
    {
        private DrawGuideWithEntityDataContext _dataModel;
        private readonly DisplayMaterial _entityMaterial;

        public DrawGuideWithEntityConduit(DrawGuideWithEntityDataContext dataModel)
        {
            _dataModel = dataModel;
            _entityMaterial = CreateMaterial(0.25, Color.Green);
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            base.PostDrawObjects(e);

            DrawBreps(e, _dataModel.Entities, _entityMaterial);
        }

        private void DrawBreps(DrawEventArgs e, List<Brep> breps, DisplayMaterial material)
        {
            if (breps != null && breps.Any())
            {
                foreach (var brep in breps)
                {
                    e.Display.DrawBrepShaded(brep, material);
                }
            }
        }

        public void CleanUp()
        {
            _dataModel = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _entityMaterial.Dispose();
            }
        }

        private DisplayMaterial CreateMaterial(double transparency, Color color)
        {
            var displayMaterial = new DisplayMaterial
            {
                Transparency = transparency,
                Diffuse = color,
                Specular = color
            };

            return displayMaterial;
        }
    }
}