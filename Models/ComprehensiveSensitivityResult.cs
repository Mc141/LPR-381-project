using System;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Result of comprehensive sensitivity analysis containing all analysis types
    /// </summary>
    public class ComprehensiveSensitivityResult
    {
        public bool IsSuccessful { get; set; }
        public string Status { get; set; } = string.Empty;
        public RangeAnalysisResult? VariableRanges { get; set; }
        public RangeAnalysisResult? ConstraintRanges { get; set; }
        public ShadowPriceResult? ShadowPrices { get; set; }
        public DualModelResult? DualModelResult { get; set; }
        public DateTime AnalysisTimestamp { get; set; } = DateTime.Now;
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString("N")[..8];
        
        public string Summary => $"Analysis {AnalysisId} completed at {AnalysisTimestamp:yyyy-MM-dd HH:mm:ss}. Status: {Status}";
        
        /// <summary>
        /// Gets a comprehensive report of all analyses performed
        /// </summary>
        public string ComprehensiveReport
        {
            get
            {
                var report = new System.Text.StringBuilder();
                report.AppendLine("=== COMPREHENSIVE SENSITIVITY ANALYSIS REPORT ===");
                report.AppendLine($"Analysis ID: {AnalysisId}");
                report.AppendLine($"Timestamp: {AnalysisTimestamp:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine($"Status: {Status}");
                report.AppendLine();
                
                if (VariableRanges != null)
                {
                    report.AppendLine("VARIABLE RANGE ANALYSIS:");
                    report.AppendLine($"  Variables analyzed: {VariableRanges.VariableRanges.Count}");
                    report.AppendLine($"  Status: {VariableRanges.Status}");
                    report.AppendLine($"  Notes: {VariableRanges.Notes}");
                    report.AppendLine();
                }
                
                if (ConstraintRanges != null)
                {
                    report.AppendLine("CONSTRAINT RHS ANALYSIS:");
                    report.AppendLine($"  Constraints analyzed: {ConstraintRanges.ConstraintRanges.Count}");
                    report.AppendLine($"  Status: {ConstraintRanges.Status}");
                    report.AppendLine($"  Notes: {ConstraintRanges.Notes}");
                    report.AppendLine();
                }
                
                if (ShadowPrices != null)
                {
                    report.AppendLine("SHADOW PRICE ANALYSIS:");
                    report.AppendLine($"  Shadow prices calculated: {ShadowPrices.ShadowPrices.Count}");
                    report.AppendLine($"  Optimal value: {ShadowPrices.OptimalValue:F3}");
                    report.AppendLine($"  Status: {ShadowPrices.Status}");
                    report.AppendLine();
                }
                
                if (DualModelResult != null)
                {
                    report.AppendLine("DUAL MODEL ANALYSIS:");
                    report.AppendLine($"  Dual variables: {DualModelResult.DualModel?.Variables.Count ?? 0}");
                    report.AppendLine($"  Dual constraints: {DualModelResult.DualModel?.Constraints.Count ?? 0}");
                    report.AppendLine($"  Status: {DualModelResult.Status}");
                    report.AppendLine();
                }
                
                return report.ToString();
            }
        }
        
        /// <summary>
        /// Validates that all required analyses were completed successfully
        /// </summary>
        public bool IsComplete => IsSuccessful && 
                                 VariableRanges != null && 
                                 ConstraintRanges != null && 
                                 ShadowPrices != null && 
                                 DualModelResult != null;
        
        /// <summary>
        /// Gets the number of successful analyses performed
        /// </summary>
        public int SuccessfulAnalysesCount
        {
            get
            {
                int count = 0;
                if (VariableRanges != null && !string.IsNullOrEmpty(VariableRanges.Status)) count++;
                if (ConstraintRanges != null && !string.IsNullOrEmpty(ConstraintRanges.Status)) count++;
                if (ShadowPrices != null && !string.IsNullOrEmpty(ShadowPrices.Status)) count++;
                if (DualModelResult != null && DualModelResult.IsSuccessful) count++;
                return count;
            }
        }
    }
}