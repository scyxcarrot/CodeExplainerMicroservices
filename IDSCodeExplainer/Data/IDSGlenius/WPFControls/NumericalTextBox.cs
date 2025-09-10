using IDS.Core.WPFControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IDSCore.Common.WPFControls
{
    public class NumericalTextBox : TextBox
    {
        private readonly NumericalTextBoxHelper _helper = new NumericalTextBoxHelper();

        public int DecimalPlaces { get; set; }
        public double? MaxValue { get; set; }
        public double? MinValue { get; set; }

        public NumericalTextBox()
        {
            //So user cant paste if it is not a number
            DataObject.AddPastingHandler(this, OnPaste);
        }

        private void SetHelperParameters(string currentText, int decimalPlaces, int selectionStart, int selectionLength)
        {
            _helper.CurrentText = currentText;
            _helper.DecimalPlaces = decimalPlaces;
            _helper.SelectionStart = selectionStart;
            _helper.SelectionLength = selectionLength;
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {

            if (e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true))
            {
                string clipBoardText = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;

                double testCandidate;
                if (!_helper.TryParseAsDouble(clipBoardText, out testCandidate))
                {
                    e.CancelCommand();
                }

                NumericalTextBox txtBox = sender as NumericalTextBox;

                if (txtBox != null)
                {
                    SetHelperParameters(txtBox.Text, txtBox.DecimalPlaces, txtBox.SelectionStart, txtBox.SelectionLength);

                    if (!_helper.AddStringCheckIsOk(clipBoardText))
                    {
                        e.CancelCommand();
                    }
                }
            }
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            SetHelperParameters(Text, DecimalPlaces, SelectionStart, SelectionLength);

            e.Handled = !_helper.AddStringCheckIsOk(e.Text);
            base.OnTextInput(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            e.Handled = (e.Key == Key.Space);

            base.OnPreviewKeyDown(e);
        }

        public bool TryGetTextValue(out double value)
        {
            var text = Text;
            text = text.Replace("°", "");
            return _helper.TryParseAsDouble(text, out value);
        }
    }
}
