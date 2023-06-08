using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Reloaded.Assembler.Definitions;
using Reloaded.Memory.Buffers;
using Reloaded.Memory.Buffers.Structs;
using Reloaded.Memory.Buffers.Structs.Params;
using static Reloaded.Assembler.Kernel32.Kernel32;

namespace Reloaded.Assembler;

/// <summary>
/// Assembler class allows you to assemble X86 and X64 mnemonics using FASM.
/// </summary>
public unsafe class Assembler : IDisposable
{
    // Address and size of allocation of where the text/mnemonics to be assembled will be stored.
    private object  _lock = new();
    private PrivateAllocation _textAllocation;
    private PrivateAllocation _resultAllocation;

    private readonly FasmDelegates.fasm_Assemble   _assembleFunction;
    private readonly FasmDelegates.fasm_GetVersion _getVersionFunction;

    /* Create the common static members. */

    /// <summary>
    /// Creates a new instance of the FASM assembler.
    /// </summary>
    /// <param name="textSize">
    ///     The minimum size of the buffer to be used for passing the
    ///     text to be assembled to FASM Assembler.
    /// </param>
    /// <param name="resultSize">
    ///     The minimum size of the buffer to be used for FASM to return the
    ///     text to be assembled.
    /// </param>
    public Assembler(int textSize = 0x10000, int resultSize = 0x8000)
    {
        // Attempt allocation of memory X times.
        AllocateText(textSize);
        AllocateResult(resultSize);

        // Get path of FASM dll
        string fasmDllPath = GetFasmDLLPath();
        var fasmDllHandle = LoadLibraryW(fasmDllPath);

        // Throw exception if dll not loaded.
        if (fasmDllHandle == IntPtr.Zero)
            throw new FasmWrapperException("Failed to load FASM dll. The FASM dll pointer from LoadLibraryW is null.");

        // Obtain delegates to FASM functions.
        IntPtr assembleAddress = GetProcAddress(fasmDllHandle, "fasm_Assemble");
        IntPtr getVersionAddress = GetProcAddress(fasmDllHandle, "fasm_GetVersion");
        _assembleFunction = Marshal.GetDelegateForFunctionPointer<FasmDelegates.fasm_Assemble>(assembleAddress);
        _getVersionFunction = Marshal.GetDelegateForFunctionPointer<FasmDelegates.fasm_GetVersion>(getVersionAddress);
    }

    /// <summary>
    /// Destroys this instance of the class.
    /// </summary>
    ~Assembler() => Dispose();

