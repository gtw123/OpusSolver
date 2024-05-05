using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Opus.IO
{
    public sealed class PuzzleReader : IDisposable
    {
        private string m_filePath;
        private BinaryReader m_reader;

        public static Puzzle ReadPuzzle(string filePath)
        {
            using var reader = new PuzzleReader(filePath);
            return reader.ReadPuzzle();
        }

        public PuzzleReader(string filePath)
        {
            m_filePath = filePath;
            m_reader = new BinaryReader(File.OpenRead(filePath));
        }

        public void Dispose()
        {
            if (m_reader != null)
            {
                m_reader.Dispose();
                m_reader = null;
            }
        }

        public Puzzle ReadPuzzle()
        {
            var version = m_reader.ReadInt32();
            if (version != 3)
            {
                throw new ParseException($"Unsupported puzzle file version: {version}");
            }

            string puzzleName = m_reader.ReadString();
            m_reader.ReadUInt64(); // creator ID; unused

            ulong partFlags = m_reader.ReadUInt64();
            var allowedGlyphs = ParseAvailableGlyphs(partFlags);
            var allowedMechanisms = ParseAllowedMechanisms(partFlags);

            var reagents = new List<Molecule>();
            int inputCount = m_reader.ReadInt32();
            for (int i = 0; i < inputCount; i++)
            {
                reagents.Add(ParseMolecule(MoleculeType.Reagent, i));
            }

            var products = new List<Molecule>();
            int outputCount = m_reader.ReadInt32();
            for (int i = 0; i < outputCount; i++)
            {
                products.Add(ParseMolecule(MoleculeType.Product, i));
            }

            int outputTargetScale = m_reader.ReadInt32(); // TODO: What to do with this?
            bool isProduction = m_reader.ReadBoolean();
            if (isProduction)
            {
                throw new ParseException("OpusSolver doesn't currently support production puzzles.");
            }

            return new Puzzle(Path.GetFileNameWithoutExtension(m_filePath), puzzleName,
                products, reagents, allowedMechanisms, allowedGlyphs);
        }

        private static readonly Dictionary<ulong, GlyphType[]> sm_availablePartMapping = new()
        {
            { 0x0001, new[] { GlyphType.Bonding } },
            { 0x0002, new[] { GlyphType.Unbonding } },
            { 0x0004, new[] { GlyphType.MultiBonding } },
            { 0x0008, new[] { GlyphType.TriplexBonding } },
            { 0x0010, new[] { GlyphType.Calcification } },
            { 0x0020, new[] { GlyphType.Duplication } },
            { 0x0040, new[] { GlyphType.Projection } },
            { 0x0080, new[] { GlyphType.Purification } },
            { 0x0100, new[] { GlyphType.Animismus } },
            { 0x0200, new[] { GlyphType.Disposal } },
            { 0x0400, new[] { GlyphType.Unification, GlyphType.Dispersion } },
        };

        private HashSet<GlyphType> ParseAvailableGlyphs(ulong partFlags)
        {
            // Example partFlags:
            // No glyphs allowed:    0x00000000 07 c0 00 0f
            // Just bonding allowed: 0x00000000 07 c0 01 0f

            // The lowest byte specifies whether arms, pistons or tracks etc. are available, but it's always 0x0f
            // for puzzles created within the game, so we ignore it.
            partFlags >>= 8;

            // The next two bytes are the allowed glyphs. We ignore the highest 4 bits as they specify whether certain
            // commands are allowed, but they're always 0xc in puzzles created within the game.
            ulong glyphFlags = partFlags & 0xfff;

            var allowedGlyphs = new HashSet<GlyphType> { GlyphType.Equilibrium };
            foreach (var (flag, glyphs) in sm_availablePartMapping)
            {
                if ((glyphFlags & flag) != 0)
                {
                    allowedGlyphs.UnionWith(glyphs);
                    glyphFlags &= ~flag;
                }
            }

            if (glyphFlags != 0)
            {
                throw new ParseException($"Puzzle allows unknown glyphs: {glyphFlags:X}");
            }

            return allowedGlyphs;
        }

        private HashSet<MechanismType> ParseAllowedMechanisms(ulong partFlags)
        {
            var allowedMechanisms = new HashSet<MechanismType>
            {
                MechanismType.Arm1,
                MechanismType.Arm2,
                MechanismType.Arm3,
                MechanismType.Arm6,
                MechanismType.Piston,
                MechanismType.Track,
            };

            // Strip off the glyph flags
            partFlags >>= 24;

            // The next byte is either 17 if Van Berlo's wheel is allowed, or 07 otherwise
            if ((partFlags & 0x10) != 0)
            {
                allowedMechanisms.Add(MechanismType.VanBerlo);
            }

            return allowedMechanisms;
        }

        private static readonly Dictionary<int, Element> sm_elementMapping = new()
        {
            { 1, Element.Salt },
            { 2, Element.Air },
            { 3, Element.Earth },
            { 4, Element.Fire },
            { 5, Element.Water },
            { 6, Element.Quicksilver },
            { 7, Element.Gold },
            { 8, Element.Silver },
            { 9, Element.Copper },
            { 10, Element.Iron },
            { 11, Element.Tin },
            { 12, Element.Lead },
            { 13, Element.Vitae },
            { 14, Element.Mors },
            { 15, Element.Repeat },
            { 16, Element.Quintessence },
        };

        private static readonly Dictionary<int, BondType> sm_bondTypeMapping = new()
        {
            { 1, BondType.Single },
            { 14, BondType.Triplex }, // Triplex Red + Black + Yellow
        };

        private static readonly Dictionary<Vector2, int> sm_bondDirectionMapping = new()
        {
            { new Vector2(1, 0), Direction.E },
            { new Vector2(0, 1), Direction.NE },
            { new Vector2(-1, 1), Direction.NW },
            { new Vector2(-1, 0), Direction.W },
            { new Vector2(0, -1), Direction.SW },
            { new Vector2(1, -1), Direction.SE },
        };

        private class AtomInfo
        {
            public Element Element;
            public Vector2 Position;
            public Dictionary<int, BondType> Bonds = new();

            public void AddBond(int direction, BondType bondType)
            {
                if (Bonds.ContainsKey(direction))
                {
                    throw new ParseException($"More than one bond found for atom at {Position} in direction {direction}.");
                }

                Bonds[direction] = bondType;
            }

            public Atom BuildAtom()
            {
                var bonds = new List<BondType>();
                for (int direction = 0; direction < Direction.Count; direction++)
                {
                    var bondType = BondType.None;
                    Bonds.TryGetValue(direction, out bondType);
                    bonds.Add(bondType);
                }

                return new Atom(Element, bonds, Position);
            }
        }

        private Molecule ParseMolecule(MoleculeType moleculeType, int id)
        {
            try
            {
                var atoms = new List<AtomInfo>();

                int atomCount = m_reader.ReadInt32();
                for (int i = 0; i < atomCount; i++)
                {
                    var atomType = m_reader.ReadByte();
                    if (!sm_elementMapping.TryGetValue(atomType, out var element))
                    {
                        throw new ParseException($"Atom {i} has unknown atom type {atomType}.");
                    }

                    var position = new Vector2(m_reader.ReadSByte(), m_reader.ReadSByte());
                    atoms.Add(new AtomInfo { Element = element, Position = position });
                }

                int bondCount = m_reader.ReadInt32();
                for (int i = 0; i < bondCount; i++)
                {
                    var bondFlags = m_reader.ReadByte();
                    if (!sm_bondTypeMapping.TryGetValue(bondFlags, out var bondType))
                    {
                        throw new ParseException($"Bond {i} has unsupported bond type {bondFlags}.");
                    }

                    var fromPosition = new Vector2(m_reader.ReadSByte(), m_reader.ReadSByte());
                    var fromAtom = atoms.Where(atom => atom.Position == fromPosition).FirstOrDefault() ?? throw new ParseException($"Bond {i} has invalid 'from' position: no atom is present at {fromPosition}.");

                    var toPosition = new Vector2(m_reader.ReadSByte(), m_reader.ReadSByte());
                    var toAtom = atoms.Where(atom => atom.Position == toPosition).FirstOrDefault() ?? throw new ParseException($"Bond {i} has invalid 'to' position: no atom is present at {toPosition}.");

                    var bondOffset = toPosition - fromPosition;
                    if (!sm_bondDirectionMapping.TryGetValue(bondOffset, out var bondDirection))
                    {
                        throw new ParseException($"Bond {i} has invalid offset {bondOffset} from {fromPosition} to {toPosition}.");
                    }

                    fromAtom.AddBond(bondDirection, bondType);
                    toAtom.AddBond(DirectionUtil.Rotate180(bondDirection), bondType);
                }

                return new Molecule(moleculeType, atoms.Select(atom => atom.BuildAtom()), id);
            }
            catch (ParseException e)
            {
                throw new ParseException($"Error parsing {moleculeType} {id}: " + e.Message);
            }
        }
    }
}
