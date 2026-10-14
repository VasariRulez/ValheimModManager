namespace ValheimModManager.App.Services;

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class MemoryOptimizationService
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetProcessWorkingSetSize(IntPtr process, nint minimumWorkingSetSize, nint maximumWorkingSetSize);

    public static void CompactAndTrimMemory()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, blocking: true, compacting: true);

            if (OperatingSystem.IsWindows())
            {
                using var proc = Process.GetCurrentProcess();
                SetProcessWorkingSetSize(proc.Handle, -1, -1);
            }
        }
        catch
        {
            // Trimming best-effort
        }
    }
}
