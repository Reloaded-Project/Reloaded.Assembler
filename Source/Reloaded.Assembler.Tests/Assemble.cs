using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Reloaded.Assembler.Definitions;
using Reloaded.Memory.Buffers.Internal.Kernel32;
using Reloaded.Memory.Buffers.Internal.Utilities;
using Xunit;

namespace Reloaded.Assembler.Tests
{
    public class Assemble
    {
        /*
         * This test file only tests three simple things:
         * - GetVersion
         * - Assemble
         * - Assemble (Bad Mnemonics)
         * - Assemble (Out of Text Memory) || Checked because this library takes responsibility for this.
         * - Dispose
         *
         * An assumption is made that FASM is stable ;)
         */

        [Fact]
        public void GetVersion()
        {
            // TODO: Remember to change version every time you update FASM.
            var asm = new Assembler();
            Version version = asm.GetVersion();
            Assert.Equal(1, version.Major);
            Assert.Equal(73, version.Minor);
        }

        [Fact]
        public void AssembleMnemonics()
        {
            var asm = new Assembler();
            string[] mnemonics = new[]
            {
                "use32",
                "jmp dword [0x123456]"
            };

            byte[] actual = asm.Assemble(mnemonics);
            byte[] expected = { 0xFF, 0x25, 0x56, 0x34, 0x12, 0x00 };
            Assert.Equal(expected, actual);
            asm.Dispose();
        }

        [Fact]
        public void ConcurrentAssembleMnemonics()
        {
            int numThreads = 100;
            var threads = new Thread[numThreads];

            for (int x = 0; x < numThreads; x++)
            {
                threads[x] = new Thread(() =>
                {
                    for (int y = 0; y < 4; y++)
                    {
                        AssembleMnemonics();
                    }
                });
                threads[x].Start();
            }

            foreach (var thread in threads)
                thread.Join();
        }

        [Fact]
        public void AssemblerSpam()
        {
            // The purpose of this test is to check for bad disposal/memory
            // allocation/deallocation synchronization problems.
            for (int x = 0; x < 5000; x++)
            {
                AssembleMnemonics();
            }
        }

        [Fact]
        public void AssembleException()
        {
            var asm = new Assembler();
            string[] mnemonics = new[]
            {
                "use32",
                "jmp [0x123456]" // Missing operand size.
            };

            try
            {
                asm.Assemble(mnemonics);
            }
            catch (FasmException ex)
            {
                // Confirm exception details.
                Assert.Equal(FasmResult.Error, ex.Result);
                Assert.Equal(mnemonics, ex.Mnemonics);
                Assert.Equal(2, ex.Line);
                Assert.Equal(FasmErrors.OperandSizeNotSpecified, ex.ErrorCode);
            }
        }

        [Fact]
        public void AssembleFile()
        {
            var asm = new Assembler();
            var exe64 = asm.AssembleFile("PE64DEMO.ASM");
            var exe32 = asm.AssembleFile("PEDEMO.ASM");
        }

        [Fact]
        public void AssembleOutOfTextMemory()
        {
            // Text and return buffer size will be rounded up.
            var asm = new Assembler(0x0, 0x0);

            // Create a huge thing to assemble.
            List<string> mnemonics = new List<string>(10000);

            mnemonics.Add("use32");
            for (int x = 0; x < 9999; x++)
                mnemonics.Add("jmp dword [0x123456]");

            Assert.Throws<FasmWrapperException>(() => { asm.Assemble(mnemonics.ToArray()); });
        }
    }
}
