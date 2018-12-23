using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace Reloaded.Assembler.Kernel32
{
    /// <summary/>
    public class Kernel32
    {
        /// <summary>
        /// <para>
        /// Loads the specified module into the address space of the calling process. The specified module may cause other modules to be loaded.
        /// </para>
        /// <para>For additional load options, use the <c>LoadLibraryEx</c> function.</para>
        /// </summary>
        /// <param name="lpFileName">
        /// <para>
        /// The name of the module. This can be either a library module (a .dll file) or an executable module (an .exe file). The name
        /// specified is the file name of the module and is not related to the name stored in the library module itself, as specified by the
        /// <c>LIBRARY</c> keyword in the module-definition (.def) file.
        /// </para>
        /// <para>If the string specifies a full path, the function searches only that path for the module.</para>
        /// <para>
        /// If the string specifies a relative path or a module name without a path, the function uses a standard search strategy to find the
        /// module; for more information, see the Remarks.
        /// </para>
        /// <para>
        /// If the function cannot find the module, the function fails. When specifying a path, be sure to use backslashes (\), not forward
        /// slashes (/). For more information about paths, see Naming a File or Directory.
        /// </para>
        /// <para>
        /// If the string specifies a module name without a path and the file name extension is omitted, the function appends the default
        /// library extension .dll to the module name. To prevent the function from appending .dll to the module name, include a trailing
        /// point character (.) in the module name string.
        /// </para>
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is a handle to the module.</para>
        /// <para>If the function fails, the return value is NULL. To get extended error information, call <c>GetLastError</c>.</para>
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryW(string lpFileName);

        /// <summary>Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).</summary>
        /// <param name="hModule">
        /// <para>
        /// A handle to the DLL module that contains the function or variable. The <c>LoadLibrary</c>, <c>LoadLibraryEx</c>,
        /// <c>LoadPackagedLibrary</c>, or <c>GetModuleHandle</c> function returns this handle.
        /// </para>
        /// <para>
        /// The <c>GetProcAddress</c> function does not retrieve addresses from modules that were loaded using the
        /// <c>LOAD_LIBRARY_AS_DATAFILE</c> flag. For more information, see <c>LoadLibraryEx</c>.
        /// </para>
        /// </param>
        /// <param name="lpProcName">
        /// The function or variable name, or the function's ordinal value. If this parameter is an ordinal value, it must be in the
        /// low-order word; the high-order word must be zero.
        /// </param>
        /// <returns>
        /// <para>If the function succeeds, the return value is the address of the exported function or variable.</para>
        /// <para>If the function fails, the return value is NULL. To get extended error information, call <c>GetLastError</c>.</para>
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    }
}
