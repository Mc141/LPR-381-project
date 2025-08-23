using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    // Represents a shadow price for a constraint
    public class ShadowPrice
    {
        public string ConstraintName { get; set; } = "";
        public double Value { get; set; }
        public string Interpretation { get; set; } = "";
        public bool IsActive { get; set; }
        
        public override string ToString()
        {
            var status = IsActive ? "Active" : "Inactive";
            return $"{ConstraintName}: {Value:F3} ({status}) - {Interpretation}";
        }
    }

    // Contains results of shadow price analysis
    public class ShadowPriceResult
    {
        public List<ShadowPrice> ShadowPrices { get; set; } = new();
        public double OptimalValue { get; set; }
        public string Status { get; set; } = "";
        public string Notes { get; set; } = "";
    }
}