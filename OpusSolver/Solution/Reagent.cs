namespace OpusSolver.Solution
{
    /// <summary>
    /// Represents a reagent molecule on the hex grid.
    /// </summary>
    public class Reagent : GameObject
    {
        public int ID { get; private set; }

        public Reagent(GameObject parent, Vector2 position, int rotation, int id)
            : base(parent, position, rotation)
        {
            ID = id;
        }
    }
}
