using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    public abstract class LowCostAtomGenerator : AtomGenerator
    {
        public ArmArea ArmArea { get; private set; }
        public GridState GridState => ArmArea.GridState;

        /// <summary>
        /// The number of cells required on the main arm track to fit this generator in.
        /// </summary>
        public virtual int RequiredWidth => 1;

        /// <summary>
        /// If true, this generator takes up no space on the grid.
        /// </summary>
        public virtual bool IsEmpty => false;

        /// <summary>
        /// The points where an atom needs to pass over this glyph, plus the required rotation of the arm
        /// when its grabber is over each of these points. These points are in the local coordinate space
        /// of this atom generator. These should be specified in counterclockwise order.
        /// </summary>
        public virtual IEnumerable<Transform2D> RequiredAccessPoints => [];

        public LowCostAtomGenerator(ProgramWriter writer, ArmArea armArea)
            : base(writer)
        {
            ArmArea = armArea;
        }
    }
}
