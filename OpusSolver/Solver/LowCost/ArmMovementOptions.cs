using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    public class ArmMovementOptions
    {
        /// <summary>
        /// Allow cardinal atoms to be moved over glyphs of calcification.
        /// </summary>
        public bool AllowCalcification;

        /// <summary>
        /// Allow salt atoms to be moved over glyphs of duplication.
        /// </summary>
        public bool AllowDuplication;

        /// <summary>
        /// Allow bonds to be created between atoms in the grabbed molecule and other atoms on the grid.
        /// </summary>
        public bool AllowExternalBonds;

        /// <summary>
        /// Allow bonds to be created between atoms within the grabbed molecule.
        /// </summary>
        public bool AllowInternalBonds;

        /// <summary>
        /// Allow bonds within the molecule to be removed.
        /// </summary>
        public bool AllowUnbonding;

        /// <summary>
        /// A bond which is only allowed to be removed once the molecule has reached its target position.
        /// </summary>
        public (Vector2 Atom1, Vector2 Atom2)? FinalBondToRemove;
    }
}
