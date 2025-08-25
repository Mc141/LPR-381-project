using System;
using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Represents a cutting plane used in the Cutting Plane algorithm
    /// </summary>
    public class CuttingPlane
    {
        /// <summary>
        /// Unique identifier for this cut
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Type of cutting plane
        /// </summary>
        public CutType Type { get; set; }
        
        /// <summary>
        /// Coefficients of the cutting plane constraint
        /// </summary>
        public Dictionary<string, double> Coefficients { get; set; } = new();
        
        /// <summary>
        /// Right-hand side value of the cut
        /// </summary>
        public double RHS { get; set; }
        
        /// <summary>
        /// Relation type (typically <=)
        /// </summary>
        public ConstraintRelation Relation { get; set; } = ConstraintRelation.LessThanEqual;
        
        /// <summary>
        /// Source variable/constraint that generated this cut
        /// </summary>
        public string Source { get; set; } = string.Empty;
        
        /// <summary>
        /// Violation amount when this cut was generated
        /// </summary>
        public double Violation { get; set; }
        
        /// <summary>
        /// Iteration when this cut was added
        /// </summary>
        public int Iteration { get; set; }
        
        /// <summary>
        /// When this cut was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Whether this cut is currently active in the model
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// Additional notes about this cut
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        
        /// <summary>
        /// Creates a constraint representation of this cut
        /// </summary>
        public Constraint ToConstraint()
        {
            return new Constraint
            {
                Name = $"Cut_{Id}",
                Coefficients = new Dictionary<string, double>(Coefficients),
                Relation = Relation,
                RHS = RHS
            };
        }
        
        /// <summary>
        /// Evaluates this cut at a given solution point
        /// </summary>
        /// <param name="solution">Solution to evaluate</param>
        /// <returns>Left-hand side value of the cut at this solution</returns>
        public double EvaluateAt(Dictionary<string, double> solution)
        {
            double lhs = 0;
            foreach (var coeff in Coefficients)
            {
                if (solution.TryGetValue(coeff.Key, out var value))
                {
                    lhs += coeff.Value * value;
                }
            }
            return lhs;
        }
        
        /// <summary>
        /// Checks if this cut is violated by a given solution
        /// </summary>
        /// <param name="solution">Solution to check</param>
        /// <param name="tolerance">Tolerance for violation checking</param>
        /// <returns>True if the cut is violated</returns>
        public bool IsViolatedBy(Dictionary<string, double> solution, double tolerance = 1e-6)
        {
            double lhs = EvaluateAt(solution);
            return Relation switch
            {
                ConstraintRelation.LessThanEqual => lhs > RHS + tolerance,
                ConstraintRelation.GreaterThanEqual => lhs < RHS - tolerance,
                ConstraintRelation.Equal => Math.Abs(lhs - RHS) > tolerance,
                _ => false
            };
        }
        
        /// <summary>
        /// Gets the violation amount for a given solution
        /// </summary>
        /// <param name="solution">Solution to check</param>
        /// <returns>Amount of violation (positive if violated, negative if satisfied)</returns>
        public double GetViolationAmount(Dictionary<string, double> solution)
        {
            double lhs = EvaluateAt(solution);
            return Relation switch
            {
                ConstraintRelation.LessThanEqual => lhs - RHS,
                ConstraintRelation.GreaterThanEqual => RHS - lhs,
                ConstraintRelation.Equal => Math.Abs(lhs - RHS),
                _ => 0
            };
        }
        
        /// <summary>
        /// Creates a formatted string representation of this cut
        /// </summary>
        public string ToFormattedString()
        {
            var terms = new List<string>();
            foreach (var coeff in Coefficients)
            {
                if (Math.Abs(coeff.Value) < 1e-10) continue;
                
                var sign = coeff.Value >= 0 ? "+" : "-";
                var absValue = Math.Abs(coeff.Value);
                
                if (terms.Count == 0 && sign == "+")
                    sign = ""; // Don't show + for first term
                    
                terms.Add($"{sign}{absValue:F3}{coeff.Key}");
            }
            
            var relationStr = Relation switch
            {
                ConstraintRelation.LessThanEqual => "?",
                ConstraintRelation.GreaterThanEqual => "?",
                ConstraintRelation.Equal => "=",
                _ => "?"
            };
            
            return $"{string.Join(" ", terms)} {relationStr} {RHS:F3}";
        }
        
        /// <summary>
        /// Creates a display string for UI purposes
        /// </summary>
        public string ToDisplayString()
        {
            var typeStr = Type switch
            {
                CutType.Gomory => "Gomory",
                CutType.Fractional => "Fractional",
                CutType.Mixed => "Mixed",
                CutType.Knapsack => "Knapsack",
                _ => "Unknown"
            };
            
            return $"[{typeStr}] {ToFormattedString()}";
        }
    }
    
    /// <summary>
    /// Types of cutting planes
    /// </summary>
    public enum CutType
    {
        /// <summary>
        /// Standard Gomory fractional cut
        /// </summary>
        Gomory,
        
        /// <summary>
        /// Fractional cut based on fractional variables
        /// </summary>
        Fractional,
        
        /// <summary>
        /// Mixed integer Gomory cut
        /// </summary>
        Mixed,
        
        /// <summary>
        /// Knapsack-based cut
        /// </summary>
        Knapsack,
        
        /// <summary>
        /// Cover inequalities
        /// </summary>
        Cover,
        
        /// <summary>
        /// User-defined cut
        /// </summary>
        UserDefined
    }
}