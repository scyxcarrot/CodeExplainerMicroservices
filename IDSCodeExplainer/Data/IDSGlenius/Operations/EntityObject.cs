using Rhino;
using Rhino.DocObjects.Tables;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace IDS.Glenius.Operations
{
    public class EntityObject
    {
        public Mesh Mesh { get; private set; }
        public Color OriginalColor { get; private set; }
        public bool IsVisible { get; private set; }

        private Nullable<bool> _isConflicting;
        public Nullable<bool> IsConflicting
        {
            get { return _isConflicting; }
            set
            {
                if (_isConflicting != value)
                {
                    gotChanges = true;
                    _isConflicting = value;
                }
            }
        }

        private bool gotChanges;
        private readonly RhinoDoc document;
        private readonly int layerId;

        public EntityObject(Mesh mesh, Color originalColor, RhinoDoc doc, Nullable<bool> isConflicting, int layerIndex)
        {
            document = doc;
            Mesh = mesh;
            OriginalColor = originalColor;
            IsConflicting = isConflicting;
            layerId = layerIndex;
            IsVisible = true;
            Update();

            RhinoDoc.LayerTableEvent += RhinoDocLayerTableEvent;
        }

        public void Update()
        {
            if (gotChanges)
            {                
                document.Views.Redraw();
                gotChanges = false;
            }
        }

        public void UnhookEvent()
        {
            RhinoDoc.LayerTableEvent -= RhinoDocLayerTableEvent;
        }

        private void RhinoDocLayerTableEvent(object sender, LayerTableEventArgs e)
        {
            if (e.EventType == LayerTableEventType.Modified && e.LayerIndex == layerId && e.NewState.IsVisible != IsVisible)
            {
                IsVisible = e.NewState.IsVisible;
            }
        }
    }
}