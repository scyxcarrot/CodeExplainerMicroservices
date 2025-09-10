using IDS.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDS.CMF.Utilities
{
    public class FormulaParser
    {
        private const string Add = "+";
        private const string Minus = "-";
        private const string Multiply = "*";
        private const string Divide = "/";
        private const string OpenBracket = "(";
        private const string CloseBracket = ")";

        public Func<double> Parse(string formulaStr)
        {
            return () =>
            {
                var segments = SplitByBrackets(formulaStr);
                return Calculate(segments);
            };
        }

        public Func<double> Parse(string formulaStrWithVariables, Dictionary<string, double> variables)
        {
            var formulaStr = formulaStrWithVariables;
            foreach (var variable in variables)
            {
                formulaStr = formulaStr.Replace(variable.Key, variable.Value.ToInvariantCultureString());
            }
            return Parse(formulaStr);
        }

        private string[] SplitByBrackets(string formulaStr)
        {
            var split = Regex.Split(formulaStr, $"([{OpenBracket}])|([{CloseBracket}])");
            return split.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        private string[] SplitByOperators(string formulaStr)
        {
            var split = Regex.Split(formulaStr, $"([{Add}])|([{Minus}])|([{Multiply}])|([{Divide}])");
            return split.Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        private bool CanOperate(string[] operations)
        {
            //{constant} {operator} {constant}
            double value;
            return (operations.Length >= 3) && (operations.Length - 1) % 2 == 0 && TryParseSegmentToDouble(operations[0], out value);
        }

        private double Operate(string[] operations)
        {
            var result = 0.0;
            
            if (operations.Length > 0)
            {
                result = operations[0].ToInvariantCultureDouble();
                for (var i = 1; i < operations.Length; i = i + 2)
                {
                    var operation = operations[i];
                    switch (operation)
                    {
                        case Add:
                            result = result + operations[i + 1].ToInvariantCultureDouble();
                            break;
                        case Minus:
                            result = result - operations[i + 1].ToInvariantCultureDouble();
                            break;
                        case Multiply:
                            result = result * operations[i + 1].ToInvariantCultureDouble();
                            break;
                        case Divide:
                            result = result / operations[i + 1].ToInvariantCultureDouble();
                            break;
                    }
                }
            }

            return result;
        }

        private double Calculate(string[] segments)
        {
            var operations = new List<string>();

            for (var i = 0; i < segments.Length; i++)
            {
                var segment = segments[i];

                double value;
                if (!TryParseSegmentToDouble(segment, out value))
                {
                    var ops = SplitByOperators(segment);
                    if (CanOperate(ops))
                    {
                        var operation = Operate(ops);
                        if (operations.Any() && RemoveBracket(operations, segments[i + 1]))
                        {
                            i++;
                        }
                        operations.Add(operation.ToInvariantCultureString());
                    }
                    else
                    {
                        operations.AddRange(ops);
                    }
                }
                else
                {
                    if (operations.Any() && RemoveBracket(operations, segments[i + 1]))
                    {
                        i++;
                    }
                    operations.Add(segment);
                }
            }

            if (operations.Contains(OpenBracket) || operations.Contains(CloseBracket))
            {
                if (operations.Count == segments.Length && CanOperate(operations.ToArray()))
                {
                    return OperateInBracket(operations);
                }
                return Calculate(operations.ToArray());
            }
            return Operate(operations.ToArray());
        }

        private double OperateInBracket(List<string> operations)
        {
            var startIndex = operations.IndexOf(OpenBracket);
            var endIndex = operations.LastIndexOf(CloseBracket);
            var operationsInBracket = operations.GetRange(startIndex + 1, endIndex - startIndex - 1);
            if (operationsInBracket.Contains(OpenBracket) || operationsInBracket.Contains(CloseBracket))
            {
                return OperateInBracket(operationsInBracket);
            }

            var operation = Operate(operationsInBracket.ToArray());
            operations.RemoveRange(startIndex, endIndex - startIndex + 1);
            operations.Insert(startIndex, operation.ToInvariantCultureString());

            return Operate(operations.ToArray());
        }

        private bool RemoveBracket(List<string> operations, string nextSegment)
        {
            if (operations.Last() == OpenBracket && nextSegment == CloseBracket)
            {
                operations.RemoveAt(operations.Count - 1);
                return true;
            }
            return false;
        }

        public bool TryParseSegmentToDouble(string value, out double result)
        {
            return double.TryParse(value,
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out result);
        }
    }
}
