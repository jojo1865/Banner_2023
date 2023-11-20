using System;
using System.Collections;
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
        /*
        /// <summary>
        /// AES-256-CBC 加密
        /// </summary>
        /// <param name="toEncrypt"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static string Enc_AES256(string toEncrypt, string key, string iv)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] ivArray = UTF8Encoding.UTF8.GetBytes(iv);
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(toEncrypt);

            //進階加密標準（英語：Advanced Encryption Standard，縮寫：AES）
            //https://zh.wikipedia.org/wiki/%E9%AB%98%E7%BA%A7%E5%8A%A0%E5%AF%86%E6%A0%87%E5%87%86

            // RijndaelManaged 類別
            // https://msdn.microsoft.com/zh-tw/library/system.security.cryptography.rijndaelmanaged(v=vs.110).aspx

            //區塊(Block)密碼工作模式（mode of operation）
            //https://zh.wikipedia.org/wiki/%E5%9D%97%E5%AF%86%E7%A0%81%E7%9A%84%E5%B7%A5%E4%BD%9C%E6%A8%A1%E5%BC%8F

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.KeySize = 256;
            rDel.Key = keyArray;
            rDel.IV = ivArray;  // 初始化向量 initialization vector (IV)
            rDel.Mode = CipherMode.CBC; // 密碼分組連結（CBC，Cipher-block chaining）模式
            rDel.Padding = PaddingMode.Zeros;

            ICryptoTransform cTransform = rDel.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }
        /// <summary>
        /// AES-256-CBC 解密
        /// </summary>
        /// <param name="toDecrypt"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static string Des_AES256(string toDecrypt, string key, string iv)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] ivArray = UTF8Encoding.UTF8.GetBytes(iv);
            byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.KeySize = 256;
            rDel.Key = keyArray;
            rDel.IV = ivArray;
            rDel.Mode = CipherMode.CBC;
            rDel.Padding = PaddingMode.Zeros;

            ICryptoTransform cTransform = rDel.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return UTF8Encoding.UTF8.GetString(resultArray);
        }
        //輸入2 Bytes Hex型式的字串，回傳ASCII字串
        public static string HexStr2ASCII(string hex_str)
        {
            string result = "";
            string tmp;

            for (int i = 0; i < hex_str.Length; i += 2)
            {
                tmp = hex_str.Substring(i, 2);
                result += Convert.ToChar(Convert.ToUInt32(tmp, 16));
            }
            return result;
        }
        //輸入ASCII字串 回傳2 Bytes Hex型式的字串
        public static string ASCII2HexStr(string str)
        {
            string result = "";

            for (int i = 0; i < str.Length; i++)
            {
                result += Convert.ToInt32(str[i]).ToString("X");
            }
            return result;
        }

        //輸入ASCII字串 回傳2 Bytes Hex型式的Bytes array
        public static byte[] ASCII2HexByte(string str)
        {
            List<byte> result = new List<byte>();

            for (int i = 0; i < str.Length; i++)
            {
                byte tmp = (byte)str[i];
                result.Add(tmp);
            }
            return result.ToArray();
        }*/

        #endregion
        #region 藍新金流用

        public static string EncryptAESHex(string source, string cryptoKey, string cryptoIV)
        {
            string result = string.Empty;

            if (!string.IsNullOrEmpty(source))
            {
                var encryptValue = EncryptAES(Encoding.UTF8.GetBytes(source), cryptoKey, cryptoIV);

                if (encryptValue != null)
                {
                    result = BitConverter.ToString(encryptValue)?.Replace("-", string.Empty)?.ToLower();
                }
            }

            return result;
        }

        /// <summary>
        /// 字串加密AES
        /// </summary>
        /// <param name="source">加密前字串</param>
        /// <param name="cryptoKey">加密金鑰</param>
        /// <param name="cryptoIV">cryptoIV</param>
        /// <returns>加密後字串</returns>
        public static byte[] EncryptAES(byte[] source, string cryptoKey, string cryptoIV)
        {
            byte[] dataKey = Encoding.UTF8.GetBytes(cryptoKey);
            byte[] dataIV = Encoding.UTF8.GetBytes(cryptoIV);

            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                aes.Key = dataKey;
                aes.IV = dataIV;

                using (var encryptor = aes.CreateEncryptor())
                {
                    return encryptor.TransformFinalBlock(source, 0, source.Length);
                }
            }
        }

        /// <summary>
        /// 字串加密SHA256
        /// </summary>
        /// <param name="source">加密前字串</param>
        /// <returns>加密後字串</returns>
        public static string EncryptSHA256(string source)
        {
            string result = string.Empty;

            using (System.Security.Cryptography.SHA256 algorithm = System.Security.Cryptography.SHA256.Create())
            {
                var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(source));

                if (hash != null)
                {
                    result = BitConverter.ToString(hash)?.Replace("-", string.Empty)?.ToUpper();
                }

            }
            return result;
        }
        /// <summary>
        /// 16 進制字串解密
        /// </summary>
        /// <param name="source">加密前字串</param>
        /// <param name="cryptoKey">加密金鑰</param>
        /// <param name="cryptoIV">cryptoIV</param>
        /// <returns>解密後的字串</returns>
        public static string DecryptAESHex(string source, string cryptoKey, string cryptoIV)
        {
            string result = string.Empty;

            if (!string.IsNullOrEmpty(source))
            {
                // 將 16 進制字串 轉為 byte[] 後
                byte[] sourceBytes = ToByteArray(source);

                if (sourceBytes != null)
                {
                    // 使用金鑰解密後，轉回 加密前 value
                    result = Encoding.UTF8.GetString(DecryptAES(sourceBytes, cryptoKey, cryptoIV)).Trim();
                }
            }

            return result;
        }

        /// <summary>
        /// 將16進位字串轉換為byteArray
        /// </summary>
        /// <param name="source">欲轉換之字串</param>
        /// <returns></returns>
        public static byte[] ToByteArray(string source)
        {
            byte[] result = null;

            if (!string.IsNullOrWhiteSpace(source))
            {
                var outputLength = source.Length / 2;
                var output = new byte[outputLength];

                for (var i = 0; i < outputLength; i++)
                {
                    output[i] = Convert.ToByte(source.Substring(i * 2, 2), 16);
                }
                result = output;
            }

            return result;
        }

        /// <summary>
        /// 字串解密AES
        /// </summary>
        /// <param name="source">解密前字串</param>
        /// <param name="cryptoKey">解密金鑰</param>
        /// <param name="cryptoIV">cryptoIV</param>
        /// <returns>解密後字串</returns>
        public static byte[] DecryptAES(byte[] source, string cryptoKey, string cryptoIV)
        {
            byte[] dataKey = Encoding.UTF8.GetBytes(cryptoKey);
            byte[] dataIV = Encoding.UTF8.GetBytes(cryptoIV);

            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                // 智付通無法直接用PaddingMode.PKCS7，會跳"填補無效，而且無法移除。"
                // 所以改為PaddingMode.None並搭配RemovePKCS7Padding
                aes.Padding = System.Security.Cryptography.PaddingMode.None;
                aes.Key = dataKey;
                aes.IV = dataIV;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] data = decryptor.TransformFinalBlock(source, 0, source.Length);
                    int iLength = data[data.Length - 1];
                    var output = new byte[data.Length - iLength];
                    Buffer.BlockCopy(data, 0, output, 0, output.Length);
                    return output;
                }
            }
        }
        #endregion
    }
}