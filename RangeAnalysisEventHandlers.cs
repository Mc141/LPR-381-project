using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LPR381_Assignment.Services.Analysis;

namespace LPR381_Assignment
{
    // Partial class containing range analysis event handlers
    public partial class TabbedMainForm
    {
        // Range analyzer instance
        private readonly RangeAnalyzer _rangeAnalyzer = new();
        private bool _rangeHandlersConnected = false;

        // Override the Load event to connect event handlers after initialization
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ConnectRangeAnalysisEventHandlers();
        }

        // Event handler for non-basic variable range analysis
        private void SaNB_ShowRange_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before analyzing variable ranges.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = _rangeAnalyzer.CalculateVariableRanges(_currentModel, basicVariablesOnly: false);

                // Clear previous results
                saNB_Output.Items.Clear();
                saNB_Output.View = View.Details;
                saNB_Output.Columns.Clear();
                saNB_Output.Columns.Add("Variable", 100);
                saNB_Output.Columns.Add("Current Coeff", 120);
                saNB_Output.Columns.Add("Allowable Range", 150);
                saNB_Output.Columns.Add("Type", 80);
                saNB_Output.Columns.Add("Interpretation", 800);

                // Add variable range results (focus on non-basic variables)
                foreach (var varRange in result.VariableRanges.Where(v => !v.IsBasic))
                {
                    var item = new ListViewItem(varRange.VariableName);
                    item.SubItems.Add(varRange.CurrentCoefficient.ToString("F3"));
                    item.SubItems.Add(varRange.FormattedRange);
                    item.SubItems.Add("Non-Basic");
                    item.SubItems.Add(varRange.Interpretation);
                    saNB_Output.Items.Add(item);
                }

                // Add summary if no non-basic variables found
                if (!result.VariableRanges.Any(v => !v.IsBasic))
                {
                    var summaryItem = new ListViewItem("INFO");
                    summaryItem.SubItems.Add("N/A");
                    summaryItem.SubItems.Add("N/A");
                    summaryItem.SubItems.Add("Summary");
                    summaryItem.SubItems.Add("All variables appear to be basic in current solution");
                    summaryItem.Font = new Font(saNB_Output.Font, FontStyle.Italic);
                    saNB_Output.Items.Add(summaryItem);
                }

                sbStatus.Text = $"Non-basic variable ranges calculated for {result.VariableRanges.Count(v => !v.IsBasic)} variables.";
                MessageBox.Show($"Variable range analysis completed!\n\nAnalyzed {result.VariableRanges.Count} variables.", 
                    "Range Analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating variable ranges: {ex.Message}", 
                    "Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Variable range analysis failed.";
            }
        }

        // Event handler for basic variable range analysis
        private void SaB_ShowRange_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before analyzing variable ranges.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = _rangeAnalyzer.CalculateVariableRanges(_currentModel, basicVariablesOnly: true);

                // Clear previous results
                saB_Output.Items.Clear();
                saB_Output.View = View.Details;
                saB_Output.Columns.Clear();
                saB_Output.Columns.Add("Variable", 100);
                saB_Output.Columns.Add("Current Coeff", 120);
                saB_Output.Columns.Add("Allowable Range", 150);
                saB_Output.Columns.Add("Type", 80);
                saB_Output.Columns.Add("Interpretation", 800);

                // Add variable range results (focus on basic variables)
                foreach (var varRange in result.VariableRanges.Where(v => v.IsBasic))
                {
                    var item = new ListViewItem(varRange.VariableName);
                    item.SubItems.Add(varRange.CurrentCoefficient.ToString("F3"));
                    item.SubItems.Add(varRange.FormattedRange);
                    item.SubItems.Add("Basic");
                    item.SubItems.Add(varRange.Interpretation);
                    saB_Output.Items.Add(item);
                }

                // Add summary if no basic variables found
                if (!result.VariableRanges.Any(v => v.IsBasic))
                {
                    var summaryItem = new ListViewItem("INFO");
                    summaryItem.SubItems.Add("N/A");
                    summaryItem.SubItems.Add("N/A");
                    summaryItem.SubItems.Add("Summary");
                    summaryItem.SubItems.Add("No variables appear to be basic in current solution");
                    summaryItem.Font = new Font(saB_Output.Font, FontStyle.Italic);
                    saB_Output.Items.Add(summaryItem);
                }

                sbStatus.Text = $"Basic variable ranges calculated for {result.VariableRanges.Count(v => v.IsBasic)} variables.";
                MessageBox.Show($"Basic variable range analysis completed!\n\nAnalyzed {result.VariableRanges.Count(v => v.IsBasic)} basic variables.", 
                    "Range Analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating basic variable ranges: {ex.Message}", 
                    "Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Basic variable range analysis failed.";
            }
        }

        // Event handler for RHS constraint range analysis
        private void SaRHS_ShowRange_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before analyzing constraint ranges.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = _rangeAnalyzer.CalculateConstraintRHSRanges(_currentModel);

                // Clear previous results
                saRHS_Output.Items.Clear();
                saRHS_Output.View = View.Details;
                saRHS_Output.Columns.Clear();
                saRHS_Output.Columns.Add("Constraint", 100);
                saRHS_Output.Columns.Add("Current RHS", 120);
                saRHS_Output.Columns.Add("Allowable Range", 150);
                saRHS_Output.Columns.Add("Type", 80);
                saRHS_Output.Columns.Add("Interpretation", 800);

                // Add constraint range results
                foreach (var rhsRange in result.ConstraintRanges)
                {
                    var item = new ListViewItem(rhsRange.ConstraintName);
                    item.SubItems.Add(rhsRange.CurrentRHS.ToString("F3"));
                    item.SubItems.Add(rhsRange.FormattedRange);
                    item.SubItems.Add("RHS");
                    item.SubItems.Add(rhsRange.Interpretation);
                    saRHS_Output.Items.Add(item);
                }

                // Add summary
                var summaryItem = new ListViewItem("SUMMARY");
                summaryItem.SubItems.Add("N/A");
                summaryItem.SubItems.Add("N/A");
                summaryItem.SubItems.Add("Info");
                summaryItem.SubItems.Add($"RHS sensitivity analysis completed for {result.ConstraintRanges.Count} constraints");
                summaryItem.Font = new Font(saRHS_Output.Font, FontStyle.Bold);
                saRHS_Output.Items.Add(summaryItem);

                sbStatus.Text = $"RHS ranges calculated for {result.ConstraintRanges.Count} constraints.";
                MessageBox.Show($"RHS range analysis completed!\n\nAnalyzed {result.ConstraintRanges.Count} constraints.", 
                    "Range Analysis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating RHS ranges: {ex.Message}", 
                    "Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "RHS range analysis failed.";
            }
        }

        // Method to connect range analysis event handlers to buttons
        private void ConnectRangeAnalysisEventHandlers()
        {
            if (_rangeHandlersConnected) return; // Prevent double connection
            
            try
            {
                // Connect the range analysis button event handlers
                if (saNB_ShowRange != null) saNB_ShowRange.Click += SaNB_ShowRange_Click;
                if (saB_ShowRange != null) saB_ShowRange.Click += SaB_ShowRange_Click;
                if (saRHS_ShowRange != null) saRHS_ShowRange.Click += SaRHS_ShowRange_Click;
                
                _rangeHandlersConnected = true;
            }
            catch (Exception ex)
            {
                // Log or handle the error gracefully
                System.Diagnostics.Debug.WriteLine($"Error connecting range analysis handlers: {ex.Message}");
            }
        }
    }
}