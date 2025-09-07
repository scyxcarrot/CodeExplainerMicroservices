using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using IDS.Glenius;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Collections;
using SolidWallInfo = System.Collections.Generic.Dictionary<System.Guid, System.Guid>;

namespace IDS.Glenius.Operations
{
    public class SolidWallInfoArchiver
    {
        private const string keySolidWallCurves = "solidWallCurveIds";
        private const string keySolidWallWraps = "solidWallWrapIds";

        //Returns Null if failed
        public ArchivableDictionary CreateArchive(SolidWallInfo info)
        {
            List<Guid> solidWallCurveIds = info.Keys.ToList();
            List<Guid> solidWallWrapIds = info.Values.ToList();

            if (solidWallCurveIds.Count == solidWallWrapIds.Count)
            {
                ArchivableDictionary dict = new ArchivableDictionary();
                dict.Set($"{keySolidWallCurves}", solidWallCurveIds);
                dict.Set($"{keySolidWallWraps}", solidWallWrapIds);
                return dict;
            }

            return null;
        }

        //Returns Null if failed
        public SolidWallInfo LoadFromArchive(ArchivableDictionary dict)
        {
            if (dict.ContainsKey(keySolidWallCurves) && dict.ContainsKey(keySolidWallWraps))
            {
                var solidWallCurveIds = ((Guid[])dict[keySolidWallCurves]).ToList();
                var solidWallWrapIds = ((Guid[])dict[keySolidWallWraps]).ToList();
                int size = solidWallWrapIds.Count;

                Assert.AreEqual(solidWallCurveIds.Count, solidWallWrapIds.Count);

                SolidWallInfo info = new SolidWallInfo();

                for (int i = 0; i < size; ++i)
                {
                    info.Add(solidWallCurveIds[i], solidWallWrapIds[i]);
                }

                return info;
            }

            return null;
        }

    }
}
