using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LPR381_Assignment.Models;
using LPR381_Assignment.Services;
using LPR381_Assignment.Services.Analysis;
using LPR381_Assignment.UI.Controls;
using LPR381_Assignment.UI.Helpers;
using LPR381_Assignment.UI.Themes;

namespace LPR381_Assignment
{
    // Main form for the LP/IP Solver application
    public partial class TabbedMainForm : Form
    {
        // Model state
        private string _rawModelText = string.Empty;
        private LPModel? _currentModel = null;
        private readonly ModelParser _modelParser = new();
        private readonly ShadowPriceCalculator _shadowPriceCalculator = new();
        private readonly DualModelGenerator _dualModelGenerator = new();

        // Menu & Status controls
        private MenuStrip mainMenu;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel sbStatus;
        private ToolStripStatusLabel sbAlgo;
        private ToolStripStatusLabel sbIter;
        private ToolStripStatusLabel sbNode;

        // Host panel that carries only content (adds top/bottom padding)
        private Panel contentHost;

        // Hidden-header TabControl
        private HiddenTabControl tabMain;

        // Tab: Model Input
        private TabPage tabModelInput;
        private Button btnLoadModelFile;
        private Button btnSaveModelFile;
        private Button btnValidateInput;
        private Label lblModelFormat;
        private DataGridView dgvObjective;
        private DataGridView dgvConstraints;
        private DataGridView dgvSignRestrictions;
        private ComboBox cmbProblemType;
        private ComboBox cmbObjectiveSense;
        private Label lblProblemType;
        private Label lblObjectiveSense;

        // Tab: Algorithm
        private TabPage tabAlgorithm;
        private StyledGroupPanel grpAlgorithms;
        private RadioButton rbPrimalSimplex;
        private RadioButton rbRevisedPrimalSimplex;
        private RadioButton rbBnBSimplex;
        private RadioButton rbCuttingPlane;
        private RadioButton rbBnBKnapsack;
        private Button btnSolve;
        private StyledGroupPanel grpOptions;
        private CheckBox chkBinary;
        private CheckBox chkGeneralInteger;

        // Tab: Canonical Form
        private TabPage tabCanonical;
        private DataGridView dgvCanonicalTableau;
        private RichTextBox rtbCanonicalNotes;

        // Tab: Iterations
        private TabPage tabIterations;
        private ListView lvIterations;
        private Button btnExpandAll;
        private Button btnCollapseAll;

        // Tab: Results
        private TabPage tabResults;
        private RichTextBox rtbResultsSummary;
        private Button btnExportResults;
        private Label lblRoundingNote;

        // Tab: Sensitivity
        private TabPage tabSensitivity;
        private StyledGroupPanel pnlSA_VarNonBasic;
        private ComboBox saNB_VarSelect;
        private Button saNB_ShowRange;
        private NumericUpDown saNB_ApplyDelta;
        private Button saNB_Apply;
        private ListView saNB_Output;

        private StyledGroupPanel pnlSA_VarBasic;
        private ComboBox saB_VarSelect;
        private Button saB_ShowRange;
        private NumericUpDown saB_ApplyDelta;
        private Button saB_Apply;
        private ListView saB_Output;

        private StyledGroupPanel pnlSA_RHS;
        private ComboBox saRHS_ConSelect;
        private Button saRHS_ShowRange;
        private NumericUpDown saRHS_ApplyDelta;
        private Button saRHS_Apply;
        private ListView saRHS_Output;

        private StyledGroupPanel pnlSA_Column;
        private ComboBox saCol_VarSelect;
        private Button saCol_ShowRange;
        private Button saCol_EditCoeffs;
        private ListView saCol_Output;

        private StyledGroupPanel pnlSA_AddActivity;
        private Button saAddAct_Add;
        private ListView saAddAct_Output;

        private StyledGroupPanel pnlSA_AddConstraint;
        private Button saAddCon_Add;
        private ListView saAddCon_Output;

        private StyledGroupPanel pnlSA_ShadowPrices;
        private Button saShadow_Show;
        private ListView saShadow_Output;
        
        private StyledGroupPanel pnlSA_Duality;
        private Button saDual_Build;
        private Button saDual_Solve;
        private Button saDual_Verify;
        private ListView saDual_Output;

        // Tab: Node Explorer
        private TabPage tabNodes;
        private SplitContainer splitNodes;
        private TreeView tvNodes;
        private Panel nodeDetailsPanel;
        private Label lblNodeTitle;
        private Label lblNodeStatus;
        private Label lblNodeBound;
        private Label lblNodeIncumbent;
        private ListView lvNodeIterations;

        // Tab: Cuts
        private TabPage tabCuts;
        private ListView lvCuts;
        private Button btnAddCut;
        private Button btnClearCuts;

        // Initializes a new instance of the form
        public TabbedMainForm()
        {
            Text = "LP/IP Solver - LPR381 Assignment";
            
            // Set full screen and disable resizing
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = true; // Keep minimize button for user convenience
            
            // Remove fixed size constraints since we're using full screen
            StartPosition = FormStartPosition.CenterScreen;

            // Reduce flicker for smoother UI
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            UpdateStyles();

            InitializeComponents();
        }

        // Custom window creation params for double-buffering
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        // -------------------- Init & Build UI --------------------

        // Builds all UI components and applies theme
        private void InitializeComponents()
        {
            BuildMenu();
            BuildStatusStrip();

            // Content host with ONLY top/bottom padding for the main content
            contentHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = AppTheme.Background,
                Padding = new Padding(0, 12, 0, 12) // margin from menu/status
            };
            Controls.Add(contentHost);

            // Tabs (hidden headers) inside content host
            tabMain = new HiddenTabControl { Dock = DockStyle.Fill };
            contentHost.Controls.Add(tabMain);

            BuildTabModelInput();
            BuildTabAlgorithm();
            BuildTabCanonical();
            BuildTabIterations();
            BuildTabResults();
            BuildTabSensitivity();
            BuildTabNodes();
            BuildTabCuts();

            tabMain.TabPages.AddRange(new[]
            {
                tabModelInput, tabAlgorithm, tabCanonical, tabIterations, tabResults, tabSensitivity, tabNodes, tabCuts
            });

            ApplyTheme();
        }

