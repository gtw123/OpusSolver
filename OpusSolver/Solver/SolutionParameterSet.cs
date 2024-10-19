using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class SolutionParameterSet
    {
        private readonly Dictionary<string, bool> m_parameterValues;

        public SolutionParameterSet(IEnumerable<KeyValuePair<string, bool>> parameterValues)
        {
            m_parameterValues = new Dictionary<string, bool>(parameterValues);
        }

        public bool GetParameterValue(string parameterName)
        {
            return m_parameterValues.TryGetValue(parameterName, out bool value) ? value : false;
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, m_parameterValues.OrderBy(p => p.Key).Select(p => $"  {p.Key} = {p.Value}"));
        }
    }
}
