
## Valve2Pipe

ファイル又は、標準入力からデータを取得しエンコーダーに転送します。
転送量を調整することでエンコーダーのＣＰＵ使用率を一定に保ちます。



------------------------------------------------------------------
### 使い方

1. ffmpegをフォルダ内におく
2. Run_Valve2Pipe.batのショートカットを作成し、TSファイルをドロップ



### 使い方　　コマンドライン

ファイル  
Valve2Pipe.exe  "C:\video.ts"        -profile RunTest_mp4  

パイプ  
Valve2Pipe.exe  -pipe "C:\video.ts"  -profile RunTest_mp4  



------------------------------------------------------------------
### 引数

    -file  "C:\video.ts"  
入力ファイルパス  
マクロ$FilePath$はC:\video.tsに置換されます。  


    -pipe  "C:\video.ts"  
パイプ  
マクロ$FilePath$はC:\video.tsに置換されます。  


    -profile RunTest_mp4
エンコーダー名を指定する。設定ファイル PresetEncoderの Nameから RunTest_mp4を探します。  


    -stdout
エンコーダーへリダイレクトしないで、標準出力へ出力します。  



------------------------------------------------------------------
### 設定

    Encoder_CPU_Max  20.0  
エンコーダープロセスのＣＰＵ使用率が２０％以下になるように転送量を調整します。  


    System__CPU_Max  80.0  
システム全体のＣＰＵ使用率が８０％以下になるように転送量を調整します。  


    ReadLimit_MiBsec  10.0  
ファイル読込速度を制限します。  


    Encoder_MultipleRun  1  
エンコーダーの同時起動数  
エンコーダープロセスが１未満になるまで実行開始を遅らせます。


    EncoderNames    ffmpeg   x264   x265  
エンコーダー名を指定します。同時起動数の制限用  


    PresetEncoder  
エンコーダーのパス、引数の指定  
パスはValve2Pipeのフォルダからの相対パスで指定できます。  



-----------------------------------------------------------------
### マクロ

BasePath、BaseArgsで使えるマクロ  

|  マクロ                |  説明                     |  例               |
|:-----------------------|:--------------------------|:------------------|
|  $FilePath$            |  入力ファイルパス         |  C:\rec\news.ts   |
|  $FolderPath$          |  フォルダパス             |  C:\rec           |
|  $FileName$            |  拡張子無しファイル名     |  news             |
|  $Ext$                 |  拡張子                   |  .ts              |
|  $FileNameExt$         |  拡張子付きファイル名     |  news.ts          |
|  $FilePathWithoutExt$  |  拡張子無しファイルパス   |  C:\rec\news      |



------------------------------------------------------------------
### SplitVideo.exeについて

作成したavi, mp4をLGLancherの生成したフレームファイルを元にカットします。  
ogmチャプターがあればチャプター付mp4も作成します。 



------------------------------------------------------------------
### 使用ライブラリ

Mono.Options  

    Authors:  
        Jonathan Pryor <jpryor@novell.com>  
        Federico Di Gregorio <fog@initd.org>  
        Rolf Bjarne Kvinge <rolf@xamarin.com>  
    Copyright (C) 2008 Novell (http://www.novell.com)  
    Copyright (C) 2009 Federico Di Gregorio.  
    Copyright (C) 2012 Xamarin Inc (http://www.xamarin.com)  


remuxer  

    Copyright (C) 2010-2015 L-SMASH project  
    https://github.com/l-smash  
 
 
 
------------------------------------------------------------------
### ライセンス

    MIT Licence
    Copyright (C) 2014  CHATRA


