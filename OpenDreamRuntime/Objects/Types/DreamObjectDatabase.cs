using System.Data;
using Microsoft.Data.Sqlite;
using OpenDreamRuntime.Procs;

namespace OpenDreamRuntime.Objects.Types;

public sealed class DreamObjectDatabase(DreamObjectDefinition objectDefinition) : DreamObject(objectDefinition) {
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

    protected override void HandleDeletion() {
        Close();
        base.HandleDeletion();
    }

    /// <summary>
    /// Establish the connection to our SQLite database
    /// </summary>
    /// <param name="filename">The path to the SQLite file</param>
    public void Open(string filename) {
        if (_connection?.State == ConnectionState.Open) {
            Close();
        }

        _connection = new SqliteConnection($"Data Source={filename};Mode=ReadWriteCreate");

        try {
            _connection.Open();
        } catch (SqliteException exception) {
            Logger.GetSawmill("opendream.db").Error($"Failed to open database {filename} - {exception}");
        }
    }

    /// <summary>
    /// Attempts to get the current connection to the SQLite database, if it is open
    /// </summary>
    /// <param name="connection">Variable to be populated with the connection.</param>
    /// <returns>Boolean of the success of the operation.</returns>
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

    /// <summary>
    /// Closes the current SQLite connection, if it is established.
    /// </summary>
    public void Close() {
        _connection?.Close();
    }
}
