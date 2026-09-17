// ============================================================================
// StartupDiagnostics —— 启动诊断日志
//
// 【为什么需要】
// 本程序是 WinExe（无控制台），在 Win7 这类"官方不支持但实际可能跑"的系统上崩掉时
// 只能看到 WER 的 BEX / 0xC0000409 —— 那是 NativeAOT 对**未处理托管异常**的 fail-fast
// 表现，不是访问违规，光看错误码完全不知道是哪一步、哪个异常。
// 实测把异常偏移 0x219865 用 PDB 反查得到
//     ClassConstructorRunner.EnsureClassConstructorRun (ClassConstructorRunner.cs:99)
// 即"某个静态构造函数抛异常且没人接"。这类问题必须拿到异常本身才能定位。
//
// 【做什么】
// 1. 启动时先落一条 boot 记录（在任何可能触发全球化初始化的调用之前）；
// 2. 记录运行时与关键原生库的可用性（ICU / synch api-set / d3d12 / opengl32 ...）；
// 3. 记录每个启动阶段（stage），用于判断崩在哪一步；
// 4. 捕获未处理异常（try/catch + AppDomain.UnhandledException），
//    把异常类型、消息、完整堆栈（含 InnerException 链）写进日志。
//
// 日志文件：<exe 所在目录>\startup-diagnostic.log（每次启动覆盖）
// 所有写入都套了 try/catch 并且禁止依赖文化相关格式化 —— 万一崩的就是全球化初始化，
// 这份日志本身也不能跟着挂。
// ============================================================================

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace MufiyApp1
{
    /// <summary>
    /// 把启动过程与未处理异常写到 exe 同目录的日志文件，供无控制台的 WinExe 排查崩溃。
    /// </summary>
    internal static class StartupDiagnostics
    {
        /// <summary>日志文件名（位于程序目录）。</summary>
        private const string LogFileName = "startup-diagnostic.log";

        /// <summary>日志完整路径。</summary>
        private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, LogFileName);

        /// <summary>安装未处理异常钩子，并写下启动头与关键原生库可用性。</summary>
        internal static void Install()
        {
            // 第一件事：先落一条，保证后面无论哪一步炸掉，至少这个文件已经存在。
            Write("==== MufiyApp1 startup diagnostics ====\n");

            try
            {
                AppDomain.CurrentDomain.UnhandledException += static (_, e) =>
                    Write("!! AppDomain.UnhandledException: " + Describe(e.ExceptionObject as Exception) + "\n");
            }
            catch (Exception ex)
            {
                Write("!! 注册 UnhandledException 失败: " + Describe(ex) + "\n");
            }

            Write("os            = " + Safe(() => Environment.OSVersion.ToString()) + "\n");
            Write("is64bitOS     = " + Safe(() => Environment.Is64BitOperatingSystem.ToString()) + "\n");
            Write("is64bitProc   = " + Safe(() => Environment.Is64BitProcess.ToString()) + "\n");
            Write("runtimeVer    = " + Safe(() => Environment.Version.ToString()) + "\n");
            Write("baseDir       = " + Safe(() => AppContext.BaseDirectory) + "\n");
            Write("procArch      = " + Safe(() => RuntimeInformation.ProcessArchitecture.ToString()) + "\n");
            Write("invariantGlob = " + Safe(() => GetSwitch("System.Globalization.Invariant")) + "\n");
            Write("useNls        = " + Safe(() => GetSwitch("System.Globalization.UseNls")) + "\n");

            // 关键原生库的可用性：Win7 上这几项决定了运行时能不能正常工作
            Write("probe icu.dll                          = " + Safe(() => TryLoad("icu.dll")) + "\n");
            Write("probe api-ms-win-core-synch-l1-2-0.dll = " + Safe(() => TryLoad("api-ms-win-core-synch-l1-2-0.dll")) + "\n");
            Write("probe d3d12.dll                        = " + Safe(() => TryLoad("d3d12.dll")) + "\n");
            Write("probe D3DCOMPILER_47.dll               = " + Safe(() => TryLoad("D3DCOMPILER_47.dll")) + "\n");
            Write("probe opengl32.dll                     = " + Safe(() => TryLoad("opengl32.dll")) + "\n");
            Write("probe SDL3.dll                         = " + Safe(() => TryLoad("SDL3.dll")) + "\n");
            Write("probe libSkiaSharp.dll                 = " + Safe(() => TryLoad("libSkiaSharp.dll")) + "\n");
            Write("probe mufiy.user32.x86.dll             = " + Safe(() => TryLoad("mufiy.user32.x86.dll")) + "\n");

            Stage("diagnostics installed");
        }

        /// <summary>记录一个启动阶段，用来判断崩溃发生在哪一步。</summary>
        internal static void Stage(string name)
        {
            Write("stage: " + name + "\n");
        }

        /// <summary>记录一个异常（含完整堆栈与 InnerException 链）。</summary>
        internal static void Error(string where, Exception? ex)
        {
            Write("!! FAILED at [" + where + "]\n" + Describe(ex) + "\n");
        }

        /// <summary>把异常展开成可读文本（异常类型 + 消息 + 完整堆栈 + 逐层 InnerException）。</summary>
        private static string Describe(Exception? ex)
        {
            if (ex is null)
            {
                return "(no exception object)";
            }

            var sb = new StringBuilder();
            for (Exception? cur = ex; cur is not null; cur = cur.InnerException)
            {
                sb.Append("   type    : ").Append(Safe(() => cur.GetType().FullName ?? cur.GetType().Name)).Append('\n');
                sb.Append("   message : ").Append(Safe(() => cur.Message)).Append('\n');
                sb.Append("   hresult : ").Append(Safe(() => "0x" + cur.HResult.ToString("X8"))).Append('\n');
                sb.Append("   stack   :\n").Append(Safe(() => cur.StackTrace ?? "(null)"))
                  .Append("\n   ---- inner ----\n");
            }
            return sb.ToString();
        }

        /// <summary>读 runtimeconfig 里的特性开关（如 System.Globalization.Invariant）。</summary>
        private static string GetSwitch(string name)
        {
            return AppContext.TryGetSwitch(name, out bool value)
                ? name + "=" + value
                : name + "=<未配置>";
        }

        /// <summary>探测某个原生库能否加载，用于区分"缺库"和"其它原因"。</summary>
        private static string TryLoad(string libraryName)
        {
            return NativeLibrary.TryLoad(libraryName, out _) ? "可加载" : "不可用";
        }

        /// <summary>执行可能抛异常的信息采集，失败时返回错误信息而不是让日志本身崩掉。</summary>
        private static string Safe(Func<string> get)
        {
            try
            {
                return get();
            }
            catch (Exception ex)
            {
                return "<threw " + ex.GetType().Name + ">";
            }
        }

        /// <summary>追加写日志；任何失败都静默吞掉（诊断功能不允许影响主流程）。</summary>
        private static void Write(string text)
        {
            try
            {
                File.AppendAllText(LogPath, text, Encoding.UTF8);
            }
            catch
            {
                // ignored: 诊断日志写不进去也不能让程序挂
            }
        }
    }
}
