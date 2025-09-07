using System.Runtime.InteropServices;
using System.Windows.Controls;
using IDS.CMF;
using IDS.CMF.DataModel;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for TeethBlockWizardExpandableColumn.xaml
    /// </summary>
    [Guid("89691AEF-644B-4D11-85B8-72E286E99F4A")]
    public partial class TeethBlockWizardExpandableColumn : UserControl
    {
        private TeethBlockExpandableColumnViewModel _vm = new TeethBlockExpandableColumnViewModel();

        private readonly CMFImplantDirector _director;
        public readonly ITeethBlockViewModel ChildViewModel;

        public TeethBlockWizardExpandableColumn(CMFImplantDirector director, UserControl userControl, ProPlanImportPartType selectedPartType)
        {
            _director = director;
            InitializeComponent();
            DataContext = _vm;
            
            switch (userControl)
            {
                case TeethBlockImportCastColumn importCast:
                    Container.Children.Add(importCast);
                    ChildViewModel = importCast.ViewModel;

                    // Open the first expander by default, since it's always enabled
                    Expander.IsExpanded = true;
                    break;

                case TeethBlockCreateLimitingSurfaceColumn createLimitingSurface:
                    Container.Children.Add(createLimitingSurface);
                    ChildViewModel = createLimitingSurface.ViewModel;
                    break;

                case TeethBlockMarkSurfaceColumn markSurface:
                    Container.Children.Add(markSurface);
                    ChildViewModel = markSurface.ViewModel;
                    break;
            }

            ChildViewModel.SelectedPartType = selectedPartType;
            InitializeColumns();
        }

        private void InitializeColumns()
        {
            _vm.ColumnTitle = ChildViewModel.ColumnTitle;
            Expander.IsEnabled = ChildViewModel.SetEnabled(_director);
        }
    }
}
