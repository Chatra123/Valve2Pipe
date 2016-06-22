﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;


namespace Valve2Pipe
{
  /// <summary>
  /// クライアントの標準入力に書き込む
  /// </summary>
  internal class OutputWriter
  {
    private List<Client_WriteStdin> WriterList;
    public TimeSpan Timeout = TimeSpan.FromSeconds(10);
    public bool HasWriter { get { return WriterList != null && 0 < WriterList.Count; } }

    /// <summary>
    /// ライターを閉じる
    /// </summary>
    ~OutputWriter()
    {
      Close();
    }

    public void Close()
    {
      if (HasWriter)
        foreach (var one in WriterList)
        {
          if (one != null && one.StdinWriter != null)
            one.StdinWriter.Close();
        }
    }


    /// <summary>
    /// WriterのPID取得        Valve2Pipe
    /// </summary>
    public int GetPID_FirstWriter()
    {
      if (HasWriter)
        return WriterList[0].Process.Id;
      else
        return -1;
    }


    /// <summary>
    /// ライター登録、実行
    /// </summary>
    /// <param name="srcList">実行するクライアント</param>
    /// <returns>ライターが１つ以上起動したか</returns>
    public bool RegisterWriter(List<Client_WriteStdin> srcList)
    {
      if (srcList == null) return false;

      WriterList = new List<Client_WriteStdin>(srcList);
      WriterList = WriterList.Where(client => client.IsEnable).ToList();
      WriterList.Reverse();                                 //末尾から登録するので逆順に。

      //プロセス実行
      for (int i = WriterList.Count - 1; 0 <= i; i--)
      {
        var writer = WriterList[i];

        //実行
        writer.Start_WriteStdin();

        //実行失敗
        if (writer.StdinWriter == null) { WriterList.Remove(writer); continue; }
      }

      return HasWriter;
    }


    /// <summary>
    /// ファイル出力ライターの登録  デバッグ用
    /// </summary>
    public void Register_OutFileWriter(string path)
    {
      WriterList = WriterList ?? new List<Client_WriteStdin>();
      WriterList.Add(new Client_OutFile(path));
    }


    /// <summary>
    /// データを書込み
    /// </summary>
    /// <param name="writeData">書き込むデータ</param>
    /// <returns>全てのクライアントに正常に書き込めたか</returns>
    public bool WriteData(byte[] writeData)
    {
      var tasklist = new List<Task<bool>>();

      //タスク作成、各プロセスに書込み
      foreach (var oneWriter in WriterList)
      {
        var writeTask = Task<bool>.Factory.StartNew((arg) =>
        {
          var writer = (Client_WriteStdin)arg;
          try
          {
            if (writer.Process.HasExited == false)
            {
              //書
              writer.StdinWriter.Write(writeData);
              return true;
            }
            else
              return false;

          }
          catch (IOException)
          {
            //IOException:パイプは終了しました。
            return false;
          }

        }, oneWriter);       //引数oneWriterはtask.AsyncState経由で参照される。

        tasklist.Add(writeTask);
      }


      //全タスクが完了するまで待機
      Task.WaitAll(tasklist.ToArray(), Timeout);


      //結果の確認
      bool success = true;
      foreach (var task in tasklist)
      {
        //タスク処理が完了？
        if (task.IsCompleted)
        {
          //task完了、書込み失敗
          if (task.Result == false)
          {
            success = false;
            var writer = (Client_WriteStdin)task.AsyncState;
            writer.StdinWriter.Close();
            WriterList.Remove(writer);
          }
        }
        else
        {
          //task未完了、クライアントがフリーズor処理が長い
          success = false;
          var writer = (Client_WriteStdin)task.AsyncState;
          writer.StdinWriter.Close();
          WriterList.Remove(writer);
        }
      }

      return success;

    }//func
  }//class
}