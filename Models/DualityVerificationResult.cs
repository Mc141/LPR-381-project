using System;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Result of duality verification analysis
    /// </summary>
    public class DualityVerificationResult
    {
        public double PrimalOptimalValue { get; set; }
        public double DualOptimalValue { get; set; }
        public double Tolerance { get; set; } = 0.001;
        public bool WeakDualityHolds { get; set; }
        public bool StrongDualityHolds { get; set; }
        public DualityType DualityType { get; set; }
        public string Interpretation { get; set; } = string.Empty;
        public double DualityGap => Math.Abs(PrimalOptimalValue - DualOptimalValue);
        
        public string Summary => $"Duality Type: {DualityType}, Gap: {DualityGap:F6}";
        
        /// <summary>
        /// Gets a detailed report of the duality verification
        /// </summary>
        public string DetailedReport => 
            $"Primal Optimal Value: {PrimalOptimalValue:F3}\n" +
            $"Dual Optimal Value: {DualOptimalValue:F3}\n" +
            $"Duality Gap: {DualityGap:F6}\n" +
            $"Tolerance: {Tolerance:F6}\n" +
            $"Weak Duality: {(WeakDualityHolds ? "? Satisfied" : "? Violated")}\n" +
            $"Strong Duality: {(StrongDualityHolds ? "? Satisfied" : "? Violated")}\n" +
            $"Duality Type: {DualityType}\n" +
            $"Interpretation: {Interpretation}";
    }
    
    /// <summary>
    /// Enumeration for different types of duality relationships
    /// </summary>
    public enum DualityType
    {
        None,    // No duality relationship holds
        Weak,    // Only weak duality holds (dual provides bound)
        Strong   // Strong duality holds (primal = dual at optimality)
    }
}