using System.Collections.Generic;

namespace OpusSolver.Solver
{
    public enum ReactionType
    {
        Reagent,
        Calcification,
        VanBerlo,
        Projection,
        Purification,
        Animismus,
        Unification,
        Dispersion,
    }

    public class Reaction(ReactionType type, int id, IReadOnlyDictionary<Element, int> inputs, IReadOnlyDictionary<Element, int> outputs)
    {
        public ReactionType Type { get; private set; } = type;
        public int ID { get; private set; } = id;
        public IReadOnlyDictionary<Element, int> Inputs { get; private set; } = inputs;
        public IReadOnlyDictionary<Element, int> Outputs { get; private set; } = outputs;
    }
}