        // Builds the main menu bar
        private void BuildMenu()
        {
            mainMenu = new MenuStrip();
            var mFile = new ToolStripMenuItem("&File");
            var mSolve = new ToolStripMenuItem("&Solve");
            var mAnalysis = new ToolStripMenuItem("&Analysis");
            var mView = new ToolStripMenuItem("&View");
            var mHelp = new ToolStripMenuItem("&Help");

            // File menu - directly wire to existing event handlers
            var miOpen = new ToolStripMenuItem("Open Model…", null, (s, e) => BtnLoadModelFile_Click(s, e));
            var miSave = new ToolStripMenuItem("Save Model…", null, (s, e) => BtnSaveModelFile_Click(s, e));
            var miExport = new ToolStripMenuItem("Export Results…", null, (s, e) => BtnExportResults_Click(s, e));
            var miExit = new ToolStripMenuItem("Exit", null, (s, e) => Close());
            mFile.DropDownItems.AddRange(new ToolStripItem[] { miOpen, miSave, new ToolStripSeparator(), miExport, new ToolStripSeparator(), miExit });

            // Solve menu - directly wire to existing controls and handlers
            var miAlgPrimal = new ToolStripMenuItem("Primal Simplex") { CheckOnClick = true };
            var miAlgRevised = new ToolStripMenuItem("Revised Primal Simplex") { CheckOnClick = true, Checked = true };
            var miAlgBnB = new ToolStripMenuItem("Branch and Bound (Simplex)") { CheckOnClick = true };
            var miAlgCuts = new ToolStripMenuItem("Cutting Plane") { CheckOnClick = true };
            var miAlgKnapsack = new ToolStripMenuItem("Branch and Bound (Knapsack)") { CheckOnClick = true };
            miAlgPrimal.Click += (s, e) => { rbPrimalSimplex.Checked = true; SetAlgoStatus("Primal Simplex"); };
            miAlgRevised.Click += (s, e) => { rbRevisedPrimalSimplex.Checked = true; SetAlgoStatus("Revised Primal Simplex"); };
            miAlgBnB.Click += (s, e) => { rbBnBSimplex.Checked = true; SetAlgoStatus("B&B (Simplex)"); };
            miAlgCuts.Click += (s, e) => { rbCuttingPlane.Checked = true; SetAlgoStatus("Cutting Plane"); };
            miAlgKnapsack.Click += (s, e) => { rbBnBKnapsack.Checked = true; SetAlgoStatus("B&B (Knapsack)"); };
            var miSolveNow = new ToolStripMenuItem("Solve Now", null, (s, e) => BtnSolve_Click(s, e));
            mSolve.DropDownItems.AddRange(new ToolStripItem[] { miAlgPrimal, miAlgRevised, miAlgBnB, miAlgCuts, miAlgKnapsack, new ToolStripSeparator(), miSolveNow });

            // Analysis menu - wire to existing controls
            var miSensitivity = new ToolStripMenuItem("Sensitivity…", null, (s, e) => tabMain.SelectedTab = tabSensitivity);
            var miShadow = new ToolStripMenuItem("Shadow Prices", null, (s, e) => saShadow_Show.PerformClick());
            var miDualBuild = new ToolStripMenuItem("Build Dual", null, (s, e) => saDual_Build.PerformClick());
            var miDualSolve = new ToolStripMenuItem("Solve Dual", null, (s, e) => saDual_Solve.PerformClick());
            var miDualVerify = new ToolStripMenuItem("Verify Duality", null, (s, e) => saDual_Verify.PerformClick());
            mAnalysis.DropDownItems.AddRange(new ToolStripItem[] { miSensitivity, new ToolStripSeparator(), miShadow, new ToolStripSeparator(), miDualBuild, miDualSolve, miDualVerify });

            // View menu - directly navigate to tabs
            var miViewModelInput = new ToolStripMenuItem("Model Input", null, (s, e) => tabMain.SelectedTab = tabModelInput);
            var miViewCanonical = new ToolStripMenuItem("Canonical Form", null, (s, e) => tabMain.SelectedTab = tabCanonical);
            var miViewIterations = new ToolStripMenuItem("Iterations", null, (s, e) => tabMain.SelectedTab = tabIterations);
            var miViewResults = new ToolStripMenuItem("Results", null, (s, e) => tabMain.SelectedTab = tabResults);
            var miViewNodes = new ToolStripMenuItem("Node Explorer", null, (s, e) => tabMain.SelectedTab = tabNodes);
            var miViewCuts = new ToolStripMenuItem("Cuts", null, (s, e) => tabMain.SelectedTab = tabCuts);
            mView.DropDownItems.AddRange(new ToolStripItem[] { miViewModelInput, miViewCanonical, miViewIterations, miViewResults, miViewNodes, miViewCuts });

            // Help menu
            var miAbout = new ToolStripMenuItem("About", null, (s, e) => MessageBox.Show("LP/IP Solver GUI\nAssignment scaffolding (GUI only).", "About", MessageBoxButtons.OK, MessageBoxIcon.Information));
            mHelp.DropDownItems.Add(miAbout);

            // Add top-level menus
            mainMenu.Items.AddRange(new ToolStripItem[] { mFile, mSolve, mAnalysis, mView, mHelp });
            mainMenu.Dock = DockStyle.Top;

            Controls.Add(mainMenu);
            this.MainMenuStrip = mainMenu;
        }

        // Builds the status strip at the bottom
        private void BuildStatusStrip()
        {
            statusStrip = new StatusStrip();
            sbStatus = new ToolStripStatusLabel("Ready");
            sbAlgo = new ToolStripStatusLabel("Alg: Revised Simplex");
            sbIter = new ToolStripStatusLabel("Iter: 0");
            sbNode = new ToolStripStatusLabel("Node: –");
            statusStrip.Items.AddRange(new ToolStripItem[] { sbStatus, new ToolStripStatusLabel("|"), sbAlgo, new ToolStripStatusLabel("|"), sbIter, new ToolStripStatusLabel("|"), sbNode });
            statusStrip.Dock = DockStyle.Bottom;
            Controls.Add(statusStrip);
        }

