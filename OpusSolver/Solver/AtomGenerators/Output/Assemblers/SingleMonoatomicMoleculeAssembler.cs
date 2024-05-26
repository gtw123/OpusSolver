﻿using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers
{
    public class SingleMonoatomicMoleculeAssembler : MoleculeAssembler
    {
        public override Vector2 OutputPosition => new Vector2();

        public SingleMonoatomicMoleculeAssembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer, parent.OutputPosition)
        {
            var product = products.Single();
            new Product(this, new Vector2(), HexRotation.R0, product);
        }

        public override void AddAtom(Element element, int productID)
        {
            // There's nothing to do here since the atom will get placed directly onto the product output
        }
    }
}
