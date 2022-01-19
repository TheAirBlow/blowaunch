using System.IO;
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
        /// Get hash of a file
        /// </summary>
        /// <param name="file">Input file</param>
        /// <returns>Hash digest</returns>
        public static string Hash(string file)
        {
            using var stream = new FileStream(file, FileMode.Open);
            var hash = new SHA1Managed().ComputeHash(stream);
            return string.Concat(hash.Select(b => b.ToString("x2")));
        }
    }
}