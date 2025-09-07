using IDS.CMF.Visualization;
using IDS.Core.Drawing;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using RhinoMtlsCore.Operations;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
#if (INTERNAL)
using IDS.Core.NonProduction;
#endif

namespace IDS.CMF.Operations
{
    public class MeshTrimmerDataContext
    {
        public bool Trimmed { get; set; } = false;
        public List<Mesh> Shells { get; set; }
        public List<MeshConduit> Conduits { get; set; }
    }

    public class MultipleMeshesTrimmer
    {
        private readonly Dictionary<Guid, MeshTrimmerDataContext> _availableMeshesDataContext;
        private static readonly Color ConduitColor = Color.DeepPink;
        private const double ConduitTransparencies = 0.4;

        public MultipleMeshesTrimmer(Dictionary<Guid, Mesh> _availableMeshes)
        {
            _availableMeshesDataContext = new Dictionary<Guid, MeshTrimmerDataContext>();

            foreach (var availableMesh in _availableMeshes)
            {
                var id = availableMesh.Key;
                var mesh = availableMesh.Value;
                var disjointedMeshes = mesh.SplitDisjointPieces().ToList();
                var meshConduits = disjointedMeshes.Select(m =>
                {
                    var conduit=new MeshConduit();
                    conduit.SetMesh(m, ConduitColor, ConduitTransparencies);
                    return conduit;
                }).ToList();

                _availableMeshesDataContext.Add(id, new MeshTrimmerDataContext()
                {
                    Shells = disjointedMeshes,
                    Conduits = meshConduits
                });
            }
        }

        public bool Execute(RhinoDoc doc, string command, out Dictionary<Guid, Mesh> trimmedMeshes)
        {
            trimmedMeshes = new Dictionary<Guid, Mesh>();

            foreach (var conduit in _availableMeshesDataContext.Values.SelectMany(
                meshTrimmerDataContext => meshTrimmerDataContext.Conduits))
            {
                conduit.Enabled = true;
            }

            doc.Views.Redraw();
            var shellPicker = new GetPoint();
            shellPicker.SetCommandPrompt(command);
            shellPicker.AcceptNothing(true);
            shellPicker.EnableTransparentCommands(false);

            var res = false;
            while (true)
            {
                var getResult = shellPicker.Get();
                if (getResult == GetResult.Cancel)
                {
                    break;
                }
                
                if (getResult == GetResult.Nothing)
                {
                    res = _availableMeshesDataContext.Any();
                    foreach (var meshTrimmerDataContext in _availableMeshesDataContext)
                    {
                        if (!meshTrimmerDataContext.Value.Trimmed)
                        {
                            continue;
                        }

                        var trimmedMeshesList = meshTrimmerDataContext.Value.Shells.Where(s => s != null);
                        trimmedMeshes.Add(meshTrimmerDataContext.Key,
                            trimmedMeshesList.Any()
                                ? MeshUtilities.AppendMeshes(meshTrimmerDataContext.Value.Shells)
                                : null);
                    }
                    break;
                }

                if (getResult != GetResult.Point)
                {
                    continue;
                }

                if (!FindPickShell(shellPicker.View().ActiveViewport, shellPicker.Point2d(), out var id,
                    out var pickedShellIdx))
                {
                    continue;
                }

                var selectedMesh = _availableMeshesDataContext[id].Shells[pickedShellIdx];
                _availableMeshesDataContext[id].Conduits[pickedShellIdx].Enabled = false;
                var trimmer = new MeshTrimmer(doc, selectedMesh);
                if (!trimmer.Execute())
                {
                    _availableMeshesDataContext[id].Conduits[pickedShellIdx].Enabled = true;
                    doc.Views.Redraw();
                    continue;
                }

                _availableMeshesDataContext[id].Trimmed = true;
                _availableMeshesDataContext[id].Shells[pickedShellIdx] = trimmer.TrimmedMesh;
                if (trimmer.TrimmedMesh != null)
                {
                    _availableMeshesDataContext[id].Conduits[pickedShellIdx]
                        .SetMesh(trimmer.TrimmedMesh, ConduitColor, ConduitTransparencies);
                    _availableMeshesDataContext[id].Conduits[pickedShellIdx].Enabled = true;
                    doc.Views.Redraw();
                }
            }

            foreach (var conduit in _availableMeshesDataContext.Values.SelectMany(
                meshTrimmerDataContext => meshTrimmerDataContext.Conduits))
            {
                conduit.Enabled = false;
            }

            return res;
        }

