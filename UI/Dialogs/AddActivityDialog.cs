using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LPR381_Assignment.Models;
using LPR381_Assignment.UI.Helpers;
using LPR381_Assignment.UI.Themes;

namespace LPR381_Assignment.UI.Dialogs
{
    /// <summary>
    /// Dialog for adding a new activity (variable) to the LP model
    /// </summary>
    public partial class AddActivityDialog : Form
    {
        private readonly LPModel _model;
        
        // Input controls
        private TextBox txtActivityName;
        private NumericUpDown nudObjectiveCoeff;
        private ComboBox cmbSignRestriction;
        private NumericUpDown[] nudConstraintCoeffs;
        private Label[] lblConstraintNames;
        
        // Buttons
        private Button btnOK;
        private Button btnCancel;
        
        // Layout panels
        private TableLayoutPanel mainLayout;
        private Panel constraintsPanel;
        private Panel buttonsPanel;
        
        public string ActivityName => txtActivityName.Text.Trim();
        public double ObjectiveCoefficient => (double)nudObjectiveCoeff.Value;
        public SignRestriction SignRestriction => (SignRestriction)cmbSignRestriction.SelectedIndex;
        public double[] ConstraintCoefficients => nudConstraintCoeffs.Select(nud => (double)nud.Value).ToArray();
        
        public AddActivityDialog(LPModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            InitializeComponent();
            SetupDialog();
        }
        
        private void InitializeComponent()
        {
            // Dialog properties
            Text = "Add New Activity";
            Size = new Size(500, Math.Max(350, 200 + _model.Constraints.Count * 35));
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = AppTheme.Background;
            Font = AppTheme.Default;
            
            // Main layout
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(16),
                BackColor = Color.Transparent
            };
            
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Input fields
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Constraints
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Buttons
            
            Controls.Add(mainLayout);
        }
        
        private void SetupDialog()
        {
            CreateInputSection();
            CreateConstraintsSection();
            CreateButtonsSection();
            
            // Apply styling
            ApplyTheme();
            
            // Set initial focus
            txtActivityName.Focus();
        }
        
        private void CreateInputSection()
        {
            var inputPanel = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 3,
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };
            
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inputPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            
            // Activity name
            var lblName = new Label { Text = "Activity Name:", AutoSize = true, Anchor = AnchorStyles.Left };
            txtActivityName = new TextBox { Width = 150, Text = GenerateDefaultName() };
            
            // Objective coefficient
            var lblObjective = new Label { Text = "Objective Coefficient:", AutoSize = true, Anchor = AnchorStyles.Left };
            nudObjectiveCoeff = new NumericUpDown 
            { 
                Width = 100, 
                DecimalPlaces = 3, 
                Minimum = -10000, 
                Maximum = 10000,
                Value = 1.0m
            };
            
            // Sign restriction
            var lblSign = new Label { Text = "Sign Restriction:", AutoSize = true, Anchor = AnchorStyles.Left };
            cmbSignRestriction = new ComboBox
            {
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSignRestriction.Items.AddRange(new string[] { "Positive (?0)", "Negative (?0)", "Unrestricted", "Integer", "Binary" });
            cmbSignRestriction.SelectedIndex = 0; // Default to Positive
            
            // Add controls to layout
            inputPanel.Controls.Add(lblName, 0, 0);
            inputPanel.Controls.Add(txtActivityName, 1, 0);
            inputPanel.Controls.Add(lblObjective, 0, 1);
            inputPanel.Controls.Add(nudObjectiveCoeff, 1, 1);
            inputPanel.Controls.Add(lblSign, 0, 2);
            inputPanel.Controls.Add(cmbSignRestriction, 1, 2);
            
            mainLayout.Controls.Add(inputPanel, 0, 0);
        }
        
        private void CreateConstraintsSection()
        {
            constraintsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
            };
            
            var lblHeader = new Label
            {
                Text = "Constraint Coefficients:",
                Font = AppTheme.Bold,
                AutoSize = true,
                Location = new Point(0, 0)
            };
            constraintsPanel.Controls.Add(lblHeader);
            
            // Create controls for each constraint
            nudConstraintCoeffs = new NumericUpDown[_model.Constraints.Count];
            lblConstraintNames = new Label[_model.Constraints.Count];
            
