using System.Data;
using Microsoft.Data.Sqlite;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectDatabase(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
    /// <summary>
    /// Database location on the host OS
    /// </summary>
    public string Path = default!;

    private SqliteConnection? _connection;

    private string? _errorMessage;
    private int? _errorCode;

    public override void Initialize(DreamProcArguments args) {
        base.Initialize(args);

        if (!args.GetArgument(0).TryGetValueAsString(out var filename)) {
            return;
        }

        Open(filename);
    }

    public void Open(string filename) {

        if (_connection?.State == ConnectionState.Open) {
            Close();
        }

        Path = filename;

        _connection = new SqliteConnection($"Data Source={filename};Mode=ReadWriteCreate");

        try {
            _connection.Open();
        } catch (SqliteException exception) {
            Logger.GetSawmill("opendream").Error($"Failed to open database {filename} - {exception}");
        }
    }

    public bool TryGetConnection(out SqliteConnection? connection) {
        if (_connection?.State == ConnectionState.Open) {
            connection = _connection;
            return true;
        }

        connection = null;
        return false;
    }

    public void SetError(int code, string message) {
        _errorCode = code;
        _errorMessage = message;
    }

    public int? GetErrorCode() {
        return _errorCode;
    }

    public string? GetErrorMessage() {
        return _errorMessage;
    }

    public void Close() {
        _connection?.Close();
    }

}
