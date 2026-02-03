using Santy.Core.Models;

namespace Santy.Web.Models;

public class DiffInfo
{
    public DateTime? LastDiffTime { get; set; }
    public int MissingCount { get; set; }
    public int PresentCount { get; set; }
    public int UncertainCount { get; set; }
    public List<DiffResult> Results { get; set; } = new();
}
