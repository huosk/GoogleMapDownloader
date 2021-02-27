@echo off

TileDownloader.exe --minlod 3 ^
--maxlod 8 ^
--url "http://mt2.google.cn/vt/lyrs=y@258000000&hl=zh-CN&gl=CN&src=app&x={x}&y={y}&z={z}" ^
--output "D:\Projects\TileApp\TileDownloader\bin\Debug\DownloadTile\{z}\{x}\{y}.jpg" ^
--proxy "http://127.0.0.1:10809" ^
--retry 3 ^
--retry_max_seconds 30 ^
--thread 32

pause