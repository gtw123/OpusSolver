using System;

namespace OpusSolver.Solver
{
    [Serializable]
    public class SolverException : Exception
    {
        public SolverException()
            : base()
        {
        }

        public SolverException(string message)
            : base(message)
        {
        }

        public SolverException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
