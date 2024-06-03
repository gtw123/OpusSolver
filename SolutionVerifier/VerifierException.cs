using System;
using System.Text;

namespace SolutionVerifier
{
    public class VerifierException : Exception
    {
        public int? Cycle { get; set; }
        public (int, int)? Location { get; set; }

        public VerifierException(string message)
            : base(message)
        {
        }

        public VerifierException(IntPtr verifier, bool includeCycleAndLocation)
            : base(NativeMethods.GetVerifierError(verifier))
        {
            // These don't make sense for some types of errors (e.g. failing to load a puzzle) so only
            // get them if requested
            if (includeCycleAndLocation)
            {
                Cycle = NativeMethods.verifier_error_cycle(verifier);
                Location = (NativeMethods.verifier_error_location_u(verifier), NativeMethods.verifier_error_location_v(verifier));
            }

            NativeMethods.verifier_error_clear(verifier);
        }

        public override string Message
        {
            get
            {
                var message = new StringBuilder(base.Message);
                if (Cycle.HasValue)
                {
                    message.Append($" on cycle {Cycle}");
                }
                if (Location.HasValue)
                {
                    message.Append($" at {Location}");
                }

                return message.ToString();
            }
        }
    }
}
