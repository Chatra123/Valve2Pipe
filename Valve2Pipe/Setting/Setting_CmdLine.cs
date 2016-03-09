using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace Valve2Pipe
{
  using Mono.Options;

  /// <summary>
  /// コマンドライン引数を処理
  /// </summary>
  class Setting_CmdLine
  {
    public bool Mode_Stdout { get; private set; }      //自身の標準出力へ出力する
    public bool IsPipeMode { get; private set; }
    public bool IsFileMode { get; private set; }
    public String SrcPath { get; private set; }
    public String Profile { get; private set; }

    /// <summary>
    /// コマンドライン解析
    /// </summary>
    /// <param name="args">解析する引数</param>
    public bool Parse(string[] args)
    {
      string pipeSrc = "", fileSrc = "";

      //引数の１つ目がファイル？
      if (0 < args.Count())
        if (File.Exists(args[0]))
          fileSrc = args[0];

      //    /*Mono.Options*/
      //case insensitive
      //”オプション”　”説明”　”オプションの引数に対するアクション”を定義する。
      //OptionSet_icaseに渡すオプションは小文字で記述し、
      //オプションの最後に=をつける。 bool型ならつけない。
      var optionset = new OptionSet_icase();
      optionset
          .Add("stdout", "", (v) => Mode_Stdout = v != null)
          .Add("pipe=", "Input pipe src", (v) => pipeSrc = v)
          .Add("file=", "Input file", (v) => fileSrc = v)
          .Add("profile=", "", (v) => Profile = v)
          .Add("and_more", "help mes", (v) => { /*action*/ });

      try
      {
        //パース仕切れなかったコマンドラインはList<string>で返される。
        var extra = optionset.Parse(args);
      }
      catch (OptionException)
      {
        //パース失敗
        return false;
      }

      if (pipeSrc != "")
      {
        IsPipeMode = true;
        SrcPath = pipeSrc;
      }
      else if (fileSrc != "")
      {
        IsFileMode = true;
        SrcPath = fileSrc;
      }
      else
      {
        IsPipeMode = false;
        IsFileMode = false;
      }

      //ファイル名　→　フルパス
      //  ファイル名形式でないと、この後のパス変換で例外がでる
      //　ファイル名だけだと引数として渡した先で使えない。
      try
      {
        //ファイル名として使える文字列？
        var finfo = new System.IO.FileInfo(SrcPath);
        SrcPath = finfo.FullName;
      }
      catch
      {
        //パスに無効な文字が含まれています。
        SrcPath = "invalid file name";
      }

      return true;
    }
  }
}
