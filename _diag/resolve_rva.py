import ctypes, sys, os
from ctypes import wintypes

class SYMBOL_INFO(ctypes.Structure):
    _fields_ = [
        ("SizeOfStruct", wintypes.ULONG),
        ("TypeIndex", wintypes.ULONG),
        ("Reserved", ctypes.c_ulonglong * 2),
        ("Index", wintypes.ULONG),
        ("Size", wintypes.ULONG),
        ("ModBase", ctypes.c_ulonglong),
        ("Flags", wintypes.ULONG),
        ("Value", ctypes.c_ulonglong),
        ("Address", ctypes.c_ulonglong),
        ("Register", wintypes.ULONG),
        ("Scope", wintypes.ULONG),
        ("Tag", wintypes.ULONG),
        ("NameLen", wintypes.ULONG),
        ("MaxNameLen", wintypes.ULONG),
        ("Name", ctypes.c_char * 4096),
    ]

class IMAGEHLP_LINE64(ctypes.Structure):
    _fields_ = [
        ("SizeOfStruct", wintypes.DWORD),
        ("Key", ctypes.c_void_p),
        ("LineNumber", wintypes.DWORD),
        ("FileName", ctypes.c_char_p),
        ("Address", ctypes.c_ulonglong),
    ]

k32 = ctypes.WinDLL('kernel32', use_last_error=True)
dbg = ctypes.WinDLL('dbghelp', use_last_error=True)

dbg.SymSetOptions(0x00000002 | 0x00000004 | 0x00000200 | 0x00000010 | 0x00000001)  # UNDNAME|DEFERRED_LOADS|LOAD_LINES|FAIL_CRITICAL|CASE_INSENSITIVE
dbg.SymInitialize.argtypes = [wintypes.HANDLE, ctypes.c_char_p, wintypes.BOOL]
dbg.SymLoadModuleEx.argtypes = [wintypes.HANDLE, wintypes.HANDLE, ctypes.c_char_p, ctypes.c_char_p,
                                ctypes.c_ulonglong, wintypes.DWORD, ctypes.c_void_p, wintypes.DWORD]
dbg.SymLoadModuleEx.restype = ctypes.c_ulonglong
dbg.SymFromAddr.argtypes = [wintypes.HANDLE, ctypes.c_ulonglong, ctypes.POINTER(ctypes.c_ulonglong), ctypes.POINTER(SYMBOL_INFO)]
dbg.SymFromAddr.restype = wintypes.BOOL
dbg.SymGetLineFromAddr64.argtypes = [wintypes.HANDLE, ctypes.c_ulonglong, ctypes.POINTER(wintypes.DWORD), ctypes.POINTER(IMAGEHLP_LINE64)]
dbg.SymGetLineFromAddr64.restype = wintypes.BOOL

exe = sys.argv[1]
offset = int(sys.argv[2], 16)
hproc = k32.GetCurrentProcess()
if not dbg.SymInitialize(hproc, None, False):
    print('SymInitialize 失败', ctypes.get_last_error()); sys.exit(1)
base = dbg.SymLoadModuleEx(hproc, None, exe.encode('utf-8'), None, 0, 0, None, 0)
print(f'模块载入 base=0x{base:x}  (查询 RVA 0x{offset:x})')
if not base:
    print('SymLoadModuleEx 失败', ctypes.get_last_error()); sys.exit(1)
addr = base + offset
disp = ctypes.c_ulonglong(0)
si = SYMBOL_INFO()
si.SizeOfStruct = 88
si.MaxNameLen = 4000
if dbg.SymFromAddr(hproc, addr, ctypes.byref(disp), ctypes.byref(si)):
    print(f'SYMBOL : {si.Name.decode("utf-8","replace")}')
    print(f'        符号内偏移 +0x{disp.value:x}')
else:
    print('SymFromAddr 失败（无符号）', ctypes.get_last_error())
ln = IMAGEHLP_LINE64()
ln.SizeOfStruct = ctypes.sizeof(IMAGEHLP_LINE64)
d2 = wintypes.DWORD(0)
if dbg.SymGetLineFromAddr64(hproc, addr, ctypes.byref(d2), ctypes.byref(ln)):
    print(f'SOURCE : {ln.FileName.decode("utf-8","replace")}:{ln.LineNumber}')
else:
    print('无行号信息')
