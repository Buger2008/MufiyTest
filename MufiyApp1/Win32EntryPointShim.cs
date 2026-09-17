// ============================================================================
// Win32EntryPointShim —— 32 位 user32 入口点补丁的托管侧
//
// 【背景】
// Mufiy.Platform.Desktop.Provider 的窗口层 P/Invoke 了 SetWindowLongPtr /
// GetWindowLongPtr，这两个导出**只存在于 64 位 user32.dll**，32 位进程一调用就抛
//     System.EntryPointNotFoundException: Unable to find an entry point named
//     'SetWindowLongPtr' in DLL 'user32.dll'.   （栈顶是 Mufiy.Window..ctor()）
// 这与 AOT 无关（非 AOT 的 win-x86 一样崩），是 Mufiy 包自身的缺陷。
//
// 【做法】
// 把该程序集对 "user32.dll" 的解析重定向到同目录下的转发 DLL
// mufiy.user32.x86.dll（由 Native/user32.x86.def 生成，纯转发表、无代码）。
// 那个 DLL 补齐了 Mufiy 用到的全部 15 个 user32 入口点，其中：
//     SetWindowLongPtr*  → user32.SetWindowLongW / A
//     GetWindowLongPtr*  → user32.GetWindowLongW / A
// 32 位下 LONG 就是指针宽度，语义等价；其余入口点 1:1 转发，行为零差异。
//
// 【只在 32 位生效】
// x64 / arm64 上直接返回，不做任何注册，解析路径与打补丁前完全一致。
//
// 【什么时候可以删】
// Mufiy 上游把窗口创建路径改成按 IntPtr.Size 选择非 Ptr 版本之后，
// 本文件 + Native/user32.x86.def + mufiy.user32.x86.dll + csproj 里的拷贝规则都可一并删除。
// ============================================================================

using System.Reflection;
using System.Runtime.InteropServices;

namespace MufiyApp1
{
    /// <summary>
    /// 把 Mufiy 平台层对 user32.dll 的 P/Invoke 解析重定向到 32 位转发 DLL，
    /// 用来补上只在 64 位 user32.dll 里存在的 SetWindowLongPtr / GetWindowLongPtr。
    /// </summary>
    internal static class Win32EntryPointShim
    {
        /// <summary>转发 DLL 的文件名（随程序集一起输出到程序目录）。</summary>
        private const string ShimFileName = "mufiy.user32.x86.dll";

        /// <summary>转发 DLL 的模块句柄，进程内只加载一次。</summary>
        private static IntPtr _shimHandle;

        /// <summary>
        /// 注册 DllImport 解析器。必须早于任何 Mufiy 窗口构造（即早于 MufiyApp.Run()）。
        /// 非 Windows / 非 32 位进程直接返回，不改变原有解析行为。
        /// </summary>
        internal static void Register()
        {
            if (!OperatingSystem.IsWindows() || IntPtr.Size != 4)
            {
                return;
            }

            // 重定向只作用于 Mufiy 平台层这一个程序集，不影响其它程序集的 P/Invoke。
            Assembly provider = typeof(Mufiy.Platform.Desktop.Provider.ServiceExtension).Assembly;
            NativeLibrary.SetDllImportResolver(provider, Resolve);
        }

        /// <summary>
        /// 解析回调：只接管 user32.dll，其它库返回 <see cref="IntPtr.Zero"/> 交回默认逻辑。
        /// </summary>
        private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!string.Equals(libraryName, "user32.dll", StringComparison.OrdinalIgnoreCase))
            {
                return IntPtr.Zero;
            }

            if (_shimHandle == IntPtr.Zero)
            {
                string path = Path.Combine(AppContext.BaseDirectory, ShimFileName);
                if (!File.Exists(path))
                {
                    // 转发 DLL 缺失时退回默认解析：user32 里有的入口点照常能用，
                    // 只有 Ptr 那两个仍会抛 EntryPointNotFoundException。
                    return IntPtr.Zero;
                }

                _shimHandle = NativeLibrary.Load(path);
            }

            return _shimHandle;
        }
    }
}
