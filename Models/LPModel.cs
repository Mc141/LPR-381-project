using System;
using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    // Represents variable sign restrictions
    public enum SignRestriction
    {
        Positive,     // +
        Negative,     // -
        Unrestricted, // urs
        Integer,      // int
        Binary        // bin
    }

    // Represents constraint relations
    public enum ConstraintRelation
    {
        LessThanEqual,    // <=
        Equal,            // =
        GreaterThanEqual  // >=
    }

    // Represents the objective sense
    public enum ObjectiveSense
    {
        Maximize,
        Minimize
    }

    // Represents a variable in the model
    public class Variable
    {
        public string Name { get; set; }
        public SignRestriction SignRestriction { get; set; }
        public double Coefficient { get; set; }
        public int Index { get; set; }

        public override string ToString() => $"{(Coefficient >= 0 ? "+" : "")}{Coefficient} {Name}";
    }

    // Represents a constraint in the model
    public class Constraint
    {
        public string Name { get; set; }
        public Dictionary<string, double> Coefficients { get; set; } = new();
        public ConstraintRelation Relation { get; set; }
        public double RHS { get; set; }

        public override string ToString()
        {
            var terms = Coefficients.Select(kvp => $"{(kvp.Value >= 0 ? "+" : "")}{kvp.Value} {kvp.Key}");
            var relStr = Relation switch
            {
                ConstraintRelation.LessThanEqual => "<=",
                ConstraintRelation.Equal => "=",
                ConstraintRelation.GreaterThanEqual => ">=",
                _ => "?"
            };
            return $"{string.Join(" ", terms)} {relStr} {RHS}";
        }
    }

    // Main model class
    public class LPModel
    {
        public ObjectiveSense Sense { get; set; }
        public Dictionary<string, Variable> Variables { get; set; } = new();
        public List<Constraint> Constraints { get; set; } = new();
        public bool HasIntegerVariables => Variables.Values.Any(v => v.SignRestriction is SignRestriction.Integer or SignRestriction.Binary);
        
        public string GetFormattedObjective()
        {
            var terms = Variables.Values.OrderBy(v => v.Index).Select(v => v.ToString());
            return $"{(Sense == ObjectiveSense.Maximize ? "max" : "min")} {string.Join(" ", terms)}";
        }
    }
}