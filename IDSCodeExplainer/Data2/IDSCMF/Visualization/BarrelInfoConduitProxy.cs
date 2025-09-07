using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.Utilities;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.CMF.Visualization
{
    public class BarrelInfoConduitProxy
    {
        private static BarrelInfoConduitProxy _instance;

        public static BarrelInfoConduitProxy GetInstance()
        {
            return _instance ?? (_instance = new BarrelInfoConduitProxy());
        }

        private List<KeyValuePair<Screw, BarrelInfoDisplayConduit>> _barrelDisplayConduits;
        private BarrelInfoConduitProxy()
        {
            _barrelDisplayConduits = new List<KeyValuePair<Screw, BarrelInfoDisplayConduit>>();
        }

        public void SetUp(List<Screw> screws)
        {
            _barrelDisplayConduits = new List<KeyValuePair<Screw, BarrelInfoDisplayConduit>>();

            screws?.ForEach(screw =>
            {
                if (screw.RegisteredBarrelId == Guid.Empty)
                {
                    return;
                }

                var registeredBarrel = screw.Document.Objects.FindId(screw.RegisteredBarrelId);
                var displayConduitPoint = registeredBarrel.Geometry.GetBoundingBox(false).Center;
                var cond = new BarrelInfoDisplayConduit
                {
                    BarrelType = screw.BarrelType,
                    Location = displayConduitPoint
                };

                _barrelDisplayConduits.Add(new KeyValuePair<Screw, BarrelInfoDisplayConduit>(screw, cond));
            });

            ConduitUtilities.RefeshConduit();
        }

        public void Reset()
        {
            _barrelDisplayConduits.ForEach(x => x.Value.Enabled = false);
            _barrelDisplayConduits.Clear();
            _barrelDisplayConduits = new List<KeyValuePair<Screw, BarrelInfoDisplayConduit>>();

            ConduitUtilities.RefeshConduit();
        }

        public void Show(bool isEnabled)
        {
            _barrelDisplayConduits?.ForEach(x => x.Value.Enabled = isEnabled);

            ConduitUtilities.RefeshConduit();
        }

        public bool IsShowing()
        {
            return _barrelDisplayConduits.Any() && _barrelDisplayConduits[0].Value.Enabled;
        }
    }
}
