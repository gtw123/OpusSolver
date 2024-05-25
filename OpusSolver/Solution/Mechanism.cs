namespace OpusSolver
{
    /// <summary>
    /// Represents a mechanism on the hex grid.
    /// </summary>
    public class Mechanism : GameObject
    {
        public MechanismType Type { get; private set; }

        public Mechanism(GameObject parent, Vector2 position, HexRotation rotation, MechanismType type)
            : base(parent, position, rotation)
        {
            Type = type;
        }
    }
}
