﻿using System.Runtime.CompilerServices;
using System.Threading;
using bottlenoselabs.C2CS.Runtime;
using Robust.Shared.Console;
using static Tracy.PInvoke;

namespace OpenDreamRuntime;

public static class Profiler {
    //internal tracking for unique IDs for memory zones, because we can't use actual object pointers sadly as they are unstable outside of `unsafe`
    private static ulong _memoryUid;

    // Plot names need to be cached for the lifetime of the program
    // see also Tracy docs section 3.1
    private static readonly Dictionary<string, CString> PlotNameCache = new();

    private static bool _isActivated;

    /// <summary>
    /// Begins a new <see cref="ProfilerZone"/> and returns the handle to that zone. Time
    /// spent inside a zone is calculated by Tracy and shown in the profiler. A zone is
    /// ended when <see cref="ProfilerZone.Dispose"/> is called either automatically via
    /// disposal scope rules or by calling it manually.
    /// </summary>
    /// <param name="zoneName">A custom name for this zone.</param>
    /// <param name="active">Is the zone active. An inactive zone wont be shown in the profiler.</param>
    /// <param name="color">An <c>RRGGBB</c> color code that Tracy will use to color the zone in the profiler.</param>
    /// <param name="text">Arbitrary text associated with this zone.</param>
    /// <param name="lineNumber">
    /// The source code line number that this zone begins at.
    /// If this param is not explicitly assigned the value will be provided by <see cref="CallerLineNumberAttribute"/>.
    /// </param>
    /// <param name="filePath">
    /// The source code file path that this zone begins at.
    /// If this param is not explicitly assigned the value will be provided by <see cref="CallerFilePathAttribute"/>.
    /// </param>
    /// <param name="memberName">
    /// The source code member name that this zone begins at.
    /// If this param is not explicitly assigned the value will be provided by <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    /// <returns></returns>
    public static ProfilerZone? BeginZone(
        string? zoneName = null,
        bool active = true,
        uint color = 0,
        string? text = null,
        [CallerLineNumber] int lineNumber = 0,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null) {
        #if !TOOLS
        return null;
        #else
        //if we're in a tools build, we still don't want the perf hit unless it's requested
        if (!_isActivated)
            return null;

        using var fileStr = GetCString(filePath, out var fileLn);
        using var memberStr = GetCString(memberName, out var memberLn);
        using var nameStr = GetCString(zoneName, out var nameLn);
        var srcLocId = TracyAllocSrclocName((uint)lineNumber, fileStr, fileLn, memberStr, memberLn, nameStr, nameLn, color);
        var context = TracyEmitZoneBeginAlloc(srcLocId, active ? 1 : 0);

        if (text != null) {
            using var textStr = GetCString(text, out var textLn);
            TracyEmitZoneText(context, textStr, textLn);
        }

        return new ProfilerZone(context);
        #endif
    }

    public static ProfilerMemory? BeginMemoryZone(ulong size, string? name) {
        #if !TOOLS
        return null;
        #else
        //if we're in a tools build, we still don't want the perf hit unless it's requested
        if (!_isActivated)
            return null;

        var nameStr = name is null ? GetPlotCString("null") : GetPlotCString(name);
        unsafe {
            return new ProfilerMemory((void*)(Interlocked.Add(ref _memoryUid, size)-size), size, nameStr);
        }
        #endif
    }

