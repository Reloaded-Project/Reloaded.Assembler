using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Reloaded.Assembler.Definitions;

namespace Reloaded.Assembler
{
    public class FasmDelegates
    {
        /// <summary>
        /// Returns the version of the FASM compiler inside FASM.DLL
        /// </summary>
        /// <returns>The return value is a double word containing the major version in lower 16 bits, and minor version in the higher 16 bits.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate int fasm_GetVersion();

        /// <summary>
        /// The native function to assemble mnemonics of FASM compiler embedded in Fasm.obj.
        /// </summary>
        /// <param name="lpSource">Contains a pointer to zero-ended source text.</param>
        /// <param name="lpMemory">The pointer to a buffer used to assemble mnemonics.</param>
        /// <param name="nSize">The memory size allocated for the buffer.</param>
        /// <param name="nPassesLimit">A value in range from 1 to 65536, defining
        /// the maximum number of passes the assembler can perform in order to generate the code.</param>
        /// <param name="hDisplayPipe">The hDisplayPipe should contain handle of the pipe, to which the output of DISPLAY directives will be written.</param>
        /// <returns>The return value is a <see cref="FasmResult"> enum instance.</returns>
        [SuppressUnmanagedCodeSecurity]
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate FasmResult fasm_Assemble(IntPtr lpSource, IntPtr lpMemory, IntPtr nSize, int nPassesLimit, IntPtr hDisplayPipe);
    }
}
