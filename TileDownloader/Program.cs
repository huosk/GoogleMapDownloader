using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections.Concurrent;
using System.Diagnostics;
using CommandLine;

namespace TileDownloader
{
    [Verb("load", isDefault: true)]
    class LoadOptions
    {
        [Option("bound", Min = 4, HelpText = "download tiles bound,defined as a set of wgs84 longitudes and latitudes,order:south latitude,west longitude,north latitude,east longitude")]
        public IEnumerable<double> bound { get; set; }

        [Option("minlod", HelpText = "min level of detail.")]
        public int minLod { get; set; }

        [Option("maxlod", HelpText = "max level of detail.")]
        public int maxLod { get; set; }

        [Option("url", Required = true, HelpText = @"tile url,eg.http://mt2.google.cn/vt/lyrs=y@258000000&hl=zh-CN&gl=CN&src=app&x={x}&y={y}&z={z}")]
        public string url { get; set; }

        [Option("output", HelpText = "tile output format,eg.D:\\path\\to\\save\\{z}\\{x}\\{y}.jpg")]
        public string output { get; set; }

        [Option("proxy", HelpText = "download tile using proxy address.")]
        public string proxyAddress { get; set; }

        [Option("retry", HelpText = "retry times,default is 0", Default = 0)]
        public int retry { get; set; }

        [Option("retry_max_seconds", HelpText = "retry max time in seconds,default value is 30.", Default = 30)]
        public int retry_max_seconds { get; set; }

        [Option("thread", HelpText = "worker thread count,default is 32", Default = 32)]
        public int thread { get; set; }
    }

    [Verb("query", HelpText = "query tile info")]
    class QueryOption
    {
        [Option("positions", Min = 3, HelpText = "wgs84 latitudes and longitudes,format:: lon1,lat1,lod1,lon2,lat2,lod2,lon3,lat3,lod3")]
        public IEnumerable<double> positions { get; set; }

        [Option("tiles", Min = 3, HelpText = "tile index in web mercator projected map,format x1,y1,lod1,x2,y2,lod2,x3,y3,lod3")]
        public IEnumerable<int> tileIds { get; set; }
    }

    class Program
    {
        const string symbol_x = "{x}";
        const string symbol_y = "{y}";
        const string symbol_lod = "{z}";
        const string symbol_south = "{south}";
        const string symbol_west = "{west}";
        const string symbol_north = "{north}";
        const string symbol_east = "{east}";
        const string symbol_center_lat = "{center_lat}";
        const string symbol_center_lon = "{center_lon}";

        static int maxTaskCount = 32;
        static int taskIndex = 0;
        static int taskCount = 0;

        static string url = null;
        static string output = null;
        static string proxyAddress = null;
        static int retryTimes = 0;
        static int retryMaxSeconds = 30;

        static List<TileKey> failed = new List<TileKey>();
        static List<TileKey> keys = new List<TileKey>();

        static double[] WorldBound = new double[]
        {
            TileUtil.MaxLatitude,
            TileUtil.MinLongitude,
            TileUtil.MinLatitude,
            TileUtil.MaxLongitude
        };

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<LoadOptions, QueryOption>(args)
                .WithParsed<LoadOptions>(HandleDownload)
                .WithParsed<QueryOption>(HandleQuery)
                .WithNotParsed(HandleParseError);
        }

        private static void HandleParseError(IEnumerable<Error> obj)
        {
            foreach (var err in obj)
            {
                Console.Error.WriteLine(err.ToString());
            }
        }

