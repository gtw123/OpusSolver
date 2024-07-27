namespace LPSolve
{
    public enum ConstraintType
    {
        LE = 1,
        EQ = 3,
        GE = 2,
        FR = 0,
    }

    public enum ObjectiveType
    {
        Minimize = 0,
        Maximize = 1,
    }

    public enum ScaleMode
    {
        SCALE_EXTREME = 1,
        SCALE_RANGE = 2,
        SCALE_MEAN = 3,
        SCALE_GEOMETRIC = 4,
        SCALE_CURTISREID = 7,
        SCALE_QUADRATIC = 8,
        SCALE_LOGARITHMIC = 16,
        SCALE_USERWEIGHT = 31,
        SCALE_POWER2 = 32,
        SCALE_EQUILIBRATE = 64,
        SCALE_INTEGERS = 128,
        SCALE_DYNUPDATE = 256,
        SCALE_ROWSONLY = 512,
        SCALE_COLSONLY = 1024,
    }
    public enum ImproveType
    {
        IMPROVE_NONE = 0,
        IMPROVE_SOLUTION = 1,
        IMPROVE_DUALFEAS = 2,
        IMPROVE_THETAGAP = 4,
        IMPROVE_BBSIMPLEX = 8,
        IMPROVE_DEFAULT = (IMPROVE_DUALFEAS + IMPROVE_THETAGAP),
        IMPROVE_INVERSE = (IMPROVE_SOLUTION + IMPROVE_THETAGAP)
    }
    public enum PivotMode
    {
        PRICER_FIRSTINDEX = 0,
        PRICER_DANTZIG = 1,
        PRICER_DEVEX = 2,
        PRICER_STEEPESTEDGE = 3,
        PRICE_PRIMALFALLBACK = 4,
        PRICE_MULTIPLE = 8,
        PRICE_PARTIAL = 16,
        PRICE_ADAPTIVE = 32,
        PRICE_HYBRID = 64,
        PRICE_RANDOMIZE = 128,
        PRICE_AUTOPARTIALCOLS = 256,
        PRICE_AUTOPARTIALROWS = 512,
        PRICE_LOOPLEFT = 1024,
        PRICE_LOOPALTERNATE = 2048,
        PRICE_AUTOPARTIAL = PivotMode.PRICE_AUTOPARTIALCOLS + PivotMode.PRICE_AUTOPARTIALROWS,
    }
    public enum PresolveMode
    {
        PRESOLVE_NONE = 0,
        PRESOLVE_ROWS = 1,
        PRESOLVE_COLS = 2,
        PRESOLVE_LINDEP = 4,
        PRESOLVE_SOS = 32,
        PRESOLVE_REDUCEMIP = 64,
        PRESOLVE_KNAPSACK = 128,
        PRESOLVE_ELIMEQ2 = 256,
        PRESOLVE_IMPLIEDFREE = 512,
        PRESOLVE_REDUCEGCD = 1024,
        PRESOLVE_PROBEFIX = 2048,
        PRESOLVE_PROBEREDUCE = 4096,
        PRESOLVE_ROWDOMINATE = 8192,
        PRESOLVE_COLDOMINATE = 16384,
        PRESOLVE_MERGEROWS = 32768,
        PRESOLVE_IMPLIEDSLK = 65536,
        PRESOLVE_COLFIXDUAL = 131072,
        PRESOLVE_BOUNDS = 262144,
        PRESOLVE_DUALS = 524288,
        PRESOLVE_SENSDUALS = 1048576
    }
    public enum AntiDegenMode
    {
        ANTIDEGEN_NONE = 0,
        ANTIDEGEN_FIXEDVARS = 1,
        ANTIDEGEN_COLUMNCHECK = 2,
        ANTIDEGEN_STALLING = 4,
        ANTIDEGEN_NUMFAILURE = 8,
        ANTIDEGEN_LOSTFEAS = 16,
        ANTIDEGEN_INFEASIBLE = 32,
        ANTIDEGEN_DYNAMIC = 64,
        ANTIDEGEN_DURINGBB = 128,
        ANTIDEGEN_RHSPERTURB = 256,
        ANTIDEGEN_BOUNDFLIP = 512
    }
    public enum BasisCrashMode
    {
        CRASH_NOTHING = 0,
        CRASH_MOSTFEASIBLE = 2,
    }
    public enum SimplexType
    {
        SIMPLEX_PRIMAL_PRIMAL = 5,
        SIMPLEX_DUAL_PRIMAL = 6,
        SIMPLEX_PRIMAL_DUAL = 9,
        SIMPLEX_DUAL_DUAL = 10,
    }
    public enum BBStrategy
    {
        NODE_FIRSTSELECT = 0,
        NODE_GAPSELECT = 1,
        NODE_RANGESELECT = 2,
        NODE_FRACTIONSELECT = 3,
        NODE_PSEUDOCOSTSELECT = 4,
        NODE_PSEUDONONINTSELECT = 5,
        NODE_PSEUDORATIOSELECT = 6,
        NODE_USERSELECT = 7,
        NODE_WEIGHTREVERSEMODE = 8,
        NODE_BRANCHREVERSEMODE = 16,
        NODE_GREEDYMODE = 32,
        NODE_PSEUDOCOSTMODE = 64,
        NODE_DEPTHFIRSTMODE = 128,
        NODE_RANDOMIZEMODE = 256,
        NODE_GUBMODE = 512,
        NODE_DYNAMICMODE = 1024,
        NODE_RESTARTMODE = 2048,
        NODE_BREADTHFIRSTMODE = 4096,
        NODE_AUTOORDER = 8192,
        NODE_RCOSTFIXING = 16384,
        NODE_STRONGINIT = 32768
    }
    public enum SolveResult
    {
        NOMEMORY = -2,
        OPTIMAL = 0,
        SUBOPTIMAL = 1,
        INFEASIBLE = 2,
        UNBOUNDED = 3,
        DEGENERATE = 4,
        NUMFAILURE = 5,
        USERABORT = 6,
        TIMEOUT = 7,
        PRESOLVED = 9,
        PROCFAIL = 10,
        PROCBREAK = 11,
        FEASFOUND = 12,
        NOFEASFOUND = 13,
    }
    public enum BranchMode
    {
        BRANCH_CEILING = 0,
        BRANCH_FLOOR = 1,
        BRANCH_AUTOMATIC = 2,
        BRANCH_DEFAULT = 3,
    }

    public enum MessageType
    {
        MSG_PRESOLVE = 1,
        MSG_LPFEASIBLE = 8,
        MSG_LPOPTIMAL = 16,
        MSG_MILPEQUAL = 32,
        MSG_MILPFEASIBLE = 128,
        MSG_MILPBETTER = 512,
    }
}
