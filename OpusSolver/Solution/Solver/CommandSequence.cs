using System.Collections.Generic;
using System.Text;

namespace OpusSolver.Solver
{
    /// <summary>
    /// A sequence of commands that are used to instruct atom generators to generate a program.
    /// </summary>
    public class CommandSequence
    {
        public IEnumerable<Command> Commands => m_commands;

        private List<Command> m_commands = new List<Command>();

        public void Add(CommandType type, Element element, ElementGenerator target, int id = 0)
        {
            Add(new Command(type, element, target, id));
        }

        public void Add(Command command)
        {
            m_commands.Add(command);
        }

        public override string ToString()
        {
            var str = new StringBuilder();
            foreach (var command in m_commands)
            {
                str.AppendLine(command.ToString());
            }

            return str.ToString();
        }
    }
}
