namespace OpusSolver
{
    /// <summary>
    /// Represents a reagent molecule on the hex grid.
    /// </summary>
    public class Reagent : MoleculeInputOutput
    {
        public Reagent(GameObject parent, Vector2 position, HexRotation rotation, Molecule molecule)
            : base(parent, position, rotation, molecule)
        {
        }
    }
}
