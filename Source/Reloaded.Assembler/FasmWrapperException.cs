using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;

namespace Reloaded.Assembler
{
    [ExcludeFromCodeCoverage]
    public class FasmWrapperException : Exception
    {
        public FasmWrapperException() {}
        public FasmWrapperException(string message) : base(message) { }
        public FasmWrapperException(string message, Exception innerException) : base(message, innerException) { }
        protected FasmWrapperException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
