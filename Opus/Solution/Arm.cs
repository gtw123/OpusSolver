namespace Opus.Solution
{
    /// <summary>
    /// Represents an arm on the hex grid.
    /// </summary>
    public class Arm : Mechanism
    {
        public int Extension { get; set; }
        public int ID { get; private set; }

        private static int sm_nextID = 1;

        public Arm(GameObject parent, Vector2 position, int rotation, MechanismType type, int extension = 1)
            : base(parent, position, rotation, type)
        {
            Extension = extension;
            ID = sm_nextID++;
        }
    }
}