        // Builds the Model Input tab where users can load, save, and define their optimization models.
        // This is typically the first tab users interact with to get their problem into the system.
        private void BuildTabModelInput()
        {
            tabModelInput = new TabPage("Model Input");

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20),
                BackColor = Color.Transparent
            };

            // Create a panel for the file operation buttons at the top
            var topPanel = new Panel { Height = 80, Dock = DockStyle.Top, BackColor = Color.Transparent };

            // Set up the three main action buttons
            btnLoadModelFile = new Button { Text = "Load Model From File", Width = 200, Height = 40, Top = 20 };
            btnSaveModelFile = new Button { Text = "Save Model To File", Width = 180, Height = 40, Top = 20 };
            btnValidateInput = new Button { Text = "Validate Input", Width = 160, Height = 40, Top = 20 };
            
            // Wire up the click events to our handler methods
            btnLoadModelFile.Click += BtnLoadModelFile_Click;
            btnSaveModelFile.Click += BtnSaveModelFile_Click;
            btnValidateInput.Click += BtnValidateInput_Click;

            // Create a method to center the buttons nicely when the window resizes
            void CenterTopButtons()
            {
                int totalWidth = btnLoadModelFile.Width + 20 + btnSaveModelFile.Width + 20 + btnValidateInput.Width;
                int startX = Math.Max(0, (topPanel.ClientSize.Width - totalWidth) / 2);
                btnLoadModelFile.Left = startX;
                btnSaveModelFile.Left = startX + btnLoadModelFile.Width + 20;
                btnValidateInput.Left = btnSaveModelFile.Left + btnSaveModelFile.Width + 20;
            }
            
            // Hook up the centering to happen whenever the panel resizes
            topPanel.Resize += (s, e) => CenterTopButtons();
            topPanel.Controls.AddRange(new Control[] { btnLoadModelFile, btnSaveModelFile, btnValidateInput });
            topPanel.CreateControl();
            CenterTopButtons(); // Center them initially

            // Add a helpful format explanation for users
            var formatPanel = new Panel { Height = 50, Dock = DockStyle.Top, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 0) };
            lblModelFormat = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Format: First line = max/min with signs & coefficients. Then constraints. Last line = sign restrictions.",
                TextAlign = ContentAlignment.MiddleLeft
            };
            formatPanel.Controls.Add(lblModelFormat);

            // Create dropdowns for problem type and objective sense
            var selectionPanel = new Panel { Height = 60, Dock = DockStyle.Top, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 10) };
            lblProblemType = new Label { Text = "Problem Type:", Left = 0, Top = 0, Width = 120, Height = 20, TextAlign = ContentAlignment.MiddleLeft };
            cmbProblemType = new ComboBox { Left = 0, Top = 25, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbProblemType.Items.AddRange(new[] { "LP", "Binary IP", "Knapsack" });
            cmbProblemType.SelectedIndex = 0; // Default to LP

            lblObjectiveSense = new Label { Text = "Objective Sense:", Left = 200, Top = 0, Width = 140, Height = 20, TextAlign = ContentAlignment.MiddleLeft };
            cmbObjectiveSense = new ComboBox { Left = 200, Top = 25, Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbObjectiveSense.Items.AddRange(new[] { "Maximize", "Minimize" });
            cmbObjectiveSense.SelectedIndex = 0; // Default to Maximize

            selectionPanel.Controls.AddRange(new Control[] { lblProblemType, cmbProblemType, lblObjectiveSense, cmbObjectiveSense });

            // Set up the objective function input grid
            var objPanel = new Panel { Height = 200, Dock = DockStyle.Top, BackColor = Color.Transparent, Padding = new Padding(0, 10, 0, 10) };
            var lblObjective = new Label { Text = "Objective Function (sign, coefficient per variable):", Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.BottomLeft };
            var spacerObjective = new Panel { Dock = DockStyle.Top, Height = 12 };
            dgvObjective = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = true, AllowUserToDeleteRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Margin = new Padding(0) };
            dgvObjective.Columns.Add("Var", "Variable");
            dgvObjective.Columns.Add("Sign", "Sign (+/-)");
            dgvObjective.Columns.Add("Coeff", "Coefficient");
            dgvObjective.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgvObjective.ColumnHeadersHeight = 40;
            objPanel.Controls.AddRange(new Control[] { dgvObjective, spacerObjective, lblObjective });

            // Set up the sign restrictions grid at the bottom
            var signPanel = new Panel { Dock = DockStyle.Bottom, Height = 140, Padding = new Padding(0, 10, 0, 0) };
            var lblSignRestrictions = new Label { Text = "Sign Restrictions (+, -, urs, int, bin):", Dock = DockStyle.Top, Height = 24, TextAlign = ContentAlignment.BottomLeft };
            dgvSignRestrictions = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
            dgvSignRestrictions.Columns.Add("VarName", "Variable");
            var colRestr = new DataGridViewComboBoxColumn { Name = "Restriction", HeaderText = "Restriction" };
            (colRestr as DataGridViewComboBoxColumn)!.Items.AddRange(new[] { "+", "-", "urs", "int", "bin" });
            dgvSignRestrictions.Columns.Add(colRestr);
            signPanel.Controls.AddRange(new Control[] { dgvSignRestrictions, lblSignRestrictions });

            // Set up the main constraints grid (this gets the remaining space)
            var constraintsPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent, Padding = new Padding(0, 16, 0, 0) };
            var lblConstraints = new Label { Text = "Constraints (signs, coefficients, relation, RHS):", Dock = DockStyle.Top, Height = 25, TextAlign = ContentAlignment.BottomLeft };
            var spacerConstraints = new Panel { Dock = DockStyle.Top, Height = 12 };
            dgvConstraints = new DataGridView { Dock = DockStyle.Fill, AllowUserToAddRows = true, AllowUserToDeleteRows = true, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, Margin = new Padding(0) };
            dgvConstraints.Columns.Add("CName", "Constraint Name");
            dgvConstraints.Columns.Add("TechSigns", "Signs (e.g. + + - ...)");
            dgvConstraints.Columns.Add("TechCoeffs", "Coefficients (e.g. 3 5 2 ...)");
            dgvConstraints.Columns.Add("Relation", "Relation (<=,=,>=)");
            dgvConstraints.Columns.Add("RHS", "Right Hand Side");
            dgvConstraints.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgvConstraints.ColumnHeadersHeight = 40;
            constraintsPanel.Controls.AddRange(new Control[] { dgvConstraints, spacerConstraints, lblConstraints });

            // Add all the panels to the main panel in the right order
            // The order matters because of the docking - last added gets the remaining space
            mainPanel.Controls.Add(constraintsPanel);    // Gets the remaining space (Fill)
            mainPanel.Controls.Add(signPanel);           // Bottom section
            mainPanel.Controls.Add(objPanel);            // Top section
            mainPanel.Controls.Add(selectionPanel);      // Top section  
            mainPanel.Controls.Add(formatPanel);         // Top section
            mainPanel.Controls.Add(topPanel);            // Very top

            // Apply consistent styling to all our controls
            ControlStyler.StyleButton(btnLoadModelFile, primary: true); // Make this one stand out
            ControlStyler.StyleButton(btnSaveModelFile);
            ControlStyler.StyleButton(btnValidateInput);
            ControlStyler.StyleLabel(lblModelFormat, muted: true); // Softer color for the help text
            ControlStyler.StyleLabel(lblProblemType);
            ControlStyler.StyleLabel(lblObjectiveSense);
            ControlStyler.StyleLabel(lblObjective);
            ControlStyler.StyleLabel(lblConstraints);
            ControlStyler.StyleLabel(lblSignRestrictions);
            ControlStyler.StyleCombo(cmbProblemType);
            ControlStyler.StyleCombo(cmbObjectiveSense);
            ControlStyler.StyleGrid(dgvObjective);
            ControlStyler.StyleGrid(dgvConstraints);
            ControlStyler.StyleGrid(dgvSignRestrictions);

            tabModelInput.Controls.Add(mainPanel);
        }

        // Builds the Algorithm tab
        private void BuildTabAlgorithm()
        {
            tabAlgorithm = new TabPage("Algorithm");

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.Transparent };

            var contentPanel = new Panel { Width = 600, Height = 520, Top = 20, Anchor = AnchorStyles.Top };
            mainPanel.Resize += (s, e) => { contentPanel.Left = Math.Max(0, (mainPanel.ClientSize.Width - contentPanel.Width) / 2); };

            grpAlgorithms = new StyledGroupPanel
            {
                Title = "Select Algorithm",
                Dock = DockStyle.Top,
                Height = 220,
                Margin = new Padding(0, 0, 0, 20),
                Padding = new Padding(16, 36, 16, 16),
                BackColor = Color.Transparent,
                TitleFont = AppTheme.Bold,
                TitleColor = AppTheme.Text,
                BorderColor = AppTheme.Border,
                BackgroundFill = AppTheme.Background,
                CornerRadius = 12
            };

            rbPrimalSimplex = new RadioButton { Text = "Primal Simplex", Left = 20, Top = 40, Width = 250, Height = 25, UseVisualStyleBackColor = true };
            rbRevisedPrimalSimplex = new RadioButton { Text = "Revised Primal Simplex", Left = 20, Top = 70, Width = 250, Height = 25, UseVisualStyleBackColor = true, Checked = true };
            rbBnBSimplex = new RadioButton { Text = "Branch && Bound (Simplex)", Left = 20, Top = 100, Width = 250, Height = 25, UseVisualStyleBackColor = true };
            rbCuttingPlane = new RadioButton { Text = "Cutting Plane", Left = 20, Top = 130, Width = 250, Height = 25, UseVisualStyleBackColor = true };
            rbBnBKnapsack = new RadioButton { Text = "Branch && Bound (Knapsack)", Left = 20, Top = 160, Width = 260, Height = 25, UseVisualStyleBackColor = true };
            grpAlgorithms.Controls.AddRange(new Control[] { rbPrimalSimplex, rbRevisedPrimalSimplex, rbBnBSimplex, rbCuttingPlane, rbBnBKnapsack });

            grpOptions = new StyledGroupPanel
            {
                Title = "Variable Type Options",
                Dock = DockStyle.Top,
                Height = 110,
                Margin = new Padding(0, 20, 0, 20),
                Padding = new Padding(16, 36, 16, 16),
                BackColor = Color.Transparent,
                TitleFont = AppTheme.Bold,
                TitleColor = AppTheme.Text,
                BorderColor = AppTheme.Border,
                BackgroundFill = AppTheme.Background,
                CornerRadius = 12
            };

            chkBinary = new CheckBox { Text = "Binary Variables", Left = 20, Top = 45, Width = 170, Height = 25, UseVisualStyleBackColor = true };
            chkGeneralInteger = new CheckBox { Text = "General Integer Variables", Left = 200, Top = 45, Width = 210, Height = 25, UseVisualStyleBackColor = true };
            grpOptions.Controls.AddRange(new Control[] { chkBinary, chkGeneralInteger });

            var buttonPanel = new Panel { Dock = DockStyle.Top, Height = 90, Padding = new Padding(0, 16, 0, 0) };
            btnSolve = new Button { Text = "Solve Problem", Width = 220, Height = 50, Top = 16 };

            void CenterSolve() => btnSolve.Left = Math.Max(0, (buttonPanel.ClientSize.Width - btnSolve.Width) / 2);
            buttonPanel.Resize += (s, e) => CenterSolve();
            buttonPanel.Controls.Add(btnSolve);
            buttonPanel.CreateControl();
            CenterSolve();

            contentPanel.Controls.Add(buttonPanel);
            contentPanel.Controls.Add(grpOptions);
            contentPanel.Controls.Add(grpAlgorithms);
            mainPanel.Controls.Add(contentPanel);

            // Style
            ControlStyler.StyleRadio(rbPrimalSimplex);
            ControlStyler.StyleRadio(rbRevisedPrimalSimplex);
            ControlStyler.StyleRadio(rbBnBSimplex);
            ControlStyler.StyleRadio(rbCuttingPlane);
            ControlStyler.StyleRadio(rbBnBKnapsack);
            ControlStyler.StyleCheckBox(chkBinary);
            ControlStyler.StyleCheckBox(chkGeneralInteger);
            ControlStyler.StyleButton(btnSolve, primary: true);

            btnSolve.Click += BtnSolve_Click;

            tabAlgorithm.Controls.Add(mainPanel);
        }

        // Builds the Canonical Form tab
        private void BuildTabCanonical()
        {
            tabCanonical = new TabPage("Canonical Form");

            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20),
            };
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

            dgvCanonicalTableau = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            dgvCanonicalTableau.Columns.Add("Basis", "Basis");
            dgvCanonicalTableau.Columns.Add("zj_cj", "zj - cj");
            dgvCanonicalTableau.Columns.Add("x1", "x1");
            dgvCanonicalTableau.Columns.Add("x2", "x2");
            dgvCanonicalTableau.Columns.Add("s1", "s1");
            dgvCanonicalTableau.Columns.Add("b", "b");

            rtbCanonicalNotes = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10F),
                Text = "Canonical form and initial tableau will appear here (GUI scaffold).\nAll values will be rounded to 3 decimals in exports."
            };

            ControlStyler.StyleGrid(dgvCanonicalTableau);

            mainPanel.Controls.Add(dgvCanonicalTableau, 0, 0);
            mainPanel.Controls.Add(rtbCanonicalNotes, 0, 1);
            tabCanonical.Controls.Add(mainPanel);
        }

        // Builds the Iterations tab
        private void BuildTabIterations()
        {
            tabIterations = new TabPage("Iterations");

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.Transparent };

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(0, 10, 0, 0) };
            btnExpandAll = new Button { Text = "Expand All Steps", Width = 160, Height = 40, Left = 0, Top = 20 };
            btnCollapseAll = new Button { Text = "Collapse All Steps", Width = 160, Height = 40, Left = 180, Top = 20 };
            btnExpandAll.Click += (s, e) => ExpandCollapseAll(true);
            btnCollapseAll.Click += (s, e) => ExpandCollapseAll(false);
            bottomPanel.Controls.Add(btnExpandAll);
            bottomPanel.Controls.Add(btnCollapseAll);

            lvIterations = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            lvIterations.Columns.Add("Step", 80);
            lvIterations.Columns.Add("Phase/Node", 160);
            lvIterations.Columns.Add("Description", 720);

            mainPanel.Controls.Add(bottomPanel);
            mainPanel.Controls.Add(lvIterations);

            ControlStyler.StyleButton(btnExpandAll);
            ControlStyler.StyleButton(btnCollapseAll);

            tabIterations.Controls.Add(mainPanel);
        }

        // Builds the Results tab
        private void BuildTabResults()
        {
            tabResults = new TabPage("Results");

            var mainPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.Transparent };

            var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(0, 10, 0, 0) };
            btnExportResults = new Button { Text = "Export Results to File", Width = 180, Height = 40, Left = 0, Top = 20 };
            lblRoundingNote = new Label { Text = "Note: All decimals exported to 3 dp.", AutoSize = true, Left = 220, Top = 28 };
            btnExportResults.Click += BtnExportResults_Click;
            bottomPanel.Controls.Add(btnExportResults);
            bottomPanel.Controls.Add(lblRoundingNote);

            rtbResultsSummary = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 10F),
                Margin = new Padding(0, 0, 0, 10)
            };

            mainPanel.Controls.Add(bottomPanel);
            mainPanel.Controls.Add(rtbResultsSummary);

            ControlStyler.StyleButton(btnExportResults, primary: true);
            ControlStyler.StyleLabel(lblRoundingNote, muted: true);

            tabResults.Controls.Add(mainPanel);
        }

        // Builds the Sensitivity tab
        private void BuildTabSensitivity()
        {
            tabSensitivity = new TabPage("Sensitivity");

            // Main scrollable container with better padding
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 20, 24, 20),
                BackColor = Color.Transparent
            };

            // Create main content container to hold all layouts
            var contentContainer = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Create main layout using TableLayoutPanel for better organization
            var mainLayout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 24)
            };

            // Configure column styles - equal width with gap
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            
            // Configure row styles - auto size
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Non-Basic Variable - improved layout (FIXED: replaced & with "and")
            pnlSA_VarNonBasic = CreateSAGroup("Non-Basic Variable: Range and Apply Change", 320);
            pnlSA_VarNonBasic.Margin = new Padding(0, 0, 12, 16);
            pnlSA_VarNonBasic.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var nbVarControls = CreateVariableSensitivityControls();
            saNB_VarSelect = nbVarControls.combo;
            saNB_ShowRange = nbVarControls.showRangeBtn;
            saNB_ApplyDelta = nbVarControls.deltaUpDown;
            saNB_Apply = nbVarControls.applyBtn;
            saNB_Output = nbVarControls.outputList;

            saNB_Apply.Text = "Apply Delta c";
            pnlSA_VarNonBasic.Controls.AddRange(new Control[] {
                saNB_VarSelect, saNB_ShowRange, saNB_ApplyDelta, saNB_Apply, saNB_Output
            });

            // Basic Variable - improved layout (FIXED: replaced & with "and")
            pnlSA_VarBasic = CreateSAGroup("Basic Variable: Range and Apply Change", 320);
            pnlSA_VarBasic.Margin = new Padding(12, 0, 0, 16);
            pnlSA_VarBasic.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var bVarControls = CreateVariableSensitivityControls();
            saB_VarSelect = bVarControls.combo;
            saB_ShowRange = bVarControls.showRangeBtn;
            saB_ApplyDelta = bVarControls.deltaUpDown;
            saB_Apply = bVarControls.applyBtn;
            saB_Output = bVarControls.outputList;

            saB_VarSelect.Items.AddRange(new[] { "x1", "x2", "x3" });
            saB_Apply.Text = "Apply Delta";
            pnlSA_VarBasic.Controls.AddRange(new Control[] {
                saB_VarSelect, saB_ShowRange, saB_ApplyDelta, saB_Apply, saB_Output
            });

            // RHS Constraint (FIXED: replaced & with "and")
            pnlSA_RHS = CreateSAGroup("Constraint RHS: Range and Apply Change", 320);
            pnlSA_RHS.Margin = new Padding(0, 0, 12, 16);
            pnlSA_RHS.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var rhsControls = CreateVariableSensitivityControls();
            saRHS_ConSelect = rhsControls.combo;
            saRHS_ShowRange = rhsControls.showRangeBtn;
            saRHS_ApplyDelta = rhsControls.deltaUpDown;
            saRHS_Apply = rhsControls.applyBtn;
            saRHS_Output = rhsControls.outputList;

            saRHS_ConSelect.Items.AddRange(new[] { "c1", "c2", "c3" });
            saRHS_Apply.Text = "Apply Delta b";
            pnlSA_RHS.Controls.AddRange(new Control[] {
                saRHS_ConSelect, saRHS_ShowRange, saRHS_ApplyDelta, saRHS_Apply, saRHS_Output
            });

            // Column analysis (FIXED: replaced & with "and")
            pnlSA_Column = CreateSAGroup("Non-Basic Column: Range and Coeff Changes", 320);
            pnlSA_Column.Margin = new Padding(12, 0, 0, 16);
            pnlSA_Column.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var colControls = CreateColumnSensitivityControls();
            saCol_VarSelect = colControls.combo;
            saCol_ShowRange = colControls.showRangeBtn;
            saCol_EditCoeffs = colControls.editBtn;
            saCol_Output = colControls.outputList;

            saCol_VarSelect.Items.AddRange(new[] { "x1", "x2", "x3" });
            pnlSA_Column.Controls.AddRange(new Control[] {
                saCol_VarSelect, saCol_ShowRange, saCol_EditCoeffs, saCol_Output
            });

            // Add to main layout (2x2 grid)
            mainLayout.Controls.Add(pnlSA_VarNonBasic, 0, 0);
            mainLayout.Controls.Add(pnlSA_VarBasic, 1, 0);
            mainLayout.Controls.Add(pnlSA_RHS, 0, 1);
            mainLayout.Controls.Add(pnlSA_Column, 1, 1);

            // Secondary operations section
            var secondaryLayout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                BackColor = Color.Transparent
            };

            secondaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            secondaryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            secondaryLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            secondaryLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Add Activity
            pnlSA_AddActivity = CreateSAGroup("Add New Activity (Column)", 320);
            pnlSA_AddActivity.Margin = new Padding(0, 0, 12, 16);
            pnlSA_AddActivity.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            saAddAct_Add = new Button { Text = "Add Activity…", Left = 20, Top = 50, Width = 180, Height = 40 };
            saAddAct_Output = CreateCompactOutputList(20, 110, 800, 160);
            pnlSA_AddActivity.Controls.AddRange(new Control[] { saAddAct_Add, saAddAct_Output });

            // Add Constraint
            pnlSA_AddConstraint = CreateSAGroup("Add New Constraint (Row)", 320);
            pnlSA_AddConstraint.Margin = new Padding(12, 0, 0, 16);
            pnlSA_AddConstraint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            saAddCon_Add = new Button { Text = "Add Constraint…", Left = 20, Top = 50, Width = 180, Height = 40 };
            saAddCon_Output = CreateCompactOutputList(20, 110, 800, 160);
            pnlSA_AddConstraint.Controls.AddRange(new Control[] { saAddCon_Add, saAddCon_Output });

            // Shadow Prices - Fixed button positioning and consistent textbox layout
            pnlSA_ShadowPrices = CreateSAGroup("Shadow Prices", 320);
            pnlSA_ShadowPrices.Margin = new Padding(0, 0, 12, 16);
            pnlSA_ShadowPrices.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            saShadow_Show = new Button { Text = "Display Shadow Prices", Left = 20, Top = 50, Width = 200, Height = 40 };
            saShadow_Output = CreateCompactOutputList(20, 110, 800, 160);
            
            // Connect the click event handler
            saShadow_Show.Click += SaShadow_Show_Click;

            pnlSA_ShadowPrices.Controls.AddRange(new Control[] { saShadow_Show, saShadow_Output });

            // Duality - Fixed button spacing and consistent textbox layout
            pnlSA_Duality = CreateSAGroup("Duality Analysis", 320);
            pnlSA_Duality.Margin = new Padding(12, 0, 0, 16);
            pnlSA_Duality.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            saDual_Build = new Button { Text = "Build Dual", Left = 20, Top = 50, Width = 120, Height = 36 };
            saDual_Solve = new Button { Text = "Solve Dual", Left = 150, Top = 50, Width = 120, Height = 36 };
            saDual_Verify = new Button { Text = "Verify Duality", Left = 280, Top = 50, Width = 140, Height = 36 };
            saDual_Output = CreateCompactOutputList(20, 110, 800, 160);
            
            // Connect the click event handlers
            saDual_Build.Click += SaDual_Build_Click;
            
            pnlSA_Duality.Controls.AddRange(new Control[] { saDual_Build, saDual_Solve, saDual_Verify, saDual_Output });

            // Add to secondary layout
            secondaryLayout.Controls.Add(pnlSA_AddActivity, 0, 0);
            secondaryLayout.Controls.Add(pnlSA_AddConstraint, 1, 0);
            secondaryLayout.Controls.Add(pnlSA_ShadowPrices, 0, 1);
            secondaryLayout.Controls.Add(pnlSA_Duality, 1, 1);

            // Add layouts to content container in correct order
            contentContainer.Controls.Add(secondaryLayout);
            contentContainer.Controls.Add(mainLayout);

            // Add content container to scroll panel
            scroll.Controls.Add(contentContainer);

            // Style all controls using the centralized styling approach
            StyleSensitivityControls();

            tabSensitivity.Controls.Add(scroll);
        }

        // Helper to create standardized variable sensitivity controls
        private (ComboBox combo, Button showRangeBtn, NumericUpDown deltaUpDown, Button applyBtn, ListView outputList) CreateVariableSensitivityControls()
        {
            var combo = new ComboBox
            {
                Left = 20, Top = 50, Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var showRangeBtn = new Button
            {
                Text = "Display Range", Left = 190, Top = 48, Width = 120, Height = 36
            };

            var deltaUpDown = new NumericUpDown
            {
                Left = 320, Top = 51, Width = 80, Height = 30,
                DecimalPlaces = 3, Minimum = -100000, Maximum = 100000,
                Font = AppTheme.Default
            };

            var applyBtn = new Button
            {
                Left = 410, Top = 48, Width = 130, Height = 36
            };

            var outputList = CreateOutputList(20, 100, 700, 180);

            return (combo, showRangeBtn, deltaUpDown, applyBtn, outputList);
        }

        // Helper to create column sensitivity controls
        private (ComboBox combo, Button showRangeBtn, Button editBtn, ListView outputList) CreateColumnSensitivityControls()
        {
            var combo = new ComboBox
            {
                Left = 20, Top = 50, Width = 160,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var showRangeBtn = new Button
            {
                Text = "Display Range", Left = 190, Top = 48, Width = 120, Height = 36
            };

            var editBtn = new Button
            {
                Text = "Edit Column Coeffs…", Left = 320, Top = 48, Width = 180, Height = 36
            };

            var outputList = CreateOutputList(20, 100, 700, 180);

            return (combo, showRangeBtn, editBtn, outputList);
        }

        // Helper to create a standardized output ListView
        private ListView CreateOutputList(int left, int top, int width, int height)
        {
            var lv = new ListView
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = AppTheme.Default
            };
            lv.Columns.Add("Result", width - 25);
            return lv;
        }

        // Helper to create a compact output ListView for secondary operations
        private ListView CreateCompactOutputList(int left, int top, int width, int height)
        {
            var lv = new ListView
            {
                Left = left,
                Top = top,
                Width = width,
                Height = height,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                Font = AppTheme.Default
            };
            lv.Columns.Add("Result", width - 25);
            return lv;
        }

        // Centralized styling for sensitivity controls
        private void StyleSensitivityControls()
        {
            // Style buttons with primary (blue) styling for better visibility
            foreach (var btn in new[] { 
                saNB_ShowRange, saNB_Apply, saB_ShowRange, saB_Apply, 
                saRHS_ShowRange, saRHS_Apply, saCol_ShowRange, saCol_EditCoeffs, 
                saAddAct_Add, saAddCon_Add, saShadow_Show, 
                saDual_Build, saDual_Solve, saDual_Verify 
            })
            {
                ControlStyler.StyleButton(btn, primary: true);
            }
            
            // Style combo boxes
            foreach (var combo in new[] { saNB_VarSelect, saB_VarSelect, saRHS_ConSelect, saCol_VarSelect })
            {
                ControlStyler.StyleCombo(combo);
            }
            
            // Style numeric up/down controls
            foreach (var nud in new[] { saNB_ApplyDelta, saB_ApplyDelta, saRHS_ApplyDelta })
            {
                ControlStyler.StyleNumericUpDown(nud);
            }
            
            // Style output lists
            foreach (var lv in new[] { 
                saNB_Output, saB_Output, saRHS_Output, saCol_Output, 
                saAddAct_Output, saAddCon_Output, saShadow_Output, saDual_Output 
            })
            {
                ControlStyler.StyleListView(lv);
            }
        }

        // Builds the Node Explorer tab
        private void BuildTabNodes()
        {
            tabNodes = new TabPage("Node Explorer");

            splitNodes = new SplitContainer { Dock = DockStyle.Fill, SplitterDistance = 320, Padding = new Padding(10), FixedPanel = FixedPanel.Panel1 };
            tvNodes = new TreeView { Dock = DockStyle.Fill };
            tvNodes.AfterSelect += (s, e) => { sbNode.Text = $"Node: {e.Node.Text}"; };

            nodeDetailsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            lblNodeTitle = new Label { Text = "Node: (select on left)", AutoSize = true, Top = 10, Left = 10, Font = AppTheme.Bold };
            lblNodeStatus = new Label { Text = "Status: –", AutoSize = true, Top = 40, Left = 10 };
            lblNodeBound = new Label { Text = "Bound: –", AutoSize = true, Top = 65, Left = 10 };
            lblNodeIncumbent = new Label { Text = "Best Incumbent: –", AutoSize = true, Top = 90, Left = 10 };

            lvNodeIterations = new ListView { View = View.Details, Dock = DockStyle.Bottom, Height = 380, FullRowSelect = true, GridLines = true };
            lvNodeIterations.Columns.Add("Iter", 60);
            lvNodeIterations.Columns.Add("Action", 160);
            lvNodeIterations.Columns.Add("Details", 480);

            nodeDetailsPanel.Controls.AddRange(new Control[] { lblNodeTitle, lblNodeStatus, lblNodeBound, lblNodeIncumbent, lvNodeIterations });

            splitNodes.Panel1.Controls.Add(tvNodes);
            splitNodes.Panel2.Controls.Add(nodeDetailsPanel);

            // Seed with placeholders
            var root = tvNodes.Nodes.Add("Root");
            root.Nodes.Add("x1 = 1");
            root.Nodes.Add("x1 = 2");
            root.Expand();

            tabNodes.Controls.Add(splitNodes);
        }

        // Builds the Cuts tab
        private void BuildTabCuts()
        {
            tabCuts = new TabPage("Cuts");

            var pan = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            lvCuts = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true };
            lvCuts.Columns.Add("Step", 60);
            lvCuts.Columns.Add("Cut Type", 160);
            lvCuts.Columns.Add("Expression", 520);

            var bottom = new Panel { Dock = DockStyle.Bottom, Height = 72 };
            btnAddCut = new Button { Text = "Add Cut…", Left = 0, Top = 16, Width = 120, Height = 36 };
            btnClearCuts = new Button { Text = "Clear", Left = 150, Top = 16, Width = 100, Height = 36 };
            ControlStyler.StyleButton(btnAddCut);
            ControlStyler.StyleButton(btnClearCuts);
            bottom.Controls.AddRange(new Control[] { btnAddCut, btnClearCuts });

            pan.Controls.Add(bottom);
            pan.Controls.Add(lvCuts);
            tabCuts.Controls.Add(pan);
        }

        // -------------------- Helpers --------------------

        // Applies the theme to the main form and menu/status controls
        private void ApplyTheme()
        {
            BackColor = AppTheme.Background;
            ForeColor = AppTheme.Text;
            Font = AppTheme.Default;

            if (mainMenu != null) { mainMenu.BackColor = AppTheme.Card; mainMenu.ForeColor = AppTheme.Text; }
            if (statusStrip != null) { statusStrip.BackColor = AppTheme.Card; statusStrip.ForeColor = AppTheme.Text; }

            tabMain.Padding = new Point(0, 0);
        }

        // Sets the algorithm status in the status bar
        private void SetAlgoStatus(string name) => sbAlgo.Text = $"Alg: {name}";

        // Gets the selected algorithm name from radio buttons
        private string GetSelectedAlgorithmName()
        {
            if (rbPrimalSimplex.Checked) return "Primal Simplex";
            if (rbRevisedPrimalSimplex.Checked) return "Revised Primal Simplex";
            if (rbBnBSimplex.Checked) return "Branch & Bound (Simplex)";
            if (rbCuttingPlane.Checked) return "Cutting Plane";
            if (rbBnBKnapsack.Checked) return "Branch & Bound (Knapsack)";
            return "Unknown";
        }

        // Generates model text from the UI (placeholder)
        private string GenerateModelText()
        {
            var sb = new System.Text.StringBuilder();
            // Objective line
            sb.Append(cmbObjectiveSense.SelectedIndex == 0 ? "max" : "min");
            foreach (DataGridViewRow row in dgvObjective.Rows)
            {
                if (row.IsNewRow) continue;
                string sign = row.Cells["Sign"].Value?.ToString() ?? "+";
                string coeff = row.Cells["Coeff"].Value?.ToString() ?? "0";
                sb.Append($" {sign}{coeff}");
            }
            sb.AppendLine();
            // Constraints
            foreach (DataGridViewRow row in dgvConstraints.Rows)
            {
                if (row.IsNewRow) continue;
                string signs = row.Cells["TechSigns"].Value?.ToString() ?? "";
                string coeffs = row.Cells["TechCoeffs"].Value?.ToString() ?? "";
                string relation = row.Cells["Relation"].Value?.ToString() ?? "";
                string rhs = row.Cells["RHS"].Value?.ToString() ?? "0";
                // Compose sign+coeff pairs
                var signParts = signs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var coeffParts = coeffs.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var pairs = new List<string>();
                for (int i = 0; i < Math.Min(signParts.Length, coeffParts.Length); i++)
                {
                    pairs.Add($"{signParts[i]}{coeffParts[i]}");
                }
                sb.Append(string.Join(" ", pairs));
                sb.Append($" {relation} {rhs}\n");
            }
            // Sign restrictions
            foreach (DataGridViewRow row in dgvSignRestrictions.Rows)
            {
                if (row.IsNewRow) continue;
                string restriction = row.Cells["Restriction"].Value?.ToString() ?? "+";
                sb.Append($"{restriction} ");
            }
            return sb.ToString().TrimEnd();
        }

        // Expands or collapses all iteration steps (placeholder)
        private void ExpandCollapseAll(bool expand)
        {
            MessageBox.Show(expand ? "Expanded all iteration steps." : "Collapsed all iteration steps.",
                            "Iterations", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ---------- Styled Group Panel ----------
        internal class StyledGroupPanel : Panel
        {
            public string Title { get; set; } = "";
            public Font TitleFont { get; set; } = SystemFonts.DefaultFont;
            public Color TitleColor { get; set; } = Color.Black;
            public Color BorderColor { get; set; } = Color.Gray;
            public Color BackgroundFill { get; set; } = Color.White;
            public int CornerRadius { get; set; } = 8;

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);

                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var titleSize = TextRenderer.MeasureText(Title, TitleFont);
                int titlePadX = 20;
                int titlePadY = 8;

                int borderTop = titlePadY + (titleSize.Height / 2);
                var bodyRect = new Rectangle(1, borderTop, Width - 2, Height - borderTop - 1);

                using (var path = GraphicsHelper.CreateRoundRectPath(bodyRect, CornerRadius))
                {
                    using (var fill = new SolidBrush(BackgroundFill))
                        e.Graphics.FillPath(fill, path);

                    using (var pen = new Pen(BorderColor, 1.5f))
                        e.Graphics.DrawPath(pen, path);
                }

                var titleBgRect = new Rectangle(titlePadX - 4, titlePadY - 2, titleSize.Width + 8, titleSize.Height + 4);
                using (var bg = new SolidBrush(Parent?.BackColor ?? BackgroundFill))
                    e.Graphics.FillRectangle(bg, titleBgRect);

                TextRenderer.DrawText(e.Graphics, Title, TitleFont,
                    new Point(titlePadX, titlePadY), TitleColor,
                    TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPadding);
            }
        }

        // -------------------- Event Handlers (bottom) --------------------

        // Handles loading a model file
        private void BtnLoadModelFile_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Title = "Load LP/IP Model File"
            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _rawModelText = File.ReadAllText(ofd.FileName);
                    ValidateAndLoadModel();
                    MessageBox.Show($"Model file loaded successfully!\n\nFile: {Path.GetFileName(ofd.FileName)}",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    sbStatus.Text = "Model loaded.";
                }
                catch (ModelValidationException mvex)
                {
                    MessageBox.Show($"Invalid model format: {mvex.Message}",
                                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    sbStatus.Text = "Model validation failed.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading file: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    sbStatus.Text = "Model load failed.";
                }
            }
        }

        /// <summary>
        /// Validates and loads a parsed model into the UI components.
        /// This method takes the raw model text and populates all the data grids with the model information.
        /// It's the bridge between the text-based model format and the visual interface.
        /// </summary>
        private void ValidateAndLoadModel()
        {
            // Parse and validate the model using our ModelParser service
            _currentModel = _modelParser.ParseModel(_rawModelText);

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

            // Now that we have a valid model, enable the buttons that need it
            btnValidateInput.Enabled = true;
            btnSolve.Enabled = true;
            
            // Update sensitivity analysis dropdowns
            UpdateVariableDropdowns();
        }

        // Handles saving a model file
        private void BtnSaveModelFile_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                FileName = "model.txt",
                Title = "Save LP/IP Model File"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string modelText = GenerateModelText(); // TODO
                    File.WriteAllText(sfd.FileName, modelText);
                    MessageBox.Show($"Model saved successfully!\n\nFile: {Path.GetFileName(sfd.FileName)}",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    sbStatus.Text = "Model saved.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Handles input validation
        private void BtnValidateInput_Click(object? sender, EventArgs e)
        {
            try
            {
                string modelText = GenerateModelText();
                var model = _modelParser.ParseModel(modelText);
                MessageBox.Show("Model format is valid.", "Validate Input", MessageBoxButtons.OK, MessageBoxIcon.Information);
                sbStatus.Text = "Input validated.";
            }
            catch (ModelValidationException mvex)
            {
                MessageBox.Show($"Invalid model format: {mvex.Message}",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Validation failed.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating model: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Validation error.";
            }
        }

        // Handles solving the problem
        private void BtnSolve_Click(object sender, EventArgs e)
        {
            try
            {
                rtbResultsSummary.Clear();
                rtbResultsSummary.AppendText("=== LP/IP SOLVER RESULTS ===\n\n");
                rtbResultsSummary.AppendText($"Problem Type: {cmbProblemType.SelectedItem}\n");
                rtbResultsSummary.AppendText($"Objective Sense: {cmbObjectiveSense.SelectedItem}\n");
                rtbResultsSummary.AppendText($"Algorithm: {GetSelectedAlgorithmName()}\n");
                rtbResultsSummary.AppendText($"Binary Variables: {(chkBinary.Checked ? "Yes" : "No")}\n");
                rtbResultsSummary.AppendText($"Integer Variables: {(chkGeneralInteger.Checked ? "Yes" : "No")}\n\n");
                rtbResultsSummary.AppendText("STATUS: Ready to solve (GUI scaffold)\n");
                rtbResultsSummary.AppendText("OPTIMAL VALUE: [To be calculated]\n");
                rtbResultsSummary.AppendText("SOLUTION VECTOR: [To be calculated]\n\n");
                rtbResultsSummary.AppendText("Note: Algorithm implementation in progress...\n");

                sbStatus.Text = "Solve started (placeholder).";
                sbIter.Text = "Iter: 0";
                tabMain.SelectedTab = tabResults;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during solving: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Handles exporting results
        private void BtnExportResults_Click(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Text files(*.txt) | *.txt",
                FileName = "results.txt",
                Title = "Export Results"
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, rtbResultsSummary.Text);
                    MessageBox.Show($"Results exported successfully!\n\nFile: {Path.GetFileName(sfd.FileName)}",
                                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    sbStatus.Text = "Results exported.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting results: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // Event handler for shadow price calculation
        private void SaShadow_Show_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before calculating shadow prices.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Calculate shadow prices
                var result = _shadowPriceCalculator.CalculateShadowPrices(_currentModel);

                // Clear previous results
                saShadow_Output.Items.Clear();

                // Configure ListView for shadow price display
                saShadow_Output.View = View.Details;
                saShadow_Output.Columns.Clear();
                saShadow_Output.Columns.Add("Constraint", 120);
                saShadow_Output.Columns.Add("Shadow Price", 150);
                saShadow_Output.Columns.Add("Status", 80);
                saShadow_Output.Columns.Add("Interpretation", 700);

                // Add shadow price results
                foreach (var shadowPrice in result.ShadowPrices)
                {
                    var item = new ListViewItem(shadowPrice.ConstraintName);
                    item.SubItems.Add(shadowPrice.Value.ToString("F3"));
                    item.SubItems.Add(shadowPrice.IsActive ? "Active" : "Inactive");
                    item.SubItems.Add(shadowPrice.Interpretation);
                    saShadow_Output.Items.Add(item);
                }

                // Add summary information
                var summaryItem = new ListViewItem("SUMMARY");
                summaryItem.SubItems.Add($"Optimal: {result.OptimalValue:F2}");
                summaryItem.SubItems.Add("Info");
                summaryItem.SubItems.Add(result.Notes);
                summaryItem.Font = new Font(saShadow_Output.Font, FontStyle.Bold);
                saShadow_Output.Items.Add(summaryItem);

                // Update status
                sbStatus.Text = $"Shadow prices calculated for {result.ShadowPrices.Count} constraints.";

                MessageBox.Show($"Shadow prices calculated successfully!\n\nFound {result.ShadowPrices.Count} constraints.\nOptimal value: {result.OptimalValue:F2}", 
                    "Shadow Prices", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating shadow prices: {ex.Message}", 
                    "Calculation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                sbStatus.Text = "Shadow price calculation failed.";
            }
        }

        // Event handler for dual model generation
        private void SaDual_Build_Click(object sender, EventArgs e)
        {
            try
            {
                if (_currentModel == null)
                {
                    MessageBox.Show("Please load a model first before building the dual model.", 
                        "No Model Loaded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var result = _dualModelGenerator.GenerateDualModel(_currentModel);
                saDual_Output.Items.Clear();
                saDual_Output.View = View.Details;
                saDual_Output.Columns.Clear();
                saDual_Output.Columns.Add("Component", 120);
                saDual_Output.Columns.Add("Details", 650);

                var headerItem = new ListViewItem("DUAL MODEL");
                headerItem.SubItems.Add($"Generated from primal ({_currentModel.Sense} -> {result.DualModel.Sense})");
                headerItem.Font = new Font(saDual_Output.Font, FontStyle.Bold);
                saDual_Output.Items.Add(headerItem);

                var objItem = new ListViewItem("Objective");
                objItem.SubItems.Add(result.DualModel.FormattedObjective);
                saDual_Output.Items.Add(objItem);

                foreach (var constraint in result.DualModel.Constraints)
                {
                    var constItem = new ListViewItem("Constraint");
                    var coeffs = constraint.Coefficients.Where(kv => kv.Value != 0).Select(kv =>
                        kv.Value == 1 ? kv.Key : $"{kv.Value}{kv.Key}");
                    var relationStr = constraint.Relation switch
                    {
                        ConstraintRelation.LessThanEqual => "<=",
                        ConstraintRelation.Equal => "=",
                        ConstraintRelation.GreaterThanEqual => ">=",
                        _ => "?"

                    };
                    var constraintText = $"{string.Join(" + ", coeffs)} {relationStr} {constraint.RHS}";
                    constItem.SubItems.Add(constraintText);
                    saDual_Output.Items.Add(constItem);
                }

                var varsItem = new ListViewItem("Variables");
                var varRestrictions = result.DualModel.Variables.Values.OrderBy(v => v.Index).Select(v =>
                    $"{v.Name} {(v.SignRestriction == SignRestriction.Positive ? ">= 0" : "urs")}");
                varsItem.SubItems.Add(string.Join(", ", varRestrictions));
                saDual_Output.Items.Add(varsItem);

                sbStatus.Text = "Dual model built successfully.";
                MessageBox.Show($"Dual model generated!\nDual: {result.DualModel.Variables.Count} vars, {result.DualModel.Constraints.Count} constraints", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Helper to create a styled group panel for sensitivity analysis
        private StyledGroupPanel CreateSAGroup(string title, int height)
        {
            return new StyledGroupPanel
            {
                Title = title,
                Height = height,
                Padding = new Padding(20, 45, 20, 20),
                TitleFont = AppTheme.Bold,
                TitleColor = AppTheme.Text,
                BorderColor = AppTheme.Border,
                BackgroundFill = AppTheme.Card,
                CornerRadius = 12
            };
        }
    }

    internal class HiddenTabControl : TabControl
    {
        public HiddenTabControl()
        {
            // Hide headers by making them 1px tall; fixed sizing prevents WinForms from recalculating.
            this.Appearance = TabAppearance.Buttons;
            this.ItemSize = new Size(0, 1);
            this.SizeMode = TabSizeMode.Fixed;
            this.Multiline = true;
            this.Padding = new Point(0, 0);
        }

        protected override void OnKeyDown(KeyEventArgs ke)
        {
            // Optional: prevent Ctrl+Tab cycling since headers are hidden
            if (ke.Control && (ke.KeyCode == Keys.Tab)) { ke.Handled = true; return; }
            base.OnKeyDown(ke);
        }
    }
}
