using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Main solver for Integer Programming problems
    /// Coordinates between different IP algorithms (Branch & Bound, Cutting Plane)
    /// </summary>
    public class IntegerSolver
    {
        private readonly SimplexEngine _simplexEngine;
        private readonly BranchAndBoundEngine _bnbEngine;
        private readonly CuttingPlaneSolver _cuttingPlaneSolver;
        
        public IntegerSolver()
        {
            _simplexEngine = new SimplexEngine();
            _bnbEngine = new BranchAndBoundEngine();
            _cuttingPlaneSolver = new CuttingPlaneSolver();
        }
        
        /// <summary>
        /// Solves an integer programming problem using the specified algorithm
        /// </summary>
        /// <param name="model">Integer programming model</param>
        /// <param name="algorithm">Algorithm to use</param>
        /// <returns>Solution result</returns>
        public SolverResult Solve(LPModel model, string algorithm)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Validate that the model has integer variables
                if (!HasIntegerVariables(model))
                {
                    return new SolverResult
                    {
                        IsSuccessful = false,
                        Status = SolutionStatus.Error,
                        ErrorMessage = "Model does not contain integer or binary variables. Use LP algorithms for continuous problems.",
                        AlgorithmUsed = algorithm,
                        ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    };
                }
                
                // Route to appropriate algorithm
                SolverResult result = algorithm.ToLower() switch
                {
                    "branch & bound (simplex)" => _bnbEngine.Solve(model, BranchAndBoundType.Simplex),
                    "branch & bound (knapsack)" => _bnbEngine.Solve(model, BranchAndBoundType.Knapsack),
                    "cutting plane" => _cuttingPlaneSolver.Solve(model),
                    _ => new SolverResult
                    {
                        IsSuccessful = false,
                        Status = SolutionStatus.Error,
                        ErrorMessage = $"Unknown integer programming algorithm: {algorithm}",
                        AlgorithmUsed = algorithm
                    }
                };
                
                stopwatch.Stop();
                if (result.ExecutionTimeMs == 0) // If not set by the specific algorithm
                {
                    result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new SolverResult
                {
                    IsSuccessful = false,
                    Status = SolutionStatus.Error,
                    ErrorMessage = $"Error solving integer programming problem: {ex.Message}",
                    AlgorithmUsed = algorithm,
                    ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }
        }
        
        /// <summary>
        /// Checks if the model contains integer or binary variables
        /// </summary>
        private bool HasIntegerVariables(LPModel model)
        {
            return model.Variables.Values.Any(v => 
                v.SignRestriction == SignRestriction.Integer || 
                v.SignRestriction == SignRestriction.Binary);
        }
        
        /// <summary>
        /// Recommends the best algorithm for a given model
        /// </summary>
        public string RecommendAlgorithm(LPModel model)
        {
            var integerVars = model.Variables.Values.Where(v => v.SignRestriction == SignRestriction.Integer).Count();
            var binaryVars = model.Variables.Values.Where(v => v.SignRestriction == SignRestriction.Binary).Count();
            var totalVars = model.Variables.Count;
            var constraints = model.Constraints.Count;
            
            // Check if it's a knapsack problem
            if (IsKnapsackProblem(model))
            {
                return "Branch & Bound (Knapsack)";
            }
            
            // For small problems, Cutting Plane might be efficient
            if (totalVars <= 10 && constraints <= 10)
            {
                return "Cutting Plane";
            }
            
            // Default to Branch & Bound (Simplex) for general problems
            return "Branch & Bound (Simplex)";
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
            
            // Check if there's exactly one main constraint
            var mainConstraints = model.Constraints.Where(c => 
                c.Relation == ConstraintRelation.LessThanEqual && c.RHS > 0).ToList();
            
            if (mainConstraints.Count != 1) return false;
            
            // Check if all coefficients in the constraint are positive
            var constraint = mainConstraints[0];
            bool allPositiveCoeffs = constraint.Coefficients.Values.All(coeff => coeff > 0);
            
            return allPositiveCoeffs;
        }
    }
}