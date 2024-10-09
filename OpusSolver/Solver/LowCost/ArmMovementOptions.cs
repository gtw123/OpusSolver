namespace OpusSolver.Solver.LowCost
{
    public class ArmMovementOptions
    {
        /// <summary>
        /// Allow cardinal atoms to be moved over glyphs of calcification.
        /// </summary>
        public bool AllowCalcification;

        /// <summary>
        /// Allow bonds to be created between the grabbed atoms and other atoms on the grid.
        /// </summary>
        public bool AllowExternalBonds;

        /// <summary>
        /// Allow bonds to be created between atoms within the grabbed atoms.
        /// </summary>
        public bool AllowInternalBonds;
    }
}
