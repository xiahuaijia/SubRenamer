using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubRenamer
{
    internal static class Utils
    {
        public static int TestSimilarity(string texta, string textb)
        {
            texta = texta.ToLower();
            textb = textb.ToLower();
            if (texta.Length > textb.Length)
            {

                var temp = texta;
                texta = textb;
                textb = temp;
            }
            var data = new int[texta.Length, textb.Length];
            for (var i = 0; i < texta.Length; i++)
            {
                if (texta[i] == textb[0])
                {
                    data[i, 0] = 1;
                }
                else
                {
                    data[i, 0] = 0;
                }
            }
            for (var i = 0; i < texta.Length; i++)
            {
                for (var j = 1; j < textb.Length; j++)
                {
                    if ((i + data[i, j - 1]) < texta.Length && texta[i + data[i, j - 1]] == textb[j])
                    {
                        data[i, j] = data[i, j - 1] + 1;
                    }
                }
            }
            var maxScore = 0;
            for (var i = 0; i < texta.Length; i++)
            {
                for (var j = 1; j < textb.Length; j++)
                {
                    maxScore = Math.Max(data[i, j], maxScore);
                }
            }
            var score = Convert.ToSingle(maxScore);
            return Math.Abs(score) < 0.01 ? 0 : Convert.ToInt32((score / texta.Length) * 100);
        }

        /// <summary>
        /// 根据字节流判断是否UTF8
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public static Encoding GetBytesEncoding(byte[] bs)
        {
            int len = bs.Length;
            if (len >= 3 && bs[0] == 0xEF && bs[1] == 0xBB && bs[2] == 0xBF)
            {
                return Encoding.UTF8;
            }
            int[] cs = { 7, 5, 4, 3, 2, 1, 0, 6, 14, 30, 62, 126 };
            for (int i = 0; i < len; i++)
            {
                int bits = -1;
                for (int j = 0; j < 6; j++)
                {
                    if (bs[i] >> cs[j] == cs[j + 6])
                    {
                        bits = j;
                        break;
                    }
                }
                if (bits == -1)
                {
                    return Encoding.Default;
                }
                while (bits-- > 0)
                {
                    i++;
                    if (i == len || bs[i] >> 6 != 2)
                    {
                        return Encoding.Default;
                    }
                }
            }
            return Encoding.UTF8;
        }

        /// <summary>
        /// 根据字节流判断是否UTF8
        /// </summary>
        /// <param name="bs"></param>
        /// <returns></returns>
        public static Encoding GetBytesEncoding(string filename)
        {
            byte[] bs = File.ReadAllBytes(filename);
            return GetBytesEncoding(bs);
        }


        /// <summary>
        /// 读取文件，并返回转换后的UTF8格式数据
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string ReadAllFormatText(string filename)
        {
            byte[] bs = File.ReadAllBytes(filename);
            int len = bs.Length;
            if (len >= 3 && bs[0] == 0xEF && bs[1] == 0xBB && bs[2] == 0xBF)
            {
                return Encoding.UTF8.GetString(bs, 3, len - 3);
            }
            int[] cs = { 7, 5, 4, 3, 2, 1, 0, 6, 14, 30, 62, 126 };
            for (int i = 0; i < len; i++)
            {
                int bits = -1;
                for (int j = 0; j < 6; j++)
                {
                    if (bs[i] >> cs[j] == cs[j + 6])
                    {
                        bits = j;
                        break;
                    }
                }
                if (bits == -1)
                {
                    return Encoding.Default.GetString(bs);
                }
                while (bits-- > 0)
                {
                    i++;
                    if (i == len || bs[i] >> 6 != 2)
                    {
                        return Encoding.Default.GetString(bs);
                    }
                }
            }
            return Encoding.UTF8.GetString(bs);
        }
    }
}
