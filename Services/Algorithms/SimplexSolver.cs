using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Implementation of the Primal Simplex Method
    /// </summary>
    public class SimplexSolver : IAlgorithmSolver
    {
        public string AlgorithmName => "Primal Simplex";
        public int MaxIterations { get; set; } = 1000;
        public double Tolerance { get; set; } = 1e-10;

        private readonly CanonicalFormGenerator _canonicalFormGenerator;

        public SimplexSolver()
        {
            _canonicalFormGenerator = new CanonicalFormGenerator();
        }

        /// <summary>
        /// Solves the LP model using the Primal Simplex method
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

                // Step 2: Check if artificial variables are needed (Phase 1)
                bool needsPhase1 = canonicalForm.ArtificialVariables.Count > 0;
                SimplexTableau currentTableau = canonicalForm.Tableau;

                if (needsPhase1)
                {
                    var phase1Result = SolvePhase1(currentTableau, canonicalForm, result);
                    if (!phase1Result.IsSuccessful)
                    {
                        result.IsSuccessful = false;
                        result.Status = phase1Result.Status;
                        result.ErrorMessage = phase1Result.ErrorMessage;
                        return result;
                    }
                    currentTableau = phase1Result.FinalTableau!;
                }

                // Step 3: Solve Phase 2 (or main problem if no Phase 1)
                var phase2Result = SolvePhase2(currentTableau, result);
                result.IsSuccessful = phase2Result.IsSuccessful;
                result.Status = phase2Result.Status;
                result.ObjectiveValue = phase2Result.ObjectiveValue;
                result.Solution = phase2Result.Solution;
                result.FinalTableau = phase2Result.FinalTableau;

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
        /// Solves Phase 1 of the simplex method (find initial feasible solution)
        /// </summary>
        private SolverResult SolvePhase1(SimplexTableau tableau, CanonicalForm canonicalForm, SolverResult mainResult)
        {
            var result = new SolverResult
            {
                AlgorithmUsed = AlgorithmName + " (Phase 1)",
                Status = SolutionStatus.Unknown
            };

            // Modify objective function for Phase 1: minimize sum of artificial variables
            ModifyObjectiveForPhase1(tableau, canonicalForm.ArtificialVariables);

            // Solve using standard simplex iterations
            var phaseResult = PerformSimplexIterations(tableau, 1, mainResult);
            
            // Check Phase 1 result
            if (phaseResult.Status == SolutionStatus.Optimal)
            {
                // Check if artificial variables are zero
                var artificialSum = canonicalForm.ArtificialVariables
                    .Sum(var => Math.Abs(tableau.GetSolution().GetValueOrDefault(var, 0)));

                if (artificialSum > Tolerance)
                {
                    result.IsSuccessful = false;
                    result.Status = SolutionStatus.Infeasible;
                    result.ErrorMessage = "Phase 1 completed with non-zero artificial variables - problem is infeasible";
                }
                else
                {
                    // Remove artificial variables and restore original objective
                    RemoveArtificialVariables(tableau, canonicalForm);
                    RestoreOriginalObjective(tableau, canonicalForm);
                    
                    result.IsSuccessful = true;
                    result.Status = SolutionStatus.Optimal;
                    result.FinalTableau = tableau;
                }
            }
            else
            {
                result.IsSuccessful = false;
                result.Status = phaseResult.Status;
                result.ErrorMessage = phaseResult.ErrorMessage;
            }

            return result;
        }

        /// <summary>
        /// Solves Phase 2 of the simplex method (optimize original objective)
        /// </summary>
        private SolverResult SolvePhase2(SimplexTableau tableau, SolverResult mainResult)
        {
            return PerformSimplexIterations(tableau, 2, mainResult);
        }

        /// <summary>
        /// Performs the main simplex iteration loop
        /// </summary>
        private SolverResult PerformSimplexIterations(SimplexTableau tableau, int phase, SolverResult mainResult)
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
                    IterationNumber = tableau.IterationNumber,
                    Phase = phase,
                    TableauBefore = tableau.Clone(),
                    Timestamp = iterationStart
                };

                // Calculate objective value correctly
                if (mainResult.OriginalModel != null)
                {
                    currentIteration.ObjectiveValue = tableau.CalculateObjectiveValue(mainResult.OriginalModel);
                }
                else
                {
                    currentIteration.ObjectiveValue = tableau.ObjectiveValue;
                }

                // Step 1: Check optimality
                if (tableau.CheckOptimality())
                {
                    currentIteration.IsOptimal = true;
                    currentIteration.Description = "Optimal solution found";
                    currentIteration.TableauAfter = tableau.Clone();
                    
                    var iterationTime = (DateTime.Now - iterationStart).TotalMilliseconds;
                    currentIteration.ExecutionTimeMs = iterationTime;
                    
                    mainResult.Iterations.Add(currentIteration);
                    
                    result.IsSuccessful = true;
                    result.Status = SolutionStatus.Optimal;
                    
                    // Use corrected objective value calculation
                    if (mainResult.OriginalModel != null)
                    {
                        result.ObjectiveValue = tableau.CalculateObjectiveValue(mainResult.OriginalModel);
                    }
                    else
                    {
                        result.ObjectiveValue = tableau.ObjectiveValue;
                    }
                    
                    result.Solution = tableau.GetSolution();
                    result.FinalTableau = tableau;
                    return result;
                }

                // Step 2: Find entering variable
                int enteringColumn = tableau.FindEnteringVariable();
                if (enteringColumn == -1)
                {
                    // This shouldn't happen if optimality check works correctly
                    currentIteration.Description = "No entering variable found (unexpected)";
                    currentIteration.Warnings.Add("Unexpected termination - no entering variable");
                    result.Status = SolutionStatus.Error;
                    result.ErrorMessage = "Algorithm error: no entering variable found";
                    return result;
                }

                currentIteration.EnteringVariable = tableau.VariableNames[enteringColumn];

                // Step 3: Check for unboundedness
                if (tableau.CheckUnboundedness(enteringColumn))
                {
                    currentIteration.IsUnbounded = true;
                    currentIteration.Description = "Problem is unbounded";
                    currentIteration.TableauAfter = tableau.Clone();
                    
                    mainResult.Iterations.Add(currentIteration);
                    
                    result.IsSuccessful = true;
                    result.Status = SolutionStatus.Unbounded;
                    result.ErrorMessage = "Problem has unbounded optimal solution";
                    return result;
                }

                // Step 4: Perform ratio test to find leaving variable
                int leavingRow = tableau.FindLeavingVariable(enteringColumn);
                if (leavingRow == -1)
                {
                    currentIteration.Description = "Ratio test failed - problem may be unbounded";
                    result.Status = SolutionStatus.Unbounded;
                    result.ErrorMessage = "Ratio test failed";
                    return result;
                }

                currentIteration.LeavingVariable = tableau.BasicVariables[leavingRow - 1];

                // Step 5: Record ratio test results
                RecordRatioTestResults(tableau, enteringColumn, currentIteration);

                // Step 6: Perform pivot operation
                try
                {
                    var pivotOperation = tableau.PerformPivot(leavingRow, enteringColumn);
                    currentIteration.PivotOperation = pivotOperation;
                    
                    // Update objective value after pivot
                    if (mainResult.OriginalModel != null)
                    {
                        currentIteration.ObjectiveValue = tableau.CalculateObjectiveValue(mainResult.OriginalModel);
                    }
                    else
                    {
                        currentIteration.ObjectiveValue = tableau.ObjectiveValue;
                    }
                    
                    currentIteration.Description = $"Pivot: {pivotOperation.EnteringVariable} enters, {pivotOperation.LeavingVariable} leaves";
                }
                catch (Exception ex)
                {
                    currentIteration.Description = $"Pivot operation failed: {ex.Message}";
                    result.Status = SolutionStatus.Error;
                    result.ErrorMessage = $"Pivot operation failed: {ex.Message}";
                    return result;
                }

                // Step 7: Check feasibility after pivot
                var feasibilityStatus = tableau.CheckFeasibility();
                if (feasibilityStatus == FeasibilityStatus.Infeasible)
                {
                    currentIteration.IsInfeasible = true;
                    currentIteration.Description = "Infeasibility detected";
                    result.Status = SolutionStatus.Infeasible;
                    result.ErrorMessage = "Problem is infeasible";
                    return result;
                }

                currentIteration.TableauAfter = tableau.Clone();
                var finalIterationTime = (DateTime.Now - iterationStart).TotalMilliseconds;
                currentIteration.ExecutionTimeMs = finalIterationTime;
                
                mainResult.Iterations.Add(currentIteration);
            }

            // Maximum iterations reached
            result.Status = SolutionStatus.MaxIterationsReached;
            result.ErrorMessage = $"Maximum iterations ({MaxIterations}) reached without convergence";
            return result;
        }

        /// <summary>
        /// Records ratio test results for an iteration
        /// </summary>
        private void RecordRatioTestResults(SimplexTableau tableau, int enteringColumn, SimplexIteration iteration)
        {
            for (int i = 1; i < tableau.Rows; i++)
            {
                var ratioTest = new Models.RatioTestResult
                {
                    RowIndex = i,
                    BasicVariable = tableau.BasicVariables[i - 1],
                    RHSValue = tableau.Matrix[i, tableau.RHSColumn],
                    PivotColumnValue = tableau.Matrix[i, enteringColumn]
                };

                if (ratioTest.PivotColumnValue > Tolerance)
                {
                    ratioTest.Ratio = ratioTest.RHSValue / ratioTest.PivotColumnValue;
                    ratioTest.IsValidRatio = ratioTest.Ratio >= 0;
                }
                else if (ratioTest.PivotColumnValue < -Tolerance)
                {
                    ratioTest.IsValidRatio = false;
                    ratioTest.Notes = "Negative coefficient - not eligible";
                }
                else
                {
                    ratioTest.IsValidRatio = false;
                    ratioTest.Notes = "Zero coefficient - not eligible";
                }

                iteration.RatioTests.Add(ratioTest);
            }
        }

        /// <summary>
        /// Modifies the objective function for Phase 1
        /// </summary>
        private void ModifyObjectiveForPhase1(SimplexTableau tableau, List<string> artificialVariables)
        {
            // Zero out original objective coefficients
            for (int j = 0; j < tableau.Columns - 1; j++)
            {
                tableau.Matrix[0, j] = 0;
            }

            // Set artificial variable coefficients to 1 (minimization)
            foreach (var artificialVar in artificialVariables)
            {
                int colIndex = tableau.VariableNames.IndexOf(artificialVar);
                if (colIndex >= 0 && colIndex < tableau.Columns - 1)
                {
                    tableau.Matrix[0, colIndex] = 1; // Coefficient for minimization
                }
            }

            tableau.ObjectiveValue = tableau.Matrix[0, tableau.RHSColumn];
        }

        /// <summary>
        /// Removes artificial variables from the tableau
        /// </summary>
        private void RemoveArtificialVariables(SimplexTableau tableau, CanonicalForm canonicalForm)
        {
            // This is a simplified implementation
            // In practice, we would need to remove columns corresponding to artificial variables
            // For now, we'll just mark them with zero coefficients
            foreach (var artificialVar in canonicalForm.ArtificialVariables)
            {
                int colIndex = tableau.VariableNames.IndexOf(artificialVar);
                if (colIndex >= 0 && colIndex < tableau.Columns - 1)
                {
                    // Zero out the column
                    for (int i = 0; i < tableau.Rows; i++)
                    {
                        tableau.Matrix[i, colIndex] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Restores the original objective function after Phase 1
        /// </summary>
        private void RestoreOriginalObjective(SimplexTableau tableau, CanonicalForm canonicalForm)
        {
            // Restore original objective coefficients from the canonical form generation
            // This is simplified - in practice we'd store the original objective somewhere
            tableau.ObjectiveValue = tableau.Matrix[0, tableau.RHSColumn];
        }

        /// <summary>
        /// Extracts shadow prices, reduced costs, and other solution information
        /// </summary>
        private void ExtractSolutionInformation(SolverResult result)
        {
            if (result.FinalTableau == null) return;

            var tableau = result.FinalTableau;

            // Extract shadow prices from objective row
            // Shadow prices correspond to slack variable coefficients in the objective row
            for (int j = 0; j < tableau.Columns - 1; j++)
            {
                var variableName = tableau.VariableNames[j];
                if (variableName.StartsWith("s") && !tableau.BasicVariables.Contains(variableName))
                {
                    // This is a non-basic slack variable
                    var shadowPrice = -tableau.Matrix[0, j]; // Negative because of maximization
                    result.ShadowPrices[variableName] = shadowPrice;
                }
            }

            // Extract reduced costs
            for (int j = 0; j < tableau.Columns - 1; j++)
            {
                var variableName = tableau.VariableNames[j];
                if (!tableau.BasicVariables.Contains(variableName))
                {
                    // Non-basic variable - reduced cost is the objective coefficient
                    result.ReducedCosts[variableName] = tableau.Matrix[0, j];
                }
                else
                {
                    // Basic variable - reduced cost is zero
                    result.ReducedCosts[variableName] = 0.0;
                }
            }
        }

        /// <summary>
        /// Checks if this solver supports the given model
        /// </summary>
        public bool SupportsModel(LPModel model)
        {
            // Primal Simplex supports standard LP problems
            // No integer variables
            return !model.Variables.Values.Any(v => 
                v.SignRestriction == SignRestriction.Integer || 
                v.SignRestriction == SignRestriction.Binary);
        }
    }
}