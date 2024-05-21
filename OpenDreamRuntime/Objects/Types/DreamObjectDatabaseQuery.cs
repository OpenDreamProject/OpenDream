using System.Text;
using Microsoft.Data.Sqlite;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectDatabaseQuery(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    private SqliteCommand? _command;
    private SqliteDataReader? _reader;

    private string? _errorMessage;
    private int? _errorCode;

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        if (!args.GetArgument(0).TryGetValueAsString(out var command)) {
            return;
        }

        SetupCommand(command, args.Values[1..]);

    }

    public void SetupCommand(string command, ReadOnlySpan<DreamValue> values) {
        _command = new SqliteCommand(ParseCommandText(command));

        for (var i = 0; i < values.Length; i++) {

            var arg = values[i];

            var type = arg.Type;
            switch (type) {
                case DreamValue.DreamValueType.String:
                    if (arg.TryGetValueAsString(out var stringValue)) {
                        _command.Parameters.AddWithValue($"@{i}", stringValue);
                    }
                    break;
                case DreamValue.DreamValueType.Float:
                    if (arg.TryGetValueAsFloat(out var floatValue)) {
                        _command.Parameters.AddWithValue($"@{i}", floatValue);
                    }
                    break;

                case DreamValue.DreamValueType.DreamResource:
                case DreamValue.DreamValueType.DreamObject:
                case DreamValue.DreamValueType.DreamType:
                case DreamValue.DreamValueType.DreamProc:
                case DreamValue.DreamValueType.Appearance:
                default:
                    // TODO: support saving BLOBS for icons, if we really want to
                    break;
            }

        }
    }

    public List<DreamValue> GetAllColumns() {
        if (_reader is null) {
            return [];
        }

        var names = new List<DreamValue>();
        for (var i = 0; i < _reader.FieldCount; i++) {
            names.Add(new DreamValue(_reader.GetName(i)));
        }

        return names;

    }

    public DreamValue GetColumn(int id) {
        if (_reader is null) {
            return DreamValue.Null;
        }

        try {
            var name = _reader.GetName(id);
            return new DreamValue(name);
        } catch (SqliteException exception) {
            _errorCode = exception.SqliteErrorCode;
            _errorMessage = exception.Message;
        }

        return DreamValue.Null;

    }

    public void ClearCommand() {
        _command = null;
    }

    public void CloseReader() {
        _reader?.Close();
    }

    public int? GetErrorCode() {
        return _errorCode;
    }

    public string? GetErrorMessage() {
        return _errorMessage;
    }

    public void ExecuteCommand(DreamObjectDatabase database) {
        if (!database.TryGetConnection(out var connection)) {
            return;
        }

        if (_command == null) {
            return;
        }

        _command.Connection = connection;

        try {
            _reader = _command.ExecuteReader();
        } catch (SqliteException exception) {
            _errorCode = exception.SqliteErrorCode;
            _errorMessage = exception.Message;
            database.SetError(exception.SqliteErrorCode, exception.Message);
        }
        ClearCommand();
    }

    public void NextRow() {
        _reader?.Read();
    }

    public void Reset() {
        // TODO: this
    }

    public bool TryGetColumn(int column, out DreamValue value) {
        if (_reader is null) {
            value = DreamValue.Null;
            return false;
        }

        value = GetDreamValueFromDbObject(_reader.GetValue(column));
        return true;
    }

    public Dictionary<string, DreamValue>? CurrentRowData() {
        if (_reader is null) {
            return null;
        }

        var dict = new Dictionary<string, DreamValue>();
        var totalColumns = _reader.FieldCount;
        try {
            for (var i = 0; i < totalColumns; i++) {
                var name = _reader.GetName(i);
                var value = _reader.GetValue(i);

                dict[name] = GetDreamValueFromDbObject(value);
            }
        } catch (SqliteException exception) {
            _errorCode = exception.SqliteErrorCode;
            _errorMessage = exception.Message;
        }

        return dict;
    }

    public int RowsAffected() {
        return _reader?.RecordsAffected ?? 0;
    }

    private DreamValue GetDreamValueFromDbObject(object value) {
        return value switch {
            float floatValue => new DreamValue(floatValue),
            double doubleValue => new DreamValue(doubleValue),
            long longValue => new DreamValue(longValue),
            int intValue => new DreamValue(intValue),
            string stringValue => new DreamValue(stringValue),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    private static string ParseCommandText(string text) {

        var newString = new StringBuilder();

        var paramsId = 0;
        var inQuotes = false;
        foreach (var character in text) {
            switch (character)
            {
                case '\'':
                case '"':
                    inQuotes = !inQuotes;
                    break;
                case '?' when !inQuotes:
                    newString.Append($"@{paramsId++}");
                    continue;
            }

            newString.Append(character);
        }

        return newString.ToString();
    }
}
