using System.Globalization;
using System.Linq;
using IDS.Core.Utilities;

namespace IDS.Core.WPFControls
{
    public class NumericalTextBoxHelper
    {
        public string CurrentText { get; set; }
        public int DecimalPlaces { get; set; }

        private int _caretIndex;

        //Setting this will set SelectionStart and SelectionLength to 0
        public int CaretIndex
        {
            get { return _caretIndex; }
            set
            {
                _caretIndex = value;
                _selectionStart = value;
                SelectionLength = 0;
            }
        }

        private int _selectionStart;

        //Setting this will set CaretIndex the same value as SelectionStart
        public int SelectionStart
        {
            get{ return _selectionStart; }
            set
            {
                _selectionStart = value;
                _caretIndex = value;
            }
        }
        public int SelectionLength { get; set; }

        private readonly char _decimalSign;

        public NumericalTextBoxHelper()
        {
            //the NumberDecimalSeparator for InvariantCulture is '.'
            _decimalSign = CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator[0];
        }

        public bool SelectionIsNumber()
        {
            var selectionEnd = SelectionStart + SelectionLength;

            for (int i = SelectionStart; i < selectionEnd; ++i)
            {
                if (!char.IsDigit(CurrentText[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int CountCurrentTextFractions()
        {
            int fractionCount = 0;

            var textSplits = CurrentText.Split(_decimalSign);

            if (textSplits.Length > 1)
            {
                foreach (var c in textSplits[1])
                {
                    if (char.IsDigit(c))
                    {
                        fractionCount++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return fractionCount;
        }

        public bool IsNumber(string str)
        {
            double testCandidate;
            return TryParseAsDouble(str, out testCandidate);
        }

        public bool TryParseAsDouble(string str, out double value)
        {
            return MathUtilities.TryParseAsDouble(str, out value);
        }

        //If anyone has better solution, please do it and make sure the unit tests are ok :)
        public bool AddStringCheckIsOk(string newStringToAdd)
        {
            //Calculate how many fractions currently it has until non digit char (like if has degrees symbol or such)
            int currFractionCount = CountCurrentTextFractions();

            bool isHasDecimal = CurrentText.IndexOf(_decimalSign) != -1;
            bool isCaretAtDecimal = CaretIndex > CurrentText.IndexOf(_decimalSign);
            bool isFractionLessThanDecimalPlaces = currFractionCount < DecimalPlaces;

            bool isSelectionsAllNumbers = ((SelectionLength > 0) && SelectionIsNumber());
            bool isSelectionOnDecimalToEnd = (isCaretAtDecimal && (SelectionLength > 1) &&
                                                                (SelectionStart + SelectionLength) == CurrentText.Length);

            if (newStringToAdd.Contains("-") && (SelectionStart == 0 && SelectionLength > 0 && CurrentText.Contains('-')))
            { 
                return true;
            }

            if (newStringToAdd.Contains("-") && CaretIndex == 0 && CurrentText.Contains('-'))
            {
                return false;
            }

            if (newStringToAdd.Contains("-") && CaretIndex != 0)
            {
                return false;
            }

            //#1 GIVEN 0.0 WHEN caret is the most left THEN user can key in negative
            //#2 GIVEN -12.0 WHEN selection starts at most left |-1|2.0 THEN user can key in negative
            if (newStringToAdd.Contains("-") && (CaretIndex == 0 && !CurrentText.Contains('-')))
            {
                return true;
            }

            if (newStringToAdd.Equals(_decimalSign.ToString()) && !(isHasDecimal))
            {
                return true;
            }

            //GIVEN 0.0123 WHEN caret is at 0.01v23 and user paste 666 THEN don't change the current number
            if (isCaretAtDecimal && SelectionLength == 0 && newStringToAdd.Length + currFractionCount > DecimalPlaces)
            {
                return false;
            }

            //GIVEN 0.0123 WHEN selection 0.0[12]3 and user paste 666 THEN don't change the current number
            if (isCaretAtDecimal && SelectionLength > 0 && newStringToAdd.Length + (currFractionCount - SelectionLength) > DecimalPlaces)
            { 
                return false;
            }

            if (isCaretAtDecimal && isSelectionsAllNumbers && (newStringToAdd.Length > DecimalPlaces))
            {
                return false;
            }

            //Can key in if input is a number, this is absolute
            //#1 GIVEN 0.0 WHEN caret is not on decimals THEN user can key in numbers.
            //#2 GIVEN 0.023° WHEN selections are (0.|023|°) THEN user can key in numbers to override.
            //#3 GIVEN 0.023° WHEN selections are  (0.0|23°|) THEN user can key in numbers to override
            //#4 GIVEN 0.0° and DecimalPlaces = 2 WHEN caret is on decimal places and current decimal places is 1 THEN can key in a number
            if (IsNumber(newStringToAdd) &&
                (!isCaretAtDecimal || isSelectionsAllNumbers || isSelectionOnDecimalToEnd || isFractionLessThanDecimalPlaces))
            { 
                return true;
            }

            return false;
        }

    }
}
