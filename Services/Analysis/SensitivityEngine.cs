using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Analysis
{
    /// <summary>
    /// Central engine that coordinates all sensitivity analysis operations
    /// </summary>
    public class SensitivityEngine
    {
        private readonly SensitivityAnalyzer _sensitivityAnalyzer;
        private readonly RangeAnalyzer _rangeAnalyzer;
        private readonly ShadowPriceCalculator _shadowPriceCalculator;
        private readonly DualityAnalyzer _dualityAnalyzer;
        private readonly DualModelGenerator _dualModelGenerator;
        
        public SensitivityEngine()
        {
            _sensitivityAnalyzer = new SensitivityAnalyzer();
            _rangeAnalyzer = new RangeAnalyzer();
            _shadowPriceCalculator = new ShadowPriceCalculator();
            _dualityAnalyzer = new DualityAnalyzer();
            _dualModelGenerator = new DualModelGenerator();
        }
        
        /// <summary>
        /// Performs comprehensive sensitivity analysis on a model
        /// </summary>
        public ComprehensiveSensitivityResult PerformComprehensiveAnalysis(LPModel model)
        {
            var result = new ComprehensiveSensitivityResult();
            
            try
            {
                // Variable range analysis
                result.VariableRanges = _rangeAnalyzer.CalculateVariableRanges(model);
                
                // Constraint RHS range analysis
                result.ConstraintRanges = _rangeAnalyzer.CalculateConstraintRHSRanges(model);
                
                // Shadow price calculation
                result.ShadowPrices = _shadowPriceCalculator.CalculateShadowPrices(model);
                
                // Dual model generation
                result.DualModelResult = _dualModelGenerator.GenerateDualModel(model);
                
                result.IsSuccessful = true;
                result.Status = "Comprehensive sensitivity analysis completed successfully";
                result.AnalysisTimestamp = DateTime.Now;
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error in comprehensive analysis: {ex.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Performs what-if analysis by applying multiple changes to a model
        /// </summary>
        public SensitivityResult PerformWhatIfAnalysis(LPModel model, List<ModelChange> changes)
        {
            var result = new SensitivityResult
            {
                OperationType = "What-If Analysis",
                OriginalModel = model
            };
            
            try
            {
                var modifiedModel = CloneModel(model);
                var appliedChanges = new List<string>();
                
                foreach (var change in changes)
                {
                    switch (change.ChangeType)
                    {
                        case ChangeType.ObjectiveCoefficient:
                            ApplyObjectiveCoefficientChange(modifiedModel, change);
                            appliedChanges.Add($"Changed {change.TargetName} coefficient by {change.Delta:F3}");
                            break;
                        case ChangeType.ConstraintRHS:
                            ApplyRHSChange(modifiedModel, change);
                            appliedChanges.Add($"Changed {change.TargetName} RHS by {change.Delta:F3}");
                            break;
                        case ChangeType.ConstraintCoefficient:
                            ApplyConstraintCoefficientChange(modifiedModel, change);
                            appliedChanges.Add($"Changed {change.TargetName} constraint coefficient");
                            break;
                    }
                }
                
                result.ModifiedModel = modifiedModel;
                result.IsSuccessful = true;
                result.Status = $"Applied {appliedChanges.Count} changes successfully";
                result.Details = string.Join("; ", appliedChanges);
                
                // Add analysis of changes
                result.Changes["Total Changes Applied"] = appliedChanges.Count;
                result.Changes["Original Variables"] = model.Variables.Count;
                result.Changes["Original Constraints"] = model.Constraints.Count;
                result.Changes["Modified Variables"] = modifiedModel.Variables.Count;
                result.Changes["Modified Constraints"] = modifiedModel.Constraints.Count;
                
                result.Recommendations.Add("Solve both original and modified models to compare results");
                result.Recommendations.Add("Analyze the impact on optimal value and solution");
                result.Recommendations.Add("Check if feasibility is maintained");
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error in what-if analysis: {ex.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Performs parametric analysis on objective coefficients
        /// </summary>
        public ParametricAnalysisResult PerformParametricAnalysis(LPModel model, string variableName, double minValue, double maxValue, int steps)
        {
            var result = new ParametricAnalysisResult
            {
                VariableName = variableName,
                MinValue = minValue,
                MaxValue = maxValue,
                Steps = steps
            };
            
            try
            {
                if (!model.Variables.ContainsKey(variableName))
                {
                    result.Status = $"Variable {variableName} not found in model";
                    return result;
                }
                
                double stepSize = (maxValue - minValue) / (steps - 1);
                var originalCoeff = model.Variables[variableName].Coefficient;
                
                for (int i = 0; i < steps; i++)
                {
                    double paramValue = minValue + i * stepSize;
                    double delta = paramValue - originalCoeff;
                    
                    // Apply change and analyze
                    var tempResult = _sensitivityAnalyzer.ApplyDeltaToObjectiveCoefficient(model, variableName, delta);
                    
                    result.ParameterValues.Add(paramValue);
                    result.Results.Add($"Coeff={paramValue:F3}: {tempResult.Status}");
                    
                    // Restore original coefficient for next iteration
                    model.Variables[variableName].Coefficient = originalCoeff;
                }
                
                result.IsSuccessful = true;
                result.Status = $"Parametric analysis completed for {steps} parameter values";
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error in parametric analysis: {ex.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Analyzes the sensitivity of the optimal solution to simultaneous changes
        /// </summary>
        public SensitivityResult AnalyzeSimultaneousChanges(LPModel model, Dictionary<string, double> variableDeltas, Dictionary<string, double> rhsDeltas)
        {
            var result = new SensitivityResult
            {
                OperationType = "Simultaneous Changes Analysis",
                OriginalModel = model
            };
            
            try
            {
                var modifiedModel = CloneModel(model);
                var changes = new List<string>();
                
                // Apply variable coefficient changes
                foreach (var change in variableDeltas)
                {
                    if (modifiedModel.Variables.ContainsKey(change.Key))
                    {
                        modifiedModel.Variables[change.Key].Coefficient += change.Value;
                        changes.Add($"Variable {change.Key}: coefficient delta = {change.Value:F3}");
                    }
                }
                
                // Apply RHS changes
                foreach (var change in rhsDeltas)
                {
                    var constraint = modifiedModel.Constraints.FirstOrDefault(c => c.Name == change.Key);
                    if (constraint != null)
                    {
                        constraint.RHS += change.Value;
                        changes.Add($"Constraint {change.Key}: RHS delta = {change.Value:F3}");
                    }
                }
                
                result.ModifiedModel = modifiedModel;
                result.IsSuccessful = true;
                result.Status = $"Applied {changes.Count} simultaneous changes";
                result.Details = string.Join("; ", changes);
                
                // Document changes
                result.Changes["Variable Changes"] = variableDeltas.Count;
                result.Changes["RHS Changes"] = rhsDeltas.Count;
                result.Changes["Total Changes"] = changes.Count;
                
                result.Recommendations.Add("Solve the modified model to assess cumulative impact");
                result.Recommendations.Add("Compare with individual change effects");
                result.Recommendations.Add("Check for interaction effects between changes");
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error in simultaneous changes analysis: {ex.Message}";
                return result;
            }
        }
        
        private void ApplyObjectiveCoefficientChange(LPModel model, ModelChange change)
        {
            if (model.Variables.TryGetValue(change.TargetName, out var variable))
            {
                variable.Coefficient += change.Delta;
            }
        }
        
        private void ApplyRHSChange(LPModel model, ModelChange change)
        {
            var constraint = model.Constraints.FirstOrDefault(c => c.Name == change.TargetName);
            if (constraint != null)
            {
                constraint.RHS += change.Delta;
            }
        }
        
        private void ApplyConstraintCoefficientChange(LPModel model, ModelChange change)
        {
            var constraint = model.Constraints.FirstOrDefault(c => c.Name == change.ConstraintName);
            if (constraint != null && constraint.Coefficients.ContainsKey(change.VariableName))
            {
                constraint.Coefficients[change.VariableName] += change.Delta;
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
    }
    
    /// <summary>
    /// Result of parametric analysis
    /// </summary>
    public class ParametricAnalysisResult
    {
        public bool IsSuccessful { get; set; }
        public string Status { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty;
        public double MinValue { get; set; }
        public double MaxValue { get; set; }
        public int Steps { get; set; }
        public List<double> ParameterValues { get; set; } = new();
        public List<string> Results { get; set; } = new();
    }
}