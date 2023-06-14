using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Banner
{
    public class HSM
    {
        private static string sAnswer;
        private static string sKey1 = "3EDFA850";
        private static string sKey2 = "BF73EADA";
        private static string sKey3 = "8EF0C91E";
        private static string sIv = "D3A470DC";
        public HSM()
        {
        }
        private static int ReNew()
        {
            return 0;
        }
        /// <summary>
        /// 單層加密
        /// </summary>
        /// <param name="sInput">輸入字串(限英數)</param>
        /// <returns></returns>
        public static string Enc_1(string sInput)
        {
            ReNew();
            return EncryptDES(sInput, sKey1, sIv);
        }
        /// <summary>
        /// 單層解密
        /// </summary>
        /// <param name="sInput">輸入字串(限英數)</param>
        /// <returns>輸出</returns>
        public static string Des_1(string sInput)
        {
            ReNew();
            return DecryptDES(sInput, sKey1, sIv);
        }
        /// <summary>
        /// 3層加密
        /// </summary>
        /// <param name="sInput">輸入字串(限英數)</param>
        /// <returns>輸出</returns>
        public static string Enc_3(string sInput)
        {
            ReNew();
            sAnswer = EncryptDES(sInput, sKey3, sIv);
            sAnswer = EncryptDES(sAnswer, sKey2, sIv);
            sAnswer = EncryptDES(sAnswer, sKey1, sIv);
            return sAnswer;
        }
        /// <summary>
        /// 3層解密
        /// </summary>
        /// <param name="sInput">輸入字串(限英數)</param>
        /// <returns>輸出</returns>
        public static string Des_3(string sInput)
        {
            ReNew();
            sAnswer = DecryptDES(sInput, sKey1, sIv);
            sAnswer = DecryptDES(sAnswer, sKey2, sIv);
            sAnswer = DecryptDES(sAnswer, sKey3, sIv);
            return sAnswer;
        }
        /// <summary>
        /// 雜湊加密(無法解密)
        /// </summary>
        /// <param name="sInput">輸入字串(限英數)</param>
        /// <returns>輸出</returns>
        public static string Hash(string sInput)
        {
            ReNew();
            sAnswer = GetMD5(sInput);
            return sAnswer;
        }
        /// <summary>
        /// MD5加密,不可解密,已能破解不建議使用
        /// </summary>
        /// <param name="sInput">輸入字串</param>
        /// <returns>輸出字串</returns>
        public static string DoMD5(string sInput)
        {
            MD5 md5Hash = MD5.Create();
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(sInput));

            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }
        /// <summary>
        /// SHA-1加密,不可解密,已能破解不建議使用
        /// </summary>
        /// <param name="sInput">輸入字串</param>
        /// <returns>輸出字串</returns>
        public static string DoSHA1(string sInput)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();//建立一個SHA1
            byte[] source = Encoding.Default.GetBytes(sInput);//將字串轉為Byte[]
            byte[] crypto = sha1.ComputeHash(source);//進行SHA1加密
            return Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串
        }
        /// <summary>
        /// SHA-256加密
        /// </summary>
        /// <param name="sInput">輸入字串</param>
        /// <returns>輸出字串</returns>
        public static string DoSHA256(string sInput)
        {
            SHA256 sha256 = new SHA256CryptoServiceProvider();//建立一個SHA256
            byte[] source = Encoding.Default.GetBytes(sInput);//將字串轉為Byte[]
            byte[] crypto = sha256.ComputeHash(source);//進行SHA256加密
            return Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串
        }
        /// <summary>
        /// SHA-384加密
        /// </summary>
        /// <param name="sInput">輸入字串</param>
        /// <returns>輸出字串</returns>
        public static string DoSHA384(string sInput)
        {
            SHA384 sha384 = new SHA384CryptoServiceProvider();//建立一個SHA384
            byte[] source = Encoding.Default.GetBytes(sInput);//將字串轉為Byte[]
            byte[] crypto = sha384.ComputeHash(source);//進行SHA384加密
            return Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串
        }
        /// <summary>
        /// SHA-512加密
        /// </summary>
        /// <param name="sInput">輸入字串</param>
        /// <returns>輸出字串</returns>
        public static string DoSHA512(string sInput)
        {
            SHA512 sha512 = new SHA512CryptoServiceProvider();//建立一個SHA512
            byte[] source = Encoding.Default.GetBytes(sInput);//將字串轉為Byte[]
            byte[] crypto = sha512.ComputeHash(source);//進行SHA512加密
            return Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串
        }

        #region 加解密底層處理

        /// <summary>   
        /// DES 加密字串   
        /// </summary>   
        /// <param name="original">原始字串</param>   
        /// <param name="key">Key，長度必須為 8 個 ASCII 字元</param>   
        /// <param name="iv">IV，長度必須為 8 個 ASCII 字元</param>   
        /// <returns></returns>   
        private static string EncryptDES(string original, string key, string iv)
        {
            try
            {
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                des.Key = Encoding.ASCII.GetBytes(key);
                des.IV = Encoding.ASCII.GetBytes(iv);
                byte[] s = Encoding.ASCII.GetBytes(original);
                ICryptoTransform desencrypt = des.CreateEncryptor();
                return BitConverter.ToString(desencrypt.TransformFinalBlock(s, 0, s.Length)).Replace("-", string.Empty);
            }
            catch { return original; }
        }

        /// <summary>   
        /// DES 解密字串   
        /// </summary>   
        /// <param name="hexString">加密後 Hex String</param>   
        /// <param name="key">Key，長度必須為 8 個 ASCII 字元</param>   
        /// <param name="iv">IV，長度必須為 8 個 ASCII 字元</param>   
        /// <returns></returns>   
        private static string DecryptDES(string hexString, string key, string iv)
        {
            try
            {
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                des.Key = Encoding.ASCII.GetBytes(key);
                des.IV = Encoding.ASCII.GetBytes(iv);

                byte[] s = new byte[hexString.Length / 2];
                int j = 0;
                for (int i = 0; i < hexString.Length / 2; i++)
                {
                    s[i] = Byte.Parse(hexString[j].ToString() + hexString[j + 1].ToString(), System.Globalization.NumberStyles.HexNumber);
                    j += 2;
                }
                ICryptoTransform desencrypt = des.CreateDecryptor();
                return Encoding.ASCII.GetString(desencrypt.TransformFinalBlock(s, 0, s.Length));
            }
            catch { return hexString; }
        }

        /// <summary>   
        /// 取得 MD5 編碼後的 Hex 字串   
        /// 加密後為 32 Bytes Hex String (16 Byte)   
        /// </summary>   
        /// <param name="original">原始字串</param>   
        /// <returns></returns>   
        private static string GetMD5(string original)
        {
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] b = md5.ComputeHash(Encoding.UTF8.GetBytes(original));
            return BitConverter.ToString(b).Replace("-", string.Empty);
        }

        #endregion
    }
}