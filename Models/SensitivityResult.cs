using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Represents the result of a sensitivity analysis operation
    /// </summary>
    public class SensitivityResult
    {
        public string OperationType { get; set; } = string.Empty;
        public bool IsSuccessful { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public Dictionary<string, object> Changes { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public LPModel? OriginalModel { get; set; }
        public LPModel? ModifiedModel { get; set; }
        
        /// <summary>
        /// Summary of the operation for display purposes
        /// </summary>
        public string Summary 
        { 
            get 
            {
                if (IsSuccessful)
                {
                    return $"{OperationType} completed successfully. {Status}";
                }
                else
                {
                    return $"{OperationType} failed: {Status}";
                }
            } 
        }
        
        /// <summary>
        /// Formatted details for display in UI
        /// </summary>
        public List<string> FormattedResults
        {
            get
            {
                var results = new List<string> { Summary };
                
                if (!string.IsNullOrEmpty(Details))
                {
                    results.Add($"Details: {Details}");
                }
                
                if (Changes.Count > 0)
                {
                    results.Add("Changes:");
                    foreach (var change in Changes)
                    {
                        results.Add($"  • {change.Key}: {change.Value}");
                    }
                }
                
                if (Warnings.Count > 0)
                {
                    results.AddRange(Warnings.ConvertAll(w => $"Warning: {w}"));
                }
                
                if (Recommendations.Count > 0)
                {
                    results.Add("Recommendations:");
                    results.AddRange(Recommendations.ConvertAll(r => $"  • {r}"));
                }
                
                return results;
            }
        }
    }
}