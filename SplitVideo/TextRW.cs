/*
 * 最終更新日
 *   17/06/19
 *  
 * 概要
 *   - テキストファイルの読み書きにファイル共有設定をつける。
 *     System.IO.File.ReadAllLines();は別のプロセスが使用中のファイルを読み込めなかった。
 *     
 *   - アセンブリリソースの読込み 
 *  
 *  
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace OctNov.IO
{
  /// <summary>
  /// 文字エンコード
  /// </summary>
  /// <remarks>
  ///  *.ts.program.txt        Shift-JIS
  /// 
  ///  avs, d2v, lwi, bat      Shift-JIS
  ///  vpy                     UTF8_nobom
  ///  srt                     UTF8_bom
  /// </remarks>
  internal class TextEnc
  {
    public static readonly
      Encoding Ascii = Encoding.ASCII,
               Shift_JIS = Encoding.GetEncoding("Shift_JIS"),
               UTF8_nobom = new UTF8Encoding(false),
               UTF8_bom = Encoding.UTF8
               ;
  }



  #region TextR

  /// <summary>
  /// 共有設定を付けて読み込む
  /// </summary>
  internal class TextR
  {
    /// <summary>
    /// 共有設定を付けてテキストを読込む
    /// </summary>
    public static List<string> ReadAllLines(string path, Encoding enc = null)
    {
      enc = enc ?? TextEnc.Shift_JIS;
      try
      {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          if (100 * 1024 * 1024 < stream.Length)// greater than 100 MB
            throw new Exception("read large file");
          using (var reader = new StreamReader(stream, enc))
          {
            var text = new List<string>();
            while (!reader.EndOfStream)
              text.Add(reader.ReadLine());
            return text;
          }
        }
      }
      catch
      {
        return null;
      }
    }

    /// <summary>
    /// 共有設定を付けてバイナリファイルを読込む
    /// </summary>
    public static byte[] ReadBytes(string path)
    {
      try
      {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
          if (100 * 1024 * 1024 < stream.Length)// greater than 100 MB
            throw new Exception("read large file");
          using (var reader = new BinaryReader(stream))
          {
            var data = new List<byte>();
            while (true)
            {
              byte[] d = reader.ReadBytes(32 * 1024);
              if (d.Count() == 0)
                break;
              else
                data.AddRange(d);
            }
            return data.ToArray();
          }
        }
      }
      catch
      {
        return null;
      }
    }


    /// <summary>
    /// アセンブリ内のリソース読込み
    /// </summary>
    /// <remarks>
    /// リソースが存在しないとnew StreamReader(null,enc)で例外
    /// bat, avs        Shift-JIS
    /// vpy             UTF8_nobom
    /// </remarks>
    public static List<string> ReadFromResource(string name, Encoding enc = null)
    {
      enc = enc ?? TextEnc.Shift_JIS;
      //マニフェストリソースからファイルオープン
      var assembly = Assembly.GetExecutingAssembly();
      var reader = new StreamReader(assembly.GetManifestResourceStream(name), enc);
      //read
      var text = new List<string>();
      while (!reader.EndOfStream)
        text.Add(reader.ReadLine());
      reader.Close();
      return text;
    }


    //=====================================
    // lwi読込み用
    //=====================================
    public bool IsOpen { get { return reader != null; } }
    private FileStream fstream;
    private StreamReader reader;

    /// <summary>
    /// Constructor
    /// </summary>
    public TextR(string path, Encoding enc = null)
    {
      enc = enc ?? TextEnc.Shift_JIS;
      try
      {
        fstream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        reader = new StreamReader(fstream, enc);
      }
      catch { /* do nothing */ }
    }
    ~TextR()
    {
      Close();
    }

    /// <summary>
    /// Close
    /// </summary>
    public void Close()
    {
      if (reader == null)
        return;
      reader.Close();
    }

    /// <summary>
    /// Ｎ行読込む
    /// </summary>
    /// <param name="NLines">読み込む最大行数</param>
    /// <returns>
    /// 読み込んだテキスト、０～Ｎ行
    /// EOFに到達すると NLinesに満たない行数を返す。
    /// </returns>
    public List<string> ReadNLines(int NLines)
    {
      var text = new List<string>();
      for (int i = 0; i < NLines; i++)
      {
        string line = reader.ReadLine();
        if (line != null)
          text.Add(line);
        else
          break;
      }
      return text;
    }
  }

  #endregion



  #region TextW

  /// <summary>
  /// テキスト書込み
  /// </summary>
  internal class TextW
  {
    /// <summary>
    /// バイナリ追記
    /// </summary>
    public static bool AppendBytes(string path, IEnumerable<byte> data)
    {
      try
      {
        var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
        stream.Write(data.ToArray(), 0, data.Count());
        stream.Close();
        return true;
      }
      catch { return false; }
    }


    //=====================================
    // lwi書込み用
    //=====================================
    public bool IsOpen { get { return writer != null; } }
    private FileStream fstream;
    private StreamWriter writer;

    /// <summary>
    /// Constructor
    /// </summary>
    public TextW(string path, Encoding enc = null)
    {
      enc = enc ?? TextEnc.Shift_JIS;
      try
      {
        fstream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        writer = new StreamWriter(fstream, enc);
      }
      catch { /* do nothing */ }
    }
    ~TextW()
    {
      Close();
    }

    /// <summary>
    /// 閉じる
    /// </summary>
    public void Close()
    {
      if (writer == null)
        return;
      writer.Close();
    }

    /// <summary>
    /// 改行コードを"\n"に変更
    /// </summary>
    public void SetNewline_n()
    {
      writer.NewLine = "\n";
    }

    /// <summary>
    /// テキスト書込み
    /// </summary>
    public void WriteLine(string line)
    {
      writer.WriteLine(line);
    }

    /// <summary>
    /// テキスト書込み
    /// </summary>
    public void WriteLine(IEnumerable<string> text)
    {
      foreach (var line in text)
        writer.WriteLine(line);
    }

    /// <summary>
    /// バイト列の書込み
    /// </summary>
    public void WriteByte(IEnumerable<byte> data)
    {
      //FileStreamに直接書き込むので、先にStreamWriterをFlush()しなくてはいけない。
      //WriteByte()は一度しか利用しないのでこれで対応する。
      writer.Flush();
      foreach (var d in data)
        fstream.WriteByte(d);
      fstream.Flush();
    }
  }

  #endregion 

}