# MufiyApp1 项目长期笔记

## 这是什么
- `MufiyApp1` 是 **Mufiy** 框架（跨平台 C# UI 框架，包源 `https://nuget.mikoui.com/v3/index.json`，
  nuget.org 上没有）的项目模板本体，源码在 用户 VS 里通过 VSIX 模板新建工程时展开。
- `MufiyApp1/MufiyApp1.csproj` 同时是「模板工程」和「可编译工程」：
  改 csproj 的 item 列表时**必须同步改 `MufiyProjectTemplate.vstemplate` 的 ProjectItem**，
  只改一处会让文件被静默丢弃或在项目树里出现缺失项（csproj 里有长注释说明）。
- 单 TargetFramework 是 `net11.0`（**不带 `-windows`**）；`-windows` 只在 Debug 下多出一个，
  专供 VS 设计器加载。Release 只有 `net11.0`。
- `(Configuration, TargetFramework)` 决定机制：Release 只保留所选框架，Debug 额外加 `-windows`。

## 与本机环境相关的约定
- Mufiy 与 WinForms/System.Drawing **存在大量同名类型**，靠 `Global.cs` 里的 `global using`
  别名统一指向 `Mufiy.*`（设计器生成的裸类型名也才能解析到 Mufiy）。**新增同名控件要同步加别名。**
- 字符串资源走 `Mufiy.ResxMapGenerator` 源生成器生成 AOT 友好代码（避免运行时反射 ResourceManager），
  生成物落在 `obj/<cfg>/<tfm>/<rid>/MufiyResxMapGenerator/`。

## NativeAOT 发布（2026-09-17 建立）
- 命令：`dotnet publish MufiyApp1.csproj -c Release -f net11.0 -r win-x64 -p:PublishAot=true`
  （多目标工程必须显式 `-f net11.0`，否则 NETSDK1129）。`win-x86` 同样可用。
- 配置位置：csproj 末尾两个条件 ItemGroup（AOT root 描述符 + win-x86 的 shim 拷贝）、
  `AotRoots.xml`（`TrimmerRootDescriptor`）、`Native/user32.x86.def` +
  `Native/mufiy.user32.x86.dll`、`Win32EntryPointShim.cs`。普通 build / 调试不受影响。
- **win-x86 用的是补丁方案**：Mufiy 窗口层 P/Invoke 了只存在于 64 位 user32.dll 的
  `SetWindowLongPtr`，32 位进程必崩（与 AOT 无关，beta9 同样崩）。
  绕法 = 转发 DLL（Ptr 系列映射到非 Ptr）+ `NativeLibrary.SetDllImportResolver`
  （NativeAOT 支持该 API，已实测）。**上游修好后这一整块可删。**
  源文件是 `Native/user32.x86.def`（事实来源）+ `Win32EntryPointShim.cs`（托管侧）；
  可独立重建/分享的打包副本：`_diag/mufiy-user32-x86-shim.zip`
  （含 def、预编译 DLL、一键 `build.cmd`、托管侧 .cs、说明）。
- **Windows 7 兼容的完整条件（已实测通过）**：`net10.0`（不是 net11.0，后者 AOT 静态导入
  Win8 的 synch api-set）+ `InvariantGlobalization=true`（Win7 无 icu.dll）
  + **`GpuBackend = GpuBackendType.Software`**（Mufiy 的文档默认值；模板原先覆盖成 OpenGL，
  在 Windows 上会落到 Skia 的 D3D12 路径，Win7 没有 d3d12.dll → GRContext 为 null → NRE）
  + 上面的 32 位 shim（+ 目标机需有 UCRT）。Win7 实测运行流畅不卡。
- **`GpuBackend = Software` 是写在 `Program.cs` 里的全局设置**，所以 Linux/macOS 构建也走
  CPU 光栅。若 Linux 上想要 GPU（Mufiy 文档里 OpenGL 支持 Linux），需要按 OS 分支再设，
  例如 `OperatingSystem.IsWindows() ? Software : OpenGL`。
- **Linux 构建**：`dotnet publish MufiyApp1/MufiyApp1.csproj -c Release -f net10.0 -r linux-x64 -p:PublishAot=true`
  （多目标必须 `-f`；**不能**在 Linux 上交叉产出 Windows exe，NativeAOT 不支持跨 OS）。
  先决条件 Ubuntu 系 `clang zlib1g-dev`；产物注意 glibc 下限（在 20.04 上构建只能跑 20.04+）。
  win-x86 的 shim 是 RID 条件 + `OperatingSystem.IsWindows()` 判断，Linux 上自动不参与。
- **包版本必须四个统一**：`Mufiy` 曾钉 Beta8、其余写 `*-Beta*` 浮动；包源发布 Beta9 后
  清缓存还原会 NU1605 包降级、编不过。现已全部钉 `0.0.1-Beta8`。升版本四个一起升。
- **踩坑备忘**：这类反射型 + 混淆发布包的框架，AOT 的 root 必须**按类型精确**（rd.xml），
  不能用 `@(TrimmerRootAssembly)` root 整个程序集（会触发 ILC 的
  `BadImageFormatException: The format of a DLL or executable being loaded is invalid`）。
  详细排查过程见 `.workbuddy/memory/2026-09-17.md`，通用方法论已沉淀进
  `~/.workbuddy/skills/librewinforms-aot`「11. 反射型 UI 框架（Mufiy / 混淆包 / 架构限制）三连坑」。
