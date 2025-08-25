using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Comprehensive result from solving an LP/IP problem
    /// </summary>
    public class SolverResult
    {
        /// <summary>
        /// Whether the solving process completed successfully
        /// </summary>
        public bool IsSuccessful { get; set; }
        
        /// <summary>
        /// Status of the solution
        /// </summary>
        public SolutionStatus Status { get; set; }
        
        /// <summary>
        /// Final objective value
        /// </summary>
        public double ObjectiveValue { get; set; }
        
        /// <summary>
        /// Optimal solution values for all variables
        /// </summary>
        public Dictionary<string, double> Solution { get; set; } = new();
        
        /// <summary>
        /// Shadow prices for constraints (if available)
        /// </summary>
        public Dictionary<string, double> ShadowPrices { get; set; } = new();
        
        /// <summary>
        /// Reduced costs for variables (if available)
        /// </summary>
        public Dictionary<string, double> ReducedCosts { get; set; } = new();
        
        /// <summary>
        /// Algorithm used to solve the problem
        /// </summary>
        public string AlgorithmUsed { get; set; } = string.Empty;
        
        /// <summary>
        /// Total number of iterations performed
        /// </summary>
        public int IterationCount { get; set; }
        
        /// <summary>
        /// Total execution time in milliseconds
        /// </summary>
        public double ExecutionTimeMs { get; set; }
        
        /// <summary>
        /// All iterations performed during solving
        /// </summary>
        public List<SimplexIteration> Iterations { get; set; } = new();
        
        /// <summary>
        /// Final tableau state
        /// </summary>
        public SimplexTableau? FinalTableau { get; set; }
        
        /// <summary>
        /// Initial tableau state
        /// </summary>
        public SimplexTableau? InitialTableau { get; set; }
        
        /// <summary>
        /// Canonical form of the problem
        /// </summary>
        public CanonicalForm? CanonicalForm { get; set; }
        
        /// <summary>
        /// Error message if solving failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;
        
        /// <summary>
        /// Warnings generated during solving
        /// </summary>
        public List<string> Warnings { get; set; } = new();
        
        /// <summary>
        /// Additional solver information
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
        
        /// <summary>
        /// Model that was solved
        /// </summary>
        public LPModel? OriginalModel { get; set; }
        
        /// <summary>
        /// Timestamp when solving started
        /// </summary>
        public DateTime SolveStartTime { get; set; }
        
        /// <summary>
        /// Timestamp when solving completed
        /// </summary>
        public DateTime SolveEndTime { get; set; }

        /// <summary>
        /// Creates a formatted summary of the solution
        /// </summary>
        public string CreateSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SOLVER RESULT SUMMARY ===");
            sb.AppendLine();
            sb.AppendLine($"Status: {Status}");
            sb.AppendLine($"Algorithm: {AlgorithmUsed}");
            sb.AppendLine($"Execution Time: {ExecutionTimeMs:F2} ms");
            sb.AppendLine($"Iterations: {IterationCount}");
            sb.AppendLine();

            if (Status == SolutionStatus.Optimal)
            {
                sb.AppendLine($"Optimal Objective Value: {ObjectiveValue:F3}");
                sb.AppendLine();
                sb.AppendLine("Optimal Solution:");
                foreach (var variable in Solution.OrderBy(kvp => kvp.Key))
                {
                    sb.AppendLine($"  {variable.Key} = {variable.Value:F3}");
                }

                if (ShadowPrices.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Shadow Prices:");
                    foreach (var price in ShadowPrices.OrderBy(kvp => kvp.Key))
                    {
                        sb.AppendLine($"  {price.Key} = {price.Value:F3}");
                    }
                }

                if (ReducedCosts.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("Reduced Costs:");
                    foreach (var cost in ReducedCosts.OrderBy(kvp => kvp.Key))
                    {
                        sb.AppendLine($"  {cost.Key} = {cost.Value:F3}");
                    }
                }
            }
            else if (Status == SolutionStatus.Infeasible)
            {
                sb.AppendLine("The problem has no feasible solution.");
            }
            else if (Status == SolutionStatus.Unbounded)
            {
                sb.AppendLine("The problem has an unbounded optimal solution.");
            }
            else if (!IsSuccessful)
            {
                sb.AppendLine($"Solving failed: {ErrorMessage}");
            }

            if (Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Warnings:");
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  - {warning}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the solution value for a specific variable
        /// </summary>
        public double GetVariableValue(string variableName)
        {
            return Solution.TryGetValue(variableName, out var value) ? value : 0.0;
        }

        /// <summary>
        /// Gets the shadow price for a specific constraint
        /// </summary>
        public double GetShadowPrice(string constraintName)
        {
            return ShadowPrices.TryGetValue(constraintName, out var price) ? price : 0.0;
        }

        /// <summary>
        /// Gets the reduced cost for a specific variable
        /// </summary>
        public double GetReducedCost(string variableName)
        {
            return ReducedCosts.TryGetValue(variableName, out var cost) ? cost : 0.0;
        }

        /// <summary>
        /// Validates the solver result
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (IsSuccessful && Status == SolutionStatus.Error)
                result.AddError("Cannot be successful with error status");

            if (!IsSuccessful && Status == SolutionStatus.Optimal)
                result.AddError("Cannot be unsuccessful with optimal status");

            if (Status == SolutionStatus.Optimal && Solution.Count == 0)
                result.AddWarning("Optimal status but no solution provided");

            if (IterationCount < 0)
                result.AddError("Iteration count cannot be negative");

            if (ExecutionTimeMs < 0)
                result.AddError("Execution time cannot be negative");

            if (FinalTableau != null)
            {
                var tableauValidation = FinalTableau.Validate();
                if (!tableauValidation.IsValid)
                {
                    foreach (var error in tableauValidation.Errors)
                        result.AddError($"Final tableau error: {error}");
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a detailed iteration report
        /// </summary>
        public string CreateIterationReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ITERATION REPORT ===");
            sb.AppendLine();

            if (InitialTableau != null)
            {
                sb.AppendLine("Initial Tableau:");
                sb.AppendLine(InitialTableau.ToFormattedString());
                sb.AppendLine();
            }

            foreach (var iteration in Iterations)
            {
                sb.AppendLine(iteration.CreateSummary());
                
                if (iteration.RatioTests.Count > 0)
                {
                    sb.AppendLine("  Ratio Test Results:");
                    foreach (var ratio in iteration.RatioTests)
                    {
                        sb.AppendLine($"    {ratio}");
                    }
                }

                if (iteration.TableauAfter != null && iteration.IterationNumber <= 5) // Limit detailed output
                {
                    sb.AppendLine("  Resulting Tableau:");
                    sb.AppendLine(iteration.TableauAfter.ToFormattedString());
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Enumeration of possible solution statuses
    /// </summary>
    public enum SolutionStatus
    {
        Unknown,
        Optimal,
        Infeasible,
        Unbounded,
        Degenerate,
        MaxIterationsReached,
        Error,
        Terminated
    }

    /// <summary>
    /// Represents the canonical form of an LP problem
    /// </summary>
    public class CanonicalForm
    {
        /// <summary>
        /// Canonical form tableau
        /// </summary>
        public SimplexTableau Tableau { get; set; }
        
        /// <summary>
        /// Mapping from original variables to canonical variables
        /// </summary>
        public Dictionary<string, string> VariableMapping { get; set; } = new();
        
        /// <summary>
        /// Slack variables added during canonicalization
        /// </summary>
        public List<string> SlackVariables { get; set; } = new();
        
        /// <summary>
        /// Surplus variables added during canonicalization
        /// </summary>
        public List<string> SurplusVariables { get; set; } = new();
        
        /// <summary>
        /// Artificial variables added during canonicalization
        /// </summary>
        public List<string> ArtificialVariables { get; set; } = new();
        
        /// <summary>
        /// Transformation steps applied
        /// </summary>
        public List<string> TransformationSteps { get; set; } = new();
        
        /// <summary>
        /// Whether the canonicalization was successful
        /// </summary>
        public bool IsValid { get; set; }
        
        /// <summary>
        /// Error message if canonicalization failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        public CanonicalForm()
        {
            Tableau = new SimplexTableau(0, 0);
        }

        /// <summary>
        /// Gets a formatted representation of the canonical form
        /// </summary>
        public string ToFormattedString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== CANONICAL FORM ===");
            sb.AppendLine();

            if (!IsValid)
            {
                sb.AppendLine($"Canonicalization failed: {ErrorMessage}");
                return sb.ToString();
            }

            sb.AppendLine("Variable Mapping:");
            foreach (var mapping in VariableMapping)
            {
                sb.AppendLine($"  {mapping.Key} -> {mapping.Value}");
            }

            if (SlackVariables.Count > 0)
            {
                sb.AppendLine($"Slack Variables: {string.Join(", ", SlackVariables)}");
            }

            if (SurplusVariables.Count > 0)
            {
                sb.AppendLine($"Surplus Variables: {string.Join(", ", SurplusVariables)}");
            }

            if (ArtificialVariables.Count > 0)
            {
                sb.AppendLine($"Artificial Variables: {string.Join(", ", ArtificialVariables)}");
            }

            sb.AppendLine();
            sb.AppendLine("Transformation Steps:");
            for (int i = 0; i < TransformationSteps.Count; i++)
            {
                sb.AppendLine($"  {i + 1}. {TransformationSteps[i]}");
            }

            sb.AppendLine();
            sb.AppendLine(Tableau.ToFormattedString());

            return sb.ToString();
        }
    }
}