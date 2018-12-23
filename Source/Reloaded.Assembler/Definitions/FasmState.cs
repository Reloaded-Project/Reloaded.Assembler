using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Reloaded.Assembler.Definitions
{
    /// <summary>
    /// Defines the state of the FASM assembler after an assembly operation.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public unsafe struct FasmState
    {
        /// <summary/>
        [FieldOffset(0)]
        public FasmResult Condition;

        /// <summary/>
        [FieldOffset(4)]
        public int OutputLength;

        /// <summary/>
        [FieldOffset(4)]
        public FasmErrors ErrorCode;

        /// <summary>
        /// 32bit pointer to the output bytes.
        /// </summary>
        [FieldOffset(8)]
        public int OutputData;

        /// <summary>
        /// 32bit pointer to the <see cref="FasmLineHeader"/> struct.
        /// </summary>
        [FieldOffset(8)]
        public int ErrorLine;

        /// <summary>
        /// Retrieves the <see cref="FasmLineHeader"/> struct from memory.
        /// </summary>
        public FasmLineHeader GetLineHeader()
        {
            Memory.Sources.Memory.CurrentProcess.Read((IntPtr)ErrorLine, out FasmLineHeader value);
            return value;
        }
    }
}
