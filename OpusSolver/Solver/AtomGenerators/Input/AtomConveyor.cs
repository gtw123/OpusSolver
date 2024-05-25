using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Input
{
    /// <summary>
    /// Transports atoms along a "conveyor" to a destination.
    /// </summary>
    public class AtomConveyor : SolverComponent
    {
        public override Vector2 OutputPosition => new Vector2(4, 0);
        public int Height { get; private set; }

        private List<Arm> m_downArms = new List<Arm>();
        private Arm m_cornerArm;
        private List<Arm> m_acrossArms = new List<Arm>();

        public AtomConveyor(SolverComponent parent, ProgramWriter writer, Vector2 position, int height)
            : base(parent, writer, position)
        {
            if (height % 2 > 0)
            {
                throw new ArgumentException("Height must be a multitple of two.", "height");
            }

            Height = height;

            for (int y = Height - 2; y >= 2; y -= 2)
            {
                m_downArms.Add(new Arm(this, new Vector2(2, y), HexRotation.R120, MechanismType.Arm1, extension: 2));
            }

            // This arm has to be in a different spot and direction to avoid atoms hitting it when moving East
            m_cornerArm = new Arm(this, new Vector2(-2, 2), HexRotation.R0, MechanismType.Arm1, extension: 2);

            for (int i = 1; i <= 2; i++)
            {
                m_acrossArms.Add(new Arm(this, new Vector2(i * 2, -2), HexRotation.R120, MechanismType.Arm1, extension: 2));
            }
        }

        public void MoveAtom(int startY)
        {
            foreach (var arm in m_downArms.Where(a => a.Position.Y + 2 <= startY))
            {
                Writer.WriteGrabResetAction(arm, Instruction.RotateCounterclockwise);
            }

            if (startY > 0)
            {
                Writer.WriteGrabResetAction(m_cornerArm, Instruction.RotateClockwise);
            }

            foreach (var arm in m_acrossArms)
            {
                Writer.WriteGrabResetAction(arm, Instruction.RotateClockwise);
            }
        }
    }
}