        private bool FindPickShell(RhinoViewport viewport, System.Drawing.Point point2d, out Guid id, out int pickedShellIdx)
        {
            var depthMax = double.MinValue;
            id = Guid.Empty;
            pickedShellIdx = -1;
            var res = false;

            var picker = new PickContext
            {
                View = viewport.ParentView,
                PickStyle = PickStyle.PointPick
            };
            var xform = viewport.GetPickTransform(point2d);
            picker.SetPickTransform(xform);

            foreach (var availableMeshDataContext in _availableMeshesDataContext)
            {
                for (var i = 0; i < availableMeshDataContext.Value.Shells.Count; i++)
                {
                    var shell = availableMeshDataContext.Value.Shells[i];

                    if (shell == null)
                    {
                        continue;
                    }

                    if (!picker.PickFrustumTest(shell.GetBoundingBox(false), out _)||
                        !picker.PickFrustumTest(shell, PickContext.MeshPickStyle.ShadedModePicking, out _, out var depth, out _, out _, out _) ||
                        !(depth > depthMax))
                    {
                        continue;
                    }

                    depthMax = depth;
                    id = availableMeshDataContext.Key;
                    pickedShellIdx = i;
                    res = true;
                }
            }
            return res;
        }

        public static bool IsSubSetOf(IEnumerable<Guid> superset, IEnumerable<Guid> subset)
        {
            var contain = true;
            foreach (var guid in subset)
            {
                contain &= superset.Contains(guid);
                if (!contain)
                {
                    break;
                }
            }

            return contain;
        }
    }

    public class MeshTrimmer
    {
        private readonly DrawGuideOnPlaneDataContext _dataContext;

        public Mesh TrimmedMesh { get; set; }

        public Mesh OperationConstraintMesh { get; private set; }

        private readonly RhinoDoc _doc;
        private readonly Mesh _boundingBoxMesh;

        public MeshTrimmer(RhinoDoc doc, Mesh constraintMesh)
        {
            OperationConstraintMesh = constraintMesh;
            var boundingBox = OperationConstraintMesh.GetBoundingBox(true);
            _boundingBoxMesh = Mesh.CreateFromBox(boundingBox, 100, 100, 100);

            _dataContext = new DrawGuideOnPlaneDataContext
            {
                PreviewMesh = constraintMesh
            };

            _doc = doc;
        }

        public bool Execute()
        {
            if (OperationConstraintMesh == null)
            {
                return false;
            }

            return Executing();
        }

        private bool Executing()
        {
            TrimmedMesh = null;

            var conduit = new DrawGuideOnPlaneConduit(_dataContext)
            {
                Enabled = true
            };
            _doc.Views.Redraw();

            while (true)
            {
                var getNext = new GetPoint();
                getNext.Constrain(_boundingBoxMesh, true);
                getNext.SetCommandPrompt("LMB to start drawing curve, Press <Enter> to finalize trimmed parts, <Esc> to discard changes to current parts.");
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

                var drawer = new DrawCurveOnPlane(_doc, OperationConstraintMesh, getNext.Point());
                conduit.Drawer = drawer;
                var contour = drawer.Draw();
                conduit.Drawer = null;
                if (contour == null || !contour.IsClosed)
                {
                    continue;
                }

                var data = new GuideOnPlaneData(contour, drawer.GetConstraintPlane(), drawer.GetPointList());

                if (PrepareResult(data))
                {
                    _dataContext.Surfaces.Add(data);
                    if (TrimmedMesh == null)
                    {
                        break;
                    }
                }
            }

            conduit.Enabled = false;
            conduit.CleanUp();
            return _dataContext.ContainsDrawing();
        }

        private bool PrepareResult(GuideOnPlaneData data)
        {
            Mesh outputMesh = null;

            try
            {
                var ops = new TrimIntoHalves();
                outputMesh = ops.PerformTrimToGetOuterMesh(OperationConstraintMesh, data.PointList, data.Plane);
            }
            catch
            {
                IDSPluginHelper.WriteLine(LogCategory.Error, "Error while trim processing...");
                return false;
            }

#if (INTERNAL)
            InternalUtilities.AddCurve(data.Contour, $"Contour", "Intermediate - TrimEntities", Color.Red);  //temporary
#endif

            if (!outputMesh.Vertices.Any() || !outputMesh.Faces.Any())
            {
                IDSPluginHelper.WriteLine(LogCategory.Default, "No parts left after trimmed. Please select the other shell for trim");
                TrimmedMesh = null;
                OperationConstraintMesh = null;
                _dataContext.PreviewMesh = null;
                return true;
            }

            TrimmedMesh = outputMesh;

#if (INTERNAL)
            InternalUtilities.ReplaceObject(TrimmedMesh, "Intermediate - TrimmedMesh from TrimEntities"); //temporary
#endif
            OperationConstraintMesh = outputMesh;
            _dataContext.PreviewMesh = outputMesh;
            return true;
        }
    }
}
