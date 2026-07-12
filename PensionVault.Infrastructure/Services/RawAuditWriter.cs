using Microsoft.Data.SqlClient;

namespace PensionVault.Infrastructure.Services;

/// <summary>
/// Writes audit log entries directly via ADO.NET raw SQL.
/// Used by non-Members microservices to avoid EF model conflicts
/// when writing to the central PensionVaultDb_Members database.
/// </summary>
public interface IRawAuditWriter
{
    Task WriteAsync(Guid userId, string action, string entityType, string? recordId);
}

public class RawAuditWriter : IRawAuditWriter
{
    private readonly string _connectionString;

    public RawAuditWriter(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task WriteAsync(Guid userId, string action, string entityType, string? recordId)
    {
        try
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO AuditLogs (AuditId, UserId, Action, EntityType, RecordId, Timestamp)
                VALUES (@AuditId, @UserId, @Action, @EntityType, @RecordId, @Timestamp)";

            cmd.Parameters.AddWithValue("@AuditId",    Guid.NewGuid());
            cmd.Parameters.AddWithValue("@UserId",     userId);
            cmd.Parameters.AddWithValue("@Action",     action.Length > 100 ? action[..100] : action);
            cmd.Parameters.AddWithValue("@EntityType", entityType.Length > 100 ? entityType[..100] : entityType);
            cmd.Parameters.AddWithValue("@RecordId",   (object?)recordId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Timestamp",  DateTime.UtcNow);

            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Audit logging must NEVER break the main request
        }
    }
}
