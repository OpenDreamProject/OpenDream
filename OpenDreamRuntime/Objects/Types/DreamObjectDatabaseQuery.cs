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

    protected override void HandleDeletion(bool possiblyThreaded) {
        if (possiblyThreaded) {
            EnterIntoDelQueue();
            return;
        }

        ClearCommand();
        CloseReader();
        base.HandleDeletion(possiblyThreaded);
    }

    /// <summary>
    /// Sets up the SQLiteCommand, setting up parameters when provided.
    /// Supports strings and floats from DMcode.
    /// </summary>
    /// <param name="command">The command text of the SQLite command, with placeholders denoted by '?'</param>
    /// <param name="values">The values to be substituted into the command</param>
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

    /// <summary>
    /// Gets the names of all the columns in the current query
    /// </summary>
    /// <returns>A list of <see cref="DreamValue"/>s containing the names of the columns in the query</returns>
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

    /// <summary>
    /// Gets the name of a single column in the current query
    /// </summary>
    /// <param name="id">The column ordinal value.</param>
    /// <returns>A <see cref="DreamValue"/> of the name of the column.</returns>
    public DreamValue GetColumn(int id) {
        if (_reader is null) {
            return DreamValue.Null;
        }

        try {
            var name = _reader.GetName(id);
            return new DreamValue(name);
        } catch (IndexOutOfRangeException exception) {
            _errorCode = 1;
            _errorMessage = exception.Message;
        }

        return DreamValue.Null;
    }

    public void ClearCommand() {
        _command?.Dispose();
        _command = null;
    }

    public void CloseReader() {
        _reader?.Dispose();
        _reader = null;
    }

    public int? GetErrorCode() {
        return _errorCode;
    }

    public string? GetErrorMessage() {
        return _errorMessage;
    }

    /// <summary>
    /// Executes the currently held query against the SQLite database
    /// </summary>
    /// <param name="database">The <see cref="DreamObjectDatabase"/> that this query is being run against.</param>
    public void ExecuteCommand(DreamObjectDatabase database) {
        if (!database.TryGetConnection(out var connection)) {
            throw new DMCrashRuntime("Bad database");
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
    }

    public void NextRow() {
        _reader?.Read();
    }

    /// <summary>
    /// Attempts to fetch the value of a specific column.
    /// </summary>
    /// <param name="column">The ordinal column number</param>
    /// <param name="value">The out variable to be populated with the <see cref="DreamValue"/>of the result.</param>
    /// <returns></returns>
    public bool TryGetColumn(int column, out DreamValue value) {
        if (_reader is null) {
            value = DreamValue.Null;
            return false;
        }

        try {
            value = GetDreamValueFromDbObject(_reader.GetValue(column));
            return true;
        } catch (Exception exception) {
            _errorCode = 1;
            _errorMessage = exception.Message;
        }

        value = DreamValue.Null;
        return false;
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
        } catch (InvalidOperationException exception) {
            _errorCode = 1;
            _errorMessage = exception.Message;
        }

        return dict;
    }

    public int RowsAffected() {
        return _reader?.RecordsAffected ?? 0;
    }

    /// <summary>
    /// Converts a <see cref="object"/> retrieved from the SQLite database to a <see cref="DreamValue"/> containing the value.
    /// </summary>
    /// <param name="value">The <see cref="object"/> from the database.</param>
    /// <returns>A <see cref="DreamValue"/> containing the value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Unsupported data type</exception>
    private static DreamValue GetDreamValueFromDbObject(object value) {
        return value switch {
            float floatValue => new DreamValue(floatValue),
            double doubleValue => new DreamValue(doubleValue),
            long longValue => new DreamValue(longValue),
            int intValue => new DreamValue(intValue),
            string stringValue => new DreamValue(stringValue),
            _ => throw new ArgumentOutOfRangeException(nameof(value)),
        };
    }

    /// <summary>
    /// Builds a new string, converting '?' characters to expressions we can bind to later
    /// </summary>
    /// <param name="text">The raw command text</param>
    /// <returns>A <see cref="string"/> with the characters converted</returns>
    private static string ParseCommandText(string text) {
        var newString = new StringBuilder();

        var paramsId = 0;
        var inQuotes = false;
        foreach (var character in text) {
            switch (character) {
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
