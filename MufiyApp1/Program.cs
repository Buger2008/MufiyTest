using Mufiy;
using Mufiy.Platform.Desktop.Provider;

namespace MufiyApp1
{
    /// <summary>
    /// 应用程序入口
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// 应用程序主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 启动诊断日志：WinExe 没有控制台，崩溃（尤其 NativeAOT 把未处理异常变成
            // 0xC0000409 fail-fast）时只能靠 exe 同目录的 startup-diagnostic.log 定位。
            // 不需要时把 Install / Stage / catch 这几处删掉即可，不影响主流程。
            StartupDiagnostics.Install();

            // 32 位进程的 user32 入口点补丁：早于任何窗口构造执行，x64 上是空操作。
            // 详见 Win32EntryPointShim.cs。
            Win32EntryPointShim.Register();

            // 链式构建拆成逐句，配合 Stage 标记即可判断崩在哪一步
            // （静态构造函数抛异常时，异常会一直冒到这里，日志里能看到完整的类型与堆栈）。
            try
            {
                StartupDiagnostics.Stage("MufiyApp.Create");
                MufiyAppBuilder builder = MufiyApp.Create();

                StartupDiagnostics.Stage("AppConfiguration");
                builder = builder.AppConfiguration(option =>
                {
                    // 图形后端：Software（CPU 光栅）。
                    // 为什么不用 OpenGL/Auto：实测（Win10 上枚举进程模块）即使配成 OpenGL，
                    // Mufiy 的 Skia 后端仍然去创建 **D3D12** 的 GRContext
                    // （加载 d3d12.dll + D3D12Core.dll + D3DCOMPILER_47.dll + dxgi.dll）。
                    // 而 d3d12 只存在于 Windows 10+，Win7 上创建失败返回 null，
                    // 紧接着 SkiaBackend.GetGRContext() 解引用就抛 NullReferenceException
                    // （见 startup-diagnostic.log）。Win7 上也没有 icu.dll，本来就只能走软渲染。
                    option.GpuBackend = GpuBackendType.Software;
                });

                StartupDiagnostics.Stage("UseDesktopPlatform");
                builder = builder.UseDesktopPlatform();

                StartupDiagnostics.Stage("RegisterTheme<Win10Theme>");
                builder = builder.RegisterTheme<Win10Theme>();

                StartupDiagnostics.Stage("UseMainWindow<Form1>");
                builder = builder.UseMainWindow<Form1>();

                StartupDiagnostics.Stage("Build");
                builder.Build();

                StartupDiagnostics.Stage("MufiyApp.Run");
                /* Here we go! */
                MufiyApp.Run();

                StartupDiagnostics.Stage("Run returned");
            }
            catch (Exception ex)
            {
                StartupDiagnostics.Error("Main", ex);
                throw;
            }
        }
    }
}
