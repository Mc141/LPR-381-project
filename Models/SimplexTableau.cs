using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Represents a Simplex tableau with complete pivot operations and feasibility checks
    /// </summary>
    public class SimplexTableau
    {
        /// <summary>
        /// The tableau matrix (rows x columns)
        /// Row 0 is the objective function, remaining rows are constraints
        /// </summary>
        public double[,] Matrix { get; set; }
        
        /// <summary>
        /// Names of all variables (decision + slack/surplus/artificial)
        /// </summary>
        public List<string> VariableNames { get; set; } = new();
        
        /// <summary>
        /// Names of basic variables (one per row, excluding objective)
        /// </summary>
        public List<string> BasicVariables { get; set; } = new();
        
        /// <summary>
        /// Names of non-basic variables
        /// </summary>
        public List<string> NonBasicVariables { get; set; } = new();
        
        /// <summary>
        /// Current objective value
        /// </summary>
        public double ObjectiveValue { get; set; }
        
        /// <summary>
        /// Number of rows (including objective row)
        /// </summary>
        public int Rows { get; set; }
        
        /// <summary>
        /// Number of columns (including RHS column)
        /// </summary>
        public int Columns { get; set; }
        
        /// <summary>
        /// Index of the RHS column
        /// </summary>
        public int RHSColumn => Columns - 1;
        
        /// <summary>
        /// Current iteration number
        /// </summary>
        public int IterationNumber { get; set; }
        
        /// <summary>
        /// Whether this tableau represents an optimal solution
        /// </summary>
        public bool IsOptimal { get; set; }
        
        /// <summary>
        /// Whether the problem is infeasible
        /// </summary>
        public bool IsInfeasible { get; set; }
        
        /// <summary>
        /// Whether the problem is unbounded
        /// </summary>
        public bool IsUnbounded { get; set; }
        
        /// <summary>
        /// Last pivot operation performed
        /// </summary>
        public PivotOperation? LastPivot { get; set; }
        
        /// <summary>
        /// Feasibility status of the current solution
        /// </summary>
        public FeasibilityStatus FeasibilityStatus { get; set; }

        /// <summary>
        /// Constructor for creating a new tableau
        /// </summary>
        public SimplexTableau(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            Matrix = new double[rows, columns];
            IterationNumber = 0;
            FeasibilityStatus = FeasibilityStatus.Unknown;
        }

        /// <summary>
        /// Creates a deep copy of the tableau
        /// </summary>
        public SimplexTableau Clone()
        {
            var clone = new SimplexTableau(Rows, Columns)
            {
                VariableNames = new List<string>(VariableNames),
                BasicVariables = new List<string>(BasicVariables),
                NonBasicVariables = new List<string>(NonBasicVariables),
                ObjectiveValue = ObjectiveValue,
                IterationNumber = IterationNumber,
                IsOptimal = IsOptimal,
                IsInfeasible = IsInfeasible,
                IsUnbounded = IsUnbounded,
                FeasibilityStatus = FeasibilityStatus
            };

            // Deep copy the matrix
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    clone.Matrix[i, j] = Matrix[i, j];
                }
            }

            return clone;
        }

        /// <summary>
        /// Performs a pivot operation at the specified row and column
        /// </summary>
        public PivotOperation PerformPivot(int pivotRow, int pivotColumn)
        {
            if (pivotRow < 1 || pivotRow >= Rows || pivotColumn < 0 || pivotColumn >= Columns - 1)
                throw new ArgumentException("Invalid pivot position");

            var pivotElement = Matrix[pivotRow, pivotColumn];
            if (Math.Abs(pivotElement) < 1e-10)
                throw new InvalidOperationException("Cannot pivot on zero element");

            var operation = new PivotOperation
            {
                PivotRow = pivotRow,
                PivotColumn = pivotColumn,
                PivotElement = pivotElement,
                EnteringVariable = VariableNames[pivotColumn],
                LeavingVariable = BasicVariables[pivotRow - 1], // -1 because basic vars don't include obj row
                IterationNumber = IterationNumber
            };

            // Normalize pivot row
            for (int j = 0; j < Columns; j++)
            {
                Matrix[pivotRow, j] /= pivotElement;
            }

            // Eliminate other entries in pivot column
            for (int i = 0; i < Rows; i++)
            {
                if (i != pivotRow)
                {
                    var multiplier = Matrix[i, pivotColumn];
                    for (int j = 0; j < Columns; j++)
                    {
                        Matrix[i, j] -= multiplier * Matrix[pivotRow, j];
                    }
                }
            }

            // Update basic variables
            BasicVariables[pivotRow - 1] = operation.EnteringVariable;
            
            // Update non-basic variables list
            UpdateVariableLists();
            
            // Update objective value
            ObjectiveValue = Matrix[0, RHSColumn];
            
            // Increment iteration
            IterationNumber++;
            
            LastPivot = operation;
            return operation;
        }

        /// <summary>
        /// Finds the entering variable using the most negative coefficient rule
        /// </summary>
        public int FindEnteringVariable()
        {
            int enteringColumn = -1;
            double mostNegative = 0;

            System.Diagnostics.Debug.WriteLine($"Finding entering variable - Iteration {IterationNumber}:");
            
            // Look for most negative coefficient in objective row (excluding RHS)
            for (int j = 0; j < Columns - 1; j++)
            {
                System.Diagnostics.Debug.WriteLine($"  {VariableNames[j]}: {Matrix[0, j]:F3}");
                if (Matrix[0, j] < mostNegative)
                {
                    mostNegative = Matrix[0, j];
                    enteringColumn = j;
                    System.Diagnostics.Debug.WriteLine($"    New most negative: {mostNegative:F3} at column {j} ({VariableNames[j]})");
                }
            }

            if (enteringColumn >= 0)
            {
                System.Diagnostics.Debug.WriteLine($"Selected entering variable: {VariableNames[enteringColumn]} (coefficient: {mostNegative:F3})");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No entering variable found (all coefficients non-negative)");
            }

            return enteringColumn;
        }

        /// <summary>
        /// Performs the ratio test to find the leaving variable
        /// </summary>
        public int FindLeavingVariable(int enteringColumn)
        {
            int leavingRow = -1;
            double minRatio = double.PositiveInfinity;

            // Perform ratio test on constraint rows (skip objective row)
            for (int i = 1; i < Rows; i++)
            {
                var constraint = Matrix[i, enteringColumn];
                if (constraint > 1e-10) // Only consider positive coefficients
                {
                    var ratio = Matrix[i, RHSColumn] / constraint;
                    if (ratio >= 0 && ratio < minRatio) // Non-negative ratios only
                    {
                        minRatio = ratio;
                        leavingRow = i;
                    }
                }
            }

            return leavingRow;
        }

        /// <summary>
        /// Checks if the current solution is optimal
        /// </summary>
        public bool CheckOptimality()
        {
            // For maximization: optimal if all coefficients in objective row are ? 0
            // Debug: Let's see what the objective row looks like
            System.Diagnostics.Debug.WriteLine($"Optimality Check - Tableau Iteration {IterationNumber}:");
            System.Diagnostics.Debug.WriteLine("Objective row coefficients:");
            
            for (int j = 0; j < Columns - 1; j++)
            {
                System.Diagnostics.Debug.WriteLine($"  {VariableNames[j]}: {Matrix[0, j]:F3}");
                if (Matrix[0, j] < -1e-10)
                {
                    System.Diagnostics.Debug.WriteLine($"  Found negative coefficient: {Matrix[0, j]:F3} - NOT OPTIMAL");
                    IsOptimal = false;
                    return false;
                }
            }
            
            System.Diagnostics.Debug.WriteLine("All coefficients non-negative - OPTIMAL");
            IsOptimal = true;
            return true;
        }

        /// <summary>
        /// Checks feasibility of the current solution
        /// </summary>
        public FeasibilityStatus CheckFeasibility()
        {
            // Check if all RHS values are non-negative
            bool allNonNegative = true;
            for (int i = 1; i < Rows; i++)
            {
                if (Matrix[i, RHSColumn] < -1e-10)
                {
                    allNonNegative = false;
                    break;
                }
            }

            if (allNonNegative)
            {
                FeasibilityStatus = FeasibilityStatus.Feasible;
                return FeasibilityStatus.Feasible;
            }

            // Check for infeasibility
            for (int i = 1; i < Rows; i++)
            {
                if (Matrix[i, RHSColumn] < -1e-10)
                {
                    // Check if all coefficients in this row are non-positive
                    bool allNonPositive = true;
                    for (int j = 0; j < Columns - 1; j++)
                    {
                        if (Matrix[i, j] > 1e-10)
                        {
                            allNonPositive = false;
                            break;
                        }
                    }
                    
                    if (allNonPositive)
                    {
                        FeasibilityStatus = FeasibilityStatus.Infeasible;
                        IsInfeasible = true;
                        return FeasibilityStatus.Infeasible;
                    }
                }
            }

            FeasibilityStatus = FeasibilityStatus.PrimalInfeasible;
            return FeasibilityStatus.PrimalInfeasible;
        }

        /// <summary>
        /// Checks if the problem is unbounded
        /// </summary>
        public bool CheckUnboundedness(int enteringColumn)
        {
            // If entering variable has no positive coefficients in constraint rows,
            // the problem is unbounded
            for (int i = 1; i < Rows; i++)
            {
                if (Matrix[i, enteringColumn] > 1e-10)
                    return false;
            }

            IsUnbounded = true;
            return true;
        }

        /// <summary>
        /// Gets the current solution values
        /// </summary>
        public Dictionary<string, double> GetSolution()
        {
            var solution = new Dictionary<string, double>();

            // Initialize all variables to 0
            foreach (var variable in VariableNames)
            {
                if (variable != "RHS")
                    solution[variable] = 0.0;
            }

            // Set basic variable values
            for (int i = 0; i < BasicVariables.Count; i++)
            {
                var variable = BasicVariables[i];
                var value = Matrix[i + 1, RHSColumn]; // +1 to skip objective row
                solution[variable] = Math.Max(0, value); // Ensure non-negative
            }

            return solution;
        }

        /// <summary>
        /// Calculates the objective value from the current solution
        /// </summary>
        public double CalculateObjectiveValue(LPModel originalModel)
        {
            if (originalModel == null) return ObjectiveValue;

            var solution = GetSolution();
            double objValue = 0;

            // Calculate objective value using original model coefficients
            foreach (var variable in originalModel.Variables.Values)
            {
                if (solution.TryGetValue(variable.Name, out double varValue))
                {
                    objValue += variable.Coefficient * varValue;
                }
            }

            return objValue;
        }

        /// <summary>
        /// Updates the lists of basic and non-basic variables
        /// </summary>
        private void UpdateVariableLists()
        {
            NonBasicVariables.Clear();
            
            foreach (var variable in VariableNames)
            {
                if (variable != "RHS" && !BasicVariables.Contains(variable))
                {
                    NonBasicVariables.Add(variable);
                }
            }
        }

        /// <summary>
        /// Gets a formatted string representation of the tableau
        /// </summary>
        public string ToFormattedString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== SIMPLEX TABLEAU - ITERATION {IterationNumber} ===");
            sb.AppendLine();

            // Header row with variable names
            sb.Append("Basis".PadRight(12));
            foreach (var variable in VariableNames)
            {
                sb.Append(variable.PadLeft(10));
            }
            sb.AppendLine();

            // Separator line
            sb.AppendLine(new string('-', 12 + VariableNames.Count * 10));

            // Objective row
            sb.Append("Z".PadRight(12));
            for (int j = 0; j < Columns; j++)
            {
                sb.Append(Matrix[0, j].ToString("F3").PadLeft(10));
            }
            sb.AppendLine();

            // Constraint rows
            for (int i = 1; i < Rows; i++)
            {
                var basicVar = i - 1 < BasicVariables.Count ? BasicVariables[i - 1] : $"x{i}";
                sb.Append(basicVar.PadRight(12));
                
                for (int j = 0; j < Columns; j++)
                {
                    sb.Append(Matrix[i, j].ToString("F3").PadLeft(10));
                }
                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine($"Objective Value: {ObjectiveValue:F3}");
            sb.AppendLine($"Status: {(IsOptimal ? "Optimal" : IsInfeasible ? "Infeasible" : IsUnbounded ? "Unbounded" : "Continuing")}");
            
            if (LastPivot != null)
            {
                // For clarity, let's show both the 0-based column index and the variable name
                // The variable name is more important than the exact column number
                sb.AppendLine($"Last Pivot: Row {LastPivot.PivotRow}, Column {LastPivot.PivotColumn} ({LastPivot.EnteringVariable} enters, {LastPivot.LeavingVariable} leaves)");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Validates the tableau structure and data
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Check dimensions
            if (Rows <= 0 || Columns <= 0)
            {
                result.AddError("Invalid tableau dimensions");
                return result;
            }

            // Check matrix initialization
            if (Matrix == null)
            {
                result.AddError("Tableau matrix not initialized");
                return result;
            }

            // Check variable names
            if (VariableNames.Count != Columns)
            {
                result.AddError($"Variable names count ({VariableNames.Count}) doesn't match columns ({Columns})");
            }

            // Check basic variables count
            if (BasicVariables.Count != Rows - 1)
            {
                result.AddError($"Basic variables count ({BasicVariables.Count}) doesn't match constraint rows ({Rows - 1})");
            }

            // Check for duplicate variable names
            if (VariableNames.Count != VariableNames.Distinct().Count())
            {
                result.AddError("Duplicate variable names found");
            }

            // Numerical checks
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    if (double.IsNaN(Matrix[i, j]) || double.IsInfinity(Matrix[i, j]))
                    {
                        result.AddError($"Invalid matrix value at [{i},{j}]: {Matrix[i, j]}");
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a pivot operation in the simplex algorithm
    /// </summary>
    public class PivotOperation
    {
        public int PivotRow { get; set; }
        public int PivotColumn { get; set; }
        public double PivotElement { get; set; }
        public string EnteringVariable { get; set; } = string.Empty;
        public string LeavingVariable { get; set; } = string.Empty;
        public int IterationNumber { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"Pivot[{IterationNumber}]: ({PivotRow},{PivotColumn}) = {PivotElement:F3}, {EnteringVariable} enters, {LeavingVariable} leaves";
        }
    }

    /// <summary>
    /// Feasibility status enumeration
    /// </summary>
    public enum FeasibilityStatus
    {
        Unknown,
        Feasible,
        Infeasible,
        PrimalInfeasible,
        DualInfeasible,
        Unbounded
    }

    /// <summary>
    /// Validation result for tableau validation
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public bool IsValid => Errors.Count == 0;

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);

        public override string ToString()
        {
            var sb = new StringBuilder();
            if (Errors.Count > 0)
            {
                sb.AppendLine("Errors:");
                foreach (var error in Errors)
                    sb.AppendLine($"  - {error}");
            }
            if (Warnings.Count > 0)
            {
                sb.AppendLine("Warnings:");
                foreach (var warning in Warnings)
                    sb.AppendLine($"  - {warning}");
            }
            return sb.ToString();
        }
    }
}