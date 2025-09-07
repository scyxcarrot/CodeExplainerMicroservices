using IDS.CMF.V2.ScrewQc;
using IDS.Core.Enumerators;
using IDS.Core.PluginHelper;
using System.Text;

namespace IDS.CMF.ScrewQc
{
    public class BarrelTypeResult : GenericScrewQcResult<BarrelTypeContent>
    {
        public override string GetQcBubbleMessage()
        {
#if (STAGING)
            IDSPluginHelper.WriteLine(LogCategory.Diagnostic, $"Barrel Type: {content.BarrelType}");
#endif
            return string.Empty;
        }

        public override string GetQcDocTableCellMessage()
        {
            var cellTextBuilder = new StringBuilder();
            cellTextBuilder.Append(
                $"<td class=\"{GetTableDataColorFromBarrel(content.BarrelErrorInGuideCreation)}\">");
            cellTextBuilder.Append(content.BarrelType);
            cellTextBuilder.Append("</td>");
            return cellTextBuilder.ToString();
        }

        public string GetTableDataColorFromBarrel(bool barrelErrorInGuideCreation)
        {
            return AssignTableDataColor(barrelErrorInGuideCreation ? QcDocCellColor.Orange : QcDocCellColor.Green);
        }

        public override object GetSerializableScrewQcResult()
        {
            return new BarrelTypeContent(content);
        }

        public BarrelTypeResult(string screwQcCheckName, BarrelTypeContent content) : 
            base(screwQcCheckName, content)
        {
        }
    }
}
