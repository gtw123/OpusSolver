namespace OpusSolver.Solver
{
    public enum AssemblyType
    {
        // O
        SingleAtom,

        // O - O - O - O etc.
        Linear,

        //       O
        //      /
        // O - O
        //      \
        //       O
        Star2,

        Universal
    }
}
