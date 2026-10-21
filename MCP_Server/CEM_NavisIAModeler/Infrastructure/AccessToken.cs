// Cemengal: the AI Modeler's local access token (docs/decisions/0009).
//
// The server listens on loopback only and refuses browsers (0005), but any program on the machine,
// another user's session included, could still call /rpc; with send_code_to_navisworks that means
// running code as the user. At each start the server writes a new random token to the user's own
// settings folder (CemUserHome, under %APPDATA%, which other users cannot read) and refuses every
// request but the health check without "Authorization: Bearer <token>". The Node client reads the
// same file for every request (MCP_Client/server/navis-rpc.js).
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CEM_CSharpExtended.Hosting;
using CEM_NavisIAModeler_Shared;

namespace CEM_NavisIAModeler.Infrastructure
{
    public static class AccessToken
    {
        /// <summary>The token file's name in the user's Navisworks settings folder; the client's defaultTokenFile agrees.</summary>
        public const string FileName = "CEM_NavisIAModeler.token";

        private const string Scheme = "Bearer ";

        /// <summary>Creates a new token and writes it to the token file. Throws if it cannot be written.</summary>
        public static string Issue()
        {
            var bytes = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(bytes);
            var token = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

            File.WriteAllText(CemUserHome.NavisworksSettingsFile(FileName), token, new UTF8Encoding(false));
            LogHelper.LogEvent("[TOKEN] issued");
            return token;
        }

        /// <summary>Deletes the token file if it still holds <paramref name="token"/> (another start may have replaced it).</summary>
        public static void Revoke(string token)
        {
            if (string.IsNullOrEmpty(token)) return;
            try
            {
                var path = CemUserHome.NavisworksSettingsFile(FileName);
                if (File.Exists(path) && File.ReadAllText(path).Trim() == token)
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                LogHelper.LogWarning($"[TOKEN] could not delete the token file: {ex.Message}");
            }
        }

        /// <summary>True when the Authorization header carries <paramref name="token"/>.</summary>
        public static bool IsAuthorized(string authorizationHeader, string token)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(authorizationHeader)) return false;
            if (!authorizationHeader.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase)) return false;
            return FixedTimeEquals(authorizationHeader.Substring(Scheme.Length).Trim(), token);
        }

        /// <summary>Compares in a time that does not depend on where the strings first differ.</summary>
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a.Length != b.Length) return false;
            var diff = 0;
            for (var i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
