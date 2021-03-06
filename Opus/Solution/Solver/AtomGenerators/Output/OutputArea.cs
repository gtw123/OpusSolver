﻿using System.Collections.Generic;
using System.Linq;

namespace Opus.Solution.Solver.AtomGenerators.Output
{
    /// <summary>
    /// The part of the solution that moves completed products to the corresponding output areas.
    /// </summary>
    public class OutputArea : SolverComponent
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

        public OutputArea(SolverComponent parent, ProgramWriter writer, IEnumerable<Molecule> products)
            : base(parent, writer,parent.OutputPosition)
        {
            m_products = products;

            CreateOutputs();

            var armPos = new Vector2(-1, 1);
            m_outputArm = new Arm(this, armPos, Direction.SE, MechanismType.Arm1);

            new Track(this, armPos, Direction.NE, m_outputs.Values.Max(o => o.DropPosition));
        }

        private void CreateOutputs()
        {
            m_outputs = new Dictionary<int, Output>();

            int totalHeight = 0;
            foreach (var product in m_products.Reverse())
            {
                // Stack the products vertically above each other
                var productLocation = new Vector2(0, totalHeight) + product.Origin;
                new Product(this, productLocation, product.Rotation, product.ID);

                // There will always be at least one atom in the first column, so use that as the one to grab
                int grabY = product.GetColumn(0).First().Position.Y;
                m_outputs[product.ID] = new Output { GrabPosition = grabY, DropPosition = grabY + totalHeight };

                totalHeight += product.Height;
            }
        }

        public void MoveProductToOutput(Molecule product)
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
