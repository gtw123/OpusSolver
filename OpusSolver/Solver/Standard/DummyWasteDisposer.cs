namespace OpusSolver.Solver.Standard
{
    /// <summary>
    /// Dummy waste disposer (the standard solver doesn't currently support the glyph of disposal).
    /// </summary>
    public class DummyWasteDisposer : AtomGenerator
    {
        public DummyWasteDisposer(ProgramWriter writer)
            : base(writer)
        {
        }
    }
}
