namespace Blowaunch.Library.Authentication;

/// <summary>
/// Blowaunch Authentication Server endpoints
/// </summary>
public static class Endpoints
{
    public static string BlowaunchServer = "https://blowaunch-server.herokuapp.com";
    public static string MinecraftServer = "https://api.minecraftservices.com/";
    public static string XboxAuthServer = "https://user.auth.xboxlive.com/";
    public static string XboxXstsServer = "https://xsts.auth.xboxlive.com/";
    public static string MojangServer = "https://authserver.mojang.com";
    public static string MojangApi = "https://api.mojang.com";

    /// <summary>
    /// Mojang endpoints
    /// </summary>
    public static class Mojang
    {
        public static readonly string Login = "/authenticate";
        public static readonly string Refresh = "/refresh";
        public static readonly string Validate = "/validate";
        public static readonly string Invalidate = "/invalidate";
    }

    /// <summary>
    /// Minecraft endpoints
    /// </summary>
    public static class Microsoft
    {
        public static readonly string LoginBrowser = "microsoft/login";
        public static readonly string XboxLiveAuth = "user/authenticate";
        public static readonly string XboxXstsAuth = "xsts/authorize";
        public static readonly string MinecraftAuth = "authentication/login_with_xbox";
        public static readonly string MinecraftProfile = "minecraft/profile";
        public static readonly string Refresh = "microsoft/refresh?token={0}";
    }

    /// <summary>
    /// Security questions
    /// </summary>
    public static class Security
    {
        public static readonly string Location = "user/security/location";
        public static readonly string Challenges = "user/security/challenges";
    }
}