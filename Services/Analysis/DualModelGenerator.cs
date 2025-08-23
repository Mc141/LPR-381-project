using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Analysis
{
    // Service for generating dual models from primal LP models
    public class DualModelGenerator
    {
        // Generate the dual model from a primal LP model
        public DualModelResult GenerateDualModel(LPModel primalModel)
        {
            var result = new DualModelResult
            {
                Status = "Successfully generated dual model",
                IsValidTransformation = true
            };

            try
            {
                // Step 1: Initialize dual model
                var dualModel = new DualModel();
                var transformationSteps = new List<string>();

                // Step 2: Record original model summary
                dualModel.OriginalModelSummary = CreatePrimalSummary(primalModel);
                transformationSteps.Add("=== PRIMAL TO DUAL TRANSFORMATION ===");
                transformationSteps.Add($"Original Primal: {primalModel.Sense} with {primalModel.Variables.Count} variables, {primalModel.Constraints.Count} constraints");

                // Step 3: Transform objective sense (Primal Max ? Dual Min, Primal Min ? Dual Max)
                dualModel.Sense = primalModel.Sense == ObjectiveSense.Maximize ? ObjectiveSense.Minimize : ObjectiveSense.Maximize;
                transformationSteps.Add($"Objective sense: {primalModel.Sense} -> {dualModel.Sense}");

                // Step 4: Create dual variables (one for each primal constraint)
                CreateDualVariables(primalModel, dualModel, transformationSteps);

                // Step 5: Create dual constraints (one for each primal variable)
                CreateDualConstraints(primalModel, dualModel, transformationSteps);

                // Step 6: Format the dual model
                dualModel.FormattedObjective = FormatDualObjective(dualModel);
                
                // Step 7: Create formatted output
                result.DualModel = dualModel;
                result.TransformationSteps = transformationSteps;
                result.FormattedDualModel = FormatCompleteDualModel(dualModel);
                result.ComparisonSummary = CreateComparisonSummary(primalModel, dualModel);

                return result;
            }
            catch (Exception ex)
            {
                result.Status = $"Error generating dual model: {ex.Message}";
                result.IsValidTransformation = false;
                return result;
            }
        }

        // Create dual variables from primal constraints
        private void CreateDualVariables(LPModel primalModel, DualModel dualModel, List<string> steps)
        {
            int variableIndex = 0;
            steps.Add("Creating dual variables from primal constraints:");

            foreach (var constraint in primalModel.Constraints)
            {
                var dualVar = new DualVariable
                {
                    Name = $"y{variableIndex + 1}",
                    Coefficient = constraint.RHS,
                    CorrespondingPrimalConstraint = constraint.Name,
                    Index = variableIndex++
                };

                // Set sign restriction based on primal constraint type and objective sense
                dualVar.SignRestriction = DetermineVariableSignRestriction(constraint.Relation, primalModel.Sense);

                dualModel.Variables[dualVar.Name] = dualVar;
                
                var signStr = dualVar.SignRestriction switch
                {
                    SignRestriction.Positive => ">= 0",
                    SignRestriction.Negative => "<= 0", 
                    SignRestriction.Unrestricted => "urs",
                    _ => "?"
                };

                steps.Add($"  {dualVar.Name}: coeff = {dualVar.Coefficient} (from {constraint.Name} RHS), sign = {signStr}");
            }
        }

        // Create dual constraints from primal variables
        private void CreateDualConstraints(LPModel primalModel, DualModel dualModel, List<string> steps)
        {
            steps.Add("Creating dual constraints from primal variables:");

            foreach (var primalVar in primalModel.Variables.Values.OrderBy(v => v.Index))
            {
                var dualConstraint = new DualConstraint
                {
                    Name = $"Constraint_{primalVar.Name}",
                    RHS = primalVar.Coefficient,
                    CorrespondingPrimalVariable = primalVar.Name
                };

                // Set constraint relation based on primal variable sign and objective sense
                dualConstraint.Relation = DetermineConstraintRelation(primalVar.SignRestriction, primalModel.Sense);

                // Set coefficients from primal constraint matrix (transpose)
                int dualVarIndex = 0;
                foreach (var primalConstraint in primalModel.Constraints)
                {
                    var dualVarName = $"y{dualVarIndex + 1}";
                    var coefficient = primalConstraint.Coefficients.TryGetValue(primalVar.Name, out var coeff) ? coeff : 0.0;
                    dualConstraint.Coefficients[dualVarName] = coefficient;
                    dualVarIndex++;
                }

                dualModel.Constraints.Add(dualConstraint);

                var relationStr = dualConstraint.Relation switch
                {
                    ConstraintRelation.LessThanEqual => "<=",
                    ConstraintRelation.Equal => "=",
                    ConstraintRelation.GreaterThanEqual => ">=",
                    _ => "?"
                };

                var coeffStr = string.Join(" + ", dualConstraint.Coefficients.Where(kv => kv.Value != 0).Select(kv => 
                    kv.Value == 1 ? kv.Key : $"{kv.Value}{kv.Key}"));

                steps.Add($"  {coeffStr} {relationStr} {dualConstraint.RHS} (from {primalVar.Name})");
            }
        }

        // Determine dual variable sign restriction
        private SignRestriction DetermineVariableSignRestriction(ConstraintRelation primalRelation, ObjectiveSense primalSense)
        {
            // Standard duality rules
            return (primalRelation, primalSense) switch
            {
                (ConstraintRelation.LessThanEqual, ObjectiveSense.Maximize) => SignRestriction.Positive,
                (ConstraintRelation.LessThanEqual, ObjectiveSense.Minimize) => SignRestriction.Negative,
                (ConstraintRelation.GreaterThanEqual, ObjectiveSense.Maximize) => SignRestriction.Negative,
                (ConstraintRelation.GreaterThanEqual, ObjectiveSense.Minimize) => SignRestriction.Positive,
                (ConstraintRelation.Equal, _) => SignRestriction.Unrestricted,
                _ => SignRestriction.Positive
            };
        }

        // Determine dual constraint relation
        private ConstraintRelation DetermineConstraintRelation(SignRestriction primalVarSign, ObjectiveSense primalSense)
        {
            // Standard duality rules
            return (primalVarSign, primalSense) switch
            {
                (SignRestriction.Positive, ObjectiveSense.Maximize) => ConstraintRelation.LessThanEqual,
                (SignRestriction.Positive, ObjectiveSense.Minimize) => ConstraintRelation.GreaterThanEqual,
                (SignRestriction.Negative, ObjectiveSense.Maximize) => ConstraintRelation.GreaterThanEqual,
                (SignRestriction.Negative, ObjectiveSense.Minimize) => ConstraintRelation.LessThanEqual,
                (SignRestriction.Unrestricted, _) => ConstraintRelation.Equal,
                _ => ConstraintRelation.LessThanEqual
            };
        }

        // Format the dual objective function
        private string FormatDualObjective(DualModel dualModel)
        {
            var sb = new StringBuilder();
            sb.Append(dualModel.Sense == ObjectiveSense.Maximize ? "maximize " : "minimize ");

            var terms = dualModel.Variables.Values.OrderBy(v => v.Index).Select(v =>
            {
                var coeff = v.Coefficient;
                var sign = coeff >= 0 ? "+" : "-";
                var absCoeff = Math.Abs(coeff);
                return absCoeff == 1 ? $"{sign}{v.Name}" : $"{sign}{absCoeff}{v.Name}";
            });

            sb.Append(string.Join(" ", terms).TrimStart('+'));
            return sb.ToString();
        }

        // Create a summary of the primal model
        private string CreatePrimalSummary(LPModel primalModel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PRIMAL MODEL:");
            sb.AppendLine(primalModel.GetFormattedObjective());
            
            foreach (var constraint in primalModel.Constraints)
            {
                sb.AppendLine(constraint.ToString());
            }

            var signRestrictions = primalModel.Variables.Values.OrderBy(v => v.Index).Select(v =>
                $"{v.Name} {(v.SignRestriction == SignRestriction.Positive ? ">= 0" : 
                          v.SignRestriction == SignRestriction.Negative ? "<= 0" : 
                          v.SignRestriction == SignRestriction.Unrestricted ? "urs" : "?")}");
            
            sb.AppendLine(string.Join(", ", signRestrictions));
            return sb.ToString();
        }

        // Format the complete dual model
        private string FormatCompleteDualModel(DualModel dualModel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("DUAL MODEL:");
            sb.AppendLine(dualModel.FormattedObjective);
            sb.AppendLine();
            sb.AppendLine("Subject to:");

            foreach (var constraint in dualModel.Constraints)
            {
                var coeffStr = string.Join(" + ", constraint.Coefficients.Where(kv => kv.Value != 0).Select(kv =>
                {
                    var coeff = kv.Value;
                    var sign = coeff >= 0 ? "+" : "-";
                    var absCoeff = Math.Abs(coeff);
                    return absCoeff == 1 ? $"{sign}{kv.Key}" : $"{sign}{absCoeff}{kv.Key}";
                })).TrimStart('+');

                var relationStr = constraint.Relation switch
                {
                    ConstraintRelation.LessThanEqual => "<=",
                    ConstraintRelation.Equal => "=",
                    ConstraintRelation.GreaterThanEqual => ">=",
                    _ => "?"
                };

                sb.AppendLine($"  {coeffStr} {relationStr} {constraint.RHS}");
            }

            sb.AppendLine();
            var signRestrictions = dualModel.Variables.Values.OrderBy(v => v.Index).Select(v =>
                $"{v.Name} {(v.SignRestriction == SignRestriction.Positive ? ">= 0" :
                          v.SignRestriction == SignRestriction.Negative ? "<= 0" :
                          v.SignRestriction == SignRestriction.Unrestricted ? "urs" : "?")}");

            sb.AppendLine(string.Join(", ", signRestrictions));
            return sb.ToString();
        }

        // Create comparison summary between primal and dual
        private string CreateComparisonSummary(LPModel primalModel, DualModel dualModel)
        {
            var sb = new StringBuilder();
            sb.AppendLine("PRIMAL-DUAL COMPARISON:");
            sb.AppendLine($"Primal variables: {primalModel.Variables.Count} -> Dual constraints: {dualModel.Constraints.Count}");
            sb.AppendLine($"Primal constraints: {primalModel.Constraints.Count} -> Dual variables: {dualModel.Variables.Count}");
            sb.AppendLine($"Primal objective: {primalModel.Sense} -> Dual objective: {dualModel.Sense}");
            sb.AppendLine();
            sb.AppendLine("Transformation follows standard LP duality theory:");
            sb.AppendLine("• Each primal constraint becomes a dual variable");
            sb.AppendLine("• Each primal variable becomes a dual constraint");
            sb.AppendLine("• Objective sense is flipped (max <-> min)");
            sb.AppendLine("• Coefficient matrix is transposed");
            return sb.ToString();
        }
    }
}