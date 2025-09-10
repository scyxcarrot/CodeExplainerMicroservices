using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.PICMF.Forms;
using IDS.PICMF.Visualization;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Input;
using Point = System.Drawing.Point;

namespace IDS.PICMF.Operations
{
    public class ImplantSupportRoIMetalInputGetter
    {
        private readonly List<KeyValuePair<Mesh, Color>> _metalMeshesAndColor;
        private Point _startPoint2d = Point.Empty;
        private Rectangle _rectangle = Rectangle.Empty;

        public ImplantSupportRoIMetalInputGetter(RhinoDoc document)
        {
            var plannedMetalRhinoObjects = ProPlanImportUtilities.GetAllProplanPartsAsRangePartType(
                document, ProplanBoneType.Planned, new List<ProPlanImportPartType>()
            {
                ProPlanImportPartType.Metal
            });

            var plannedMetalMeshesDictionary = plannedMetalRhinoObjects.Where(r => r.Geometry is Mesh)
                .ToDictionary(r => (Mesh) r.Geometry, r=>r.Attributes.ObjectColor);
            _metalMeshesAndColor = new List<KeyValuePair<Mesh, Color>>();

            foreach (var plannedMetalMeshAndColor in plannedMetalMeshesDictionary)
            {
                var plannedMetalMesh = plannedMetalMeshAndColor.Key;
                var plannedMetalDefaultColor = plannedMetalMeshAndColor.Value;

                switch (plannedMetalMesh.DisjointMeshCount)
                {
                    case 0:
                        break;
                    case 1:
                        _metalMeshesAndColor.Add(new KeyValuePair<Mesh, Color>(plannedMetalMesh, plannedMetalDefaultColor));
                        break;
                    default:
                        var singleShellMetals = plannedMetalMesh.SplitDisjointPieces();
                        _metalMeshesAndColor.AddRange(singleShellMetals.Select(m =>
                            new KeyValuePair<Mesh, Color>(m, plannedMetalDefaultColor)));
                        break;
                }
            }
        }

        public Result SelectMetal(out List<MetalIntegrationInfo> integratedMetalInfos)
        {
            var metalConduits = _metalMeshesAndColor.Select(
                kv => new ImplantSupportRoIMetalInputConduit(kv.Key, kv.Value)
                {
                    Enabled = true
                }).ToList();

            ConduitUtilities.RefeshConduit();

            integratedMetalInfos = new List<MetalIntegrationInfo>();

            var metalSelector = new GetPoint();
            metalSelector.SetCommandPrompt("Select metal to Remove/Remain (Press SHIFT to unselected)");
            metalSelector.AcceptNothing(true);
            metalSelector.EnableTransparentCommands(false); 
            var modes = new[]
            {
                EMetalIntegrationState.Remove,
                EMetalIntegrationState.Remain,
            };

            var mode = EMetalIntegrationState.Remove;
            metalSelector.AddOptionEnumList("Mode", mode, modes);

            metalSelector.MouseDown += GetObject_MouseDown;
            metalSelector.MouseMove += GetPointOnMouseMove;
            metalSelector.DynamicDraw += GetPointOnDynamicDraw;
            Result result;
            
            while (true)
            {
                _startPoint2d = Point.Empty;
                _rectangle = Rectangle.Empty;

                var getResult = metalSelector.Get(true);
                if (getResult == GetResult.Option)
                {
                    mode = modes[metalSelector.Option().CurrentListOptionIndex];
                }
                else if(getResult == GetResult.Point)
                {
                    var isShiftKeyPressing = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));
                    var viewport = metalSelector.View().ActiveViewport;
                    var isPointPick = _rectangle.IsEmpty;

                    if (isPointPick) // Split into 2 function for easier review
                    {
                        DoPointPickMetal(metalConduits, viewport, metalSelector.Point2d(), isShiftKeyPressing, mode); 
                    }
                    else
                    {
                        DoWindowPickMetal(metalConduits, viewport, _rectangle, isShiftKeyPressing, mode);
                    }
                }
                else if (getResult == GetResult.Cancel || getResult == GetResult.Nothing)
                {
                    result = (getResult == GetResult.Nothing) ? Result.Success : Result.Cancel;
                    break;
                }
            }

