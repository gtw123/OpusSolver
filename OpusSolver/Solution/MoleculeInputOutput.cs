namespace OpusSolver
{
    /// <summary>
    /// Represents a molecule input or output on the hex grid.
    /// </summary>
    public abstract class MoleculeInputOutput : GameObject
    {
        public Molecule Molecule { get; private set; }

        public MoleculeInputOutput(GameObject parent, Vector2 position, HexRotation rotation, Molecule molecule)
            : base(parent, position, rotation)
        {
            Molecule = molecule;
        }
    }
}
