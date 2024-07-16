using System.Security.Cryptography;
using System.Text;

namespace TestTaskMessanger.Utils
{
    public static class Cipner
    {
        public static string Encode(string OpenText)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] data = md5.ComputeHash(Encoding.UTF8.GetBytes(OpenText));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }
    }
}
