using System;
using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Represents an item in a knapsack problem
    /// </summary>
    public class KnapsackItem
    {
        /// <summary>
        /// Unique identifier for this item
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Name of the item
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Value (profit) of the item
        /// </summary>
        public double Value { get; set; }
        
        /// <summary>
        /// Weight (resource consumption) of the item
        /// </summary>
        public double Weight { get; set; }
        
        /// <summary>
        /// Value-to-weight ratio for greedy ordering
        /// </summary>
        public double Efficiency => Weight > 0 ? Value / Weight : double.MaxValue;
        
        /// <summary>
        /// Whether this item is currently selected
        /// </summary>
        public bool IsSelected { get; set; }
        
        /// <summary>
        /// Index in the original problem formulation
        /// </summary>
        public int OriginalIndex { get; set; }
        
        /// <summary>
        /// Additional constraints or properties
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();
        
        /// <summary>
        /// Creates a copy of this item
        /// </summary>
        public KnapsackItem Clone()
        {
            return new KnapsackItem
            {
                Id = Id,
                Name = Name,
                Value = Value,
                Weight = Weight,
                IsSelected = IsSelected,
                OriginalIndex = OriginalIndex,
                Properties = new Dictionary<string, object>(Properties)
            };
        }
        
        /// <summary>
        /// String representation for display
        /// </summary>
        public override string ToString()
        {
            return $"{Name}: Value={Value:F2}, Weight={Weight:F2}, Efficiency={Efficiency:F3}";
        }
    }
    
    /// <summary>
    /// Represents a knapsack instance for the knapsack-specific Branch and Bound
    /// </summary>
    public class KnapsackInstance
    {
        /// <summary>
        /// Capacity of the knapsack
        /// </summary>
        public double Capacity { get; set; }
        
        /// <summary>
        /// Items available for selection
        /// </summary>
        public List<KnapsackItem> Items { get; set; } = new();
        
        /// <summary>
        /// Whether this is a 0-1 knapsack (binary variables)
        /// </summary>
        public bool IsBinary { get; set; } = true;
        
        /// <summary>
        /// Creates a knapsack instance from an LP model
        /// </summary>
        /// <param name="model">LP model to convert</param>
        /// <returns>Knapsack instance</returns>
        public static KnapsackInstance FromLPModel(LPModel model)
        {
            var instance = new KnapsackInstance();
            
            // Find the capacity constraint (assuming single constraint for knapsack)
            if (model.Constraints.Count > 0)
            {
                var constraint = model.Constraints[0];
                instance.Capacity = constraint.RHS;
                
                // Create items from variables
                int id = 0;
                foreach (var variable in model.Variables.Values)
                {
                    var item = new KnapsackItem
                    {
                        Id = id++,
                        Name = variable.Name,
                        Value = variable.Coefficient,
                        Weight = constraint.Coefficients.GetValueOrDefault(variable.Name, 0),
                        OriginalIndex = variable.Index
                    };
                    
                    instance.Items.Add(item);
                }
            }
            
            // Check if all variables are binary
            instance.IsBinary = model.Variables.Values.All(v => 
                v.SignRestriction == SignRestriction.Binary);
            
            return instance;
        }
        
        /// <summary>
        /// Calculates the LP relaxation upper bound using fractional knapsack
        /// </summary>
        public double CalculateLPBound()
        {
            // Sort items by efficiency (value/weight ratio) in descending order
            var sortedItems = Items.OrderByDescending(item => item.Efficiency).ToList();
            
            double remainingCapacity = Capacity;
            double totalValue = 0;
            
            foreach (var item in sortedItems)
            {
                if (item.Weight <= remainingCapacity)
                {
                    // Take the whole item
                    totalValue += item.Value;
                    remainingCapacity -= item.Weight;
                }
                else if (remainingCapacity > 0)
                {
                    // Take a fraction of the item (LP relaxation)
                    double fraction = remainingCapacity / item.Weight;
                    totalValue += fraction * item.Value;
                    break;
                }
            }
            
            return totalValue;
        }
        
        /// <summary>
        /// Gets the greedy solution (take items in efficiency order)
        /// </summary>
        public KnapsackSolution GetGreedySolution()
        {
            var solution = new KnapsackSolution();
            var sortedItems = Items.OrderByDescending(item => item.Efficiency).ToList();
            
            double remainingCapacity = Capacity;
            
            foreach (var item in sortedItems)
            {
                if (item.Weight <= remainingCapacity)
                {
                    solution.SelectedItems.Add(item.Clone());
                    solution.TotalValue += item.Value;
                    solution.TotalWeight += item.Weight;
                    remainingCapacity -= item.Weight;
                }
            }
            
            solution.IsOptimal = false; // Greedy is not necessarily optimal
            return solution;
        }
        
        /// <summary>
        /// Validates a solution against the knapsack constraints
        /// </summary>
        public bool IsValidSolution(KnapsackSolution solution)
        {
            return solution.TotalWeight <= Capacity + 1e-6; // Small tolerance for floating point
        }
    }
    
    /// <summary>
    /// Represents a solution to a knapsack problem
    /// </summary>
    public class KnapsackSolution
    {
        /// <summary>
        /// Items selected in this solution
        /// </summary>
        public List<KnapsackItem> SelectedItems { get; set; } = new();
        
        /// <summary>
        /// Total value of selected items
        /// </summary>
        public double TotalValue { get; set; }
        
        /// <summary>
        /// Total weight of selected items
        /// </summary>
        public double TotalWeight { get; set; }
        
        /// <summary>
        /// Whether this is the optimal solution
        /// </summary>
        public bool IsOptimal { get; set; }
        
        /// <summary>
        /// Node where this solution was found
        /// </summary>
        public int NodeId { get; set; }
        
        /// <summary>
        /// Creates an IntegerSolution from this knapsack solution
        /// </summary>
        public IntegerSolution ToIntegerSolution()
        {
            var intSolution = new IntegerSolution
            {
                ObjectiveValue = TotalValue,
                IsFeasible = true,
                NodeId = NodeId
            };
            
            // Add binary variables for selected items
            foreach (var item in SelectedItems)
            {
                intSolution.Variables[item.Name] = 1.0;
            }
            
            return intSolution;
        }
        
        /// <summary>
        /// String representation for display
        /// </summary>
        public override string ToString()
        {
            return $"Value: {TotalValue:F2}, Weight: {TotalWeight:F2}, Items: {SelectedItems.Count}";
        }
    }
}