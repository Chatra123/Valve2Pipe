using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;



namespace Pipe2File
{
  class Program
  {
    static void Main(string[] args)
    {
      //  Mode_OutFile = true     ファイル出力
      //               = false    読込みのみ

      bool Mode_OutFile = false;
      string OutPath = "";

      if (0 < args.Count())
      {
        Mode_OutFile = true;
        OutPath = args[0];

        try
        {
          var out_fstream = new FileStream(OutPath, FileMode.Create, FileAccess.Write, FileShare.Read);
          out_fstream.Close();
        }
        catch
        {
          /*  file open error  */
          return;
        }
      }


      var reader = new BinaryReader(Console.OpenStandardInput());

      BinaryWriter writer = null;
      if (Mode_OutFile)
      {
        var out_fstream = new FileStream(OutPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        writer = new BinaryWriter(out_fstream);
      }


      while (true)
      {
        var readData = reader.ReadBytes(1000 * 10);
        if (readData == null) break;
        if (readData.Count() == 0) break;

        if (writer != null)
        {
          writer.Write(readData);
          //writer.Flush();
        }
      }

      if (reader != null) reader.Close();
      if (writer != null) writer.Close();

    }
  }
}
