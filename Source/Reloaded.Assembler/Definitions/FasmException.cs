using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Reloaded.Assembler.Definitions
{
    public class FasmException : Exception
    {
        /// <summary>
        /// The specific error code that triggered the exception.
        /// </summary>
        public FasmErrors ErrorCode { get; private set; }

        /// <summary>
        /// The condition of the Assembler.
        /// </summary>
        public FasmResult Result { get; private set; }

        /// <summary>
        /// The line that caused the exception to be thrown.
        /// </summary>
        public int Line { get; private set; }

        /// <summary>
        /// The original supplied text to be assembled that thrown the exception.
        /// </summary>
        public string[] Mnemonics { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FasmException" /> class.
        /// </summary>
        public FasmException(FasmErrors errorCode, FasmResult condition, int lineNumber, string[] mnemonics) : base($"Failed to assemble FASM Mnemonics: Error name: {errorCode.ToString()}, Line Number: {lineNumber}, Result: {condition.ToString()}")
        {
            Result = condition;
            ErrorCode = errorCode;
            Line = lineNumber;
            Mnemonics = mnemonics;
        }
    }
}
