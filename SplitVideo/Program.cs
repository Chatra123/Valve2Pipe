using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;


namespace SplitVideo
{
  using OctNov.IO;


  class Program
  {
    //アプリパス
    private static readonly string
            AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location,
            AppDir = Path.GetDirectoryName(AppPath);

    //ファイルパス
    static string FFmpegPath, LSM_remuxerPath;

    static string TsPath, TsDir, TsNameWithoutExt;
    static string AviPath, AviShortName, AviWithoutExt, AviExt;
    static string CutAvi_ShortPath, CutAvi_Name;

    static string[] ExtList = { ".avi", ".mp4" };


    static void Main(string[] args)
    {
      ////test args
      //args = new string[] { @"E:\TS_PFDebug\b60s.ts" };
      //args[0] = args[0].Trim();

      //カレントディレクトリ
      Directory.SetCurrentDirectory(AppDir);

      //パス作成
      var errmsg = MakePath(args);
      if (errmsg != null)
      {
        Console.Error.WriteLine(errmsg);
        Thread.Sleep(2000);
        return;
      }


      //フレームファイル読込み
      var framePath = Path.Combine(TsDir, TsNameWithoutExt + ".frame.txt");
      var frameList = FrameFile_to_List(framePath);

      //　読込み失敗？
      if (frameList == null)
      {
        Console.Error.WriteLine("invalid frameList");
        Thread.Sleep(2000);
        return;
      }



      //batファイル作成
      string batPath;
      {
        batPath = Path.Combine(TsDir, AviShortName + ".split_cat.bat");

        var batText = CreateBatText(frameList);

        //１行に変換
        //  List<string>  →  string
        string batString = "";
        batText.ForEach((line) => { batString += line + Environment.NewLine; });


        //bat はshift-jisで保存
        //UTF-8で保存すると実行時に日本語ファイルが取り扱えない。
        File.WriteAllText(batPath, batString, Encoding.GetEncoding("Shift_JIS"));
      }

      //Run bat
      {
        var prc = new Process();
        prc.StartInfo.FileName = batPath;
        prc.StartInfo.CreateNoWindow = true;
        prc.StartInfo.UseShellExecute = false;
        prc.Start();
        prc.WaitForExit();
        prc.Close();
      }

      //Delete bat
      try
      {
        Thread.Sleep(1000 * 2);
        File.Delete(batPath);
      }
      catch
      {
        /* fail to delete */
      }

    }



    /// <summary>
    /// パス作成
    /// </summary>
    /// <param name="args"></param>
    /// <returns>
    /// file check
    /// success →  null
    /// fail  　→  エラーメッセージ
    /// </returns>
    private static string MakePath(string[] args)
    {
      //コマンドラインチェック
      if (args.Count() == 0)
      {
        return "not found args[0]";
      }

      try
      {
        var name = Path.GetFileName(args[0]);
      }
      catch (ArgumentException)
      {
        //無効な文字を 1 つ以上含んでいます
        return "args[0] contains invalid charactor";
      }


      //ffmpeg
      FFmpegPath = Path.Combine(AppDir, "ffmpeg.exe");

      //remuxer
      LSM_remuxerPath = Path.Combine(AppDir, "remuxer.exe");

      //Ts
      TsPath = args[0];
      TsDir = Path.GetDirectoryName(TsPath);
      TsNameWithoutExt = Path.GetFileNameWithoutExtension(TsPath);

      //Avi
      foreach (var ext in ExtList)
      {
        string path = Path.Combine(TsDir, TsNameWithoutExt + ext);
        if (File.Exists(path))
        {
          AviPath = path;
          AviExt = ext;
          break;
        }
        else
          AviPath = "not found";
      }

      AviWithoutExt = TsNameWithoutExt;

      //ShortName  作業用のファイル名
      string timecode = DateTime.Now.ToString("mmssff");
      string pid = Process.GetCurrentProcess().Id.ToString();

      AviShortName = new Regex("[ $|()^　]").Replace(TsNameWithoutExt, "_");      //batの特殊文字　置換
      AviShortName = (5 < AviShortName.Length) ? AviShortName.Substring(0, 5) : AviShortName;
      AviShortName = AviShortName + "_" + timecode + "_" + pid;


      //cat video
      CutAvi_ShortPath = Path.Combine(TsDir, AviShortName + ".cut" + AviExt);
      CutAvi_Name = TsNameWithoutExt + ".cut" + AviExt;


      //show path
      Console.Error.WriteLine("TsPath    =" + TsPath);
      Console.Error.WriteLine("AviPath   =" + AviPath);
      Console.Error.WriteLine("ShortName =" + AviShortName);
      Console.Error.WriteLine();
      Console.Error.WriteLine();


      //ファイルチェック
      //  Video
      if (File.Exists(AviPath) == false)
      {
        return "not found Video";
      }

      //　FFmpeg
      if (File.Exists(FFmpegPath) == false)
      {
        return "not found FFmpeg";
      }

      //all OK
      return null;
    }