            metalSelector.MouseDown -= GetObject_MouseDown;
            metalSelector.MouseMove -= GetPointOnMouseMove;
            metalSelector.DynamicDraw -= GetPointOnDynamicDraw;

            foreach (var metalConduit in metalConduits)
            {
                if (result == Result.Success)
                {
                    switch (metalConduit.State)
                    {
                        case EMetalIntegrationState.Remain:
                        case EMetalIntegrationState.Remove:
                            integratedMetalInfos.Add(new MetalIntegrationInfo()
                            {
                                SelectedMesh = metalConduit.MetalMesh,
                                State = metalConduit.State
                            });
                            break;
                    }
                }
                metalConduit.Enabled = false;
                metalConduit.Dispose();
            }
            return result;
        }

        private void DoPointPickMetal(IEnumerable<ImplantSupportRoIMetalInputConduit> metalConduits, RhinoViewport viewport, Point point2d, bool isShiftKeyPressing, EMetalIntegrationState mode)
        {
            var depthMax = double.MinValue;
            ImplantSupportRoIMetalInputConduit pointPickedMetalConduit = null;
            var picker = new PickContext
            {
                View = viewport.ParentView,
                PickStyle = PickStyle.PointPick
            };
            var xform = viewport.GetPickTransform(point2d);
            picker.SetPickTransform(xform);

            foreach (var metalConduit in metalConduits)
            {
                if (!isShiftKeyPressing && metalConduit.State == mode) //skip the metal already selected
                {
                    continue;
                }

                if (!picker.PickFrustumTest(metalConduit.MetalMesh.GetBoundingBox(false), out _))
                {
                    continue;
                }

                if (picker.PickFrustumTest(metalConduit.MetalMesh, PickContext.MeshPickStyle.ShadedModePicking,
                    out _, out var depth, out _, out _, out _))
                {
                    if (depth > depthMax)
                    {
                        depthMax = depth;
                        pointPickedMetalConduit = metalConduit;
                    }
                }
            }
            if (pointPickedMetalConduit != null)
            {
                pointPickedMetalConduit.State = isShiftKeyPressing ? EMetalIntegrationState.Unselected : mode;
            }
        }

        private void DoWindowPickMetal(IEnumerable<ImplantSupportRoIMetalInputConduit> metalConduits, RhinoViewport viewport, Rectangle rectangle, bool isShiftKeyPressing, EMetalIntegrationState mode)
        {
            var picker = new PickContext
            {
                View = viewport.ParentView,
                PickStyle = PickStyle.PointPick
            };
            var xform = viewport.GetPickTransform(rectangle);
            picker.SetPickTransform(xform);

            foreach (var metalConduit in metalConduits)
            {
                if (!isShiftKeyPressing && metalConduit.State == mode) //skip the metal already selected
                {
                    continue;
                }

                if (!picker.PickFrustumTest(metalConduit.MetalMesh.GetBoundingBox(false), out _))
                {
                    continue;
                }

                if (picker.PickFrustumTest(metalConduit.MetalMesh, PickContext.MeshPickStyle.ShadedModePicking,
                    out _, out _, out _, out _, out _))
                {
                    metalConduit.State = isShiftKeyPressing ? EMetalIntegrationState.Unselected : mode;
                }
            }
        }

        private void GetObject_MouseDown(object sender, GetPointMouseEventArgs e)
        {
            _startPoint2d = e.WindowPoint;
        }

        private void GetPointOnMouseMove(object sender, GetPointMouseEventArgs e)
        {
            if (!_startPoint2d.IsEmpty)
            {
                var endPoint2d = e.WindowPoint;
                var size = new Size(endPoint2d.X - _startPoint2d.X, endPoint2d.Y - _startPoint2d.Y);
                if (size.IsEmpty)
                {
                    return;
                }

                _rectangle = new Rectangle(_startPoint2d, size);
            }
        }

        private void GetPointOnDynamicDraw(object sender, GetPointDrawEventArgs e)
        {
            if (!_rectangle.IsEmpty)
            {
                e.Display.Draw2dRectangle(_rectangle, Color.Gray, 1, Color.Transparent);
            }
        }
    }
}