        private static void HandleQuery(QueryOption options)
        {
            List<TileKey> tiles = new List<TileKey>();

            double[] positions = options.positions.ToArray();
            int[] tileIds = options.tileIds.ToArray();

            for (int i = 0; i < positions.Length; i += 3)
            {
                if (i + 2 < positions.Length)
                {
                    double lon = positions[i];
                    double lat = positions[i + 1];
                    int lod = Convert.ToInt32(positions[i + 2]);

                    var t = TileUtil.LatLongToTile(lat, lon, lod);
                    tiles.Add(t);
                }
            }

            for (int i = 0; i < tileIds.Length; i += 3)
            {
                if (i + 2 < tileIds.Length)
                {
                    int tileX = tileIds[i];
                    int tileY = tileIds[i + 1];
                    int tileLod = tileIds[i + 2];

                    tiles.Add(new TileKey(tileX, tileY, tileLod));
                }
            }

            StringBuilder outputBuilder = new StringBuilder();
            string title_ = "Tile  Info";
            int padLen = (int)((100 - title_.Length) >> 1);
            outputBuilder.AppendLine($"{new string('-', padLen)}{title_}{new string('-', padLen)}");
            outputBuilder.Append("Number".PadRight(10));
            outputBuilder.Append("Lon".PadRight(12));
            outputBuilder.Append("Lat".PadRight(12));
            outputBuilder.Append("X".PadRight(10));
            outputBuilder.Append("Y".PadRight(10));
            outputBuilder.Append("Z".PadRight(6));
            outputBuilder.Append("East".PadRight(10));
            outputBuilder.Append("South".PadRight(10));
            outputBuilder.Append("West".PadRight(10));
            outputBuilder.Append("North".PadRight(10));
            outputBuilder.AppendLine();

            for (int i = 0; i < tiles.Count; i++)
            {
                double lon_ = 0.0;
                double lat_ = 0.0;
                double south_ = 0.0;
                double west_ = 0.0;
                double north_ = 0.0;
                double east_ = 0.0;

                TileUtil.TileToLatLon(tiles[i].X, tiles[i].Y, tiles[i].Lod, out lat_, out lon_);

                TileUtil.TileToLatLon(tiles[i].WestBound, tiles[i].NorthBound, tiles[i].Lod, out north_, out east_);
                TileUtil.TileToLatLon(tiles[i].EastBound, tiles[i].SouthBound, tiles[i].Lod, out south_, out west_);

                outputBuilder.Append((i + 1).ToString().PadRight(10));
                outputBuilder.Append(lon_.ToString("f5").PadRight(12));
                outputBuilder.Append(lat_.ToString("f5").PadRight(12));
                outputBuilder.Append(tiles[i].X.ToString().PadRight(10));
                outputBuilder.Append(tiles[i].Y.ToString().PadRight(10));
                outputBuilder.Append(tiles[i].Lod.ToString().PadRight(6));
                outputBuilder.Append(east_.ToString("f5").PadRight(10));
                outputBuilder.Append(south_.ToString("f5").PadRight(10));
                outputBuilder.Append(west_.ToString("f5").PadRight(10));
                outputBuilder.Append(north_.ToString("f5").PadRight(10));

                outputBuilder.AppendLine();
            }

            outputBuilder.AppendLine(new string('-', 100));

            Console.WriteLine(outputBuilder.ToString());
        }

        private static void HandleDownload(LoadOptions options)
        {
            if (options == null)
                throw new ArgumentNullException("options");

            double[] bounds = (options.bound == null || options.bound.Count() == 0) ? WorldBound : options.bound.ToArray();
            double south = bounds[0];   // 20.097
            double west = bounds[1];    // 110.0
            double north = bounds[2];   // 19.628;
            double east = bounds[3];    // 110.8

            int minlod = options.minLod;
            int maxlod = options.maxLod;

            url = options.url;
            output = options.output;
            if (string.IsNullOrEmpty(output))
            {
                output = string.Format(@"{0}\Download\{z}\{x}\{y}.jpg", Environment.CurrentDirectory);
            }

            proxyAddress = options.proxyAddress;
            retryTimes = options.retry;
            retryMaxSeconds = options.retry_max_seconds;

            for (int lod = minlod; lod <= maxlod; lod++)
            {
                var fromKey = TileUtil.LatLongToTile(north, west, lod);
                var toKey = TileUtil.LatLongToTile(south, east, lod);

                int x0 = fromKey.X < toKey.X ? fromKey.X : toKey.X;
                int x1 = fromKey.X < toKey.X ? toKey.X : fromKey.X;

                int y0 = fromKey.Y < toKey.Y ? fromKey.Y : toKey.Y;
                int y1 = fromKey.Y < toKey.Y ? toKey.Y : fromKey.Y;

                for (int x = x0; x <= x1; x++)
                {
                    for (int y = y0; y <= y1; y++)
                    {
                        keys.Add(new TileKey(x, y, lod));
                    }
                }
            }

            ConcurrentDictionary<Task, bool> tasks = new ConcurrentDictionary<Task, bool>();

            Console.WriteLine("开始下载");
            Console.WriteLine();

            while (taskIndex < keys.Count)
            {
                while (taskCount < maxTaskCount)
                {
                    if (taskIndex >= keys.Count)
                    {
                        break;
                    }

                    var toLoadKey = keys[taskIndex];
                    var t = LoadKey(toLoadKey);
                    t.ContinueWith((task) =>
                    {
                        tasks.TryRemove(task, out bool _);
                    });
                    tasks.TryAdd(t, false);

                    Interlocked.Increment(ref taskIndex);
                    Interlocked.Increment(ref taskCount);

                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($"{taskIndex} / {keys.Count}");
                }
            }

            Task.WaitAll(tasks.Keys.ToArray());

            Console.WriteLine($"下载完成：成功{keys.Count - failed.Count},失败:{failed.Count}");
        }

