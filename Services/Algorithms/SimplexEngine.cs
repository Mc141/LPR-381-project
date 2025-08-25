using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Central engine for managing and coordinating simplex algorithms
    /// </summary>
    public class SimplexEngine
    {
        private readonly Dictionary<string, IAlgorithmSolver> _solvers;

        /// <summary>
        /// Gets all available algorithm names
        /// </summary>
        public IEnumerable<string> AvailableAlgorithms => _solvers.Keys;

        public SimplexEngine()
        {
            _solvers = new Dictionary<string, IAlgorithmSolver>
            {
                { "Primal Simplex", new SimplexSolver() },
                { "Revised Primal Simplex", new RevisedSimplexSolver() }
            };
        }

        /// <summary>
        /// Solves an LP model using the specified algorithm
        /// </summary>
        /// <param name="model">The LP model to solve</param>
        /// <param name="algorithmName">Name of the algorithm to use</param>
        /// <returns>Solution result</returns>
        public SolverResult Solve(LPModel model, string algorithmName)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (string.IsNullOrWhiteSpace(algorithmName))
                throw new ArgumentException("Algorithm name cannot be null or empty", nameof(algorithmName));

            // Validate model first
            var validation = ValidateModel(model);
            if (!validation.IsValid)
            {
                return new SolverResult
                {
                    IsSuccessful = false,
                    Status = SolutionStatus.Error,
                    ErrorMessage = $"Model validation failed: {string.Join(", ", validation.Errors)}",
                    AlgorithmUsed = algorithmName,
                    OriginalModel = model
                };
            }

            // Get the appropriate solver
            if (!_solvers.TryGetValue(algorithmName, out var solver))
            {
                return new SolverResult
                {
                    IsSuccessful = false,
                    Status = SolutionStatus.Error,
                    ErrorMessage = $"Unknown algorithm: {algorithmName}. Available algorithms: {string.Join(", ", AvailableAlgorithms)}",
                    AlgorithmUsed = algorithmName,
                    OriginalModel = model
                };
            }

            // Check if solver supports this model type
            if (!solver.SupportsModel(model))
            {
                return new SolverResult
                {
                    IsSuccessful = false,
                    Status = SolutionStatus.Error,
                    ErrorMessage = $"Algorithm '{algorithmName}' does not support this model type",
                    AlgorithmUsed = algorithmName,
                    OriginalModel = model
                };
            }

            try
            {
                // Solve the model
                var result = solver.Solve(model);
                
                // Add engine-level information
                result.AdditionalInfo["Engine"] = "SimplexEngine";
                result.AdditionalInfo["ValidationPassed"] = true;
                result.AdditionalInfo["ModelType"] = DetermineModelType(model);

                return result;
            }
            catch (Exception ex)
            {
                return new SolverResult
                {
                    IsSuccessful = false,
                    Status = SolutionStatus.Error,
                    ErrorMessage = $"Unexpected error during solving: {ex.Message}",
                    AlgorithmUsed = algorithmName,
                    OriginalModel = model
                };
            }
        }

        /// <summary>
        /// Validates an LP model for solving
        /// </summary>
        public ValidationResult ValidateModel(LPModel model)
        {
            var result = new ValidationResult();

            if (model == null)
            {
                result.AddError("Model is null");
                return result;
            }

            // Check variables
            if (model.Variables.Count == 0)
            {
                result.AddError("Model has no variables");
            }

            // Check constraints
            if (model.Constraints.Count == 0)
            {
                result.AddError("Model has no constraints");
            }

            // Check for empty variable names
            foreach (var variable in model.Variables.Values)
            {
                if (string.IsNullOrWhiteSpace(variable.Name))
                {
                    result.AddError("Variable with empty name found");
                }
            }

            // Check for duplicate variable names
            var duplicateNames = model.Variables.Values
                .GroupBy(v => v.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var duplicateName in duplicateNames)
            {
                result.AddError($"Duplicate variable name: {duplicateName}");
            }

            // Check constraint structure
            foreach (var constraint in model.Constraints)
            {
                if (string.IsNullOrWhiteSpace(constraint.Name))
                {
                    result.AddWarning("Constraint with empty name found");
                }

                if (constraint.Coefficients.Count == 0)
                {
                    result.AddError($"Constraint '{constraint.Name}' has no coefficients");
                }

                // Check for references to non-existent variables
                foreach (var varName in constraint.Coefficients.Keys)
                {
                    if (!model.Variables.ContainsKey(varName))
                    {
                        result.AddError($"Constraint '{constraint.Name}' references unknown variable '{varName}'");
                    }
                }

                // Check for negative RHS with certain constraint types
                if (constraint.RHS < 0)
                {
                    result.AddWarning($"Constraint '{constraint.Name}' has negative RHS ({constraint.RHS}) - may need preprocessing");
                }
            }

            // Check for unrestricted variables
            var unrestrictedVars = model.Variables.Values
                .Where(v => v.SignRestriction == SignRestriction.Unrestricted)
                .Select(v => v.Name);

            foreach (var varName in unrestrictedVars)
            {
                result.AddWarning($"Variable '{varName}' is unrestricted - may need special handling");
            }

            // Check for integer/binary variables
            var integerVars = model.Variables.Values
                .Where(v => v.SignRestriction == SignRestriction.Integer || v.SignRestriction == SignRestriction.Binary)
                .Select(v => v.Name);

            foreach (var varName in integerVars)
            {
                result.AddWarning($"Variable '{varName}' is integer/binary - simplex methods provide LP relaxation only");
            }

            return result;
        }

        /// <summary>
        /// Gets information about a specific algorithm
        /// </summary>
        public AlgorithmInfo GetAlgorithmInfo(string algorithmName)
        {
            if (!_solvers.TryGetValue(algorithmName, out var solver))
            {
                return new AlgorithmInfo
                {
                    Name = algorithmName,
                    IsAvailable = false,
                    Description = "Algorithm not found"
                };
            }

            return new AlgorithmInfo
            {
                Name = solver.AlgorithmName,
                IsAvailable = true,
                MaxIterations = solver.MaxIterations,
                Tolerance = solver.Tolerance,
                Description = GetAlgorithmDescription(algorithmName)
            };
        }

        /// <summary>
        /// Sets algorithm parameters
        /// </summary>
        public void SetAlgorithmParameters(string algorithmName, int maxIterations, double tolerance)
        {
            if (_solvers.TryGetValue(algorithmName, out var solver))
            {
                solver.MaxIterations = maxIterations;
                solver.Tolerance = tolerance;
            }
        }

        /// <summary>
        /// Determines the type of LP model
        /// </summary>
        private string DetermineModelType(LPModel model)
        {
            bool hasInteger = model.Variables.Values.Any(v => 
                v.SignRestriction == SignRestriction.Integer || 
                v.SignRestriction == SignRestriction.Binary);

            bool hasBinary = model.Variables.Values.Any(v => 
                v.SignRestriction == SignRestriction.Binary);

            if (hasBinary)
                return "Binary Integer Programming";
            else if (hasInteger)
                return "Mixed Integer Programming";
            else
                return "Linear Programming";
        }

        /// <summary>
        /// Gets description for an algorithm
        /// </summary>
        private string GetAlgorithmDescription(string algorithmName)
        {
            return algorithmName switch
            {
                "Primal Simplex" => "Standard primal simplex method with tableau operations",
                "Revised Primal Simplex" => "Matrix-based primal simplex with improved numerical stability",
                _ => "Unknown algorithm"
            };
        }

        /// <summary>
        /// Generates a comprehensive algorithm comparison report
        /// </summary>
        public string GenerateAlgorithmReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== SIMPLEX ALGORITHM REPORT ===");
            report.AppendLine();

            foreach (var algorithmName in AvailableAlgorithms)
            {
                var info = GetAlgorithmInfo(algorithmName);
                report.AppendLine($"Algorithm: {info.Name}");
                report.AppendLine($"  Description: {info.Description}");
                report.AppendLine($"  Max Iterations: {info.MaxIterations}");
                report.AppendLine($"  Tolerance: {info.Tolerance:E}");
                report.AppendLine($"  Available: {info.IsAvailable}");
                report.AppendLine();
            }

            return report.ToString();
        }

        /// <summary>
        /// Creates a demo model for testing
        /// </summary>
        public LPModel CreateDemoModel()
        {
            var model = new LPModel
            {
                Sense = ObjectiveSense.Maximize
            };

            // Variables: maximize 3x1 + 2x2
            model.Variables["x1"] = new Variable { Name = "x1", Coefficient = 3, Index = 0, SignRestriction = SignRestriction.Positive };
            model.Variables["x2"] = new Variable { Name = "x2", Coefficient = 2, Index = 1, SignRestriction = SignRestriction.Positive };

            // Constraints: x1 + x2 <= 4, 2x1 + x2 <= 6
            var c1 = new Constraint 
            { 
                Name = "c1", 
                Relation = ConstraintRelation.LessThanEqual, 
                RHS = 4,
                Coefficients = new Dictionary<string, double> { { "x1", 1 }, { "x2", 1 } }
            };

            var c2 = new Constraint 
            { 
                Name = "c2", 
                Relation = ConstraintRelation.LessThanEqual, 
                RHS = 6,
                Coefficients = new Dictionary<string, double> { { "x1", 2 }, { "x2", 1 } }
            };

            model.Constraints.Add(c1);
            model.Constraints.Add(c2);

            return model;
        }
    }

    /// <summary>
    /// Information about an available algorithm
    /// </summary>
    public class AlgorithmInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string Description { get; set; } = string.Empty;
        public int MaxIterations { get; set; }
        public double Tolerance { get; set; }
    }
}