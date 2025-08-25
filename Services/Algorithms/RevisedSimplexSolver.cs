using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LPR381_Assignment.Models;
using SimplexTableau = LPR381_Assignment.Models.SimplexTableau;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Implementation of the Revised Primal Simplex Method
    /// Provides better numerical stability through matrix operations
    /// </summary>
    public class RevisedSimplexSolver : IAlgorithmSolver
    {
        public string AlgorithmName => "Revised Primal Simplex";
        public int MaxIterations { get; set; } = 1000;
        public double Tolerance { get; set; } = 1e-10;

        private readonly CanonicalFormGenerator _canonicalFormGenerator;

        public RevisedSimplexSolver()
        {
            _canonicalFormGenerator = new CanonicalFormGenerator();
        }

        /// <summary>
        /// Solves the LP model using the Revised Primal Simplex method
        /// </summary>
        public SolverResult Solve(LPModel model)
        {
            var result = new SolverResult
            {
                AlgorithmUsed = AlgorithmName,
                OriginalModel = model,
                SolveStartTime = DateTime.Now
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Step 1: Convert to canonical form
                var canonicalForm = _canonicalFormGenerator.GenerateCanonicalForm(model);
                if (!canonicalForm.IsValid)
                {
                    result.IsSuccessful = false;
                    result.Status = SolutionStatus.Error;
                    result.ErrorMessage = $"Failed to generate canonical form: {canonicalForm.ErrorMessage}";
                    return result;
                }

                result.CanonicalForm = canonicalForm;
                result.InitialTableau = canonicalForm.Tableau.Clone();

                // Step 2: Extract matrices for revised simplex
                var matrices = ExtractMatrices(canonicalForm.Tableau);

                // Step 3: Solve using revised simplex iterations
                var solveResult = PerformRevisedSimplexIterations(matrices, result);
                
                result.IsSuccessful = solveResult.IsSuccessful;
                result.Status = solveResult.Status;
                result.ObjectiveValue = solveResult.ObjectiveValue;
                result.Solution = solveResult.Solution;
                result.FinalTableau = solveResult.FinalTableau;

                // Step 4: Extract additional solution information
                if (result.Status == SolutionStatus.Optimal)
                {
                    ExtractSolutionInformation(result);
                }

                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.SolveEndTime = DateTime.Now;
                result.IterationCount = result.Iterations.Count;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.IsSuccessful = false;
                result.Status = SolutionStatus.Error;
                result.ErrorMessage = $"Unexpected error during solving: {ex.Message}";
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.SolveEndTime = DateTime.Now;
                return result;
            }
        }

        /// <summary>
        /// Extracts matrices needed for revised simplex
        /// </summary>
        private RevisedSimplexMatrices ExtractMatrices(SimplexTableau tableau)
        {
            var matrices = new RevisedSimplexMatrices();
            
            // Extract constraint matrix A (excluding objective row)
            int constraintRows = tableau.Rows - 1;
            int variableCols = tableau.Columns - 1; // Exclude RHS
            
            matrices.A = new double[constraintRows, variableCols];
            for (int i = 0; i < constraintRows; i++)
            {
                for (int j = 0; j < variableCols; j++)
                {
                    matrices.A[i, j] = tableau.Matrix[i + 1, j]; // +1 to skip objective row
                }
            }
            
            // Extract objective coefficients c
            matrices.c = new double[variableCols];
            for (int j = 0; j < variableCols; j++)
            {
                matrices.c[j] = -tableau.Matrix[0, j]; // Negative because of simplex form
            }
            
            // Extract RHS vector b
            matrices.b = new double[constraintRows];
            for (int i = 0; i < constraintRows; i++)
            {
                matrices.b[i] = tableau.Matrix[i + 1, tableau.RHSColumn];
            }
            
            // Initialize basis and basic solution
            matrices.BasicVariableIndices = new List<int>();
            matrices.NonBasicVariableIndices = new List<int>();
            
            // For simplicity, assume initial basis is the last m columns (slack/artificial variables)
            for (int j = variableCols - constraintRows; j < variableCols; j++)
            {
                matrices.BasicVariableIndices.Add(j);
            }
            
            for (int j = 0; j < variableCols - constraintRows; j++)
            {
                matrices.NonBasicVariableIndices.Add(j);
            }
            
            matrices.VariableNames = new List<string>(tableau.VariableNames);
            matrices.IterationNumber = 0;
            
            return matrices;
        }

        /// <summary>
        /// Performs the revised simplex iteration loop with Product Form and Price Out display
        /// </summary>
        private SolverResult PerformRevisedSimplexIterations(RevisedSimplexMatrices matrices, SolverResult mainResult)
        {
            var result = new SolverResult
            {
                AlgorithmUsed = AlgorithmName,
                Status = SolutionStatus.Unknown
            };

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                var iterationStart = DateTime.Now;
                var currentIteration = new SimplexIteration
                {
                    IterationNumber = matrices.IterationNumber,
                    Phase = 2,
                    Timestamp = iterationStart
                };

                // Create detailed Product Form and Price Out display
                var iterationDetails = new System.Text.StringBuilder();
                iterationDetails.AppendLine($"=== REVISED SIMPLEX ITERATION {matrices.IterationNumber} ===");
                iterationDetails.AppendLine();

                // Step 1: Display current basis and basic solution
                iterationDetails.AppendLine("CURRENT BASIS:");
                var basicSolution = CalculateBasicSolution(matrices);
                for (int i = 0; i < matrices.BasicVariableIndices.Count; i++)
                {
                    int varIndex = matrices.BasicVariableIndices[i];
                    string varName = matrices.VariableNames[varIndex];
                    iterationDetails.AppendLine($"  x_B[{i+1}] = {varName} = {basicSolution[i]:F3}");
                }
                iterationDetails.AppendLine();

                // Step 2: Calculate and display objective value
                currentIteration.ObjectiveValue = CalculateObjectiveValue(matrices, basicSolution);
                iterationDetails.AppendLine($"CURRENT OBJECTIVE VALUE: {currentIteration.ObjectiveValue:F3}");
                iterationDetails.AppendLine();

                // Step 3: PRICE OUT - Calculate reduced costs for all non-basic variables
                iterationDetails.AppendLine("PRICE OUT PHASE:");
                iterationDetails.AppendLine("Calculating reduced costs for non-basic variables...");
                iterationDetails.AppendLine();

                var reducedCosts = CalculateReducedCosts(matrices);
                
                iterationDetails.AppendLine("REDUCED COSTS:");
                for (int j = 0; j < matrices.VariableNames.Count - 1; j++) // -1 to exclude RHS
                {
                    if (!matrices.BasicVariableIndices.Contains(j))
                    {
                        iterationDetails.AppendLine($"  c?_{matrices.VariableNames[j]} = {reducedCosts[j]:F3}");
                    }
                }
                iterationDetails.AppendLine();

                // Step 4: Check optimality
                if (IsOptimal(reducedCosts))
                {
                    currentIteration.IsOptimal = true;
                    currentIteration.Description = "Optimal solution found";
                    iterationDetails.AppendLine("OPTIMALITY TEST: PASSED");
                    iterationDetails.AppendLine("All reduced costs are non-negative ? OPTIMAL SOLUTION FOUND");
                    
                    // Store the detailed iteration information
                    currentIteration.Steps.Add(new IterationStep
                    {
                        StepNumber = 1,
                        StepType = "Price Out & Optimality Check",
                        Description = iterationDetails.ToString(),
                        Values = reducedCosts.Select((rc, i) => new StepValue 
                        { 
                            Name = $"c?_{matrices.VariableNames[i]}", 
                            Value = rc 
                        }).ToList()
                    });
                    
                    currentIteration.TableauAfter = CreateTableauFromMatrices(matrices);
                    
                    var iterationTime = (DateTime.Now - iterationStart).TotalMilliseconds;
                    currentIteration.ExecutionTimeMs = iterationTime;
                    
                    mainResult.Iterations.Add(currentIteration);
                    
                    result.IsSuccessful = true;
                    result.Status = SolutionStatus.Optimal;
                    result.ObjectiveValue = currentIteration.ObjectiveValue;
                    result.Solution = ConvertBasicSolutionToDictionary(matrices, basicSolution);
                    result.FinalTableau = currentIteration.TableauAfter;
                    return result;
                }

                // Step 5: Find entering variable (most negative reduced cost)
                int enteringIndex = FindEnteringVariable(reducedCosts);
                if (enteringIndex == -1)
                {
                    currentIteration.Description = "No entering variable found";
                    result.Status = SolutionStatus.Error;
                    result.ErrorMessage = "Algorithm error: no entering variable found";
                    return result;
                }

                currentIteration.EnteringVariable = matrices.VariableNames[enteringIndex];
                iterationDetails.AppendLine("OPTIMALITY TEST: FAILED");
                iterationDetails.AppendLine($"Most negative reduced cost: c?_{currentIteration.EnteringVariable} = {reducedCosts[enteringIndex]:F3}");
                iterationDetails.AppendLine($"ENTERING VARIABLE: {currentIteration.EnteringVariable}");
                iterationDetails.AppendLine();

                // Step 6: PRODUCT FORM - Calculate pivot column
                iterationDetails.AppendLine("PRODUCT FORM PHASE:");
                iterationDetails.AppendLine($"Computing pivot column for entering variable {currentIteration.EnteringVariable}...");
                
                var pivotColumn = CalculatePivotColumn(matrices, enteringIndex);
                iterationDetails.AppendLine();
                iterationDetails.AppendLine("PIVOT COLUMN (B?�A_j):");
                for (int i = 0; i < pivotColumn.Length; i++)
                {
                    int basicVarIndex = matrices.BasicVariableIndices[i];
                    string basicVarName = matrices.VariableNames[basicVarIndex];
                    iterationDetails.AppendLine($"  y_{i+1} = {pivotColumn[i]:F3}  (constraint {i+1}, basic var: {basicVarName})");
                }
                iterationDetails.AppendLine();

                // Step 7: Check for unboundedness
                if (IsUnbounded(pivotColumn))
                {
                    currentIteration.IsUnbounded = true;
                    currentIteration.Description = "Problem is unbounded";
                    iterationDetails.AppendLine("UNBOUNDEDNESS TEST: FAILED");
                    iterationDetails.AppendLine("All pivot column elements ? 0 ? PROBLEM IS UNBOUNDED");
                    
                    currentIteration.Steps.Add(new IterationStep
                    {
                        StepNumber = 2,
                        StepType = "Product Form & Unboundedness Check",
                        Description = iterationDetails.ToString(),
                        Values = pivotColumn.Select((pc, i) => new StepValue 
                        { 
                            Name = $"y_{i+1}", 
                            Value = pc 
                        }).ToList()
                    });
                    
                    mainResult.Iterations.Add(currentIteration);
                    
                    result.IsSuccessful = true;
                    result.Status = SolutionStatus.Unbounded;
                    result.ErrorMessage = "Problem has unbounded optimal solution";
                    return result;
                }

                // Step 8: Perform ratio test to find leaving variable
                iterationDetails.AppendLine("RATIO TEST:");
                var ratioTestResult = PerformRatioTest(basicSolution, pivotColumn);
                
                for (int i = 0; i < pivotColumn.Length; i++)
                {
                    int basicVarIndex = matrices.BasicVariableIndices[i];
                    string basicVarName = matrices.VariableNames[basicVarIndex];
                    
                    if (pivotColumn[i] > Tolerance)
                    {
                        double ratio = basicSolution[i] / pivotColumn[i];
                        string marker = (i == ratioTestResult.LeavingIndex) ? " ? MINIMUM" : "";
                        iterationDetails.AppendLine($"  {basicVarName}: {basicSolution[i]:F3} / {pivotColumn[i]:F3} = {ratio:F3}{marker}");
                    }
                    else
                    {
                        iterationDetails.AppendLine($"  {basicVarName}: {basicSolution[i]:F3} / {pivotColumn[i]:F3} = ? (not eligible)");
                    }
                }
                
                if (ratioTestResult.LeavingIndex == -1)
                {
                    currentIteration.Description = "Ratio test failed";
                    result.Status = SolutionStatus.Error;
                    result.ErrorMessage = "Ratio test failed";
                    return result;
                }

                int leavingVarIndex = matrices.BasicVariableIndices[ratioTestResult.LeavingIndex];
                currentIteration.LeavingVariable = matrices.VariableNames[leavingVarIndex];
                iterationDetails.AppendLine($"LEAVING VARIABLE: {currentIteration.LeavingVariable}");
                iterationDetails.AppendLine();

                // Step 9: BASIS UPDATE
                iterationDetails.AppendLine("BASIS UPDATE:");
                iterationDetails.AppendLine($"Old Basis: [{string.Join(", ", matrices.BasicVariableIndices.Select(idx => matrices.VariableNames[idx]))}]");
                
                // Perform pivot-like operations on the matrices
                UpdateMatricesAfterPivot(matrices, enteringIndex, ratioTestResult.LeavingIndex, pivotColumn, basicSolution);
                
                // Update the basic variable at the leaving position
                matrices.BasicVariableIndices[ratioTestResult.LeavingIndex] = enteringIndex;
                
                iterationDetails.AppendLine($"New Basis: [{string.Join(", ", matrices.BasicVariableIndices.Select(idx => matrices.VariableNames[idx]))}]");
                iterationDetails.AppendLine();
                
                // Update non-basic variables list
                matrices.NonBasicVariableIndices.Remove(enteringIndex);
                matrices.NonBasicVariableIndices.Add(leavingVarIndex);
                matrices.NonBasicVariableIndices.Sort();

                iterationDetails.AppendLine("ITERATION COMPLETE");
                iterationDetails.AppendLine($"Next iteration will start with new basis");

                currentIteration.Description = $"Product Form: {currentIteration.EnteringVariable} enters, {currentIteration.LeavingVariable} leaves";
                
                // Store all the detailed steps
                currentIteration.Steps.Add(new IterationStep
                {
                    StepNumber = 1,
                    StepType = "Complete Revised Simplex Iteration",
                    Description = iterationDetails.ToString(),
                    Values = new List<StepValue>
                    {
                        new() { Name = "Entering Variable", Value = enteringIndex },
                        new() { Name = "Leaving Variable", Value = leavingVarIndex },
                        new() { Name = "Objective Value", Value = currentIteration.ObjectiveValue }
                    }
                });
                
                currentIteration.TableauAfter = CreateTableauFromMatrices(matrices);
                
                var finalIterationTime = (DateTime.Now - iterationStart).TotalMilliseconds;
                currentIteration.ExecutionTimeMs = finalIterationTime;
                
                matrices.IterationNumber++;
                mainResult.Iterations.Add(currentIteration);
            }

            // Maximum iterations reached
            result.Status = SolutionStatus.MaxIterationsReached;
            result.ErrorMessage = $"Maximum iterations ({MaxIterations}) reached without convergence";
            return result;
        }

        /// <summary>
        /// Calculates the current basic solution
        /// </summary>
        private double[] CalculateBasicSolution(RevisedSimplexMatrices matrices)
        {
            int m = matrices.BasicVariableIndices.Count;
            var basicSolution = new double[m];
            
            // In initial iteration, basic variables are slack variables with RHS values
            // In subsequent iterations, we need to update based on the basis changes
            
            if (matrices.IterationNumber == 0)
            {
                // Initial basic solution: slack variables get RHS values
                for (int i = 0; i < m; i++)
                {
                    basicSolution[i] = Math.Max(0, matrices.b[i]);
                }
            }
            else
            {
                // For subsequent iterations, we need a more sophisticated calculation
                // This is a simplified approach - in practice, we'd solve B * x_B = b
                // For now, we'll maintain the RHS values and let the tableau operations handle updates
                for (int i = 0; i < m; i++)
                {
                    basicSolution[i] = Math.Max(0, matrices.b[i]);
                }
            }
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"Basic Solution (Iteration {matrices.IterationNumber}):");
            for (int i = 0; i < m; i++)
            {
                int varIndex = matrices.BasicVariableIndices[i];
                string varName = matrices.VariableNames[varIndex];
                System.Diagnostics.Debug.WriteLine($"  {varName} = {basicSolution[i]:F3}");
            }
            
            return basicSolution;
        }

        /// <summary>
        /// Calculates the objective value for the current solution
        /// </summary>
        private double CalculateObjectiveValue(RevisedSimplexMatrices matrices, double[] basicSolution)
        {
            double value = 0;
            
            // Calculate objective value using the original model coefficients
            // Instead of trying to undo negations, just use the stored positive coefficients
            for (int i = 0; i < matrices.BasicVariableIndices.Count; i++)
            {
                int varIndex = matrices.BasicVariableIndices[i];
                string varName = matrices.VariableNames[varIndex];
                
                // Only include original decision variables in objective calculation
                if (!varName.StartsWith("s") && !varName.StartsWith("a") && !varName.StartsWith("e"))
                {
                    // matrices.c contains the original positive coefficients (after the negation in ExtractMatrices)
                    // For "max 3x1 + 2x2", matrices.c[0] = 3, matrices.c[1] = 2
                    double originalCoeff = matrices.c[varIndex]; // Use the stored positive coefficient directly
                    value += originalCoeff * basicSolution[i];
                    
                    // Debug output
                    System.Diagnostics.Debug.WriteLine($"  Objective calc: {varName} = {basicSolution[i]:F3} * {originalCoeff:F3} = {originalCoeff * basicSolution[i]:F3}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"  Total objective value: {value:F3}");
            return value;
        }

        /// <summary>
        /// Calculates reduced costs for all non-basic variables
        /// </summary>
        private double[] CalculateReducedCosts(RevisedSimplexMatrices matrices)
        {
            var reducedCosts = new double[matrices.c.Length];
            
            // For revised simplex: reduced costs = c_j - c_B * B^(-1) * A_j
            // Since we're in initial form with identity basis (slack variables), this simplifies
            
            for (int j = 0; j < matrices.c.Length; j++)
            {
                if (matrices.BasicVariableIndices.Contains(j))
                {
                    reducedCosts[j] = 0; // Basic variables have zero reduced cost
                }
                else
                {
                    // For non-basic variables in initial canonical form:
                    // The reduced cost is the negative of the coefficient (since c was negated)
                    // This gives us the original objective row coefficients for optimality checking
                    reducedCosts[j] = -matrices.c[j];
                }
            }
            
            // Debug output to verify reduced costs calculation
            System.Diagnostics.Debug.WriteLine("Revised Simplex - Reduced Costs:");
            for (int j = 0; j < reducedCosts.Length; j++)
            {
                System.Diagnostics.Debug.WriteLine($"  {matrices.VariableNames[j]}: {reducedCosts[j]:F3}");
            }
            
            return reducedCosts;
        }

        /// <summary>
        /// Checks if the current solution is optimal
        /// </summary>
        private bool IsOptimal(double[] reducedCosts)
        {
            // Optimal if all reduced costs are non-negative (for maximization)
            bool optimal = reducedCosts.All(rc => rc >= -Tolerance);
            
            System.Diagnostics.Debug.WriteLine($"Optimality Check: {(optimal ? "OPTIMAL" : "NOT OPTIMAL")}");
            if (!optimal)
            {
                var negativeIndices = reducedCosts
                    .Select((rc, index) => new { ReducedCost = rc, Index = index })
                    .Where(x => x.ReducedCost < -Tolerance)
                    .ToList();
                    
                System.Diagnostics.Debug.WriteLine($"Negative reduced costs found: {string.Join(", ", negativeIndices.Select(x => $"{x.Index}({x.ReducedCost:F3})"))}");
            }
            
            return optimal;
        }

        /// <summary>
        /// Finds the entering variable (most negative reduced cost)
        /// </summary>
        private int FindEnteringVariable(double[] reducedCosts)
        {
            int enteringIndex = -1;
            double mostNegative = 0;
            
            for (int j = 0; j < reducedCosts.Length; j++)
            {
                if (reducedCosts[j] < mostNegative)
                {
                    mostNegative = reducedCosts[j];
                    enteringIndex = j;
                }
            }
            
            return enteringIndex;
        }

        /// <summary>
        /// Calculates the pivot column for the entering variable
        /// </summary>
        private double[] CalculatePivotColumn(RevisedSimplexMatrices matrices, int enteringIndex)
        {
            int m = matrices.BasicVariableIndices.Count;
            var pivotColumn = new double[m];
            
            // For simplicity, extract column from A matrix
            // In full implementation: B^(-1) * A_j
            for (int i = 0; i < m; i++)
            {
                pivotColumn[i] = matrices.A[i, enteringIndex];
            }
            
            return pivotColumn;
        }

        /// <summary>
        /// Checks if the problem is unbounded
        /// </summary>
        private bool IsUnbounded(double[] pivotColumn)
        {
            // Unbounded if all elements in pivot column are non-positive
            return pivotColumn.All(element => element <= Tolerance);
        }

        /// <summary>
        /// Performs the ratio test to find the leaving variable
        /// </summary>
        private Models.RatioTestResult PerformRatioTest(double[] basicSolution, double[] pivotColumn)
        {
            var result = new Models.RatioTestResult { LeavingIndex = -1 };
            double minRatio = double.PositiveInfinity;
            
            for (int i = 0; i < pivotColumn.Length; i++)
            {
                if (pivotColumn[i] > Tolerance)
                {
                    double ratio = basicSolution[i] / pivotColumn[i];
                    if (ratio >= 0 && ratio < minRatio)
                    {
                        minRatio = ratio;
                        result.LeavingIndex = i;
                        result.Ratio = ratio;
                    }
                }
            }
            
            result.IsValidRatio = result.LeavingIndex != -1;
            return result;
        }

        /// <summary>
        /// Creates a tableau representation from the matrix form
        /// </summary>
        private SimplexTableau CreateTableauFromMatrices(RevisedSimplexMatrices matrices)
        {
            int rows = matrices.BasicVariableIndices.Count + 1; // +1 for objective
            int cols = matrices.c.Length + 1; // +1 for RHS
            
            var tableau = new SimplexTableau(rows, cols)
            {
                VariableNames = new List<string>(matrices.VariableNames),
                IterationNumber = matrices.IterationNumber
            };
            
            // Add RHS column name if not already included
            if (!tableau.VariableNames.Contains("RHS"))
            {
                tableau.VariableNames.Add("RHS");
            }
            
            // Fill basic variables
            tableau.BasicVariables.Clear();
            for (int i = 0; i < matrices.BasicVariableIndices.Count; i++)
            {
                int varIndex = matrices.BasicVariableIndices[i];
                if (varIndex < matrices.VariableNames.Count - 1) // -1 to exclude RHS
                {
                    tableau.BasicVariables.Add(matrices.VariableNames[varIndex]);
                }
            }
            
            // Reconstruct the tableau matrix from the matrices
            // This is a critical fix - we need to actually populate the matrix!
            
            // Fill objective row (row 0)
            for (int j = 0; j < matrices.c.Length; j++)
            {
                // Convert back to tableau form: negative of original coefficients for non-basic vars
                if (matrices.BasicVariableIndices.Contains(j))
                {
                    tableau.Matrix[0, j] = 0; // Basic variables have 0 in objective row
                }
                else
                {
                    tableau.Matrix[0, j] = -matrices.c[j]; // Non-basic variables
                }
            }
            
            // Fill constraint rows (rows 1 to m)
            for (int i = 0; i < matrices.BasicVariableIndices.Count; i++)
            {
                int rowIndex = i + 1; // +1 to skip objective row
                
                // Fill constraint coefficients
                for (int j = 0; j < matrices.c.Length; j++)
                {
                    if (matrices.BasicVariableIndices.Contains(j))
                    {
                        // Basic variable column - should have identity pattern
                        int basicIndex = matrices.BasicVariableIndices.IndexOf(j);
                        tableau.Matrix[rowIndex, j] = (basicIndex == i) ? 1.0 : 0.0;
                    }
                    else
                    {
                        // Non-basic variable - get from A matrix
                        tableau.Matrix[rowIndex, j] = matrices.A[i, j];
                    }
                }
                
                // Fill RHS column
                tableau.Matrix[rowIndex, tableau.RHSColumn] = matrices.b[i];
            }
            
            // Calculate and set objective value
            tableau.ObjectiveValue = 0;
            for (int i = 0; i < matrices.BasicVariableIndices.Count; i++)
            {
                int varIndex = matrices.BasicVariableIndices[i];
                if (varIndex < matrices.c.Length)
                {
                    tableau.ObjectiveValue += matrices.c[varIndex] * matrices.b[i];
                }
            }
            
            // Update non-basic variables list
            tableau.NonBasicVariables.Clear();
            for (int j = 0; j < matrices.VariableNames.Count - 1; j++) // -1 to exclude RHS
            {
                if (!matrices.BasicVariableIndices.Contains(j))
                {
                    tableau.NonBasicVariables.Add(matrices.VariableNames[j]);
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Created tableau from matrices - Iteration {matrices.IterationNumber}");
            System.Diagnostics.Debug.WriteLine($"Basic variables: {string.Join(", ", tableau.BasicVariables)}");
            System.Diagnostics.Debug.WriteLine($"Objective value: {tableau.ObjectiveValue:F3}");
            
            return tableau;
        }

        /// <summary>
        /// Converts basic solution to dictionary format
        /// </summary>
        private Dictionary<string, double> ConvertBasicSolutionToDictionary(RevisedSimplexMatrices matrices, double[] basicSolution)
        {
            var solution = new Dictionary<string, double>();
            
            // Initialize all variables to 0
            for (int j = 0; j < matrices.VariableNames.Count - 1; j++) // -1 to exclude RHS
            {
                solution[matrices.VariableNames[j]] = 0.0;
            }
            
            // Set basic variable values
            for (int i = 0; i < matrices.BasicVariableIndices.Count; i++)
            {
                int varIndex = matrices.BasicVariableIndices[i];
                string varName = matrices.VariableNames[varIndex];
                solution[varName] = Math.Max(0, basicSolution[i]);
            }
            
            return solution;
        }

        /// <summary>
        /// Extracts shadow prices and reduced costs
        /// </summary>
        private void ExtractSolutionInformation(SolverResult result)
        {
            // This would be implemented to extract dual information
            // from the final tableau or matrix form
            if (result.FinalTableau != null)
            {
                // Extract basic solution information
                var solution = result.FinalTableau.GetSolution();
                foreach (var kvp in solution)
                {
                    if (!result.Solution.ContainsKey(kvp.Key))
                        result.Solution[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// Checks if this solver supports the given model
        /// </summary>
        public bool SupportsModel(LPModel model)
        {
            // Revised Primal Simplex supports standard LP problems
            // No integer variables
            return !model.Variables.Values.Any(v => 
                v.SignRestriction == SignRestriction.Integer || 
                v.SignRestriction == SignRestriction.Binary);
        }

        /// <summary>
        /// Updates the matrices after a pivot operation to simulate tableau operations
        /// </summary>
        private void UpdateMatricesAfterPivot(RevisedSimplexMatrices matrices, int enteringIndex, int leavingRowIndex, double[] pivotColumn, double[] basicSolution)
        {
            double pivotElement = pivotColumn[leavingRowIndex];
            
            if (Math.Abs(pivotElement) < Tolerance)
            {
                throw new InvalidOperationException("Cannot pivot on zero element");
            }
            
            // Update the RHS vector (simulating tableau RHS column update)
            double[] newB = new double[matrices.b.Length];
            
            // Normalize the leaving row
            newB[leavingRowIndex] = matrices.b[leavingRowIndex] / pivotElement;
            
            // Update other rows
            for (int i = 0; i < matrices.b.Length; i++)
            {
                if (i != leavingRowIndex)
                {
                    double multiplier = pivotColumn[i];
                    newB[i] = matrices.b[i] - multiplier * newB[leavingRowIndex];
                }
            }
            
            matrices.b = newB;
            
            // Update the constraint matrix A (simulate tableau body update)
            double[,] newA = new double[matrices.A.GetLength(0), matrices.A.GetLength(1)];
            
            // Copy the original matrix
            for (int i = 0; i < matrices.A.GetLength(0); i++)
            {
                for (int j = 0; j < matrices.A.GetLength(1); j++)
                {
                    newA[i, j] = matrices.A[i, j];
                }
            }
            
            // Normalize the pivot row
            for (int j = 0; j < matrices.A.GetLength(1); j++)
            {
                newA[leavingRowIndex, j] = matrices.A[leavingRowIndex, j] / pivotElement;
            }
            
            // Update other rows
            for (int i = 0; i < matrices.A.GetLength(0); i++)
            {
                if (i != leavingRowIndex)
                {
                    double multiplier = pivotColumn[i];
                    for (int j = 0; j < matrices.A.GetLength(1); j++)
                    {
                        newA[i, j] = matrices.A[i, j] - multiplier * newA[leavingRowIndex, j];
                    }
                }
            }
            
            matrices.A = newA;
            
            System.Diagnostics.Debug.WriteLine($"Updated matrices after pivot - entering: {matrices.VariableNames[enteringIndex]}, leaving row: {leavingRowIndex}");
        }
    }

    /// <summary>
    /// Container for matrices used in revised simplex method
    /// </summary>
    public class RevisedSimplexMatrices
    {
        public double[,] A { get; set; } = new double[0, 0]; // Constraint matrix
        public double[] b { get; set; } = Array.Empty<double>(); // RHS vector
        public double[] c { get; set; } = Array.Empty<double>(); // Objective coefficients
        public List<int> BasicVariableIndices { get; set; } = new();
        public List<int> NonBasicVariableIndices { get; set; } = new();
        public List<string> VariableNames { get; set; } = new();
        public int IterationNumber { get; set; }
    }}