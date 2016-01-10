using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.IO;


namespace LGLauncher
{
  static class WaitForSystemIdle
  {
    private static Semaphore Semaphore;

    /// <summary>
    /// 同時起動数の制限
    /// </summary>
    public static bool GetReady(int multiRun, List<string> targetNames, bool check_SysIdle)
    {
      if (multiRun <= 0) return false;

      //targetNamesのプロセスがあれば終了するまで待機する。
      targetNames = targetNames.Select(
                                (prcname) =>
                                {
                                  //.exe削除
                                  prcname = prcname.Trim();
                                  bool haveExe = (Path.GetExtension(prcname).ToLower() == ".exe");
                                  prcname = (haveExe) ? Path.GetFileNameWithoutExtension(prcname) : prcname;
                                  return prcname;
                                })
                               .ToList();


      /// <summary>
      /// セマフォを取得
      ///    多重起動時の衝突回避
      /// </summary>
      /// <returns>
      ///   return semaphore;　→　セマフォ取得成功
      ///   return null; 　　　→　        取得失敗
      /// </returns>
      var GetSemaphore = new Func<Semaphore>(() =>
      {
        const int timeout_min = 60;
        //var semaphore = new Semaphore(multiRun, multiRun, "LGL-A8245043-3476");     //LGL
        var semaphore = new Semaphore(multiRun, multiRun, "V2P-491E1B11-9DC0");    //V2P

        if (semaphore.WaitOne(TimeSpan.FromMinutes(timeout_min)))
        {
          return semaphore;
        }
        else
        {
          //プロセスが強制終了されているとセマフォが解放されず取得できない。
          //一定時間でタイムアウトさせる。
          //全ての待機プロセスが終了するとセマフォがリセットされ再取得できるようになる。
          //Log.WriteLine("  timeout of waiting for semaphore");               //LGL
          return null;
        }
      });


      /// <summary>
      /// targetのプロセス数がmultiRun未満か？
      ///   target単体、外部ランチャーとの衝突回避
      /// </summary>
      var TargetHasExited = new Func<bool>(() =>
      {
        foreach (var target in targetNames)
        {
          var prclist = Process.GetProcessesByName(target);      //プロセス数確認  ".exe"はつけない
          if (multiRun <= prclist.Count())
            return false;
        }

        return true;
      });


      /// <summary>
      /// システムがアイドル状態か？
      /// </summary>
      var SystemIsIdle = new Func<bool>(() =>
      {
        //SystemIdleMonitor.exeは起動の負荷が少し高い
        //string monitor_path = Path.Combine(PathList.LSystemDir, "SystemIdleMonitor.exe");    //LGL
        string monitor_path = "disable launch";                                            //V2P

        //ファイルが無ければtrue
        if (File.Exists(monitor_path) == false) return true;

        var prc = new Process();
        {
          prc.StartInfo.FileName = monitor_path;
          prc.StartInfo.Arguments = "";
          prc.StartInfo.CreateNoWindow = true;
          prc.StartInfo.UseShellExecute = false;
          prc.Start();
          prc.WaitForExit(5 * 60 * 1000);
        }

        return prc.HasExited && prc.ExitCode == 0;
      });



      //
      //システムチェック
      //
      var rand = new Random(DateTime.Now.Millisecond + Process.GetCurrentProcess().Id);

      //セマフォ取得
      Semaphore = GetSemaphore();


      //タイムアウトなし
      while (true)
      {
        //プロセス数  チェック
        while (TargetHasExited() == false)
        {
          Thread.Sleep(60 * 1000);                                   // 1 min
        }

        //ＣＰＵ使用率
        if (check_SysIdle && SystemIsIdle() == false)
        {
          Thread.Sleep(rand.Next(5 * 60 * 1000, 6 * 60 * 1000));     // 5  to  6 min
          continue;
        }

        //セマフォが取得できない場合は追加待機
        if (Semaphore == null)
        {
          Thread.Sleep(rand.Next(0 * 1000, 60 * 1000));              // 0  to  1 min
        }

        Thread.Sleep(10 * 1000);                                     // 10 sec

        //プロセス数  再チェック
        if (TargetHasExited() == false)
          continue;

        //システムチェックＯＫ
        return true;
      }

    }


    /// <summary>
    /// セマフォ解放
    /// </summary>
    public static void ReleaseSemaphore()
    {
      if (Semaphore != null)
        Semaphore.Release();
    }
  }

}
