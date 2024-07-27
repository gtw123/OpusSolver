using System;
using static System.FormattableString;

namespace OpusSolver.Solver
{
    public enum CommandType
    {
        Consume,
        Generate,
        PassThrough
    }

    /// <summary>
    /// A command that instructs an atom generator to perform a certain operation.
    /// </summary>
    public class Command
    {
        /// <summary>
        /// The type of command to perform.
        /// </summary>
        public CommandType Type { get; private set; }

        /// <summary>
        /// The element associated with the command.
        /// </summary>
        public Element Element { get; private set; }

        /// <summary>
        /// The element generator on which to execute the command.
        /// </summary>
        public ElementGenerator ElementGenerator { get; private set; }

        /// <summary>
        /// Context-sensitive ID. Used to distinguish inputs, outputs and other parts of a generator.
        /// </summary>
        public int ID { get; private set; }

        public Command(CommandType type, Element element, ElementGenerator target, int id)
        {
            Type = type;
            Element = element;
            ElementGenerator = target;
            ID = id;
        }

        public void Execute()
        {
            var atomGenerator = ElementGenerator.AtomGenerator;

            switch (Type)
            {
                case CommandType.Consume:
                    atomGenerator.Consume(Element, ID);
                    break;
                case CommandType.Generate:
                    atomGenerator.Generate(Element, ID);
                    break;
                case CommandType.PassThrough:
                    atomGenerator.PassThrough(Element);
                    break;
                default:
                    throw new InvalidOperationException(Invariant($"Invalid command type: {Type}."));
            }
        }

        public override string ToString()
        {
            return Invariant($"{Type,-11} {Element} {ElementGenerator.GetType().Name} {ID}");
        }
    }
}
