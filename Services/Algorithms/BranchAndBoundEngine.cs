using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LPR381_Assignment.Models;
using LPR381_Assignment.Services.Algorithms;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Main engine for Branch and Bound algorithms
    /// Coordinates between simplex-based and knapsack-specific implementations
    /// </summary>
    public class BranchAndBoundEngine
    {
        private readonly SimplexEngine _simplexEngine;
        private readonly BranchAndBoundSimplex _bnbSimplex;
        private readonly BranchAndBoundKnapsack _bnbKnapsack;
        
        public BranchAndBoundEngine()
        {
            _simplexEngine = new SimplexEngine();
            _bnbSimplex = new BranchAndBoundSimplex(_simplexEngine);
            _bnbKnapsack = new BranchAndBoundKnapsack();
        }
        
        /// <summary>
        /// Solves an integer programming problem using Branch and Bound
        /// </summary>
        /// <param name="model">Integer programming model</param>
        /// <param name="algorithmType">Type of B&B algorithm to use</param>
        /// <returns>Solution result with branch and bound tree</returns>
        public BranchAndBoundResult Solve(LPModel model, BranchAndBoundType algorithmType)
        {
            var stopwatch = Stopwatch.StartNew();
            
            var result = new BranchAndBoundResult
            {
                AlgorithmUsed = algorithmType.ToString(),
                OriginalModel = model,
                SolveStartTime = DateTime.Now
            };
            
            try
            {
                // Validate that the model has integer variables
                var integerVars = GetIntegerVariables(model);
                if (integerVars.Count == 0)
                {
                    result.IsSuccessful = false;
                    result.Status = SolutionStatus.Error;
                    result.ErrorMessage = "No integer variables found in the model. Use LP algorithms for continuous problems.";
                    return result;
                }
                
                result.IntegerVariables = integerVars;
                
                // Route to appropriate algorithm
                switch (algorithmType)
                {
                    case BranchAndBoundType.Simplex:
                        var bnbResult = _bnbSimplex.Solve(model);
                        if (bnbResult is BranchAndBoundResult specificResult)
                        {
                            result = specificResult;
                        }
                        else
                        {
                            // Convert SolverResult to BranchAndBoundResult
                            result.IsSuccessful = bnbResult.IsSuccessful;
                            result.Status = bnbResult.Status;
                            result.ObjectiveValue = bnbResult.ObjectiveValue;
                            result.Solution = bnbResult.Solution;
                            result.ErrorMessage = bnbResult.ErrorMessage;
                            result.FinalTableau = bnbResult.FinalTableau;
                            result.Iterations = bnbResult.Iterations;
                        }
                        break;
                        
                    case BranchAndBoundType.Knapsack:
                        // Check if model is suitable for knapsack algorithm
                        if (IsKnapsackProblem(model))
                        {
                            var knapsackResult = _bnbKnapsack.Solve(model);
                            if (knapsackResult is BranchAndBoundResult knapsackBnBResult)
                            {
                                result = knapsackBnBResult;
                            }
                            else
                            {
                                // Convert SolverResult to BranchAndBoundResult
                                result.IsSuccessful = knapsackResult.IsSuccessful;
                                result.Status = knapsackResult.Status;
                                result.ObjectiveValue = knapsackResult.ObjectiveValue;
                                result.Solution = knapsackResult.Solution;
                                result.ErrorMessage = knapsackResult.ErrorMessage;
                                result.FinalTableau = knapsackResult.FinalTableau;
                                result.Iterations = knapsackResult.Iterations;
                            }
                        }
                        else
                        {
                            result.IsSuccessful = false;
                            result.Status = SolutionStatus.Error;
                            result.ErrorMessage = "Model is not suitable for knapsack-specific Branch and Bound. Use Simplex-based B&B instead.";
                        }
                        break;
                        
                    default:
                        result.IsSuccessful = false;
                        result.Status = SolutionStatus.Error;
                        result.ErrorMessage = $"Unknown Branch and Bound algorithm type: {algorithmType}";
                        break;
                }
                
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.SolveEndTime = DateTime.Now;
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.IsSuccessful = false;
                result.Status = SolutionStatus.Error;
                result.ErrorMessage = $"Error during Branch and Bound solving: {ex.Message}";
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.SolveEndTime = DateTime.Now;
                return result;
            }
        }
        
        /// <summary>
        /// Gets all integer and binary variables from the model
        /// </summary>
        private List<string> GetIntegerVariables(LPModel model)
        {
            return model.Variables.Values
                .Where(v => v.SignRestriction == SignRestriction.Integer || 
                           v.SignRestriction == SignRestriction.Binary)
                .Select(v => v.Name)
                .ToList();
        }
        
        /// <summary>
        /// Determines if a model is suitable for knapsack-specific algorithms
        /// </summary>
        private bool IsKnapsackProblem(LPModel model)
        {
            // Basic knapsack characteristics:
            // 1. All variables are binary (0-1)
            // 2. Single constraint (capacity constraint)
            // 3. All coefficients in constraint are positive
            // 4. Maximization problem
            
            // Check if all variables are binary
            bool allBinary = model.Variables.Values.All(v => 
                v.SignRestriction == SignRestriction.Binary);
            
            if (!allBinary) return false;
            
            // Check if it's a maximization problem
            if (model.Sense != ObjectiveSense.Maximize) return false;
            
            // Check if there's exactly one constraint (excluding bounds)
            var mainConstraints = model.Constraints.Where(c => 
                c.Relation == ConstraintRelation.LessThanEqual && c.RHS > 0).ToList();
            
            if (mainConstraints.Count != 1) return false;
            
            // Check if all coefficients in the constraint are positive
            var constraint = mainConstraints[0];
            bool allPositiveCoeffs = constraint.Coefficients.Values.All(coeff => coeff > 0);
            
            return allPositiveCoeffs;
        }
    }
    
    /// <summary>
    /// Type of Branch and Bound algorithm to use
    /// </summary>
    public enum BranchAndBoundType
    {
        /// <summary>
        /// Simplex-based Branch and Bound for general integer programming
        /// </summary>
        Simplex,
        
        /// <summary>
        /// Specialized Branch and Bound for knapsack problems
        /// </summary>
        Knapsack
    }
    
    /// <summary>
    /// Result of a Branch and Bound solve operation
    /// </summary>
    public class BranchAndBoundResult : SolverResult
    {
        /// <summary>
        /// Root node of the Branch and Bound tree
        /// </summary>
        public BranchNode? RootNode { get; set; }
        
        /// <summary>
        /// All nodes created during the solve process
        /// </summary>
        public List<BranchNode> AllNodes { get; set; } = new();
        
        /// <summary>
        /// Best integer solution found
        /// </summary>
        public IntegerSolution? BestIntegerSolution { get; set; }
        
        /// <summary>
        /// LP relaxation bound from the root node
        /// </summary>
        public double RootBound { get; set; } = double.NaN;
        
        /// <summary>
        /// Final optimality gap (if applicable)
        /// </summary>
        public double OptimalityGap { get; set; } = double.NaN;
        
        /// <summary>
        /// Variables that were treated as integer
        /// </summary>
        public List<string> IntegerVariables { get; set; } = new();
        
        /// <summary>
        /// Number of nodes processed
        /// </summary>
        public int NodesProcessed => AllNodes.Count;
        
        /// <summary>
        /// Number of nodes fathomed
        /// </summary>
        public int NodesFathomed => AllNodes.Count(n => 
            n.Status == NodeStatus.FathomedByBound ||
            n.Status == NodeStatus.FathomedByInfeasibility ||
            n.Status == NodeStatus.FathomedByIntegrality);
        
        /// <summary>
        /// Creates a summary of the Branch and Bound results
        /// </summary>
        public new string CreateSummary()
        {
            var summary = new List<string>
            {
                "=== BRANCH AND BOUND RESULTS ===",
                "",
                $"Algorithm: {AlgorithmUsed}",
                $"Status: {Status}",
                $"Execution Time: {ExecutionTimeMs:F2} ms",
                ""
            };
            
            if (IsSuccessful && BestIntegerSolution != null)
            {
                summary.Add($"Optimal Integer Value: {BestIntegerSolution.ObjectiveValue:F3}");
                summary.Add($"Root LP Bound: {RootBound:F3}");
                if (!double.IsNaN(OptimalityGap))
                {
                    summary.Add($"Optimality Gap: {OptimalityGap:F3}%");
                }
                summary.Add("");
                summary.Add("Integer Solution:");
                foreach (var variable in BestIntegerSolution.Variables.OrderBy(kv => kv.Key))
                {
                    summary.Add($"  {variable.Key} = {variable.Value:F0}");
                }
            }
            else if (!string.IsNullOrEmpty(ErrorMessage))
            {
                summary.Add($"Error: {ErrorMessage}");
            }
            
            summary.Add("");
            summary.Add("Branch and Bound Statistics:");
            summary.Add($"  Nodes Created: {NodesProcessed}");
            summary.Add($"  Nodes Fathomed: {NodesFathomed}");
            summary.Add($"  Integer Variables: {IntegerVariables.Count} ({string.Join(", ", IntegerVariables)})");
            
            if (Warnings.Count > 0)
            {
                summary.Add("");
                summary.Add("Warnings:");
                foreach (var warning in Warnings)
                {
                    summary.Add($"  • {warning}");
                }
            }
            
            return string.Join(Environment.NewLine, summary);
        }
    }
}