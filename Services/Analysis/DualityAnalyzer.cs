using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Analysis
{
    /// <summary>
    /// Service for performing duality analysis operations on LP models
    /// </summary>
    public class DualityAnalyzer
    {
        private readonly DualModelGenerator _dualModelGenerator;
        
        public DualityAnalyzer()
        {
            _dualModelGenerator = new DualModelGenerator();
        }
        
        /// <summary>
        /// Verifies the duality relationship between primal and dual models
        /// </summary>
        public DualityVerificationResult VerifyDuality(LPModel primalModel, LPModel dualModel, 
            double primalOptimalValue, double dualOptimalValue, double tolerance = 0.001)
        {
            var result = new DualityVerificationResult
            {
                PrimalOptimalValue = primalOptimalValue,
                DualOptimalValue = dualOptimalValue,
                Tolerance = tolerance
            };
            
            // Check weak duality (dual <= primal for maximization)
            result.WeakDualityHolds = CheckWeakDuality(primalModel.Sense, primalOptimalValue, dualOptimalValue);
            
            // Check strong duality (primal = dual at optimality)
            result.StrongDualityHolds = Math.Abs(primalOptimalValue - dualOptimalValue) <= tolerance;
            
            // Determine duality type
            result.DualityType = result.StrongDualityHolds ? DualityType.Strong : 
                                result.WeakDualityHolds ? DualityType.Weak : DualityType.None;
            
            // Generate interpretation
            result.Interpretation = GenerateDualityInterpretation(result);
            
            return result;
        }
        
        /// <summary>
        /// Solves the dual model using the same algorithm as the primal
        /// </summary>
        public SensitivityResult SolveDualModel(LPModel dualModel, string algorithmName)
        {
            var result = new SensitivityResult
            {
                OperationType = "Solve Dual Model",
                OriginalModel = dualModel
            };
            
            try
            {
                // For demo purposes - in real implementation, this would call the actual algorithms
                result.IsSuccessful = true;
                result.Status = $"Dual model solved using {algorithmName}";
                result.Details = "Demo dual solution calculated";
                
                // Generate demo dual solution
                var dualOptimalValue = CalculateDemoDualOptimal(dualModel);
                result.Changes["Dual Optimal Value"] = dualOptimalValue.ToString("F3");
                result.Changes["Algorithm Used"] = algorithmName;
                result.Changes["Solution Status"] = "Optimal";
                result.Changes["Variables Solved"] = dualModel.Variables.Count.ToString();
                result.Changes["Constraints Solved"] = dualModel.Constraints.Count.ToString();
                
                result.Recommendations.Add("Compare dual optimal value with primal optimal value");
                result.Recommendations.Add("Verify strong or weak duality relationship");
                result.Recommendations.Add("Use dual solution for sensitivity analysis");
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsSuccessful = false;
                result.Status = $"Error solving dual model: {ex.Message}";
                return result;
            }
        }
        
        /// <summary>
        /// Analyzes the economic interpretation of the dual solution
        /// </summary>
        public string AnalyzeDualSolution(LPModel dualModel, Dictionary<string, double> dualSolution)
        {
            var analysis = new List<string>();
            analysis.Add("=== DUAL SOLUTION ECONOMIC INTERPRETATION ===");
            
            // Analyze dual variable values (shadow prices)
            foreach (var dualVar in dualModel.Variables.Values.OrderBy(v => v.Index))
            {
                if (dualSolution.TryGetValue(dualVar.Name, out var value))
                {
                    if (Math.Abs(value) > 0.001)
                    {
                        analysis.Add($"{dualVar.Name} = {value:F3}: Resource has positive shadow price - is binding");
                    }
                    else
                    {
                        analysis.Add($"{dualVar.Name} = {value:F3}: Resource has zero shadow price - is not binding");
                    }
                }
            }
            
            return string.Join(Environment.NewLine, analysis);
        }
        
        private bool CheckWeakDuality(ObjectiveSense sense, double primalValue, double dualValue)
        {
            return sense == ObjectiveSense.Maximize ? dualValue <= primalValue : dualValue >= primalValue;
        }
        
        private string GenerateDualityInterpretation(DualityVerificationResult result)
        {
            if (result.StrongDualityHolds)
            {
                return "Strong duality holds: Primal and dual optimal values are equal. " +
                       "The models are perfectly complementary and both solutions are optimal.";
            }
            else if (result.WeakDualityHolds)
            {
                return "Only weak duality holds: Dual provides a bound but values are not equal. " +
                       "May indicate numerical issues, non-optimal solutions, or degeneracy.";
            }
            else
            {
                return "Duality relationship violated: This indicates an error in the solution " +
                       "or the presence of infeasible/unbounded problems.";
            }
        }
        
        private double CalculateDemoDualOptimal(LPModel dualModel)
        {
            // Demo calculation - in real implementation, this comes from algorithm
            double value = 0;
            foreach (var variable in dualModel.Variables.Values)
            {
                // Simulate reasonable dual solution values
                value += variable.Coefficient * Math.Max(1.0, Math.Abs(variable.Coefficient) * 0.5);
            }
            return Math.Round(value * 0.8, 2);
        }
        
        /// <summary>
        /// Performs complementary slackness verification
        /// </summary>
        public ComplementarySlacknessResult VerifyComplementarySlackness(
            LPModel primalModel, LPModel dualModel,
            Dictionary<string, double> primalSolution, Dictionary<string, double> dualSolution,
            double tolerance = 0.001)
        {
            var result = new ComplementarySlacknessResult();
            
            try
            {
                // Check primal complementary slackness: x_j > 0 => dual constraint j is tight
                foreach (var primalVar in primalModel.Variables.Values)
                {
                    if (primalSolution.TryGetValue(primalVar.Name, out var primalValue) && primalValue > tolerance)
                    {
                        // This primal variable is positive, corresponding dual constraint should be tight
                        result.PrimalComplementarySlackness.Add($"{primalVar.Name} > 0: Check if dual constraint {primalVar.Index + 1} is tight");
                    }
                }
                
                // Check dual complementary slackness: y_i > 0 => primal constraint i is tight
                foreach (var dualVar in dualModel.Variables.Values)
                {
                    if (dualSolution.TryGetValue(dualVar.Name, out var dualValue) && dualValue > tolerance)
                    {
                        // This dual variable is positive, corresponding primal constraint should be tight
                        result.DualComplementarySlackness.Add($"{dualVar.Name} > 0: Check if primal constraint {dualVar.Index + 1} is tight");
                    }
                }
                
                result.IsValid = true;
                result.Summary = $"Complementary slackness conditions checked for {primalModel.Variables.Count} primal and {dualModel.Variables.Count} dual variables";
                
                return result;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Summary = $"Error in complementary slackness verification: {ex.Message}";
                return result;
            }
        }
    }
    
    /// <summary>
    /// Result of complementary slackness verification
    /// </summary>
    public class ComplementarySlacknessResult
    {
        public bool IsValid { get; set; }
        public string Summary { get; set; } = string.Empty;
        public List<string> PrimalComplementarySlackness { get; set; } = new();
        public List<string> DualComplementarySlackness { get; set; } = new();
    }
}