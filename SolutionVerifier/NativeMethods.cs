using System;
using System.Runtime.InteropServices;

namespace SolutionVerifier
{
    internal static class NativeMethods
    {
        [DllImport("libverify")]
        public static extern IntPtr verifier_create(string puzzle_filename, string solution_filename);

        [DllImport("libverify")]
        public static extern void verifier_destroy(IntPtr verifier);

        [DllImport("libverify")]
        private static extern IntPtr verifier_error(IntPtr verifier);

        /// <summary>
        /// Wrapper function for calling verifier_error safely. This is required because verifier_error
        /// returns a const char* which should not be freed. If we simply declare it as returning a string
        /// then the default .NET marshalling will attempt to free the returned pointer, crashing the program.
        /// </summary>
        public static string GetVerifierError(IntPtr verifier)
        {
            return Marshal.PtrToStringAnsi(verifier_error(verifier));
        }

        [DllImport("libverify")]
        public static extern int verifier_error_cycle(IntPtr verifier);

        [DllImport("libverify")]
        public static extern int verifier_error_location_u(IntPtr verifier);

        [DllImport("libverify")]
        public static extern int verifier_error_location_v(IntPtr verifier);

        [DllImport("libverify")]
        public static extern void verifier_error_clear(IntPtr verifier);

        [DllImport("libverify")]
        public static extern int verifier_evaluate_metric(IntPtr verifier, string metric);
    }
}
