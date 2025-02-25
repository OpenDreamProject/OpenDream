using OpenDreamRuntime.Objects;
using OpenDreamRuntime.Objects.Types;
using DreamValueTypeFlag = OpenDreamRuntime.DreamValue.DreamValueTypeFlag;

namespace OpenDreamRuntime.Procs.Native;

internal static class DreamProcNativeDatabaseQuery {
    [DreamProc("Add")]
    [DreamProcParameter("text", Type = DreamValueTypeFlag.String)]
    [DreamProcParameter("item1")]
    public static DreamValue NativeProc_Add(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        query.ClearCommand();

        if (!bundle.GetArgument(0, "text").TryGetValueAsString(out var command)) {
            return DreamValue.Null;
        }

        query.SetupCommand(command, bundle.Arguments[1..]);

        return DreamValue.Null;
    }

    [DreamProc("Clear")]
    public static DreamValue NativeProc_Clear(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        query.ClearCommand();

        return DreamValue.Null;
    }

    [DreamProc("Close")]
    public static DreamValue NativeProc_Close(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        query.CloseReader();

        return DreamValue.Null;
    }

    [DreamProc("Columns")]
    [DreamProcParameter("column", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_Columns(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        if (bundle.GetArgument(0, "column").TryGetValueAsInteger(out var column)) {
            return query.GetColumn(column);
        }

        var list = bundle.ObjectTree.CreateList();

        foreach (var value in query.GetAllColumns()) {
            list.AddValue(value);
        }

        return new DreamValue(list);
    }

    [DreamProc("Error")]
    public static DreamValue NativeProc_Error(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        var code = query.GetErrorCode();
        return code.HasValue ? new DreamValue(code.Value) : DreamValue.Null;
    }

    [DreamProc("ErrorMsg")]
    public static DreamValue NativeProc_ErrorMsg(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        var message = query.GetErrorMessage();

        return message == null ? DreamValue.Null : new DreamValue(message);
    }

    [DreamProc("Execute")]
    [DreamProcParameter("database", Type = DreamValueTypeFlag.DreamObject)]
    public static DreamValue NativeProc_Execute(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        if (!bundle.GetArgument(0, "database").TryGetValueAsDreamObject(out DreamObjectDatabase? database))
            return DreamValue.Null;

        query.ExecuteCommand(database);

        return DreamValue.Null;
    }

    [DreamProc("RowsAffected")]
    public static DreamValue NativeProc_RowsAffected(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        return new DreamValue(query.RowsAffected());
    }

    [DreamProc("NextRow")]
    public static DreamValue NativeProc_NextRow(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        query.NextRow();

        return DreamValue.Null;
    }

    [DreamProc("GetColumn")]
    [DreamProcParameter("column", Type = DreamValueTypeFlag.Float)]
    public static DreamValue NativeProc_GetColumn(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        if (!bundle.GetArgument(0, "column").TryGetValueAsInteger(out var column)) {
            return DreamValue.Null;
        }

        if (!query.TryGetColumn(column, out var value)) {
            return DreamValue.Null;
        }

        return value;
    }

    [DreamProc("GetRowData")]
    public static DreamValue NativeProc_GetRowData(NativeProc.Bundle bundle, DreamObject? src, DreamObject? usr) {
        var query = (DreamObjectDatabaseQuery)src!;

        var data = query.CurrentRowData();

        if (data == null) {
            return DreamValue.Null;
        }

        var list = bundle.ObjectTree.CreateList();
        foreach (var entry in data) {
            list.SetValue(new DreamValue(entry.Key), entry.Value);
        }

        return new DreamValue(list);
    }
}
