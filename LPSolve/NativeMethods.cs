using System;
using System.Runtime.InteropServices;

namespace LPSolve
{
    internal static class NativeMethods
    {
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_column(IntPtr lp, double[] column);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_columnex(IntPtr lp, int count, double[] column, int[] rowno);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_constraint(IntPtr lp, double[] row, ConstraintType constr_type, double rh);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_constraintex(IntPtr lp, int count, double[] row, int[] colno, ConstraintType constr_type, double rh);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte add_lag_con(IntPtr lp, double[] row, ConstraintType con_type, double rhs);
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
        public static extern AntiDegenMode get_anti_degen(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_basis(IntPtr lp, int[] bascolumn, byte nonbasic);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern BasisCrashMode get_basiscrash(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern int get_bb_depthlimit(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern BranchMode get_bb_floorfirst(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern BBStrategy get_bb_rule(IntPtr lp);
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
        public static extern ConstraintType get_constr_type(IntPtr lp, int row);
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
        public static extern ImproveType get_improve(IntPtr lp);
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
        public static extern PivotMode get_pivoting(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern PresolveMode get_presolve(IntPtr lp);
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
        public static extern ScaleMode get_scaling(IntPtr lp);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_sensitivity_obj(IntPtr lp, double[] objfrom, double[] objtill);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_sensitivity_objex(IntPtr lp, double[] objfrom, double[] objtill, double[] objfromvalue, double[] objtillvalue);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte get_sensitivity_rhs(IntPtr lp, double[] duals, double[] dualsfrom, double[] dualstill);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern SimplexType get_simplextype(IntPtr lp);
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
        public static extern BranchMode get_var_branch(IntPtr lp, int column);
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
        public static extern byte is_anti_degen(IntPtr lp, ScaleMode testmask);
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
        public static extern byte is_piv_mode(IntPtr lp, ScaleMode testmask);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_piv_rule(IntPtr lp, PivotMode rule);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_presolve(IntPtr lp, ScaleMode testmask);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_scalemode(IntPtr lp, ScaleMode testmask);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte is_scaletype(IntPtr lp, ScaleMode scaletype);
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
        public delegate void msgfunc(IntPtr lp, int userhandle, MessageType message);
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
        public static extern void set_anti_degen(IntPtr lp, AntiDegenMode anti_degen);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_basis(IntPtr lp, int[] bascolumn, byte nonbasic);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_basiscrash(IntPtr lp, BasisCrashMode mode);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_basisvar(IntPtr lp, int basisPos, int enteringCol);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_bb_depthlimit(IntPtr lp, int bb_maxlevel);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_bb_floorfirst(IntPtr lp, BranchMode bb_floorfirst);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_bb_rule(IntPtr lp, BBStrategy bb_rule);
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
        public static extern byte set_constr_type(IntPtr lp, int row, ConstraintType con_type);
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
        public static extern void set_improve(IntPtr lp, ImproveType improve);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_infinite(IntPtr lp, double infinite);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_int(IntPtr lp, int column, [MarshalAs(UnmanagedType.I1)] bool must_be_int);
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
        public static extern void set_pivoting(IntPtr lp, PivotMode piv_rule);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_preferdual(IntPtr lp, byte dodual);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_presolve(IntPtr lp, PresolveMode do_presolve, int maxloops);
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
        public static extern void set_scaling(IntPtr lp, ScaleMode scalemode);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_semicont(IntPtr lp, int column, byte must_be_sc);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_sense(IntPtr lp, [MarshalAs(UnmanagedType.I1)] bool maximize);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_simplextype(IntPtr lp, SimplexType simplextype);
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
        public static extern byte set_var_branch(IntPtr lp, int column, BranchMode branch_mode);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern byte set_var_weights(IntPtr lp, double[] weights);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern void set_verbose(IntPtr lp, int verbose);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte set_XLI(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string filename);
        [DllImport("lpsolve55.dll", SetLastError = true)]
        public static extern SolveResult solve(IntPtr lp);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte str_add_column(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string col_string);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte str_add_constraint(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string row_string, ConstraintType constr_type, double rh);
        [DllImport("lpsolve55.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true, SetLastError = true)]
        public static extern byte str_add_lag_con(IntPtr lp, [MarshalAs(UnmanagedType.LPStr)] string row_string, ConstraintType con_type, double rhs);
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