using IDS.Core.Utilities;
using Rhino.Collections;

namespace IDS.Glenius
{
    public class PreopCorArchiver
    {
        private const string KeyCenterPoint = "PreopCorCenterPoint";
        private const string KeyRadius = "PreopCorRadius";

        public ArchivableDictionary CreateArchive(AnalyticSphere preopCor)
        {
            if (preopCor == null)
            {
                return null;
            }

            var dict = new ArchivableDictionary();
            dict.Set($"{KeyCenterPoint}", preopCor.CenterPoint);
            dict.Set($"{KeyRadius}", preopCor.Radius);
            return dict;
        }

        public AnalyticSphere LoadFromArchive(ArchivableDictionary dict)
        {
            if (!dict.ContainsKey(KeyCenterPoint) || !dict.ContainsKey(KeyRadius))
            {
                return null;
            }

            var preopCor = new AnalyticSphere
            {
                CenterPoint = dict.GetPoint3d($"{KeyCenterPoint}"),
                Radius = dict.GetDouble($"{KeyRadius}")
            };
            return preopCor;
        }
    }
}
