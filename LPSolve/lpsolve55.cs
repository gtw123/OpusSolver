// This file is based on the C# example code from lp_solve. Since lp_solve is licensed under LGPL, this modified file
// and in fact this entire wrapper project must be licensed under LGPL as well. However, since this wrapper project
// builds to a shared library, using it within OpusSolver does not mean the rest of OpusSolver needs to be licensed
// under LGPL as well.

using System;
using System.Runtime.InteropServices;

namespace LPSolve
{
    public static class lpsolve
    {
        public enum lpsolve_constr_types
        {
            LE = 1,
            EQ = 3,
            GE = 2,
            FR = 0,
        }
        public enum lpsolve_scales
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
        public enum lpsolve_improves
        {
            IMPROVE_NONE = 0,
            IMPROVE_SOLUTION = 1,
            IMPROVE_DUALFEAS = 2,
            IMPROVE_THETAGAP = 4,
            IMPROVE_BBSIMPLEX = 8,
            IMPROVE_DEFAULT = (IMPROVE_DUALFEAS + IMPROVE_THETAGAP),
            IMPROVE_INVERSE = (IMPROVE_SOLUTION + IMPROVE_THETAGAP)
        }
        public enum lpsolve_piv_rules
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
            PRICE_AUTOPARTIAL = lpsolve_piv_rules.PRICE_AUTOPARTIALCOLS + lpsolve_piv_rules.PRICE_AUTOPARTIALROWS,
        }
        public enum lpsolve_presolve
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
        public enum lpsolve_anti_degen
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
        public enum lpsolve_basiscrash
        {
            CRASH_NOTHING = 0,
            CRASH_MOSTFEASIBLE = 2,
        }
        public enum lpsolve_simplextypes
        {
            SIMPLEX_PRIMAL_PRIMAL = 5,
            SIMPLEX_DUAL_PRIMAL = 6,
            SIMPLEX_PRIMAL_DUAL = 9,
            SIMPLEX_DUAL_DUAL = 10,
        }
        public enum lpsolve_BBstrategies
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
        public enum lpsolve_return
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
        public enum lpsolve_branch
        {
            BRANCH_CEILING = 0,
            BRANCH_FLOOR = 1,
            BRANCH_AUTOMATIC = 2,
            BRANCH_DEFAULT = 3,
        }

