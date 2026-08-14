using System.Reflection;

namespace Tracos3DStudio;

public static class AppVersion
{
    public static string DisplayBuildLabel { get; } = ResolveDisplayBuildLabel();

    private static string ResolveDisplayBuildLabel()
    {
        string? info = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(info) || info is "1.0.0" or "1.0.0.0")
            return "desenvolvimento";

        return info;
    }
}
