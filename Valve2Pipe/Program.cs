using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Xml.Serialization;

namespace Valve2Pipe
{
  using OctNov.Excp;

  class Program
  {

    static void Main(string[] args)
    {
      ////テスト引数
      //var testArgs = new List<string>();
      //testArgs.Add(@"-File");
      //testArgs.Add(@"ac2s.ts");
      //testArgs.Add(@"-stdout");
      ////testArgs.Add(@"-profile");
      ////testArgs.Add(@"  through  ");
      //args = testArgs.ToArray();



      //例外を捕捉する
      AppDomain.CurrentDomain.UnhandledException += ExceptionInfo.OnUnhandledException;


      //設定
      //  Setting_CmdLine
      Setting_CmdLine cmdline = null;
      {
        cmdline = new Setting_CmdLine();
        var canparse = cmdline.Parse(args);

        if (canparse == false)
        {
          Console.Error.WriteLine("Command Line");
          foreach (var arg in args)
            Console.Error.WriteLine(arg);
          Console.Error.WriteLine();
          Console.Error.WriteLine("CmdLine Parse error");

          Thread.Sleep(2 * 1000);
          return;
        }
      }


      //カレントディレクトリ設定
      string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
      string AppDir = System.IO.Path.GetDirectoryName(AppPath);
      Directory.SetCurrentDirectory(AppDir);


      //  Setting_File
      Setting_File setting_file = null;
      {
        setting_file = Setting_File.LoadFile();
      }
      //　Clientのマクロを設定
      {
        Client.Macro_SrcPath = cmdline.SrcPath;
      }


      //Reader
      BinaryReader reader = null;
      {
        reader = SelectReaderWriter.GetReader(
          cmdline.IsPipeMode,
          cmdline.IsFileMode,
          cmdline.SrcPath);

        if (reader == null)
        {
          Console.Error.WriteLine("Command Line");
          foreach (var arg in args)
            Console.Error.WriteLine(arg);
          Console.Error.WriteLine();
          Console.Error.WriteLine("no input");

          Thread.Sleep(2 * 1000);
          return;
        }
      }


      try
      {
        //エンコーダープロセス数のチェック
        //  ffmpeg、x264が動作していたら終了するまで待機
        {
          int multiRun = setting_file.Encoder_MultipleRun;
          var encorderNames = setting_file.EncoderNames
                                          .Split()                        //スペース分割
                                          .Where(ext => string.IsNullOrWhiteSpace(ext) == false)
                                          .Distinct()
                                          .ToList();
          LGLauncher.WaitForSystemIdle.GetReady(multiRun, encorderNames, false);           //セマフォ取得
        }


        //Encorder 起動
        var writer = new OutputWriter();
        int writer_pid = -1;
        {
          var client = SelectReaderWriter.GetEncorderClinet(
                                                cmdline.Mode_Stdout,          //自身の標準出力から送信するモードか
                                                cmdline.Profile,              //コマンドライン指定のエンコーダー
                                                setting_file.PresetEncoder);  //設定ファイルのエンコーダーの設定一覧
          writer.RegisterWriter(client);
          writer.Timeout_msec = -1;
          writer_pid = (cmdline.Mode_Stdout) ? -1 : writer.GetPID_FirstWriter();

          if (writer.HasWriter == false)
          {
            Console.Error.WriteLine("no output writer");

            Thread.Sleep(2 * 1000);
            return;
          }
        }


        // SendSpeed
        SendSpeedManager sendSpeed;
        {
          int pid = writer_pid;
          int prc_CPU = setting_file.Encoder_CPU_Max;
          int sys_CPU = setting_file.System__CPU_Max;
          double max_speed = setting_file.ReadLimit_MiBsec;
          sendSpeed = new SendSpeedManager(pid, prc_CPU, sys_CPU, max_speed);
        }

        //
        // 転送
        //
        {
          while (true)
          {
            const int requestSize = 1024 * 100;                 //１回の読込み量

            //read
            var readData = reader.ReadBytes(requestSize);
            if (readData.Length == 0) break;                   //ファイル終端

            //送信速度　調整
            sendSpeed.Update_and_Sleep(readData.Length);

            //write
            writer.WriteData(readData);
            if (writer.HasWriter == false) break;

          }

          reader.Close();
          writer.Close();
        }

      }
      finally
      {
        LGLauncher.WaitForSystemIdle.ReleaseSemaphore();                           //セマフォ解放
      }

    }




  }//class
}
