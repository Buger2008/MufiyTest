# _diag —— Win7 排查用文件

这个目录只服务于「让 win-x86 AOT 产物在 Windows 7 上跑起来」这件事，可以随时整个删掉，
不影响工程构建。

## 文件

| 文件 | 用途 |
|---|---|
| `AotProbe-net10-win-x86.exe` | 最小运行时探针（net10.0 / x86 / AOT / 无任何第三方依赖） |
| `resolve_rva.py` | 把 WER 的「异常偏移」用 PDB 反查成函数名 + 源码行 |
| `check_modules.py` | 跑起来后枚举进程已加载模块，用来判断实际走了哪条渲染路径 |
| `check_render.py` | 截取窗口像素并统计颜色分布，区分「出了个空窗口」和「真的渲染了」 |

## 怎么用

### 1. 先跑探针，把「运行时」和「我们的栈」分开

把 `AotProbe-net10-win-x86.exe` 单独拷到 Win7 上双击：

- **弹出 "AotProbe OK"** → net10.0 的 AOT 运行时在 Win7 上没问题，问题在 Mufiy 或原生依赖那一层；
- **没弹框 / 报错** → 运行时层就不通，先解决这个，其它都白搭。

探针会先在同目录写 `probe-result.txt`，再弹框，所以两条通道都能拿到结果。

### 2. 再跑主程序，读启动诊断日志

跑 `bin\Release\net10.0\win-x86\publish\MufiyApp1.exe`，然后看**同目录**的
`startup-diagnostic.log`。它会记录：

- 系统版本 / 进程位数 / 运行时版本 / 进程架构
- `System.Globalization.Invariant` 等运行时配置的**实际取值**
- ICU、`api-ms-win-core-synch-l1-2-0.dll`、d3d12、SDL3、libSkiaSharp、shim 等能否加载
- 每个启动阶段（`stage: xxx`）—— **最后一条 stage 就是崩在哪一步**
- 未处理异常的**类型 / 消息 / 完整堆栈 / InnerException 链**

`0xC0000409`（WER 报 BEX）不代表栈溢出：NativeAOT 会把**未处理的托管异常**变成 fail-fast，
所以日志里的异常信息才是真相。

### 3. 如果崩溃信息里只有偏移，用脚本反查

```bash
python resolve_rva.py <对应的 exe> 0x219865
```

需要 exe 与同名 `.pdb` 放在一起。**偏移只对得上产生它的那次构建**——
换了代码或 TFM 之后偏移会变，要用新产物的偏移重查。

## 已知结论（2026-09-17）

Win7（6.1.7601 x86）实测链路：

| 版本 | 结果 |
|---|---|
| net11.0，无 InvariantGlobalization | 起不来（BEX / 0xC0000409） |
| net10.0 + InvariantGlobalization + `GpuBackend=OpenGL` | 能加载、能跑到 `MufiyApp.Run`，随后在 `SkiaBackend.CreatePopWindowGraphics → GetGRContext()` 抛 NRE |
| net10.0 + InvariantGlobalization + `GpuBackend=Software` | **Win7 实测通过，运行流畅不卡** |

`GpuBackend=OpenGL` 在 Windows 上会落到 Skia 的 **D3D12** GPU 上下文，而 Win7 没有 `d3d12.dll`
（`d3d12` 只存在于 Win10+，且不可移植），上下文创建失败返回 null → 解引用即 NRE。
`MufiyAppConfig.GpuBackend` 的**文档默认值就是 `Software`**，模板里覆盖成 `OpenGL` 才是问题源头。

⚠ 别用「放一个同名空壳 DLL」来本机模拟缺库：`NativeLibrary.TryLoad("d3d12.dll")` 会因为空壳存在
而返回 true，直接把程序引到另一条分支，模拟不成立。
