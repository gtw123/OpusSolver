﻿using System.Collections.Generic;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Generates an atom of a metal from an atom of a lower metal and quicksilver atoms.
    /// </summary>
    public class MetalProjector : LowCostAtomGenerator
    {
        private Element? m_currentMetal;

        private static readonly Transform2D QuicksilverTransform = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D MetalTransform = new Transform2D(new Vector2(-1, 1), HexRotation.R0);

        public override int RequiredWidth => 2;

        public override IEnumerable<Transform2D> RequiredAccessPoints => [QuicksilverTransform, MetalTransform];


        public MetalProjector(ProgramWriter writer, ArmArea armArea)
            : base(writer, armArea)
        {
            new Glyph(this, QuicksilverTransform.Position, HexRotation.R120, GlyphType.Projection);
        }

        public override void Consume(Element element, int id)
        {
            if (m_currentMetal == null)
            {
                ArmController.MoveAtomsTo(MetalTransform, this);
                ArmController.DropAtoms();
                m_currentMetal = element;
            }
            else
            {
                ArmController.MoveAtomsTo(QuicksilverTransform, this);
                ArmController.DropAtoms(addToGrid: false);
                m_currentMetal++;
                GridState.RegisterAtom(MetalTransform.Position, m_currentMetal, this);
            }
        }

        public override void Generate(Element element, int id)
        {
            ArmController.MoveGrabberTo(MetalTransform, this);
            ArmController.GrabAtoms(new AtomCollection(element, MetalTransform, this));
            m_currentMetal = null;
        }
    }
}
