using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.AtomGenerators.Output.Assemblers
{
    public class Star2MoleculeAssembler : MoleculeAssembler
    {
        public override Vector2 OutputPosition => new Vector2();

        private readonly IEnumerable<Molecule> m_products;
        private readonly LoopingCoroutine<object> m_assembleCoroutine;
        private readonly Arm m_assemblyArm;
        private readonly List<Arm> m_outputArms = new();

        private Molecule m_currentProduct;

        private Dictionary<int, int> m_outputLocationsById = new();

        public Star2MoleculeAssembler(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer, parent.OutputPosition)
        {
            m_products = products;
            m_assembleCoroutine = new LoopingCoroutine<object>(Assemble);

            new Glyph(this, new Vector2(1, 0), HexRotation.R60, GlyphType.MultiBonding);
            m_assemblyArm = new Arm(this, new Vector2(0, -3), HexRotation.R60, ArmType.Arm1, 3);

            new Track(this, m_assemblyArm.Transform.Position, HexRotation.R0, 1);

            int index = 0;
            HexRotation rotation = HexRotation.R300;
            foreach (var product in products)
            {
                var transform = new Transform2D(new Vector2(4 + index * 3, -3), rotation);
                transform = transform.Apply(new Transform2D(-new Vector2(1, 1), HexRotation.R0));
                transform = transform.Apply(product.GlyphTransform);

                new Product(this, transform.Position, transform.Rotation, product.ID);
                m_outputLocationsById[product.ID] = index;

                if (index > 0)
                {
                    m_outputArms.Add(new Arm(this, new Vector2(1 + index * 3, 0), HexRotation.R240, ArmType.Arm1, 3));
                }

                rotation = rotation.Rotate60Counterclockwise();
                index++;
            }
        }

        public override void AddAtom(Element element, int productID)
        {
            m_currentProduct = m_products.Single(product => product.ID == productID);
            m_assembleCoroutine.Next();
        }

        private IEnumerable<object> Assemble()
        {
            Writer.Write(m_assemblyArm, [Instruction.Grab, Instruction.MovePositive]);
            yield return null;

            for (int i = 0; i < 2; i++)
            {
                Writer.Write(m_assemblyArm, [Instruction.PivotClockwise, Instruction.PivotClockwise]);
                yield return null;
            }

            Writer.Write(m_assemblyArm, [Instruction.RotateClockwise, Instruction.Reset]);
            Writer.AdjustTime(-1);

            // Use the output arms to rotate the molecule to the corresponding product glyph
            int outputLocation = m_outputLocationsById[m_currentProduct.ID];
            if (outputLocation > 0)
            {
                for (int i = 0; i < outputLocation; i++)
                {
                    Writer.WriteGrabResetAction(m_outputArms[i], Instruction.RotateCounterclockwise);
                }
            }

            yield return null;
        }
    }
}