    /// <summary>
    /// フレームリスト　→　バッチテキスト
    /// <summary>
    private static List<string> CreateBatText(List<int> frameList)
    {

      //フレームリスト　→　開始、終了秒数
      var BeginSec = new List<int>();
      var EndSec = new List<int>();
      var DurationSec = new List<int>();
      var PartCount = frameList.Count / 2;

      for (int i = 0; i < frameList.Count; i += 2)
      {
        int startFrame = frameList[i];
        int endFrame = frameList[i + 1];
        double startSec = 1.0 * frameList[i] / 29.970;
        double endSec = 1.0 * frameList[i + 1] / 29.970;
        double durSec = endSec - startSec + 1;

        BeginSec.Add((int)startSec);
        EndSec.Add((int)endSec);
        DurationSec.Add((int)durSec);
      }


      //バッチテキスト
      var batText = new List<string>();
      {
        //Resource読込み
        batText = FileR.ReadFromResource("SplitVideo.ResourceText.BaseSplitVideo.bat");

        //置換
        for (int i = 0; i < batText.Count; i++)
        {
          var line = batText[i];

          //FFmpeg  L-Smash remuxer
          {
            line = Regex.Replace(line, @"\$ffmpeg\$", FFmpegPath, RegexOptions.IgnoreCase);
            line = Regex.Replace(line, @"\$remuxer\$", LSM_remuxerPath, RegexOptions.IgnoreCase);
          }

          //input
          {
            line = Regex.Replace(line, @"\$PartCount\$", "" + PartCount, RegexOptions.IgnoreCase);
            line = Regex.Replace(line, @"\$ext\$", AviExt, RegexOptions.IgnoreCase);

            line = Regex.Replace(line, @"\$AviPath\$", AviPath, RegexOptions.IgnoreCase);
            line = Regex.Replace(line, @"\$AviShort\$", AviShortName, RegexOptions.IgnoreCase);

            line = Regex.Replace(line, @"\$AviWithoutExt\$", AviWithoutExt, RegexOptions.IgnoreCase);

            //Rename CutAvi
            line = Regex.Replace(line, @"\$CutAvi_ShortPath\$", CutAvi_ShortPath, RegexOptions.IgnoreCase);
            line = Regex.Replace(line, @"\$CutAvi_Name\$", CutAvi_Name, RegexOptions.IgnoreCase);
          }

          //part LineBlocker
          for (int partNo = 1; partNo <= PartCount; partNo++)
          {
            string lineBlocker = "::part" + partNo + "::";
            line = Regex.Replace(line, lineBlocker, "", RegexOptions.IgnoreCase);
          }

          //BeginSec
          for (int partNo = 1; partNo <= PartCount; partNo++)
          {
            int idx = partNo - 1;
            string begin_sec = @"\$BeginSecP" + partNo + @"\$";
            string end_sec = @"\$EndSecP" + partNo + @"\$";
            string duration_sec = @"\$DurSecP" + partNo + @"\$";

            line = Regex.Replace(line, begin_sec, "" + BeginSec[idx], RegexOptions.IgnoreCase);
            line = Regex.Replace(line, end_sec, "" + EndSec[idx], RegexOptions.IgnoreCase);
            line = Regex.Replace(line, duration_sec, "" + DurationSec[idx], RegexOptions.IgnoreCase);
          }

          //set replaced line
          batText[i] = line;
        }
      }

      return batText;
    }



    /// <summary>
    /// File  →  List<int>
    /// </summary>
    /// <param name="framePath">フレームファイルパス</param>
    /// <returns>
    /// 取得成功　→　List<int>
    /// 　　失敗　→　null
    /// </returns>
    private static List<int> FrameFile_to_List(string framePath)
    {
      //List<string>  -->  List<int>
      var ConvertToIntList = new Func<List<string>, List<int>>(
        (stringList) =>
        {
          var intList = new List<int>();
          int result;

          stringList = stringList.Select(
                                  (line) =>
                                  {
                                    //コメント削除、トリム
                                    int found = line.IndexOf("//");
                                    line = (0 <= found) ? line.Substring(0, found) : line;
                                    line = line.Trim();
                                    return line;
                                  })
                                  .Where((line) => string.IsNullOrWhiteSpace(line) == false)    //空白行削除
                                  .Distinct()                                                   //重複削除
                                  .ToList();

          foreach (var line in stringList)
          {
            if (int.TryParse(line, out result) == false) return null;          //変換失敗
            intList.Add(result);
          }

          return intList;
        });

      //読込み
      if (File.Exists(framePath) == false) return null;    //ファイルチェック
      var frameText = FileR.ReadAllLines(framePath);       //List<string>でファイル取得
      if (frameText == null) return null;

      //List<int>に変換
      var frameList = ConvertToIntList(frameText);

      //エラーチェック
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;           //奇数個ならエラー
      return frameList;
    }




















  }
}
