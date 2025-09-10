using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IDS.Glenius;
using IDS.Glenius.ImplantBuildingBlocks;

namespace IDS.Glenius.Query
{
    public class QCDocumentScrewInfoProvider
    {
        private readonly List<Screw> screws;

        public QCDocumentScrewInfoProvider(GleniusImplantDirector director)
        {
            var objectManager = new GleniusObjectManager(director);
            screws = objectManager.GetAllBuildingBlocks(IBB.Screw).Select(x => x as Screw).OrderBy(x => x.Index).ToList();
        }

        public Dictionary<int,double> GetLengths()
        {
            var dict = new Dictionary<int, double>();
            screws.ForEach(x => dict.Add(x.Index, x.TotalLength));
            return dict;
        }

        public Dictionary<int, double> GetDistanceUntilBone()
        {
            var dict = new Dictionary<int, double>();
            screws.ForEach(x => dict.Add(x.Index, x.GetDistanceUntilBone()));
            return dict;
        }

        public Dictionary<int, double> GetDistanceInBone()
        {
            var dict = new Dictionary<int, double>();
            screws.ForEach(x => dict.Add(x.Index, x.GetDistanceInBone()));
            return dict;
        }

        public Dictionary<int, double> GetDiameter()
        {
            var dict = new Dictionary<int, double>();
            screws.ForEach(x => dict.Add(x.Index, x.Diameter));
            return dict;
        }

        public Dictionary<int, string> GetScrewLockingType()
        {
            var dict = new Dictionary<int, string>();
            screws.ForEach(x => dict.Add(x.Index, x.GetScrewLockingType()));
            return dict;
        }

        public Dictionary<int, double> GetScrewMantleElongationLengths()
        {
            var dict = new Dictionary<int, double>();
            screws.ForEach(x => dict.Add(x.Index, x.GetScrewMantle().ExtensionLength));
            return dict;
        }

        public Dictionary<int, double> GetScrewOffset()
        {
            var dict = new Dictionary<int, double>();
            screws.ForEach(x => dict.Add(x.Index, x.GetOffsetFromIdealPlacementPlane()));
            return dict;
        }

        public Dictionary<int, bool> GetIsBicortical()
        {
            var dict = new Dictionary<int, bool>();
            screws.ForEach(x => dict.Add(x.Index, x.IsBicortical));
            return dict;
        }
    }
}
