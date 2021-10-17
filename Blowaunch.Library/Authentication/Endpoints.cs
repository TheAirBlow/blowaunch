namespace Blowaunch.Library.Authentication
{
    /// <summary>
    /// Blowaunch Authentication Server endpoints
    /// </summary>
    public static class Endpoints
    {
        /// <summary>
        /// Hosted Blowaunch Authentication Server
        /// </summary>
        private static string Server = "https://blowaunch-server.herokuapp.com";

        /// <summary>
        /// Mojang endpoints
        /// </summary>
        public static class Mojang
        {
            public static readonly string Login = Server + "/mojang/login?username={0}&password={1}";
            public static readonly string Refresh = Server + "/mojang/refresh?token={0}&id={1}&name={2}";
            public static readonly string Validate = Server + "/mojang/validate?token={0}";
            public static readonly string Invalidate = Server + "/mojang/invalidate?token={0}";
        }

        /// <summary>
        /// Minecraft endpoints
        /// </summary>
        public static class Microsoft
        {
            public static readonly string LoginBrowser = Server + "/microsoft/login";
            public static readonly string Refresh = Server + "/microsoft/refresh?token={0}";
            public static readonly string XboxLogin = Server + "/xbox/login?token={0}";
            public static readonly string XboxXsts = Server + "/xbox/xsts?userhash={1}&token={0}";
            public static readonly string MinecraftLogin = Server + "/minecraft/login?userhash={1}&token={0}";
            public static readonly string Ownership = Server + "/minecraft/ownership?token={0}";
            public static readonly string Profile = Server + "/minecraft/profile?token={0}";
        }
    }
}