using IDS.CMF;
using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using IDS.CMF.Visualization;
using IDS.Core.CommandBase;
using IDS.Core.ImplantBuildingBlocks;
using IDS.Core.PluginHelper;
using Rhino;
using Rhino.DocObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Visualization
{
    public abstract class CMFVisualizationComponentBase : ICommandVisualizationComponent
    {
        private IDictionary<string, bool> _visibilitySnapshot = null;
        private IDictionary<string, double> _transparencySnapshot = null;

        public abstract void OnCommandBeginVisualization(RhinoDoc doc);

        public abstract void OnCommandCanceledVisualization(RhinoDoc doc);

        public abstract void OnCommandFailureVisualization(RhinoDoc doc);

        public abstract void OnCommandSuccessVisualization(RhinoDoc doc);

        protected CMFImplantDirector GetDirector(RhinoDoc doc)
        {
            return IDSPluginHelper.GetDirector<CMFImplantDirector>(doc.DocumentId);
        }

        protected void HandlePreOpLayerVisibility(RhinoDoc doc, bool isVisible)
        {
            SetLayerVisibility(ProPlanImport.PreopLayer, doc, isVisible);
        }

        protected void HandleOriginalLayerVisibility(RhinoDoc doc, bool isVisible)
        {
            SetLayerVisibility(ProPlanImport.OriginalLayer, doc, isVisible);
        }

        protected void HandlePlannedLayerVisibility(RhinoDoc doc, bool isVisible)
        {
            SetLayerVisibility(ProPlanImport.PlannedLayer, doc, isVisible);
        }

        public void SetBuildingBlockLayerVisibility(ExtendedImplantBuildingBlock eBlock, RhinoDoc document, bool isVisible)
        {
            SetLayerVisibility(eBlock.Block.Layer, document, isVisible);
        }

        protected void SetAllImplantExtendedBuildingBlockLayerVisibility(IBB block, RhinoDoc doc, bool isVisible)
        {
            var objManager = new CMFObjectManager(GetDirector(doc));

            var blocks = objManager.GetAllImplantExtendedImplantBuildingBlocks(block);
            blocks.ForEach(b =>
            {
                SetBuildingBlockLayerVisibility(b, doc, isVisible);
            });
        }

        public void SetAllImplantExtendedBuildingBlockTransparency(IBB block, RhinoDoc doc,
            double transparency = 0.0)
        {
            var objManager = new CMFObjectManager(GetDirector(doc));
            foreach (var implantBuildingBlock in objManager.GetAllImplantBuildingBlocks(block))
            {
                ImplantBuildingBlockProperties.SetTransparency(implantBuildingBlock, doc, transparency);
            }
        }

        protected void SetAllGuideExtendedBuildingBlockLayerVisibility(IBB block, RhinoDoc doc, bool isVisible)
        {
            var objManager = new CMFObjectManager(GetDirector(doc));

            var director = GetDirector(doc);
            var blocks = objManager.GetAllGuideExtendedImplantBuildingBlocks(block, director.CasePrefManager.GuidePreferences);
            blocks.ForEach(b =>
            {
                SetBuildingBlockLayerVisibility(b, doc, isVisible);
            });
        }

        protected void SetBuildingBlockLayerVisibility(IBB block, RhinoDoc doc, bool isVisible)
        {
            var layer = BuildingBlocks.Blocks[block].Layer;

            SetLayerVisibility(layer, doc, isVisible);
        }

        protected List<Guid> FindParentLayers(string fullLayerPath, RhinoDoc doc)
        {
            var res = new List<Guid>();

            while (true)
            {
                var index = doc.Layers.FindByFullPath(fullLayerPath, true);
                var newLayerSettings = doc.Layers[index];

                var parentId = newLayerSettings.ParentLayerId;

                if (parentId == Guid.Empty || res.Contains(parentId))
                {
                    return res;
                }

                res.Add(parentId);
                var parentIndex = doc.Layers.Find(parentId, false);
                var parentLayerSettings = doc.Layers[parentIndex];
                fullLayerPath = parentLayerSettings.FullPath;
            }
        }

        protected void SetLayerVisibility(string fullLayerPath, RhinoDoc doc, bool isVisible)
        {
            var director = GetDirector(doc);
            int index = doc.Layers.FindByFullPath(fullLayerPath, true);

            var prevUndoRecordingEnabled = doc.UndoRecordingEnabled;
            doc.UndoRecordingEnabled = false;

            if (index >= 0)
            {
                var newLayerSettings = director.Document.Layers[index];

                if (isVisible)
                {
                    if (!newLayerSettings.IsVisible && newLayerSettings.IsValid)
                    {
                        doc.Layers.ForceLayerVisible(newLayerSettings.Id);
                    }
                }
                else
                {
                    newLayerSettings.IsVisible = false;
                    if (newLayerSettings.GetChildren() == null)
                    {
                        //Set persistent visibility to false for child layer when it is set to IsVisible = false
                        //This is to make sure that when parent layer is turned on(after being off), child layer remain off
                        newLayerSettings.SetPersistentVisibility(false);
                    }
                }

            }

            doc.Views.Redraw();
            doc.UndoRecordingEnabled = prevUndoRecordingEnabled;
        }

        protected void HideAllLayerVisibility(RhinoDoc doc)
        {
            Visibility.SetVisualization(doc, new Dictionary<IBB, double>());
        }

        private IDictionary<string, bool> GetLayersVisibility(IEnumerable<Layer> layers)
        {
            var visibilitySnapshot = new Dictionary<string, bool>();

            foreach (var layer in layers)    
            {
                if (layer.IsDeleted)
                {
                    continue;
                }

                if (visibilitySnapshot.ContainsKey(layer.FullPath))
                {
                    continue;
                }
                visibilitySnapshot.Add(layer.FullPath, layer.IsVisible);
            }

            return visibilitySnapshot;
        }

        private IDictionary<string, double> GetTransparencySnapshot(RhinoDoc doc)
        {
            var transparencySnapshot = new Dictionary<string, double>();

            foreach (var mat in doc.Materials)
            {
                if (mat.IsDeleted || transparencySnapshot.ContainsKey(mat.Name))
                {
                    continue;
                }

                transparencySnapshot.Add(mat.Name, mat.Transparency);
            }

            return transparencySnapshot;
        }

        protected void SnapshotVisualisation(RhinoDoc doc)
        {
            _visibilitySnapshot = GetLayersVisibility(doc.Layers);
            _transparencySnapshot = GetTransparencySnapshot(doc);
        }

        protected void StoreLayerVisibility(List<ExtendedImplantBuildingBlock> blocks, RhinoDoc doc)
        {
            var layerList = new List<Layer>();
            
            foreach (var extendedImplantBuildingBlock in blocks)
            {
                var index = doc.Layers.FindByFullPath(extendedImplantBuildingBlock.Block.Layer, -1);
                if (index >= 0)
                {
                    layerList.Add(doc.Layers[index]);
                }
            }

            _visibilitySnapshot = GetLayersVisibility(layerList);
        }

        private void RestoreTransparency(RhinoDoc doc, IDictionary<string, double> transparencySnapshot)
        {
            var prevUndoRecordingEnabled = doc.UndoRecordingEnabled;
            doc.UndoRecordingEnabled = false;

            foreach (var mat in doc.Materials)
            {
                if (mat.IsDeleted)
                {
                    continue;
                }

                if (transparencySnapshot.TryGetValue(mat.Name, out var transparencyValue))
                {
                    if (Math.Abs(mat.Transparency - transparencyValue) > Transparency.Epsilon)
                    {
                        mat.Transparency = transparencyValue;
                        mat.CommitChanges();
                    }
                }
            }

            doc.UndoRecordingEnabled = prevUndoRecordingEnabled;
        }

        private void RestoreLayersVisibility(RhinoDoc doc, IDictionary<string, bool> visibilitySnapshot)
        {
            var newVisibilitySnapshot = GetLayersVisibility(doc.Layers)
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var prevUndoRecordingEnabled = doc.UndoRecordingEnabled;
            doc.UndoRecordingEnabled = false;

            foreach (var individualSnapshot in visibilitySnapshot)
            {
                if (newVisibilitySnapshot.TryGetValue(individualSnapshot.Key, out var newVisibility))
                {
                    if (individualSnapshot.Value == newVisibility)
                    {
                        continue;
                    }
                }
                SetLayerVisibility(individualSnapshot.Key, doc, individualSnapshot.Value);
            }

            doc.UndoRecordingEnabled = prevUndoRecordingEnabled;
        }

        public void RestoreVisualisation(RhinoDoc doc, bool restoreTransparency = true)
        {
            if (_visibilitySnapshot == null ||(restoreTransparency && _transparencySnapshot == null))
            {
                return;
            }
            RestoreLayersVisibility(doc, _visibilitySnapshot);
            if (restoreTransparency)
            {
                RestoreTransparency(doc, _transparencySnapshot);
            }
        }

        protected void SetPartTypeVisibility(ProPlanImportPartType partType, string parentLayerName, RhinoDoc doc, bool isVisible)
        {
            SetRangePartTypesVisibility(new List<ProPlanImportPartType>() {partType}, parentLayerName, doc, isVisible);
        }

        protected void SetRangePartTypesVisibility(IEnumerable<ProPlanImportPartType> partTypes, string parentLayerName, RhinoDoc doc, bool isVisible)
        {
            var fullLayersName = ProPlanImportUtilities.GetFullLayerNamesByRangePartType(partTypes, parentLayerName);
            foreach (var fullLayerName in fullLayersName)
            {
                SetLayerVisibility(fullLayerName, doc, isVisible);
            }
        }

        protected void HandlePlannedLayerAndChildrenVisibility(RhinoDoc doc, bool isVisible)
        {
            var index = doc.Layers.FindByFullPath(ProPlanImport.PlannedLayer, true);

            if (index >= 0)
            {
                var parentLayer = doc.Layers[index];
                SetLayerVisibility(parentLayer.FullPath, doc, isVisible);

                var layers = parentLayer.GetChildren();
                // only one layer of children
                foreach (var layer in layers)
                {
                    SetLayerVisibility(layer.FullPath, doc, isVisible);
                }
            }
        }

        protected void HandleLayerAndChildrenVisibility(string parentLayer, RhinoDoc doc, bool isVisible)
        {
            HandleLayerAndChildrenVisibility(parentLayer, doc, isVisible, false);
        }

        protected void HandleLayerAndChildrenVisibility(string parentLayer, RhinoDoc doc, bool isVisible, bool hideExcludedLayer, params string [] exclusion)
        {
            var index = doc.Layers.FindByFullPath(parentLayer, true);

            if (index >= 0)
            {
                var parentLayerObj = doc.Layers[index];
                SetLayerVisibility(parentLayerObj.FullPath, doc, isVisible);

                var layers = parentLayerObj.GetChildren();
                // only one layer of children
                foreach (var layer in layers)
                {
                    if (!exclusion.ToList().Contains(layer.Name))
                    {
                        SetLayerVisibility(layer.FullPath, doc, isVisible);
                    }
                    else
                    {
                        if (hideExcludedLayer)
                        {
                            SetLayerVisibility(layer.FullPath, doc, false);
                        }
                    }
                }
            }
        }

        public void ApplyTransparency(IBB ibb, CMFImplantDirector director, double opacity, bool toggleOn)
        {
            var objManager = new CMFObjectManager(director);
            var rhinoObject = objManager.GetBuildingBlock(ibb);

            var curMat = rhinoObject.GetMaterial(true);
            double transparencyValue;

            if (toggleOn)
            {
                transparencyValue = 1 - opacity;
            }
            else
            {
                transparencyValue = 0;
            }

            if (Math.Abs(transparencyValue - curMat.Transparency) > Transparency.Epsilon)
            {
                curMat.Transparency = transparencyValue;
                curMat.CommitChanges();
            }
        }
    }
}
