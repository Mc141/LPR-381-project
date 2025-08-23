using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    // Represents the result of range analysis for sensitivity analysis
    public class RangeAnalysisResult
    {
        public List<VariableRange> VariableRanges { get; set; } = new();
        public List<ConstraintRange> ConstraintRanges { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    // Represents the allowable range for a variable coefficient
    public class VariableRange
    {
        public string VariableName { get; set; } = string.Empty;
        public double CurrentCoefficient { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public bool IsBasic { get; set; }
        public string Interpretation { get; set; } = string.Empty;
        
        public bool HasLowerBound => !double.IsNegativeInfinity(LowerBound);
        public bool HasUpperBound => !double.IsPositiveInfinity(UpperBound);
        
        public string FormattedRange
        {
            get
            {
                var lower = HasLowerBound ? LowerBound.ToString("F3") : "-?";
                var upper = HasUpperBound ? UpperBound.ToString("F3") : "+?";
                return $"[{lower}, {upper}]";
            }
        }
    }

    // Represents the allowable range for a constraint RHS value
    public class ConstraintRange
    {
        public string ConstraintName { get; set; } = string.Empty;
        public double CurrentRHS { get; set; }
        public double LowerBound { get; set; }
        public double UpperBound { get; set; }
        public string Interpretation { get; set; } = string.Empty;
        
        public bool HasLowerBound => !double.IsNegativeInfinity(LowerBound);
        public bool HasUpperBound => !double.IsPositiveInfinity(UpperBound);
        
        public string FormattedRange
        {
            get
            {
                var lower = HasLowerBound ? LowerBound.ToString("F3") : "-?";
                var upper = HasUpperBound ? UpperBound.ToString("F3") : "+?";
                return $"[{lower}, {upper}]";
            }
        }
    }

    // Enumeration for different types of range analysis
    public enum RangeAnalysisType
    {
        ObjectiveCoefficient,
        ConstraintRHS,
        BasicVariable,
        NonBasicVariable
    }
}