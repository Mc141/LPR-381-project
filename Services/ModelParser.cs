using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services
{
    public class ModelValidationException : Exception
    {
        public ModelValidationException(string message) : base(message) { }
    }

    public class ModelParser
    {
        // Regex for relation
        private static readonly Regex RelationRegex = new(@"(<=|>=|=)");

        public LPModel ParseModel(string modelText)
        {
            var lines = modelText.Split('\n')
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lines.Count < 3)
                throw new ModelValidationException("Model must contain at least an objective, one constraint, and sign restrictions.");

            var model = new LPModel();
            var variables = new List<string>();

            // Parse objective line
            ParseObjectiveLine(lines[0], model, variables);

            // Parse constraints
            int signRestrictionLineIdx = lines.Count - 1;
            for (int i = 1; i < signRestrictionLineIdx; i++)
                ParseConstraintLine(lines[i], model, variables, i);

            // Parse sign restrictions
            ParseSignRestrictionsLine(lines[signRestrictionLineIdx], model, variables);

            return model;
        }

        private void ParseObjectiveLine(string line, LPModel model, List<string> variables)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new ModelValidationException("Objective line must contain 'max' or 'min' and at least one variable.");

            model.Sense = parts[0].ToLower() == "max" ? ObjectiveSense.Maximize : parts[0].ToLower() == "min" ? ObjectiveSense.Minimize : throw new ModelValidationException("Objective must start with 'max' or 'min'.");

            int varIndex = 0;
            for (int i = 1; i < parts.Length; i++)
            {
                string signCoeff = parts[i];
                // Match sign and coefficient
                var match = Regex.Match(signCoeff, @"^([+-])?(\d+(\.\d+)?)$");
                if (!match.Success)
                    throw new ModelValidationException($"Invalid objective variable format: '{signCoeff}'");
                var sign = match.Groups[1].Value;
                var coeffStr = match.Groups[2].Value;
                double coeff = double.Parse(coeffStr);
                if (sign == "-") coeff = -coeff;
                // Variable name: x{index+1}
                string varName = $"x{varIndex + 1}";
                model.Variables[varName] = new Variable
                {
                    Name = varName,
                    Coefficient = coeff,
                    Index = varIndex
                };
                variables.Add(varName);
                varIndex++;
            }
        }

        private void ParseConstraintLine(string line, LPModel model, List<string> variables, int constraintIdx)
        {
            // Remove extra spaces and split
            var parts = line.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            int nVars = variables.Count;
            if (parts.Length < nVars + 2)
                throw new ModelValidationException($"Constraint line does not have enough values: '{line}'");

            // First nVars: sign+coeffs
            var coeffs = new double[nVars];
            for (int i = 0; i < nVars; i++)
            {
                string signCoeff = parts[i];
                var match = Regex.Match(signCoeff, @"^([+-])?(\d+(\.\d+)?)$");
                if (!match.Success)
                    throw new ModelValidationException($"Invalid constraint variable format: '{signCoeff}'");
                var sign = match.Groups[1].Value;
                var coeffStr = match.Groups[2].Value;
                double coeff = double.Parse(coeffStr);
                if (sign == "-") coeff = -coeff;
                coeffs[i] = coeff;
            }
            // Next: relation and RHS
            string relation = parts[nVars];
            if (!RelationRegex.IsMatch(relation))
                throw new ModelValidationException($"Invalid constraint relation: '{relation}'");
            string rhsStr = parts[nVars + 1];
            if (!double.TryParse(rhsStr, out double rhs))
                throw new ModelValidationException($"Invalid constraint RHS: '{rhsStr}'");

            var constraint = new Constraint
            {
                Name = $"c{constraintIdx}",
                Relation = relation switch
                {
                    "<=" => ConstraintRelation.LessThanEqual,
                    ">=" => ConstraintRelation.GreaterThanEqual,
                    "=" => ConstraintRelation.Equal,
                    _ => throw new ModelValidationException($"Invalid relation: {relation}")
                },
                RHS = rhs
            };
            for (int i = 0; i < nVars; i++)
            {
                constraint.Coefficients[variables[i]] = coeffs[i];
            }
            model.Constraints.Add(constraint);
        }

        private void ParseSignRestrictionsLine(string line, LPModel model, List<string> variables)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != variables.Count)
                throw new ModelValidationException("Sign restrictions line must have the same number of entries as variables.");
            for (int i = 0; i < variables.Count; i++)
            {
                string restriction = parts[i].ToLower();
                model.Variables[variables[i]].SignRestriction = restriction switch
                {
                    "+" => SignRestriction.Positive,
                    "-" => SignRestriction.Negative,
                    "urs" => SignRestriction.Unrestricted,
                    "int" => SignRestriction.Integer,
                    "bin" => SignRestriction.Binary,
                    _ => throw new ModelValidationException($"Invalid sign restriction: {restriction}")
                };
            }
        }
    }
}