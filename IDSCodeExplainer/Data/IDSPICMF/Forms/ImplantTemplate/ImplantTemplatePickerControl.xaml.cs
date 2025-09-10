using IDS.CMF.Constants;
using IDS.CMF.DataModel;
using IDS.CMF.Visualization;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace IDS.PICMF.Forms
{
    /// <summary>
    /// Interaction logic for ImplantTemplatePickerControl.xaml
    /// </summary>
    public partial class ImplantTemplatePickerControl : UserControl
    {
        private readonly ImplantTemplateDataModel _implantTemplateDataModel;

        public delegate void OnPlaceImplantTemplateDelegate(ImplantTemplateDataModel data);

        public OnPlaceImplantTemplateDelegate OnPlaceImplantTemplateHandler { get; set; }


        public ImplantTemplatePickerControl(ImplantTemplateDataModel implantTemplateDataModel)
        {
            InitializeComponent();

            _implantTemplateDataModel = implantTemplateDataModel;

            this.DataContext = _implantTemplateDataModel;
        }

        public static BitmapImage ToBitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        private void DrawAllComponent()
        {
            ImplantTemplateName.Text = _implantTemplateDataModel.Name;

            var implantTemplateBitmapDrawer = new ImplantTemplateBitmapDrawer();
            implantTemplateBitmapDrawer.DrawFromImplantTemplateDataModel(_implantTemplateDataModel, true);

            ImplantTemplateImage.Source = ToBitmapImage(implantTemplateBitmapDrawer.LatestBitmap);
        }

        private void ImplantTemplateImage_OnLoaded(object sender, RoutedEventArgs e)
        {
            DrawAllComponent();
        }

        private void ImplantTemplateBorder_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnPlaceImplantTemplateHandler?.Invoke(_implantTemplateDataModel);
        }

        private void ImplantTemplateBorder_OnMouseEnter(object sender, MouseEventArgs e)
        {
            ImplantTemplateBorder.BorderBrush = ImplantTemplateAppearance.ImplantTemplateBorderActiveBrush;
        }

        private void ImplantTemplateBorder_OnMouseLeave(object sender, MouseEventArgs e)
        {
            ImplantTemplateBorder.BorderBrush = ImplantTemplateAppearance.ImplantTemplateBorderInactiveBrush;
        }

        private void ImplantTemplateImage_OnUnloaded(object sender, RoutedEventArgs e)
        {
            OnPlaceImplantTemplateHandler = null;
        }
    }
}
