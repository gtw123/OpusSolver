namespace OpusSolver.Solver.LowCost
{
    public class ArmArea : SolverComponent
    {
        public Arm MainArm { get; private set; }
        public Track Track { get; private set; }

        public Transform2D ArmTransform { get; private set; }
        public int ArmLength => MainArm.Extension;

        public ArmArea(SolverComponent parent, ProgramWriter writer)
            : base(parent, writer, new Vector2())
        {
            // TODO: Set default rotation properly
            ArmTransform = new Transform2D(new Vector2(), HexRotation.R240);

            MainArm = new Arm(this, ArmTransform.Position, ArmTransform.Rotation, ArmType.Arm1, extension: 2);
        }

        public void RotateArmTo(HexRotation targetRotation)
        {
            // TODO: Add a utility method to do this? It's similar logic to VanBerloController
            foreach (var rot in ArmTransform.Rotation.CalculateRotationsTo(targetRotation))
            {
                var instruction = ((rot - ArmTransform.Rotation) == HexRotation.R60) ? Instruction.RotateCounterclockwise : Instruction.RotateClockwise;
                Writer.Write(MainArm, instruction);
                ArmTransform.Rotation = rot;
            }
        }

        public void ResetArm()
        {
            Writer.Write(MainArm, Instruction.Reset);
            ArmTransform.Rotation = MainArm.Transform.Rotation;
        }
    }
}