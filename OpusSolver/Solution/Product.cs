namespace OpusSolver
{
    /// <summary>
    /// Represents a product molecule on the hex grid.
    /// </summary>
    public class Product : GameObject
    {
        public int ID { get; private set; }

        public Product(GameObject parent, Vector2 position, int rotation, int id)
            : base(parent, position, rotation)
        {
            ID = id;
        }
    }
}
