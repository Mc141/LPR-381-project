using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Analysis
{
    /// <summary>
    /// Service for performing sensitivity analysis operations on LP models
    /// </summary>
    public class SensitivityAnalyzer
    {
        /// <summary>
        /// Adds a new activity (variable) to the LP model
        /// </summary>
        /// <param name="model">The original LP model</param>
        /// <param name="activityName">Name of the new activity</param>
        /// <param name="objectiveCoefficient">Coefficient in the objective function</param>
        /// <param name="constraintCoefficients">Coefficients for each constraint</param>
        /// <param name="signRestriction">Sign restriction for the new variable</param>
        /// <returns>Result of the sensitivity analysis</returns>
        public SensitivityResult AddNewActivity(
            LPModel model, 
            string activityName,
            double objectiveCoefficient,
            double[] constraintCoefficients,
            SignRestriction signRestriction = SignRestriction.Positive)
        {
            var result = new SensitivityResult
            {
                OperationType = "Add New Activity",
                OriginalModel = model
            };
            
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(activityName))
                {
                    result.Status = "Activity name cannot be empty";
                    return result;
                }
                
                if (model.Variables.ContainsKey(activityName))
                {
                    result.Status = $"Variable '{activityName}' already exists in the model";
                    return result;
                }
                
                if (constraintCoefficients.Length != model.Constraints.Count)
                {
                    result.Status = $"Expected {model.Constraints.Count} constraint coefficients, but got {constraintCoefficients.Length}";
                    return result;
                }
                
                // Create a deep copy of the original model
                var modifiedModel = CloneModel(model);
                
                // Add new variable to the model
                var newVariable = new Variable
                {
                    Name = activityName,
                    Coefficient = objectiveCoefficient,
                    SignRestriction = signRestriction,
                    Index = modifiedModel.Variables.Count
                };
                
                modifiedModel.Variables.Add(activityName, newVariable);
                
                // Add coefficients to each constraint
                for (int i = 0; i < constraintCoefficients.Length; i++)
                {
                    var constraint = modifiedModel.Constraints[i];
                    constraint.Coefficients[activityName] = constraintCoefficients[i];
                }
                
                // Prepare the result
                result.ModifiedModel = modifiedModel;
                result.IsSuccessful = true;
                result.Status = $"Successfully added activity '{activityName}' to the model";
                result.Details = $"New variable with objective coefficient {objectiveCoefficient:F3} and {constraintCoefficients.Length} constraint coefficients";
                
                // Document changes
                result.Changes["New Variable"] = $"{activityName} (coefficient: {objectiveCoefficient:F3})";
                result.Changes["Sign Restriction"] = signRestriction.ToString();
                result.Changes["Constraint Coefficients"] = string.Join(", ", constraintCoefficients.Select(c => c.ToString("F3")));
                result.Changes["Total Variables"] = $"{model.Variables.Count} ? {modifiedModel.Variables.Count}";
                
                // Add recommendations
                result.Recommendations.Add("Re-solve the model to see the impact of the new activity");
                result.Recommendations.Add("Check if the new activity improves the objective value");
                
                if (objectiveCoefficient > 0 && model.Sense == ObjectiveSense.Maximize)
                {
                    result.Recommendations.Add("Positive coefficient in maximization - this activity may be beneficial");
                }
                else if (objectiveCoefficient < 0 && model.Sense == ObjectiveSense.Minimize)
                {
                    result.Recommendations.Add("Negative coefficient in minimization - this activity may be beneficial");
                }
                
                // Check for potential issues
                if (constraintCoefficients.All(c => c == 0))
                {
                    result.Warnings.Add("All constraint coefficients are zero - this activity is unconstrained");
                }
                
                if (Math.Abs(objectiveCoefficient) < 0.001)
                {
                    result.Warnings.Add("Objective coefficient is very small - this activity may have minimal impact");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error adding activity: {ex.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Analyzes the impact of adding a new constraint to the model
        /// </summary>
        public SensitivityResult AddNewConstraint(
            LPModel model,
            string constraintName,
            Dictionary<string, double> coefficients,
            ConstraintRelation relation,
            double rhs)
        {
            var result = new SensitivityResult
            {
                OperationType = "Add New Constraint",
                OriginalModel = model
            };
            
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(constraintName))
                {
                    result.Status = "Constraint name cannot be empty";
                    return result;
                }
                
                if (model.Constraints.Any(c => c.Name == constraintName))
                {
                    result.Status = $"Constraint '{constraintName}' already exists in the model";
                    return result;
                }
                
                // Create a deep copy of the original model
                var modifiedModel = CloneModel(model);
                
                // Create new constraint
                var newConstraint = new Constraint
                {
                    Name = constraintName,
                    Coefficients = new Dictionary<string, double>(coefficients),
                    Relation = relation,
                    RHS = rhs
                };
                
                // Ensure all variables have coefficients (default to 0 if not specified)
                foreach (var variable in modifiedModel.Variables.Keys)
                {
                    if (!newConstraint.Coefficients.ContainsKey(variable))
                    {
                        newConstraint.Coefficients[variable] = 0.0;
                    }
                }
                
                modifiedModel.Constraints.Add(newConstraint);
                
                // Prepare the result
                result.ModifiedModel = modifiedModel;
                result.IsSuccessful = true;
                result.Status = $"Successfully added constraint '{constraintName}' to the model";
                result.Details = $"New constraint: {newConstraint}";
                
                // Document changes
                result.Changes["New Constraint"] = constraintName;
                result.Changes["Relation"] = relation.ToString();
                result.Changes["RHS"] = rhs.ToString("F3");
                result.Changes["Total Constraints"] = $"{model.Constraints.Count} ? {modifiedModel.Constraints.Count}";
                
                // Add recommendations
                result.Recommendations.Add("Re-solve the model to check feasibility");
                result.Recommendations.Add("The new constraint may reduce the feasible region");
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error adding constraint: {ex.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Creates a deep copy of an LP model
        /// </summary>
        private LPModel CloneModel(LPModel original)
        {
            var clone = new LPModel
            {
                Sense = original.Sense
            };
            
            // Clone variables
            foreach (var kvp in original.Variables)
            {
                var originalVar = kvp.Value;
                var clonedVar = new Variable
                {
                    Name = originalVar.Name,
                    Coefficient = originalVar.Coefficient,
                    SignRestriction = originalVar.SignRestriction,
                    Index = originalVar.Index
                };
                clone.Variables.Add(kvp.Key, clonedVar);
            }
            
            // Clone constraints
            foreach (var originalConstraint in original.Constraints)
            {
                var clonedConstraint = new Constraint
                {
                    Name = originalConstraint.Name,
                    Coefficients = new Dictionary<string, double>(originalConstraint.Coefficients),
                    Relation = originalConstraint.Relation,
                    RHS = originalConstraint.RHS
                };
                clone.Constraints.Add(clonedConstraint);
            }
            
            return clone;
        }
        
        /// <summary>
        /// Generates a summary comparison between two models
        /// </summary>
        public string CompareModels(LPModel original, LPModel modified)
        {
            var summary = new List<string>();
            
            summary.Add("=== MODEL COMPARISON ===");
            summary.Add($"Original: {original.Variables.Count} variables, {original.Constraints.Count} constraints");
            summary.Add($"Modified: {modified.Variables.Count} variables, {modified.Constraints.Count} constraints");
            
            if (modified.Variables.Count > original.Variables.Count)
            {
                var newVars = modified.Variables.Keys.Except(original.Variables.Keys);
                summary.Add($"New variables: {string.Join(", ", newVars)}");
            }
            
            if (modified.Constraints.Count > original.Constraints.Count)
            {
                var newConstraints = modified.Constraints.Skip(original.Constraints.Count);
                summary.Add($"New constraints: {string.Join(", ", newConstraints.Select(c => c.Name))}");
            }
            
            return string.Join(Environment.NewLine, summary);
        }
        
        /// <summary>
        /// Applies a delta change to an objective function coefficient
        /// </summary>
        /// <param name="model">The original LP model</param>
        /// <param name="variableName">Name of the variable to modify</param>
        /// <param name="delta">Amount to change the coefficient (can be positive or negative)</param>
        /// <returns>Result of the sensitivity analysis</returns>
        public SensitivityResult ApplyDeltaToObjectiveCoefficient(LPModel model, string variableName, double delta)
        {
            var result = new SensitivityResult
            {
                OperationType = "Apply Delta to Objective Coefficient",
                OriginalModel = model
            };
            
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(variableName))
                {
                    result.Status = "Variable name cannot be empty";
                    return result;
                }
                
                if (!model.Variables.ContainsKey(variableName))
                {
                    result.Status = $"Variable '{variableName}' not found in the model";
                    return result;
                }
                
                if (Math.Abs(delta) < 0.001)
                {
                    result.Status = "Delta value is too small to have meaningful impact";
                    result.Warnings.Add("Consider using a larger delta value for more noticeable changes");
                }
                
                // Create a deep copy of the original model
                var modifiedModel = CloneModel(model);
                
                // Get the variable and apply delta
                var variable = modifiedModel.Variables[variableName];
                var originalCoeff = variable.Coefficient;
                var newCoeff = originalCoeff + delta;
                
                variable.Coefficient = newCoeff;
                
                // Prepare the result
                result.ModifiedModel = modifiedModel;
                result.IsSuccessful = true;
                result.Status = $"Successfully applied delta {delta:F3} to {variableName}";
                result.Details = $"Coefficient changed from {originalCoeff:F3} to {newCoeff:F3}";
                
                // Document changes
                result.Changes["Variable"] = variableName;
                result.Changes["Original Coefficient"] = originalCoeff.ToString("F3");
                result.Changes["Delta Applied"] = delta.ToString("F3");
                result.Changes["New Coefficient"] = newCoeff.ToString("F3");
                result.Changes["Change Percentage"] = $"{(Math.Abs(originalCoeff) > 0.001 ? (delta / originalCoeff * 100) : 0):F1}%";
                
                // Add analysis
                if (model.Sense == ObjectiveSense.Maximize)
                {
                    if (delta > 0)
                        result.Recommendations.Add("Positive delta in maximization - this variable becomes more attractive");
                    else
                        result.Recommendations.Add("Negative delta in maximization - this variable becomes less attractive");
                }
                else
                {
                    if (delta < 0)
                        result.Recommendations.Add("Negative delta in minimization - this variable becomes more attractive");
                    else
                        result.Recommendations.Add("Positive delta in minimization - this variable becomes less attractive");
                }
                
                result.Recommendations.Add("Re-solve the model to see the impact on the optimal solution");
                
                // Check for potential issues
                if (newCoeff == 0)
                {
                    result.Warnings.Add("New coefficient is zero - this variable will not contribute to the objective");
                }
                
                if ((originalCoeff > 0 && newCoeff < 0) || (originalCoeff < 0 && newCoeff > 0))
                {
                    result.Warnings.Add("Coefficient changed sign - this significantly alters the variable's role");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error applying delta: {ex.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Applies a delta change to a constraint RHS value
        /// </summary>
        /// <param name="model">The original LP model</param>
        /// <param name="constraintName">Name of the constraint to modify</param>
        /// <param name="delta">Amount to change the RHS (can be positive or negative)</param>
        /// <returns>Result of the sensitivity analysis</returns>
        public SensitivityResult ApplyDeltaToConstraintRHS(LPModel model, string constraintName, double delta)
        {
            var result = new SensitivityResult
            {
                OperationType = "Apply Delta to Constraint RHS",
                OriginalModel = model
            };
            
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(constraintName))
                {
                    result.Status = "Constraint name cannot be empty";
                    return result;
                }
                
                var constraint = model.Constraints.FirstOrDefault(c => c.Name == constraintName);
                if (constraint == null)
                {
                    result.Status = $"Constraint '{constraintName}' not found in the model";
                    return result;
                }
                
                if (Math.Abs(delta) < 0.001)
                {
                    result.Status = "Delta value is too small to have meaningful impact";
                    result.Warnings.Add("Consider using a larger delta value for more noticeable changes");
                }
                
                // Create a deep copy of the original model
                var modifiedModel = CloneModel(model);
                
                // Find the constraint in the modified model and apply delta
                var modifiedConstraint = modifiedModel.Constraints.FirstOrDefault(c => c.Name == constraintName);
                var originalRHS = modifiedConstraint.RHS;
                var newRHS = originalRHS + delta;
                
                modifiedConstraint.RHS = newRHS;
                
                // Prepare the result
                result.ModifiedModel = modifiedModel;
                result.IsSuccessful = true;
                result.Status = $"Successfully applied delta {delta:F3} to {constraintName}";
                result.Details = $"RHS changed from {originalRHS:F3} to {newRHS:F3}";
                
                // Document changes
                result.Changes["Constraint"] = constraintName;
                result.Changes["Original RHS"] = originalRHS.ToString("F3");
                result.Changes["Delta Applied"] = delta.ToString("F3");
                result.Changes["New RHS"] = newRHS.ToString("F3");
                result.Changes["Constraint Type"] = constraint.Relation.ToString();
                
                // Add analysis based on constraint type
                switch (constraint.Relation)
                {
                    case ConstraintRelation.LessThanEqual:
                        if (delta > 0)
                            result.Recommendations.Add("Increasing RHS for ? constraint - relaxes the constraint (more feasible region)");
                        else
                            result.Recommendations.Add("Decreasing RHS for ? constraint - tightens the constraint (smaller feasible region)");
                        break;
                    case ConstraintRelation.GreaterThanEqual:
                        if (delta > 0)
                            result.Recommendations.Add("Increasing RHS for ? constraint - tightens the constraint (smaller feasible region)");
                        else
                            result.Recommendations.Add("Decreasing RHS for ? constraint - relaxes the constraint (more feasible region)");
                        break;
                    case ConstraintRelation.Equal:
                        result.Recommendations.Add("Changed RHS for equality constraint - shifts the constraint line");
                        break;
                }
                
                result.Recommendations.Add("Re-solve the model to check feasibility and optimal value changes");
                
                // Check for potential issues
                if (newRHS < 0 && constraint.Relation == ConstraintRelation.GreaterThanEqual)
                {
                    result.Warnings.Add("Negative RHS with ? constraint may create feasibility issues");
                }
                
                if (Math.Abs(newRHS) < 0.001)
                {
                    result.Warnings.Add("RHS is very close to zero - constraint becomes very restrictive");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error applying delta: {ex.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Applies a delta change to a constraint coefficient
        /// </summary>
        /// <param name="model">The original LP model</param>
        /// <param name="constraintName">Name of the constraint to modify</param>
        /// <param name="variableName">Name of the variable whose coefficient to change</param>
        /// <param name="delta">Amount to change the coefficient</param>
        /// <returns>Result of the sensitivity analysis</returns>
        public SensitivityResult ApplyDeltaToConstraintCoefficient(LPModel model, string constraintName, string variableName, double delta)
        {
            var result = new SensitivityResult
            {
                OperationType = "Apply Delta to Constraint Coefficient",
                OriginalModel = model
            };
            
            try
            {
                // Validate inputs
                var constraint = model.Constraints.FirstOrDefault(c => c.Name == constraintName);
                if (constraint == null)
                {
                    result.Status = $"Constraint '{constraintName}' not found";
                    return result;
                }
                
                if (!model.Variables.ContainsKey(variableName))
                {
                    result.Status = $"Variable '{variableName}' not found";
                    return result;
                }
                
                // Create a deep copy of the original model
                var modifiedModel = CloneModel(model);
                
                // Find the constraint and apply delta
                var modifiedConstraint = modifiedModel.Constraints.FirstOrDefault(c => c.Name == constraintName);
                var originalCoeff = modifiedConstraint.Coefficients.TryGetValue(variableName, out var coeff) ? coeff : 0.0;
                var newCoeff = originalCoeff + delta;
                
                modifiedConstraint.Coefficients[variableName] = newCoeff;
                
                // Prepare the result
                result.ModifiedModel = modifiedModel;
                result.IsSuccessful = true;
                result.Status = $"Successfully applied delta {delta:F3} to {variableName} in {constraintName}";
                result.Details = $"Coefficient changed from {originalCoeff:F3} to {newCoeff:F3}";
                
                // Document changes
                result.Changes["Constraint"] = constraintName;
                result.Changes["Variable"] = variableName;
                result.Changes["Original Coefficient"] = originalCoeff.ToString("F3");
                result.Changes["Delta Applied"] = delta.ToString("F3");
                result.Changes["New Coefficient"] = newCoeff.ToString("F3");
                
                result.Recommendations.Add("Re-solve the model to see the impact on feasibility and optimality");
                result.Recommendations.Add("Check if the constraint becomes more or less restrictive");
                
                if (Math.Abs(newCoeff) < 0.001)
                {
                    result.Warnings.Add("New coefficient is very close to zero - variable has minimal impact on this constraint");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error applying delta: {ex.Message}";
                return result;
            }
        }
    }
}