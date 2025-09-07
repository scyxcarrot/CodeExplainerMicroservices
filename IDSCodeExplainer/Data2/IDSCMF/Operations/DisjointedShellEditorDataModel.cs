using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public partial class DisjointedShellEditor
    {
        public enum PreprocessMode
        {
            Preprocess,
            SelectAll,
            DeselectAll,
        }

        protected class DisjointedShellInfo
        {
            public readonly Mesh DisjointedMesh;
            public readonly BoundingBox DisjointedMeshBoundingBox;
            public readonly Guid Id;
            public readonly DisplayMaterial OwnRemainMaterial;
            public readonly DisplayMaterial OwnRejectMaterial;
            public bool Keep { get; private set; }

            public DisjointedShellInfo(Mesh meshDisplay, Guid id, bool keep, DisplayMaterial ownRemainMaterial, DisplayMaterial ownRejectMaterial)
            {
                DisjointedMesh = meshDisplay;
                DisjointedMeshBoundingBox = DisjointedMesh.GetBoundingBox(true);
                Id = id;
                OwnRemainMaterial = ownRemainMaterial;
                OwnRejectMaterial = ownRejectMaterial;
                Keep = keep;
            }

            public void ToggleKeepStatus()
            {
                Keep = !Keep;
            }
        }

        protected class DisjointedShellEditorDataModel
        {
            private readonly List<DisjointedShellInfo> _disjointedShellInfos;

            public DisjointedShellEditorDataModel()
            {
                _disjointedShellInfos = new List<DisjointedShellInfo>();
            }

            public void Add(Mesh disjointedMesh, Guid id, bool keep, DisplayMaterial ownRemainMaterial, DisplayMaterial ownRejectMaterial)
            {
                _disjointedShellInfos.Add(new DisjointedShellInfo(disjointedMesh, id, keep, ownRemainMaterial, ownRejectMaterial));
            }

            public void ClearItem()
            {
                _disjointedShellInfos.Clear();
            }

            public IEnumerable<DisjointedShellInfo> GetDisjointedShellInfos()
            {
                return _disjointedShellInfos.ToList();
            }

            public void GetPickedDisjointedPartForToggle(PickContext pickerContext)
            {
                var refDepth = double.MinValue;
                DisjointedShellInfo pickedDisjointedPart = null;

                foreach (var disjointedShellInfo in _disjointedShellInfos)
                {
                    bool boxCompletelyInFrustum;
                    if (!pickerContext.PickFrustumTest(disjointedShellInfo.DisjointedMeshBoundingBox,
                        out boxCompletelyInFrustum))
                    {
                        continue;
                    }

                    var mesh = disjointedShellInfo.DisjointedMesh;

                    var t1 = !pickerContext.PickFrustumTest(mesh, PickContext.MeshPickStyle.ShadedModePicking, out _,
                        out var depth, out var distance, out _, out _);
                    var t2 = !(Math.Abs(distance) < double.Epsilon);
                    var t3 = !(depth > refDepth);

                    //depth returned here for point picks LARGER values are NEARER to the camera. SMALLER values are FARTHER from the camera.
                    if (t1 || t2 || t3)
                    {
                        continue;
                    }

                    refDepth = depth;
                    pickedDisjointedPart = disjointedShellInfo;
                }

                pickedDisjointedPart?.ToggleKeepStatus();
            }

            public Dictionary<Guid, Mesh> FinalPickedShell()
            {
                var remainMesh = _disjointedShellInfos.Where(d => d.Keep).ToList();

                if (!remainMesh.Any())
                {
                    return null;
                }

                var finalMeshesDictionary = new Dictionary<Guid, Mesh>();

                foreach (var mesh in remainMesh)
                {
                    if (!finalMeshesDictionary.ContainsKey(mesh.Id))
                    {
                        finalMeshesDictionary.Add(mesh.Id, new Mesh());
                    }

                    var finalMesh = finalMeshesDictionary[mesh.Id];
                    finalMesh.Append(mesh.DisjointedMesh);
                }

                return finalMeshesDictionary;
            }
        }
    }
}
