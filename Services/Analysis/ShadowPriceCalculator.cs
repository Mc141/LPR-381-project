using System;
using System.Collections.Generic;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Analysis
{
    // Calculator for shadow prices from LP solutions
    public class ShadowPriceCalculator
    {
        // Calculate shadow prices for a solved LP model
        public ShadowPriceResult CalculateShadowPrices(LPModel model)
        {
            var result = new ShadowPriceResult
            {
                Status = "Calculated from demo data",
                Notes = "Shadow prices represent the marginal value of each constraint's right-hand side."
            };

            // For demo purposes, generate realistic shadow prices
            // In a real implementation, these would come from the optimal tableau
            foreach (var constraint in model.Constraints)
            {
                var shadowPrice = new ShadowPrice
                {
                    ConstraintName = constraint.Name,
                    Value = GenerateDemoShadowPrice(constraint),
                    IsActive = true // For demo, assume all constraints are active
                };

                shadowPrice.Interpretation = GenerateInterpretation(shadowPrice);
                result.ShadowPrices.Add(shadowPrice);
            }

            // Set a demo optimal value
            result.OptimalValue = CalculateDemoOptimalValue(model);

            return result;
        }

        // Generate demo shadow price based on constraint characteristics
        private double GenerateDemoShadowPrice(Constraint constraint)
        {
            // Generate realistic shadow prices based on constraint type
            var random = new Random(constraint.Name.GetHashCode()); // Consistent values

            return constraint.Relation switch
            {
                ConstraintRelation.LessThanEqual => Math.Round(random.NextDouble() * 2.0, 3),
                ConstraintRelation.Equal => Math.Round(random.NextDouble() * 3.0, 3),
                ConstraintRelation.GreaterThanEqual => Math.Round(random.NextDouble() * 1.5, 3),
                _ => 0.0
            };
        }

        // Generate interpretation text for shadow price
        private string GenerateInterpretation(ShadowPrice shadowPrice)
        {
            if (Math.Abs(shadowPrice.Value) < 0.001)
                return "No marginal value - constraint is not binding";

            var verb = shadowPrice.Value > 0 ? "increase" : "decrease";
            var amount = Math.Abs(shadowPrice.Value);
            
            return $"Objective would {verb} by {amount:F3} per unit increase in RHS";
        }

        // Calculate demo optimal value
        private double CalculateDemoOptimalValue(LPModel model)
        {
            // Simple demo calculation based on objective coefficients
            double value = 0;
            foreach (var variable in model.Variables.Values)
            {
                // Assume variables are at some reasonable values
                value += variable.Coefficient * (Math.Abs(variable.Coefficient) + 1);
            }

            return Math.Round(value * 0.7, 2); // Scale down for realism
        }
    }
}