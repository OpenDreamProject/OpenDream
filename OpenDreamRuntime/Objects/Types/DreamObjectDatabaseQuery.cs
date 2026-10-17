using System.Text;
using Microsoft.Data.Sqlite;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectDatabaseQuery(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    private SqliteCommand? _command;
    private SqliteDataReader? _reader;
    private DreamObjectDatabase? _temporaryDatabase;

    private string? _errorMessage;
    private int? _errorCode;

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        var commandArgument = args.GetArgument(0);
        if (commandArgument.IsNull) {
            return;
        }

        if (!commandArgument.TryGetValueAsString(out var command)) {
            throw new DMCrashRuntime("Invalid database query text");
        }

        SetupCommand(command, args.Values[1..]);
    }

    protected override void HandleDeletion() {
        ClearCommand();
        CloseReader();
        CloseTemporaryDatabase();
        base.HandleDeletion();
    }

    /// <summary>
    /// Associates a database with this query that was opened from a filename
    /// (BYOND's <c>query.Execute(filename)</c> overload). The query owns it for
    /// the duration of reading rows, and closes it when done.
    /// </summary>
    public void SetTemporaryDatabase(DreamObjectDatabase database) {
        CloseTemporaryDatabase();
        _temporaryDatabase = database;
    }

    private void CloseTemporaryDatabase() {
        _temporaryDatabase?.Close();
        _temporaryDatabase?.DecRef();
        _temporaryDatabase = null;
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

            if (arg.IsNull) {
                _command.Parameters.AddWithValue($"@{i}", DBNull.Value);
            } else if (arg.TryGetValueAsDreamResource(out var resource)) {
                AddBlobParameter(i, resource.ResourceData);
            } else if (arg.TryGetValueAsDreamObject<DreamObjectIcon>(out var icon)) {
                AddBlobParameter(i, icon.Icon.GenerateDMI().ResourceData);
            } else if (arg.TryGetValueAsString(out var stringValue)) {
                _command.Parameters.AddWithValue($"@{i}", stringValue);
            } else if (arg.TryGetValueAsFloat(out var floatValue)) {
                _command.Parameters.AddWithValue($"@{i}", floatValue);
            } else {
                throw new DMCrashRuntime("Invalid database query parameter");
            }
        }
    }

    private void AddBlobParameter(int index, byte[]? data) {
        _command!.Parameters.Add($"@{index}", SqliteType.Blob).Value = data is null ? DBNull.Value : data;
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
    /// Gets the name of a selected column for <c>/database/query.Columns(index)</c>.
    /// </summary>
    /// <param name="column">The one-based column index.</param>
    /// <returns>The column name, or an empty string for an invalid index.</returns>
    public DreamValue GetColumnName(int column) {
        if (_reader is null || column <= 0 || column > _reader.FieldCount) {
            // BYOND returns an empty string for an invalid Columns() index.
            return new DreamValue(string.Empty);
        }

        return new DreamValue(_reader.GetName(column - 1));
    }

    public bool HasCommand => _command != null;

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
    public bool ExecuteCommand(DreamObjectDatabase database) {
        if (_command == null) {
            return false;
        }

        CloseReader();

        if (!database.TryGetConnection(out var connection)) {
            throw new DMCrashRuntime("Bad database");
        }

        _command.Connection = connection;

        try {
            _reader = _command.ExecuteReader();
            return true;
        } catch (SqliteException exception) {
            _errorCode = exception.SqliteErrorCode;
            _errorMessage = exception.Message;
            database.SetError(exception.SqliteErrorCode, exception.Message);
            return false;
        }
    }

    public bool NextRow() {
        return _reader?.Read() ?? false;
    }

    /// <summary>
    /// Attempts to fetch the current row's value for <c>/database/query.GetColumn(index)</c>.
    /// </summary>
    /// <param name="column">The one-based column index.</param>
    /// <param name="value">The current row's column value.</param>
    /// <returns>True when the value was read; otherwise false.</returns>
    public bool TryGetColumnValue(int column, out DreamValue value) {
        if (_reader is null || column <= 0 || column > _reader.FieldCount) {
            // BYOND returns null for an invalid GetColumn() index.
            value = DreamValue.Null;
            return false;
        }

        try {
            value = GetDreamValueFromDbObject(_reader.GetValue(column - 1));
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
            // BYOND's database API returns null, rather than exposing SQLite BLOB data.
            byte[] => DreamValue.Null,
            DBNull => DreamValue.Null,
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
