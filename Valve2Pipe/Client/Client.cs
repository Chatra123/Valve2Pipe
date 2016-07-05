using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Valve2Pipe
{
  /// <summary>
  /// 出力用クライアント
  /// </summary>
  [Serializable]
  public class Client
  {
    //マクロ用の値  簡単なのでstaticで保持
    public static string Macro_SrcPath;

    //ＸＭＬに保存する値
    public int Enable = 1;
    public string memo = "      ";
    public string Name = "  ";
    public string BasePath = "      ";
    public string BaseArgs1 = "      ";
    public string BaseArgs2 = "      ";
    public string BaseArgs3 = "      ";
    public string BaseArgs4 = "      ";

    public bool IsEnable { get { return 0 < Enable; } }
    public string FileName { get { return Path.GetFileName(BasePath).Trim(); } }
    public override string ToString()
    {
      return (string.IsNullOrWhiteSpace(Name) == false) ? Name : FileName;
    }

    [XmlIgnore]
    public Process Process { get; protected set; }

    [XmlIgnore]
    public BinaryWriter StdinWriter { get; protected set; }

    /// <summary>
    /// プロセス作成
    /// </summary>
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
        if (string.IsNullOrWhiteSpace(sessionPath))
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
      return prc;
    }


    /// <summary>
    /// 引数のマクロを置換
    /// </summary>
    protected string ReplaceMacro(string before)
    {
      if (string.IsNullOrEmpty(before)) return before;

      string after = before;

      /*
       * r12からEDCBと同じマクロ名に変更
       * 
       * ファイルパス　（フルパス）       $fPath$           --> $FilePath$               C:\rec\news.ts
       * フォルダパス  （最後に\は無し）  $fDir$            --> $FolderPath$             C:\rec
       * ファイル名    （拡張子無し）     $fNameWithoutExt$ --> $FileName$               news
       * 拡張子                           none                  $Ext$                    .ts
       * ファイル名    （拡張子付き）　   $fName$           --> $FileNameExt$            news.ts
       * ファイルパス  （拡張子無し）　   $fPathWithoutExt$ --> $FilePathWithoutExt$     C:\rec\news
       */
      //パス　（r12から）
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

      //パス  （r11まで）
      {
        Macro_SrcPath = Macro_SrcPath ?? "";
        string fPath = Macro_SrcPath;
        string fDir = Path.GetDirectoryName(fPath);
        string fNameWithoutExt = Path.GetFileNameWithoutExtension(fPath);
        string fName = Path.GetFileName(fPath);
        string fPathWithoutExt = Path.Combine(fDir, fNameWithoutExt);
        after = Regex.Replace(after, @"\$fPath\$", fPath, RegexOptions.IgnoreCase);
        after = Regex.Replace(after, @"\$fDir\$", fDir, RegexOptions.IgnoreCase);
        after = Regex.Replace(after, @"\$fNameWithoutExt\$", fNameWithoutExt, RegexOptions.IgnoreCase);
        after = Regex.Replace(after, @"\$fName\$", fName, RegexOptions.IgnoreCase);
        after = Regex.Replace(after, @"\$fPathWithoutExt\$", fPathWithoutExt, RegexOptions.IgnoreCase);
      }

      return after;
    }

  }



  /// <summary>
  /// 標準入力への出力用クライアント
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
      //Client_OutStdoutは既にダミープロセスを割り当て済み。
      //this.Processに直接いれず、prcを経由する。
      var prc = CreateProcess();
      if (prc == null) return false;               //Process起動失敗

      Process = prc;

      //シェルコマンドを無効に、入出力をリダイレクトするなら必ずfalseに設定
      Process.StartInfo.UseShellExecute = false;

      //入出力のリダイレクト
      //標準入力
      Process.StartInfo.RedirectStandardInput = true;

      //標準出力
      Process.StartInfo.RedirectStandardOutput = false;

      //標準エラー
      //  バッファが詰まるのでfalse or 非同期で取り出す
      //　falseだとコンソールに表示される
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
      catch (Exception)
      {
        launch = false;
      }
      return launch;
    }
  }






  #region Client_Out

  /// <summary>
  /// クライアント　Stdoutに出力        Valve2Pipe
  /// </summary>
  public class Client_OutStdout : Client_WriteStdin
  {
    public Client_OutStdout()
    {
      Enable = 1;
      Name = "OutStdout";
      //ダミーのProcessを割り当てる。プロセスの生存チェック回避
      Process = Process.GetCurrentProcess();

      StdinWriter = new BinaryWriter(Console.OpenStandardOutput());
    }
  }


  /// <summary>
  /// クライアント　ファイルに出力　　デバッグ用
  /// </summary>
  public class Client_OutFile : Client_WriteStdin
  {
    public Client_OutFile(string filepath)
    {
      Enable = 1;

      //ダミーのProcessを割り当てる。プロセスの生存チェック回避
      Process = Process.GetCurrentProcess();
      StdinWriter = CreateOutFileWriter(filepath);
    }

    /// <summary>
    /// ファイル出力ライター作成
    /// </summary>
    private BinaryWriter CreateOutFileWriter(string filepath)
    {
      try
      {
        var stream = new FileStream(filepath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        var writer = new BinaryWriter(stream);
        return writer;
      }
      catch
      {
        throw new IOException("Client_OutFileの作成に失敗。ファイル出力先パスを確認。");
      }
    }

  }

  #endregion Client_OutFile

}