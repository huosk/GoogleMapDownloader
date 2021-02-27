# GoogleMapDownloader

## Build

1. Download and install [Curl](https://curl.se/)
2. Insert curl path to environment PATH
3. Open solution,install reference library `Install-Package CommandLineParser -Version 2.8.0`
4. Build project

## Usage

Download tiles of world

```bat
TileDownloader.exe --minlod 3 ^
--maxlod 8 ^
--url "http://mt2.google.cn/vt/lyrs=y@258000000&hl=zh-CN&gl=CN&src=app&x={x}&y={y}&z={z}" ^
--output "D:\Projects\TileApp\TileDownloader\bin\Debug\DownloadTile\{z}\{x}\{y}.jpg" ^
--proxy "http://127.0.0.1:10809" ^
--retry 3 ^
--retry_max_seconds 30 ^
--thread 32
```

Download the specified range of tiles

```bat
TileDownloader.exe --bound 20.097 110.0 19.628 110.8 ^
--minlod 3 ^
--maxlod 8 ^
--url "http://mt2.google.cn/vt/lyrs=y@258000000&hl=zh-CN&gl=CN&src=app&x={x}&y={y}&z={z}" ^
--output "D:\Projects\TileApp\TileDownloader\bin\Debug\DownloadTile\{z}\{x}\{y}.jpg" ^
--proxy "http://127.0.0.1:10809" ^
--retry 3 ^
--retry_max_seconds 30 ^
--thread 32
```
