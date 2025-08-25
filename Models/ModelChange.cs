namespace LPR381_Assignment.Models
{
    /// <summary>
    /// Represents a change to be applied to a model for what-if analysis
    /// </summary>
    public class ModelChange
    {
        public ChangeType ChangeType { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public string VariableName { get; set; } = string.Empty;  // For constraint coefficient changes
        public string ConstraintName { get; set; } = string.Empty;  // For constraint coefficient changes
        public double Delta { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Priority { get; set; } = 1; // For ordering changes
        
        /// <summary>
        /// Creates a new model change for objective coefficient modification
        /// </summary>
        public static ModelChange CreateObjectiveCoefficientChange(string variableName, double delta, string description = "")
        {
            return new ModelChange
            {
                ChangeType = ChangeType.ObjectiveCoefficient,
                TargetName = variableName,
                Delta = delta,
                Description = string.IsNullOrEmpty(description) ? $"Change {variableName} coefficient by {delta:F3}" : description
            };
        }
        
        /// <summary>
        /// Creates a new model change for constraint RHS modification
        /// </summary>
        public static ModelChange CreateConstraintRHSChange(string constraintName, double delta, string description = "")
        {
            return new ModelChange
            {
                ChangeType = ChangeType.ConstraintRHS,
                TargetName = constraintName,
                Delta = delta,
                Description = string.IsNullOrEmpty(description) ? $"Change {constraintName} RHS by {delta:F3}" : description
            };
        }
        
        /// <summary>
        /// Creates a new model change for constraint coefficient modification
        /// </summary>
        public static ModelChange CreateConstraintCoefficientChange(string constraintName, string variableName, double delta, string description = "")
        {
            return new ModelChange
            {
                ChangeType = ChangeType.ConstraintCoefficient,
                ConstraintName = constraintName,
                VariableName = variableName,
                TargetName = $"{constraintName}[{variableName}]",
                Delta = delta,
                Description = string.IsNullOrEmpty(description) ? $"Change {variableName} coefficient in {constraintName} by {delta:F3}" : description
            };
        }
        
        /// <summary>
        /// Returns a string representation of the change
        /// </summary>
        public override string ToString()
        {
            return !string.IsNullOrEmpty(Description) ? Description : $"{ChangeType}: {TargetName} Delta={Delta:F3}";
        }
    }
    
    /// <summary>
    /// Types of changes that can be applied to a model
    /// </summary>
    public enum ChangeType
    {
        ObjectiveCoefficient,    // Change a variable's objective function coefficient
        ConstraintRHS,          // Change a constraint's right-hand side value
        ConstraintCoefficient,  // Change a variable's coefficient in a constraint
        AddVariable,            // Add a new variable to the model
        AddConstraint,          // Add a new constraint to the model
        RemoveVariable,         // Remove a variable from the model
        RemoveConstraint        // Remove a constraint from the model
    }
}