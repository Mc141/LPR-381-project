using LPR381_Assignment.Models;

namespace LPR381_Assignment.Services.Algorithms
{
    /// <summary>
    /// Interface for all optimization algorithm solvers
    /// </summary>
    public interface IAlgorithmSolver
    {
        /// <summary>
        /// Name of the algorithm
        /// </summary>
        string AlgorithmName { get; }
        
        /// <summary>
        /// Solves the given LP model
        /// </summary>
        /// <param name="model">The LP model to solve</param>
        /// <returns>Solution result</returns>
        SolverResult Solve(LPModel model);
        
        /// <summary>
        /// Whether this solver supports the given model type
        /// </summary>
        /// <param name="model">The model to check</param>
        /// <returns>True if supported</returns>
        bool SupportsModel(LPModel model);
        
        /// <summary>
        /// Maximum number of iterations allowed
        /// </summary>
        int MaxIterations { get; set; }
        
        /// <summary>
        /// Numerical tolerance for comparisons
        /// </summary>
        double Tolerance { get; set; }
    }
}