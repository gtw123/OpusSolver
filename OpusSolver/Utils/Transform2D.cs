using static System.FormattableString;

namespace OpusSolver
{
    public record struct Transform2D
    {
        public Vector2 Position;
        public HexRotation Rotation;

        public Transform2D(Vector2 translation, HexRotation rotation)
        {
            Position = translation;
            Rotation = rotation;
        }

        public readonly Vector2 Apply(Vector2 vec)
        {
            return Position + vec.RotateBy(Rotation);
        }

        public readonly HexRotation Apply(HexRotation rotation)
        {
            return Rotation + rotation;
        }

        public readonly Transform2D Apply(Transform2D transform)
        {
            return new Transform2D(Apply(transform.Position), Apply(transform.Rotation));
        }

        public readonly Transform2D OffsetBy(Vector2 offset)
        {
            return new Transform2D(Position + offset, Rotation);
        }

        public readonly Transform2D RotateAbout(Vector2 point, HexRotation rotation)
        {
            return new Transform2D(Position.RotateAbout(point, rotation), Rotation + rotation);
        }

        public readonly Transform2D Inverse()
        {
            return new Transform2D(-Position.RotateBy(-Rotation), -Rotation);
        }

        public override readonly string ToString()
        {
            return Invariant($"({Position}, {Rotation})");
        }
    }
}