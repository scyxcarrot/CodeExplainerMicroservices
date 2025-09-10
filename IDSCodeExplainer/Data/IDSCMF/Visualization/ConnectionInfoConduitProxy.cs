using IDS.CMF.DataModel;
using IDS.CMF.Utilities;
using IDS.CMF.V2.DataModel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class ConnectionInfoConduitProxy
    {
        private static ConnectionInfoConduitProxy _instance;

        public static ConnectionInfoConduitProxy GetInstance()
        {
            return _instance ?? (_instance = new ConnectionInfoConduitProxy());
        }

        private List<KeyValuePair<Curve, ConnectionInfoBubbleConduit>> _connectionInfoDisplayConduits;
        private CMFImplantDirector _director;

        public ConnectionInfoConduitProxy()
        {
            _connectionInfoDisplayConduits = new List<KeyValuePair<Curve, ConnectionInfoBubbleConduit>>();
        }

        public bool SetUp(CMFImplantDirector director, bool showOnlyConnectionWithOverridenWidth)
        {
            _director = director;
            return InvalidateConduits(showOnlyConnectionWithOverridenWidth);
        }

        public bool InvalidateConduits(bool showOnlyConnectionWithOverridenWidth)
        {
            _connectionInfoDisplayConduits = new List<KeyValuePair<Curve, ConnectionInfoBubbleConduit>>();

            foreach (var casePreference in _director.CasePrefManager.CasePreferences)
            {
                var connections = casePreference.ImplantDataModel.ConnectionList;
                if (!connections.Any())
                {
                    continue;
                }

                var defaultPlateWidth = casePreference.CasePrefData.PlateWidthMm;
                var defaultLinkWidth = casePreference.CasePrefData.LinkWidthMm;

                var implantCurves = ImplantCreationUtilities.CreateImplantConnectionCurves(connections);

                foreach (var connectionCurve in implantCurves)
                {
                    var segment = DataModelUtilities.GetConnections(connectionCurve, connections);
                    var refSegment = segment.First();

                    var conduit = new ConnectionInfoBubbleConduit();

                    if (refSegment is ConnectionPlate)
                    {
                        if (showOnlyConnectionWithOverridenWidth && Math.Abs(refSegment.Width - defaultPlateWidth) < 0.001)
                        {
                            continue;
                        }
                        
                        conduit.DefaultWidth = defaultPlateWidth;
                        conduit.DotColor = Color.Blue;
                    }
                    else if (refSegment is ConnectionLink)
                    {
                        if (showOnlyConnectionWithOverridenWidth && Math.Abs(refSegment.Width - defaultLinkWidth) < 0.001)
                        {
                            continue;
                        }
                        
                        conduit.DefaultWidth = defaultLinkWidth;
                        conduit.DotColor = Color.Green;
                    }

                    conduit.Width = refSegment.Width;
                    Point3d[] points;
                    connectionCurve.DivideByCount(2, false, out points);
                    conduit.Location = points.First();

                    _connectionInfoDisplayConduits.Add(new KeyValuePair<Curve, ConnectionInfoBubbleConduit>(connectionCurve, conduit));
                }
            }

            ConduitUtilities.RefeshConduit();

            return _connectionInfoDisplayConduits.Any();
        }

        public void Reset()
        {
            _connectionInfoDisplayConduits.ForEach(x => x.Value.Enabled = false);
            _connectionInfoDisplayConduits.Clear();
            _connectionInfoDisplayConduits = new List<KeyValuePair<Curve, ConnectionInfoBubbleConduit>>();

            ConduitUtilities.RefeshConduit();
        }

        public void Show(bool isEnabled)
        {
            _connectionInfoDisplayConduits?.ForEach(x => x.Value.Enabled = isEnabled);

            ConduitUtilities.RefeshConduit();
        }

        public bool IsShowing()
        {
            return _connectionInfoDisplayConduits.Any() && _connectionInfoDisplayConduits[0].Value.Enabled;
        }
    }
}
