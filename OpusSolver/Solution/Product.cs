namespace OpusSolver
{
    /// <summary>
    /// Represents a product molecule on the hex grid.
    /// </summary>
    public class Product : MoleculeInputOutput
    {
        public Product(GameObject parent, Vector2 position, HexRotation rotation, Molecule molecule)
            : base(parent, position, rotation, molecule)
        {
        }
    }
}
