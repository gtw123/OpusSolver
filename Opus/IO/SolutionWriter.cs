using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Opus.Solution;

namespace Opus.IO
{
    public sealed class SolutionWriter : IDisposable
    {
        private PuzzleSolution m_solution;
        private BinaryWriter m_writer;
        private Dictionary<Arm, int> m_armIDs;

        public static void WriteSolution(PuzzleSolution solution, string filePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            using var writer = new SolutionWriter(solution, filePath);
            writer.WriteSolution();
        }

        public SolutionWriter(PuzzleSolution solution, string filePath)
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
            m_writer.Write(0); // TODO: Write metrics

            IEnumerable<GameObject> realObjects = m_solution.GetObjects<Glyph>();
            realObjects = realObjects.Concat(m_solution.GetObjects<Mechanism>())
                .Concat(m_solution.GetObjects<Reagent>())
                .Concat(m_solution.GetObjects<Product>())
                .ToList();
            
            m_writer.Write(realObjects.Count());
            foreach (var obj in realObjects)
            {
                WriteObject(obj);
            }
        }

        private void WriteObject(GameObject obj)
        {
            m_writer.Write(GetObjectName(obj));
            m_writer.Write((byte)1);
            WriteVector2(obj.GetWorldPosition());
            m_writer.Write((obj is Arm arm) ? arm.Extension : 1);
            m_writer.Write(obj.Rotation);

            int id = (obj is Product product) ? product.ID : (obj is Reagent reagent) ? reagent.ID : 0;
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
            Mechanism mechanism => mechanism.Type switch
            {
                MechanismType.Arm1 => "arm1",
                MechanismType.Arm2 => "arm2",
                MechanismType.Arm3 => "arm3",
                MechanismType.Arm6 => "arm6",
                MechanismType.Piston => "piston",
                MechanismType.Track => "track",
                MechanismType.VanBerlo => "baron",
                _ => throw new ArgumentException($"Unknown mechanism type {mechanism.Type}")
            },
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
                _ => throw new ArgumentException($"Unknown mechanism type {glyph.Type}")
            },
            Reagent => "input",
            Product product => m_solution.Puzzle.Products[product.ID].HasRepeats ? "out-rep" : "out-std",
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
            foreach (var pos in path)
            {
                WriteVector2(pos);
            }
        }

        private void WriteVector2(Vector2 vector)
        {
            m_writer.Write(vector.X);
            m_writer.Write(vector.Y);
        }
    }
}
