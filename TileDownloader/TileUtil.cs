using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TileDownloader
{
    /// <summary>
    /// 地球信息
    /// </summary>
    public class Earth
    {
        /// <summary>
        /// 半径
        /// </summary>
        public const int RADIUS = 6378137;
    }

    /*
     * 切片空间坐标系，用于计算切片关系的有限离散辅助坐标系
     * 原点：地图左上角为原点（0，0）
     * X轴：水平向右（自西向东）
     * Y轴：垂直向下（自北向南）
     * 单位：每个切片为 1 单位（即 256 pixel）
     */
    [Serializable]
    public struct TileKey : IEquatable<TileKey>
    {
        public int X;
        public int Y;
        public int Lod;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="lod"></param>
        /// <param name="normalize">如果为<c>true</c>，x 轴将会循环限定(水平方向可以无限循环滚动)，y轴将会限定范围</param>
        public TileKey(int x, int y, int lod, bool normalize = true)
        {
            this.X = x;
            this.Y = y;
            this.Lod = lod;
            if (normalize)
            {
                // x 可以进行循环
                this.X = MathUtil.Wrap(X, MinValue(lod), Extent(lod));

                // y 必须进行限定，为什么 y 不能循环？因为 x 表示的是纬度，它的最大最小值是首尾相接的，而经度则不是
                this.Y = MathUtil.Clamp(Y, MinValue(lod), MaxValue(lod));
            }
        }

        public int WestBound
        {
            get
            {
                return X;
            }
        }

        public int NorthBound
        {
            get
            {
                return Y;
            }
        }

        public int EastBound
        {
            get
            {
                return MathUtil.Wrap(X + 1, MinValue(Lod), Extent(Lod));
            }
        }

        public int SouthBound
        {
            get
            {
                return Y + 1;
            }
        }

        /// <summary>
        /// 当前实例是否为 <paramref name="child"/> 的祖先级
        /// </summary>
        /// <param name="child"></param>
        /// <returns></returns>
        public bool IsAncestorOf(TileKey child)
        {
            if (Lod > child.Lod)
            {
                return false;
            }

            // 将两者转换到同一 Lod 层级
            int xWithSameLod = child.X >> child.Lod - Lod;
            int yWithSameLod = child.Y >> child.Lod - Lod;

            return xWithSameLod == X && yWithSameLod == Y;
        }

        /// <summary>
        /// 获取当前实例指定<paramref name="lod"/>的祖先级
        /// </summary>
        /// <param name="lod"></param>
        /// <returns></returns>
        public TileKey GetAncestorAtLod(int lod)
        {
            int num = this.Lod - lod;
            if (num < 0)
            {
                throw new ArgumentException(this.Lod + " < " + lod);
            }
            return new TileKey(X >> num, Y >> num, lod);
        }

        /// <summary>
        /// 获取上一级切片
        /// </summary>
        /// <returns></returns>
        public TileKey GetParent()
        {
            return GetAncestorAtLod(this.Lod - 1);
        }

        /// <summary>
        /// 根据指定的偏移值，获取切片信息
        /// 如果偏移值不合法，将会返回<c>false</c>
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="xOffset"></param>
        /// <param name="yOffset"></param>
        /// <param name="normalize"></param>
        /// <returns></returns>
        public bool Offset(out TileKey offset, int xOffset, int yOffset, bool normalize = true)
        {
            int min = MinValue(this.Lod);
            int max = MaxValue(this.Lod);
            int newX = X + xOffset;
            int newY = Y + yOffset;
            bool result = (normalize || (newX >= min && newX <= max)) && newY >= min && newY <= max;
            offset = new TileKey(X + xOffset, Y + yOffset, Lod, normalize);
            return result;
        }

        /// <summary>
        /// 获取西邻的切片( x - 1 )
        /// </summary>
        /// <param name="west"></param>
        /// <param name="normalize"></param>
        /// <returns></returns>
        public bool West(out TileKey west, bool normalize = true)
        {
            return Offset(out west, -1, 0, normalize);
        }

        /// <summary>
        /// 获取东邻的切片 ( x + 1 )
        /// </summary>
        /// <param name="east"></param>
        /// <param name="normalize"></param>
        /// <returns></returns>
        public bool East(out TileKey east, bool normalize = true)
        {
            return Offset(out east, 1, 0, normalize);
        }

        /// <summary>
        /// 获取北邻的切片 （ y - 1 ）
        /// </summary>
        /// <param name="north"></param>
        /// <param name="normalize"></param>
        /// <returns></returns>
        public bool North(out TileKey north, bool normalize = true)
        {
            return Offset(out north, 0, -1, normalize);
        }

        /// <summary>
        /// 获取南邻的切片 ( y + 1 )
        /// </summary>
        /// <param name="south"></param>
        /// <param name="normalize"></param>
        /// <returns></returns>
        public bool South(out TileKey south, bool normalize = true)
        {
            return Offset(out south, 0, 1, normalize);
        }

        /// <summary>
        /// 获取邻居切片
        /// 返回邻居数组，存储顺序如下，
        /// 
        ///     +-------->
        ///     |
        ///     |               北
        ///     |  +--------+--------+--------+
        ///     |  |        |        |        |
        ///     v  |        |        |        |
        ///        |   0    |   3    |   5    |
        ///        |        |        |        |
        ///        +--------------------------+
        ///        |        |        |        |
        ///        |   1    |        |   6    |
        ///      西|        |        |        |东
        ///        |        |        |        |
        ///        +--------------------------+
        ///        |        |        |        |
        ///        |   2    |   4    |   7    |
        ///        |        |        |        |
        ///        |        |        |        |
        ///        +--------+--------+--------+
        ///                      南
        ///                      
        /// </summary>
        /// <param name="normalize"></param>
        /// <returns></returns>
        public TileKey[] Neighbors(bool normalize = true)
        {
            int neighborCount = 8;
            int min = MinValue(Lod);
            int max = MaxValue(Lod);
            bool isXMinOrMax = X == min || X == max;
            bool isYMinOrMax = Y == min || Y == max;
            neighborCount = ((isXMinOrMax && isYMinOrMax) ? ((!normalize) ? 3 : 5) : (isXMinOrMax ? ((!normalize) ? 5 : 8) : ((!isYMinOrMax) ? 8 : 5)));
            int index = 0;
            TileKey[] array = new TileKey[neighborCount];
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if ((i != 0 || j != 0) && Offset(out TileKey offset, i, j, normalize))
                    {
                        array[index] = offset;
                        index++;
                    }
                }
            }
            return array;
        }

        /// <summary>
        /// 获取指定<paramref name="lod"/>的 x 和 y 轴方向的瓦片数量
        /// </summary>
        /// <param name="lod"></param>
        /// <returns></returns>
        public static int Extent(int lod)
        {
            return 1 << lod;
        }

        /// <summary>
        /// 切片空间的最小值
        /// </summary>
        /// <param name="lod"></param>
        /// <returns></returns>
        public static int MinValue(int lod)
        {
            return 0;
        }

        /// <summary>
        /// 切片空间的最大值
        /// </summary>
        /// <param name="lod"></param>
        /// <returns></returns>
        public static int MaxValue(int lod)
        {
            return Extent(lod) - 1;
        }

        /// <summary>
        /// 获取指定 <paramref name="lod"/> 的瓦片宽度（像素值 pixel）
        /// 因为 1 级地图大小是 512 * 512(pixels)，每提升一级，都会增大 2 倍，因此对于任意级别的地图的大小为 256 * 2^lod 等价于 256 << lod
        /// </summary>
        /// <param name="lod"></param>
        /// <returns></returns>
        public static uint MapSize(int lod)
        {
            return (uint)256 << lod;
        }

        /// <summary>
        /// 赤道线上切换的大小尺寸（米）
        /// </summary>
        /// <param name="lod"></param>
        /// <returns></returns>
        public static double TileSize(int lod)
        {
            return 2.0 * Math.PI * Earth.RADIUS / (Extent(lod));
        }

        public static double TileSize(int lod, double radius)
        {
            return 2.0 * Math.PI * radius / Extent(lod);
        }

        public static List<TileKey> GetChildTileCoords(TileKey tileCoord)
        {
            if (tileCoord.Lod >= 21)
            {
                return new List<TileKey>();
            }
            List<TileKey> list = new List<TileKey>();
            list.Add(new TileKey(2 * tileCoord.X, 2 * tileCoord.Y, tileCoord.Lod + 1));
            list.Add(new TileKey(2 * tileCoord.X, 2 * tileCoord.Y + 1, tileCoord.Lod + 1));
            list.Add(new TileKey(2 * tileCoord.X + 1, 2 * tileCoord.Y, tileCoord.Lod + 1));
            list.Add(new TileKey(2 * tileCoord.X + 1, 2 * tileCoord.Y + 1, tileCoord.Lod + 1));
            return list;
        }

        /// <summary>
        /// 根据像素坐标，计算其所在瓦片的索引
        /// 每一片瓦片大小为 256 pixel，并且 tile 索引是从 0 开始
        /// </summary>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        public static void PixelToTile(int pixelX, int pixelY, out int tileX, out int tileY)
        {
            tileX = (int)Math.Floor(pixelX / 256.0);
            tileY = (int)Math.Floor(pixelY / 256.0);
        }

        /// <summary>
        /// 根据瓦片索引，计算其起始点（西南角）的像素坐标
        /// </summary>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="pixelX"></param>
        /// <param name="pixelY"></param>
        public static void TileToPixel(int tileX, int tileY, out int pixelX, out int pixelY)
        {
            pixelX = tileX * 256;
            pixelY = tileY * 256;
        }

        /// <summary>
        /// 根据瓦片的 X、Y 索引，生成四叉树键值
        /// </summary>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="levelOfDetail"></param>
        /// <returns></returns>
        public static string TileXYToQuadKey(int tileX, int tileY, int levelOfDetail)
        {
            StringBuilder quadKey = new StringBuilder();
            for (int i = levelOfDetail; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((tileX & mask) != 0)
                {
                    digit++;
                }
                if ((tileY & mask) != 0)
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }

        /// <summary>
        /// 根据四叉树键值，生成瓦片的 X、Y 索引
        /// </summary>
        /// <param name="quadKey"></param>
        /// <param name="tileX"></param>
        /// <param name="tileY"></param>
        /// <param name="levelOfDetail"></param>
        public static void QuadKeyToTileXY(string quadKey, out int tileX, out int tileY, out int levelOfDetail)
        {
            tileX = tileY = 0;
            levelOfDetail = quadKey.Length;
            for (int i = levelOfDetail; i > 0; i--)
            {
                int mask = 1 << (i - 1);
                switch (quadKey[levelOfDetail - i])
                {
                    case '0':
                        break;

                    case '1':
                        tileX |= mask;
                        break;

                    case '2':
                        tileY |= mask;
                        break;

                    case '3':
                        tileX |= mask;
                        tileY |= mask;
                        break;

                    default:
                        throw new ArgumentException("Invalid QuadKey digit sequence.");
                }
            }
        }

        #region Override

        public override string ToString()
        {
            return $"({X}, {Y}, {Lod})";
        }

        public bool Equals(TileKey other)
        {
            return other.X == X && other.Y == Y && other.Lod == Lod;
        }

        public override bool Equals(object obj)
        {
            return obj is TileKey && Equals((TileKey)obj);
        }

        public override int GetHashCode()
        {
            int num = 17;
            num = 31 * num + X;
            num = 31 * num + Y;
            return 31 * num + Lod;
        }

        #endregion
    }

    public class TileUtil
    {
        /// <summary>
        /// 最小纬度(角度值)
        /// </summary>
        public const double MinLatitude = -85.0511287798;

        /// <summary>
        /// 最大纬度(角度值)
        /// </summary>
        public const double MaxLatitude = 85.0511287798;

        /// <summary>
        /// 最小经度（角度）
        /// </summary>
        public const double MinLongitude = -180.0;

        /// <summary>
        /// 最大经度（角度）
        /// </summary>
        public const double MaxLongitude = 180.0;

        /// <summary>
        /// 根据经纬度，计算指定<paramref name="levelOfDetail"/>的瓦片索引
        /// </summary>
        /// <param name="latitude">纬度（角度）</param>
        /// <param name="longitude">经度（角度）</param>
        /// <param name="levelOfDetail">瓦片的Lod</param>
        /// <returns></returns>
        internal static TileKey LatLongToTile(double latitude, double longitude, int levelOfDetail)
        {
            latitude = MathUtil.Clamp(latitude, MinLatitude, MaxLatitude);
            longitude = MathUtil.Clamp(longitude, MinLongitude, MaxLongitude);

            double x = (longitude + 180.0) / 360.0;
            double sinLatitude = Math.Sin(latitude * MathUtil.Deg2Rad);
            double y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4.0 * Math.PI);

            uint mapSize = TileKey.MapSize(levelOfDetail);

            int pixelX = (int)MathUtil.Clamp(x * mapSize + 0.5, 0, mapSize - 1);
            int pixelY = (int)MathUtil.Clamp(y * mapSize + 0.5, 0, mapSize - 1);

            int tileX;
            int tileY;
            TileKey.PixelToTile(pixelX, pixelY, out tileX, out tileY);
            return new TileKey(tileX, tileY, levelOfDetail);
        }

        internal static void TileToLatLon(int xTile, int yTile, int lod, out double lat, out double lon)
        {
            int pixelX = 0;
            int pixelY = 0;

            TileKey.TileToPixel(xTile, yTile, out pixelX, out pixelY);
            double mapSizeInPixel = TileKey.MapSize(lod);

            double x = (MathUtil.Clamp(pixelX, 0, mapSizeInPixel - 1) / mapSizeInPixel) - 0.5;
            double y = 0.5 - (MathUtil.Clamp(pixelY, 0, mapSizeInPixel - 1) / mapSizeInPixel);

            double latitude = 90 - 360 * Math.Atan(Math.Exp(-y * 2 * Math.PI)) / Math.PI;
            double longtitue = x * 360.0;

            lat = MathUtil.Clamp(latitude, MinLatitude, MaxLatitude);
            lon = MathUtil.Clamp(longtitue, MinLongitude, MaxLongitude);
        }

    }
}
