using System.Globalization;

namespace IDS.CMF.Query
{
    public class QcDocBaseScrewInfoModel
    {
        private readonly QcDocBaseScrewInfoData _data;
        public QcDocBaseScrewInfoModel(QcDocBaseScrewInfoData data)
        {
            _data = data;
        }

        public int Index => _data.Index;
        public string IndexStr => Index.ToString();
        public string ScrewType => _data.ScrewType;
        public string Diameter => string.Format(CultureInfo.InvariantCulture, "{0:0.##}", _data.Diameter);
        public string Length => string.Format(CultureInfo.InvariantCulture, "{0:0.#}", _data.Length);
        public string Angle => string.Format(CultureInfo.InvariantCulture, "{0:0.##}°", _data.Angle);
    }
}
