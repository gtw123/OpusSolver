﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpusSolver.Solver
{
    public enum ReactionType
    {
        Reagent,
        Product,
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

        public override string ToString()
        {
            string GetElementListString(IReadOnlyDictionary<Element, int> elements)
            {
                string GetCoefficientString(int value) => value > 1 ? $"{value} " : "";
                return string.Join(" + ", elements.Select(p => $"{GetCoefficientString(p.Value)}{p.Key}"));
            }

            var str = new StringBuilder();

            string idString = (Type == ReactionType.Reagent || Type == ReactionType.Product) ? $" #{ID}" : "";
            str.Append($"{Type}{idString}: ");

            if (Type != ReactionType.Reagent)
            {
                str.Append(GetElementListString(Inputs));
                if (Type != ReactionType.Product)
                {
                    str.Append(" -> ");
                }
            }

            if (Type != ReactionType.Product)
            {
                str.Append(GetElementListString(Outputs));
            }

            return str.ToString();
        }
    }
}