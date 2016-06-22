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
      AppDomain.CurrentDomain.UnhandledException += OctNov.Excp.ExceptionInfo.OnUnhandledException;


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
      Setting_File setting_file = Setting_File.LoadFile();

      //Clientのマクロを設定
      Client.Macro_SrcPath = cmdline.SrcPath;


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


      LGLauncher.WaitForSystemReady waitForReady = null;
      try
      {
        //エンコーダープロセス数のチェック
        //  ffmpeg、x264が動作していたら終了するまで待機
        {
          int multi = setting_file.Encoder_MultipleRun;
          var encorderNames = setting_file.EncoderNames
                                          .Split()                        //スペースで分割
                                          .Where(ext => string.IsNullOrWhiteSpace(ext) == false)
                                          .Distinct()
                                          .ToList();
          //Semaphore取得
          waitForReady = new LGLauncher.WaitForSystemReady();
          waitForReady.GetReady(encorderNames,
                                multi,
                                false);
        }


        //Encoder起動
        var writer = new OutputWriter();
        int writer_pid = -1;
        {
          var client = SelectReaderWriter.GetEncorderClinet(
                                                cmdline.Mode_Stdout,          //標準出力から送信するモードか
                                                cmdline.Profile,              //指定のプロフィール名
                                                setting_file.PresetEncoder);  //設定ファイルのプロフィール一覧
          writer.RegisterWriter(client);
          writer.Timeout = TimeSpan.FromMilliseconds(-1);
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
          double limit = setting_file.ReadLimit_MiBsec;
          sendSpeed = new SendSpeedManager(pid, prc_CPU, sys_CPU, limit);
        }

        //
        // 転送
        //
        while (true)
        {
          const int requestSize = 1024 * 100;                //１回の読込み量
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
      finally
      {
        //Semaphore解放
        if (waitForReady != null)
          waitForReady.Release();
      }

    }




  }//class
}
