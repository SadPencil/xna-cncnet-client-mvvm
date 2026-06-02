using Microsoft.Extensions.DependencyInjection;

namespace AvMainClientView;

internal static class ViewConstants
{
    // It is an anti-pattern of dependency injection to store the ServiceProvider variable, but we tolerate such a usage for now.
    public static ServiceProvider ServiceProvider { get; set; } = null!;
}
