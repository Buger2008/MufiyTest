"""抓取窗口内容并统计像素分布，用来判断"出了窗口"和"真的渲染了"。

用法: python check_render.py <exe路径> [等待秒数]
判据: 不同颜色数明显大于 1、且非背景色像素占比可观 -> 有真实绘制内容。
"""
import ctypes, subprocess, sys, time
from ctypes import wintypes

u32 = ctypes.WinDLL('user32', use_last_error=True)
g32 = ctypes.WinDLL('gdi32', use_last_error=True)


class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [("biSize", wintypes.DWORD), ("biWidth", wintypes.LONG), ("biHeight", wintypes.LONG),
                ("biPlanes", wintypes.WORD), ("biBitCount", wintypes.WORD), ("biCompression", wintypes.DWORD),
                ("biSizeImage", wintypes.DWORD), ("biXPelsPerMeter", wintypes.LONG),
                ("biYPelsPerMeter", wintypes.LONG), ("biClrUsed", wintypes.DWORD), ("biClrImportant", wintypes.DWORD)]


class BITMAPINFO(ctypes.Structure):
    _fields_ = [("bmiHeader", BITMAPINFOHEADER), ("bmiColors", wintypes.DWORD * 3)]


exe = sys.argv[1]
settle = float(sys.argv[2]) if len(sys.argv) > 2 else 3.0

p = subprocess.Popen([exe])
hwnd = None
t0 = time.time()
WE = ctypes.WINFUNCTYPE(wintypes.BOOL, wintypes.HWND, wintypes.LPARAM)
while time.time() - t0 < 15:
    if p.poll() is not None:
        break
    r = []
    def cb(h, l):
        pid = wintypes.DWORD()
        u32.GetWindowThreadProcessId(h, ctypes.byref(pid))
        if pid.value == p.pid:
            b = ctypes.create_unicode_buffer(256); u32.GetWindowTextW(h, b, 256)
            rc = wintypes.RECT(); u32.GetWindowRect(h, ctypes.byref(rc))
            if u32.IsWindowVisible(h) and rc.right - rc.left > 50:
                r.append((h, b.value, rc.right - rc.left, rc.bottom - rc.top))
        return True
    u32.EnumWindows(WE(cb), 0)
    if r:
        hwnd = r[0]
        break
    time.sleep(0.02)

if not hwnd:
    print('没出现可见窗口，进程存活=', p.poll() is None)
    if p.poll() is None:
        p.terminate()
    sys.exit(0)

h, title, w, ht = hwnd
print(f'窗口 "{title}" {w}x{ht}，等待 {settle}s 后截图')
time.sleep(settle)

hdc = u32.GetWindowDC(h)
mem = g32.CreateCompatibleDC(hdc)
bi = BITMAPINFO()
bi.bmiHeader.biSize = ctypes.sizeof(BITMAPINFOHEADER)
bi.bmiHeader.biWidth = w
bi.bmiHeader.biHeight = -ht
bi.bmiHeader.biPlanes = 1
bi.bmiHeader.biBitCount = 32
bi.bmiHeader.biCompression = 0
buf = ctypes.create_string_buffer(w * ht * 4)
bits = ctypes.c_void_p()
bmp = g32.CreateDIBSection(hdc, ctypes.byref(bi), 0, ctypes.byref(bits), None, 0)
g32.SelectObject(mem, bmp)
ok = g32.BitBlt(mem, 0, 0, w, ht, hdc, 0, 0, 0x00CC0020)
data = ctypes.string_at(bits, w * ht * 4)
colors = {}
bg = data[0:3]
nonbg = 0
for i in range(0, len(data), 4):
    px = data[i:i + 3]
    colors[px] = colors.get(px, 0) + 1
    if px != bg:
        nonbg += 1
total = w * ht
print('BitBlt:', bool(ok))
print('不同颜色数:', len(colors))
print(f'非左上角像素占比: {nonbg / total:.1%}')
print('出现次数最多的 5 种颜色:', sorted(colors.items(), key=lambda kv: -kv[1])[:5])

g32.DeleteObject(bmp); g32.DeleteDC(mem); u32.ReleaseDC(h, hdc)
p.terminate()
