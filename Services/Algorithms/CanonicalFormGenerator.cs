using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Assignment.Models;
using SimplexTableau = LPR381_Assignment.Models.SimplexTableau;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Generates canonical form for LP problems
    /// </summary>
    public class CanonicalFormGenerator
    {
        /// <summary>
        /// Converts an LP model to canonical form
        /// </summary>
        public CanonicalForm GenerateCanonicalForm(LPModel model)
        {
            var canonicalForm = new CanonicalForm();
            
            try
            {
                canonicalForm.TransformationSteps.Add("Starting canonical form generation...");
                
                // Step 1: Handle objective function sense
                bool isMinimization = model.Sense == ObjectiveSense.Minimize;
                if (isMinimization)
                {
                    canonicalForm.TransformationSteps.Add("Converting minimization to maximization (multiply by -1)");
                }
                
                // Step 2: Count variables and constraints
                int originalVars = model.Variables.Count;
                int constraints = model.Constraints.Count;
                
                // Step 3: Determine additional variables needed
                int slackCount = 0;
                int surplusCount = 0;
                int artificialCount = 0;
                
                foreach (var constraint in model.Constraints)
                {
                    switch (constraint.Relation)
                    {
                        case ConstraintRelation.LessThanEqual:
                            slackCount++;
                            break;
                        case ConstraintRelation.GreaterThanEqual:
                            surplusCount++;
                            artificialCount++; // Need artificial for >= constraints
                            break;
                        case ConstraintRelation.Equal:
                            artificialCount++; // Need artificial for = constraints
                            break;
                    }
                }
                
                canonicalForm.TransformationSteps.Add($"Adding {slackCount} slack variables");
                canonicalForm.TransformationSteps.Add($"Adding {surplusCount} surplus variables");
                canonicalForm.TransformationSteps.Add($"Adding {artificialCount} artificial variables");
                
                // Step 4: Calculate tableau dimensions
                int totalVars = originalVars + slackCount + surplusCount + artificialCount;
                int tableauRows = constraints + 1; // +1 for objective row
                int tableauCols = totalVars + 1; // +1 for RHS column
                
                // Step 5: Create tableau
                var tableau = new SimplexTableau(tableauRows, tableauCols);
                
                // Step 6: Build variable names list
                var variableNames = new List<string>();
                
                // Original variables
                foreach (var variable in model.Variables.Values.OrderBy(v => v.Index))
                {
                    variableNames.Add(variable.Name);
                    canonicalForm.VariableMapping[variable.Name] = variable.Name;
                }
                
                // Slack variables
                for (int i = 0; i < slackCount; i++)
                {
                    var slackName = $"s{i + 1}";
                    variableNames.Add(slackName);
                    canonicalForm.SlackVariables.Add(slackName);
                }
                
                // Surplus variables
                for (int i = 0; i < surplusCount; i++)
                {
                    var surplusName = $"e{i + 1}";
                    variableNames.Add(surplusName);
                    canonicalForm.SurplusVariables.Add(surplusName);
                }
                
                // Artificial variables
                for (int i = 0; i < artificialCount; i++)
                {
                    var artificialName = $"a{i + 1}";
                    variableNames.Add(artificialName);
                    canonicalForm.ArtificialVariables.Add(artificialName);
                }
                
                // RHS column
                variableNames.Add("RHS");
                
                tableau.VariableNames = variableNames;
                
                // Step 7: Fill objective row
                FillObjectiveRow(tableau, model, canonicalForm, isMinimization);
                
                // Step 8: Fill constraint rows
                FillConstraintRows(tableau, model, canonicalForm);
                
                // Step 9: Set up basic variables (initially artificial and slack variables)
                SetupInitialBasicVariables(tableau, canonicalForm);
                
                // Step 10: Validate and finalize
                var validation = tableau.Validate();
                if (validation.IsValid)
                {
                    canonicalForm.Tableau = tableau;
                    canonicalForm.IsValid = true;
                    canonicalForm.TransformationSteps.Add("Canonical form generation completed successfully");
                }
                else
                {
                    canonicalForm.IsValid = false;
                    canonicalForm.ErrorMessage = $"Validation failed: {string.Join(", ", validation.Errors)}";
                }
                
                return canonicalForm;
            }
            catch (Exception ex)
            {
                canonicalForm.IsValid = false;
                canonicalForm.ErrorMessage = $"Error generating canonical form: {ex.Message}";
                return canonicalForm;
            }
        }
        
        /// <summary>
        /// Fills the objective row of the tableau
        /// </summary>
        private void FillObjectiveRow(SimplexTableau tableau, LPModel model, CanonicalForm canonicalForm, bool isMinimization)
        {
            int colIndex = 0;
            
            // Original variables
            foreach (var variable in model.Variables.Values.OrderBy(v => v.Index))
            {
                double coefficient = variable.Coefficient;
                if (isMinimization)
                    coefficient = -coefficient; // Convert to maximization
                
                // For maximization problems, we need negative coefficients in the objective row
                // For the simplex method to work correctly (we want to maximize, so negative coeffs indicate improvement)
                tableau.Matrix[0, colIndex] = -coefficient;
                colIndex++;
            }
            
            // Slack variables have 0 coefficient in objective
            foreach (var slack in canonicalForm.SlackVariables)
            {
                tableau.Matrix[0, colIndex] = 0;
                colIndex++;
            }
            
            // Surplus variables have 0 coefficient in objective
            foreach (var surplus in canonicalForm.SurplusVariables)
            {
                tableau.Matrix[0, colIndex] = 0;
                colIndex++;
            }
            
            // Artificial variables have large negative coefficient (Big M method)
            // For now, use -1000 as a large negative number
            foreach (var artificial in canonicalForm.ArtificialVariables)
            {
                tableau.Matrix[0, colIndex] = -1000;
                colIndex++;
            }
            
            // RHS of objective is initially 0
            tableau.Matrix[0, tableau.RHSColumn] = 0;
        }
        
        /// <summary>
        /// Fills the constraint rows of the tableau
        /// </summary>
        private void FillConstraintRows(SimplexTableau tableau, LPModel model, CanonicalForm canonicalForm)
        {
            int rowIndex = 1; // Start after objective row
            int slackIndex = 0;
            int surplusIndex = 0;
            int artificialIndex = 0;
            
            foreach (var constraint in model.Constraints)
            {
                int colIndex = 0;
                
                // Original variables
                foreach (var variable in model.Variables.Values.OrderBy(v => v.Index))
                {
                    double coefficient = constraint.Coefficients.TryGetValue(variable.Name, out var coeff) ? coeff : 0;
                    tableau.Matrix[rowIndex, colIndex] = coefficient;
                    colIndex++;
                }
                
                // Slack variables - each constraint gets its own slack variable
                for (int i = 0; i < canonicalForm.SlackVariables.Count; i++)
                {
                    if (constraint.Relation == ConstraintRelation.LessThanEqual && i == slackIndex)
                    {
                        tableau.Matrix[rowIndex, colIndex] = 1; // Add slack to this constraint
                    }
                    else
                    {
                        tableau.Matrix[rowIndex, colIndex] = 0; // No slack for other constraints
                    }
                    colIndex++;
                }
                
                // Surplus variables - each >= constraint gets its own surplus variable
                for (int i = 0; i < canonicalForm.SurplusVariables.Count; i++)
                {
                    if (constraint.Relation == ConstraintRelation.GreaterThanEqual && i == surplusIndex)
                    {
                        tableau.Matrix[rowIndex, colIndex] = -1; // Subtract surplus from this constraint
                    }
                    else
                    {
                        tableau.Matrix[rowIndex, colIndex] = 0; // No surplus for other constraints
                    }
                    colIndex++;
                }
                
                // Artificial variables - each >= or = constraint gets its own artificial variable
                for (int i = 0; i < canonicalForm.ArtificialVariables.Count; i++)
                {
                    if ((constraint.Relation == ConstraintRelation.GreaterThanEqual || 
                         constraint.Relation == ConstraintRelation.Equal) && 
                        i == artificialIndex)
                    {
                        tableau.Matrix[rowIndex, colIndex] = 1; // Add artificial to this constraint
                    }
                    else
                    {
                        tableau.Matrix[rowIndex, colIndex] = 0; // No artificial for other constraints
                    }
                    colIndex++;
                }
                
                // RHS
                tableau.Matrix[rowIndex, tableau.RHSColumn] = constraint.RHS;
                
                // Update indices based on constraint type
                if (constraint.Relation == ConstraintRelation.LessThanEqual)
                    slackIndex++;
                if (constraint.Relation == ConstraintRelation.GreaterThanEqual)
                {
                    surplusIndex++;
                    artificialIndex++;
                }
                if (constraint.Relation == ConstraintRelation.Equal)
                    artificialIndex++;
                
                rowIndex++;
            }
        }
        
        /// <summary>
        /// Sets up the initial basic variables
        /// </summary>
        private void SetupInitialBasicVariables(SimplexTableau tableau, CanonicalForm canonicalForm)
        {
            tableau.BasicVariables.Clear();
            
            // Initially, basic variables are slack and artificial variables
            // This is a simplified approach - in practice, we need to ensure an identity matrix
            
            int constraintIndex = 0;
            foreach (var slack in canonicalForm.SlackVariables)
            {
                if (constraintIndex < tableau.Rows - 1)
                {
                    tableau.BasicVariables.Add(slack);
                    constraintIndex++;
                }
            }
            
            foreach (var artificial in canonicalForm.ArtificialVariables)
            {
                if (constraintIndex < tableau.Rows - 1)
                {
                    tableau.BasicVariables.Add(artificial);
                    constraintIndex++;
                }
            }
            
            // Fill remaining with dummy variables if needed
            while (tableau.BasicVariables.Count < tableau.Rows - 1)
            {
                tableau.BasicVariables.Add($"dummy{tableau.BasicVariables.Count + 1}");
            }
            
            // Update non-basic variables
            tableau.NonBasicVariables.Clear();
            foreach (var variable in tableau.VariableNames)
            {
                if (variable != "RHS" && !tableau.BasicVariables.Contains(variable))
                {
                    tableau.NonBasicVariables.Add(variable);
                }
            }
        }
        
        /// <summary>
        /// Gets the index of a constraint in the model
        /// </summary>
        private int GetConstraintIndex(Constraint constraint, LPModel model)
        {
            return model.Constraints.IndexOf(constraint);
        }
        
        /// <summary>
        /// Validates that the model can be converted to canonical form
        /// </summary>
        public ValidationResult ValidateModelForCanonicalForm(LPModel model)
        {
            var result = new ValidationResult();
            
            if (model.Variables.Count == 0)
                result.AddError("Model has no variables");
            
            if (model.Constraints.Count == 0)
                result.AddError("Model has no constraints");
            
            // Check for negative RHS values
            foreach (var constraint in model.Constraints)
            {
                if (constraint.RHS < 0)
                {
                    result.AddWarning($"Constraint {constraint.Name} has negative RHS ({constraint.RHS}) - may need preprocessing");
                }
            }
            
            // Check for unrestricted variables
            foreach (var variable in model.Variables.Values)
            {
                if (variable.SignRestriction == SignRestriction.Unrestricted)
                {
                    result.AddWarning($"Variable {variable.Name} is unrestricted - may need splitting into two variables");
                }
            }
            
            return result;
        }
    }
}