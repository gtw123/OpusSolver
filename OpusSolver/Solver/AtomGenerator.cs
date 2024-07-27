namespace OpusSolver.Solver
{
    /// <summary>
    /// A solution component which generates atoms from other atoms.
    /// </summary>
    public abstract class AtomGenerator : SolverComponent
    {
        protected Arm OutputArm { get; set; }

        public override Vector2 OutputPosition
        {
            get
            {
                // Assume W, SW, SE rotates CCW and NW, NE, E rotates CW.
                var rot = OutputArm.Transform.Rotation;
                var dir = (rot == HexRotation.R180 || rot == HexRotation.R240 || rot == HexRotation.R300) ? rot.Rotate60Counterclockwise()
                    : rot.Rotate60Clockwise();
                return OutputArm.Transform.Position.OffsetInDirection(dir, OutputArm.Extension);
            }
        }

        protected AtomGenerator(ProgramWriter writer)
            : base(writer)
        {
        }

        /// <summary>
        /// Consumes an atom of the specified element. The exact meaning of "consume"
        /// depends on the generator, but it generally means that atom will be stored
        /// temporarily for conversion into another atom.
        /// </summary>
        public virtual void Consume(Element element, int id)
        {
        }

        /// <summary>
        /// Generates an atom of the specified element.
        /// </summary>
        public virtual void Generate(Element element, int id)
        {
        }

        /// <summary>
        /// Passes an atom through this generator without modifying it.
        /// </summary>
        public virtual void PassThrough(Element element)
        {
        }

        /// <summary>
        /// Called after the solution has been created for all products to allow optional cleanup.
        /// </summary>
        public virtual void EndSolution()
        {
        }

        /// <summary>
        /// Called after the solution has been created for all products. Gives the component an opportunity
        /// to remove any parts that aren't used (including itself).
        /// </summary>
        public virtual void OptimizeParts()
        {
        }
    }
}
