using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Self-contained Cutting Plane algorithm with embedded simplex solver
    /// </summary>
    public class CuttingPlaneSolver : IAlgorithmSolver
    {
        public string AlgorithmName => "Cutting Plane";
        public int MaxIterations { get; set; } = 20;
        public double Tolerance { get; set; } = 1e-6;
        public double FractionalityThreshold { get; set; } = 0.01;

        private int _cutIdCounter = 0;
        private LPModel? _originalModel = null;

        /// <summary>
        /// Solves an integer programming problem using the Cutting Plane method
        /// </summary>
        public SolverResult Solve(LPModel model)
        {
            _originalModel = model;

            var result = new CuttingPlaneResult
            {
                AlgorithmUsed = AlgorithmName,
                OriginalModel = model,
                SolveStartTime = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Get integer variables
                var integerVars = GetIntegerVariables(model);
                if (integerVars.Count == 0)
                {
                    result.IsSuccessful = false;
                    result.Status = SolutionStatus.Error;
                    result.ErrorMessage = "No integer variables found in the model.";
                    return result;
                }

                result.IntegerVariables = integerVars;
                Debug.WriteLine($"Integer variables found: {string.Join(", ", integerVars)}");

                // Create working model (copy of original)
                var workingModel = CreateWorkingModel(model);

                // Main cutting plane loop
                double previousObjectiveValue = double.NegativeInfinity;
                int consecutiveNoImprovementCount = 0;
                const int maxNoImprovementIterations = 5;

                for (int iteration = 0; iteration < MaxIterations; iteration++)
                {
                    Debug.WriteLine($"\n=== CUTTING PLANE ITERATION {iteration} ===");

                    // Solve current LP relaxation using embedded simplex
                    var lpResult = SolveLP(workingModel);

                    if (!lpResult.IsSuccessful || lpResult.Status != SolutionStatus.Optimal)
                    {
                        Debug.WriteLine($"LP solve failed: {lpResult.Status} - {lpResult.ErrorMessage}");
                        result.IsSuccessful = false;
                        result.Status = lpResult.Status;
                        result.ErrorMessage = $"LP relaxation failed in iteration {iteration}: {lpResult.ErrorMessage}";
                        break;
                    }

                    Debug.WriteLine($"LP objective: {lpResult.ObjectiveValue:F6}");

                    // Check for objective improvement
                    if (iteration > 0)
                    {
                        double improvement = Math.Abs(lpResult.ObjectiveValue - previousObjectiveValue);
                        if (improvement < 1e-6)
                        {
                            consecutiveNoImprovementCount++;
                            if (consecutiveNoImprovementCount >= maxNoImprovementIterations)
                            {
                                result.IsSuccessful = false;
                                result.Status = SolutionStatus.Error;
                                result.ErrorMessage = $"Algorithm stalled: no improvement for {maxNoImprovementIterations} iterations.";
                                break;
                            }
                        }
                        else
                        {
                            consecutiveNoImprovementCount = 0;
                        }
                    }
                    previousObjectiveValue = lpResult.ObjectiveValue;

                    // Check if solution is integer
                    if (IsIntegerSolution(lpResult.Solution, integerVars))
                    {
                        Debug.WriteLine("INTEGER SOLUTION FOUND!");
                        result.IsSuccessful = true;
                        result.Status = SolutionStatus.Optimal;
                        result.ObjectiveValue = lpResult.ObjectiveValue;
                        result.Solution = lpResult.Solution;
                        result.BestIntegerSolution = new IntegerSolution
                        {
                            Variables = new Dictionary<string, double>(lpResult.Solution),
                            ObjectiveValue = lpResult.ObjectiveValue,
                            IsFeasible = true,
                            Algorithm = AlgorithmName
                        };
                        break;
                    }

                    // Generate cutting planes using embedded tableau
                    var cuts = GenerateCuts(lpResult, integerVars);
                    Debug.WriteLine($"Generated {cuts.Count} cuts");

                    if (cuts.Count == 0)
                    {
                        result.IsSuccessful = false;
                        result.Status = SolutionStatus.Error;
                        result.ErrorMessage = $"No valid cuts found in iteration {iteration}. Algorithm terminated.";
                        break;
                    }

                    // Add cuts to working model
                    foreach (var cut in cuts)
                    {
                        cut.Iteration = iteration;
                        result.CutsGenerated.Add(cut);
                        AddCutToModel(workingModel, cut);
                        Debug.WriteLine($"Added cut {cut.Id}: {cut.ToFormattedString()}");
                    }

                    result.IterationCount++;

                    var iterationInfo = new SimplexIteration
                    {
                        IterationNumber = iteration,
                        Description = $"Cutting Plane iteration {iteration}: Added {cuts.Count} cuts",
                        ObjectiveValue = lpResult.ObjectiveValue,
                        IsOptimal = false,
                        ExecutionTimeMs = 0
                    };
                    result.Iterations.Add(iterationInfo);
                }

                // Check final status
                if (result.Status != SolutionStatus.Optimal && result.Status != SolutionStatus.Error)
                {
                    result.IsSuccessful = false;
                    result.Status = SolutionStatus.MaxIterationsReached;
                    result.ErrorMessage = $"Maximum iterations ({MaxIterations}) reached without finding integer solution.";
                }

                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.SolveEndTime = DateTime.Now;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new CuttingPlaneResult
                {
                    AlgorithmUsed = AlgorithmName,
                    OriginalModel = model,
                    IsSuccessful = false,
                    Status = SolutionStatus.Error,
                    ErrorMessage = $"Error during Cutting Plane solving: {ex.Message}",
                    ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds,
                    SolveEndTime = DateTime.Now
                };
            }
        }

        #region Embedded Simplex Solver

        /// <summary>
        /// Embedded simplex solver for LP relaxation
        /// </summary>
        private SolverResult SolveLP(LPModel model)
        {
            try
            {
                // Convert to standard form and solve
                var standardForm = ConvertToStandardForm(model);
                var tableau = CreateInitialTableau(standardForm);

                if (tableau == null)
                {
                    return new SolverResult
                    {
                        IsSuccessful = false,
                        Status = SolutionStatus.Error,
                        ErrorMessage = "Failed to create initial tableau"
                    };
                }

                // Solve using simplex method
                var result = SolveTableau(tableau, standardForm);
                return result;
            }
            catch (Exception ex)
            {
                return new SolverResult
                {
                    IsSuccessful = false,
                    Status = SolutionStatus.Error,
                    ErrorMessage = $"Error in embedded LP solver: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Convert model to standard form
        /// </summary>
        private LPModel ConvertToStandardForm(LPModel model)
        {
            var standardModel = new LPModel
            {
                Sense = model.Sense,
                Variables = new Dictionary<string, Variable>(),
                Constraints = new List<Constraint>()
            };

            // Copy original variables
            foreach (var var in model.Variables.Values)
            {
                standardModel.Variables[var.Name] = new Variable
                {
                    Name = var.Name,
                    Index = var.Index,
                    Coefficient = var.Coefficient,
                    SignRestriction = SignRestriction.Positive
                };
            }

            int nextVarIndex = model.Variables.Count;

            // Convert constraints to equality form
            for (int i = 0; i < model.Constraints.Count; i++)
            {
                var constraint = model.Constraints[i];
                var newConstraint = new Constraint
                {
                    Name = constraint.Name ?? $"C{i}",
                    Coefficients = new Dictionary<string, double>(constraint.Coefficients),
                    Relation = ConstraintRelation.Equal,
                    RHS = constraint.RHS
                };

                // Add slack variables for ≤ constraints
                if (constraint.Relation == ConstraintRelation.LessThanEqual)
                {
                    string slackName = $"s{i + 1}";
                    newConstraint.Coefficients[slackName] = 1.0;
                    standardModel.Variables[slackName] = new Variable
                    {
                        Name = slackName,
                        Index = nextVarIndex++,
                        Coefficient = 0.0,
                        SignRestriction = SignRestriction.Positive
                    };
                }

                standardModel.Constraints.Add(newConstraint);
            }

            return standardModel;
        }

        /// <summary>
        /// Create initial tableau
        /// </summary>
        private CuttingPlaneTableau? CreateInitialTableau(LPModel standardModel)
        {
            int m = standardModel.Constraints.Count;
            int n = standardModel.Variables.Count;

            var matrix = new double[m + 1, n + 1];
            var varNames = standardModel.Variables.Values.OrderBy(v => v.Index).Select(v => v.Name).ToList();
            var basicVars = new List<string>();

            // Objective row (maximize problem, so negate for minimization tableau)
            for (int j = 0; j < n; j++)
            {
                var varName = varNames[j];
                if (standardModel.Variables.TryGetValue(varName, out var variable))
                {
                    matrix[0, j] = standardModel.Sense == ObjectiveSense.Maximize ? -variable.Coefficient : variable.Coefficient;
                }
            }
            matrix[0, n] = 0.0;

            // Constraint rows
            for (int i = 0; i < m; i++)
            {
                var constraint = standardModel.Constraints[i];
                for (int j = 0; j < n; j++)
                {
                    var varName = varNames[j];
                    if (constraint.Coefficients.TryGetValue(varName, out double coeff))
                    {
                        matrix[i + 1, j] = coeff;
                    }
                }
                matrix[i + 1, n] = constraint.RHS;

                // Find basic variable (slack variable for this constraint)
                string basicVar = $"s{i + 1}";
                if (varNames.Contains(basicVar))
                {
                    basicVars.Add(basicVar);
                }
                else
                {
                    // Fallback - find first variable with coefficient 1 in this row
                    for (int j = 0; j < n; j++)
                    {
                        if (Math.Abs(matrix[i + 1, j] - 1.0) < 1e-9)
                        {
                            basicVars.Add(varNames[j]);
                            break;
                        }
                    }
                }
            }

            return new CuttingPlaneTableau
            {
                Matrix = matrix,
                Rows = m + 1,
                Columns = n + 1,
                VariableNames = varNames,
                BasicVariables = basicVars
            };
        }

        /// <summary>
        /// Solve tableau using simplex method
        /// </summary>
        private SolverResult SolveTableau(CuttingPlaneTableau tableau, LPModel standardModel)
        {
            const int maxIterations = 1000;

            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                // Find entering variable (most negative in objective row)
                int enteringCol = -1;
                double mostNegative = 0.0;

                for (int j = 0; j < tableau.Columns - 1; j++)
                {
                    if (tableau.Matrix[0, j] < mostNegative - 1e-9)
                    {
                        mostNegative = tableau.Matrix[0, j];
                        enteringCol = j;
                    }
                }

                if (enteringCol == -1)
                {
                    // Optimal solution found
                    return ExtractSolution(tableau, standardModel);
                }

                // Find leaving variable using ratio test
                int leavingRow = -1;
                double minRatio = double.PositiveInfinity;

                for (int i = 1; i < tableau.Rows; i++)
                {
                    double pivot = tableau.Matrix[i, enteringCol];
                    if (pivot > 1e-9)
                    {
                        double ratio = tableau.Matrix[i, tableau.Columns - 1] / pivot;
                        if (ratio < minRatio - 1e-9)
                        {
                            minRatio = ratio;
                            leavingRow = i;
                        }
                    }
                }

                if (leavingRow == -1)
                {
                    return new SolverResult
                    {
                        IsSuccessful = false,
                        Status = SolutionStatus.Unbounded,
                        ErrorMessage = "Problem is unbounded"
                    };
                }

                // Perform pivot operation
                Pivot(tableau, leavingRow, enteringCol);
                tableau.BasicVariables[leavingRow - 1] = tableau.VariableNames[enteringCol];
            }

            return new SolverResult
            {
                IsSuccessful = false,
                Status = SolutionStatus.MaxIterationsReached,
                ErrorMessage = "Maximum iterations reached in simplex solver"
            };
        }

        /// <summary>
        /// Perform pivot operation
        /// </summary>
        private void Pivot(CuttingPlaneTableau tableau, int pivotRow, int pivotCol)
        {
            double pivotElement = tableau.Matrix[pivotRow, pivotCol];

            // Normalize pivot row
            for (int j = 0; j < tableau.Columns; j++)
            {
                tableau.Matrix[pivotRow, j] /= pivotElement;
            }

            // Eliminate pivot column in other rows
            for (int i = 0; i < tableau.Rows; i++)
            {
                if (i != pivotRow)
                {
                    double multiplier = tableau.Matrix[i, pivotCol];
                    for (int j = 0; j < tableau.Columns; j++)
                    {
                        tableau.Matrix[i, j] -= multiplier * tableau.Matrix[pivotRow, j];
                    }
                }
            }
        }

        /// <summary>
        /// Extract solution from final tableau
        /// </summary>
        private SolverResult ExtractSolution(CuttingPlaneTableau tableau, LPModel standardModel)
        {
            var solution = new Dictionary<string, double>();

            // Initialize all variables to 0
            foreach (var varName in tableau.VariableNames)
            {
                if (!varName.StartsWith("s")) // Skip slack variables
                    solution[varName] = 0.0;
            }

            // Set basic variables to their RHS values
            for (int i = 1; i < tableau.Rows; i++)
            {
                var basicVar = tableau.BasicVariables[i - 1];
                if (!basicVar.StartsWith("s")) // Only original variables
                {
                    solution[basicVar] = Math.Max(0.0, tableau.Matrix[i, tableau.Columns - 1]);
                }
            }

            // Calculate objective value using original model coefficients
            double objectiveValue = 0.0;
            foreach (var varName in solution.Keys)
            {
                if (standardModel.Variables.TryGetValue(varName, out var variable))
                {
                    objectiveValue += variable.Coefficient * solution[varName];
                }
            }

            // No need to adjust for maximization/minimization since we're using original coefficients
            Debug.WriteLine($"Calculated objective value: {objectiveValue:F6}");

            return new SolverResult
            {
                IsSuccessful = true,
                Status = SolutionStatus.Optimal,
                ObjectiveValue = objectiveValue,
                Solution = solution,
                FinalTableau = ConvertToSimplexTableau(tableau)
            };
        }

        /// <summary>
        /// Convert internal tableau to external SimplexTableau format
        /// </summary>
        private SimplexTableau ConvertToSimplexTableau(CuttingPlaneTableau internalTableau)
        {
            var tableau = new SimplexTableau(internalTableau.Rows, internalTableau.Columns);
            
            // Copy matrix
            for (int i = 0; i < internalTableau.Rows; i++)
            {
                for (int j = 0; j < internalTableau.Columns; j++)
                {
                    tableau.Matrix[i, j] = internalTableau.Matrix[i, j];
                }
            }
            
            tableau.VariableNames = new List<string>(internalTableau.VariableNames);
            tableau.BasicVariables = new List<string>(internalTableau.BasicVariables);
            tableau.ObjectiveValue = tableau.Matrix[0, tableau.RHSColumn];
            
            return tableau;
        }

        #endregion

        /// <summary>
        /// Generate cuts from the current solution and tableau
        /// </summary>
        private List<CuttingPlane> GenerateCuts(SolverResult lpResult, List<string> integerVars)
        {
            var cuts = new List<CuttingPlane>();

            // Generate fractional cuts
            foreach (var varName in integerVars)
            {
                if (lpResult.Solution.TryGetValue(varName, out double value))
                {
                    double fractionalPart = value - Math.Floor(value);

                    if (fractionalPart > FractionalityThreshold && fractionalPart < (1 - FractionalityThreshold))
                    {
                        // Generate simple bound cut: x ≤ floor(value)
                        var cut = new CuttingPlane
                        {
                            Id = _cutIdCounter++,
                            Type = CutType.Gomory,
                            Coefficients = new Dictionary<string, double> { { varName, 1.0 } },
                            RHS = Math.Floor(value),
                            Relation = ConstraintRelation.LessThanEqual,
                            Source = varName,
                            Violation = fractionalPart
                        };

                        cuts.Add(cut);
                        Debug.WriteLine($"Generated fractional cut: {varName} <= {cut.RHS} (current: {value:F4})");

                        // Limit to 2 cuts per iteration
                        if (cuts.Count >= 2)
                            break;
                    }
                }
            }

            return cuts;
        }

        /// <summary>
        /// Creates a working copy of the model for the cutting plane algorithm
        /// </summary>
        private LPModel CreateWorkingModel(LPModel originalModel)
        {
            var workingModel = new LPModel
            {
                Sense = originalModel.Sense,
                Variables = new Dictionary<string, Variable>(),
                Constraints = new List<Constraint>()
            };

            // Copy variables (convert integer to continuous for LP relaxation)
            foreach (var variable in originalModel.Variables.Values)
            {
                var newVar = new Variable
                {
                    Name = variable.Name,
                    Index = variable.Index,
                    Coefficient = variable.Coefficient,
                    SignRestriction = SignRestriction.Positive // LP relaxation
                };
                workingModel.Variables[newVar.Name] = newVar;
            }

            // Copy constraints
            foreach (var constraint in originalModel.Constraints)
            {
                workingModel.Constraints.Add(new Constraint
                {
                    Name = constraint.Name,
                    Coefficients = new Dictionary<string, double>(constraint.Coefficients),
                    Relation = constraint.Relation,
                    RHS = constraint.RHS
                });
            }

            // Add binary constraints for binary variables
            foreach (var variable in originalModel.Variables.Values)
            {
                if (variable.SignRestriction == SignRestriction.Binary)
                {
                    var binaryConstraint = new Constraint
                    {
                        Name = $"Binary_{variable.Name}",
                        Coefficients = new Dictionary<string, double> { { variable.Name, 1.0 } },
                        Relation = ConstraintRelation.LessThanEqual,
                        RHS = 1.0
                    };
                    workingModel.Constraints.Add(binaryConstraint);
                }
            }

            return workingModel;
        }

        /// <summary>
        /// Adds a cutting plane to the working model as a new constraint
        /// </summary>
        private void AddCutToModel(LPModel model, CuttingPlane cut)
        {
            var constraint = cut.ToConstraint();
            model.Constraints.Add(constraint);
        }

        /// <summary>
        /// Checks if a solution has integer values for all integer variables
        /// </summary>
        private bool IsIntegerSolution(Dictionary<string, double> solution, List<string> integerVars)
        {
            foreach (var varName in integerVars)
            {
                if (solution.TryGetValue(varName, out var value))
                {
                    if (Math.Abs(value - Math.Round(value)) > Tolerance)
                        return false;

                    // Check binary constraints
                    if (_originalModel != null && _originalModel.Variables.TryGetValue(varName, out var variable))
                    {
                        if (variable.SignRestriction == SignRestriction.Binary)
                        {
                            double roundedValue = Math.Round(value);
                            if (roundedValue < 0 || roundedValue > 1)
                                return false;
                        }
                    }
                }
            }
            return true;
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
        /// Checks if this solver supports the given model
        /// </summary>
        public bool SupportsModel(LPModel model)
        {
            return model.Variables.Values.Any(v =>
                v.SignRestriction == SignRestriction.Integer ||
                v.SignRestriction == SignRestriction.Binary);
        }
    }

    /// <summary>
    /// Internal tableau class for cutting plane algorithm
    /// </summary>
    public class CuttingPlaneTableau
    {
        public double[,] Matrix { get; set; } = null!;
        public int Rows { get; set; }
        public int Columns { get; set; }
        public List<string> VariableNames { get; set; } = new();
        public List<string> BasicVariables { get; set; } = new();
    }

    /// <summary>
    /// Result of a Cutting Plane solve operation
    /// </summary>
    public class CuttingPlaneResult : SolverResult
    {
        /// <summary>
        /// All cutting planes generated during the solve
        /// </summary>
        public List<CuttingPlane> CutsGenerated { get; set; } = new();

        /// <summary>
        /// Best integer solution found (if any)
        /// </summary>
        public IntegerSolution? BestIntegerSolution { get; set; }

        /// <summary>
        /// Variables that were treated as integer
        /// </summary>
        public List<string> IntegerVariables { get; set; } = new();

        /// <summary>
        /// Creates a summary of the Cutting Plane results
        /// </summary>
        public new string CreateSummary()
        {
            var summary = new List<string>
            {
                "=== CUTTING PLANE RESULTS ===",
                "",
                $"Algorithm: {AlgorithmUsed}",
                $"Status: {Status}",
                $"Iterations: {IterationCount}",
                $"Execution Time: {ExecutionTimeMs:F2} ms",
                ""
            };

            if (IsSuccessful && BestIntegerSolution != null)
            {
                summary.Add($"Optimal Integer Value: {BestIntegerSolution.ObjectiveValue:F3}");
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
            summary.Add("Cutting Plane Statistics:");
            summary.Add($"  Total Cuts Generated: {CutsGenerated.Count}");
            summary.Add($"  Integer Variables: {IntegerVariables.Count} ({string.Join(", ", IntegerVariables)})");

            if (CutsGenerated.Count > 0)
            {
                summary.Add("");
                summary.Add("Generated Cuts:");
                foreach (var cut in CutsGenerated.Take(5)) // Show first 5 cuts
                {
                    summary.Add($"  Cut {cut.Id}: {cut.ToFormattedString()}");
                }
                if (CutsGenerated.Count > 5)
                {
                    summary.Add($"  ... and {CutsGenerated.Count - 5} more cuts");
                }
            }

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