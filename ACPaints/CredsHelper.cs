using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ACPaints
{
    class CredsHelper
    {
        private static byte[] s_salt = { 241, 10, 67, 118, 46 };

        public static string EncryptPassword(string password)
        {
            var passBytes = Encoding.UTF8.GetBytes(password);
            passBytes = Protect(passBytes);
            return Convert.ToBase64String(passBytes);
        }

        public static string DecryptPassword(string encrypted)
        {
            var passBytes = Convert.FromBase64String(encrypted);
            passBytes = Unprotect(passBytes);
            return Encoding.UTF8.GetString(passBytes);
        }

        private static byte[] Protect(byte[] data)
        {
            try
            {
                // Encrypt the data using DataProtectionScope.CurrentUser. The result can be decrypted
                // only by the same current user.
                return ProtectedData.Protect(data, s_salt, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not encrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

        private static byte[] Unprotect(byte[] data)
        {
            try
            {
                //Decrypt the data using DataProtectionScope.CurrentUser.
                return ProtectedData.Unprotect(data, s_salt, DataProtectionScope.CurrentUser);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Data was not decrypted. An error occurred.");
                Console.WriteLine(e.ToString());
                return null;
            }
        }

    }
}
