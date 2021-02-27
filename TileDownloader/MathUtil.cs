using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileDownloader
{
    internal static class MathUtil
    {
        /// <summary>
        /// 弧度转角度
        /// </summary>
        public const double Rad2Deg = 180.0 / Math.PI;

        /// <summary>
        /// 角度转弧度
        /// </summary>
        public const double Deg2Rad = Math.PI / 180.0;

        /// <summary>
        /// 将 <paramref name="n"/>限制在范围 [<paramref name="minValue"/>,<paramref name="maxValue"/>] 之间
        /// </summary>
        /// <param name="n"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static double Clamp(double n, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(n, minValue), maxValue);
        }

        public static float Clamp(float n, float minValue, float maxValue)
        {
            return Math.Min(Math.Max(n, minValue), maxValue);
        }

        public static int Clamp(int val, int min, int max)
        {
            return Math.Min(Math.Max(val, min), max);
        }

        /// <summary>
        /// 将给定的<paramref name="val"/>循环限定在左闭右开的范围内
        /// 比如范围 [0,10)，10 变成 0，11 变成 1
        /// </summary>
        /// <param name="val"></param>
        /// <param name="min">最小值（包含）</param>
        /// <param name="max">最大值（不包含）</param>
        /// <returns>The wrapped value</returns>
        public static double Wrap(double val, double min, double max)
        {
            return NonNegativeMod(val - min, max - min) + min;
        }

        public static int Wrap(int val, int min, int max)
        {
            return NonNegativeMod(val - min, max - min) + min;
        }

        /// <summary>
        /// Returns the non-negative remainder of x / m.
        /// </summary>
        public static double NonNegativeMod(double x, double m)
        {
            return (x % m + m) % m;
        }

        public static int NonNegativeMod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }
}
