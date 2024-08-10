using System.Collections.Generic;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    /// <summary>
    /// Assembles molecules from their component atoms.
    /// </summary>
    public abstract class MoleculeAssembler : SolverComponent
    {
        public MoleculeAssembler(SolverComponent parent, ProgramWriter writer)
            : base(parent, writer, parent.OutputPosition)
        {
        }

        /// <summary>
        /// Returns the required generation order of the elements of a product.
        /// </summary>
        public abstract IEnumerable<Element> GetProductElementOrder(Molecule product);

        /// <summary>
        /// Adds an atom to the assembly area.
        /// </summary>
        public abstract void AddAtom(Element element, int productID);

        public virtual void OptimizeParts()
        {
        }
    }
}
