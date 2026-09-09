using Microsoft.Data.SqlClient;

namespace RentalApp.Infrastructure.Configuration;

public static class ApplicationDataPathHelper
{
    public static string ResolvePath(string path, string contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(contentRootPath, path));
    }

    public static SqlConnectionStringBuilder GetSqlServerConnectionStringBuilder(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string is required.");
        }

        return new SqlConnectionStringBuilder(connectionString);
    }

    public static string GetSqlServerDatabaseName(string? connectionString)
    {
        var builder = GetSqlServerConnectionStringBuilder(connectionString);
        var databaseName = string.IsNullOrWhiteSpace(builder.InitialCatalog)
            ? builder["Database"]?.ToString()
            : builder.InitialCatalog;

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("DefaultConnection must include a SQL Server database name.");
        }

        return databaseName;
    }

    public static string BuildSqlServerConnectionString(string? connectionString, string initialCatalog)
    {
        if (string.IsNullOrWhiteSpace(initialCatalog))
        {
            throw new InvalidOperationException("A SQL Server database name is required.");
        }

        var builder = GetSqlServerConnectionStringBuilder(connectionString);
        builder.InitialCatalog = initialCatalog;
        return builder.ConnectionString;
    }

    public static bool IsPathInsideDirectory(string candidatePath, string directoryPath)
    {
        var candidateFull = Path.GetFullPath(candidatePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var directoryFull = Path.GetFullPath(directoryPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var relative = Path.GetRelativePath(directoryFull, candidateFull);
        return !relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative);
    }
}
