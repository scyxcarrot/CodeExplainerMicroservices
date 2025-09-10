using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Operations
{
    public partial class DisjointedShellEditor
    {
        private readonly DisjointedShellEditorDataModel _editorDataModel;

        private readonly DisjointedShellEditorConduit _editorConduit;

        public DisjointedShellEditor()
        {
            _editorDataModel = new DisjointedShellEditorDataModel();
            _editorConduit = new DisjointedShellEditorConduit(_editorDataModel);
        }

        private void PreparedDataModel(Mesh disjointableMesh, Guid id, DisplayMaterial ownDisplayMaterial, DisplayMaterial ownRejectMaterial, PreprocessMode preprocessMode)
        {
            IEnumerable<Mesh> meshesKeep;
            IEnumerable<Mesh> meshesRemove;
            PreprocessDataModel(disjointableMesh, preprocessMode, out meshesKeep, out meshesRemove);

            foreach (var mesh in meshesKeep)
            {
                _editorDataModel.Add(mesh, id, true, ownDisplayMaterial, ownRejectMaterial);
            }

            foreach (var mesh in meshesRemove)
            {
                _editorDataModel.Add(mesh, id,false, ownDisplayMaterial, ownRejectMaterial);
            }
        }

        protected virtual void PreprocessDataModel(Mesh disjointableMesh, PreprocessMode preprocessMode, out IEnumerable<Mesh> meshesKeep,
            out IEnumerable<Mesh> meshesRemove)
        {
            var disjointedMeshes = disjointableMesh.SplitDisjointPieces().ToList();
            if (preprocessMode == PreprocessMode.DeselectAll)
            {
                meshesKeep = new List<Mesh>();
                meshesRemove = disjointedMeshes;
            }
            else
            {
                meshesKeep = disjointedMeshes;
                meshesRemove = new List<Mesh>();
            }
        }

        public Dictionary<Guid, Mesh> Execute(Dictionary<Guid, Mesh> disjointableMeshesDictionary, 
            Dictionary<Guid, DisplayMaterial> ownRemainMaterialDictionary = null, Dictionary<Guid, DisplayMaterial> ownRejectMaterialDictionary = null,
            Dictionary<Guid, PreprocessMode> preprocessModes = null)
        {
            foreach (var meshDictionary in disjointableMeshesDictionary)
            {
                var mesh = meshDictionary.Value;
                if (meshDictionary.Value.DisjointMeshCount == 0 && meshDictionary.Value.Faces.Count > 0)
                {
                    MeshUtilities.RepairMesh(ref mesh);
                }

                var ownRemainMaterial = (ownRemainMaterialDictionary == null) ? null :
                    (!ownRemainMaterialDictionary.ContainsKey(meshDictionary.Key)) ? null :
                    ownRemainMaterialDictionary[meshDictionary.Key];
                var ownRejectMaterial = (ownRejectMaterialDictionary == null) ? null :
                    (!ownRejectMaterialDictionary.ContainsKey(meshDictionary.Key)) ? null :
                    ownRejectMaterialDictionary[meshDictionary.Key];
                var preprocessMode = (preprocessModes == null) ? PreprocessMode.Preprocess :
                    (!preprocessModes.ContainsKey(meshDictionary.Key)) ? PreprocessMode.Preprocess :
                    preprocessModes[meshDictionary.Key];

                var disjointableMeshCopy = mesh.DuplicateMesh();
                PreparedDataModel(disjointableMeshCopy, meshDictionary.Key, ownRemainMaterial, ownRejectMaterial, preprocessMode);
            }

            _editorConduit.Show = true;

            var getPoints = new GetPoint();

            getPoints.AcceptNothing(true);
            getPoints.SetCommandPrompt("Select/Deselect the parts that want to remove/keep (<Orange> = Remove; <Esc> to discard changed; <Enter> to finalized the selection)");
            var finalMesh = disjointableMeshesDictionary;

            while (true)
            {
                var res = getPoints.Get();
                if (res == GetResult.Cancel)
                {
                    break;
                }

                if (res == GetResult.Nothing)
                {
                    finalMesh = _editorDataModel.FinalPickedShell();
                    if (finalMesh != null)
                    {
                        break;
                    }
                    IDSPluginHelper.WriteLine(LogCategory.Warning, "User must selected at least one part to keep");
                }

                if (res == GetResult.Point)
                {
                    GetPickedDisjointedPart(getPoints.View().ActiveViewport, getPoints.Point2d());
                }
            }

            _editorConduit.Show = false;

            return finalMesh;
        }

        public void RemoveItem()
        {
            _editorDataModel.ClearItem();
        }

        public void CleanUp()
        {
            RemoveItem();
            _editorConduit.Enabled = false;
            _editorConduit.Dispose();
        }

        private void GetPickedDisjointedPart(RhinoViewport activeViewport, System.Drawing.Point selectedPoint)
        {
            var pickerContext = new PickContext();
            pickerContext.View = activeViewport.ParentView;
            pickerContext.PickStyle = PickStyle.PointPick;
            var transform = activeViewport.GetPickTransform(selectedPoint);
            pickerContext.SetPickTransform(transform);

            _editorDataModel.GetPickedDisjointedPartForToggle(pickerContext);
        }
    }
}