    public static void Activate() {
        _isActivated = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsActivated() {
        return _isActivated;
    }

    /// <summary>
    /// Configure how Tracy will display plotted values.
    /// </summary>
    /// <param name="name">
    /// Name of the plot to configure. Each <paramref name="name"/> represents a unique plot.
    /// </param>
    /// <param name="type">
    /// Changes how the values in the plot are presented by the profiler.
    /// </param>
    /// <param name="step">
    /// Determines whether the plot will be displayed as a staircase or will smoothly change between plot points
    /// </param>
    /// <param name="fill">
    /// If <see langword="false"/> the the area below the plot will not be filled with a solid color.
    /// </param>
    /// <param name="color">
    /// An <c>RRGGBB</c> color code that Tracy will use to color the plot in the profiler.
    /// </param>
    public static void PlotConfig(string name, PlotType type = PlotType.Number, bool step = false, bool fill = true, uint color = 0) {
        var nameStr = GetPlotCString(name);

        TracyEmitPlotConfig(nameStr, (int)type, step ? 1 : 0, fill ? 1 : 0, color);
    }

    /// <summary>
    /// Add a <see langword="double"/> value to a plot.
    /// </summary>
    public static void Plot(string name, double val) {
        var nameStr = GetPlotCString(name);

        TracyEmitPlot(nameStr, val);
    }

    /// <summary>
    /// Add a <see langword="float"/> value to a plot.
    /// </summary>
    public static void Plot(string name, int val) {
        var nameStr = GetPlotCString(name);

        TracyEmitPlotInt(nameStr, val);
    }

    /// <summary>
    /// Add a <see langword="float"/> value to a plot.
    /// </summary>
    public static void Plot(string name, float val) {
        var nameStr = GetPlotCString(name);

        TracyEmitPlotFloat(nameStr, val);
    }

    private static CString GetPlotCString(string name) {
        if (!PlotNameCache.TryGetValue(name, out var plotCString)) {
            plotCString = CString.FromString(name);
            PlotNameCache.Add(name, plotCString);
        }

        return plotCString;
    }

    /// <summary>
    /// Emit a string that will be included along with the trace description.
    /// </summary>
    /// <remarks>
    /// Viewable in the Info tab in the profiler.
    /// </remarks>
    public static void AppInfo(string appInfo) {
        using var infoStr = GetCString(appInfo, out var infoLn);

        TracyEmitMessageAppinfo(infoStr, infoLn);
    }

    /// <summary>
    /// Emit the top-level frame marker.
    /// </summary>
    /// <remarks>
    /// Tracy Cpp API and docs refer to this as the <c>FrameMark</c> macro.
    /// </remarks>
    public static void EmitFrameMark() {
        //if we're in a tools build, we still don't want the perf hit unless it's requested
        if (!_isActivated)
            return;

        TracyEmitFrameMark(null);
    }

    /// <summary>
    /// Is the app connected to the external profiler?
    /// </summary>
    /// <returns></returns>
    public static bool IsConnected() {
        return TracyConnected() != 0;
    }

    /// <summary>
    /// Creates a <seealso cref="CString"/> for use by Tracy. Also returns the
    /// length of the string for interop convenience.
    /// </summary>
    public static CString GetCString(string? fromString, out ulong cLength) {
        if (fromString == null) {
            cLength = 0;
            return new CString(0);
        }

        cLength = (ulong)fromString.Length;
        return CString.FromString(fromString);
    }

    public enum PlotType{
        /// <summary>
        /// Values will be displayed as plain numbers.
        /// </summary>
        Number = 0,

        /// <summary>
        /// Treats the values as memory sizes. Will display kilobytes, megabytes, etc.
        /// </summary>
        Memory = 1,

        /// <summary>
        /// Values will be displayed as percentage (with value 100 being equal to 100%).
        /// </summary>
        Percentage = 2,
    }
}

public readonly struct ProfilerZone : IDisposable {
    public readonly TracyCZoneCtx Context;

    public uint Id => Context.Data.Id;

    public int Active => Context.Data.Active;

    internal ProfilerZone(TracyCZoneCtx context) {
        Context = context;
    }

    public void EmitName(string name) {
        using var nameStr = Profiler.GetCString(name, out var nameLn);
        TracyEmitZoneName(Context, nameStr, nameLn);
    }

    public void EmitColor(uint color) {
        TracyEmitZoneColor(Context, color);
    }

    public void EmitText(string text) {
        using var textStr = Profiler.GetCString(text, out var textLn);
        TracyEmitZoneText(Context, textStr, textLn);
    }

    public void Dispose() {
        TracyEmitZoneEnd(Context);
    }
}

public sealed unsafe class ProfilerMemory {
    private readonly void* _ptr;
    private readonly CString _name;
    private int _hasRun;

    internal ProfilerMemory(void* pointer, ulong size, CString name) {
        _ptr = pointer;
        _name = name;
        TracyEmitMemoryAllocNamed(_ptr, size, 0, _name);
    }

    public void ReleaseMemory() {
        if (Interlocked.Exchange(ref _hasRun, 1) == 0) { // only run once
            TracyEmitMemoryFreeNamed(_ptr, 0, _name);
        }
    }

    ~ProfilerMemory() {
        ReleaseMemory();
    }
}

public sealed class ActivateProfilerCommand : IConsoleCommand {
    // ReSharper disable once StringLiteralTypo
    public string Command => "activatetracy";
    public string Description => "Enable the Tracy profiler for the server.";
    public string Help => "";
    public bool RequireServerOrSingleplayer => true;

    public void Execute(IConsoleShell shell, string argStr, string[] args) {
        if (!shell.IsLocal) {
            shell.WriteError("You cannot use this command as a client. Execute it on the server console.");
            return;
        }

        if (Profiler.IsActivated()) {
            shell.WriteError("Tracy is already activated.");
            return;
        }

        Profiler.Activate();
        shell.WriteLine("Tracy activated. You can now use the profiler.");
    }
}
