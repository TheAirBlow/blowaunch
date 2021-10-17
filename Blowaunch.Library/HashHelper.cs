using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Blowaunch.Library
{
    /// <summary>
    /// SHA1 Hash Helper
    /// </summary>
    public static class HashHelper
    {
        /// <summary>
        /// Get hash of buffer
        /// </summary>
        /// <param name="input">Input</param>
        /// <returns>Hash digest</returns>
        public static string Hash(byte[] input)
        {
            var hash = new SHA1Managed().ComputeHash(input);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}