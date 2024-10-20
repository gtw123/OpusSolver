using System.Collections.Generic;

namespace OpusSolver.Solver
{
    public interface ISolutionBuilder
    {
        SolutionPlan CreatePlan();

        void CreateAtomGenerators(ElementPipeline pipeline);

        IEnumerable<GameObject> GetAllObjects();

        SolutionParameterRegistry GetAvailableParameters();
    }
}
