using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CopyTileApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string srcDir = @"D:\MapData\TilesCache";
            string dstDir = @"D:\MapData\NewCache";

            string[] files = Directory.GetFiles(srcDir, "*.*", SearchOption.AllDirectories);

            int copyCount = 0;
            int fileCount = files.Length;

            var t = Parallel.ForEach(files, (file) =>
             {
                 if (!File.Exists(file))
                     return;

                 // \lod\x\y
                 string dir = Path.GetDirectoryName(file);

                 // y
                 string y = Path.GetFileName(dir);
                 string ext = Path.GetExtension(file);

                 string relPath = Path.GetDirectoryName(dir).Substring(srcDir.Length).Trim('\\', '/');

                 string newDir = Path.Combine(dstDir, relPath);
                 if (!Directory.Exists(newDir))
                     Directory.CreateDirectory(newDir);

                 string newFile = $"{newDir}\\{y}.{ext}";
                 byte[] dat = File.ReadAllBytes(file);
                 File.WriteAllBytes(newFile, dat);

                 Interlocked.Increment(ref copyCount);

                 int curTop = Console.CursorTop;
                 Console.WriteLine($"处理进度::{copyCount} / {fileCount}");
                 Console.SetCursorPosition(0, curTop);
             });

            while (!t.IsCompleted)
                Thread.Sleep(1000);

            Console.WriteLine("Copy complete");

            Console.ReadKey();
        }

        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
