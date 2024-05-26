using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Input
{
    /// <summary>
    /// A trivial input area used when there is only one reagent and it has a single atom.
    /// </summary>
    public class TrivialInputArea : AtomGenerator
    {
        public override Vector2 OutputPosition => new Vector2();

        public TrivialInputArea(ProgramWriter writer, IEnumerable<Molecule> reagents)
            : base(writer)
        {
            var reagent = reagents.First();
            if (reagent.Atoms.Count() > 1)
            {
                throw new ArgumentException("TrivialInputArea can't handle reagents with multiple atoms.");
            }

            new Reagent(this, new Vector2(0, 0), HexRotation.R0, reagent);
        }

        public override void Generate(Element element, int id)
        {
            Writer.NewFragment();
        }
    }
}
