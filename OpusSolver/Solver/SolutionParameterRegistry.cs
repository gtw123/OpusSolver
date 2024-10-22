using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver
{
    public class SolutionParameterRegistry
    {
        private readonly List<string> m_parameterNames = [];

        public class Common
        {
            public const string ReverseReagentElementOrder = nameof(ReverseReagentElementOrder);
            public const string ReverseFirstProductElementOrder = nameof(ReverseFirstProductElementOrder);
            public const string ReverseOtherProductElementOrder = nameof(ReverseOtherProductElementOrder);
            public const string ReverseProductBuildOrder = nameof(ReverseProductBuildOrder);
        }

        public void AddParameter(string parameterName)
        {
            m_parameterNames.Add(parameterName);
        }

        public IEnumerable<SolutionParameterSet> CreateParameterSets()
        {
            var values = m_parameterNames.Select(p => new[] { new KeyValuePair<string, bool>(p, false), new KeyValuePair<string, bool>(p, true) }.AsEnumerable());

            return values.CartesianProduct().Select(p => new SolutionParameterSet(p));
        }
    }
}
