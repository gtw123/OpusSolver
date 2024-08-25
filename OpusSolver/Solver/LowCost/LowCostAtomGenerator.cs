using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    public abstract class LowCostAtomGenerator : AtomGenerator
    {
        public ArmArea ArmArea { get; private set; }

        /// <summary>
        /// The number of cells required on the main arm track to fit this generator in.
        /// </summary>
        public virtual int RequiredWidth => 1;

        /// <summary>
        /// The points where an atom needs to pass over this glyph, plus the required rotation of the arm
        /// when its grabber is over each of these points. These points are in the local coordinate space
        /// of this atom generator.
        /// </summary>
        public virtual IEnumerable<Transform2D> RequiredAccessPoints => [];

        public LowCostAtomGenerator(ProgramWriter writer, ArmArea armArea)
            : base(writer)
        {
            ArmArea = armArea;
        }
    }
}
