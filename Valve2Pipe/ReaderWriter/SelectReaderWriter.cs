using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace Valve2Pipe
{
  class SelectReaderWriter
  {
    /// <summary>
    /// BinaryReader作成
    /// </summary>
    public static BinaryReader GetReader(bool pipeMode, bool fileMode, string srcPath)
    {
      if (pipeMode && File.Exists(srcPath))
      {
        return new BinaryReader(Console.OpenStandardInput());
      }

      if (fileMode && File.Exists(srcPath))
      {
        var fStream = new FileStream(srcPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return new BinaryReader(fStream);
      }

      //log
      //ファイルが無い
      if (pipeMode || fileMode)
        if (File.Exists(srcPath) == false)
        {
          Console.Error.WriteLine("not exist input file");
          Console.Error.WriteLine("  " + srcPath);
        }

      return null;
    }


    /// <summary>
    /// コマンドラインで指定されているClientを選択
    /// </summary>
    public static List<Client_WriteStdin>
      GetEncorderClinet(
                        bool mode_stdout,
                        string sel_profile,
                        List<Client_WriteStdin> presetEncorder
                        )
    {
      if (mode_stdout)
      {
        //標準出力に出力
        return new List<Client_WriteStdin> { new Client_OutStdout() };
      }
      else
      {
        //コマンドラインで指定された sel_profileを設定ファイルの presetEncorderから選択
        sel_profile = sel_profile ?? "";
        sel_profile = sel_profile.ToLower().Trim();
        presetEncorder = presetEncorder ?? new List<Client_WriteStdin>();
        //sel_profileとNameが完全一致　　（前後の空白を除いた後、文字列と長さが一致）
        var encorder = presetEncorder
                        .Where((client) => 0 == client.Name.ToLower().Trim().IndexOf(sel_profile))
                        .Where((client) => sel_profile.Length == client.Name.ToLower().Trim().Length)
                        .ToList();
        encorder = encorder.Take(1).ToList();
        return encorder;
      }
    }



  }
}


