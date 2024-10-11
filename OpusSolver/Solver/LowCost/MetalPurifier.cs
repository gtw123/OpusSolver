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
        private bool m_hasInputAtom = false;

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
            var transform = m_hasInputAtom ? Input2Transform : Input1Transform;
            ArmController.MoveMoleculeTo(transform, this);
            ArmController.DropMolecule();

            if (m_hasInputAtom)
            {
                GridState.UnregisterAtom(Input1Transform.Position, this);
                GridState.UnregisterAtom(Input2Transform.Position, this);

                var newMetal = element + 1;
                GridState.RegisterAtom(OutputTransform.Position, newMetal, this);

                var sequence = m_sequences[id];
                while (newMetal != sequence.TargetMetal)
                {
                    int metalIndex = newMetal - sequence.LowestMetalUsed - 1;
                    var storageLocation = m_storageLocations[metalIndex];
                    if (storageLocation.Atom == null)
                    {
                        storageLocation.Atom = new AtomCollection(newMetal, OutputTransform, this);
                        ArmController.SetMoleculeToGrab(storageLocation.Atom);
                        ArmController.MoveMoleculeTo(storageLocation.Transform, this);
                        ArmController.DropMolecule();
                        break;
                    }

                    ArmController.SetMoleculeToGrab(storageLocation.Atom);
                    ArmController.MoveMoleculeTo(Input2Transform, this);
                    ArmController.DropMolecule();

                    ArmController.SetMoleculeToGrab(new AtomCollection(newMetal, OutputTransform, this));
                    ArmController.MoveMoleculeTo(Input1Transform, this);
                    ArmController.DropMolecule();

                    GridState.UnregisterAtom(Input1Transform.Position, this);
                    GridState.UnregisterAtom(Input2Transform.Position, this);

                    newMetal++;
                    GridState.RegisterAtom(OutputTransform.Position, newMetal, this);

                    storageLocation.Atom = null;
                }
            }

            m_hasInputAtom = !m_hasInputAtom;
        }

        public override void Generate(Element element, int id)
        {
            ArmController.SetMoleculeToGrab(new AtomCollection(element, OutputTransform, this));
        }
    }
}
