using IDS.CMF.ImplantBuildingBlocks;
using IDS.CMF.V2.ScrewQc;
using IDS.Core.Plugin;
using IDS.Interface.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace IDS.CMF.ScrewQc
{
    public abstract class ImplantScrewQcProxyChecker : ImplantScrewQcChecker, IScrewQcChecker
    {
        protected IScrewQcCheckerV2 Checker;

        protected ImplantScrewQcProxyChecker(ImplantScrewQcCheck implantScrewQcCheckName) :
            base(new IDSRhinoConsole(), implantScrewQcCheckName)
        {
        }

        protected ImplantScrewQcProxyChecker(IConsole console, ImplantScrewQcCheck implantScrewQcCheckName) :
            base(console, implantScrewQcCheckName)
        {
        }

        public IImmutableDictionary<Guid, IScrewQcResult> CheckAll(IEnumerable<Screw> screws, out Dictionary<Guid, long> timeTracker)
        {
            var results = new Dictionary<Guid, IScrewQcResult>();
            timeTracker = new Dictionary<Guid, long>();
            foreach (var screw in screws)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                results.Add(screw.Id, Check(screw));
                stopwatch.Stop();
                timeTracker.Add(screw.Id, stopwatch.ElapsedMilliseconds);
            }

            return results.ToImmutableDictionary();
        }

        public virtual IScrewQcResult Check(Screw screw)
        {
            return Check(ScrewQcData.CreateImplantScrewQcData(screw));
        }

        public override IScrewQcResult Check(IScrewQcData screwQcData)
        {
            return Checker?.Check(screwQcData);
        }
    }
}
