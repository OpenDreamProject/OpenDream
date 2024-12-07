using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using bottlenoselabs.C2CS.Runtime;
using static Tracy.PInvoke;

public static class Profiler
{
    // Plot names need to be cached for the lifetime of the program
    // seealso Tracy docs section 3.1
    private static readonly Dictionary<string, CString> PlotNameCache = new Dictionary<string, CString>();

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
    /// If this param is not explicitly assigned the value will provided by <see cref="CallerLineNumberAttribute"/>.
    /// </param>
    /// <param name="filePath">
    /// The source code file path that this zone begins at.
    /// If this param is not explicitly assigned the value will provided by <see cref="CallerFilePathAttribute"/>.
    /// </param>
    /// <param name="memberName">
    /// The source code member name that this zone begins at.
    /// If this param is not explicitly assigned the value will provided by <see cref="CallerMemberNameAttribute"/>.
    /// </param>
    /// <returns></returns>
    public static ProfilerZone BeginZone(
        string? zoneName = null,
        bool active = true,
        uint color = 0,
        string? text = null,
        [CallerLineNumber] uint lineNumber = 0,
        [CallerFilePath] string? filePath = null,
        [CallerMemberName] string? memberName = null)
    {
        using var filestr = GetCString(filePath, out var fileln);
        using var memberstr = GetCString(memberName, out var memberln);
        using var namestr = GetCString(zoneName, out var nameln);
        var srcLocId = TracyAllocSrclocName(lineNumber, filestr, fileln, memberstr, memberln, namestr, nameln, color);
        var context = TracyEmitZoneBeginAlloc(srcLocId, active ? 1 : 0);

        if (text != null)
        {
            using var textstr = GetCString(text, out var textln);
            TracyEmitZoneText(context, textstr, textln);
        }

        return new ProfilerZone(context);
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
    public static void PlotConfig(string name, PlotType type = PlotType.Number, bool step = false, bool fill = true, uint color = 0)
    {
        var namestr = GetPlotCString(name);
        TracyEmitPlotConfig(namestr, (int)type, step ? 1 : 0, fill ? 1 : 0, color);
    }

    /// <summary>
    /// Add a <see langword="double"/> value to a plot.
    /// </summary>
    public static void Plot(string name, double val)
    {
        var namestr = GetPlotCString(name);
        TracyEmitPlot(namestr, val);
    }

    /// <summary>
    /// Add a <see langword="float"/> value to a plot.
    /// </summary>
    public static void Plot(string name, int val)
    {
        var namestr = GetPlotCString(name);
        TracyEmitPlotInt(namestr, val);
    }

    /// <summary>
    /// Add a <see langword="float"/> value to a plot.
    /// </summary>
    public static void Plot(string name, float val)
    {
        var namestr = GetPlotCString(name);
        TracyEmitPlotFloat(namestr, val);
    }

    private static CString GetPlotCString(string name)
    {
        if(!PlotNameCache.TryGetValue(name, out var plotCString))
        {
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
    public static void AppInfo(string appInfo)
    {
        using var infostr = GetCString(appInfo, out var infoln);
        TracyEmitMessageAppinfo(infostr, infoln);
    }


    /// <summary>
    /// Emit the top-level frame marker.
    /// </summary>
    /// <remarks>
    /// Tracy Cpp API and docs refer to this as the <c>FrameMark</c> macro.
    /// </remarks>
    public static void EmitFrameMark()
    {
        TracyEmitFrameMark(null);
    }

    /// <summary>
    /// Is the app connected to the external profiler?
    /// </summary>
    /// <returns></returns>
    public static bool IsConnected()
    {
        return TracyConnected() != 0;
    }

    /// <summary>
    /// Creates a <seealso cref="CString"/> for use by Tracy. Also returns the
    /// length of the string for interop convenience.
    /// </summary>
    public static CString GetCString(string? fromString, out ulong clength)
    {
        if (fromString == null)
        {
            clength = 0;
            return new CString(0);
        }
        clength = (ulong)fromString.Length;
        return CString.FromString(fromString);
    }

    public enum PlotType
    {
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

public readonly struct ProfilerZone : IDisposable
{
    public readonly TracyCZoneCtx Context;

    public uint Id => Context.Data.Id;

    public int Active => Context.Data.Active;

    internal ProfilerZone(TracyCZoneCtx context)
    {
        Context = context;
    }

    public void EmitName(string name)
    {
        using var namestr = Profiler.GetCString(name, out var nameln);
        TracyEmitZoneName(Context, namestr, nameln);
    }

    public void EmitColor(uint color)
    {
        TracyEmitZoneColor(Context, color);
    }

    public void EmitText(string text)
    {
        using var textstr = Profiler.GetCString(text, out var textln);
        TracyEmitZoneText(Context, textstr, textln);
    }

    public void Dispose()
    {
        TracyEmitZoneEnd(Context);
    }
}
