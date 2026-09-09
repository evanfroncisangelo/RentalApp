namespace RentalApp.Infrastructure.Configuration;

public class ApplicationDataOptions
{
    public const string SectionName = "ApplicationData";

    public string? RootPath { get; set; }
    public string? DatabasePath { get; set; }
    public string? UploadsPath { get; set; }
    public string? BackupsPath { get; set; }
}
