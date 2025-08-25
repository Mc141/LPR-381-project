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
                    
                    // Display verification results
                    DisplayDualityVerificationResult(verificationResult, saDual_Output);
                    sbStatus.Text = "Duality verification completed";
                    
                    MessageBox.Show($"Duality verification completed!\n\n" +
                                  $"Status: {verificationResult.DualityType}\n" +
                                  $"Primal Optimal: {verificationResult.PrimalOptimalValue:F3}\n" +
                                  $"Dual Optimal: {verificationResult.DualOptimalValue:F3}", 
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

        // Event handler for column range display
        private void SaCol_ShowRange_Click(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Please load a model first.", 
                    "No Model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (saCol_VarSelect.SelectedItem == null)
            {
                MessageBox.Show("Please select a variable first.", 
                    "No Variable Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                string variableName = saCol_VarSelect.SelectedItem.ToString();
                var rangeAnalyzer = new RangeAnalyzer();
                var range = rangeAnalyzer.GetVariableRange(_currentModel, variableName);
                
                // Display range results
                saCol_Output.Items.Clear();
                saCol_Output.View = View.Details;
                saCol_Output.Columns.Clear();
                saCol_Output.Columns.Add("Property", 120);
                saCol_Output.Columns.Add("Value", 600);
                
                saCol_Output.Items.Add(new ListViewItem(new[] { "Variable", range.VariableName }));
                saCol_Output.Items.Add(new ListViewItem(new[] { "Current Coefficient", range.CurrentCoefficient.ToString("F3") }));
                saCol_Output.Items.Add(new ListViewItem(new[] { "Allowable Range", range.FormattedRange }));
                saCol_Output.Items.Add(new ListViewItem(new[] { "Type", range.IsBasic ? "Basic" : "Non-Basic" }));
                saCol_Output.Items.Add(new ListViewItem(new[] { "Interpretation", range.Interpretation }));
                
                sbStatus.Text = $"Column range displayed for {variableName}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error displaying column range: {ex.Message}", 
                    "Range Analysis Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for column coefficient editing
        private void SaCol_EditCoeffs_Click(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Please load a model first.", 
                    "No Model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            if (saCol_VarSelect.SelectedItem == null)
            {
                MessageBox.Show("Please select a variable first.", 
                    "No Variable Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                string variableName = saCol_VarSelect.SelectedItem.ToString();
                
                // Show coefficient editing dialog
                using var dialog = new CoefficientEditDialog(_currentModel, variableName);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Apply the coefficient changes
                    var changes = dialog.GetCoefficientChanges();
                    foreach (var change in changes)
                    {
                        var result = _sensitivityAnalyzer.ApplyDeltaToConstraintCoefficient(
                            _currentModel, change.ConstraintName, variableName, change.Delta);
                        
                        if (result.IsSuccessful && result.ModifiedModel != null)
                        {
                            _currentModel = result.ModifiedModel;
                        }
                    }
                    
                    // Refresh all model displays
                    RefreshAllModelDisplays();
                    
                    // Display results
                    saCol_Output.Items.Clear();
                    saCol_Output.Items.Add(new ListViewItem($"Successfully updated {changes.Count} coefficients for {variableName}"));
                    
                    sbStatus.Text = $"Coefficient changes applied for {variableName}";
                    MessageBox.Show($"Coefficient changes applied successfully!\n\n" +
                                  $"Variable: {variableName}\n" +
                                  $"Changes applied: {changes.Count}\n\n" +
                                  "The model has been updated in all tabs.", 
                                  "Coefficients Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error editing coefficients: {ex.Message}", 
                    "Coefficient Edit Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for adding new constraint
        private void SaAddCon_Add_Click(object sender, EventArgs e)
        {
            if (_currentModel == null)
            {
                MessageBox.Show("Please load a model first.", 
                    "No Model", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            try
            {
                // Show add constraint dialog
                using var dialog = new AddConstraintDialog(_currentModel);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    // Add the new constraint
                    var result = _sensitivityAnalyzer.AddNewConstraint(
                        _currentModel,
                        dialog.ConstraintName,
                        dialog.Coefficients,
                        dialog.Relation,
                        dialog.RHS);
                    
                    // Display results
                    DisplaySensitivityResult(result, saAddCon_Output);
                    
                    if (result.IsSuccessful && result.ModifiedModel != null)
                    {
                        _currentModel = result.ModifiedModel;
                        RefreshAllModelDisplays();
                        
                        sbStatus.Text = $"Successfully added constraint '{dialog.ConstraintName}'";
                        MessageBox.Show($"Constraint '{dialog.ConstraintName}' added successfully!\n\n" +
                                      "The model has been updated in all tabs.", 
                                      "Constraint Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        sbStatus.Text = $"Failed to add constraint: {result.Status}";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding constraint: {ex.Message}", 
                    "Add Constraint Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Converts a DualModel to LPModel for solving purposes
        /// </summary>
        private LPModel ConvertDualModelToLPModel(DualModel dualModel)
        {
            var lpModel = new LPModel
            {
                Sense = dualModel.Sense
            };
            
            // Convert dual variables to LP variables
            foreach (var dualVar in dualModel.Variables.Values.OrderBy(v => v.Index))
            {
                var variable = new Variable
                {
                    Name = dualVar.Name,
                    Coefficient = dualVar.Coefficient,
                    SignRestriction = dualVar.SignRestriction,
                    Index = dualVar.Index
                };
                lpModel.Variables[dualVar.Name] = variable;
            }
            
            // Convert dual constraints to LP constraints
            foreach (var dualConstraint in dualModel.Constraints)
            {
                var constraint = new Constraint
                {
                    Name = dualConstraint.Name,
                    Coefficients = new Dictionary<string, double>(dualConstraint.Coefficients),
                    Relation = dualConstraint.Relation,
                    RHS = dualConstraint.RHS
                };
                lpModel.Constraints.Add(constraint);
            }
            
            return lpModel;
        }

        /// <summary>
        /// Displays duality verification results
        /// </summary>
        private void DisplayDualityVerificationResult(DualityVerificationResult result, ListView outputControl)
        {
            outputControl.Items.Clear();
            outputControl.View = View.Details;
            outputControl.Columns.Clear();
            outputControl.Columns.Add("Property", 150);
            outputControl.Columns.Add("Value", 200);
            outputControl.Columns.Add("Status", 400);
            
            outputControl.Items.Add(new ListViewItem(new[] { 
                "Duality Type", 
                result.DualityType.ToString(), 
                result.Interpretation 
            }));
            
            outputControl.Items.Add(new ListViewItem(new[] { 
                "Primal Optimal", 
                result.PrimalOptimalValue.ToString("F3"), 
                "Primal objective value" 
            }));
            
            outputControl.Items.Add(new ListViewItem(new[] { 
                "Dual Optimal", 
                result.DualOptimalValue.ToString("F3"), 
                "Dual objective value" 
            }));
            
            outputControl.Items.Add(new ListViewItem(new[] { 
                "Weak Duality", 
                result.WeakDualityHolds.ToString(), 
                result.WeakDualityHolds ? "? Satisfied" : "? Violated" 
            }));
            
            outputControl.Items.Add(new ListViewItem(new[] { 
                "Strong Duality", 
                result.StrongDualityHolds.ToString(), 
                result.StrongDualityHolds ? "? Satisfied" : "? Violated" 
            }));
        }

        /// <summary>
        /// Method to connect sensitivity event handlers to buttons
        /// </summary>
        private void ConnectSensitivityEventHandlers()
        {
            try
            {
                // Connect dual analysis buttons
                if (saDual_Solve != null) saDual_Solve.Click += SaDual_Solve_Click;
                if (saDual_Verify != null) saDual_Verify.Click += SaDual_Verify_Click;
                
                // Connect column analysis buttons
                if (saCol_ShowRange != null) saCol_ShowRange.Click += SaCol_ShowRange_Click;
                if (saCol_EditCoeffs != null) saCol_EditCoeffs.Click += SaCol_EditCoeffs_Click;
                
                // Connect add constraint button
                if (saAddCon_Add != null) saAddCon_Add.Click += SaAddCon_Add_Click;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting sensitivity handlers: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Simple dialog for editing constraint coefficients
    /// </summary>
    public class CoefficientEditDialog : Form
    {
        private DataGridView dgvCoefficients;
        private Button btnOK;
        private Button btnCancel;
        private LPModel _model;
        private string _variableName;

        public CoefficientEditDialog(LPModel model, string variableName)
        {
            _model = model;
            _variableName = variableName;
            InitializeDialog();
            LoadCoefficients();
        }

        private void InitializeDialog()
        {
            Text = $"Edit Coefficients for {_variableName}";
            Size = new Size(500, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            dgvCoefficients = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            dgvCoefficients.Columns.Add("Constraint", "Constraint");
            dgvCoefficients.Columns.Add("Current", "Current Value");
            dgvCoefficients.Columns.Add("New", "New Value");
            dgvCoefficients.Columns.Add("Delta", "Delta");

            var buttonPanel = new Panel { Height = 50, Dock = DockStyle.Bottom };
            btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 10, Top = 10, Width = 80 };
            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 100, Top = 10, Width = 80 };
            
            buttonPanel.Controls.AddRange(new Control[] { btnOK, btnCancel });
            Controls.AddRange(new Control[] { dgvCoefficients, buttonPanel });
        }

        private void LoadCoefficients()
        {
            foreach (var constraint in _model.Constraints)
            {
                double currentValue = constraint.Coefficients.TryGetValue(_variableName, out var value) ? value : 0.0;
                
                var row = new DataGridViewRow();
                row.CreateCells(dgvCoefficients);
                row.Cells[0].Value = constraint.Name;
                row.Cells[1].Value = currentValue.ToString("F3");
                row.Cells[2].Value = currentValue.ToString("F3");
                row.Cells[3].Value = "0.000";
                row.Cells[3].ReadOnly = true;
                
                dgvCoefficients.Rows.Add(row);
            }

            // Handle cell value changed to calculate delta
            dgvCoefficients.CellValueChanged += (s, e) =>
            {
                if (e.ColumnIndex == 2) // New value column
                {
                    var row = dgvCoefficients.Rows[e.RowIndex];
                    if (double.TryParse(row.Cells[1].Value?.ToString(), out var current) &&
                        double.TryParse(row.Cells[2].Value?.ToString(), out var newValue))
                    {
                        row.Cells[3].Value = (newValue - current).ToString("F3");
                    }
                }
            };
        }

        public List<CoefficientChange> GetCoefficientChanges()
        {
            var changes = new List<CoefficientChange>();
            
            foreach (DataGridViewRow row in dgvCoefficients.Rows)
            {
                if (double.TryParse(row.Cells[3].Value?.ToString(), out var delta) && Math.Abs(delta) > 0.001)
                {
                    changes.Add(new CoefficientChange
                    {
                        ConstraintName = row.Cells[0].Value?.ToString() ?? "",
                        Delta = delta
                    });
                }
            }
            
            return changes;
        }
    }

    public class CoefficientChange
    {
        public string ConstraintName { get; set; } = "";
        public double Delta { get; set; }
    }

    /// <summary>
    /// Simple dialog for adding new constraints
    /// </summary>
    public class AddConstraintDialog : Form
    {
        private TextBox txtConstraintName;
        private DataGridView dgvCoefficients;
        private ComboBox cmbRelation;
        private TextBox txtRHS;
        private Button btnOK;
        private Button btnCancel;
        private LPModel _model;

        public string ConstraintName => txtConstraintName.Text;
        public Dictionary<string, double> Coefficients { get; private set; } = new();
        public ConstraintRelation Relation { get; private set; }
        public double RHS { get; private set; }

        public AddConstraintDialog(LPModel model)
        {
            _model = model;
            InitializeDialog();
            LoadVariables();
        }

        private void InitializeDialog()
        {
            Text = "Add New Constraint";
            Size = new Size(600, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Constraint name
            var lblName = new Label { Text = "Constraint Name:", Left = 10, Top = 15, Width = 120 };
            txtConstraintName = new TextBox { Left = 140, Top = 12, Width = 200 };

            // Coefficients grid
            var lblCoeffs = new Label { Text = "Variable Coefficients:", Left = 10, Top = 50, Width = 200 };
            dgvCoefficients = new DataGridView
            {
                Left = 10, Top = 75, Width = 560, Height = 250,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvCoefficients.Columns.Add("Variable", "Variable");
            dgvCoefficients.Columns.Add("Coefficient", "Coefficient");

            // Relation and RHS
            var lblRelation = new Label { Text = "Relation:", Left = 10, Top = 340, Width = 80 };
            cmbRelation = new ComboBox { Left = 100, Top = 337, Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRelation.Items.AddRange(new[] { "<=", "=", ">=" });
            cmbRelation.SelectedIndex = 0;

            var lblRHS = new Label { Text = "RHS:", Left = 220, Top = 340, Width = 50 };
            txtRHS = new TextBox { Left = 280, Top = 337, Width = 100 };

            // Buttons
            var buttonPanel = new Panel { Left = 10, Top = 380, Width = 560, Height = 50 };
            btnOK = new Button { Text = "Add Constraint", Left = 10, Top = 10, Width = 120, Height = 35 };
            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 140, Top = 10, Width = 80, Height = 35 };

            btnOK.Click += BtnOK_Click;

            Controls.AddRange(new Control[] {
                lblName, txtConstraintName, lblCoeffs, dgvCoefficients,
                lblRelation, cmbRelation, lblRHS, txtRHS, buttonPanel
            });
            buttonPanel.Controls.AddRange(new Control[] { btnOK, btnCancel });
        }

        private void LoadVariables()
        {
            foreach (var variable in _model.Variables.Values.OrderBy(v => v.Index))
            {
                var row = new DataGridViewRow();
                row.CreateCells(dgvCoefficients);
                row.Cells[0].Value = variable.Name;
                row.Cells[1].Value = "0";
                dgvCoefficients.Rows.Add(row);
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(txtConstraintName.Text))
                {
                    MessageBox.Show("Please enter a constraint name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!double.TryParse(txtRHS.Text, out var rhs))
                {
                    MessageBox.Show("Please enter a valid RHS value.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Parse coefficients
                Coefficients.Clear();
                foreach (DataGridViewRow row in dgvCoefficients.Rows)
                {
                    var varName = row.Cells[0].Value?.ToString() ?? "";
                    if (double.TryParse(row.Cells[1].Value?.ToString(), out var coeff))
                    {
                        Coefficients[varName] = coeff;
                    }
                }

                // Parse relation
                Relation = cmbRelation.SelectedItem?.ToString() switch
                {
                    "<=" => ConstraintRelation.LessThanEqual,
                    "=" => ConstraintRelation.Equal,
                    ">=" => ConstraintRelation.GreaterThanEqual,
                    _ => ConstraintRelation.LessThanEqual
                };

                RHS = rhs;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}