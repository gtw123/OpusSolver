using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpusSolver.IO
{
    public sealed class SolutionWriter : IDisposable
    {
        private Solution m_solution;
        private BinaryWriter m_writer;
        private Dictionary<Arm, int> m_armIDs;

        public static void WriteSolution(Solution solution, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using var writer = new SolutionWriter(solution, filePath);
            writer.WriteSolution();
        }

        public SolutionWriter(Solution solution, string filePath)
        {
            m_solution = solution;
            m_writer = new BinaryWriter(File.Create(filePath));

            // Generate consecutive IDs for all the arms in the solution
            m_armIDs = m_solution.GetObjects<Arm>().Select((arm, index) => (arm, index)).ToDictionary(pair => pair.arm, pair => pair.index);
        }

        public void Dispose()
        {
            if (m_writer != null)
            {
                m_writer.Dispose();
                m_writer = null;
            }
        }

        public void WriteSolution()
        {
            m_writer.Write(7); // Solution format
            m_writer.Write(m_solution.Puzzle.FileName);
            m_writer.Write(m_solution.Name);
            WriteMetrics(m_solution.Metrics);

            IEnumerable<GameObject> realObjects = m_solution.GetObjects<Glyph>();
            realObjects = realObjects.Concat(m_solution.GetObjects<Arm>())
                .Concat(m_solution.GetObjects<Track>())
                .Concat(m_solution.GetObjects<MoleculeInputOutput>())
                .ToList();
            
            m_writer.Write(realObjects.Count());
            foreach (var obj in realObjects)
            {
                WriteObject(obj);
            }
        }

        private void WriteMetrics(Metrics metrics)
        {
            if (metrics == null)
            {
                m_writer.Write(0);
                return;
            }

            // Number of metrics
            m_writer.Write(4);

            m_writer.Write(0);
            m_writer.Write(metrics.Cycles);
            m_writer.Write(1);
            m_writer.Write(metrics.Cost);
            m_writer.Write(2);
            m_writer.Write(metrics.Area);
            m_writer.Write(3);
            m_writer.Write(metrics.Instructions);
        }

        private void WriteObject(GameObject obj)
        {
            m_writer.Write(GetObjectName(obj));
            m_writer.Write((byte)1);
            
            var transform = obj.GetWorldTransform();
            if (obj is MoleculeInputOutput mol)
            {
                transform = transform.Apply(mol.Molecule.GlyphTransform);
            }

            WriteVector2(transform.Position);
            m_writer.Write((obj is Arm arm) ? arm.Extension : 1);

            // By convention, certain objects don't have a rotation written to the solution file
            bool ignoreRotation = obj is Track || obj is MoleculeInputOutput m && m.Molecule.Atoms.Count() == 1;
            m_writer.Write(ignoreRotation ? 0 : transform.Rotation.IntValue);

            int id = (obj is MoleculeInputOutput m2) ? m2.Molecule.ID : 0;
            m_writer.Write(id);

            WriteInstructions(obj);

            if (obj is Track track)
            {
                WriteTrack(track);
            }

            m_writer.Write((obj is Arm arm2) ? m_armIDs[arm2] : 0);
        }

        private string GetObjectName(GameObject obj) => obj switch
        {
            Arm arm => arm.Type switch
            {
                ArmType.Arm1 => "arm1",
                ArmType.Arm2 => "arm2",
                ArmType.Arm3 => "arm3",
                ArmType.Arm6 => "arm6",
                ArmType.Piston => "piston",
                ArmType.VanBerlo => "baron",
                _ => throw new ArgumentException($"Unknown arm type {arm.Type}")
            },
            Track => "track",
            Glyph glyph => glyph.Type switch
            {
                GlyphType.Bonding => "bonder",
                GlyphType.MultiBonding => "bonder-speed",
                GlyphType.TriplexBonding => "bonder-prisma",
                GlyphType.Unbonding => "unbonder",
                GlyphType.Calcification => "glyph-calcification",
                GlyphType.Duplication => "glyph-duplication",
                GlyphType.Projection => "glyph-projection",
                GlyphType.Purification => "glyph-purification",
                GlyphType.Animismus => "glyph-life-and-death",
                GlyphType.Disposal => "glyph-disposal",
                GlyphType.Equilibrium => "glyph-marker",
                GlyphType.Unification => "glyph-unification",
                GlyphType.Dispersion => "glyph-dispersion",
                _ => throw new ArgumentException($"Unknown glyph type {glyph.Type}")
            },
            Reagent => "input",
            Product product => product.Molecule.HasRepeats ? "out-rep" : "out-std",
            _ => throw new ArgumentException($"Unknown object type {obj.GetType()}")
        };

        private void WriteInstructions(GameObject obj)
        {
            if (obj is Arm arm)
            {
                var instructions = m_solution.Program.GetArmInstructions(arm);
                var writableInstructions = instructions.Select((instr, index) => (instr, index))
                    .Where(pair => pair.instr != Instruction.None && pair.instr != Instruction.Wait).ToList();

                m_writer.Write(writableInstructions.Count);
                foreach (var (instruction, index) in writableInstructions)
                {
                    m_writer.Write(index);
                    m_writer.Write(GetInstructionCode(instruction));
                }
            }
            else
            {
                m_writer.Write(0);
            }
        }

        private byte GetInstructionCode(Instruction instruction)
        {
            char c = instruction switch
            {
                Instruction.PivotCounterclockwise => 'p',
                Instruction.Extend => 'E',
                Instruction.PivotClockwise => 'P',
                Instruction.Drop => 'g',
                Instruction.MoveNegative => 'a',
                Instruction.RotateCounterclockwise => 'r',
                Instruction.Retract => 'e',
                Instruction.RotateClockwise => 'R',
                Instruction.Grab => 'G',
                Instruction.MovePositive => 'A',
                Instruction.PeriodOverride => 'O',
                Instruction.Reset => 'X',
                Instruction.Repeat => 'C',
                _ => throw new ArgumentException($"Invalid instruction: {instruction}")
            };

            // Since we're only using chars between 0 and 255 it should be safe to just cast this
            return (byte)c;
        }

        private void WriteTrack(Track track)
        {
            var path = track.Path;
            m_writer.Write(path.Count());

            // Track objects themselves always have a 0 rotation in the solution file, so we need to explicitly
            // rotate the path locations.
            var transform = track.GetWorldTransform();
            foreach (var pos in path)
            {
                WriteVector2(pos.RotateBy(transform.Rotation));
            }
        }

        private void WriteVector2(Vector2 vector)
        {
            m_writer.Write(vector.X);
            m_writer.Write(vector.Y);
        }
    }
}
