using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.Core.Visualization
{

    public class DimensionVisualizer : IDisposable
    {

        private static DimensionVisualizer _instance;

        public static DimensionVisualizer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DimensionVisualizer();
                }

                return _instance;
            }
        }

        private List<DimensionConduit> _conduits;

        public DimensionVisualizer()
        {
            _conduits = new List<DimensionConduit>();
        }

        public void SetVisible(bool isVisible, ObjRef rhObj)
        {
            _conduits.ForEach(x =>
            {
                if (x.RhObjPair.ObjectId == rhObj.ObjectId)
                {
                    x.IsVisible = isVisible;
                }
            });
        }

        public void SetColor(Color color, ObjRef rhObj)
        {
            _conduits.ForEach(x =>
            {
                if (x.RhObjPair.ObjectId == rhObj.ObjectId)
                {
                    x.DisplayColor = color;
                }
            });
        }

        public void SetAllColorToDefault()
        {
            _conduits.ForEach(x =>
            {
                x.SetToDefaultDisplayColor();
            });
        }

        public void SetColorToDefault(ObjRef rhObj)
        {
            _conduits.ForEach(x =>
            {
                if (x.RhObjPair.ObjectId == rhObj.ObjectId)
                {
                    x.SetToDefaultDisplayColor();
                }
            });
        }

        public bool GetIsVisible(ObjRef rhObj)
        {
            var r = _conduits.Find(x => x.RhObjPair.ObjectId == rhObj.ObjectId);
            return r.IsVisible;
        }

        public void ResetConduits()
        {
            _conduits.ForEach(x =>
            {
                x.Enabled = false;
                x.Dispose();
            });
            _conduits = new List<DimensionConduit>();
        }

        public void InvalidateConduits(RhinoDoc doc)
        {
            doc.Layers?.ToList().ForEach(x =>
                {
                    if (x.Name != null && !x.IsDeleted && x.Name.Contains("Measurements -"))
                    {
                        var childLayers = x.GetChildren();

                        if (childLayers != null)
                        {
                            foreach (var childLayer in childLayers)
                            {
                                var objs = doc.Objects.FindByLayer(childLayer).ToList();

                                objs.ForEach(o =>
                                {
                                    if (!_conduits.Exists(y => y.RhObjPair.ObjectId == o.Id))
                                    {
                                        var cond = new DimensionConduit((Dimension)o.Geometry, new ObjRef(o));
                                        cond.IsVisible = childLayer.IsVisible;
                                        _conduits.Add(cond);
                                        cond.Enabled = true;
                                    }

                                });
                            }
                        }
                    }
                }
            );

            var itemsToRemove = new List<ObjRef>();
            _conduits.ForEach(x =>
            {
                var obj = doc.Objects.Find(x.RhObjPair.ObjectId);

                if (obj == null) //object got deleted.
                {
                    itemsToRemove.Add(x.RhObjPair);
                }
            });

            itemsToRemove.ForEach(x =>
            {
                _conduits.RemoveAll(y =>
                {

                    if (y.RhObjPair.ObjectId == x.ObjectId)
                    {
                        y.Enabled = false;
                        y.Dispose();
                        return true;
                    }

                    return false;
                });
            });

        }

        public void Dispose()
        {

        }
    }
}
