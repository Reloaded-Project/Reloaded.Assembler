/*
    [Reloaded] Mod Loader Common Library (libReloaded)
    The main library acting as common, shared code between the Reloaded Mod 
    Loader Launcher, Mods as well as plugins.
    Copyright (C) 2018  Sewer. Sz (Sewer56)

    [Reloaded] is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    [Reloaded] is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Reloaded.Assembler.Definitions;
using Reloaded.Memory.Buffers;
using Reloaded.Memory.Sources;
using static Reloaded.Assembler.Kernel32.Kernel32;
using static Reloaded.Memory.Kernel32.Kernel32;

namespace Reloaded.Assembler
{
    /// <summary>
    /// Assembler class allows you to assemble X86 and X64 mnemonics using FASM.
    /// </summary>
    public unsafe class Assembler : IDisposable
    {
        // Address and size of allocation of where the text/mnemonics to be assembled will be stored.
        private IntPtr  _textAddress;
        private int     _textSize;
        private IntPtr  _resultAddress;
        private int     _resultSize;

        private static readonly MemoryBufferHelper _bufferHelper;
        private static readonly Memory.Sources.Memory _processMemory;
        
        private readonly FasmDelegates.fasm_Assemble   _assembleFunction;
        private readonly FasmDelegates.fasm_GetVersion _getVersionFunction;

        /* Create the common static members. */
        static Assembler()
        {
            _processMemory = new Memory.Sources.Memory();
            _bufferHelper = new MemoryBufferHelper(Process.GetCurrentProcess());
        }

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
            AllocateText(textSize, 3);
            AllocateResult(resultSize, 3);

            IntPtr fasmDllHandle;

            // Set functions
            if (IntPtr.Size == 4)
                fasmDllHandle = LoadLibraryW("FASM.dll");
            else if (IntPtr.Size == 8)
                fasmDllHandle = LoadLibraryW("FASMX64.dll");
            else
            {
                // Does not actually check OS or architecture but it should be good enough for our purposes.
                // Users should know that this is a Windows lib for x86/x64
                throw new FasmWrapperException("Only 32bit and 64bit desktop architectures are supported (X86 and X86_64).");
            }

            // Obtain delegates to FASM functions.
            IntPtr assembleAddress = GetProcAddress(fasmDllHandle, "fasm_Assemble");
            IntPtr getVersionAddress = GetProcAddress(fasmDllHandle, "fasm_GetVersion");
            _assembleFunction = Marshal.GetDelegateForFunctionPointer<FasmDelegates.fasm_Assemble>(assembleAddress);
            _getVersionFunction = Marshal.GetDelegateForFunctionPointer<FasmDelegates.fasm_GetVersion>(getVersionAddress);
        }

        /// <summary>
        /// Destroys this instance of the class.
        /// </summary>
        ~Assembler()
        {
            Dispose();
        }

        /// <summary>
        /// Releases the allocated memory for the assembler.
        /// </summary>
        public void Dispose()
        {
            _bufferHelper.Free(_textAddress);
            _bufferHelper.Free(_resultAddress);
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

            if (mnemonicBytes.Length > _textSize)
                throw new FasmWrapperException($"Your supplied array of mnemonics to be assembled is too large ({mnemonicBytes.Length} > {_textSize} bytes)." +
                                                "Consider simplifying your code or creating a new Assembler with greater textSize.");

            _processMemory.WriteRaw(_textAddress, mnemonicBytes);

            // Assemble and check result.
            FasmResult result = _assembleFunction(_textAddress, _resultAddress, (IntPtr)_resultSize, passLimit, IntPtr.Zero);

            //    As stated in FASMDLL.TXT, at the beginning of the block, the FASM_STATE structure will reside.
            //    It is defined in FASM.ASH. We read it here.
            
            _processMemory.Read(_resultAddress, out FasmState state);

            if (result == FasmResult.Ok)
            {
                byte[] assembledBytes = new byte[state.OutputLength];
                Marshal.Copy((IntPtr)state.OutputData, assembledBytes, 0, assembledBytes.Length);
                return assembledBytes;
            }
            else
            {
                // TODO: Make this exception more detailed in time with FASMX64's development.
                /* For now, I still do not know if FASMX64 will ever plan to change the pointer size,
                   so for now, I will opt to not get the line details and/or other more "complex" info. */

                string[] originalMnemonics = mnemonics.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                var lineHeader = state.GetLineHeader();

                throw new FasmException(state.ErrorCode, state.Condition, lineHeader.LineNumber, originalMnemonics);
            }
        }

        /// <summary>
        /// Attempts to allocate the memory to store the text to be supplied to FASM assembler.
        /// </summary>
        private void AllocateText(int textSize, int retryCount)
        {
            var allocationProperties = _bufferHelper.Allocate(textSize, 1, Int32.MaxValue, retryCount);
            _textAddress = allocationProperties.MemoryAddress;
            _textSize = allocationProperties.Size;

            if (_textAddress == IntPtr.Zero)
                throw new FasmWrapperException("Failed to allocate text memory for Assembler.");
        }

        /// <summary>
        /// Attempts to allocate the memory to store the result received from FASM assembler.
        /// </summary>
        private void AllocateResult(int resultSize, int retryCount)
        {
            var allocationProperties = _bufferHelper.Allocate(resultSize, 1, Int32.MaxValue, retryCount);
            _resultAddress = allocationProperties.MemoryAddress;
            _resultSize = allocationProperties.Size;

            if (_resultAddress == IntPtr.Zero)
                throw new FasmWrapperException("Failed to allocate result memory for Assembler.");
        }

    }
}
