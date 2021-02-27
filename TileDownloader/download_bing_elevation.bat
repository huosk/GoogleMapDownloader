@echo off

TileDownloader.exe --minlod 3 ^
--maxlod 8 ^
--url "http://dev.virtualearth.net/REST/v1/Elevation/Bounds?bounds={south},{west},{north},{east}&rows=17&cols=17&key={BingMapsAPIKey}" ^
--output "D:\Projects\TileApp\TileDownloader\bin\Debug\DownloadTile\{z}\{x}\{y}.txt" ^
--proxy "http://127.0.0.1:10809" ^
--retry 3 ^
--retry_max_seconds 30 ^
--thread 32

pause