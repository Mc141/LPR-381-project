using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LPR381_Assignment.Models;
using LPR381_Assignment.Services.Analysis;
using LPR381_Assignment.UI.Dialogs;

namespace LPR381_Assignment
{
    // Partial class containing range analysis event handlers
    public partial class TabbedMainForm
    {
        // Range analyzer instance
        private readonly RangeAnalyzer _rangeAnalyzer = new();
        private readonly SensitivityAnalyzer _sensitivityAnalyzer = new();
        private bool _rangeHandlersConnected = false;

        // Override the Load event to connect event handlers after initialization
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ConnectRangeAnalysisEventHandlers();
            ConnectSensitivityEventHandlers(); // Add this missing call
            ConnectEnhancedSolveHandler(); // Connect the enhanced solve functionality
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
                saNB_Output.Columns.Add("Interpretation", 600);

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
                saB_Output.Columns.Add("Interpretation", 600);

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
                saRHS_Output.Columns.Add("Interpretation", 600);

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

        // NEW: Event handler for adding new activity
        private void SaAddAct_Add_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before adding new activities.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Show the Add Activity dialog
                using var dialog = new AddActivityDialog(_currentModel);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Perform the sensitivity analysis
                    var result = _sensitivityAnalyzer.AddNewActivity(
                        _currentModel,
                        dialog.ActivityName,
                        dialog.ObjectiveCoefficient,
                        dialog.ConstraintCoefficients,
                        dialog.SignRestriction);

                    // Display results
                    DisplaySensitivityResult(result, saAddAct_Output);

