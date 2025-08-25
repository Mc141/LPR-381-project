using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LPR381_Assignment.Models;
using LPR381_Assignment.Services.Algorithms;

namespace LPR381_Assignment
{
    /// <summary>
    /// Event handlers for Integer Programming features (Branch & Bound tree, Cutting Planes)
    /// </summary>
    public partial class TabbedMainForm
    {
        // Additional dependencies for integer programming
        private readonly IntegerSolver _integerSolver = new();
        private BranchAndBoundResult? _lastBnBResult = null;
        private CuttingPlaneResult? _lastCuttingPlaneResult = null;
        
        /// <summary>
        /// Updates the node tree display after a Branch and Bound solve
        /// </summary>
        private void UpdateNodeTreeDisplay(BranchAndBoundResult result)
        {
            try
            {
                tvNodes.BeginUpdate();
                tvNodes.Nodes.Clear();
                
                if (result.RootNode == null)
                {
                    var noNodesNode = tvNodes.Nodes.Add("No Branch and Bound tree available");
                    noNodesNode.ForeColor = Color.Gray;
                    return;
                }
                
                // Add root node
                var rootTreeNode = tvNodes.Nodes.Add($"Root (Bound: {result.RootNode.Bound:F3})");
                rootTreeNode.Tag = result.RootNode;
                rootTreeNode.ImageIndex = GetNodeImageIndex(result.RootNode);
                
                // Build tree recursively
                BuildNodeTreeRecursive(rootTreeNode, result.RootNode);
                
                // Expand root and first level
                rootTreeNode.Expand();
                
                // Update status
                sbNode.Text = $"Nodes: {result.NodesProcessed}";
                
                // Auto-select root node
                tvNodes.SelectedNode = rootTreeNode;
                
            }
            finally
            {
                tvNodes.EndUpdate();
            }
        }
        
        /// <summary>
        /// Recursively builds the node tree UI from the Branch and Bound tree
        /// </summary>
        private void BuildNodeTreeRecursive(TreeNode treeNode, BranchNode branchNode)
        {
            foreach (var child in branchNode.Children)
            {
                var childText = child.ToDisplayString();
                if (!double.IsNaN(child.Bound))
                {
                    childText += $" (Bound: {child.Bound:F3})";
                }
                
                var childTreeNode = treeNode.Nodes.Add(childText);
                childTreeNode.Tag = child;
                
                // Set colors based on node status
                switch (child.Status)
                {
                    case NodeStatus.FathomedByBound:
                        childTreeNode.ForeColor = Color.Red;
                        childTreeNode.ToolTipText = "Fathomed by bound";
                        break;
                    case NodeStatus.FathomedByInfeasibility:
                        childTreeNode.ForeColor = Color.DarkRed;
                        childTreeNode.ToolTipText = "Fathomed by infeasibility";
                        break;
                    case NodeStatus.FathomedByIntegrality:
                        childTreeNode.ForeColor = Color.Green;
                        childTreeNode.ToolTipText = "Integer solution found";
                        break;
                    case NodeStatus.Completed:
                        childTreeNode.ForeColor = Color.Blue;
                        break;
                    default:
                        childTreeNode.ForeColor = Color.Black;
                        break;
                }
                
                // Recursively build children
                BuildNodeTreeRecursive(childTreeNode, child);
            }
        }
        
        /// <summary>
        /// Gets the appropriate image index for a node based on its status
        /// </summary>
        private int GetNodeImageIndex(BranchNode node)
        {
            return node.Status switch
            {
                NodeStatus.FathomedByIntegrality => 1, // Green icon
                NodeStatus.FathomedByBound => 2,       // Red icon
                NodeStatus.FathomedByInfeasibility => 3, // Dark red icon
                _ => 0 // Default icon
            };
        }
        
        /// <summary>
        /// Updates the node details panel when a node is selected
        /// </summary>
        private void UpdateNodeDetails(BranchNode? node)
        {
            if (node == null)
            {
                lblNodeTitle.Text = "Node: (select on left)";
                lblNodeStatus.Text = "Status: –";
                lblNodeBound.Text = "Bound: –";
                lblNodeIncumbent.Text = "Best Incumbent: –";
                lvNodeIterations.Items.Clear();
                return;
            }
            
            // Update node information
            lblNodeTitle.Text = $"Node: {node.ToDisplayString()} (ID: {node.Id})";
            lblNodeStatus.Text = $"Status: {node.Status}";
            lblNodeBound.Text = double.IsNaN(node.Bound) ? "Bound: –" : $"Bound: {node.Bound:F3}";
            
            // Show best incumbent
            if (node.BestIntegerSolution != null)
            {
                lblNodeIncumbent.Text = $"Best Incumbent: {node.BestIntegerSolution.ObjectiveValue:F3}";
            }
            else
            {
                lblNodeIncumbent.Text = "Best Incumbent: –";
            }
            
            // Update node iterations/details
            lvNodeIterations.Items.Clear();
            lvNodeIterations.Items.Add(new ListViewItem(new[] { "Solve", "LP Relaxation", $"Time: {node.SolveTimeMs:F2}ms, Bound: {(double.IsNaN(node.Bound) ? "N/A" : node.Bound.ToString("F3"))}" }));
            
            if (!string.IsNullOrEmpty(node.Notes))
            {
                lvNodeIterations.Items.Add(new ListViewItem(new[] { "Info", "Notes", node.Notes }));
            }
            
            // Show solution variables if available
            if (node.Solution.Count > 0)
            {
                var solutionStr = string.Join(", ", node.Solution.Take(5).Select(kv => $"{kv.Key}={kv.Value:F3}"));
                if (node.Solution.Count > 5)
                    solutionStr += "...";
                lvNodeIterations.Items.Add(new ListViewItem(new[] { "Solution", "Variables", solutionStr }));
            }
            
            // Show branching constraints
            var constraints = node.GetAllConstraints();
            if (constraints.Count > 0)
            {
                var constraintStr = string.Join(", ", constraints.Select(c => c.ToString()));
                lvNodeIterations.Items.Add(new ListViewItem(new[] { "Branch", "Constraints", constraintStr }));
            }
            
            // Update status bar
            sbNode.Text = $"Node: {node.Id}";
        }
        
