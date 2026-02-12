using System;
using System.Collections.Generic;

namespace Ace
{
    /// <summary>
    /// Thread-safe random number generator using per-thread instances.
    /// </summary>
    internal class Random
    {
        [ThreadStatic]
        private static System.Random _local;

        /// <summary>
        /// Gets the thread-local number generator instance.
        /// </summary>
        private static System.Random Instance
        {
            get
            {
                if (_local == null)
                {
                    _local = new System.Random(0x5851f42d);
                }
                return _local;
            }
        }

        /// <summary>
        /// Bind a deterministic stream to the current thread.
        /// </summary>
        /// <param name="seed">Seed for number generator.</param>
        internal static void Bind(int seed)
        {
            _local = new System.Random(seed);
        }

        /// <summary>
        /// Returns a random integer in the [0, Max) range.
        /// </summary>
        /// <returns>A random value in specified range.</returns>
        internal static int Next(int max)
        {
            return Instance.Next(max);
        }

        /// <summary>
        /// Returns a random integer in the [Min, Max) range.
        /// </summary>
        /// <param name="min">Lower bound (inclusive).</param>
        /// <param name="max">Upper bound (exclusive).</param>
        /// <returns>A random value in specified range.</returns>
        internal static int Next(int min, int max)
        {
            return Instance.Next(min, max);
        }

        /// <summary>
        /// Shuffles a list in-place using Fisher-Yates method.
        /// </summary>
        /// <typeparam name="T">Element type.</typeparam>
        /// <param name="list">List to shuffle.</param>
        internal static void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Instance.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
