using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Scour.Services;

/// <summary>
/// Reports physical memory usage and frees RAM by trimming process working sets (and, when run
/// elevated, purging the system standby cache). Windows-native addition.
/// </summary>
public sealed partial class RamCleaner
{
    public readonly record struct MemoryStats(ulong TotalBytes, ulong AvailableBytes, uint LoadPercent)
    {
        public ulong UsedBytes => TotalBytes - AvailableBytes;
    }

    public MemoryStats GetStats()
    {
        var m = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (GlobalMemoryStatusEx(ref m))
            return new MemoryStats(m.ullTotalPhys, m.ullAvailPhys, m.dwMemoryLoad);
        return default;
    }

    /// <summary>
    /// Trims every accessible process's working set and attempts to purge the standby list.
    /// Returns (bytesFreed, processesTrimmed). bytesFreed is measured as the rise in available
    /// physical memory across the operation.
    /// </summary>
    public async Task<(long freed, int trimmed)> CleanAsync(CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var before = GetStats();
            int trimmed = 0;
            ConsoleManager.Shared.Append("Trimming process working sets...");

            foreach (var p in Process.GetProcesses())
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    if (EmptyWorkingSet(p.Handle)) trimmed++;
                }
                catch { /* protected/system process — skip */ }
                finally { try { p.Dispose(); } catch { } }
            }

            TryPurgeStandbyList();

            var after = GetStats();
            long freed = (long)after.AvailableBytes - (long)before.AvailableBytes;
            if (freed < 0) freed = 0;
            ConsoleManager.Shared.Append($"Trimmed {trimmed} processes, freed ~{Util.FormatBytes(freed)}.");
            return (freed, trimmed);
        }, ct);
    }

    /// <summary>Purge the OS standby (cached) memory list. Requires admin + SeProfileSingleProcessPrivilege.</summary>
    private static void TryPurgeStandbyList()
    {
        try
        {
            if (!EnablePrivilege("SeProfileSingleProcessPrivilege")) return;
            int command = MemoryPurgeStandbyList;
            var handle = GCHandle.Alloc(command, GCHandleType.Pinned);
            try
            {
                int status = NtSetSystemInformation(SystemMemoryListInformation, handle.AddrOfPinnedObject(), Marshal.SizeOf<int>());
                if (status == 0) ConsoleManager.Shared.Append("Purged system standby cache.");
            }
            finally { handle.Free(); }
        }
        catch { /* not elevated — working-set trim still applied */ }
    }

    // ----- P/Invoke -----

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    private const int SystemMemoryListInformation = 0x50;
    private const int MemoryPurgeStandbyList = 4;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [LibraryImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EmptyWorkingSet(IntPtr hProcess);

    [LibraryImport("ntdll.dll")]
    private static partial int NtSetSystemInformation(int infoClass, IntPtr info, int length);

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID { public uint LowPart; public int HighPart; }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID Luid; public uint Attributes; }

    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint TOKEN_QUERY = 0x0008;
    private const uint SE_PRIVILEGE_ENABLED = 0x0002;

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [LibraryImport("advapi32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool LookupPrivilegeValueW(string? lpSystemName, string lpName, out LUID lpLuid);

    [LibraryImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
        ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr hObject);

    private static bool EnablePrivilege(string name)
    {
        if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var token))
            return false;
        try
        {
            if (!LookupPrivilegeValueW(null, name, out var luid)) return false;
            var tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = SE_PRIVILEGE_ENABLED };
            return AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero)
                   && Marshal.GetLastWin32Error() == 0;
        }
        finally { CloseHandle(token); }
    }
}
