using System;
using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Represents a single iteration in the simplex algorithm
    /// </summary>
    public class SimplexIteration
    {
        /// <summary>
        /// Iteration number (starts from 0)
        /// </summary>
        public int IterationNumber { get; set; }
        
        /// <summary>
        /// Tableau state before this iteration
        /// </summary>
        public SimplexTableau? TableauBefore { get; set; }
        
        /// <summary>
        /// Tableau state after this iteration
        /// </summary>
        public SimplexTableau? TableauAfter { get; set; }
        
        /// <summary>
        /// Pivot operation performed in this iteration
        /// </summary>
        public PivotOperation? PivotOperation { get; set; }
        
        /// <summary>
        /// Entering variable for this iteration
        /// </summary>
        public string EnteringVariable { get; set; } = string.Empty;
        
        /// <summary>
        /// Leaving variable for this iteration
        /// </summary>
        public string LeavingVariable { get; set; } = string.Empty;
        
        /// <summary>
        /// Objective value at the end of this iteration
        /// </summary>
        public double ObjectiveValue { get; set; }
        
        /// <summary>
        /// Whether this iteration reached optimality
        /// </summary>
        public bool IsOptimal { get; set; }
        
        /// <summary>
        /// Whether infeasibility was detected in this iteration
        /// </summary>
        public bool IsInfeasible { get; set; }
        
        /// <summary>
        /// Whether unboundedness was detected in this iteration
        /// </summary>
        public bool IsUnbounded { get; set; }
        
        /// <summary>
        /// Phase of the simplex algorithm (1 or 2)
        /// </summary>
        public int Phase { get; set; } = 2;
        
        /// <summary>
        /// Detailed description of this iteration step
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp when this iteration was performed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Execution time for this iteration in milliseconds
        /// </summary>
        public double ExecutionTimeMs { get; set; }
        
        /// <summary>
        /// Ratio test results for this iteration
        /// </summary>
        public List<RatioTestResult> RatioTests { get; set; } = new();
        
        /// <summary>
        /// Step-by-step details of the iteration
        /// </summary>
        public List<IterationStep> Steps { get; set; } = new();
        
        /// <summary>
        /// Warnings generated during this iteration
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Creates a summary string for this iteration
        /// </summary>
        public string CreateSummary()
        {
            if (IsOptimal)
                return "Optimal solution found";
            
            if (IsInfeasible)
                return "Infeasibility detected";
            
            if (IsUnbounded)
                return "Unboundedness detected";
            
            if (!string.IsNullOrEmpty(EnteringVariable) && !string.IsNullOrEmpty(LeavingVariable))
                return $"{EnteringVariable} enters, {LeavingVariable} leaves";
            
            if (!string.IsNullOrEmpty(Description))
                return Description;
            
            return "Iteration performed";
        }

        /// <summary>
        /// Gets the current basic solution
        /// </summary>
        public Dictionary<string, double> GetBasicSolution()
        {
            return TableauAfter?.GetSolution() ?? new Dictionary<string, double>();
        }

        /// <summary>
        /// Validates the iteration data
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (IterationNumber < 0)
                result.AddError("Iteration number cannot be negative");

            if (Phase != 1 && Phase != 2)
                result.AddError("Phase must be 1 or 2");

            if (TableauAfter != null)
            {
                var tableauValidation = TableauAfter.Validate();
                if (!tableauValidation.IsValid)
                {
                    foreach (var error in tableauValidation.Errors)
                        result.AddError($"Tableau error: {error}");
                }
            }

            if (IsOptimal && IsInfeasible)
                result.AddError("Cannot be both optimal and infeasible");

            if (IsOptimal && IsUnbounded)
                result.AddError("Cannot be both optimal and unbounded");

            if (IsInfeasible && IsUnbounded)
                result.AddError("Cannot be both infeasible and unbounded");

            return result;
        }

        /// <summary>
        /// Creates a detailed string representation of this iteration
        /// </summary>
        public override string ToString()
        {
            return $"Iteration {IterationNumber}: {CreateSummary()} (Objective: {ObjectiveValue:F3})";
        }
    }

    /// <summary>
    /// Represents a ratio test result for pivot selection
    /// </summary>
    public class RatioTestResult
    {
        public int RowIndex { get; set; }
        public int LeavingIndex { get; set; } = -1; // Add this property for revised simplex
        public string BasicVariable { get; set; } = string.Empty;
        public double RHSValue { get; set; }
        public double PivotColumnValue { get; set; }
        public double Ratio { get; set; }
        public bool IsValidRatio { get; set; }
        public string Notes { get; set; } = string.Empty;

        public override string ToString()
        {
            if (!IsValidRatio)
                return $"Row {RowIndex} ({BasicVariable}): Invalid ratio - {Notes}";
            
            return $"Row {RowIndex} ({BasicVariable}): {RHSValue:F3} / {PivotColumnValue:F3} = {Ratio:F3}";
        }
    }

    /// <summary>
    /// Represents a step within an iteration for detailed tracking
    /// </summary>
    public class IterationStep
    {
        /// <summary>
        /// Step number within the iteration
        /// </summary>
        public int StepNumber { get; set; }
        
        /// <summary>
        /// Type of step (e.g., "Price Out", "Product Form", "Ratio Test")
        /// </summary>
        public string StepType { get; set; } = string.Empty;
        
        /// <summary>
        /// Detailed description of this step
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Values calculated or used in this step
        /// </summary>
        public List<StepValue> Values { get; set; } = new();
        
        /// <summary>
        /// Timestamp when this step was performed
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Represents a value calculated in an iteration step
    /// </summary>
    public class StepValue
    {
        /// <summary>
        /// Name of the value (e.g., "c?_x1", "y_1", "ratio")
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Numerical value
        /// </summary>
        public double Value { get; set; }
        
        /// <summary>
        /// Optional description or notes about this value
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        
        /// <summary>
        /// String representation of the value
        /// </summary>
        public override string ToString()
        {
            return $"{Name} = {Value:F3}";
        }
    }

    /// <summary>
    /// Summary statistics for a complete simplex run
    /// </summary>
    public class SimplexRunSummary
    {
        public int TotalIterations { get; set; }
        public double TotalExecutionTimeMs { get; set; }
        public double AverageIterationTimeMs => TotalIterations > 0 ? TotalExecutionTimeMs / TotalIterations : 0;
        public int Phase1Iterations { get; set; }
        public int Phase2Iterations { get; set; }
        public bool ReachedOptimality { get; set; }
        public bool DetectedInfeasibility { get; set; }
        public bool DetectedUnboundedness { get; set; }
        public double FinalObjectiveValue { get; set; }
        public Dictionary<string, double> OptimalSolution { get; set; } = new();
        public List<string> AlgorithmWarnings { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public override string ToString()
        {
            var status = ReachedOptimality ? "Optimal" : 
                        DetectedInfeasibility ? "Infeasible" : 
                        DetectedUnboundedness ? "Unbounded" : "Unknown";
            
            return $"Simplex Run: {TotalIterations} iterations, {TotalExecutionTimeMs:F1}ms, Status: {status}, Objective: {FinalObjectiveValue:F3}";
        }
    }
}