namespace Opus.Solution
{
    /// <summary>
    /// Represents a mechanism on the hex grid.
    /// </summary>
    public class Mechanism : GameObject
    {
        public MechanismType Type { get; private set; }

        public Mechanism(GameObject parent, Vector2 position, int rotation, MechanismType type)
            : base(parent, position, rotation)
        {
            Type = type;
        }
    }
}
