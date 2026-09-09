# SQL Server Database Setup Script for RentalApp
# This script creates the RentalAppDb database and applies EF Core migrations

param(
	[string]$ServerName = "(local)",
	[string]$DatabaseName = "RentalAppDb",
	[switch]$DropExisting = $false
)

Write-Host "SQL Server Database Setup Script" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Server: $ServerName" -ForegroundColor Yellow
Write-Host "Database: $DatabaseName" -ForegroundColor Yellow
Write-Host ""

# Step 1: Check if database exists and handle accordingly
Write-Host "Step 1: Checking database status..." -ForegroundColor Green

$serverConnectionStringBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$serverConnectionStringBuilder.DataSource = $ServerName
$serverConnectionStringBuilder.IntegratedSecurity = $true
$serverConnectionStringBuilder.Encrypt = $false
$serverConnectionStringBuilder.TrustServerCertificate = $true
$connectionString = $serverConnectionStringBuilder.ConnectionString

try {
	$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
	$connection.Open()

	$command = New-Object System.Data.SqlClient.SqlCommand
	$command.Connection = $connection
	$command.CommandText = "SELECT COUNT(*) FROM sys.databases WHERE name = @databaseName"
	$null = $command.Parameters.Add("@databaseName", [System.Data.SqlDbType]::NVarChar, 128)
	$command.Parameters["@databaseName"].Value = $DatabaseName

	$dbExists = $command.ExecuteScalar()
	$connection.Close()

	if ($dbExists -gt 0) {
		Write-Host "✓ Database '$DatabaseName' already exists" -ForegroundColor Yellow

		if ($DropExisting) {
			Write-Host "  Dropping existing database..." -ForegroundColor Yellow
			$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
			$connection.Open()
			$command = New-Object System.Data.SqlClient.SqlCommand
			$command.Connection = $connection
			$command.CommandText = "ALTER DATABASE [$DatabaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$DatabaseName]"
			$command.ExecuteNonQuery()
			$connection.Close()
			Write-Host "✓ Database dropped successfully" -ForegroundColor Green
		} else {
			Write-Host "  Use -DropExisting flag to recreate the database" -ForegroundColor Cyan
		}
	} else {
		Write-Host "✓ Database '$DatabaseName' does not exist" -ForegroundColor Green
	}
} catch {
	Write-Host "✗ Error: Cannot connect to SQL Server at $ServerName" -ForegroundColor Red
	Write-Host "  Make sure SQL Server is running and you have the proper permissions" -ForegroundColor Red
	exit 1
}

# Step 2: Apply EF Core migrations
Write-Host ""
Write-Host "Step 2: Applying EF Core migrations..." -ForegroundColor Green

$databaseConnectionStringBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
$databaseConnectionStringBuilder.InitialCatalog = $DatabaseName
$databaseConnectionString = $databaseConnectionStringBuilder.ConnectionString

try {
	$previousEnvironment = $env:ASPNETCORE_ENVIRONMENT
	$previousConnection = $env:ConnectionStrings__DefaultConnection
	$env:ASPNETCORE_ENVIRONMENT = "Development"
	$env:ConnectionStrings__DefaultConnection = $databaseConnectionString

	# This will create the specified database if it doesn't exist and apply all migrations
	dotnet ef database update --project RentalApp.csproj

	if ($LASTEXITCODE -ne 0) {
		Write-Host "✗ Migration failed" -ForegroundColor Red
		exit 1
	}

	Write-Host "✓ Migrations applied successfully" -ForegroundColor Green
} catch {
	Write-Host "✗ Error during migration: $_" -ForegroundColor Red
	exit 1
} finally {
	if ($null -ne $previousEnvironment) {
		$env:ASPNETCORE_ENVIRONMENT = $previousEnvironment
	} else {
		Remove-Item Env:ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
	}

	if ($null -ne $previousConnection) {
		$env:ConnectionStrings__DefaultConnection = $previousConnection
	} else {
		Remove-Item Env:ConnectionStrings__DefaultConnection -ErrorAction SilentlyContinue
	}
}

# Step 3: Verify database and tables
Write-Host ""
Write-Host "Step 3: Verifying database schema..." -ForegroundColor Green

try {
	$verificationConnectionStringBuilder = New-Object System.Data.SqlClient.SqlConnectionStringBuilder $connectionString
	$verificationConnectionStringBuilder.InitialCatalog = $DatabaseName
	$connection = New-Object System.Data.SqlClient.SqlConnection $verificationConnectionStringBuilder.ConnectionString
	$connection.Open()

	$command = New-Object System.Data.SqlClient.SqlCommand
	$command.Connection = $connection
	$command.CommandText = "SELECT COUNT(*) FROM sys.tables"

	$tableCount = $command.ExecuteScalar()
	$connection.Close()

	Write-Host "✓ Database contains $tableCount tables" -ForegroundColor Green
} catch {
	Write-Host "✗ Error verifying database: $_" -ForegroundColor Red
	exit 1
}

Write-Host ""
Write-Host "Setup Complete!" -ForegroundColor Cyan
Write-Host "Your SQL Server database '$DatabaseName' is ready for use." -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Configure the production ConnectionStrings__DefaultConnection value outside source control"
Write-Host "2. Test the application with 'dotnet run'"
Write-Host "3. Verify the target database schema and data"
