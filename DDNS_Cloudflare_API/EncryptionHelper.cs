using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace DDNS_Cloudflare_API
{
    public static class EncryptionHelper
    {
        // Encrypt a string and return it in Base-64 URL format
        public static string EncryptString(string plainText)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(plainText);
                byte[] encrypted = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
                string base64 = Convert.ToBase64String(encrypted);
                return Base64UrlEncode(base64);
            }
            catch (Exception ex)
            {
                // Log or handle encryption error
                Debug.WriteLine($"Encryption Error: {ex.Message}");
                throw;
            }
        }

        // Decrypt a Base-64 URL encoded string
        public static string DecryptString(string encryptedText)
        {
            try
            {
                string base64 = Base64UrlDecode(encryptedText);
                byte[] data = Convert.FromBase64String(base64);
                byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                // Log or handle decryption error
                Debug.WriteLine($"Decryption Error: {ex.Message}");
                throw;
            }
        }

        // Encode a Base-64 string to Base-64 URL format
        private static string Base64UrlEncode(string base64)
        {
            return base64
                .Replace('+', '-') // Base-64 to Base-64 URL
                .Replace('/', '_')
                .TrimEnd('='); // Remove padding
        }

        // Decode a Base-64 URL encoded string to standard Base-64 format
        private static string Base64UrlDecode(string base64Url)
        {
            string base64 = base64Url
                .Replace('-', '+') // Base-64 URL to Base-64
                .Replace('_', '/');

            // Add padding
            int padding = base64.Length % 4;
            if (padding > 0)
            {
                base64 = base64.PadRight(base64.Length + (4 - padding), '=');
            }

            return base64;
        }
    }
}
