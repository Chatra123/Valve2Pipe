﻿using System;
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

    static string TsPath, TsDir, TsName;
    static string AviPath, AviShortName, AviName, AviExt;
    static string CutAvi_ShortPath, CutAvi_Name;

    static string[] ExtList = { ".avi", ".mp4" };


    static void Main(string[] args)
    {
      ////test args
      //args = new string[] { @"E:\TS_PFDebug\n60s.ts" };
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


      //frame読
      var framePath = Path.Combine(TsDir, TsName + ".frame.txt");
      var frameList = Read_FrameFile(framePath);

      if (frameList == null)
      {
        Console.Error.WriteLine("invalid frameList");
        Thread.Sleep(2000);
        return;
      }


      //bat作成
      string batPath;
      {
        batPath = Path.Combine(TsDir, AviShortName + ".split_cat.bat");

        var textList = CreateBatText(frameList);

        //List<string>  →  string
        string batText = "";
        textList.ForEach((line) => { batText += line + Environment.NewLine; });

        //batはshift-jisで保存
        //UTF-8で保存すると実行時に日本語ファイルが取り扱えない。
        File.WriteAllText(batPath, batText, Encoding.GetEncoding("Shift_JIS"));
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
      TsName = Path.GetFileNameWithoutExtension(TsPath);

      //Avi
      foreach (var ext in ExtList)
      {
        string path = Path.Combine(TsDir, TsName + ext);
        if (File.Exists(path))
        {
          AviPath = path;
          AviExt = ext;
          break;
        }
        else
          AviPath = "not found";
      }

      AviName = TsName;

      //ShortName  作業用のファイル名
      string timecode = DateTime.Now.ToString("mmssff");
      string pid = Process.GetCurrentProcess().Id.ToString();

      AviShortName = new Regex("[ $|()^　]").Replace(TsName, "_");      //batの特殊文字　置換
      AviShortName = (5 < AviShortName.Length) ? AviShortName.Substring(0, 5) : AviShortName;
      AviShortName = AviShortName + "_" + timecode + "_" + pid;


      //cat video
      CutAvi_ShortPath = Path.Combine(TsDir, AviShortName + ".cut" + AviExt);
      CutAvi_Name = TsName + ".cut" + AviExt;

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
        batText = FileR.ReadFromResource("SplitVideo.Resource.SplitVideo.bat");

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
            line = Regex.Replace(line, @"\$AviName\$", AviName, RegexOptions.IgnoreCase);
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

          batText[i] = line;
        }
      }
      return batText;
    }



    /// <summary>
    /// read *.frame.txt  -->  List<int>
    /// </summary>
    /// <returns>
    /// 取得成功  -->  List<int>
    /// 　　失敗  -->  null
    /// </returns>
    public static List<int> Read_FrameFile(string framePath)
    {
      //check
      if (File.Exists(framePath) == false) return null;

      //読
      var text = FileR.ReadAllLines(framePath);
      if (text == null) return null;

      //コメント削除、トリム
      text = text.Select(
        (line) =>
        {
          int found = line.IndexOf("//");
          line = (0 <= found) ? line.Substring(0, found) : line;
          return line.Trim();
        })
        .Where((line) => string.IsNullOrWhiteSpace(line) == false)    //空白行削除
        .ToList();


      //List<string>  -->  List<int>
      List<int> frameList;
      try
      {
        frameList = text.Select(line => int.Parse(line)).ToList();
      }
      catch
      {
        frameList = null;  //変換失敗
      }

      //check
      if (frameList == null) return null;
      if (frameList.Count % 2 == 1) return null;
      return frameList;
    }


















  }
}
