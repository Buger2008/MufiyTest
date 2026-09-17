import ctypes, subprocess, sys, time
from ctypes import wintypes

TH32CS_SNAPMODULE = 0x00000008
TH32CS_SNAPMODULE32 = 0x00000010
INVALID_HANDLE_VALUE = ctypes.c_void_p(-1).value

class MODULEENTRY32W(ctypes.Structure):
    _fields_ = [("dwSize", wintypes.DWORD),
                ("th32ModuleID", wintypes.DWORD),
                ("th32ProcessID", wintypes.DWORD),
                ("GlblcntUsage", wintypes.DWORD),
                ("ProccntUsage", wintypes.DWORD),
                ("modBaseAddr", ctypes.c_void_p),
                ("modBaseSize", wintypes.DWORD),
                ("hModule", wintypes.HMODULE),
                ("szModule", wintypes.WCHAR * 256),
                ("szExePath", wintypes.WCHAR * 260)]

k32 = ctypes.WinDLL('kernel32', use_last_error=True)
u32 = ctypes.WinDLL('user32', use_last_error=True)
k32.CreateToolhelp32Snapshot.restype = ctypes.c_void_p
k32.Module32FirstW.argtypes = [ctypes.c_void_p, ctypes.POINTER(MODULEENTRY32W)]
k32.Module32NextW.argtypes = [ctypes.c_void_p, ctypes.POINTER(MODULEENTRY32W)]

exe = sys.argv[1]
wait = float(sys.argv[2]) if len(sys.argv) > 2 else 4.0

p = subprocess.Popen([exe])
t0 = time.time()
hwnd = None
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
                r.append(b.value)
        return True
    u32.EnumWindows(WE(cb), 0)
    if r:
        hwnd = r[0]
        break
    time.sleep(0.02)
print('窗口:', hwnd, ' 存活:', p.poll() is None)
time.sleep(wait)

mods = []
if p.poll() is None:
    snap = k32.CreateToolhelp32Snapshot(TH32CS_SNAPMODULE | TH32CS_SNAPMODULE32, p.pid)
    if snap and snap != INVALID_HANDLE_VALUE:
        me = MODULEENTRY32W()
        me.dwSize = ctypes.sizeof(me)
        if k32.Module32FirstW(snap, ctypes.byref(me)):
            while True:
                mods.append(me.szModule)
                if not k32.Module32NextW(snap, ctypes.byref(me)):
                    break
        k32.CloseHandle(ctypes.c_void_p(snap))

print('已加载模块数:', len(mods))
for key in ('d3d12', 'd3d11', 'dxgi', 'd3dcompiler', 'opengl32', 'libegl', 'libglesv2', 'libskia', 'sdl3', 'vulkan'):
    hit = [m for m in mods if key in m.lower()]
    print(f'  {key:14} -> {hit if hit else "未加载"}')

if p.poll() is None:
    p.terminate()
