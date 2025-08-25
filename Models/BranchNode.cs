using System;
using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Represents a node in the Branch and Bound tree
    /// </summary>
    public class BranchNode
    {
        /// <summary>
        /// Unique identifier for this node
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Parent node (null for root node)
        /// </summary>
        public BranchNode? Parent { get; set; }
        
        /// <summary>
        /// Child nodes
        /// </summary>
        public List<BranchNode> Children { get; set; } = new();
        
        /// <summary>
        /// Depth level in the tree (0 for root)
        /// </summary>
        public int Level { get; set; }
        
        /// <summary>
        /// Current status of this node
        /// </summary>
        public NodeStatus Status { get; set; } = NodeStatus.Active;
        
        /// <summary>
        /// LP relaxation bound for this node
        /// </summary>
        public double Bound { get; set; } = double.NaN;
        
        /// <summary>
        /// LP relaxation solution for this node
        /// </summary>
        public Dictionary<string, double> Solution { get; set; } = new();
        
        /// <summary>
        /// Branching variable and constraints for this node
        /// </summary>
        public List<BranchConstraint> BranchConstraints { get; set; } = new();
        
        /// <summary>
        /// Variable that was branched on to create this node
        /// </summary>
        public string? BranchingVariable { get; set; }
        
        /// <summary>
        /// Value of the branching variable
        /// </summary>
        public double BranchingValue { get; set; }
        
        /// <summary>
        /// Direction of the branch (left = <=, right = >=)
        /// </summary>
        public BranchDirection BranchDirection { get; set; }
        
        /// <summary>
        /// Best integer solution found at or below this node
        /// </summary>
        public IntegerSolution? BestIntegerSolution { get; set; }
        
        /// <summary>
        /// Execution time for solving this node's LP relaxation
        /// </summary>
        public double SolveTimeMs { get; set; }
        
        /// <summary>
        /// When this node was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        /// <summary>
        /// Additional information for debugging/display
        /// </summary>
        public string Notes { get; set; } = string.Empty;
        
        /// <summary>
        /// Determines if this node can be branched further
        /// </summary>
        public bool CanBranch => Status == NodeStatus.Active && !double.IsNaN(Bound);
        
        /// <summary>
        /// Gets the path from root to this node
        /// </summary>
        public List<BranchNode> GetPathFromRoot()
        {
            var path = new List<BranchNode>();
            var current = this;
            while (current != null)
            {
                path.Insert(0, current);
                current = current.Parent;
            }
            return path;
        }
        
        /// <summary>
        /// Gets all constraints that apply to this node (from root down)
        /// </summary>
        public List<BranchConstraint> GetAllConstraints()
        {
            var constraints = new List<BranchConstraint>();
            var path = GetPathFromRoot();
            foreach (var node in path)
            {
                constraints.AddRange(node.BranchConstraints);
            }
            return constraints;
        }
        
        /// <summary>
        /// Creates a display string for this node
        /// </summary>
        public string ToDisplayString()
        {
            if (Parent == null)
                return "Root";
                
            var direction = BranchDirection == BranchDirection.Left ? "?" : "?";
            return $"{BranchingVariable} {direction} {BranchingValue:F0}";
        }
        
        /// <summary>
        /// Gets detailed node information for UI display
        /// </summary>
        public string GetDetailedInfo()
        {
            var info = new List<string>
            {
                $"Node ID: {Id}",
                $"Level: {Level}",
                $"Status: {Status}",
                $"Bound: {(double.IsNaN(Bound) ? "N/A" : Bound.ToString("F3"))}",
                $"Solve Time: {SolveTimeMs:F2} ms"
            };
            
            if (!string.IsNullOrEmpty(BranchingVariable))
            {
                var direction = BranchDirection == BranchDirection.Left ? "?" : "?";
                info.Add($"Branch: {BranchingVariable} {direction} {BranchingValue:F0}");
            }
            
            if (BestIntegerSolution != null)
            {
                info.Add($"Best Integer Solution: {BestIntegerSolution.ObjectiveValue:F3}");
            }
            
            if (!string.IsNullOrEmpty(Notes))
            {
                info.Add($"Notes: {Notes}");
            }
            
            return string.Join(Environment.NewLine, info);
        }
    }
    
    /// <summary>
    /// Represents a branching constraint added to a node
    /// </summary>
    public class BranchConstraint
    {
        /// <summary>
        /// Variable being constrained
        /// </summary>
        public string VariableName { get; set; } = string.Empty;
        
        /// <summary>
        /// Type of constraint
        /// </summary>
        public ConstraintRelation Relation { get; set; }
        
        /// <summary>
        /// Right-hand side value
        /// </summary>
        public double Value { get; set; }
        
        /// <summary>
        /// Creates a constraint string representation
        /// </summary>
        public override string ToString()
        {
            var relationStr = Relation switch
            {
                ConstraintRelation.LessThanEqual => "?",
                ConstraintRelation.GreaterThanEqual => "?",
                ConstraintRelation.Equal => "=",
                _ => "?"
            };
            return $"{VariableName} {relationStr} {Value:F0}";
        }
    }
    
    /// <summary>
    /// Status of a node in the Branch and Bound tree
    /// </summary>
    public enum NodeStatus
    {
        /// <summary>
        /// Node is active and can be processed
        /// </summary>
        Active,
        
        /// <summary>
        /// Node is fathomed (pruned) due to bound
        /// </summary>
        FathomedByBound,
        
        /// <summary>
        /// Node is fathomed due to infeasibility
        /// </summary>
        FathomedByInfeasibility,
        
        /// <summary>
        /// Node is fathomed due to integrality (integer solution found)
        /// </summary>
        FathomedByIntegrality,
        
        /// <summary>
        /// Node is being processed
        /// </summary>
        Processing,
        
        /// <summary>
        /// Node processing completed
        /// </summary>
        Completed
    }
    
    /// <summary>
    /// Direction of branching from a parent node
    /// </summary>
    public enum BranchDirection
    {
        /// <summary>
        /// Left branch (typically x ? floor(value))
        /// </summary>
        Left,
        
        /// <summary>
        /// Right branch (typically x ? ceil(value))
        /// </summary>
        Right
    }
}