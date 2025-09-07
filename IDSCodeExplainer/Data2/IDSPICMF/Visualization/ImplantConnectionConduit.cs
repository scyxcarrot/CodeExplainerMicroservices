using IDS.CMF.DataModel;
using IDS.CMF.Factory;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using IDS.Core.Utilities;
using IDS.Interface.Implant;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.PICMF.Visualization
{
    public class ImplantConnectionConduit : DisplayConduit, IDisposable
    {
        private readonly PastilleBrepFactory _pastilleBrepFactory;
        private readonly DisplayMaterial _plateDisplayMaterial;
        private readonly DisplayMaterial _linkDisplayMaterial;
        private readonly DisplayMaterial _sphereDisplayMaterial;
        private readonly DisplayMaterial _highlightDisplayMaterial;
        private readonly DisplayMaterial _selectedDisplayMaterial;
        private readonly List<Brep> _pastilleSphereBreps;
        private readonly List<Mesh> _selectedConnections;

        public Dictionary<Mesh, List<IConnection>> ConnectionDictionary { get; private set; }
        public Mesh HighlightedConnection { get; set; }
        public List<Mesh> SelectedConnections => new List<Mesh>(_selectedConnections);

        public ImplantConnectionConduit()
        {
            _plateDisplayMaterial = new DisplayMaterial(Color.Blue);
            _linkDisplayMaterial = new DisplayMaterial(Color.Green);
            _sphereDisplayMaterial = new DisplayMaterial(Color.White);
            _highlightDisplayMaterial = new DisplayMaterial(Color.Yellow);
            _selectedDisplayMaterial = new DisplayMaterial(Color.Orange);

            _pastilleBrepFactory = new PastilleBrepFactory();
            _pastilleSphereBreps = new List<Brep>();
            _selectedConnections = new List<Mesh>();

            ConnectionDictionary = new Dictionary<Mesh, List<IConnection>>();
            HighlightedConnection = null;
        }

        public void SetDotsAndConnections(List<IDot> dots, List<IConnection> connections)
        {
            GeneratePastilles(dots, connections);
            GenerateConnections(connections);
        }

        public void SelectConnection(Mesh connection)
        {
            if (connection != null && !_selectedConnections.Contains(connection))
            {
                _selectedConnections.Add(connection);
            }
        }

        public void DeselectConnection(Mesh connection)
        {
            if (connection != null && _selectedConnections.Contains(connection))
            {
                _selectedConnections.Remove(connection);
            }
        }

        private void GeneratePastilles(List<IDot> dots, List<IConnection> connections)
        {
            var pastilles = dots.Where(dot => dot is DotPastille).Cast<DotPastille>();

            foreach (var pastille in pastilles)
            {
                var direction = DataModelUtilities.GetAverageDirection(connections, pastille);
                var p = _pastilleBrepFactory.CreatePastille(pastille, direction);
                var s = Brep.CreateFromSphere(ScrewUtilities.CreateScrewSphere(pastille, 2.0));
                _pastilleSphereBreps.Add(s);
            }
        }

        private void GenerateConnections(List<IConnection> connections)
        {
            var implantCurves = ImplantCreationUtilities.CreateImplantConnectionCurves(connections);

            foreach (var connectionCurve in implantCurves)
            {
                var segment = DataModelUtilities.GetConnections(connectionCurve, connections);

                var connectionParts = new List<Mesh>();
                foreach (var connection in segment)
                {
                    connectionParts.Add(MeshUtilities.ConvertBrepToMesh(ConnectionBrepFactory.CreateConnection(connection)));
                }

                var connectionMesh = MeshUtilities.AppendMeshes(connectionParts);
                ConnectionDictionary.Add(connectionMesh, segment);
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            DrawConnections(e.Display);
            DrawDots(e.Display);
        }

        private void DrawConnections(DisplayPipeline p)
        {
            foreach (var connection in ConnectionDictionary)
            {
                var displayMaterial = connection.Value[0] is ConnectionPlate ? _plateDisplayMaterial : _linkDisplayMaterial;
                p.DrawMeshShaded(connection.Key, displayMaterial);
            }

            foreach (var connection in _selectedConnections)
            {
                p.DrawMeshShaded(connection, _selectedDisplayMaterial);
            }

            if (HighlightedConnection != null)
            {
                p.DrawMeshShaded(HighlightedConnection, _highlightDisplayMaterial);
            }
        }

        private void DrawDots(DisplayPipeline p)
        {
            _pastilleSphereBreps.ForEach(x => { p.DrawBrepShaded(x, _sphereDisplayMaterial); });
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
                _plateDisplayMaterial.Dispose();
                _linkDisplayMaterial.Dispose();
                _sphereDisplayMaterial.Dispose();
                _highlightDisplayMaterial.Dispose();
                _selectedDisplayMaterial.Dispose();
            }
        }
    }
}