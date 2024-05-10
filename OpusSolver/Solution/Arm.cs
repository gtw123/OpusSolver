namespace OpusSolver
{
    /// <summary>
    /// Represents an arm on the hex grid.
    /// </summary>
    public class Arm : Mechanism
    {
        public int Extension { get; set; }

        /// <summary>
        /// Internal ID of this arm. This is just to provide a simple way to uniquely identify and
        /// sort arms within a solution. It's not meant to be used in generated solution files.
        /// </summary>
        public int UniqueID { get; private set; }

        private static int sm_nextID = 0;

        public static void ResetArmIDs()
        {
            sm_nextID = 0;
        }

        public Arm(GameObject parent, Vector2 position, int rotation, MechanismType type, int extension = 1)
            : base(parent, position, rotation, type)
        {
            Extension = extension;
            UniqueID = sm_nextID++;
        }
    }
}
