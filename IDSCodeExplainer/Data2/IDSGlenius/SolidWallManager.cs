using IDS.Core.ImplantBuildingBlocks;
using IDS.Glenius.ImplantBuildingBlocks;
using IDS.Glenius.Operations;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Linq;
using SolidWallInformation = System.Collections.Generic.Dictionary<System.Guid, System.Guid>;

namespace IDS.Glenius
{
    public class SolidWallManager
    {
        private readonly SolidWallInfoArchiver _archiver;

        //Key = Curve, Value = Wrap
        public SolidWallInformation SolidWallInfo { get; private set; }
        private readonly GleniusObjectManager _objectManager;
        public bool IsUndoRedoSubscribed { get; private set; }

        public SolidWallManager(GleniusImplantDirector director)
        {
            SolidWallInfo = new SolidWallInformation();
            _objectManager = new GleniusObjectManager(director);
            _archiver = new SolidWallInfoArchiver();
            IsUndoRedoSubscribed = false;
        }

        public void SubscribeForUndoRedo()
        {
            if (IsUndoRedoSubscribed)
            {
                return;
            }

            RhinoDoc.DeleteRhinoObject += RhinoDocDeleteRhinoObjectEvent;
            RhinoDoc.UndeleteRhinoObject += RhinoDocUnDeleteRhinoObjectEvent;
            IsUndoRedoSubscribed = true;
        }

        public void UnsubscribeForUndoRedo()
        {
            if (!IsUndoRedoSubscribed)
            {
                return;
            }

            RhinoDoc.DeleteRhinoObject -= RhinoDocDeleteRhinoObjectEvent;
            RhinoDoc.UndeleteRhinoObject -= RhinoDocUnDeleteRhinoObjectEvent;
            IsUndoRedoSubscribed = false;
        }

        private void RhinoDocUnDeleteRhinoObjectEvent(object sender, RhinoObjectEventArgs e)
        {
            var documentSolidWallCurveIds = _objectManager.GetAllBuildingBlockIds(IBB.SolidWallCurve);
            var documentSolidWallWrapIds = _objectManager.GetAllBuildingBlockIds(IBB.SolidWallWrap);
            var managedSolidWallCurveIds = SolidWallInfo.Keys.ToList();
            var managedSolidWallWrapIds = SolidWallInfo.Values.ToList();

            //Add missing ones into Manager

            var documentSolidWallCurveIdToRegister = documentSolidWallCurveIds.Where
                (id => !managedSolidWallCurveIds.Contains(id)).ToList();

            var documentSolidWallWrapIdToRegister = documentSolidWallWrapIds.Where
                (id => !managedSolidWallWrapIds.Contains(id)).ToList();

            //When Undo Redo, it will add curve and wrap part. 
            //Each addition will invoke this method and should only register when both are present.
            if (documentSolidWallCurveIdToRegister.Count != documentSolidWallWrapIdToRegister.Count)
            {
                return;
            }

            for (var i = 0; i < documentSolidWallCurveIdToRegister.Count; ++i)
            {
                RegisterSolidWall(documentSolidWallCurveIdToRegister[i], documentSolidWallWrapIdToRegister[i]);
            }
        }

        //Add back things into document
        private void RhinoDocDeleteRhinoObjectEvent(object sender, RhinoObjectEventArgs e)
        {
            var documentSolidWallCurveIds = _objectManager.GetAllBuildingBlockIds(IBB.SolidWallCurve);
            var documentSolidWallWrapIds = _objectManager.GetAllBuildingBlockIds(IBB.SolidWallWrap);
            var managedSolidWallCurveIds = SolidWallInfo.Keys.ToList();
            var managedSolidWallWrapIds = SolidWallInfo.Values.ToList();

            //Clean Manager

            var managedSolidWallCurveIdToDelete = managedSolidWallCurveIds.Where
                (id => !documentSolidWallCurveIds.Contains(id)).ToList();

            var managedSolidWallWrapIdToDelete = managedSolidWallWrapIds.Where
                (id => !documentSolidWallWrapIds.Contains(id)).ToList();

            if (managedSolidWallCurveIdToDelete.Count != managedSolidWallWrapIdToDelete.Count)
            {
                return;
            }

            managedSolidWallCurveIdToDelete.ForEach(DeRegisterSolidWallByCurve);
            managedSolidWallWrapIdToDelete.ForEach(DeRegisterSolidWallByWrap);
        }

        public void AddSolidWall(Curve solidWallCurve, Mesh solidWallWrap, RhinoDoc doc)
        {
            var curveId = _objectManager.AddNewBuildingBlock(IBB.SolidWallCurve, solidWallCurve);
            var meshId = _objectManager.AddNewBuildingBlock(IBB.SolidWallWrap, solidWallWrap);
            SolidWallInfo.Add(curveId, meshId);

            ImplantBuildingBlockProperties.SetTransparency(BuildingBlocks.Blocks[IBB.SolidWallWrap], doc, 0.5);
        }

        public bool ReplaceSolidWall(Guid solidWallCurveId, Curve solidWallCurve, Mesh solidWallWrap)
        {
            if (!SolidWallInfo.ContainsKey(solidWallCurveId))
            {
                return false;
            }

            _objectManager.SetBuildingBlock(IBB.SolidWallCurve, solidWallCurve, solidWallCurveId);
            _objectManager.SetBuildingBlock(IBB.SolidWallWrap, solidWallWrap, SolidWallInfo[solidWallCurveId]);
            return true;
        }

        public void RegisterSolidWall(Guid curveId, Guid wrapId)
        {
            SolidWallInfo.Add(curveId, wrapId);
        }

        public void DeRegisterSolidWallByCurve(Guid curveId)
        {
            if (SolidWallInfo.ContainsKey(curveId))
            {
                SolidWallInfo.Remove(curveId);
            }
        }

        public void DeRegisterSolidWallByWrap(Guid wrapId)
        {
            if (!SolidWallInfo.ContainsValue(wrapId))
            {
                return;
            }

            var key = GetSolidWallCurveId(wrapId);
            SolidWallInfo.Remove(key);
        }

        public Guid GetSolidWallCurveId(Guid solidWallWrapId)
        {
            return SolidWallInfo.FirstOrDefault(x => x.Value == solidWallWrapId).Key;
        }

        public bool DeleteSolidWall(Guid solidWallId)
        {
            if (!SolidWallInfo.ContainsKey(solidWallId) && !SolidWallInfo.ContainsValue(solidWallId))
            {
                return false;
            }

            var keyId = solidWallId;
            Guid valueId;

            //check if it is a value, find its key
            if (SolidWallInfo.ContainsValue(solidWallId))
            {
                keyId = GetSolidWallCurveId(solidWallId);
                valueId = solidWallId;
            }
            else
            {
                valueId = SolidWallInfo[keyId];
            }

            _objectManager.DeleteObject(keyId);
            _objectManager.DeleteObject(valueId);
            SolidWallInfo.Remove(keyId);

            return true;
        }

        public ArchivableDictionary CreateArchive()
        {
            return _archiver.CreateArchive(SolidWallInfo);
        }

        public bool LoadFromArchive(ArchivableDictionary dict)
        {
            var info = _archiver.LoadFromArchive(dict);

            if (info != null)
            {
                SolidWallInfo = info;
            }

            return SolidWallInfo != null;
        }
    }
}
