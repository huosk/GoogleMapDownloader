@echo off

.\TileDownload.exe --bound 20.097 110.0 19.628 110.8 ^
--minlod 3 ^
--maxlod 18 ^
--url "http://mt2.google.cn/vt/lyrs=y@258000000&hl=zh-CN&gl=CN&src=app&x={x}&y={y}&z={z}" ^
--output "D:\DownloadTile\{z}\{x}\{y}.jpg" ^
--proxy "http://127.0.0.1:10809" ^
--retry 3 ^
--retry_max_seconds 30 ^
--thread 32

pause