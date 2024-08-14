using System.Collections.Generic;

namespace OpusSolver.Solver.AtomGenerators.Output.Hex3
{
    /// <summary>
    /// Generates instructions for assembling a molecule.
    /// </summary>
    public abstract class MoleculeBuilder
    {
        public Molecule Product { get; private set; }

        public abstract Vector2 CenterAtomPosition { get; }
        public abstract OutputLocation OutputLocation { get; }
        public abstract HexRotation OutputRotation { get; }

        public virtual Vector2 OutputPositionOffset => new Vector2(0, 0);
        public virtual bool RequiresRotationsBetweenOutputPositions { get; } = true;

        public MoleculeBuilder(Molecule product)
        {
            Product = product;
        }

        public abstract IEnumerable<Element> GetElementsInBuildOrder();

        public abstract IEnumerable<Program> GenerateFragments(AssemblyArea assemblyArea);
    }
}
