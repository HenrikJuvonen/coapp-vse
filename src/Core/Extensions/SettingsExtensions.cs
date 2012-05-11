using System;
using System.Security.Cryptography;
using System.Text;

namespace CoGet
{
    public static class SettingsExtensions
    {
        private static byte[] StringToBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        private static string BytesToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
