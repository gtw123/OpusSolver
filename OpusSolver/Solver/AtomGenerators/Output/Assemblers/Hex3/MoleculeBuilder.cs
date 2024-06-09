using System.Collections.Generic;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers.Hex3
{
    /// <summary>
    /// Generates instructions for assembling a molecule.
    /// </summary>
    public abstract class MoleculeBuilder
    {
        public AssemblyArea AssemblyArea { get; private set; }
        public Molecule Product { get; private set; }

        public abstract Vector2 CenterAtomPosition { get; }
        public abstract OutputLocation OutputLocation { get; }
        public abstract HexRotation OutputRotation { get; }

        public virtual Vector2 OutputPositionOffset => new Vector2(0, 0);
        public virtual bool RequiresRotationsBetweenOutputPositions { get; } = true;

        protected ProgramWriter Writer { get; private set; } = new ProgramWriter();
        public IEnumerable<Program> Fragments => Writer.Fragments;

        public MoleculeBuilder(AssemblyArea assemblyArea, Molecule product)
        {
            AssemblyArea = assemblyArea;
            Product = product;
        }

        public abstract IEnumerable<Element> GetElementsInBuildOrder();
    }
}
