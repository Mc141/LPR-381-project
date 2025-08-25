using System;
using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Represents an integer solution found during Branch and Bound
    /// </summary>
    public class IntegerSolution
    {
        /// <summary>
        /// Variable values in the integer solution
        /// </summary>
        public Dictionary<string, double> Variables { get; set; } = new();
        
        /// <summary>
        /// Objective function value
        /// </summary>
        public double ObjectiveValue { get; set; }
        
        /// <summary>
        /// Whether this is a feasible integer solution
        /// </summary>
        public bool IsFeasible { get; set; }
        
        /// <summary>
        /// Node where this solution was found
        /// </summary>
        public int NodeId { get; set; }
        
        /// <summary>
        /// When this solution was found
        /// </summary>
        public DateTime FoundAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Algorithm used to find this solution
        /// </summary>
        public string Algorithm { get; set; } = string.Empty;
        
        /// <summary>
        /// Time taken to find this solution (in milliseconds)
        /// </summary>
        public double SolutionTimeMs { get; set; }
        
        /// <summary>
        /// Checks if all integer variables have integer values
        /// </summary>
        /// <param name="integerVariables">List of variables that must be integer</param>
        /// <param name="tolerance">Tolerance for checking integrality</param>
        /// <returns>True if all integer variables are integral</returns>
        public bool IsIntegral(IEnumerable<string> integerVariables, double tolerance = 1e-6)
        {
            foreach (var varName in integerVariables)
            {
                if (Variables.TryGetValue(varName, out var value))
                {
                    if (Math.Abs(value - Math.Round(value)) > tolerance)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// Gets the integer variables that are not integral
        /// </summary>
        /// <param name="integerVariables">List of variables that must be integer</param>
        /// <param name="tolerance">Tolerance for checking integrality</param>
        /// <returns>List of non-integral integer variables</returns>
        public List<(string Variable, double Value, double Fractional)> GetNonIntegralVariables(
            IEnumerable<string> integerVariables, double tolerance = 1e-6)
        {
            var nonIntegral = new List<(string, double, double)>();
            
            foreach (var varName in integerVariables)
            {
                if (Variables.TryGetValue(varName, out var value))
                {
                    var fractional = Math.Abs(value - Math.Round(value));
                    if (fractional > tolerance)
                    {
                        nonIntegral.Add((varName, value, fractional));
                    }
                }
            }
            
            return nonIntegral;
        }
        
        /// <summary>
        /// Creates a formatted string representation of the solution
        /// </summary>
        public string ToFormattedString()
        {
            var lines = new List<string>
            {
                $"Objective Value: {ObjectiveValue:F3}",
                $"Feasible: {(IsFeasible ? "Yes" : "No")}",
                $"Found at Node: {NodeId}",
                $"Solution Time: {SolutionTimeMs:F2} ms",
                "Variables:"
            };
            
            foreach (var variable in Variables)
            {
                lines.Add($"  {variable.Key} = {variable.Value:F3}");
            }
            
            return string.Join(Environment.NewLine, lines);
        }
        
        /// <summary>
        /// Creates a copy of this solution
        /// </summary>
        public IntegerSolution Clone()
        {
            return new IntegerSolution
            {
                Variables = new Dictionary<string, double>(Variables),
                ObjectiveValue = ObjectiveValue,
                IsFeasible = IsFeasible,
                NodeId = NodeId,
                FoundAt = FoundAt,
                Algorithm = Algorithm,
                SolutionTimeMs = SolutionTimeMs
            };
        }
        
        /// <summary>
        /// Compares this solution with another for optimality
        /// </summary>
        /// <param name="other">Other solution to compare with</param>
        /// <param name="isMaximization">True if this is a maximization problem</param>
        /// <returns>True if this solution is better than the other</returns>
        public bool IsBetterThan(IntegerSolution other, bool isMaximization)
        {
            if (!IsFeasible) return false;
            if (!other.IsFeasible) return true;
            
            return isMaximization ? 
                ObjectiveValue > other.ObjectiveValue : 
                ObjectiveValue < other.ObjectiveValue;
        }
    }
}