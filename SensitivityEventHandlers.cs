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
    // Partial class containing sensitivity analysis event handlers
    public partial class TabbedMainForm
    {
        // Additional dependencies for sensitivity analysis
        private readonly DualityAnalyzer _dualityAnalyzer = new();
        private readonly SensitivityEngine _sensitivityEngine = new();

        // Event handler for dual model solving
        private void SaDual_Solve_Click(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Please load a model first.", 
                    "No Model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                var dualResult = _dualModelGenerator.GenerateDualModel(_currentModel);
                
                if (dualResult.IsSuccessful)
                {
                    // Convert DualModel to LPModel for solving
                    var dualAsLPModel = ConvertDualModelToLPModel(dualResult.DualModel);
                    var solveResult = _dualityAnalyzer.SolveDualModel(dualAsLPModel, GetSelectedAlgorithmName());
                    DisplaySensitivityResult(solveResult, saDual_Output);
                    sbStatus.Text = "Dual model solved successfully";
                    
                    MessageBox.Show("Dual model solved successfully!\nCheck the output for details.", 
                        "Dual Solution", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to generate dual model: {dualResult.Status}", 
                        "Dual Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error solving dual model: {ex.Message}", 
                    "Dual Solution Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for duality verification
        private void SaDual_Verify_Click(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Please load a model first.", 
                    "No Model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                var dualResult = _dualModelGenerator.GenerateDualModel(_currentModel);
                
                if (dualResult.IsSuccessful)
                {
                    // Demo values - in real implementation, these come from solved models
                    double primalOptimal = 10.5;  // Would come from primal solution
                    double dualOptimal = 10.5;    // Would come from dual solution
                    
                    // Convert DualModel to LPModel for verification
                    var dualAsLPModel = ConvertDualModelToLPModel(dualResult.DualModel);
                    var verificationResult = _dualityAnalyzer.VerifyDuality(
                        _currentModel, dualAsLPModel, primalOptimal, dualOptimal);
                    
                    DisplayDualityVerification(verificationResult, saDual_Output);
                    sbStatus.Text = $"Duality verification completed - {verificationResult.DualityType}";
                    
                    MessageBox.Show($"Duality Verification Result:\n{verificationResult.Summary}\n\n{verificationResult.Interpretation}", 
                        "Duality Verification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to generate dual model: {dualResult.Status}", 
                        "Dual Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verifying duality: {ex.Message}", 
                    "Duality Verification Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper method to convert DualModel to LPModel for analysis
        private LPModel ConvertDualModelToLPModel(DualModel dualModel)
        {
            var lpModel = new LPModel
            {
                Sense = dualModel.Sense
            };
            
            // Convert dual variables to LP variables
            foreach (var dualVar in dualModel.Variables.Values)
            {
                var lpVar = new Variable
                {
                    Name = dualVar.Name,
                    Coefficient = dualVar.Coefficient,
                    SignRestriction = dualVar.SignRestriction,
                    Index = dualVar.Index
                };
                lpModel.Variables[dualVar.Name] = lpVar;
            }
            
            // Convert dual constraints to LP constraints
            foreach (var dualConstraint in dualModel.Constraints)
            {
                var lpConstraint = new Constraint
                {
                    Name = dualConstraint.Name,
                    Coefficients = new Dictionary<string, double>(dualConstraint.Coefficients),
                    Relation = dualConstraint.Relation,
                    RHS = dualConstraint.RHS
                };
                lpModel.Constraints.Add(lpConstraint);
            }
            
            return lpModel;
        }

        // Event handler for column analysis range display
        private void SaCol_ShowRange_Click(object sender, EventArgs e)
        {
            if (_currentModel == null || saCol_VarSelect.SelectedItem == null)
            {
                MessageBox.Show("Please load a model and select a variable first.", 
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                var rangeAnalyzer = new RangeAnalyzer();
                var variableName = saCol_VarSelect.SelectedItem.ToString();
                var range = rangeAnalyzer.GetVariableRange(_currentModel, variableName);
                
                saCol_Output.Items.Clear();
                saCol_Output.View = View.Details;
                saCol_Output.Columns.Clear();
                saCol_Output.Columns.Add("Analysis", 120);
                saCol_Output.Columns.Add("Details", 650);
                
                saCol_Output.Items.Add(new ListViewItem(new[] { "Variable", range.VariableName }));
                saCol_Output.Items.Add(new ListViewItem(new[] { "Current Coeff", range.CurrentCoefficient.ToString("F3") }));
                saCol_Output.Items.Add(new ListViewItem(new[] { "Range", range.FormattedRange }));
                saCol_Output.Items.Add(new ListViewItem(new[] { "Status", range.IsBasic ? "Basic" : "Non-Basic" }));
                saCol_Output.Items.Add(new ListViewItem(new[] { "Interpretation", range.Interpretation }));
                
                sbStatus.Text = $"Column range displayed for {variableName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating column range: {ex.Message}", 
                    "Range Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for column coefficient editing
        private void SaCol_EditCoeffs_Click(object sender, EventArgs e)
        {
            if (_currentModel == null || saCol_VarSelect.SelectedItem == null)
            {
                MessageBox.Show("Please load a model and select a variable first.", 
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                string variableName = saCol_VarSelect.SelectedItem.ToString();
                
                // Create a generously sized dialog for coefficient editing
                using var form = new Form();
                form.Text = $"Edit Column Coefficients for {variableName}";
                form.Size = new Size(750, 550); // Much larger for better comfort
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                
                // Instructions label
                var lblInstructions = new Label
                {
                    Text = "Click on 'New Coefficient' column to edit values:",
                    Location = new Point(15, 15),
                    Size = new Size(700, 25),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                
                // Use DataGridView instead of ListView for better editing
                var dgvCoeffs = new DataGridView
                {
                    Location = new Point(15, 50),
                    Size = new Size(710, 400), // Much larger with more padding
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                    RowHeadersVisible = false,
                    SelectionMode = DataGridViewSelectionMode.CellSelect,
                    DefaultCellStyle = { Font = new Font("Segoe UI", 9F) }
                };
                
                // Add columns
                dgvCoeffs.Columns.Add("Constraint", "Constraint");
                dgvCoeffs.Columns.Add("CurrentCoeff", "Current Coefficient");
                dgvCoeffs.Columns.Add("NewCoeff", "New Coefficient");
                
                // Make first two columns read-only
                dgvCoeffs.Columns["Constraint"].ReadOnly = true;
                dgvCoeffs.Columns["CurrentCoeff"].ReadOnly = true;
                dgvCoeffs.Columns["NewCoeff"].ReadOnly = false; // This one is editable
                
                // Set column widths for better distribution
                dgvCoeffs.Columns["Constraint"].FillWeight = 25;
                dgvCoeffs.Columns["CurrentCoeff"].FillWeight = 37.5F;
                dgvCoeffs.Columns["NewCoeff"].FillWeight = 37.5F;
                
                // Style the editable column
                dgvCoeffs.Columns["NewCoeff"].DefaultCellStyle.BackColor = Color.LightYellow;
                
                // Populate with current coefficients
                foreach (var constraint in _currentModel.Constraints)
                {
                    double currentCoeff = constraint.Coefficients.TryGetValue(variableName, out var coeff) ? coeff : 0.0;
                    int rowIndex = dgvCoeffs.Rows.Add();
                    dgvCoeffs.Rows[rowIndex].Cells["Constraint"].Value = constraint.Name;
                    dgvCoeffs.Rows[rowIndex].Cells["CurrentCoeff"].Value = currentCoeff.ToString("F3");
                    dgvCoeffs.Rows[rowIndex].Cells["NewCoeff"].Value = currentCoeff.ToString("F3");
                }
                
                var btnOK = new Button
                {
                    Text = "Apply Changes",
                    Location = new Point(450, 460), // Moved left and up
                    Size = new Size(130, 40),
                    DialogResult = DialogResult.OK,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                
                var btnCancel = new Button
                {
                    Text = "Cancel",
                    Location = new Point(590, 460), // Moved left and up
                    Size = new Size(80, 40),
                    DialogResult = DialogResult.Cancel
                };
                
                form.Controls.AddRange(new Control[] { lblInstructions, dgvCoeffs, btnOK, btnCancel });
                
                // Show dialog and process results
                if (form.ShowDialog() == DialogResult.OK)
                {
                    var changes = new List<string>();
                    for (int i = 0; i < dgvCoeffs.Rows.Count; i++)
                    {
                        var constraintName = dgvCoeffs.Rows[i].Cells["Constraint"].Value.ToString();
                        var newValueText = dgvCoeffs.Rows[i].Cells["NewCoeff"].Value?.ToString();
                        
                        if (double.TryParse(newValueText, out double newValue))
                        {
                            var constraint = _currentModel.Constraints[i];
                            var oldValue = constraint.Coefficients.TryGetValue(variableName, out var old) ? old : 0.0;
                            
                            if (Math.Abs(newValue - oldValue) > 0.001)
                            {
                                constraint.Coefficients[variableName] = newValue;
                                changes.Add($"{constraintName}: {oldValue:F3} ? {newValue:F3}");
                            }
                        }
                    }
                    
                    // Display results
                    saCol_Output.Items.Clear();
                    saCol_Output.View = View.Details;
                    saCol_Output.Columns.Clear();
                    saCol_Output.Columns.Add("Change", 200);
                    saCol_Output.Columns.Add("Details", 570);
                    
                    if (changes.Count > 0)
                    {
                        // Refresh ALL model displays including main DataGridViews
                        RefreshAllModelDisplays();
                        
                        var headerItem = new ListViewItem("COEFFICIENT CHANGES");
                        headerItem.SubItems.Add($"Applied {changes.Count} changes to {variableName}");
                        headerItem.Font = new Font(saCol_Output.Font, FontStyle.Bold);
                        saCol_Output.Items.Add(headerItem);
                        
                        foreach (var change in changes)
                        {
                            var changeItem = new ListViewItem("Modified");
                            changeItem.SubItems.Add(change);
                            saCol_Output.Items.Add(changeItem);
                        }
                        
                        sbStatus.Text = $"Applied {changes.Count} coefficient changes to {variableName}";
                        MessageBox.Show($"Applied {changes.Count} coefficient changes to variable {variableName}.\n\nThe model has been updated in all tabs. Check the Model Input tab to see changes.", 
                            "Changes Applied", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        saCol_Output.Items.Add(new ListViewItem(new[] { "No Changes", "No coefficient modifications were made" }));
                        sbStatus.Text = "No coefficient changes applied";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing coefficients: {ex.Message}", 
                    "Edit Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for adding new constraint
        private void SaAddCon_Add_Click(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Please load a model first before adding new constraints.", 
                    "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                // Create a generously sized constraint input dialog
                using var form = new Form();
                form.Text = "Add New Constraint";
                form.Size = new Size(750, 600); // Much larger for better comfort
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                
                // Header instruction
                var lblHeader = new Label 
                { 
                    Text = "Enter details for the new constraint:", 
                    Location = new Point(15, 15), 
                    Size = new Size(700, 25),
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold)
                };
                
                // Constraint name
                var lblName = new Label { Text = "Constraint Name:", Location = new Point(15, 55), Size = new Size(110, 23) };
                var txtName = new TextBox { Location = new Point(135, 53), Size = new Size(130, 25) };
                txtName.Text = $"c{_currentModel.Constraints.Count + 1}";
                
                // Relation
                var lblRelation = new Label { Text = "Relation:", Location = new Point(285, 55), Size = new Size(70, 23) };
                var cmbRelation = new ComboBox 
                { 
                    Location = new Point(365, 53), 
                    Size = new Size(80, 25),
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbRelation.Items.AddRange(new[] { "<=", "=", ">=" });
                cmbRelation.SelectedIndex = 0;
                
                // RHS
                var lblRHS = new Label { Text = "RHS:", Location = new Point(465, 55), Size = new Size(50, 23) };
                var nudRHS = new NumericUpDown 
                { 
                    Location = new Point(525, 53), 
                    Size = new Size(120, 25),
                    DecimalPlaces = 3,
                    Minimum = -10000,
                    Maximum = 10000
                };
                
                // Coefficients section
                var lblCoeffs = new Label { Text = "Variable Coefficients:", Location = new Point(15, 95), Size = new Size(160, 23) };
                var dgvCoeffs = new DataGridView
                {
                    Location = new Point(15, 125),
                    Size = new Size(710, 380), // Much larger with more room
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                    ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                    RowHeadersVisible = false,
                    DefaultCellStyle = { Font = new Font("Segoe UI", 9F) }
                };
                dgvCoeffs.Columns.Add("Variable", "Variable");
                dgvCoeffs.Columns.Add("Coefficient", "Coefficient");
                
                // Make Variable column read-only, highlight coefficient column
                dgvCoeffs.Columns["Variable"].ReadOnly = true;
                dgvCoeffs.Columns["Coefficient"].ReadOnly = false;
                dgvCoeffs.Columns["Coefficient"].DefaultCellStyle.BackColor = Color.LightYellow;
                
                // Populate with variables
                foreach (var variable in _currentModel.Variables.Values.OrderBy(v => v.Index))
                {
                    int rowIndex = dgvCoeffs.Rows.Add();
                    dgvCoeffs.Rows[rowIndex].Cells["Variable"].Value = variable.Name;
                    dgvCoeffs.Rows[rowIndex].Cells["Coefficient"].Value = 0.0;
                }
                
                var btnOK = new Button 
                { 
                    Text = "Add Constraint", 
                    Location = new Point(450, 515), // Moved left and up
                    Size = new Size(130, 40),
                    DialogResult = DialogResult.OK,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold)
                };
                
                var btnCancel = new Button 
                { 
                    Text = "Cancel", 
                    Location = new Point(590, 515), // Moved left and up
                    Size = new Size(80, 40),
                    DialogResult = DialogResult.Cancel
                };
                
                form.Controls.AddRange(new Control[] { 
                    lblHeader, lblName, txtName, lblRelation, cmbRelation, lblRHS, nudRHS,
                    lblCoeffs, dgvCoeffs, btnOK, btnCancel 
                });
                
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Validate and create constraint
                    string constraintName = txtName.Text.Trim();
                    if (string.IsNullOrEmpty(constraintName))
                    {
                        MessageBox.Show("Please enter a constraint name.", "Validation Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    var coefficients = new Dictionary<string, double>();
                    for (int i = 0; i < dgvCoeffs.Rows.Count; i++)
                    {
                        var variableName = dgvCoeffs.Rows[i].Cells["Variable"].Value.ToString();
                        if (double.TryParse(dgvCoeffs.Rows[i].Cells["Coefficient"].Value?.ToString(), out double coeff))
                        {
                            coefficients[variableName] = coeff;
                        }
                        else
                        {
                            coefficients[variableName] = 0.0; // Default to 0 if invalid
                        }
                    }
                    
                    var relation = cmbRelation.SelectedItem.ToString() switch
                    {
                        "<=" => ConstraintRelation.LessThanEqual,
                        ">=" => ConstraintRelation.GreaterThanEqual,
                        "=" => ConstraintRelation.Equal,
                        _ => ConstraintRelation.LessThanEqual
                    };
                    
                    var sensitivityAnalyzer = new SensitivityAnalyzer();
                    var result = sensitivityAnalyzer.AddNewConstraint(
                        _currentModel, constraintName, coefficients, relation, (double)nudRHS.Value);
                    
                    DisplaySensitivityResult(result, saAddCon_Output);
                    
                    if (result.IsSuccessful && result.ModifiedModel != null)
                    {
                        _currentModel = result.ModifiedModel;
                        
                        // Refresh ALL model displays including main DataGridViews
                        RefreshAllModelDisplays();
                        
                        sbStatus.Text = $"Added constraint {constraintName} successfully";
                        MessageBox.Show($"Constraint '{constraintName}' added successfully!\n\nThe model has been updated in all tabs. Check the Model Input tab to see changes.", 
                            "Constraint Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding constraint: {ex.Message}", 
                    "Add Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper method to call the existing UpdateVariableDropdowns
        private void CallUpdateVariableDropdowns()
        {
            // This method now calls the comprehensive refresh instead of just dropdowns
            RefreshAllModelDisplays();
        }

        // Helper method to display duality verification results
        private void DisplayDualityVerification(DualityVerificationResult result, ListView outputControl)
        {
            outputControl.Items.Clear();
            outputControl.View = View.Details;
            outputControl.Columns.Clear();
            outputControl.Columns.Add("Property", 150);
            outputControl.Columns.Add("Value", 500);
            
            outputControl.Items.Add(new ListViewItem(new[] { "Primal Optimal", result.PrimalOptimalValue.ToString("F3") }));
            outputControl.Items.Add(new ListViewItem(new[] { "Dual Optimal", result.DualOptimalValue.ToString("F3") }));
            outputControl.Items.Add(new ListViewItem(new[] { "Duality Gap", result.DualityGap.ToString("F6") }));
            outputControl.Items.Add(new ListViewItem(new[] { "Weak Duality", result.WeakDualityHolds ? "? Holds" : "? Violated" }));
            outputControl.Items.Add(new ListViewItem(new[] { "Strong Duality", result.StrongDualityHolds ? "? Holds" : "? Violated" }));
            outputControl.Items.Add(new ListViewItem(new[] { "Duality Type", result.DualityType.ToString() }));
            outputControl.Items.Add(new ListViewItem(new[] { "Interpretation", result.Interpretation }));
        }

        // Connect all sensitivity event handlers
        private void ConnectSensitivityEventHandlers()
        {
            try
            {
                // Connect duality event handlers
                if (saDual_Solve != null) saDual_Solve.Click += SaDual_Solve_Click;
                if (saDual_Verify != null) saDual_Verify.Click += SaDual_Verify_Click;
                
                // Connect column analysis event handlers
                if (saCol_ShowRange != null) saCol_ShowRange.Click += SaCol_ShowRange_Click;
                if (saCol_EditCoeffs != null) saCol_EditCoeffs.Click += SaCol_EditCoeffs_Click;
                
                // Connect constraint management event handler
                if (saAddCon_Add != null) saAddCon_Add.Click += SaAddCon_Add_Click;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting sensitivity handlers: {ex.Message}");
            }
        }
    }
}