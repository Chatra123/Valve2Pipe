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
  static class Log
  {
    static private bool Enable = true;
    static StreamWriter writer;

    public static void Close()
    {
      if (writer != null)
        writer.Close();
    }

    private static StreamWriter CreateWriter(string filename)
    {
      try
      {
        var logfile = new FileInfo(filename);
        bool append = logfile.Exists && logfile.Length <= 64 * 1024;  //64 KB 以下なら追記
        var writer = new StreamWriter(filename, append, Encoding.UTF8);   //UTF-8 bom
        return writer;
      }
      catch
      {
        Enable = false;
        return null;
      }
    }

    public static void WriteLine(string line = "")
    {
      if (Enable == false) return;
      Console.Error.WriteLine(line);
      if (writer != null)
        writer = CreateWriter("log.txt");
      if (writer != null)
        writer.WriteLine(line);
    }
  }



  class Program
  {
    static void Main(string[] args)
    {
      ////テスト引数
      //var testArgs = new List<string>();
      //testArgs.Add(@"-file");
      //testArgs.Add(@"E:\TS_Samp\t30s.ts");
      ////testArgs.Add(@"-stdout");
      //testArgs.Add(@"-profile");
      //testArgs.Add(@"  RunTest_mp4  ");
      //args = testArgs.ToArray();

      //例外を捕捉する
      AppDomain.CurrentDomain.UnhandledException += OctNov.Excp.ExceptionInfo.OnUnhandledException;

      string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
      string AppDir = System.IO.Path.GetDirectoryName(AppPath);
      Directory.SetCurrentDirectory(AppDir);

      Log.WriteLine();
      Log.WriteLine("----------------------");
      Log.WriteLine("[ Args ]");
      foreach (var arg in args)
        Log.WriteLine(arg);
      Log.WriteLine();


      //設定
      //  Setting_CmdLine
      Setting_CmdLine cmdline = null;
      {
        cmdline = new Setting_CmdLine();
        var get_cmdline = cmdline.Parse(args);
        if (get_cmdline == false)
        {
          Log.WriteLine("CommandLine Parse error");
          Log.Close();
          Thread.Sleep(2 * 1000);
          return;
        }
      }

      Setting_File setting_file = Setting_File.LoadFile();
      if (setting_file == null)
      {
        Console.Error.WriteLine("fail to read xml");
        Thread.Sleep(2 * 1000);
        return;
      }
      Client.Macro_SrcPath = cmdline.SrcPath;

      BinaryReader reader = null;
      {
        reader = SelectReaderWriter.GetReader(
          cmdline.IsPipeMode,
          cmdline.IsFileMode,
          cmdline.SrcPath);

        if (reader == null)
        {
          Log.WriteLine("no input");
          Log.Close();
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
        var writer = new Writer();
        int writer_pid = -1;
        {
          Log.WriteLine("[ Profile ]");
          Log.WriteLine("    cmdline  : " + cmdline.Profile);
          Log.WriteLine("  xml Profile");
          var xml_profile = setting_file.PresetEncoder.Select(enc => enc.Name.Trim()).ToList();
          foreach (string prf in xml_profile)
            Log.WriteLine("             : " + prf);

          var client = SelectReaderWriter.GetEncorderClinet(
                                            cmdline.Mode_Stdout,          //標準出力から送信するモードか？
                                            cmdline.Profile,              //指定のプロフィール名
                                            setting_file.PresetEncoder);  //設定ファイルのプロフィール一覧
          Log.WriteLine("[ RegisterClient ]");
          writer.RegisterClient(client);
          writer.Timeout = TimeSpan.FromMilliseconds(-1);
          writer_pid = cmdline.Mode_Stdout ? -1 : writer.GetPID_FirstClient();


          if (writer.HasClient == false)
          {
            Log.WriteLine("no output writer");
            Log.Close();
            Thread.Sleep(2 * 1000);
            return;
          }
        }

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
        Log.WriteLine("transport");
        while (true)
        {
          const int requestSize = 1024 * 100;
          var data = reader.ReadBytes(requestSize);
          if (data.Length == 0) break;               //ファイル終端
          sendSpeed.Update_and_Sleep(data.Length);

          writer.Write(data);
          if (writer.HasClient == false) break;
        }
        reader.Close();
        writer.Close();
        Log.WriteLine("Close");
      }
      finally
      {
        //Semaphore解放
        if (waitForReady != null)
          waitForReady.Release();
        Log.Close();
      }

    }




  }//class
}
