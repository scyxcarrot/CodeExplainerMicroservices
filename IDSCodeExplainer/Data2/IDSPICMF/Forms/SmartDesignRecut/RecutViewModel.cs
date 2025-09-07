using IDS.CMF.DataModel;
using System.Collections.Generic;
using System.Linq;

namespace IDS.PICMF.Forms
{
    public interface IRecutViewModel
    {
        string RecutType { get; set; }
        Dictionary<string, PartSelectionViewModel> PartSelections { get; set; }

        void CleanUp();
        ISmartDesignRecutModel ConvertToDataModel();
        bool ValidateCustomInputs();
    }

    public abstract class RecutViewModel : IRecutViewModel
    {
        public string RecutType { get; set; }
        public Dictionary<string, PartSelectionViewModel> PartSelections { get; set; }

        public void CleanUp()
        {
            foreach (var value in PartSelections.Values)
            {
                value.Reset();
            }
        }

        public abstract ISmartDesignRecutModel ConvertToDataModel();

        protected List<string> GetSourcePartNames(string keyName)
        {
            return PartSelections[keyName].SourcePartNames.ToList();
        }

        protected string GetSourcePartName(string keyName)
        {
            var partList = GetSourcePartNames(keyName);
            return partList.Any() ? partList[0] : string.Empty;
        }

        public virtual bool ValidateCustomInputs()
        {
            return true;
        }
    }
}