        /// <summary>
        /// Updates the cuts display after a Cutting Plane solve
        /// </summary>
        private void UpdateCutsDisplay(CuttingPlaneResult result)
        {
            try
            {
                lvCuts.BeginUpdate();
                lvCuts.Items.Clear();
                
                if (result.CutsGenerated.Count == 0)
                {
                    var noCutsItem = new ListViewItem(new[] { "0", "None", "No cuts were generated" });
                    noCutsItem.ForeColor = Color.Gray;
                    lvCuts.Items.Add(noCutsItem);
                    return;
                }
                
                // Add cuts to the display
                foreach (var cut in result.CutsGenerated)
                {
                    var item = new ListViewItem(new[]
                    {
                        cut.Iteration.ToString(),
                        cut.Type.ToString(),
                        cut.ToDisplayString()
                    });
                    
                    // Color code by cut type
                    switch (cut.Type)
                    {
                        case CutType.Gomory:
                            item.ForeColor = Color.DarkBlue;
                            break;
                        case CutType.Fractional:
                            item.ForeColor = Color.DarkGreen;
                            break;
                        default:
                            item.ForeColor = Color.Black;
                            break;
                    }
                    
                    item.Tag = cut;
                    item.ToolTipText = $"Violation: {cut.Violation:F6}, Source: {cut.Source}";
                    lvCuts.Items.Add(item);
                }
                
                // Auto-resize columns
                foreach (ColumnHeader column in lvCuts.Columns)
                {
                    column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                }
                
            }
            finally
            {
                lvCuts.EndUpdate();
            }
        }
        
        /// <summary>
        /// Event handler for node selection in the tree view
        /// </summary>
        private void TvNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is BranchNode node)
            {
                UpdateNodeDetails(node);
            }
        }
        
        /// <summary>
        /// Event handler for adding a manual cut
        /// </summary>
        private void BtnAddCut_Click(object sender, EventArgs e)
        {
            // TODO: Implement manual cut addition dialog
            MessageBox.Show("Manual cut addition not yet implemented.", "Feature Coming Soon", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        /// <summary>
        /// Event handler for clearing all cuts
        /// </summary>
        private void BtnClearCuts_Click(object sender, EventArgs e)
        {
            lvCuts.Items.Clear();
            _lastCuttingPlaneResult = null;
            
            var clearedItem = new ListViewItem(new[] { "–", "Cleared", "All cuts have been cleared" });
            clearedItem.ForeColor = Color.Gray;
            lvCuts.Items.Add(clearedItem);
        }
        
        /// <summary>
        /// Clears integer programming results when starting a new solve
        /// </summary>
        private void ClearIntegerProgrammingResults()
        {
            try
            {
                // Clear node tree
                tvNodes.BeginUpdate();
                tvNodes.Nodes.Clear();
                var defaultNode = tvNodes.Nodes.Add("No tree available");
                defaultNode.ForeColor = Color.Gray;
                tvNodes.EndUpdate();
                
                // Clear cuts
                lvCuts.BeginUpdate();
                lvCuts.Items.Clear();
                var defaultCut = new ListViewItem(new[] { "–", "None", "No cuts available" });
                defaultCut.ForeColor = Color.Gray;
                lvCuts.Items.Add(defaultCut);
                lvCuts.EndUpdate();
                
                // Clear details
                UpdateNodeDetails(null);
                
                // Reset status
                sbNode.Text = "Node: –";
                
                // Clear cached results
                _lastBnBResult = null;
                _lastCuttingPlaneResult = null;
                
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing integer programming results: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Connects integer programming event handlers
        /// </summary>
        private void ConnectIntegerProgrammingHandlers()
        {
            try
            {
                // Connect node tree events
                tvNodes.AfterSelect += TvNodes_AfterSelect;
                
                // Connect cuts events
                btnAddCut.Click += BtnAddCut_Click;
                btnClearCuts.Click += BtnClearCuts_Click;
                
                System.Diagnostics.Debug.WriteLine("Integer programming event handlers connected successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting integer programming handlers: {ex.Message}");
            }
        }
    }
}