        private static void ReplaceSymbol(StringBuilder builder, Dictionary<string, string> symbols)
        {
            if (symbols == null)
                return;

            foreach (var symbol in symbols)
            {
                if (string.IsNullOrEmpty(symbol.Key) ||
                    string.IsNullOrEmpty(symbol.Value))
                    continue;

                builder.Replace(symbol.Key, symbol.ToString());
            }
        }

        private static async Task LoadKey(TileKey key)
        {
            try
            {
                double south_, west_, north_, east_;
                TileUtil.TileToLatLon(key.WestBound, key.NorthBound, key.Lod, out north_, out east_);
                TileUtil.TileToLatLon(key.EastBound, key.SouthBound, key.Lod, out south_, out west_);

                double center_lat = (south_ + north_) / 2.0;
                double center_lon = (east_ + west_) / 2.0;

                north_ = MathUtil.Clamp(north_, TileUtil.MinLatitude, TileUtil.MaxLatitude);
                south_ = MathUtil.Clamp(south_, TileUtil.MinLatitude, TileUtil.MaxLatitude);

                Dictionary<string, string> symbols = new Dictionary<string, string>();
                symbols[symbol_x] = key.X.ToString();
                symbols[symbol_y] = key.Y.ToString();
                symbols[symbol_lod] = key.Lod.ToString();
                symbols[symbol_south] = south_.ToString();
                symbols[symbol_west] = west_.ToString();
                symbols[symbol_north] = north_.ToString();
                symbols[symbol_east] = east_.ToString();
                symbols[symbol_center_lat] = center_lat.ToString();
                symbols[symbol_center_lon] = center_lon.ToString();

                StringBuilder address = new StringBuilder(url);
                ReplaceSymbol(address, symbols);

                StringBuilder outputFileBuilder = new StringBuilder(output);
                ReplaceSymbol(outputFileBuilder, symbols);

                string file = outputFileBuilder.ToString();
                string dir = System.IO.Path.GetDirectoryName(file);
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                StringBuilder argument = new StringBuilder();
                argument.Append($"--output {file} ");
                argument.Append($"--retry {retryTimes} ");
                argument.Append($"--retry-max-time {retryMaxSeconds} ");

                if (!string.IsNullOrEmpty(proxyAddress))
                    argument.Append($"--proxy {proxyAddress} ");

                argument.Append($"{address.ToString()}");

                Process curl = new Process();
                curl.StartInfo.FileName = "curl";
                curl.StartInfo.Arguments = argument.ToString();
                curl.StartInfo.UseShellExecute = false;
                curl.StartInfo.CreateNoWindow = true;
                curl.Start();

                await Task.Run(() => curl.WaitForExit());

                if (curl.ExitCode != 0)
                {
                    failed.Add(key);
                }
            }
            catch
            {
                lock (failed)
                {
                    failed.Add(key);
                }
            }
            finally
            {
                Interlocked.Decrement(ref taskCount);
            }
        }
    }
}
