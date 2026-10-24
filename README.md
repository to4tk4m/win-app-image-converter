# 画像フォーマット変換

画像ファイルをドラッグ＆ドロップして、JPEG / PNG に変換する Windows アプリです。

変換元: JPEG / PNG / BMP / GIF / TIFF / WebP / HEIC（HEIF）/ AVIF。
WebP・HEIC・AVIF の読み込みライブラリはアプリに同梱され、追加の Windows コーデックは不要です。
変換先は JPEG / PNG です。

## 使い方

1. `ImageConverter.exe` を起動します。
2. ドロップダウンリストで変換先の形式（JPEG / PNG）を選びます。
3. ウィンドウに画像ファイルをドロップします。複数ファイルもまとめて処理できます。
4. 元画像と同じフォルダーに、拡張子を `.jpg` / `.png` にした画像を保存します。

元画像は変更しません。実際の画像形式が変換先と同じ場合は何もしません。
出力先に同名ファイルがある場合は `画像 (1).jpg` のように連番を付けます。
処理結果やエラーはウィンドウ下部に表示します。読み込めないファイルが混ざっても、残りの処理を続けます。

JPEG は品質 90 で保存し、透過部分を白で塗ります。PNG は透過に対応します。
アニメーション画像・複数ページ画像は最初のフレームのみ変換します。
画像のメタデータは引き継ぎません。フォルダーのドロップには対応していません。
写真の向き情報は変換前に反映します。出力は各色 8 ビットです。

## 開発・実行

Windows と .NET 9 SDK が必要です。
画像処理には [Magick.NET-Q8-AnyCPU](https://github.com/dlemstra/Magick.NET) 15.0.0 を使用します。
初回ビルド時には NuGet からパッケージをダウンロードします。

```powershell
dotnet run --project ImageConverter
```

変換処理の確認:

```powershell
dotnet run --project ImageConverter.SmokeTests
```

HEIC の実画像も確認する場合（元画像は変更せず、一時フォルダーにコピーしてテストします）:

```powershell
dotnet run --project ImageConverter.SmokeTests -- "C:\Photos\sample.heic"
```

## 配布用ビルド

.NET 9 Desktop Runtime がインストールされている Windows 向け:

```powershell
dotnet publish ImageConverter -c Release -o artifacts/app
```

`artifacts/app` フォルダー全体を配布し、`ImageConverter.exe` を起動してください。
画像処理ライブラリと `ThirdPartyNotices` 内のライセンス表記も配布物に含まれます。

ランタイムを同梱する Windows x64 向けビルド（初回はダウンロードが必要です）:

```powershell
dotnet publish ImageConverter -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o artifacts/standalone
```
