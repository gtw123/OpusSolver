﻿using System;
using System.Collections.Generic;
using System.Linq;
using static System.FormattableString;

namespace OpusSolver.Solver.AtomGenerators.Output
{
    /// <summary>
    /// A simple output area used when all the products are single atoms.
    /// </summary>
    public class SimpleOutputArea : AtomGenerator
    {
        public override Vector2 OutputPosition => new Vector2();

        public const int MaxProducts = 4;

        private Dictionary<int, Arm> m_outputArms = new Dictionary<int, Arm>();

        public SimpleOutputArea(ProgramWriter writer, IEnumerable<Molecule> products)
            : base(writer)
        {
            if (products.Any(r => r.Atoms.Count() > 1))
            {
                throw new ArgumentException("SimpleOutputArea can't handle products with multiple atoms.");
            }

            if (products.Count() > MaxProducts)
            {
                throw new ArgumentException(Invariant($"SimpleOutputArea can't handle more than {MaxProducts} products."));
            }

            var dir = HexRotation.R0;
            foreach (var product in products)
            {
                CreateOutput(product, dir);
                dir = dir.Rotate60Clockwise();
            }
        }

        private void CreateOutput(Molecule product, HexRotation direction)
        {
            var pos = new Vector2(0, 0).OffsetInDirection(direction, 1);
            new Product(this, pos, product.Rotation, product.ID);
            m_outputArms[product.ID] = new Arm(this, pos * 2, direction.Rotate180(), MechanismType.Piston, extension: 2);
        }

        public override void Consume(Element element, int id)
        {
            Writer.WriteGrabResetAction(m_outputArms[id], Instruction.Retract);
        }
    }
}
