using System.Collections.Generic;

namespace LPR381_Assignment.Models
{
    // Represents a dual LP model
    public class DualModel
    {
        public ObjectiveSense Sense { get; set; }
        public Dictionary<string, DualVariable> Variables { get; set; } = new();
        public List<DualConstraint> Constraints { get; set; } = new();
        public string FormattedObjective { get; set; } = string.Empty;
        public List<string> TransformationSteps { get; set; } = new();
        public string OriginalModelSummary { get; set; } = string.Empty;
    }

    // Represents a variable in the dual model
    public class DualVariable
    {
        public string Name { get; set; } = string.Empty;
        public double Coefficient { get; set; }
        public SignRestriction SignRestriction { get; set; }
        public string CorrespondingPrimalConstraint { get; set; } = string.Empty;
        public int Index { get; set; }

        public override string ToString()
        {
            return $"{Name} (coeff: {Coefficient}, from: {CorrespondingPrimalConstraint})";
        }
    }

    // Represents a constraint in the dual model
    public class DualConstraint
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, double> Coefficients { get; set; } = new();
        public ConstraintRelation Relation { get; set; }
        public double RHS { get; set; }
        public string CorrespondingPrimalVariable { get; set; } = string.Empty;

        public override string ToString()
        {
            var coeffStr = string.Join(" + ", Coefficients.Select(kv => $"{kv.Value}{kv.Key}"));
            var relationStr = Relation switch
            {
                ConstraintRelation.LessThanEqual => "<=",
                ConstraintRelation.Equal => "=",
                ConstraintRelation.GreaterThanEqual => ">=",
                _ => "?"
            };
            return $"{coeffStr} {relationStr} {RHS}";
        }
    }

    // Result of dual model generation
    public class DualModelResult
    {
        public DualModel DualModel { get; set; } = new();
        public string Status { get; set; } = string.Empty;
        public List<string> TransformationSteps { get; set; } = new();
        public string FormattedDualModel { get; set; } = string.Empty;
        public string ComparisonSummary { get; set; } = string.Empty;
        public bool IsValidTransformation { get; set; } = true;
    }
}