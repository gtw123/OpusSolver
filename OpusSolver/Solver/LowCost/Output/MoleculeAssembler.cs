using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost.Output
{
    /// <summary>
    /// Assembles molecules from their component atoms.
    /// </summary>
    public abstract class MoleculeAssembler : SolverComponent
    {
        public ArmArea ArmArea { get; private set; }

        /// <summary>
        /// The number of cells required on the main arm track to fit this generator in.
        /// </summary>
        public virtual int RequiredWidth => 1;

        public virtual IEnumerable<Transform2D> RequiredAccessPoints { get; }

        public MoleculeAssembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea)
            : base(parent, writer, parent.OutputPosition)
        {
            ArmArea = armArea;
        }

        /// <summary>
        /// Adds an atom to the assembly area.
        /// </summary>
        public abstract void AddAtom(Element element, int productID);

        public virtual void OptimizeParts()
        {
        }
    }
}
