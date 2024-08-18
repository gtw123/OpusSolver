using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.Standard.Output
{
    /// <summary>
    /// Transports assembled products to their output locations.
    /// </summary>
    public class ProductConveyor : SolverComponent
    {
        private class Output
        {
            public int GrabPosition;
            public int DropPosition;
        }

        private Arm m_outputArm;
        private Dictionary<int, Output> m_outputs;

        private IEnumerable<Molecule> m_products;

        public override Vector2 OutputPosition => new Vector2();

        public ProductConveyor(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer, parent.OutputPosition)
        {
            m_products = products;

            CreateOutputs();

            var armPos = new Vector2(-1, 1);
            m_outputArm = new Arm(this, armPos, HexRotation.R300, ArmType.Arm1);

            new Track(this, armPos, HexRotation.R60, m_outputs.Values.Max(o => o.DropPosition));
        }

        private void CreateOutputs()
        {
            m_outputs = new Dictionary<int, Output>();

            int totalHeight = 0;
            foreach (var product in m_products.Reverse())
            {
                // Stack the products vertically above each other
                new Product(this, new Vector2(0, totalHeight), HexRotation.R0, product);

                // There will always be at least one atom in the first column, so use that as the one to grab
                int grabY = product.GetColumn(0).First().Position.Y;
                m_outputs[product.ID] = new Output { GrabPosition = grabY, DropPosition = grabY + totalHeight };

                totalHeight += product.Height;
            }
        }

        public void MoveProductToOutputLocation(Molecule product)
        {
            var output = m_outputs[product.ID];

            if (output.DropPosition != output.GrabPosition)
            {
                Writer.AdjustTime(-output.GrabPosition);
                Writer.Write(m_outputArm, Enumerable.Repeat(Instruction.MovePositive, output.GrabPosition));
                Writer.Write(m_outputArm, Instruction.Grab);
                Writer.Write(m_outputArm, Enumerable.Repeat(Instruction.MovePositive, output.DropPosition - output.GrabPosition));
                Writer.Write(m_outputArm, Instruction.Reset);
            }
        }
    }
}
