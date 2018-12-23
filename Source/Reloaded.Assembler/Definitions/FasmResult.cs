namespace Reloaded.Assembler.Definitions
{
    /// <summary/>
    public enum FasmResult : int
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        Ok = 0,
        Working = 1,
        Error = 2,
        InvalidParameter = -1,
        OutOfMemory = -2,
        StackOverflow = -3,
        SourceNotFound = -4,
        UnexpectedEndOfSource = -5,
        CannotGenerateCode = -6,
        FormatLimitationsExcedded = -7,
        WriteFailed = -8
    }
}
