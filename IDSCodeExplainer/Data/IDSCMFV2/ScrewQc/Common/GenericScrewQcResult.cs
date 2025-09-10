using System;

namespace IDS.CMF.V2.ScrewQc
{
    public abstract class GenericScrewQcResult<TContent> : IScrewQcResult
    {
        protected readonly TContent content;

        private readonly string _screwQcCheckName;

        protected GenericScrewQcResult(string screwQcCheckName, TContent content)
        {
            _screwQcCheckName = screwQcCheckName;
            this.content = content;
        }

        public string GetScrewQcCheckName()
        {
            return _screwQcCheckName;
        }

        protected static string AssignTableDataColor(bool hasError)
        {
            return hasError ? AssignTableDataColor(QcDocCellColor.Red) : AssignTableDataColor(QcDocCellColor.Green);
        }

        protected static string AssignTableDataColor(QcDocCellColor cellColor)
        {
            switch (cellColor)
            {
                case QcDocCellColor.Green:
                    return "col_green";
                case QcDocCellColor.Yellow:
                    return "col_yellow";
                case QcDocCellColor.Orange:
                    return "col_orange";
                case QcDocCellColor.Red:
                    return "col_red";
                default:
                    throw new ArgumentOutOfRangeException($"Invalid QcDocCellColor enum. Passed enum = {cellColor}");
            }
        }

        #region Abstract Function

        public abstract string GetQcBubbleMessage();

        public abstract string GetQcDocTableCellMessage();

        public abstract object GetSerializableScrewQcResult();

        #endregion
    }
}