                    // Update status
                    if (result.IsSuccessful)
                    {
                        sbStatus.Text = $"Successfully added activity '{dialog.ActivityName}' to model.";
                        
                        // Update the current model with the modified version
                        if (result.ModifiedModel != null)
                        {
                            _currentModel = result.ModifiedModel;
                            
                            // Refresh ALL model displays including main DataGridViews
                            RefreshAllModelDisplays();
                            
                            // Show success message with option to see details
                            var detailsMsg = $"Activity '{dialog.ActivityName}' added successfully!\n\n" +
                                           $"Objective coefficient: {dialog.ObjectiveCoefficient:F3}\n" +
                                           $"Constraint coefficients: [{string.Join(", ", dialog.ConstraintCoefficients.Select(c => c.ToString("F3")))}]\n\n" +
                                           "The model has been updated in all tabs. Check the Model Input tab to see changes.";
                            

                            MessageBox.Show(detailsMsg, "Activity Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        sbStatus.Text = $"Failed to add activity: {result.Status}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding new activity: {ex.Message}", 
                    "Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Add activity operation failed.";
            }
        }

        // NEW: Event handler for applying delta to non-basic variable coefficient
        private void SaNB_Apply_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before applying delta changes.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get selected variable
                if (saNB_VarSelect.SelectedItem == null)
                {
                    MessageBox.Show("Please select a variable before applying delta changes.", 
                        "No Variable Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string variableName = saNB_VarSelect.SelectedItem.ToString();
                double delta = (double)saNB_ApplyDelta.Value;

                // Apply delta to objective coefficient
                var result = _sensitivityAnalyzer.ApplyDeltaToObjectiveCoefficient(_currentModel, variableName, delta);

                // Display results
                DisplaySensitivityResult(result, saNB_Output);

                // Update status and model if successful
                if (result.IsSuccessful)
                {
                    sbStatus.Text = $"Applied delta {delta:F3} to {variableName}";
                    
                    if (result.ModifiedModel != null)
                    {
                        _currentModel = result.ModifiedModel;
                        
                        // Refresh ALL model displays including main DataGridViews
                        RefreshAllModelDisplays();
                        
                        MessageBox.Show($"Delta applied successfully!\n\n" +
                                      $"Variable: {variableName}\n" +
                                      $"Delta: {delta:F3}\n" +
                                      $"New coefficient: {_currentModel.Variables[variableName].Coefficient:F3}\n\n" +
                                      "The model has been updated in all tabs. Check the Model Input tab to see changes.", 
                                      "Delta Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    sbStatus.Text = $"Failed to apply delta: {result.Status}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying delta: {ex.Message}", 
                    "Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Delta application failed.";
            }
        }

        // NEW: Event handler for applying delta to basic variable coefficient
        private void SaB_Apply_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before applying delta changes.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get selected variable
                if (saB_VarSelect.SelectedItem == null)
                {
                    MessageBox.Show("Please select a variable before applying delta changes.", 
                        "No Variable Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string variableName = saB_VarSelect.SelectedItem.ToString();
                double delta = (double)saB_ApplyDelta.Value;

                // Apply delta to objective coefficient
                var result = _sensitivityAnalyzer.ApplyDeltaToObjectiveCoefficient(_currentModel, variableName, delta);

                // Display results
                DisplaySensitivityResult(result, saB_Output);

                // Update status and model if successful
                if (result.IsSuccessful)
                {
                    sbStatus.Text = $"Applied delta {delta:F3} to {variableName}";
                    
                    if (result.ModifiedModel != null)
                    {
                        _currentModel = result.ModifiedModel;
                        
                        // Refresh ALL model displays including main DataGridViews
                        RefreshAllModelDisplays();
                        
                        MessageBox.Show($"Delta applied successfully!\n\n" +
                                      $"Variable: {variableName}\n" +
                                      $"Delta: {delta:F3}\n" +
                                      $"New coefficient: {_currentModel.Variables[variableName].Coefficient:F3}\n\n" +
                                      "The model has been updated in all tabs. Check the Model Input tab to see changes.", 
                                      "Delta Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    sbStatus.Text = $"Failed to apply delta: {result.Status}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying delta: {ex.Message}", 
                    "Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Delta application failed.";
            }
        }

        // NEW: Event handler for applying delta to constraint RHS
        private void SaRHS_Apply_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before applying delta changes.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get selected constraint
                if (saRHS_ConSelect.SelectedItem == null)
                {
                    MessageBox.Show("Please select a constraint before applying delta changes.", 
                        "No Constraint Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string constraintName = saRHS_ConSelect.SelectedItem.ToString();
                double delta = (double)saRHS_ApplyDelta.Value;

                // Apply delta to constraint RHS
                var result = _sensitivityAnalyzer.ApplyDeltaToConstraintRHS(_currentModel, constraintName, delta);

                // Display results
                DisplaySensitivityResult(result, saRHS_Output);

                // Update status and model if successful
                if (result.IsSuccessful)
                {
                    sbStatus.Text = $"Applied delta {delta:F3} to {constraintName} RHS";
                    
                    if (result.ModifiedModel != null)
                    {
                        _currentModel = result.ModifiedModel;
                        
                        // Refresh ALL model displays including main DataGridViews
                        RefreshAllModelDisplays();
                        
                        var constraint = _currentModel.Constraints.FirstOrDefault(c => c.Name == constraintName);
                        MessageBox.Show($"Delta applied successfully!\n\n" +
                                      $"Constraint: {constraintName}\n" +
                                      $"Delta: {delta:F3}\n" +
                                      $"New RHS: {constraint?.RHS:F3}\n\n" +
                                      "The model has been updated in all tabs. Check the Model Input tab to see changes.", 
                                      "Delta Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    sbStatus.Text = $"Failed to apply delta: {result.Status}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying delta: {ex.Message}", 
                    "Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Delta application failed.";
            }
        }

        // Helper method to display sensitivity analysis results
        private void DisplaySensitivityResult(Models.SensitivityResult result, ListView outputControl)
        {
            // Clear previous results
            outputControl.Items.Clear();
            outputControl.View = View.Details;
            outputControl.Columns.Clear();
            outputControl.Columns.Add("Type", 100);
            outputControl.Columns.Add("Details", 700);

            // Add main result
            var statusItem = new ListViewItem(result.IsSuccessful ? "SUCCESS" : "ERROR");
            statusItem.SubItems.Add(result.Status);
            statusItem.BackColor = result.IsSuccessful ? Color.LightGreen : Color.LightPink;
            statusItem.Font = new Font(outputControl.Font, FontStyle.Bold);
            outputControl.Items.Add(statusItem);

            // Add details if available
            if (!string.IsNullOrEmpty(result.Details))
            {
                var detailsItem = new ListViewItem("Details");
                detailsItem.SubItems.Add(result.Details);
                outputControl.Items.Add(detailsItem);
            }

            // Add changes
            foreach (var change in result.Changes)
            {
                var changeItem = new ListViewItem($"Change");
                changeItem.SubItems.Add($"{change.Key}: {change.Value}");
                outputControl.Items.Add(changeItem);
            }

            // Add warnings
            foreach (var warning in result.Warnings)
            {
                var warningItem = new ListViewItem("Warning");
                warningItem.SubItems.Add(warning);
                warningItem.ForeColor = Color.Orange;
                outputControl.Items.Add(warningItem);
            }

            // Add recommendations
            foreach (var recommendation in result.Recommendations)
            {
                var recItem = new ListViewItem("Recommend");
                recItem.SubItems.Add(recommendation);
                recItem.ForeColor = Color.Blue;
                outputControl.Items.Add(recItem);
            }

            // Auto-resize columns
            foreach (ColumnHeader column in outputControl.Columns)
            {
                column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
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
                
                // Connect the delta application button event handlers
                if (saNB_Apply != null) saNB_Apply.Click += SaNB_Apply_Click;
                if (saB_Apply != null) saB_Apply.Click += SaB_Apply_Click;
                if (saRHS_Apply != null) saRHS_Apply.Click += SaRHS_Apply_Click;
                
                // Connect the new Add Activity button event handler
                if (saAddAct_Add != null) saAddAct_Add.Click += SaAddAct_Add_Click;
                
                _rangeHandlersConnected = true;
            }
            catch (Exception ex)
            {
                // Log or handle the error gracefully
                System.Diagnostics.Debug.WriteLine($"Error connecting range analysis handlers: {ex.Message}");
            }
        }

        // Method to update variable dropdown lists based on basic/non-basic classification
        private void UpdateVariableDropdowns()
        {
            if (_currentModel == null) return;

            try
            {
                // Get variable classifications
                var result = _rangeAnalyzer.CalculateVariableRanges(_currentModel, basicVariablesOnly: false);

                // Update Non-Basic Variable dropdown
                saNB_VarSelect.Items.Clear();
                var nonBasicVariables = result.VariableRanges.Where(v => !v.IsBasic).Select(v => v.VariableName).ToArray();
                if (nonBasicVariables.Length > 0)
                {
                    saNB_VarSelect.Items.AddRange(nonBasicVariables);
                }
                else
                {
                    saNB_VarSelect.Items.Add("(No non-basic variables)");
                }

                // Update Basic Variable dropdown
                saB_VarSelect.Items.Clear();
                var basicVariables = result.VariableRanges.Where(v => v.IsBasic).Select(v => v.VariableName).ToArray();
                if (basicVariables.Length > 0)
                {
                    saB_VarSelect.Items.AddRange(basicVariables);
                }
                else
                {
                    saB_VarSelect.Items.Add("(No basic variables)");
                }

                // Update Constraint dropdown
                saRHS_ConSelect.Items.Clear();
                var constraints = _currentModel.Constraints.Select(c => c.Name).ToArray();
                if (constraints.Length > 0)
                {
                    saRHS_ConSelect.Items.AddRange(constraints);
                }

                // Also update the column analysis dropdown
                if (saCol_VarSelect != null)
                {
                    saCol_VarSelect.Items.Clear();
                    var allVariables = _currentModel.Variables.Keys.ToArray();
                    if (allVariables.Length > 0)
                    {
                        saCol_VarSelect.Items.AddRange(allVariables);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating variable dropdowns: {ex.Message}");
            }
        }

        /// <summary>
        /// Master refresh method that updates ALL UI components when the model changes.
        /// This includes both the main Model Input tab DataGridViews and sensitivity analysis dropdowns.
        /// </summary>
        private void RefreshAllModelDisplays()
        {
            if (_currentModel == null) return;

            try
            {
                // Update the main Model Input tab DataGridViews
                RefreshMainModelDisplays();

                // Update sensitivity analysis dropdowns
                UpdateVariableDropdowns();

                // Update status to reflect the model change
                sbStatus.Text = "Model updated - all displays refreshed.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing model displays: {ex.Message}");
                MessageBox.Show($"Error refreshing model display: {ex.Message}", 
                    "Display Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Refreshes the main Model Input tab DataGridViews with current model data.
        /// This updates the objective function, constraints, and sign restrictions displays.
        /// </summary>
        private void RefreshMainModelDisplays()
        {
            if (_currentModel == null) return;

            // Update the objective sense dropdown (maximize or minimize)
            cmbObjectiveSense.SelectedIndex = _currentModel.Sense == ObjectiveSense.Maximize ? 0 : 1;

            // Clear out any existing data to start fresh
            dgvObjective.Rows.Clear();
            dgvConstraints.Rows.Clear();
            dgvSignRestrictions.Rows.Clear();

            // Fill in the objective function coefficients
            foreach (var variable in _currentModel.Variables.Values.OrderBy(v => v.Index))
            {
                dgvObjective.Rows.Add(
                    variable.Name,
                    variable.Coefficient >= 0 ? "+" : "-",
                    Math.Abs(variable.Coefficient)
                );
            }

            // Fill in all the constraint information
            foreach (var constraint in _currentModel.Constraints)
            {
                var signs = new List<string>();
                var coeffs = new List<string>();
                
                // For each variable, figure out its coefficient in this constraint
                foreach (var variable in _currentModel.Variables.Values.OrderBy(v => v.Index))
                {
                    double coeff = constraint.Coefficients.TryGetValue(variable.Name, out var c) ? c : 0;
                    signs.Add(coeff >= 0 ? "+" : "-");
                    coeffs.Add(Math.Abs(coeff).ToString());
                }
                
                // Convert the constraint relation to display format
                string relation = constraint.Relation switch
                {
                    ConstraintRelation.LessThanEqual => "<=",
                    ConstraintRelation.Equal => "=",
                    ConstraintRelation.GreaterThanEqual => ">=",
                    _ => "?"
                };

                dgvConstraints.Rows.Add(
                    constraint.Name,
                    string.Join(" ", signs),
                    string.Join(" ", coeffs),
                    relation,
                    constraint.RHS
                );
            }

            // Fill in the sign restrictions for each variable
            foreach (var variable in _currentModel.Variables.Values.OrderBy(v => v.Index))
            {
                string signStr = variable.SignRestriction switch
                {
                    SignRestriction.Positive => "+",
                    SignRestriction.Negative => "-",
                    SignRestriction.Unrestricted => "urs",
                    SignRestriction.Integer => "int",
                    SignRestriction.Binary => "bin",
                    _ => "?"
                };

                dgvSignRestrictions.Rows.Add(variable.Name, signStr);
            }

            // Figure out the problem type based on variable characteristics
            cmbProblemType.SelectedIndex = _currentModel.Variables.Values
                .Any(v => v.SignRestriction is SignRestriction.Integer or SignRestriction.Binary) ? 1 : 0;

            // Ensure the buttons remain enabled
            btnValidateInput.Enabled = true;
            btnSolve.Enabled = true;
        }
    }
}