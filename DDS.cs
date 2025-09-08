using System;
using System.Runtime.InteropServices;
using static Ace.Extensions;

namespace Ace
{
    /// <summary>
    /// Managed wrapper for the Bridge Calculator double‑dummy solver.
    /// </summary>
    internal sealed class DDS : IDisposable
    {
        private IntPtr _instance = IntPtr.Zero;

        /// <summary>
        /// Provides access to the native double-dummy solver functions.
        /// </summary>
        private static class Solver
        {
            [DllImport("libbcalcdds", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern IntPtr bcalcDDS_new(string format, string hands, int strain, int leader);

            [DllImport("libbcalcdds", CallingConvention = CallingConvention.Cdecl)]
            internal static extern void bcalcDDS_delete(IntPtr solver);

            [DllImport("libbcalcdds", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void bcalcDDS_exec(IntPtr solver, string command);

            [DllImport("libbcalcdds", CallingConvention = CallingConvention.Cdecl)]
            internal static extern int bcalcDDS_getTricksToTake(IntPtr solver);
        }

        /// <summary>
        /// Creates a DDS wrapper for an existing unmanaged solver pointer.
        /// </summary>
        /// <param name="pointer">Native pointer to DDS instance.</param>
        internal DDS(IntPtr pointer)
        {
            this._instance = pointer;
        }

        /// <summary>
        /// Creates a new double-dummy solver for the specified deal.
        /// </summary>
        /// <param name="hands">String representation of hands.</param>
        /// <param name="strain">Contract strain (suit or NT).</param>
        /// <param name="leader">Player to lead a card.</param>
        internal DDS(string hands, Suit strain, Player leader)
        {
            this._instance = Solver.bcalcDDS_new("PBN", hands, (int)strain, (int)leader);
        }

        /// <summary>
        /// Sends a command to the solver for execution.
        /// </summary>
        /// <param name="command">Command string.</param>
        internal void Execute(string command)
        {
            Solver.bcalcDDS_exec(this._instance, command);
        }

        /// <summary>
        /// Gets how many tricks the leader's side can win from this state.
        /// </summary>
        /// <returns>A number of tricks available at this point.</returns>
        internal int Tricks()
        {
            return Solver.bcalcDDS_getTricksToTake(this._instance);
        }

        /// <summary>
        /// Finalizer for <see cref="DDS"/> instance.
        /// </summary>
        ~DDS() => this.Release();

        /// <summary>
        /// Frees the native solver instance, if allocated.
        /// </summary>
        private void Release()
        {
            if (this._instance != IntPtr.Zero)
            {
                Solver.bcalcDDS_delete(this._instance);
                this._instance = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Disposes the DDS wrapper and releases unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Release();
            GC.SuppressFinalize(this);
        }
    }
}
