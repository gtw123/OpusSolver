namespace OpusSolver.Solver
{
    public enum AssemblyType
    {
        // O
        Monoatomic,

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
