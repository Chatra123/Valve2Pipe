﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Valve2Pipe
{
  using OctNov.IO;

  /// <summary>
  /// 設定ＸＭＬファイル
  /// </summary>
  [Serializable]
  public class Setting_File
  {
    const double CurrentRev = 13.0;

    public double Rev = 0.0;
    public int Encoder_CPU_Max;
    public int System__CPU_Max;
    public double ReadLimit_MiBsec;
    public int Encoder_MultipleRun;
    public string EncoderNames;
    public List<Client_WriteStdin> PresetEncoder;

    private static readonly string
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location,
            AppDir = Path.GetDirectoryName(AppPath),
            AppName = Path.GetFileNameWithoutExtension(AppPath),
            Default_XmlName = AppName + ".xml",
            Default_XmlPath = Path.Combine(AppDir, Default_XmlName);

    /// <summary>
    /// constructor
    /// </summary>
    public Setting_File()
    {
      Encoder_CPU_Max = 20;
      System__CPU_Max = 80;
      Encoder_MultipleRun = 1;
      EncoderNames = "  ffmpeg   x264   x265  ";
      ReadLimit_MiBsec = 10.0;
      PresetEncoder = new List<Client_WriteStdin>();
    }


    /// <summary>
    /// 設定ファイルを読込
    /// </summary>
    public static Setting_File LoadFile(string xmlpath = null)
    {
      //デフォルト名を使用、新規作成
      if (string.IsNullOrEmpty(xmlpath))
      {
        xmlpath = Default_XmlPath;
        if (File.Exists(xmlpath) == false)
          XmlRW.Save(xmlpath, Sample_RunTest());
      }

      var file = XmlRW.Load<Setting_File>(xmlpath);

      //追加された項目、削除された項目を書き換え。
      //ユーザーが消したタグなども復元される。
      if (file.Rev != CurrentRev)
      {
        file.Rev = CurrentRev;
        XmlRW.Save(xmlpath, file);
      }
      return file;
    }


    /// <summary>
    /// サンプル設定　　Encの動作テスト
    /// </summary>
    public static Setting_File Sample_RunTest()
    {
      var setting = new Setting_File();

      setting.PresetEncoder = new List<Client_WriteStdin>()
      {
        new Client_WriteStdin()
        {
          memo= "  動作確認　xvid  ",
          Name = "  RunTest_avi  ",
          BasePath = @"   .\ffmpeg.exe   ",
          BaseArgs1 = " -i pipe:0  -threads 1                     ",
          BaseArgs2 = " -vcodec libxvid    -s  160x120 -b:v 128k  ",
          BaseArgs3 = " -acodec libmp3lame -ar 48000   -b:a  64k  ",
          BaseArgs4 = " -y  \"$FilePathWithoutExt$.avi\"            ",
        },

        new Client_WriteStdin()
        {
          memo= "  動作確認  x264  ",
          Name = "  RunTest_mp4  ",
          BasePath = @"   .\ffmpeg.exe   ",
          BaseArgs1 = " -i pipe:0  -threads 1                     ",
          BaseArgs2 = " -vcodec libx264    -s 160x120  -crf 40    ",
          BaseArgs3 = "                                           ",
          BaseArgs4 = " -y  \"$FilePathWithoutExt$.mp4\"            ",
        },
      };
      return setting;
    }




  }
}