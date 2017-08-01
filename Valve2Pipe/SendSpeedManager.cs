using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using SIM = SystemIdleMonitor;

namespace Valve2Pipe
{
  /// <summary>
  /// 送信速度の制御  プロセスのＣＰＵ使用率を調整するため
  /// </summary>
  class SendSpeedManager
  {
    //////log4net
    ////private static readonly log4net.ILog log =
    ////  log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    //制限速度　最大
    readonly double Max_SendLimit;
    //現在の制限速度
    double sendlimit = 1024 * 1024 * 2.0 * 0.4;           //参考：　地デジの等速　1.8 MB/sec
    double SendLimit
    {
      get { return sendlimit; }
      set
      {
        sendlimit = value;
        if (0 < Max_SendLimit)
          sendlimit = (Max_SendLimit < sendlimit) ? Max_SendLimit : sendlimit;
        sendlimit = (sendlimit <= 0) ? 1 : sendlimit;
      }
    }
    //速度計算用
    double tickSendSize = 0;                     //単位時間の送信量
    DateTime tickBeginTime;                      //計測開始時間

    SIM.BlackProcessChecker blackChecker = null;
    SIM.BusyProcessChecker busyChecker = null;
    DateTime timeCheckBlack, timeCheckBusy;

    /// <summary>
    /// SystemChecker初期化
    /// </summary>
    public SendSpeedManager(int sys_cpu_max,
                            int prc_cpu_max, int prc_pid,
                            double limit_MiBsec)
    {
      //BlackProcessChecker
      {
        string blacklistPath;
        {
          string AppPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
          string AppDir = Path.GetDirectoryName(AppPath);
          string AppName = Path.GetFileNameWithoutExtension(AppPath);
          blacklistPath = Path.Combine(AppDir, AppName + ".txt");
        }
        //ブラックリスト読込み
        var SIM_setting = new SIM.Setting_File();
        SIM_setting.Load(blacklistPath, V2P_Text.Default);
        blackChecker = new SIM.BlackProcessChecker(SIM_setting.ProcessList);
      }

      //BusyProcessChecker
      //prc_pid = -1 ならProcessのＣＰＵ使用率は評価しない。
      busyChecker = new SIM.BusyProcessChecker(sys_cpu_max, prc_cpu_max, prc_pid);
      //Byte/sec
      Max_SendLimit = 0 < limit_MiBsec
                      ? limit_MiBsec * 1024 * 1024
                      : 0;
    }


    /// <summary>
    /// 送信量を計算、待機
    /// </summary>
    public void Update_and_Sleep(int sendsize)
    {
      tickSendSize += sendsize;

      //ブラックプロセスが停止するまで待機
      while (true)
      {
        if (10 * 1000 < (DateTime.Now - timeCheckBlack).TotalMilliseconds)
          if (blackChecker.ExistBlack())
          {
            timeCheckBlack = DateTime.Now;
            Thread.Sleep(11 * 1000);
            continue;
          }
        break;
      }

      //ＣＰＵ使用率が高いか？
      //  制限速度を調整
      if (500 < (DateTime.Now - timeCheckBusy).TotalMilliseconds)
      {
        timeCheckBusy = DateTime.Now;
        const double Delta = 0.01;                //SendLimit増減値   500ms毎に１％増減

        //Is Busy ?
        if (busyChecker.IsBusy())
        {
          SendLimit -= SendLimit * Delta;
          //////log4net
          ////string limit_KBsec = (SendLimit / 1024).ToString("F3");
          ////log.Info("  --SendLimit = " + limit_KBsec + " KB/sec");
        }
        else
        {
          //＋＋
          //　送信量がSendLimitに迫っているときのみ、SendLimitを増やす。
          //　急にtickSendSizeが小さくなった後も　SendLimitが無尽蔵に大きくなるのを防止する。
          //　SendLimitが大きいと戻すのにも時間がかかる。
          //－－
          //　送信量と比べて SendLimitがかなり大きいなら減らす。
          double ticklimit = SendLimit * (200.0 / 1000.0);  // 200ms間の送信上限サイズ
          if (ticklimit * 0.95 < tickSendSize)
          {
            SendLimit += SendLimit * Delta;
            //////log4net
            ////string limit_KBsec = (SendLimit / 1024).ToString("F3");
            ////log.Info("  ++SendLimit = " + limit_KBsec + " KB/sec");
          }
          else if (tickSendSize < ticklimit * 0.70)
          {
            SendLimit -= SendLimit * Delta;
            //////log4net
            ////string limit_KBsec = (SendLimit / 1024).ToString("F3");
            ////log.Info("  --SendLimit = " + limit_KBsec + " KB/sec");
          }
        }
      }

      //速度制限
      if (0 < SendLimit)
      {
        double elapse = (DateTime.Now - tickBeginTime).TotalMilliseconds;
        if (200 < elapse)      //Nmsごとにカウンタリセット
        {
          tickBeginTime = DateTime.Now;
          tickSendSize = 0;
        }
        //送信量が制限をこえていたらsleep()
        //  送信サイズに直して比較　　not 速度
        if (SendLimit * (200.0 / 1000.0) < tickSendSize)
        {
          //////log4net
          ////int sleep = (int)(200 - tickDuration);
          ////log.Info("        sleep = " + sleep);
          int sleep = (int)(200 - elapse);
          sleep = 0 <= sleep ? sleep : 0;
          Thread.Sleep(sleep);
        }
      }

    }//func
  }//class





  //ブラックリスト　設定テキスト
  public static class V2P_Text
  {
    public const string Default =
   @"
//
//### Valve2Pipeについて
//
//  * データ送信元とエンコーダーの間に入り、転送量をシステム負荷に応じて調整します。
//
//  * このファイルで指定されたプロセス名が起動していたら転送を一時中断します。
//
//  * ffmpeg等エンコーダー名はかかないでください。
//　　自身で起動したffmpegかは判断していないのでフリーズします。
//
//
//
//### プロセスでフィルター
//
//  * プロセスのイメージ名はこのファイルの下部に書いてください。
//    イメージ名はタスクマネージャーを見てください。
//
//  * 大文字小文字の違いは無視する。
//    全角半角、ひらがなカタカナは区別する。
//
//  * 拡張子に.exeが付いていたら無視して評価します。
//
//  * ワイルドカードが使えます。
//        ０文字以上：  *        １文字：  +
//
//  * ワイルドカードを正規表現に変換しているのでnotepad++はエラーとなり使えません。
//    notepad++でなくnotepad*と指定してください。
//    他にも正規表現でエラーとなる文字列は使えません。
//
//
//
//### 文字コード
//
//  * このテキストの文字コード　UTF-8 bom
//
//
//









";
  }




}






