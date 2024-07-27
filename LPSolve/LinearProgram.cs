using System;

namespace LPSolve
{
    public sealed class LinearProgram : IDisposable
    {
        private IntPtr m_lp;

        public int VariableCount => NativeMethods.get_Ncolumns(m_lp);

        public LinearProgram(int variableCount)
        {
            m_lp = NativeMethods.make_lp(0, variableCount);
            if (m_lp == IntPtr.Zero)
            {
                throw new InvalidOperationException("make_lp returned null.");
            }

            // Suppress output by default
            NativeMethods.set_outputfile(m_lp, "");
        }

        public void AddConstraint(double[] values, ConstraintType type, double righthandValue)
        {
            if (values.Length != VariableCount)
            {
                throw new ArgumentException($"Expected values to have length {VariableCount} but was {values.Length}.");
            }

            var result = NativeMethods.add_constraint(m_lp, ConvertToOneBasedArray(values), type, righthandValue);
            if (result != 1)
            {
                throw new InvalidOperationException($"add_constraint returned {result}.");
            }
        }

        public void SetContraintType(int constraintIndex, ConstraintType type)
        {
            var result = NativeMethods.set_constr_type(m_lp, constraintIndex + 1, type);
            if (result != 1)
            {
                throw new InvalidOperationException($"set_constr_type returned {result}");
            }
        }

        public void SetObjectiveFunction(double[] values)
        {
            if (values.Length != VariableCount)
            {
                throw new ArgumentException($"Expected values to have length {VariableCount} but was {values.Length}.");
            }

            var result = NativeMethods.set_obj_fn(m_lp, ConvertToOneBasedArray(values));
            if (result != 1)
            {
                throw new InvalidOperationException($"set_obj_fn returned {result}.");
            }
        }

        public void SetObjectiveType(ObjectiveType type)
        {
            NativeMethods.set_sense(m_lp, type == ObjectiveType.Maximize);
        }

        public void SetVariableIsInteger(int variableIndex, bool isInteger)
        {
            var result = NativeMethods.set_int(m_lp, variableIndex + 1, isInteger);
            if (result != 1)
            {
                throw new InvalidOperationException($"set_int returned {result}.");
            }
        }

        public SolveResult Solve()
        {
            return NativeMethods.solve(m_lp);
        }

        public double[] GetVariableValues()
        {
            // Note: Unlike other functions, get_variables returns a 0-based array so we don't need to convert it
            var values = new double[VariableCount];
            var result = NativeMethods.get_variables(m_lp, values);
            if (result != 1)
            {
                throw new InvalidOperationException($"get_variables returned {result}.");
            }

            return values;
        }

        public void Print()
        {
            NativeMethods.set_outputfile(m_lp, null);
            NativeMethods.print_lp(m_lp);
            NativeMethods.set_outputfile(m_lp, "");
        }

        public void Dispose()
        {
            if (m_lp != IntPtr.Zero)
            {
                NativeMethods.delete_lp(m_lp);
                m_lp = IntPtr.Zero;
            }
        }

        private double[] ConvertToOneBasedArray(double[] array)
        {
            var reindexedArray = new double[array.Length + 1];
            Array.Copy(array, 0, reindexedArray, 1, array.Length);
            return reindexedArray;
        }
    }
}
