using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LPR381_Assignment.Models;
using LPR381_Assignment.Services.Algorithms;

namespace LPR381_Assignment
{
    // Partial class containing simplex algorithm integration
    public partial class TabbedMainForm
    {
        // Additional dependencies for simplex solving
        private readonly SimplexEngine _simplexEngine = new();
        private SolverResult? _lastSolveResult = null;

        // Field to prevent multiple dialog instances
        private bool _iterationDetailsDialogOpen = false;

        /// <summary>
        /// Enhanced solve handler that integrates with the simplex algorithms
        /// </summary>
        private void BtnSolve_Click_Enhanced(object sender, EventArgs e)
        {
            try
            {
                // Check if we have a model loaded
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before solving.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Clear previous results completely
                ClearAllPreviousResults();

                // Update status
                sbStatus.Text = "Solving problem...";
                sbIter.Text = "Iter: 0";

                // Get selected algorithm
                string algorithmName = GetSelectedAlgorithmName();
                
                // Validate algorithm selection
                if (algorithmName == "Unknown")
                {
                    MessageBox.Show("Please select an algorithm before solving.", 
                        "No Algorithm Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if algorithm supports the model type
                bool hasIntegerVars = _currentModel.Variables.Values.Any(v => 
                    v.SignRestriction == SignRestriction.Integer || 
                    v.SignRestriction == SignRestriction.Binary);

                if (hasIntegerVars && (algorithmName == "Primal Simplex" || algorithmName == "Revised Primal Simplex"))
                {
                    var result = MessageBox.Show(
                        "The selected algorithm only solves LP relaxation of integer problems.\n\n" +
                        "Do you want to continue with LP relaxation?", 
                        "Integer Variables Detected", 
                        MessageBoxButtons.YesNo, 
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.No)
                        return;
                }

                // Solve the model
                _lastSolveResult = _simplexEngine.Solve(_currentModel, algorithmName);

                // Debug: Print initial and final tableau information
                if (_lastSolveResult.InitialTableau != null)
                {
                    System.Diagnostics.Debug.WriteLine("=== INITIAL TABLEAU ===");
                    System.Diagnostics.Debug.WriteLine(_lastSolveResult.InitialTableau.ToFormattedString());
                }

                if (_lastSolveResult.FinalTableau != null)
                {
                    System.Diagnostics.Debug.WriteLine("=== FINAL TABLEAU ===");
                    System.Diagnostics.Debug.WriteLine(_lastSolveResult.FinalTableau.ToFormattedString());
                }

                // Update status based on result
                if (_lastSolveResult.IsSuccessful)
                {
                    sbStatus.Text = $"Solved successfully - {_lastSolveResult.Status}";
                    sbIter.Text = $"Iter: {_lastSolveResult.IterationCount}";
                }
                else
                {
                    sbStatus.Text = $"Solve failed - {_lastSolveResult.Status}";
                }

                // Display results in Results tab
                DisplaySolverResults(_lastSolveResult);

                // Display canonical form if available
                DisplayCanonicalForm(_lastSolveResult);

                // Display iterations
                DisplayIterations(_lastSolveResult);

                // Update sensitivity analysis dropdowns with solve results
                UpdateVariableDropdownsFromSolveResult();

                // Navigate to appropriate tab based on result
                if (_lastSolveResult.IsSuccessful)
                {
                    tabMain.SelectedTab = tabResults;
                }
                else
                {
                    // Show error in results tab
                    tabMain.SelectedTab = tabResults;
                }

                // Show completion message
                if (_lastSolveResult.IsSuccessful)
                {
                    MessageBox.Show(
                        $"Problem solved successfully!\n\n" +
                        $"Status: {_lastSolveResult.Status}\n" +
                        $"Algorithm: {_lastSolveResult.AlgorithmUsed}\n" +
                        $"Iterations: {_lastSolveResult.IterationCount}\n" +
                        $"Execution Time: {_lastSolveResult.ExecutionTimeMs:F2} ms\n\n" +
                        ((_lastSolveResult.Status == SolutionStatus.Optimal) ? 
                            $"Optimal Value: {_lastSolveResult.ObjectiveValue:F3}" : ""),
                        "Solve Complete", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        $"Solving failed!\n\n" +
                        $"Error: {_lastSolveResult.ErrorMessage}\n" +
                        $"Algorithm: {_lastSolveResult.AlgorithmUsed}",
                        "Solve Failed", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error during solving: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Solve error.";
            }
        }

        /// <summary>
        /// Displays the solver results in the Results tab
        /// </summary>
        private void DisplaySolverResults(SolverResult result)
        {
            rtbResultsSummary.Clear();
            rtbResultsSummary.AppendText(result.CreateSummary());
            
            if (result.Warnings.Count > 0)
            {
                rtbResultsSummary.AppendText("\n\n=== WARNINGS ===\n");
                foreach (var warning in result.Warnings)
                {
                    rtbResultsSummary.AppendText($"• {warning}\n");
                }
            }

            if (result.AdditionalInfo.Count > 0)
            {
                rtbResultsSummary.AppendText("\n\n=== ADDITIONAL INFORMATION ===\n");
                foreach (var info in result.AdditionalInfo)
                {
                    rtbResultsSummary.AppendText($"{info.Key}: {info.Value}\n");
                }
            }
        }

        /// <summary>
        /// Displays the canonical form in the Canonical Form tab
        /// </summary>
        private void DisplayCanonicalForm(SolverResult result)
        {
            if (result.CanonicalForm == null || !result.CanonicalForm.IsValid)
            {
                rtbCanonicalNotes.Text = "Canonical form not available or invalid.";
                if (result.CanonicalForm != null)
                {
                    rtbCanonicalNotes.Text += $"\nError: {result.CanonicalForm.ErrorMessage}";
                }
                return;
            }

            var canonicalForm = result.CanonicalForm;
            var tableau = canonicalForm.Tableau;

            // Clear and setup the canonical tableau grid
            dgvCanonicalTableau.Rows.Clear();
            dgvCanonicalTableau.Columns.Clear();

            // Add columns for each variable plus RHS
            dgvCanonicalTableau.Columns.Add("Basis", "Basis");
            foreach (var varName in tableau.VariableNames)
            {
                dgvCanonicalTableau.Columns.Add(varName, varName);
            }

            // Add objective row
            var objRow = new DataGridViewRow();
            objRow.CreateCells(dgvCanonicalTableau);
            objRow.Cells[0].Value = "Z";
            for (int j = 0; j < tableau.Columns; j++)
            {
                objRow.Cells[j + 1].Value = tableau.Matrix[0, j].ToString("F3");
            }
            dgvCanonicalTableau.Rows.Add(objRow);

            // Add constraint rows
            for (int i = 1; i < tableau.Rows; i++)
            {
                var constraintRow = new DataGridViewRow();
                constraintRow.CreateCells(dgvCanonicalTableau);
                
                string basicVar = i - 1 < tableau.BasicVariables.Count ? 
                    tableau.BasicVariables[i - 1] : $"Row{i}";
                constraintRow.Cells[0].Value = basicVar;
                
                for (int j = 0; j < tableau.Columns; j++)
                {
                    constraintRow.Cells[j + 1].Value = tableau.Matrix[i, j].ToString("F3");
                }
                dgvCanonicalTableau.Rows.Add(constraintRow);
            }

            // Display canonical form information in notes
            rtbCanonicalNotes.Text = canonicalForm.ToFormattedString();
        }

        /// <summary>
        /// Displays the iterations in the Iterations tab using the expanded format by default
        /// </summary>
        private void DisplayIterations(SolverResult result)
        {
            try
            {
                // Use BeginUpdate/EndUpdate for better performance and visual consistency
                lvIterations.BeginUpdate();

                // Always clear everything first to ensure clean state
                lvIterations.Items.Clear();
                lvIterations.Columns.Clear();
                lvIterations.Groups.Clear();

                if (result.Iterations.Count == 0)
                {
                    // Setup columns for the expanded format even when no iterations
                    lvIterations.View = View.Details;
                    lvIterations.Columns.Add("Step", 120);
                    lvIterations.Columns.Add("Phase", 120);
                    lvIterations.Columns.Add("Description", 300);
                    lvIterations.Columns.Add("Entering", 120);
                    lvIterations.Columns.Add("Leaving", 120);
                    lvIterations.Columns.Add("Objective", 120);
                    lvIterations.Columns.Add("Time (ms)", 120);

                    var noIterItem = new ListViewItem("0");
                    noIterItem.SubItems.Add("Initial");
                    noIterItem.SubItems.Add("No iterations performed");
                    noIterItem.SubItems.Add("–");
                    noIterItem.SubItems.Add("–");
                    noIterItem.SubItems.Add("–");
                    noIterItem.SubItems.Add("–");
                    lvIterations.Items.Add(noIterItem);
                    
                    System.Diagnostics.Debug.WriteLine("DisplayIterations: No iterations to display");
                    return;
                }

                // Always use the expanded format by default
                lvIterations.View = View.Details;
                lvIterations.Columns.Add("Step", 120);
                lvIterations.Columns.Add("Phase", 120);
                lvIterations.Columns.Add("Description", 300);
                lvIterations.Columns.Add("Entering", 120);
                lvIterations.Columns.Add("Leaving", 120);
                lvIterations.Columns.Add("Objective", 120);
                lvIterations.Columns.Add("Time (ms)", 120);

                System.Diagnostics.Debug.WriteLine($"DisplayIterations: Displaying {result.Iterations.Count} iterations for algorithm: {result.AlgorithmUsed}");

                foreach (var iteration in result.Iterations)
                {
                    var item = new ListViewItem(iteration.IterationNumber.ToString());
                    item.SubItems.Add($"Phase {iteration.Phase}");
                    item.SubItems.Add(iteration.CreateSummary());
                    item.SubItems.Add(iteration.EnteringVariable ?? "–");
                    item.SubItems.Add(iteration.LeavingVariable ?? "–");
                    item.SubItems.Add(iteration.ObjectiveValue.ToString("F3"));
                    item.SubItems.Add(iteration.ExecutionTimeMs.ToString("F2"));

                    // Color coding based on iteration result
                    if (iteration.IsOptimal)
                    {
                        item.BackColor = Color.LightGreen;
                        item.ToolTipText = "Optimal solution reached";
                    }
                    else if (iteration.IsInfeasible)
                    {
                        item.BackColor = Color.LightCoral;
                        item.ToolTipText = "Infeasibility detected";
                    }
                    else if (iteration.IsUnbounded)
                    {
                        item.BackColor = Color.LightYellow;
                        item.ToolTipText = "Unboundedness detected";
                    }
                    else if (iteration.PivotOperation != null)
                    {
                        item.ToolTipText = $"Pivot: {iteration.PivotOperation}";
                    }

                    // Store iteration data for detailed view
                    item.Tag = iteration;
                    
                    lvIterations.Items.Add(item);
                }

                // Auto-resize columns to fit content, but maintain minimum widths
                foreach (ColumnHeader column in lvIterations.Columns)
                {
                    column.AutoResize(ColumnHeaderAutoResizeStyle.ColumnContent);
                    // Ensure minimum widths are maintained
                    if (column.Width < 120 && column.Text != "Description")
                    {
                        column.Width = 120;
                    }
                    else if (column.Text == "Description" && column.Width < 300)
                    {
                        column.Width = 300;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"DisplayIterations: Successfully displayed {lvIterations.Items.Count} iteration items");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in DisplayIterations: {ex.Message}");
                
                // Show error message to user
                lvIterations.Items.Clear();
                lvIterations.Columns.Clear();
                lvIterations.Columns.Add("Error", 500);
                var errorItem = new ListViewItem($"Error displaying iterations: {ex.Message}");
                errorItem.BackColor = Color.LightCoral;
                lvIterations.Items.Add(errorItem);
            }
            finally
            {
                lvIterations.EndUpdate();
            }
        }

        /// <summary>
        /// Gets the last solve result for sensitivity analysis
        /// </summary>
        public SolverResult? GetLastSolveResult()
        {
            return _lastSolveResult;
        }

        /// <summary>
        /// Updates variable dropdowns with information from the solve result
        /// </summary>
        private void UpdateVariableDropdownsFromSolveResult()
        {
            if (_lastSolveResult?.FinalTableau == null) return;

            try
            {
                var tableau = _lastSolveResult.FinalTableau;

                // Update Non-Basic Variable dropdown
                saNB_VarSelect.Items.Clear();
                var nonBasicVars = tableau.NonBasicVariables.Where(v => !v.StartsWith("s") && !v.StartsWith("a") && v != "RHS").ToArray();
                if (nonBasicVars.Length > 0)
                {
                    saNB_VarSelect.Items.AddRange(nonBasicVars);
                }
                else
                {
                    saNB_VarSelect.Items.Add("(No non-basic variables)");
                }

                // Update Basic Variable dropdown
                saB_VarSelect.Items.Clear();
                var basicVars = tableau.BasicVariables.Where(v => !v.StartsWith("s") && !v.StartsWith("a")).ToArray();
                if (basicVars.Length > 0)
                {
                    saB_VarSelect.Items.AddRange(basicVars);
                }
                else
                {
                    saB_VarSelect.Items.Add("(No basic variables)");
                }

                // Update Constraint dropdown remains the same
                saRHS_ConSelect.Items.Clear();
                if (_currentModel != null)
                {
                    var constraints = _currentModel.Constraints.Select(c => c.Name).ToArray();
                    if (constraints.Length > 0)
                    {
                        saRHS_ConSelect.Items.AddRange(constraints);
                    }
                }

                // Update Column analysis dropdown
                if (saCol_VarSelect != null && _currentModel != null)
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
                System.Diagnostics.Debug.WriteLine($"Error updating variable dropdowns from solve result: {ex.Message}");
            }
        }

        /// <summary>
        /// Event handler for lvIterations double-click to show detailed iteration information
        /// </summary>
        private void LvIterations_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("LvIterations_DoubleClick: Event triggered");
                
                if (lvIterations.SelectedItems.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("LvIterations_DoubleClick: No items selected");
                    return;
                }

                var selectedItem = lvIterations.SelectedItems[0];
                if (selectedItem.Tag is SimplexIteration iteration)
                {
                    System.Diagnostics.Debug.WriteLine($"LvIterations_DoubleClick: Showing details for iteration {iteration.IterationNumber}");
                    ShowIterationDetails(iteration);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("LvIterations_DoubleClick: Selected item does not contain iteration data");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LvIterations_DoubleClick: {ex.Message}");
                MessageBox.Show($"Error showing iteration details: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Shows detailed information about a specific iteration
        /// </summary>
        private void ShowIterationDetails(SimplexIteration iteration)
        {
            // Prevent multiple dialogs from opening simultaneously
            if (_iterationDetailsDialogOpen)
            {
                System.Diagnostics.Debug.WriteLine("ShowIterationDetails: Dialog already open, ignoring request");
                return;
            }

            try
            {
                _iterationDetailsDialogOpen = true;
                System.Diagnostics.Debug.WriteLine($"ShowIterationDetails: Opening dialog for iteration {iteration.IterationNumber}");

                var details = new System.Text.StringBuilder();
                
                // Check if this is a Revised Simplex iteration with detailed steps
                if (iteration.Steps.Count > 0 && iteration.Description.Contains("Product Form"))
                {
                    // Display Product Form and Price Out details instead of tableau
                    details.AppendLine($"=== REVISED SIMPLEX ITERATION {iteration.IterationNumber} DETAILS ===");
                    details.AppendLine();
                    details.AppendLine($"Algorithm: Revised Primal Simplex Method");
                    details.AppendLine($"Phase: {iteration.Phase}");
                    details.AppendLine($"Execution Time: {iteration.ExecutionTimeMs:F2} ms");
                    details.AppendLine();

                    // Show the detailed Product Form and Price Out steps
                    foreach (var step in iteration.Steps)
                    {
                        details.AppendLine(step.Description);
                    }

                    if (iteration.Warnings.Count > 0)
                    {
                        details.AppendLine("WARNINGS:");
                        foreach (var warning in iteration.Warnings)
                        {
                            details.AppendLine($"  • {warning}");
                        }
                        details.AppendLine();
                    }
                }
                else
                {
                    // Standard tableau display for regular Primal Simplex
                    details.AppendLine($"=== ITERATION {iteration.IterationNumber} DETAILS ===");
                    details.AppendLine();
                    details.AppendLine($"Phase: {iteration.Phase}")
                           .AppendLine($"Description: {iteration.Description}")
                           .AppendLine($"Execution Time: {iteration.ExecutionTimeMs:F2} ms")
                           .AppendLine($"Objective Value: {iteration.ObjectiveValue:F3}")
                           .AppendLine();

                    if (iteration.PivotOperation != null)
                    {
                        details.AppendLine("PIVOT OPERATION:");
                        details.AppendLine($"  Entering Variable: {iteration.EnteringVariable}");
                        details.AppendLine($"  Leaving Variable: {iteration.LeavingVariable}");
                        details.AppendLine($"  Pivot Row: {iteration.PivotOperation.PivotRow}");
                        details.AppendLine($"  Pivot Column: {iteration.PivotOperation.PivotColumn}");
                        details.AppendLine($"  Pivot Element: {iteration.PivotOperation.PivotElement:F3}");
                        details.AppendLine();
                    }

                    if (iteration.RatioTests.Count > 0)
                    {
                        details.AppendLine("RATIO TEST RESULTS:");
                        foreach (var ratio in iteration.RatioTests)
                        {
                            details.AppendLine($"  {ratio}");
                        }
                        details.AppendLine();
                    }

                    if (iteration.Warnings.Count > 0)
                    {
                        details.AppendLine("WARNINGS:");
                        foreach (var warning in iteration.Warnings)
                        {
                            details.AppendLine($"  • {warning}");
                        }
                        details.AppendLine();
                    }

                    if (iteration.TableauAfter != null)
                    {
                        details.AppendLine("RESULTING TABLEAU:");
                        details.AppendLine(iteration.TableauAfter.ToFormattedString());
                    }
                }

                // Create a scrollable message box using a custom form
                ShowIterationDetailsDialog(iteration.IterationNumber, details.ToString());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowIterationDetails: {ex.Message}");
                MessageBox.Show($"Error preparing iteration details: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _iterationDetailsDialogOpen = false;
                System.Diagnostics.Debug.WriteLine("ShowIterationDetails: Dialog closed, flag reset");
            }
        }

        /// <summary>
        /// Shows iteration details in a custom scrollable dialog
        /// </summary>
        private void ShowIterationDetailsDialog(int iterationNumber, string details)
        {
            Form? form = null;
            try
            {
                System.Diagnostics.Debug.WriteLine($"ShowIterationDetailsDialog: Creating dialog for iteration {iterationNumber}");
                
                form = new Form
                {
                    Text = $"Iteration {iterationNumber} Details",
                    Size = new Size(900, 700),
                    StartPosition = FormStartPosition.CenterParent,
                    ShowIcon = false,
                    MinimizeBox = false,
                    MaximizeBox = true,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    ShowInTaskbar = false
                };

                var textBox = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Both,
                    Font = new Font("Consolas", 10F),
                    Text = details,
                    Margin = new Padding(10)
                };

                var buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 50,
                    Padding = new Padding(10)
                };

                var closeButton = new Button
                {
                    Text = "Close",
                    Width = 80,
                    Height = 30,
                    Anchor = AnchorStyles.Right | AnchorStyles.Top,
                    DialogResult = DialogResult.OK
                };
                closeButton.Left = buttonPanel.Width - closeButton.Width - 10;

                var copyButton = new Button
                {
                    Text = "Copy to Clipboard",
                    Width = 120,
                    Height = 30,
                    Anchor = AnchorStyles.Left | AnchorStyles.Top,
                    Left = 10
                };
                
                // Fix the event handler to avoid dialog reopening issues
                copyButton.Click += (s, e) => 
                {
                    try
                    {
                        Clipboard.SetText(details);
                        // Use a simple status update instead of another modal dialog
                        copyButton.Text = "Copied!";
                        copyButton.Enabled = false;
                        
                        // Reset the button after a short delay
                        var timer = new System.Windows.Forms.Timer { Interval = 1500 };
                        timer.Tick += (timerSender, timerArgs) =>
                        {
                            try
                            {
                                if (form != null && !form.IsDisposed && !copyButton.IsDisposed)
                                {
                                    copyButton.Text = "Copy to Clipboard";
                                    copyButton.Enabled = true;
                                }
                            }
                            catch
                            {
                                // Ignore errors during cleanup
                            }
                            finally
                            {
                                timer.Stop();
                                timer.Dispose();
                            }
                        };
                        timer.Start();
                    }
                    catch (Exception ex)
                    {
                        // Show error in button text instead of modal dialog
                        copyButton.Text = "Copy Failed";
                        System.Diagnostics.Debug.WriteLine($"Failed to copy to clipboard: {ex.Message}");
                    }
                };

                buttonPanel.Controls.Add(closeButton);
                buttonPanel.Controls.Add(copyButton);

                form.Controls.Add(textBox);
                form.Controls.Add(buttonPanel);

                // Add form closing event to ensure cleanup
                form.FormClosing += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine("ShowIterationDetailsDialog: Form closing event");
                };

                System.Diagnostics.Debug.WriteLine("ShowIterationDetailsDialog: Showing modal dialog");
                
                // Use ShowDialog() with proper parent and disposal
                using (form)
                {
                    form.ShowDialog(this);
                }
                
                System.Diagnostics.Debug.WriteLine("ShowIterationDetailsDialog: Dialog closed and disposed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ShowIterationDetailsDialog: {ex.Message}");
                
                // Clean up form if it was created but failed
                if (form != null && !form.IsDisposed)
                {
                    try
                    {
                        form.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
                
                // Show a simple message instead
                MessageBox.Show($"Error displaying iteration details: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Method to connect the enhanced solve handler
        /// </summary>
        private void ConnectEnhancedSolveHandler()
        {
            try
            {
                // Remove any existing handlers first to prevent duplicates
                btnSolve.Click -= BtnSolve_Click_Enhanced;
                lvIterations.DoubleClick -= LvIterations_DoubleClick;
                
                // Add the enhanced handler
                btnSolve.Click += BtnSolve_Click_Enhanced;
                
                // Connect iteration details handler
                lvIterations.DoubleClick += LvIterations_DoubleClick;
                
                // Note: Expand/collapse buttons have been removed since we now use the expanded format by default
                
                // Ensure correct algorithm status is shown
                SetAlgoStatus(GetSelectedAlgorithmName());
                
                // Debug confirmation
                System.Diagnostics.Debug.WriteLine("Enhanced solve handler connected successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error connecting enhanced solve handler: {ex.Message}");
                MessageBox.Show($"Error connecting solve functionality: {ex.Message}", "Connection Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Completely clears all previous results to ensure clean state
        /// </summary>
        private void ClearAllPreviousResults()
        {
            try
            {
                // Clear Results tab
                rtbResultsSummary.Clear();
                
                // Clear Canonical Form tab
                dgvCanonicalTableau.Rows.Clear();
                dgvCanonicalTableau.Columns.Clear();
                rtbCanonicalNotes.Clear();
                
                // Clear Iterations tab completely
                lvIterations.BeginUpdate();
                try
                {
                    lvIterations.Items.Clear();
                    lvIterations.Columns.Clear();
                    lvIterations.Groups.Clear();
                    
                    // Force refresh of the ListView
                    lvIterations.Refresh();
                }
                finally
                {
                    lvIterations.EndUpdate();
                }
                
                // Clear any cached solve result
                _lastSolveResult = null;
                
                System.Diagnostics.Debug.WriteLine("All previous results cleared successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing previous results: {ex.Message}");
                // Continue with solve even if clearing fails
            }
        }
    }
}