using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Serialization;


namespace Valve2Pipe
{

  /// <summary>
  /// クライアント
  /// </summary>
  [Serializable]
  public class Client
  {
    //マクロ用の値  簡単なのでstaticで保持
    public static string Macro_SrcPath;

    //ＸＭＬに保存する値
    public int Enable = 1;
    public string memo = "  ";
    public string Name = "  ";
    public string BasePath = "  ";
    public string BaseArgs1 = "      ";
    public string BaseArgs2 = "      ";
    public string BaseArgs3 = "      ";
    public string BaseArgs4 = "      ";
    public bool IsEnable { get { return 0 < Enable; } }
    public string FileName { get { return Path.GetFileName(BasePath).Trim(); } }

    [XmlIgnore]
    public Process Process { get; protected set; }
    [XmlIgnore]
    public BinaryWriter StdinWriter { get; protected set; }

    /// <summary>
    /// プロセス作成
    /// </summary>
    /// <returns>作成したプロセス</returns>
    protected Process CreateProcess()
    {
      if (IsEnable == false) return null;
      if (BasePath == null) return null;

      var prc = new Process();

      //Path
      string sessionPath;  //マクロ置換後のパス
      {
        sessionPath = BasePath ?? "";
        sessionPath = ReplaceMacro(sessionPath);
        sessionPath = sessionPath.Trim();
        if (string.IsNullOrEmpty(sessionPath))
          return null;                               //パスが無効
      }
      //Args
      string sessionArgs;  //マクロ置換後の引数
      {
        BaseArgs1 = BaseArgs1 ?? "";
        BaseArgs2 = BaseArgs2 ?? "";
        BaseArgs3 = BaseArgs3 ?? "";
        BaseArgs4 = BaseArgs4 ?? "";
        sessionArgs = BaseArgs1 + BaseArgs2 + BaseArgs3 + BaseArgs4;
        sessionArgs = ReplaceMacro(sessionArgs);
        sessionArgs = sessionArgs.Trim();
      }

      prc.StartInfo.FileName = sessionPath;
      prc.StartInfo.Arguments = sessionArgs;

      Log.WriteLine("  " + FileName);
      Log.WriteLine("      BasePath  :" + BasePath);
      Log.WriteLine("      BaseArgs1 :" + BaseArgs1);
      Log.WriteLine("      BaseArgs2 :" + BaseArgs2);
      Log.WriteLine("      BaseArgs3 :" + BaseArgs3);
      Log.WriteLine("      BaseArgs4 :" + BaseArgs4);
      Log.WriteLine("          Path  :" + sessionPath);
      Log.WriteLine("          Args  :" + sessionArgs);
      Log.WriteLine("                :");
      Log.WriteLine();
      return prc;
    }



    /// <summary>
    /// パス、引数のマクロを置換
    /// </summary>
    protected string ReplaceMacro(string before)
    {
      if (string.IsNullOrEmpty(before)) return before;

      string after = before;
      /*
       * ファイルパス　（フルパス）       $FilePath$               C:\rec\news.ts
       * フォルダパス  （最後に\は無し）  $FolderPath$             C:\rec
       * ファイル名    （拡張子無し）     $FileName$               news
       * 拡張子                           $Ext$                    .ts
       * ファイル名    （拡張子付き）　   $FileNameExt$            news.ts
       * ファイルパス  （拡張子無し）　   $FilePathWithoutExt$     C:\rec\news
       */
      //パス
      {
        Macro_SrcPath = Macro_SrcPath ?? "";
        string filePath = Macro_SrcPath;
        string folderPath = Path.GetDirectoryName(filePath);
        string fileName = Path.GetFileNameWithoutExtension(filePath);
        string ext = Path.GetExtension(filePath);
        string fileNameExt = Path.GetFileName(filePath);
        string filePathWithoutExt = Path.Combine(folderPath, fileName);
        after = Regex.Replace(after, @"\$FilePath\$", filePath, RegexOptions.IgnoreCase);
        after = Regex.Replace(after, @"\$FolderPath\$", folderPath, RegexOptions.IgnoreCase);
        after = Regex.Replace(after, @"\$FileName\$", fileName, RegexOptions.IgnoreCase);
        after = Regex.Replace(after, @"\$Ext\$", ext, RegexOptions.IgnoreCase);
        after = Regex.Replace(after, @"\$FileNameExt\$", fileNameExt, RegexOptions.IgnoreCase);
        after = Regex.Replace(after, @"\$FilePathWithoutExt\$", filePathWithoutExt, RegexOptions.IgnoreCase);
      }
      return after;
    }

  }



  /// <summary>
  /// 標準入力をリダイレクトするクライアント
  /// </summary>
  [Serializable]
  public class Client_WriteStdin : Client
  {
    /// <summary>
    /// プロセス実行  標準入力に書き込む
    /// </summary>
    /// <returns>プロセスが実行できたか</returns>
    public bool Start_WriteStdin()
    {
      var prc = CreateProcess();
      if (prc == null) return false;

      Process = prc;

      //シェルコマンドを無効に、入出力をリダイレクトするなら必ずfalseに設定
      Process.StartInfo.UseShellExecute = false;
      //入出力のリダイレクト
      Process.StartInfo.RedirectStandardInput = true;
      Process.StartInfo.RedirectStandardOutput = false;
      //  CreateLwiのバッファが詰まるのでfalse or 非同期で取り出す。
      //　falseだとコンソールに表示されるので非同期で取り出して捨てる。
      Process.StartInfo.RedirectStandardError = true;
      //標準エラーを取り出す。
      Process.ErrorDataReceived += (o, e) =>
      {
        //Valve2Pipeはそのまま表示する
        if (e.Data != null)
          Console.Error.WriteLine(e.Data);
      };

      //プロセス実行
      bool launch;
      try
      {
        launch = Process.Start();
        StdinWriter = new BinaryWriter(Process.StandardInput.BaseStream);      //同期　　書き込み用ライター
        Process.BeginErrorReadLine();                                          //非同期　標準エラーを取得
      }
      catch
      {
        launch = false;
      }
      return launch;
    }
  }




  #region Client_Out

  /// <summary>
  /// クライアント　Stdoutに出力        Valve2Pipe
  ///   StdinWriterに書き込まれたら自身のStandardOutputに出力
  /// </summary>
  public class Client_OutStdout : Client_WriteStdin
  {
    public Client_OutStdout()
    {
      Enable = 1;
      //ダミーのProcessを割り当てる。プロセスの生存チェック回避用
      Process = Process.GetCurrentProcess();
      StdinWriter = new BinaryWriter(Console.OpenStandardOutput());
    }
  }


  /// <summary>
  /// クライアント　ファイルに出力　　デバッグ用
  /// 　　StdinWriterに書き込まれたらそのままファイルに書く
  /// </summary>
  public class Client_OutFile : Client_WriteStdin
  {
    public Client_OutFile(string filepath)
    {
      Enable = 1;
      //ダミーのProcessを割り当てる。プロセスの生存チェック回避用
      Process = Process.GetCurrentProcess();
      try
      {
        var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Read);
        StdinWriter = new BinaryWriter(stream);
      }
      catch
      {
        throw new IOException("Client_OutFileの作成に失敗。ファイル出力先パスを確認。");
      }
    }



  }

  #endregion Client_OutFile





}