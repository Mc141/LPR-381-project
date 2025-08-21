# LPR381 Assignment - Linear Programming Solver GUI

A comprehensive Linear Programming and Integer Programming solver with a modern Windows Forms GUI, built for the LPR381 Operations Research course.

## Table of Contents

- [Features](#features)
- [Prerequisites](#prerequisites)
- [Installation](#installation)
- [Usage](#usage)
- [Architecture](#architecture)
- [Algorithm Support](#algorithm-support)
- [Project Structure](#project-structure)
- [Contributing](#contributing)
- [Documentation](#documentation)

## Features

### Core Functionality
- **Multiple Algorithm Support**: Primal Simplex, Revised Primal Simplex, Branch & Bound, Cutting Plane
- **Problem Types**: Linear Programming (LP), Binary Integer Programming, Knapsack Problems
- **Model Input/Output**: Load and save models from text files
- **Results Export**: Export solving results to formatted text files
- **Real-time Validation**: Validate model format before solving

### Advanced Analysis
- **Sensitivity Analysis**: 
  - Range analysis for objective coefficients
  - RHS constraint sensitivity
  - Shadow price calculation
  - Variable range analysis (basic and non-basic)
- **Duality Analysis**: 
  - Automatic dual problem construction
  - Dual problem solving
  - Strong/weak duality verification
- **Interactive Features**:
  - Add new activities (columns)
  - Add new constraints (rows)
  - Coefficient modification

### User Interface
- **Modern Design**: Clean, professional interface with custom styling
- **Tabbed Navigation**: Organized workflow across multiple tabs
- **Responsive Layout**: Adaptive UI that works across different screen sizes
- **Real-time Feedback**: Status bar with algorithm, iteration, and node information

## Prerequisites

- **Operating System**: Windows 10/11 (Windows Forms requirement)
- **.NET 8 SDK**: [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
- **IDE**: Visual Studio 2022 (recommended) or Visual Studio Code with C# extension

## Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/LPR381-Assignment.git
   cd LPR381-Assignment
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build --configuration Release
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

## Usage

### Loading a Model
1. Click **File > Open Model** or use the "Load Model From File" button
2. Select a `.txt` file containing your LP/IP model
3. The model will be automatically parsed and loaded into the interface

### Model Format
The application expects models in the following format:
```
max +3x1 +2x2
+1x1 +1x2 <= 4
+2x1 +1x2 <= 6
+ + 
```

- **First line**: Objective function (max/min followed by coefficients)
- **Middle lines**: Constraints with coefficients, relation (<=, =, >=), and RHS
- **Last line**: Sign restrictions (+, -, urs, int, bin for each variable)

### Solving Problems
1. Navigate to the **Algorithm** tab
2. Select your preferred solving algorithm
3. Configure variable type options (binary, integer)
4. Click **Solve Problem**
5. View results in the **Results** tab

### Sensitivity Analysis
1. Navigate to the **Sensitivity** tab after solving
2. Use the various panels for different types of analysis:
   - **Variable Analysis**: Range and delta applications
   - **Constraint Analysis**: RHS sensitivity
   - **Shadow Prices**: Resource value analysis
   - **Duality**: Dual problem construction and verification

### Key Components

```
LPR381-Assignment/
├── Models/                 # Data models and business entities
│   └── LPModel.cs         # Core model representation
├── Services/              # Business logic and algorithms
│   └── ModelParser.cs     # Model parsing and validation
├── UI/
│   ├── Builders/          # UI construction classes
│   │   ├── SensitivityUIBuilder.cs
│   │   └── MenuAndStatusBuilder.cs
│   ├── Controls/          # Custom UI controls
│   │   ├── StyledGroupPanel.cs
│   │   └── HiddenTabControl.cs
│   ├── Helpers/           # UI utility classes
│   │   ├── ControlStyler.cs
│   │   └── GraphicsHelper.cs
│   └── Themes/            # Styling and theming
│       └── AppTheme.cs
├── EventHandlers/         # Separated event handling logic
│   └── MainFormEventHandlers.cs
└── TabbedMainForm.cs      # Main form coordinator
```

## Algorithm Support

### Implemented Algorithms
- **Primal Simplex Method**: Standard simplex algorithm for LP problems
- **Revised Primal Simplex**: Matrix-based simplex for improved numerical stability
- **Branch & Bound (Simplex)**: Integer programming using simplex relaxations
- **Cutting Plane Method**: Integer programming using cutting planes
- **Branch & Bound (Knapsack)**: Specialized algorithm for knapsack problems

### Problem Types
- **Linear Programming (LP)**: Continuous variable optimization
- **Binary Integer Programming**: 0-1 variable optimization
- **Mixed Integer Programming**: Combination of continuous and integer variables
- **Knapsack Problems**: Resource allocation optimization

## Project Structure

### Core Classes

#### `TabbedMainForm`
The main application window that coordinates all UI components and user interactions.

#### `ModelParser`
Responsible for parsing text-based model files into internal model representations.

#### `LPModel`
Data structure representing a linear programming model with variables, constraints, and objective function.

#### `SensitivityUIBuilder`
Builds sensitivity analysis UI components following the Single Responsibility Principle.

#### `ControlStyler`
Provides consistent styling across all UI controls for a professional appearance.

### Custom Controls

#### `StyledGroupPanel`
A custom panel with rounded borders and title rendering for organized UI sections.

#### `HiddenTabControl`
A modified TabControl that hides tab headers for a cleaner navigation experience.