namespace FinDistill.Web.Models;

/// <summary>
/// View model for the error page. Populated by the error action in <c>HomeController</c>.
/// </summary>
public class ErrorViewModel
{
    /// <summary>Current request trace identifier for correlation with logs.</summary>
    public string? RequestId { get; set; }

    /// <summary>Returns true when <see cref="RequestId"/> should be displayed to the user.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
