using System;
using System.Text;

namespace SFTPReaderQueue.Helper
{
    public static class Base64Helper
    {
        public static string EncodeToBase64(string plainText)
        {
            try
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(plainTextBytes);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during encoding
                throw new Exception("Error encoding text to Base64.", ex);
            }
        }

        public static string DecodeFromBase64(string base64EncodedText)
        {
            try
            {
                byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedText);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during decoding
                throw new Exception("Error decoding text from Base64.", ex);
            }
        }
    }
}
