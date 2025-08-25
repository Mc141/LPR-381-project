using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Specialized Branch and Bound algorithm for knapsack problems
    /// Uses dynamic programming principles and efficient bounds
    /// </summary>
    public class BranchAndBoundKnapsack : IAlgorithmSolver
    {
        public string AlgorithmName => "Branch & Bound (Knapsack)";
        public int MaxIterations { get; set; } = 1000;
        public double Tolerance { get; set; } = 1e-6;
        
        private int _nodeIdCounter = 0;
        
        /// <summary>
        /// Solves a knapsack problem using specialized Branch and Bound
        /// </summary>
        public SolverResult Solve(LPModel model)
        {
            var result = new BranchAndBoundResult
            {
                AlgorithmUsed = AlgorithmName,
                OriginalModel = model,
                SolveStartTime = DateTime.Now
            };
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Convert to knapsack instance
                var knapsackInstance = KnapsackInstance.FromLPModel(model);
                
                if (!knapsackInstance.IsBinary)
                {
                    result.IsSuccessful = false;
                    result.Status = SolutionStatus.Error;
                    result.ErrorMessage = "Knapsack algorithm only supports binary (0-1) variables.";
                    return result;
                }
                
                // Get variable names for result mapping
                var variableNames = model.Variables.Values.OrderBy(v => v.Index).Select(v => v.Name).ToList();
                result.IntegerVariables = variableNames;
                
                // Initialize Branch and Bound
                var queue = new List<KnapsackNode>();
                KnapsackSolution? bestSolution = null;
                double bestValue = double.NegativeInfinity;
                
                // Create root node
                var rootNode = new KnapsackNode
                {
                    Id = _nodeIdCounter++,
                    Level = 0,
                    IncludedItems = new bool[knapsackInstance.Items.Count],
                    RemainingCapacity = knapsackInstance.Capacity,
                    CurrentValue = 0,
                    Status = NodeStatus.Active
                };
                
                // Calculate upper bound for root
                rootNode.UpperBound = CalculateUpperBound(knapsackInstance, rootNode);
                result.RootNode = ConvertToGenericNode(rootNode, knapsackInstance);
                result.AllNodes.Add(result.RootNode);
                result.RootBound = rootNode.UpperBound;
                
                queue.Add(rootNode);
                
                // Main Branch and Bound loop
                while (queue.Count > 0 && result.AllNodes.Count < MaxIterations)
                {
                    // Select node with best upper bound (best-first search)
                    var currentNode = queue.OrderByDescending(n => n.UpperBound).First();
                    queue.Remove(currentNode);
                    
                    // Check if node can be fathomed by bound
                    if (currentNode.UpperBound <= bestValue + Tolerance)
                    {
                        var genericNode = ConvertToGenericNode(currentNode, knapsackInstance);
                        genericNode.Status = NodeStatus.FathomedByBound;
                        genericNode.Notes = $"Fathomed: bound {currentNode.UpperBound:F3} ? incumbent {bestValue:F3}";
                        result.AllNodes.Add(genericNode);
                        continue;
                    }
                    
                    // Check if we've processed all items (leaf node)
                    if (currentNode.Level >= knapsackInstance.Items.Count)
                    {
                        // This is a complete solution
                        if (currentNode.CurrentValue > bestValue + Tolerance)
                        {
                            bestValue = currentNode.CurrentValue;
                            bestSolution = CreateKnapsackSolution(currentNode, knapsackInstance);
                        }
                        
                        var genericNode = ConvertToGenericNode(currentNode, knapsackInstance);
                        genericNode.Status = NodeStatus.FathomedByIntegrality;
                        genericNode.Notes = $"Complete solution: value = {currentNode.CurrentValue:F3}";
                        result.AllNodes.Add(genericNode);
                        continue;
                    }
                    
                    // Branch on next item
                    var nextItemIndex = currentNode.Level;
                    var nextItem = knapsackInstance.Items[nextItemIndex];
                    
                    // Create children: include item (if feasible) and exclude item
                    var children = new List<KnapsackNode>();
                    
                    // Right child: include the item (if feasible)
                    if (nextItem.Weight <= currentNode.RemainingCapacity + Tolerance)
                    {
                        var includeNode = new KnapsackNode
                        {
                            Id = _nodeIdCounter++,
                            Parent = currentNode,
                            Level = currentNode.Level + 1,
                            IncludedItems = (bool[])currentNode.IncludedItems.Clone(),
                            RemainingCapacity = currentNode.RemainingCapacity - nextItem.Weight,
                            CurrentValue = currentNode.CurrentValue + nextItem.Value,
                            Status = NodeStatus.Active,
                            BranchingVariable = nextItem.Name,
                            BranchingValue = 1,
                            BranchDirection = BranchDirection.Right
                        };
                        includeNode.IncludedItems[nextItemIndex] = true;
                        includeNode.UpperBound = CalculateUpperBound(knapsackInstance, includeNode);
                        
                        children.Add(includeNode);
                    }
                    
                    // Left child: exclude the item
                    var excludeNode = new KnapsackNode
                    {
                        Id = _nodeIdCounter++,
                        Parent = currentNode,
                        Level = currentNode.Level + 1,
                        IncludedItems = (bool[])currentNode.IncludedItems.Clone(),
                        RemainingCapacity = currentNode.RemainingCapacity,
                        CurrentValue = currentNode.CurrentValue,
                        Status = NodeStatus.Active,
                        BranchingVariable = nextItem.Name,
                        BranchingValue = 0,
                        BranchDirection = BranchDirection.Left
                    };
                    excludeNode.UpperBound = CalculateUpperBound(knapsackInstance, excludeNode);
                    
                    children.Add(excludeNode);
                    
                    // Add viable children to queue
                    foreach (var child in children)
                    {
                        if (child.UpperBound > bestValue + Tolerance)
                        {
                            queue.Add(child);
                        }
                        
                        var genericChild = ConvertToGenericNode(child, knapsackInstance);
                        result.AllNodes.Add(genericChild);
                        result.RootNode.Children.Add(genericChild);
                    }
                    
                    // Update current node status
                    var currentGenericNode = ConvertToGenericNode(currentNode, knapsackInstance);
                    currentGenericNode.Status = NodeStatus.Completed;
                    currentGenericNode.Children.AddRange(children.Select(c => ConvertToGenericNode(c, knapsackInstance)));
                    result.AllNodes.Add(currentGenericNode);
                }
                
                // Finalize results
                if (bestSolution != null)
                {
                    result.IsSuccessful = true;
                    result.Status = SolutionStatus.Optimal;
                    result.ObjectiveValue = bestSolution.TotalValue;
                    result.BestIntegerSolution = bestSolution.ToIntegerSolution();
                    result.BestIntegerSolution.Algorithm = AlgorithmName;
                    
                    // Create solution dictionary
                    result.Solution = new Dictionary<string, double>();
                    for (int i = 0; i < knapsackInstance.Items.Count; i++)
                    {
                        var item = knapsackInstance.Items[i];
                        result.Solution[item.Name] = bestSolution.SelectedItems.Any(si => si.Name == item.Name) ? 1.0 : 0.0;
                    }
                    
                    // Calculate optimality gap
                    if (Math.Abs(result.RootBound) > Tolerance)
                    {
                        result.OptimalityGap = Math.Abs((result.RootBound - bestSolution.TotalValue) / result.RootBound) * 100;
                    }
                }
                else
                {
                    result.IsSuccessful = true;
                    result.Status = queue.Count >= MaxIterations ? SolutionStatus.MaxIterationsReached : SolutionStatus.Infeasible;
                    result.ErrorMessage = queue.Count >= MaxIterations ? 
                        $"Maximum nodes ({MaxIterations}) reached." : 
                        "No feasible solution found.";
                }
                
                stopwatch.Stop();
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.SolveEndTime = DateTime.Now;
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.IsSuccessful = false;
                result.Status = SolutionStatus.Error;
                result.ErrorMessage = $"Error during knapsack Branch and Bound: {ex.Message}";
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.SolveEndTime = DateTime.Now;
                return result;
            }
        }
        
        /// <summary>
        /// Calculates the upper bound for a knapsack node using fractional knapsack
        /// </summary>
        private double CalculateUpperBound(KnapsackInstance instance, KnapsackNode node)
        {
            double bound = node.CurrentValue;
            double remainingCapacity = node.RemainingCapacity;
            
            // Sort remaining items by efficiency (value/weight ratio) in descending order
            var remainingItems = new List<(KnapsackItem item, int index)>();
            for (int i = node.Level; i < instance.Items.Count; i++)
            {
                if (!node.IncludedItems[i])
                {
                    remainingItems.Add((instance.Items[i], i));
                }
            }
            
            remainingItems = remainingItems.OrderByDescending(x => x.item.Efficiency).ToList();
            
            // Greedily add items (fractionally if necessary for the last item)
            foreach (var (item, index) in remainingItems)
            {
                if (item.Weight <= remainingCapacity)
                {
                    // Take the whole item
                    bound += item.Value;
                    remainingCapacity -= item.Weight;
                }
                else if (remainingCapacity > 0)
                {
                    // Take a fraction of the item (LP relaxation)
                    double fraction = remainingCapacity / item.Weight;
                    bound += fraction * item.Value;
                    break;
                }
            }
            
            return bound;
        }
        
        /// <summary>
        /// Converts a knapsack-specific node to a generic BranchNode
        /// </summary>
        private BranchNode ConvertToGenericNode(KnapsackNode knapsackNode, KnapsackInstance instance)
        {
            var genericNode = new BranchNode
            {
                Id = knapsackNode.Id,
                Level = knapsackNode.Level,
                Status = knapsackNode.Status,
                Bound = knapsackNode.UpperBound,
                BranchingVariable = knapsackNode.BranchingVariable,
                BranchingValue = knapsackNode.BranchingValue,
                BranchDirection = knapsackNode.BranchDirection,
                Notes = $"Knapsack node: value={knapsackNode.CurrentValue:F3}, capacity={knapsackNode.RemainingCapacity:F3}"
            };
            
            // Create solution dictionary
            for (int i = 0; i < Math.Min(knapsackNode.IncludedItems.Length, instance.Items.Count); i++)
            {
                var item = instance.Items[i];
                genericNode.Solution[item.Name] = knapsackNode.IncludedItems[i] ? 1.0 : 0.0;
            }
            
            return genericNode;
        }
        
        /// <summary>
        /// Creates a KnapsackSolution from a knapsack node
        /// </summary>
        private KnapsackSolution CreateKnapsackSolution(KnapsackNode node, KnapsackInstance instance)
        {
            var solution = new KnapsackSolution
            {
                TotalValue = node.CurrentValue,
                TotalWeight = instance.Capacity - node.RemainingCapacity,
                IsOptimal = true,
                NodeId = node.Id
            };
            
            for (int i = 0; i < node.IncludedItems.Length && i < instance.Items.Count; i++)
            {
                if (node.IncludedItems[i])
                {
                    solution.SelectedItems.Add(instance.Items[i].Clone());
                }
            }
            
            return solution;
        }
        
        /// <summary>
        /// Checks if this solver supports the given model
        /// </summary>
        public bool SupportsModel(LPModel model)
        {
            // Check if it's a knapsack problem (binary variables, single constraint, maximization)
            if (model.Sense != ObjectiveSense.Maximize) return false;
            
            var binaryVars = model.Variables.Values.Where(v => v.SignRestriction == SignRestriction.Binary).Count();
            if (binaryVars != model.Variables.Count) return false;
            
            var mainConstraints = model.Constraints.Where(c => 
                c.Relation == ConstraintRelation.LessThanEqual && c.RHS > 0).ToList();
            
            return mainConstraints.Count == 1;
        }
    }
    
    /// <summary>
    /// Specialized node for knapsack Branch and Bound
    /// </summary>
    internal class KnapsackNode
    {
        public int Id { get; set; }
        public KnapsackNode? Parent { get; set; }
        public int Level { get; set; }
        public bool[] IncludedItems { get; set; } = Array.Empty<bool>();
        public double RemainingCapacity { get; set; }
        public double CurrentValue { get; set; }
        public double UpperBound { get; set; }
        public NodeStatus Status { get; set; } = NodeStatus.Active;
        public string BranchingVariable { get; set; } = string.Empty;
        public double BranchingValue { get; set; }
        public BranchDirection BranchDirection { get; set; }
    }
}