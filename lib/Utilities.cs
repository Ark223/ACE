using System.Runtime.CompilerServices;

namespace Ace
{
    /// <summary>
    /// General utility functions for combinatorics and bitwise operations.
    /// </summary>
    internal class Utilities
    {
        /// <summary>
        /// Computes the binomial coefficient C(n, k) = n! / (k! * (n - k)!).
        /// </summary>
        /// <param name="n">Size of the total set.</param>
        /// <param name="k">Number of elements to choose.</param>
        /// <returns>A binomial coefficient ("n choose k").</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Permutations(int n, int k)
        {
            int result = 1;
            if (k > n - k) k = n - k;
            for (int i = 1; i <= k; i++)
            {
                result *= n - k + i;
                result /= i;
            }
            return result;
        }

        /// <summary>
        /// Counts how many bits set to 1 exist in the specified value.
        /// </summary>
        /// <param name="value">Unsigned value to count bits in.</param>
        /// <returns>A number of set bits in the input value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte PopCount(ulong value)
        {
            byte result = 0;
            while (value != 0)
            {
                value &= value - 1;
                result++;
            }
            return result;
        }

        /// <summary>
        /// Counts how many low-order zero bits precede the first one-bit.
        /// </summary>
        /// <param name="value">Value to scan (must be non-zero).</param>
        /// <returns>A position of the least significant set bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte TrailingZeroCount(ulong value)
        {
            if (value == 0)
            {
                return 64;
            }
            byte result = 0;
            while ((value & 1ul) == 0)
            {
                result++;
                value >>= 1;
            }
            return result;
        }
    }
}
