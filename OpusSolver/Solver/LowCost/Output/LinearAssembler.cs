﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost.Output
{
    /// <summary>
    /// Assembles molecules which are shaped in a single long chain of atoms.
    /// </summary>
    public class LinearAssembler : MoleculeAssembler
    {
        private readonly IEnumerable<Molecule> m_products;
        private readonly Dictionary<int, Product> m_outputs = new();

        private readonly LoopingCoroutine<object> m_assembleCoroutine;
        private Molecule m_currentProduct;

        public const int MaxProducts = 2;

        public override int RequiredWidth => 2;

        private static readonly Transform2D LowerBonderPosition = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D UpperBonderPosition = new Transform2D(new Vector2(-1, 1), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => [LowerBonderPosition, UpperBonderPosition];

        private static readonly HexRotation OutputRotationOffset = HexRotation.R60;

        public LinearAssembler(SolverComponent parent, ProgramWriter writer, ArmArea armArea, IEnumerable<Molecule> products)
            : base(parent, writer, armArea)
        {
            if (products.Any(p => p.Height != 1))
            {
                throw new ArgumentException($"{nameof(LinearAssembler)} only works with linear molecules that are orientated horizontally.");
            }

            if (products.Any(r => r.HasTriplex))
            {
                throw new ArgumentException($"{nameof(LinearAssembler)} doesn't work with triplex bonds.");
            }

            if (products.Count() > MaxProducts)
            {
                throw new ArgumentException($"{nameof(LinearAssembler)} can't handle more than {MaxProducts} products.");
            }

            m_products = products;
            m_assembleCoroutine = new LoopingCoroutine<object>(Assemble);

            new Glyph(this, LowerBonderPosition.Position, HexRotation.R120, GlyphType.Bonding);

            var productList = products.Reverse().ToList();

            var armPos = UpperBonderPosition.Position - new Vector2(ArmArea.ArmLength, 0);
            var pos = UpperBonderPosition.Position.RotateAbout(armPos, HexRotation.R60);
            AddProductOutput(productList[0], new Transform2D(pos, HexRotation.R60 + OutputRotationOffset));

            if (productList.Count > 1)
            {
                pos = UpperBonderPosition.Position.RotateAbout(armPos, HexRotation.R120);
                AddProductOutput(productList[1], new Transform2D(pos, HexRotation.R120 + OutputRotationOffset));
            }
        }

        private void AddProductOutput(Molecule product, Transform2D transform)
        {
            var output = new Product(this, transform.Position, transform.Rotation, product);
            m_outputs[product.ID] = output;
        }

        public override void AddAtom(Element element, int productID)
        {
            m_currentProduct = m_products.Single(product => product.ID == productID);
            m_assembleCoroutine.Next();
        }

        private IEnumerable<object> Assemble()
        {
            var placedAtoms = new List<Atom>();

            for (int x = m_currentProduct.Width - 1; x >= 0; x--)
            {
                // Bond the atom to the other product atoms (if any)
                ArmArea.MoveGrabberTo(this, LowerBonderPosition);

                if (placedAtoms.Any())
                {
                    var lastAtom = placedAtoms.Last();
                    foreach (var atom in placedAtoms)
                    {
                        var pos = UpperBonderPosition.Position + (atom.Position - lastAtom.Position).RotateBy(HexRotation.R120);
                        GridState.RegisterAtom(pos, null, this);
                    }
                }

                ArmArea.MoveGrabberTo(this, UpperBonderPosition);
    
                if (x == 0)
                {
                    // Do an extra pivot to help avoid hitting reagents in a counterclockwise direction
                    ArmArea.PivotClockwise();

                    var transform = m_outputs[m_currentProduct.ID].Transform;
                    ArmArea.MoveGrabberTo(this, new Transform2D(transform.Position, transform.Rotation - OutputRotationOffset));
                }
                else
                {
                    placedAtoms.Add(m_currentProduct.GetAtom(new Vector2(x, 0)));

                    var lastAtom = placedAtoms.Last();
                    foreach (var atom in placedAtoms)
                    {
                        var pos = UpperBonderPosition.Position + (atom.Position - lastAtom.Position).RotateBy(HexRotation.R120);
                        GridState.RegisterAtom(pos, atom.Element, this);
                    }
                }

                ArmArea.DropAtom();
                yield return null;
            }
        }
    }
}