        public enum lpsolve_msgmask
        {
            MSG_PRESOLVE = 1,
            MSG_LPFEASIBLE = 8,
            MSG_LPOPTIMAL = 16,
            MSG_MILPEQUAL = 32,
            MSG_MILPFEASIBLE = 128,
            MSG_MILPBETTER = 512,
        }

        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_column(IntPtr lp, double[] column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_columnex(IntPtr lp, int count, double[] column, int[] rowno);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_constraint(IntPtr lp, double[] row, lpsolve_constr_types constr_type, double rh);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_constraintex(IntPtr lp, int count, double[] row, int[] colno, lpsolve_constr_types constr_type, double rh);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_lag_con(IntPtr lp, double[] row, lpsolve_constr_types con_type, double rhs);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern int add_SOS(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string name, int sostype, int priority, int count, int[] sosvars, double[] weights);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int column_in_lp(IntPtr lp, double[] column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern IntPtr copy_lp(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void default_basis(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte del_column(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte del_constraint(IntPtr lp, int del_row);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void delete_lp(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte dualize_lp(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_anti_degen get_anti_degen(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_basis(IntPtr lp, int[] bascolumn, byte nonbasic);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_basiscrash get_basiscrash(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_bb_depthlimit(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_branch get_bb_floorfirst(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_BBstrategies get_bb_rule(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_bounds_tighter(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_break_at_value(IntPtr lp);
        //[DllImport("lpsolve55.dll", SetLastError=true)] public static extern string get_col_name(IntPtr lp, int column);
#if lpsolve_unsafe
    [DllImport("lpsolve55.dll", EntryPoint = "get_col_name", SetLastError = true)] private unsafe static extern IntPtr get_col_name_c(IntPtr lp, int column);
#endif
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_column(IntPtr lp, int col_nr, double[] column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_columnex(IntPtr lp, int col_nr, double[] column, int[] nzrow);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_constr_types get_constr_type(IntPtr lp, int row);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_constr_value(IntPtr lp, int row, int count, double[] primsolution, int[] nzindex);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_constraints(IntPtr lp, double[] constr);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_dual_solution(IntPtr lp, double[] rc);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_epsb(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_epsd(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_epsel(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_epsint(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_epsperturb(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_epspivot(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_improves get_improve(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_infinite(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_lambda(IntPtr lp, double[] lambda);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_lowbo(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_lp_index(IntPtr lp, int orig_index);
        //[DllImport("lpsolve55.dll", SetLastError=true)] public static extern string get_lp_name(IntPtr lp);
#if lpsolve_unsafe
    [DllImport("lpsolve55.dll", EntryPoint = "get_lp_name", SetLastError=true)] private unsafe static extern IntPtr get_lp_name_c(IntPtr lp);
#endif
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_Lrows(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_mat(IntPtr lp, int row, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_max_level(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_maxpivot(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_mip_gap(IntPtr lp, byte absolute);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_Ncolumns(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_negrange(IntPtr lp);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern int get_nameindex(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string name, byte isrow);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_nonzeros(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_Norig_columns(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_Norig_rows(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_Nrows(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_obj_bound(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_objective(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_orig_index(IntPtr lp, IntPtr lp_index);
        //[DllImport("lpsolve55.dll", SetLastError=true)] public static extern string get_origcol_name(IntPtr lp, int column);
#if lpsolve_unsafe
    [DllImport("lpsolve55.dll", EntryPoint = "get_origcol_name", SetLastError=true)] private unsafe static extern IntPtr get_origcol_name_c(IntPtr lp, int column);
#endif
        //[DllImport("lpsolve55.dll", SetLastError=true)] public static extern string get_origrow_name(IntPtr lp, int row);
#if lpsolve_unsafe
    [DllImport("lpsolve55.dll", EntryPoint = "get_origrow_name", SetLastError=true)] private unsafe static extern IntPtr get_origrow_name_c(IntPtr lp, int row);
#endif
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_piv_rules get_pivoting(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_presolve get_presolve(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_presolveloops(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_primal_solution(IntPtr lp, double[] pv);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_print_sol(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_rh(IntPtr lp, int row);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_rh_range(IntPtr lp, int row);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_row(IntPtr lp, int row_nr, double[] row);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_rowex(IntPtr lp, int row_nr, double[] row, int[] colno);
        //[DllImport("lpsolve55.dll", SetLastError=true)] public static extern string get_row_name(IntPtr lp, int row);
#if lpsolve_unsafe
    [DllImport("lpsolve55.dll", EntryPoint = "get_row_name", SetLastError=true)] private unsafe static extern IntPtr get_row_name_c(IntPtr lp, int row);
#endif
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_scalelimit(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_scales get_scaling(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_sensitivity_obj(IntPtr lp, double[] objfrom, double[] objtill);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_sensitivity_objex(IntPtr lp, double[] objfrom, double[] objtill, double[] objfromvalue, double[] objtillvalue);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_sensitivity_rhs(IntPtr lp, double[] duals, double[] dualsfrom, double[] dualstill);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_simplextypes get_simplextype(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_solutioncount(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_solutionlimit(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_status(IntPtr lp);
        //[DllImport("lpsolve55.dll", SetLastError=true)] public static extern string get_statustext(IntPtr lp, int statuscode);
#if lpsolve_unsafe
    [DllImport("lpsolve55.dll", EntryPoint = "get_statustext", SetLastError=true)] private unsafe static extern IntPtr get_statustext_c(IntPtr lp, int statuscode);
#endif
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_timeout(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern long get_total_iter(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern long get_total_nodes(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_upbo(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_branch get_var_branch(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_var_dualresult(IntPtr lp, int index);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_var_primalresult(IntPtr lp, int index);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_var_priority(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_variables(IntPtr lp, double[] var);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_verbose(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double get_working_objective(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte guess_basis(IntPtr lp, double[] guessvector, int[] basisvector);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte has_BFP(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte has_XLI(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_add_rowmode(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_anti_degen(IntPtr lp, lpsolve_scales testmask);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_binary(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_break_at_first(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_constr_type(IntPtr lp, int row, int mask);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_debug(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_feasible(IntPtr lp, double[] values, double threshold);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_infinite(IntPtr lp, double value);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_int(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_integerscaling(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_lag_trace(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_maxim(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_nativeBFP(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_nativeXLI(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_negative(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_piv_mode(IntPtr lp, lpsolve_scales testmask);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_piv_rule(IntPtr lp, lpsolve_piv_rules rule);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_presolve(IntPtr lp, lpsolve_scales testmask);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_scalemode(IntPtr lp, lpsolve_scales testmask);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_scaletype(IntPtr lp, lpsolve_scales scaletype);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_semicont(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_SOS_var(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_trace(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_unbounded(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_use_names(IntPtr lp, byte isrow);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void lp_solve_version(ref int majorversion, ref int minorversion, ref int release, ref int build);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern IntPtr make_lp(int rows, int columns);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern bool resize_lp(IntPtr lp, int rows, int columns);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void print_constraints(IntPtr lp, int columns);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte print_debugdump(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void print_duals(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void print_lp(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void print_objective(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void print_scales(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void print_solution(IntPtr lp, int columns);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern void print_str(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string str);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void print_tableau(IntPtr lp);
        public delegate byte ctrlcfunc(IntPtr lp, int userhandle);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern void put_abortfunc(IntPtr lp, ctrlcfunc newctrlc, int ctrlchandle);
        public delegate void logfunc(IntPtr lp, int userhandle, [MarshalAs(UnmanagedType.LPStr)] string buf);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void put_logfunc(IntPtr lp, logfunc newlog, int loghandle);
        public delegate void msgfunc(IntPtr lp, int userhandle, lpsolve_msgmask message);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void put_msgfunc(IntPtr lp, msgfunc newmsg, int msghandle, int mask);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte read_basis(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename, [MarshalAs(UnmanagedType.LPStr)] string info);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern IntPtr read_freeMPS([MarshalAs(UnmanagedType.LPStr)] string filename, int options);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern IntPtr read_LP([MarshalAs(UnmanagedType.LPStr)] string filename, int verbose, [MarshalAs(UnmanagedType.LPStr)] string lp_name);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern IntPtr read_MPS([MarshalAs(UnmanagedType.LPStr)] string filename, int options);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern IntPtr read_XLI([MarshalAs(UnmanagedType.LPStr)] string xliname, [MarshalAs(UnmanagedType.LPStr)] string modelname, [MarshalAs(UnmanagedType.LPStr)] string dataname, [MarshalAs(UnmanagedType.LPStr)] string options, int verbose);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte read_params(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename, [MarshalAs(UnmanagedType.LPStr)] string options);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void reset_basis(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void reset_params(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_add_rowmode(IntPtr lp, byte turnon);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_anti_degen(IntPtr lp, lpsolve_anti_degen anti_degen);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_basis(IntPtr lp, int[] bascolumn, byte nonbasic);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_basiscrash(IntPtr lp, lpsolve_basiscrash mode);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_basisvar(IntPtr lp, int basisPos, int enteringCol);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_bb_depthlimit(IntPtr lp, int bb_maxlevel);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_bb_floorfirst(IntPtr lp, lpsolve_branch bb_floorfirst);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_bb_rule(IntPtr lp, lpsolve_BBstrategies bb_rule);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte set_BFP(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_binary(IntPtr lp, int column, byte must_be_bin);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_bounds(IntPtr lp, int column, double lower, double upper);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_bounds_tighter(IntPtr lp, byte tighten);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_break_at_first(IntPtr lp, byte break_at_first);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_break_at_value(IntPtr lp, double break_at_value);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte set_col_name(IntPtr lp, int column, [MarshalAs(UnmanagedType.LPStr)] string new_name);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_column(IntPtr lp, int col_no, double[] column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_columnex(IntPtr lp, int col_no, int count, double[] column, int[] rowno);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_constr_type(IntPtr lp, int row, lpsolve_constr_types con_type);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_debug(IntPtr lp, byte debug);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_epsb(IntPtr lp, double epsb);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_epsd(IntPtr lp, double epsd);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_epsel(IntPtr lp, double epsel);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_epsint(IntPtr lp, double epsint);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_epslevel(IntPtr lp, int level);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_epsperturb(IntPtr lp, double epsperturb);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_epspivot(IntPtr lp, double epspivot);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_improve(IntPtr lp, lpsolve_improves improve);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_infinite(IntPtr lp, double infinite);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_int(IntPtr lp, int column, byte must_be_int);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_lag_trace(IntPtr lp, byte lag_trace);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_lowbo(IntPtr lp, int column, double value);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte set_lp_name(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string lpname);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_mat(IntPtr lp, int row, int column, double value);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_maxim(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_maxpivot(IntPtr lp, int max_num_inv);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_minim(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_mip_gap(IntPtr lp, byte absolute, double mip_gap);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_negrange(IntPtr lp, double negrange);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_obj(IntPtr lp, int Column, double Value);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_obj_bound(IntPtr lp, double obj_bound);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_obj_fn(IntPtr lp, double[] row);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_obj_fnex(IntPtr lp, int count, double[] row, int[] colno);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte set_outputfile(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_pivoting(IntPtr lp, lpsolve_piv_rules piv_rule);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_preferdual(IntPtr lp, byte dodual);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_presolve(IntPtr lp, lpsolve_presolve do_presolve, int maxloops);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_print_sol(IntPtr lp, int print_sol);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_rh(IntPtr lp, int row, double value);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_rh_range(IntPtr lp, int row, double deltavalue);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_rh_vec(IntPtr lp, double[] rh);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_row(IntPtr lp, int row_no, double[] row);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte set_row_name(IntPtr lp, int row, [MarshalAs(UnmanagedType.LPStr)] string new_name);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_rowex(IntPtr lp, int row_no, int count, double[] row, int[] colno);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_scalelimit(IntPtr lp, double scalelimit);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_scaling(IntPtr lp, lpsolve_scales scalemode);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_semicont(IntPtr lp, int column, byte must_be_sc);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_sense(IntPtr lp, byte maximize);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_simplextype(IntPtr lp, lpsolve_simplextypes simplextype);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_solutionlimit(IntPtr lp, int limit);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_timeout(IntPtr lp, int sectimeout);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_trace(IntPtr lp, byte trace);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_unbounded(IntPtr lp, int column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_upbo(IntPtr lp, int column, double value);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_use_names(IntPtr lp, byte isrow, byte use_names);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_var_branch(IntPtr lp, int column, lpsolve_branch branch_mode);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_var_weights(IntPtr lp, double[] weights);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_verbose(IntPtr lp, int verbose);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte set_XLI(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern lpsolve_return solve(IntPtr lp);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte str_add_column(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string col_string);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte str_add_constraint(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string row_string, lpsolve_constr_types constr_type, double rh);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte str_add_lag_con(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string row_string, lpsolve_constr_types con_type, double rhs);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte str_set_obj_fn(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string row_string);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte str_set_rh_vec(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string rh_string);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern double time_elapsed(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void unscale(IntPtr lp);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte write_basis(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte write_freemps(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte write_lp(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte write_mps(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte write_XLI(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename, [MarshalAs(UnmanagedType.LPStr)] string options, byte results);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte write_params(IntPtr lp, [MarshalAs(UnmanagedType.LPStr) ] string filename, [MarshalAs(UnmanagedType.LPStr)] string options);

#if lpsolve_unsafe
        public static string get_col_name(IntPtr lp, int column)
        {
            return (Marshal.PtrToStringAnsi(get_col_name_c(lp, column)));
        }

        public static string get_lp_name(IntPtr lp)
        {
            return(Marshal.PtrToStringAnsi(get_lp_name_c(lp)));
        }

        public static string get_origcol_name(IntPtr lp, int column)
        {
            return(Marshal.PtrToStringAnsi(get_origcol_name_c(lp, column)));
        }

        public static string get_origrow_name(IntPtr lp, int row)
        {
            return(Marshal.PtrToStringAnsi(get_origrow_name_c(lp, row)));
        }

        public static string get_row_name(IntPtr lp, int row)
        {
            return(Marshal.PtrToStringAnsi(get_row_name_c(lp, row)));
        }

        public static string get_statustext(IntPtr lp, int statuscode)
        {
            return(Marshal.PtrToStringAnsi(get_statustext_c(lp, statuscode)));
        }
#endif      
    }
}