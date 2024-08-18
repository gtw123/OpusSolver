namespace OpusSolver.Solver
{
    /// <summary>
    /// Represents an object (or group of objects) on the hex grid that can write instructions to a program.
    /// </summary>
    public abstract class SolverComponent : GameObject
    {
        public virtual Vector2 OutputPosition { get; } = new Vector2();

        protected ProgramWriter Writer { get; private set; }

        protected SolverComponent(ProgramWriter writer)
            : this(null, writer, new Vector2())
        {
        }

        protected SolverComponent(SolverComponent parent, ProgramWriter writer, Vector2 position)
            : base(parent, position, HexRotation.R0)
        {
            Writer = writer;
        }
    }
}
