namespace Santy.Web.Models;

public class AppSettings
{
    public string DatabasePath { get; set; } = "./santy.db";
    public string SourceName { get; set; } = "icloud";
    public string LocalRootPath { get; set; } = "";
}