    /// <summary>
    /// Releases the allocated memory for the assembler.
    /// </summary>
    public void Dispose()
    {
        _textAllocation.Dispose();
        _resultAllocation.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Retrieves the version of the internally used FASM assembler DLL.
    /// </summary>
    /// <returns></returns>
    public Version GetVersion()
    {
        // Call the native function to get the version
        int nativeVersion = _getVersionFunction();
            
        // Create and return a managed version object
        return new Version(nativeVersion & 0xff, (nativeVersion >> 16) & 0xff);
    }

    /// <summary>
    /// Assembles a file at a given path.
    /// </summary>
    /// <param name="filePath">The path of the file to be assembled.</param>
    /// <returns>The assembled file.</returns>
    public byte[] AssembleFile(string filePath)
    {
        return Assemble(File.ReadAllLines(filePath));
    }

    /// <summary>
    /// Assembles the given mnemonics.
    /// </summary>
    /// <param name="mnemonics">The mnemonics to assemble, with each line being represented as a string in the array.</param>
    /// <param name="passLimit">The maximum number of passes to perform when assembling data.</param>
    public byte[] Assemble(IEnumerable<string> mnemonics, ushort passLimit = 100)
    {
        string mnemonicsString = String.Join(Environment.NewLine, mnemonics);
        return Assemble(mnemonicsString);
    }

    /// <summary>
    /// Assembles the given mnemonics.
    /// </summary>
    /// <param name="mnemonics">The mnemonics to assemble; delimited by new line \n for each new instruction.</param>
    /// <param name="passLimit">The maximum number of passes to perform when assembling data.</param>
    /// <exception cref="FasmWrapperException">Your text to be assembled is too large to fit in the preallocated region for program text.</exception>
    /// <exception cref="FasmException">An error thrown by the native FASM compiler.</exception>
    public byte[] Assemble(string mnemonics, ushort passLimit = 100)
    {
        // Convert Text & Append
        byte[] mnemonicBytes = Encoding.ASCII.GetBytes(mnemonics + "\0");

        if (mnemonicBytes.Length > _textAllocation.Size)
            throw new FasmWrapperException($"Your supplied array of mnemonics to be assembled is too large ({mnemonicBytes.Length} > {_textAllocation.Size} bytes)." +
                                           "Consider simplifying your code or creating a new Assembler with greater textSize.");

        lock (_lock)
        {
            Memory.Memory.Instance.WriteRaw(_textAllocation.BaseAddress, mnemonicBytes);

            // Assemble and check result.
            var result = _assembleFunction((IntPtr)(void*)_textAllocation.BaseAddress, (IntPtr)(void*)_resultAllocation.BaseAddress, (IntPtr)_resultAllocation.Size, passLimit, IntPtr.Zero);

            //    As stated in FASMDLL.TXT, at the beginning of the block, the FASM_STATE structure will reside.
            //    It is defined in FASM.ASH. We read it here.
            Memory.Memory.Instance.Read(_resultAllocation.BaseAddress, out FasmState state);

            if (result == FasmResult.Ok)
            {
                byte[] assembledBytes = new byte[state.OutputLength];
                Marshal.Copy((IntPtr)state.OutputData, assembledBytes, 0, assembledBytes.Length);
                return assembledBytes;
            }
                
            // TODO: Make this exception more detailed in time with FASMX64's development.
            /* For now, I still do not know if FASMX64 will ever plan to change the pointer size,
                   so for now, I will opt to not get the line details and/or other more "complex" info. */

            string[] originalMnemonics = mnemonics.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var lineHeader = state.GetLineHeader();

            throw new FasmException(state.ErrorCode, state.Condition, lineHeader.LineNumber, originalMnemonics);
        }
    }

    /// <summary>
    /// Retrieves the path of the FASM dll to load.
    /// </summary>
    private string GetFasmDLLPath()
    {
        const string FASM86DLL = "FASM.dll";
        const string FASM64DLL = "FASMX64.dll";

        // Check current directory.
        if (IntPtr.Size == 4 && File.Exists(FASM86DLL))
            return FASM86DLL;
        else if (IntPtr.Size == 8 && File.Exists(FASM64DLL))
            return FASM64DLL;

        // Check DLL Directory
        string assemblyDirectory = GetExecutingDLLDirectory();
        string asmDirectoryFasm86 = Path.Combine(assemblyDirectory, FASM86DLL);
        string asmDirectoryFasm64 = Path.Combine(assemblyDirectory, FASM64DLL);

        if (IntPtr.Size == 4 && File.Exists(asmDirectoryFasm86))
            return asmDirectoryFasm86;
        else if (IntPtr.Size == 8 && File.Exists(asmDirectoryFasm64))
            return asmDirectoryFasm64;

        throw new FasmWrapperException("Appropriate FASM DLL for X86/64 has not been found in either current or library directory.");
    }

    /// <summary>
    /// Gets the directory of the currently executing assembly.
    /// </summary>
    private string GetExecutingDLLDirectory()
    {
        string codeBase = Assembly.GetExecutingAssembly().CodeBase;
        UriBuilder uri = new UriBuilder(codeBase);
        string path = Uri.UnescapeDataString(uri.Path);
        return Path.GetDirectoryName(path);
    }

    /// <summary>
    /// Attempts to allocate the memory to store the text to be supplied to FASM assembler.
    /// </summary>
    private void AllocateText(int textSize)
    {
        _textAllocation = Buffers.AllocatePrivateMemory(new BufferAllocatorSettings()
        {
            Size = (uint)textSize,
            MinAddress = 1,
            MaxAddress = Int32.MaxValue
        });
    }

    /// <summary>
    /// Attempts to allocate the memory to store the result received from FASM assembler.
    /// </summary>
    private void AllocateResult(int resultSize)
    {
        _resultAllocation = Buffers.AllocatePrivateMemory(new BufferAllocatorSettings()
        {
            Size = (uint)resultSize,
            MinAddress = 1,
            MaxAddress = Int32.MaxValue
        });
    }
}