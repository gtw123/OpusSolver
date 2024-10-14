using OpusSolver.Solver.ElementGenerators;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpusSolver.Solver.LowCost
{
    /// <summary>
    /// Generates an atom of a metal from atoms of lower metals.
    /// </summary>
    public class MetalPurifier : LowCostAtomGenerator
    {
        private IReadOnlyList<MetalPurifierGenerator.PurificationSequence> m_sequences;

        private class StorageLocation
        {
            public AtomCollection Atom;
            public Transform2D Transform;
        }

        private readonly StorageLocation[] m_storageLocations;
        private AtomCollection m_atomOnGlyphInput;

        private static readonly Transform2D Input1Transform = new Transform2D(new Vector2(-1, 1), HexRotation.R0);
        private static readonly Transform2D Input2Transform = new Transform2D(new Vector2(0, 0), HexRotation.R0);
        private static readonly Transform2D OutputTransform = new Transform2D(new Vector2(-1, 0), HexRotation.R0);

        public override IEnumerable<Transform2D> RequiredAccessPoints => new[] { Input2Transform, OutputTransform, Input1Transform }.Concat(m_storageLocations.Select(s => s.Transform));

        public override int RequiredWidth => Math.Max(2, m_storageLocations.Length + 1);

        public MetalPurifier(ProgramWriter writer, ArmArea armArea, IReadOnlyList<MetalPurifierGenerator.PurificationSequence> sequences)
            : base(writer, armArea)
        {
            m_sequences = sequences;

            int size = sequences.Max(s => PeriodicTable.GetMetalDifference(s.LowestMetalUsed, s.TargetMetal)) - 1;
            m_storageLocations = new StorageLocation[size];
            for (int i = 0; i < size; i++)
            {
                m_storageLocations[i] = new StorageLocation { Transform = new Transform2D(new Vector2(-2 - i, 2 + i), HexRotation.R60) };
            }

            new Glyph(this, new(0, 0), HexRotation.R120, GlyphType.Purification);
        }

        public override void Consume(Element element, int id)
        {
            var sequence = m_sequences[id];

            if (element == sequence.LowestMetalUsed)
            {
                if (m_atomOnGlyphInput == null)
                {
                    m_atomOnGlyphInput = ArmController.DropMoleculeAt(Input1Transform, this);
                    return;
                }

                ArmController.DropMoleculeAt(Input2Transform, this);
                m_atomOnGlyphInput = null;
            }
            else
            {
                int metalIndex = element - sequence.LowestMetalUsed - 1;
                var storageLocation = m_storageLocations[metalIndex];
                if (storageLocation.Atom == null)
                {
                    storageLocation.Atom = ArmController.DropMoleculeAt(storageLocation.Transform, this);
                    return;
                }

                if (m_atomOnGlyphInput != null)
                {
                    // We want to combine two metals but there's already a lower metal on one of the glyph inputs.
                    // Temporarily move this atom out the way, then temporarily stash it in the location where the
                    // other metal is being stored.
                    ArmController.DropMoleculeAt(Input2Transform, this);

                    ArmController.SetMoleculeToGrab(m_atomOnGlyphInput);
                    ArmController.DropMoleculeAt(OutputTransform, this);

                    ArmController.SetMoleculeToGrab(storageLocation.Atom);
                    ArmController.DropMoleculeAt(Input1Transform, this);

                    ArmController.SetMoleculeToGrab(m_atomOnGlyphInput);
                    ArmController.DropMoleculeAt(storageLocation.Transform, this);
                }
                else
                {
                    ArmController.DropMoleculeAt(Input2Transform, this);

                    ArmController.SetMoleculeToGrab(storageLocation.Atom);
                    ArmController.DropMoleculeAt(Input1Transform, this);
                }

                storageLocation.Atom = null;
            }

            GridState.UnregisterAtom(Input1Transform.Position, this);
            GridState.UnregisterAtom(Input2Transform.Position, this);

            var newMetal = element + 1;
            GridState.RegisterAtom(OutputTransform.Position, newMetal, this);

            while (newMetal != sequence.TargetMetal)
            {
                int metalIndex = newMetal - sequence.LowestMetalUsed - 1;
                var storageLocation = m_storageLocations[metalIndex];
                if (storageLocation.Atom == null)
                {
                    storageLocation.Atom = new AtomCollection(newMetal, OutputTransform, this);
                    ArmController.SetMoleculeToGrab(storageLocation.Atom);
                    ArmController.DropMoleculeAt(storageLocation.Transform, this);
                    break;
                }

                ArmController.SetMoleculeToGrab(storageLocation.Atom);
                ArmController.DropMoleculeAt(Input2Transform, this);

                ArmController.SetMoleculeToGrab(new AtomCollection(newMetal, OutputTransform, this));
                ArmController.DropMoleculeAt(Input1Transform, this);

                GridState.UnregisterAtom(Input1Transform.Position, this);
                GridState.UnregisterAtom(Input2Transform.Position, this);

                newMetal++;
                GridState.RegisterAtom(OutputTransform.Position, newMetal, this);

                storageLocation.Atom = null;
            }

            if (m_atomOnGlyphInput != null)
            {
                // Move the stash atom back onto the glyph input
                ArmController.SetMoleculeToGrab(m_atomOnGlyphInput);
                ArmController.DropMoleculeAt(Input1Transform, this);
            }
        }

        public override void Generate(Element element, int id)
        {
            ArmController.SetMoleculeToGrab(new AtomCollection(element, OutputTransform, this));
        }
    }
}
