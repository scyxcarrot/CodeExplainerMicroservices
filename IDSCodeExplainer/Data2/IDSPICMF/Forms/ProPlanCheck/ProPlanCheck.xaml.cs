using IDS.CMF.Forms;
using IDS.CMF.V2.Constants;
using IDS.CMF.V2.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for ProPlanCheck.xaml
    /// </summary>
    public partial class ProPlanCheck : Window, IDisposable
    {
        private ProPlanCheckViewModel _vm;

        public ProPlanCheck()
        {
            InitializeComponent();

            _vm = new ProPlanCheckViewModel();
            this.DataContext = _vm;
        }

        public void SetPartList(List<DisplayStringDataModel> checkList)
        {
            var displayModels = checkList.SelectMany(CreateDisplayModels)
                .OrderBy(d => OrderByGroupDescription(d.DisplayGroup)).ThenBy(d => d.DisplayString);
            _vm.SppcObjectNames = new ObservableCollection<DisplayStringModel>(displayModels);

            var view = (CollectionView)CollectionViewSource.GetDefaultView(_vm.SppcObjectNames);
            var groupDescription = new PropertyGroupDescription("DisplayGroup");
            view.GroupDescriptions.Add(groupDescription);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _vm.Dispose();
            }
        }

        private List<DisplayStringModel> CreateDisplayModels(DisplayStringDataModel dataModel)
        {
            var list = new List<DisplayStringModel>();
            foreach (var displayGroup in dataModel.DisplayGroup)
            {
                list.Add(new DisplayStringModel
                {
                    DisplayGroup = displayGroup,
                    DisplayString = dataModel.DisplayString
                });
            }
            return list;
        }

        private int OrderByGroupDescription(string description)
        {
            switch (description)
            {
                case ProPlanCheckConstants.DuplicatePartsGroup:
                    return 1;
                case ProPlanCheckConstants.PartsNotRecognizedGroup:
                    return 2;
                case ProPlanCheckConstants.NoMatchingPartsGroup:
                    return 3;
                case ProPlanCheckConstants.CorrectPartsGroup:
                    return 4;
                default:
                    return 5;
            }
        }
    }
}
