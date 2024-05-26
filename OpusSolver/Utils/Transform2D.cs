namespace OpusSolver
{
    public class Transform2D
    {
        public Vector2 Position { get; set; }
        public HexRotation Rotation { get; set; }

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
    }
}