@echo off
setlocal


::WorkDir
pushd "%~dp0"


::
::  [[  ffmpeg  ]]
::    aviファイルを分割してから結合
::  　音声は再エンコードされる。
::  

set ffmpeg="$ffmpeg$"

::
:: [ set name ]
::
set SrcVideo="$AviPath$"

set PartCount=$PartCount$
set ext=$ext$

set P1=$AviShort$.p1%ext%
set P2=$AviShort$.p2%ext%
set P3=$AviShort$.p3%ext%
set P4=$AviShort$.p4%ext%
set P5=$AviShort$.p5%ext%
set P6=$AviShort$.p6%ext%
set P7=$AviShort$.p7%ext%
set P8=$AviShort$.p8%ext%
set P9=$AviShort$.p9%ext%

set CatName="$AviShort$.cut%ext%"

set ListPath="$AviShort$$ext$.catlist.txt"

:: check ffmpeg
if not exist %ffmpeg% (
   exit /b
)


::
:: [ split ]
::
::part1::   %ffmpeg% -ss $BeginSecP1$  -i %SrcVideo%  -threads 1  -filter_complex trim=duration=$DurSecP1$,setpts=PTS-STARTPTS;atrim=duration=$DurSecP1$,asetpts=PTS-STARTPTS    -y "%P1%"
::part2::   %ffmpeg% -ss $BeginSecP2$  -i %SrcVideo%  -threads 1  -filter_complex trim=duration=$DurSecP2$,setpts=PTS-STARTPTS;atrim=duration=$DurSecP2$,asetpts=PTS-STARTPTS    -y "%P2%"
::part3::   %ffmpeg% -ss $BeginSecP3$  -i %SrcVideo%  -threads 1  -filter_complex trim=duration=$DurSecP3$,setpts=PTS-STARTPTS;atrim=duration=$DurSecP3$,asetpts=PTS-STARTPTS    -y "%P3%"
::part4::   %ffmpeg% -ss $BeginSecP4$  -i %SrcVideo%  -threads 1  -filter_complex trim=duration=$DurSecP4$,setpts=PTS-STARTPTS;atrim=duration=$DurSecP4$,asetpts=PTS-STARTPTS    -y "%P4%"
::part5::   %ffmpeg% -ss $BeginSecP5$  -i %SrcVideo%  -threads 1  -filter_complex trim=duration=$DurSecP5$,setpts=PTS-STARTPTS;atrim=duration=$DurSecP5$,asetpts=PTS-STARTPTS    -y "%P5%"
::part6::   %ffmpeg% -ss $BeginSecP6$  -i %SrcVideo%  -threads 1  -filter_complex trim=duration=$DurSecP6$,setpts=PTS-STARTPTS;atrim=duration=$DurSecP6$,asetpts=PTS-STARTPTS    -y "%P6%"
::part7::   %ffmpeg% -ss $BeginSecP7$  -i %SrcVideo%  -threads 1  -filter_complex trim=duration=$DurSecP7$,setpts=PTS-STARTPTS;atrim=duration=$DurSecP7$,asetpts=PTS-STARTPTS    -y "%P7%"
::part8::   %ffmpeg% -ss $BeginSecP8$  -i %SrcVideo%  -threads 1  -filter_complex trim=duration=$DurSecP8$,setpts=PTS-STARTPTS;atrim=duration=$DurSecP8$,asetpts=PTS-STARTPTS    -y "%P8%"
::part9::   %ffmpeg% -ss $BeginSecP9$  -i %SrcVideo%  -threads 1  -filter_complex trim=duration=$DurSecP9$,setpts=PTS-STARTPTS;atrim=duration=$DurSecP9$,asetpts=PTS-STARTPTS    -y "%P9%"



::
:: [ concat ]
::
::  テキストの作成、中身をクリア
echo. >%ListPath%

::  part video名をテキストに書き込む
for /L %%n in (1,1,%PartCount%) do  echo file $AviShort$.p%%n$ext$>>%ListPath%
timeout /t 2 /nobreak

%ffmpeg% -f concat -i %ListPath%  -threads 1  -c copy  -y "%CatName%"




::
:: [ Rename and Remove ]
::
timeout /t 2 /nobreak

del /q  "$CutAvi_Name$"
rename  "$CutAvi_ShortPath$"  "$CutAvi_Name$"

::   /q  削除前に確認メッセージを表示しない
del /q "$AviShort$.p*%ext%"
del /q "%ListPath%"




::
::　[[  L-Smash remuxer  ]]
::    ogm chapterを mp4に結合
::  
set remuxer="$remuxer$"
set ChapterVideo="$AviName$.chap%ext%"
set nero="$AviName$.ogm.chapter"

if exist %remuxer% (
  if exist %nero% (
       timeout /t 2 /nobreak
       %remuxer% -i %SrcVideo% --chapter %nero% -o %ChapterVideo%
  )
)


popd
endlocal
::  timeout /t 5 /nobreak
::  pause
::  exit /b







::
::
::  ☆ファイル名の文字について
::
::　通常のbatはファイル名に&があると
::  set P1=ANN&ニュース.ts
::  が処理できない。　batの制御文字&と認識される。
::  必ず
::  set P1="ANN&ニュース.ts"
::  にする。
::
::  ffmpegに、
::  "concat:%P1%|%P2%"  
::  があると、P1が "" で囲われているためffmpeg側の処理ができなくなる。
::
::  そのため、事前にファイル名から&を除いてから$AviShort$に使用する。
::  SrcVideo="InputName.avi"は&を置換していないので""で囲う。
::
::　今回は事前にshortnameからバッチの特殊文字を _ に置換するようにした。
::
::
::　☆文字コード
::  UTF-8でもbatは実行できるが日本語ファイルの取り扱いができないので
::  Shift-JISで保存、実行する。
::
::








