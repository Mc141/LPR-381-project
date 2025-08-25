using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Branch and Bound algorithm using Simplex method for LP relaxations
    /// </summary>
    public class BranchAndBoundSimplex : IAlgorithmSolver
    {
        public string AlgorithmName => "Branch & Bound (Simplex)";
        public int MaxIterations { get; set; } = 1000;
        public double Tolerance { get; set; } = 1e-6;
        
        private readonly SimplexEngine _simplexEngine;
        private int _nodeIdCounter = 0;
        private readonly int _maxNodes = 1000;
        private LPModel? _originalModel = null;
        
        public BranchAndBoundSimplex(SimplexEngine simplexEngine)
        {
            _simplexEngine = simplexEngine;
        }
        
        /// <summary>
        /// Solves an integer programming problem using Branch and Bound with Simplex
        /// </summary>
        public SolverResult Solve(LPModel model)
        {
            _originalModel = model; // Store reference to original model
            
            var result = new BranchAndBoundResult
            {
                AlgorithmUsed = AlgorithmName,
                OriginalModel = model,
                SolveStartTime = DateTime.Now
            };
            
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Get integer variables
                var integerVars = GetIntegerVariables(model);
                if (integerVars.Count == 0)
                {
                    result.IsSuccessful = false;
                    result.Status = SolutionStatus.Error;
                    result.ErrorMessage = "No integer variables found in the model.";
                    return result;
                }
                
                result.IntegerVariables = integerVars;
                
                // Initialize Branch and Bound
                var queue = new List<BranchNode>();
                IntegerSolution? bestSolution = null;
                double bestObjective = model.Sense == ObjectiveSense.Maximize ? 
                    double.NegativeInfinity : double.PositiveInfinity;
                
                // Create root node
                var rootNode = CreateRootNode(model);
                result.RootNode = rootNode;
                result.AllNodes.Add(rootNode);
                queue.Add(rootNode);
                
                // Solve root LP relaxation
                SolveNodeLPRelaxation(rootNode, model, result);
                
                if (rootNode.Status == NodeStatus.FathomedByInfeasibility)
                {
                    result.IsSuccessful = true;
                    result.Status = SolutionStatus.Infeasible;
                    result.ErrorMessage = "Root LP relaxation is infeasible.";
                    stopwatch.Stop();
                    result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                    return result;
                }
                
                result.RootBound = rootNode.Bound;
                
                // Check if root solution is already integer
                if (IsIntegerSolution(rootNode.Solution, integerVars))
                {
                    bestSolution = CreateIntegerSolution(rootNode, rootNode.Solution, rootNode.Bound);
                    bestObjective = rootNode.Bound;
                    rootNode.Status = NodeStatus.FathomedByIntegrality;
                    rootNode.BestIntegerSolution = bestSolution;
                }
                
                // Main Branch and Bound loop
                while (queue.Count > 0 && result.AllNodes.Count < _maxNodes)
                {
                    // Select node (best-first search)
                    var currentNode = SelectNextNode(queue, model.Sense);
                    queue.Remove(currentNode);
                    
                    // Check if node can be fathomed by bound
                    if (CanFathomByBound(currentNode, bestObjective, model.Sense))
                    {
                        currentNode.Status = NodeStatus.FathomedByBound;
                        currentNode.Notes = $"Fathomed: bound {currentNode.Bound:F3} worse than incumbent {bestObjective:F3}";
                        continue;
                    }
                    
                    // Check if solution is integer
                    if (IsIntegerSolution(currentNode.Solution, integerVars))
                    {
                        // Found integer solution
                        var intSolution = CreateIntegerSolution(currentNode, currentNode.Solution, currentNode.Bound);
                        
                        if (IsBetterSolution(currentNode.Bound, bestObjective, model.Sense))
                        {
                            bestSolution = intSolution;
                            bestObjective = currentNode.Bound;
                            currentNode.BestIntegerSolution = intSolution;
                        }
                        
                        currentNode.Status = NodeStatus.FathomedByIntegrality;
                        currentNode.Notes = $"Integer solution found: {currentNode.Bound:F3}";
                        continue;
                    }
                    
                    // Branch on fractional variable
                    var branchVar = SelectBranchingVariable(currentNode.Solution, integerVars);
                    if (branchVar == null)
                    {
                        currentNode.Status = NodeStatus.Completed;
                        currentNode.Notes = "No fractional integer variables found";
                        continue;
                    }
                    
                    // Create child nodes
                    var children = CreateChildNodes(currentNode, branchVar, currentNode.Solution[branchVar]);
                    foreach (var child in children)
                    {
                        result.AllNodes.Add(child);
                        
                        // Solve child LP relaxation
                        SolveNodeLPRelaxation(child, model, result);
                        
                        if (child.Status != NodeStatus.FathomedByInfeasibility &&
                            !CanFathomByBound(child, bestObjective, model.Sense))
                        {
                            queue.Add(child);
                        }
                    }
                    
                    currentNode.Status = NodeStatus.Completed;
                    currentNode.Children.AddRange(children);
                }
                
                // Finalize results
                result.BestIntegerSolution = bestSolution;
                
                if (bestSolution != null)
                {
                    result.IsSuccessful = true;
                    result.Status = SolutionStatus.Optimal;
                    result.ObjectiveValue = bestSolution.ObjectiveValue;
                    result.Solution = bestSolution.Variables;
                    
                    // Calculate optimality gap
                    if (!double.IsNaN(result.RootBound) && Math.Abs(result.RootBound) > Tolerance)
                    {
                        result.OptimalityGap = Math.Abs((result.RootBound - bestObjective) / result.RootBound) * 100;
                    }
                }
                else
                {
                    result.IsSuccessful = true;
                    result.Status = queue.Count >= _maxNodes ? SolutionStatus.MaxIterationsReached : SolutionStatus.Infeasible;
                    if (queue.Count >= _maxNodes)
                    {
                        result.ErrorMessage = $"Maximum number of nodes ({_maxNodes}) reached without finding optimal solution.";
                    }
                    else
                    {
                        result.ErrorMessage = "No integer solution found.";
                    }
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
                result.ErrorMessage = $"Error during Branch and Bound: {ex.Message}";
                result.ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                result.SolveEndTime = DateTime.Now;
                return result;
            }
        }
        
        /// <summary>
        /// Creates the root node for Branch and Bound
        /// </summary>
        private BranchNode CreateRootNode(LPModel model)
        {
            return new BranchNode
            {
                Id = _nodeIdCounter++,
                Level = 0,
                Status = NodeStatus.Active,
                Notes = "Root node - LP relaxation of original problem"
            };
        }
        
        /// <summary>
        /// Solves the LP relaxation for a given node
        /// </summary>
        private void SolveNodeLPRelaxation(BranchNode node, LPModel originalModel, BranchAndBoundResult result)
        {
            var nodeStopwatch = Stopwatch.StartNew();
            
            try
            {
                // Create modified model for this node
                var nodeModel = CreateNodeModel(originalModel, node);
                
                // Solve LP relaxation using Primal Simplex
                var lpResult = _simplexEngine.Solve(nodeModel, "Primal Simplex");
                
                nodeStopwatch.Stop();
                node.SolveTimeMs = nodeStopwatch.Elapsed.TotalMilliseconds;
                
                if (lpResult.IsSuccessful && lpResult.Status == SolutionStatus.Optimal)
                {
                    node.Bound = lpResult.ObjectiveValue;
                    node.Solution = new Dictionary<string, double>(lpResult.Solution);
                    node.Status = NodeStatus.Active;
                    node.Notes = $"LP relaxation solved: bound = {node.Bound:F3}";
                }
                else if (lpResult.Status == SolutionStatus.Infeasible)
                {
                    node.Status = NodeStatus.FathomedByInfeasibility;
                    node.Notes = "LP relaxation is infeasible";
                }
                else if (lpResult.Status == SolutionStatus.Unbounded)
                {
                    node.Status = NodeStatus.FathomedByInfeasibility;
                    node.Notes = "LP relaxation is unbounded - treating as infeasible for integer problem";
                }
                else
                {
                    node.Status = NodeStatus.FathomedByInfeasibility;
                    node.Notes = $"LP relaxation failed: {lpResult.ErrorMessage}";
                }
            }
            catch (Exception ex)
            {
                nodeStopwatch.Stop();
                node.SolveTimeMs = nodeStopwatch.Elapsed.TotalMilliseconds;
                node.Status = NodeStatus.FathomedByInfeasibility;
                node.Notes = $"Error solving LP relaxation: {ex.Message}";
            }
        }
        
        /// <summary>
        /// Creates a model for a specific node by adding branching constraints
        /// </summary>
        private LPModel CreateNodeModel(LPModel originalModel, BranchNode node)
        {
            // Create a copy of the original model
            var nodeModel = new LPModel
            {
                Sense = originalModel.Sense,
                Variables = new Dictionary<string, Variable>(),
                Constraints = new List<Constraint>()
            };
            
            // Copy variables (convert integer to continuous for LP relaxation)
            foreach (var variable in originalModel.Variables.Values)
            {
                var newVar = new Variable
                {
                    Name = variable.Name,
                    Index = variable.Index,
                    Coefficient = variable.Coefficient,
                    SignRestriction = variable.SignRestriction == SignRestriction.Integer || 
                                     variable.SignRestriction == SignRestriction.Binary ? 
                                     SignRestriction.Positive : variable.SignRestriction
                };
                nodeModel.Variables[newVar.Name] = newVar;
            }
            
            // Copy original constraints
            foreach (var constraint in originalModel.Constraints)
            {
                nodeModel.Constraints.Add(new Constraint
                {
                    Name = constraint.Name,
                    Coefficients = new Dictionary<string, double>(constraint.Coefficients),
                    Relation = constraint.Relation,
                    RHS = constraint.RHS
                });
            }
            
            // Add binary constraints for binary variables (x <= 1)
            int constraintIndex = nodeModel.Constraints.Count;
            foreach (var variable in originalModel.Variables.Values)
            {
                if (variable.SignRestriction == SignRestriction.Binary)
                {
                    var binaryConstraint = new Constraint
                    {
                        Name = $"Binary_{variable.Name}",
                        Coefficients = new Dictionary<string, double> { { variable.Name, 1.0 } },
                        Relation = ConstraintRelation.LessThanEqual,
                        RHS = 1.0
                    };
                    nodeModel.Constraints.Add(binaryConstraint);
                    constraintIndex++;
                }
            }
            
            // Add branching constraints from node path
            var allConstraints = node.GetAllConstraints();
            
            foreach (var branchConstraint in allConstraints)
            {
                var constraint = new Constraint
                {
                    Name = $"Branch_{constraintIndex++}",
                    Coefficients = new Dictionary<string, double> { { branchConstraint.VariableName, 1.0 } },
                    Relation = branchConstraint.Relation,
                    RHS = branchConstraint.Value
                };
                nodeModel.Constraints.Add(constraint);
            }
            
            return nodeModel;
        }
        
        /// <summary>
        /// Selects the next node to process (best-first strategy)
        /// </summary>
        private BranchNode SelectNextNode(List<BranchNode> queue, ObjectiveSense sense)
        {
            if (sense == ObjectiveSense.Maximize)
            {
                return queue.OrderByDescending(n => n.Bound).First();
            }
            else
            {
                return queue.OrderBy(n => n.Bound).First();
            }
        }
        
        /// <summary>
        /// Checks if a node can be fathomed by bound
        /// </summary>
        private bool CanFathomByBound(BranchNode node, double bestObjective, ObjectiveSense sense)
        {
            if (double.IsNaN(node.Bound) || double.IsInfinity(Math.Abs(bestObjective)))
                return false;
                
            if (sense == ObjectiveSense.Maximize)
            {
                return node.Bound <= bestObjective + Tolerance;
            }
            else
            {
                return node.Bound >= bestObjective - Tolerance;
            }
        }
        
        /// <summary>
        /// Checks if a solution has integer values for all integer variables
        /// </summary>
        private bool IsIntegerSolution(Dictionary<string, double> solution, List<string> integerVars)
        {
            foreach (var varName in integerVars)
            {
                if (solution.TryGetValue(varName, out var value))
                {
                    // Check if value is integer
                    if (Math.Abs(value - Math.Round(value)) > Tolerance)
                        return false;
                    
                    // For binary variables, also check 0 <= value <= 1
                    if (_originalModel != null && _originalModel.Variables.TryGetValue(varName, out var variable))
                    {
                        if (variable.SignRestriction == SignRestriction.Binary)
                        {
                            double roundedValue = Math.Round(value);
                            if (roundedValue < 0 || roundedValue > 1)
                                return false;
                        }
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// Selects the variable to branch on (most fractional strategy)
        /// </summary>
        private string? SelectBranchingVariable(Dictionary<string, double> solution, List<string> integerVars)
        {
            string? bestVar = null;
            double maxFractional = 0;
            
            foreach (var varName in integerVars)
            {
                if (solution.TryGetValue(varName, out var value))
                {
                    double fractional = Math.Abs(value - Math.Round(value));
                    if (fractional > maxFractional)
                    {
                        maxFractional = fractional;
                        bestVar = varName;
                    }
                }
            }
            
            return maxFractional > Tolerance ? bestVar : null;
        }
        
        /// <summary>
        /// Creates child nodes by branching on a variable
        /// </summary>
        private List<BranchNode> CreateChildNodes(BranchNode parent, string branchVar, double branchValue)
        {
            var children = new List<BranchNode>();
            
            // Check if this is a binary variable
            bool isBinary = _originalModel?.Variables.TryGetValue(branchVar, out var variable) == true && 
                           variable.SignRestriction == SignRestriction.Binary;
            
            if (isBinary)
            {
                // For binary variables, branch as x = 0 and x = 1
                // Left child: x <= 0 (effectively x = 0)
                var leftChild = new BranchNode
                {
                    Id = _nodeIdCounter++,
                    Parent = parent,
                    Level = parent.Level + 1,
                    Status = NodeStatus.Active,
                    BranchingVariable = branchVar,
                    BranchingValue = 0,
                    BranchDirection = BranchDirection.Left
                };
                
                leftChild.BranchConstraints.Add(new BranchConstraint
                {
                    VariableName = branchVar,
                    Relation = ConstraintRelation.LessThanEqual,
                    Value = 0
                });
                
                // Right child: x >= 1 (effectively x = 1)
                var rightChild = new BranchNode
                {
                    Id = _nodeIdCounter++,
                    Parent = parent,
                    Level = parent.Level + 1,
                    Status = NodeStatus.Active,
                    BranchingVariable = branchVar,
                    BranchingValue = 1,
                    BranchDirection = BranchDirection.Right
                };
                
                rightChild.BranchConstraints.Add(new BranchConstraint
                {
                    VariableName = branchVar,
                    Relation = ConstraintRelation.GreaterThanEqual,
                    Value = 1
                });
                
                children.Add(leftChild);
                children.Add(rightChild);
            }
            else
            {
                // For integer variables, use standard floor/ceil branching
                // Left child: x <= floor(value)
                var leftChild = new BranchNode
                {
                    Id = _nodeIdCounter++,
                    Parent = parent,
                    Level = parent.Level + 1,
                    Status = NodeStatus.Active,
                    BranchingVariable = branchVar,
                    BranchingValue = Math.Floor(branchValue),
                    BranchDirection = BranchDirection.Left
                };
                
                leftChild.BranchConstraints.Add(new BranchConstraint
                {
                    VariableName = branchVar,
                    Relation = ConstraintRelation.LessThanEqual,
                    Value = Math.Floor(branchValue)
                });
                
                // Right child: x >= ceil(value)
                var rightChild = new BranchNode
                {
                    Id = _nodeIdCounter++,
                    Parent = parent,
                    Level = parent.Level + 1,
                    Status = NodeStatus.Active,
                    BranchingVariable = branchVar,
                    BranchingValue = Math.Ceiling(branchValue),
                    BranchDirection = BranchDirection.Right
                };
                
                rightChild.BranchConstraints.Add(new BranchConstraint
                {
                    VariableName = branchVar,
                    Relation = ConstraintRelation.GreaterThanEqual,
                    Value = Math.Ceiling(branchValue)
                });
                
                children.Add(leftChild);
                children.Add(rightChild);
            }
            
            return children;
        }
        
        /// <summary>
        /// Creates an IntegerSolution from a node solution
        /// </summary>
        private IntegerSolution CreateIntegerSolution(BranchNode node, Dictionary<string, double> solution, double objectiveValue)
        {
            return new IntegerSolution
            {
                Variables = new Dictionary<string, double>(solution),
                ObjectiveValue = objectiveValue,
                IsFeasible = true,
                NodeId = node.Id,
                Algorithm = AlgorithmName,
                SolutionTimeMs = node.SolveTimeMs
            };
        }
        
        /// <summary>
        /// Determines if one solution is better than another
        /// </summary>
        private bool IsBetterSolution(double newObjective, double currentBest, ObjectiveSense sense)
        {
            if (double.IsInfinity(Math.Abs(currentBest)))
                return true;
                
            return sense == ObjectiveSense.Maximize ? 
                newObjective > currentBest + Tolerance : 
                newObjective < currentBest - Tolerance;
        }
        
        /// <summary>
        /// Gets all integer and binary variables from the model
        /// </summary>
        private List<string> GetIntegerVariables(LPModel model)
        {
            return model.Variables.Values
                .Where(v => v.SignRestriction == SignRestriction.Integer || 
                           v.SignRestriction == SignRestriction.Binary)
                .Select(v => v.Name)
                .ToList();
        }
        
        /// <summary>
        /// Checks if this solver supports the given model
        /// </summary>
        public bool SupportsModel(LPModel model)
        {
            // Supports models with integer or binary variables
            return model.Variables.Values.Any(v => 
                v.SignRestriction == SignRestriction.Integer || 
                v.SignRestriction == SignRestriction.Binary);
        }
    }
}