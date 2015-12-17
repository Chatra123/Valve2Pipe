
## Valve2Pipe

ファイル又は、パイプからデータを取得しエンコーダーに転送します。
転送量を調整することでエンコーダーのＣＰＵ使用率を一定に保ちます。



------------------------------------------------------------------
### 使い方

１.　ffmpegを同じフォルダにいれる。
２.　Run_Valve2Pipe.batにTSファイルをドロップ


### 使い方　　コマンドライン

ファイル  
Valve2Pipe.exe  "C:\video.ts"        -profile RunTest_mp4

パイプ  
Valve2Pipe.exe  -pipe "C:\video.ts"  -profile RunTest_mp4



------------------------------------------------------------------
### 引数

    -file  "C:\video.ts"
入力ファイルパス
置換マクロで"C:\video.ts"が使用されます。

    -pipe  "C:\video.ts"
パイプ入力
置換マクロで"C:\video.ts"が使用されます。


    -profile RunTest_mp4
エンコーダー名を指定する。設定ファイル PresetEncoderの sNameから RunTest_mp4を探します。


    -stdout
エンコーダーへリダイレクトしないで、標準出力へ出力します。



------------------------------------------------------------------
### 設定
実行時に設定ファイルがなければ作成されます。  

    iEncoder_CPU_Max  20  
エンコーダープロセスのＣＰＵ使用率が２０％以下になるように転送量を調整します。  


    iSystem__CPU_Max  80  
システム全体のＣＰＵ使用率が８０％以下になるように転送量を調整します。  


    iEncorder_MultipleRun  1  
sEncorderNamesで指定したプロセス名の同時起動数  


    sEncorderNames    ffmeg   x264   x265  
エンコーダー名を指定します。同時起動数の制限用  


    dReadLimit_MiBsec  10  
ファイル読込速度を制限します。  


    PresetEncoder  
エンコーダーのパス、引数の指定  
パスはValve2Pipeのフォルダからの相対パスで指定できます。  



-----------------------------------------------------------------
### マクロ

sBasePath、sBaseArgsで使えるマクロ  


|  マクロ            |  説明                        |  例              |
|:-------------------|:-----------------------------|:-----------------|
|  $fPath$           |  入力ファイルパス            |  C:\rec\news.ts  |
|  $fDir$            |  ディレクトリ名              |  C:\rec          |
|  $fName$           |  ファイル名                  |  news.ts         |
|  $fNameWithoutExt$ |  拡張子なしファイル名        |  news            |
|  $fPathWithoutExt$ |  拡張子なしファイルパス      |  C:\rec\news     |


  
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
 
 

------------------------------------------------------------------
### ライセンス

    GPL v3
    Copyright (C) 2014  CHATRA
    http://www.gnu.org/licenses/