            for (int i = 0; i < _model.Constraints.Count; i++)
            {
                var constraint = _model.Constraints[i];
                
                // Constraint name label
                lblConstraintNames[i] = new Label
                {
                    Text = $"{constraint.Name}:",
                    AutoSize = true,
                    Location = new Point(0, 35 + i * 30)
                };
                
                // Coefficient input
                nudConstraintCoeffs[i] = new NumericUpDown
                {
                    Width = 100,
                    DecimalPlaces = 3,
                    Minimum = -10000,
                    Maximum = 10000,
                    Value = 0,
                    Location = new Point(120, 32 + i * 30)
                };
                
                // Constraint details label
                var detailsLabel = new Label
                {
                    Text = $"({constraint})",
                    AutoSize = true,
                    ForeColor = AppTheme.TextMuted,
                    Location = new Point(230, 35 + i * 30)
                };
                
                constraintsPanel.Controls.Add(lblConstraintNames[i]);
                constraintsPanel.Controls.Add(nudConstraintCoeffs[i]);
                constraintsPanel.Controls.Add(detailsLabel);
            }
            
            mainLayout.Controls.Add(constraintsPanel, 0, 1);
        }
        
        private void CreateButtonsSection()
        {
            buttonsPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom,
                BackColor = Color.Transparent
            };
            
            btnOK = new Button
            {
                Text = "Add Activity",
                Size = new Size(120, 35),
                DialogResult = DialogResult.OK
            };
            
            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel
            };
            
            // Position buttons
            btnCancel.Location = new Point(buttonsPanel.Width - btnCancel.Width - 10, 8);
            btnOK.Location = new Point(btnCancel.Left - btnOK.Width - 10, 8);
            
            // Handle resize to keep buttons positioned correctly
            buttonsPanel.Resize += (s, e) =>
            {
                btnCancel.Location = new Point(buttonsPanel.Width - btnCancel.Width - 10, 8);
                btnOK.Location = new Point(btnCancel.Left - btnOK.Width - 10, 8);
            };
            
            buttonsPanel.Controls.Add(btnOK);
            buttonsPanel.Controls.Add(btnCancel);
            
            // Validation
            btnOK.Click += (s, e) =>
            {
                if (!ValidateInput())
                {
                    DialogResult = DialogResult.None; // Prevent dialog from closing
                }
            };
            
            mainLayout.Controls.Add(buttonsPanel, 0, 2);
        }
        
        private void ApplyTheme()
        {
            // Style labels
            foreach (var control in this.GetAllControls().OfType<Label>())
            {
                ControlStyler.StyleLabel(control, control.ForeColor == AppTheme.TextMuted);
            }
            
            // Style buttons
            ControlStyler.StyleButton(btnOK, primary: true);
            ControlStyler.StyleButton(btnCancel);
            
            // Style combo box
            ControlStyler.StyleCombo(cmbSignRestriction);
            
            // Style text box
            txtActivityName.BackColor = AppTheme.Card;
            txtActivityName.ForeColor = AppTheme.Text;
            txtActivityName.BorderStyle = BorderStyle.FixedSingle;
            
            // Style numeric up/down controls
            foreach (var nud in nudConstraintCoeffs.Concat(new[] { nudObjectiveCoeff }))
            {
                nud.BackColor = AppTheme.Card;
                nud.ForeColor = AppTheme.Text;
            }
        }
        
        private bool ValidateInput()
        {
            // Check activity name
            if (string.IsNullOrWhiteSpace(txtActivityName.Text))
            {
                MessageBox.Show("Please enter an activity name.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtActivityName.Focus();
                return false;
            }
            
            // Check for valid variable name format
            var name = txtActivityName.Text.Trim();
            if (!IsValidVariableName(name))
            {
                MessageBox.Show("Activity name must start with a letter and contain only letters and numbers.", 
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtActivityName.Focus();
                return false;
            }
            
            // Check if name already exists
            if (_model.Variables.ContainsKey(name))
            {
                MessageBox.Show($"Variable '{name}' already exists in the model.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtActivityName.Focus();
                return false;
            }
            
            return true;
        }
        
        private bool IsValidVariableName(string name)
        {
            if (string.IsNullOrEmpty(name) || !char.IsLetter(name[0]))
                return false;
                
            return name.All(c => char.IsLetterOrDigit(c));
        }
        
        private string GenerateDefaultName()
        {
            // Generate a default name like x4, x5, etc.
            int index = _model.Variables.Count + 1;
            string baseName = "x";
            
            while (_model.Variables.ContainsKey($"{baseName}{index}"))
            {
                index++;
            }
            
            return $"{baseName}{index}";
        }
    }
    
    /// <summary>
    /// Extension method to get all controls recursively
    /// </summary>
    public static class ControlExtensions
    {
        public static IEnumerable<Control> GetAllControls(this Control container)
        {
            var controls = new List<Control>();
            
            foreach (Control control in container.Controls)
            {
                controls.Add(control);
                controls.AddRange(control.GetAllControls());
            }
            
            return controls;
        }
    }
}