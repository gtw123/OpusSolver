using static System.FormattableString;

namespace OpusSolver
{
    public struct Transform2D
    {
        public Vector2 Position;
        public HexRotation Rotation;

        public Transform2D(Vector2 translation, HexRotation rotation)
        {
            Position = translation;
            Rotation = rotation;
        }

        public Vector2 Apply(Vector2 vec)
        {
            return Position + vec.RotateBy(Rotation);
        }

        public HexRotation Apply(HexRotation rotation)
        {
            return Rotation + rotation;
        }

        public Transform2D Apply(Transform2D transform)
        {
            return new Transform2D(Apply(transform.Position), Apply(transform.Rotation));
        }

        public Transform2D RotateAbout(Vector2 point, HexRotation rotation)
        {
            return new Transform2D(Position.RotateAbout(point, rotation), Rotation + rotation);
        }

        public override string ToString()
        {
            return Invariant($"({Position}, {Rotation})");
        }
    }
}