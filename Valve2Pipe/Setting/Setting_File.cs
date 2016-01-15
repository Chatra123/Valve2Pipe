using System;
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
    public int Encoder_CPU_Max;
    public int System__CPU_Max;
    public double ReadLimit_MiBsec;

    public int Encoder_MultipleRun;
    public string EncoderNames;

    public List<Client_WriteStdin> PresetEncoder;


    //設定ファイル名
    private static readonly string
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location,
            AppDir = Path.GetDirectoryName(AppPath),
            AppName = Path.GetFileNameWithoutExtension(AppPath),
            Default_XmlName = AppName + ".xml",
            Default_XmlPath = Path.Combine(AppDir, Default_XmlName)
            ;

    /// <summary>
    /// constructor
    /// </summary>
    public Setting_File()
    {
      //初期設定
      Encoder_CPU_Max = 20;
      System__CPU_Max = 80;
      Encoder_MultipleRun = 1;
      EncoderNames = "  ffmeg   x264   x265  ";
      ReadLimit_MiBsec = 10.0;
      PresetEncoder = new List<Client_WriteStdin>();
    }


    /// <summary>
    /// 設定ファイルを読み込む
    /// </summary>
    /// <param name="xmlpath">読込むファイルを指定</param>
    public static Setting_File LoadFile(string xmlpath = null)
    {
      //デフォルト名を使用
      if (xmlpath == null)
      {
        xmlpath = Default_XmlPath;

        if (File.Exists(xmlpath) == false)
        {
          //設定ファイル作成
          var def_Setting = Sample_RunTest();
          XmlRW.Save(xmlpath, def_Setting);
        }
      }

      var file = XmlRW.Load<Setting_File>(xmlpath);

      XmlRW.Save(xmlpath, file);                //古いバージョンのファイルなら新たに追加された項目がxmlに加わる。


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
          BaseArgs1 = "  -i pipe:0  -threads 1                                                        ",
          BaseArgs2 = "  -vcodec libxvid  -s 160x120 -b:v 128k -acodec libmp3lame -ar 48000 -b:a 64k  ",
          BaseArgs3 = "  -y  \"$fPathWithoutExt$.avi\"                                                  ",
        },

        new Client_WriteStdin()
        {
          memo= "  動作確認  x264  ",
          Name = "  RunTest_mp4  ",
          BasePath = @"   .\ffmpeg.exe   ",
          BaseArgs1 = "  -i pipe:0  -threads 1                 ",
          BaseArgs2 = "  -vcodec libx264  -crf 40  -s 160x120  ",
          BaseArgs3 = "  -y  \"$fPathWithoutExt$.mp4\"           ",
        },

        ////new Client_WriteStdin()
        ////{
        ////  memo= "  xvid 256k  ",
        ////  sName = "  RunTest_avi_2  ",
        ////  sBasePath = @"   .\ffmpeg.exe   ",
        ////  sBaseArgs1 = "  -i pipe:0  -threads 1                                                        ",
        ////  sBaseArgs2 = "  -vcodec libxvid  -s 320x240 -b:v 256k -acodec libmp3lame -ar 48000 -b:a 64k  ",
        ////  sBaseArgs3 = "  -y  \"$fPathWithoutExt$.avi\"                                                  ",
        ////},

        ////new Client_WriteStdin()
        ////{
        ////  memo= "  copy to file  ",
        ////  sName = "  copy  ",
        ////  sBasePath = @"  .\Pipe2File.exe   ",
        ////  sBaseArgs1 = "  \"$fPath$.Pipe2File_copy\"     ",
        ////},   
    
        ////new Client_WriteStdin()
        ////{
        ////  memo= "  readonly  ",
        ////  sName = "  readonly  ",
        ////  sBasePath = @"  .\pipe2File.exe   ",
        ////},   

      };
      return setting;
    }














  }//class
}