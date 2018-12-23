
<div align="center">
	<h1>Project Reloaded: Assembler Library</h1>
	<img src="https://i.imgur.com/BjPn7rU.png" width="150" align="center" />
	<br/> <br/>
	<strong><i>x86 Assembly is like IKEA Furniture</i></strong>
	<br/> <br/>
	<!-- Coverage -->
	<a href="https://codecov.io/gh/Reloaded-Project/Reloaded.Assembler">
		<img src="https://codecov.io/gh/Reloaded-Project/Reloaded.Assembler/branch/master/graph/badge.svg" alt="Coverage" />
	</a>
	<!-- Build Status -->
	<a href="https://ci.appveyor.com/project/sewer56lol/reloaded-assembler">
		<img src="https://ci.appveyor.com/api/projects/status/yats2utii7jss9ap?svg=true" alt="Build Status" />
	</a>
</div>

# Introduction
Reloaded.Assembler is a minimal .NET wrapper around the simple, easy to use Flat Assembler written by Tomasz Grysztar.

It combines the standard tried and tested `FASM` DLL and a modified version of the recent experimental `FASMX64` DLL to provide JIT, on the fly assembly of user supplied mnemonics inside x86 and x64 programs.

## Getting Started

To get started, install the package from NuGet and simply create a new instance of the `Assembler` class from the `Reloaded.Assembler` namespace:

```csharp
var assembler = new Assembler();
``` 
And, uh... well... that's it.

From there you can call `GetVersion()` to retrieve the version of FASM assembler that the wrapper wraps around and assemble mnemonics with `Assemble()`.

### Example

```csharp
var asm = new Assembler();
string[] mnemonics = new[]
{
    "use32",
    "jmp dword [0x123456]"
};
byte[] actual = asm.Assemble(mnemonics);
// Result: 0xFF, 0x25, 0x56, 0x34, 0x12, 0x00
```


Just don't forget to dispose the assembler when you're done 😉,

```csharp
assembler.Dispose();
```

### Small Tip

If the assembly operations fail, the wrapper library will throw an exception with a summary of the error. 

You can obtain a slightly more detailed versions of the exceptions by catching them
explicitly and checking their properties.

```csharp
try { asm.Assemble(mnemonics); }
catch (FasmException ex)
{
    // Assembler result (e.g. Error, OutOfMemory) : ex.Result
    // Original text given to the assembler: ex.Mnemonics
    // Line of text error occured in: ex.Line
    // The error itself: ex.ErrorCode
}
```
## Reloaded Assembler Compared to FASM.NET

Reloaded.Assembler is not the only standalone library that exposes FASM to the world of .NET. For a while now, there has been another wrapper worth mentioning willing to fulfill the same purpose.

Below is a quick list of differences you should expect when using Reloaded.Assembler as opposed to `FASM.NET`; and some of the reasons why I decided to write this library 

### Advantages
- Does not require the Visual C++ Runtime to operate.
- Can be used in both x64 and x86 applications vs only x86.
- No memory allocation on each assembly request, reuses the same buffers resulting in better performance.

### Other Differences
- Reloaded.Assembler is written in pure C#, FASM.NET is written in C++/CLI.
- Reloaded.Assembler has a slightly more minimal interface.

## Other Links

Flat Assembler forums post : https://board.flatassembler.net/templates/phpVB2/images/icon_minipost.gif

This post briefly describes the changes I made to the experimental `FASMX64` DLL that made it consumable from C# (and likely other high level languages). 

## Misc. Notes

The folder `Source/FASMX64` contains the source code of my modified version of `FASMX64` DLL. This is a "fixed" version of the modified version of FASM made by the flat assembler forum member *bazizmix*. (See link in "Other Links" for more details).

## Contributions
As with the standard for all of the `Reloaded-Project`, repositories; contributions are very welcome and encouraged.

Feel free to implement new features, make bug fixes or suggestions so long as they are accompanied by an issue with a clear description of the pull request 😉.
