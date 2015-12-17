using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;


namespace Valve2Pipe
{
  using SystemIdleMonitor;

  /// <summary>
  /// 送信速度の制御  プロセスのＣＰＵ使用率を調整するため
  /// </summary>
  class SendSpeedManager
  {
    //////log4net
    ////private static readonly log4net.ILog log =
    ////  log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


    //速度制限　最大
    readonly double Max_SendLimit;

    //現在の速度制限
    double sendlimit = 1024 * 1024 * 2.0 * 0.4;           //地デジの等速　1.8 MB/sec
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
    int tickBeginTime = 0;                       //計測開始時間


    //System Checker
    BlackProcessChecker blackChecker = null;
    ProcessBusyChecker busyChecker = null;

    //  last system check time
    int timeCheckBlack = 0, timeCheckBusy = 0;



    /// <summary>
    /// SystemChecker初期化
    /// </summary>
    public SendSpeedManager(int target_pid,
                            int process_cpu_max, int system_cpu_max,
                            double max_speed_MiBsec
                           )
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

        var SIM_setting_file = new SystemIdleMonitor.Setting_File();

        //ブラックリスト読込み
        SIM_setting_file.Load(blacklistPath,
                              Setting_BlackList_Default.Valve2Pipe);

        blackChecker = new BlackProcessChecker(SIM_setting_file.ProcessList);
      }

      //ProcessBusyChecker
      {
        //target_pid = -1 なら process_CPUに関しては評価されない。
        busyChecker = new ProcessBusyChecker(target_pid, process_cpu_max, system_cpu_max);
      }

      //Max_SendLimit  Byte/sec
      {
        Max_SendLimit = (0 < max_speed_MiBsec)
                        ? max_speed_MiBsec * 1024 * 1024
                        : 0;
      }
    }


    /// <summary>
    /// 送信量を計算、待機
    /// </summary>
    public void Update_and_Sleep(int sendsize)
    {
      //送信量　更新
      tickSendSize += sendsize;    //記録  送信速度制限用

      //ブラックプロセスが停止するまで待機
      while (true)
      {
        if (10 * 1000 < Environment.TickCount - timeCheckBlack)
          if (blackChecker.ExistBlack())
          {
            timeCheckBlack = Environment.TickCount;

            Thread.Sleep(11 * 1000);
            continue;
          }

        break;
      }

      //ＣＰＵ使用率が高いか？
      //  制限速度を調整
      if (500 < Environment.TickCount - timeCheckBusy)
      {
        timeCheckBusy = Environment.TickCount;
        const double Delta = 0.01;                //SendLimit増減値   500 msに１回 １％増減

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

          double ticklimit = SendLimit * (200.0 / 1000.0);

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


      //送信速度制限
      {
        //計測開始からの経過時間
        double tickDuration = Environment.TickCount - tickBeginTime;

        if (200 < tickDuration)      //Nmsごとにカウンタリセット
        {
          tickBeginTime = Environment.TickCount;
          tickSendSize = 0;
        }

        //送信量が制限をこえていたらsleep()
        //  送信サイズに直して比較　　not 速度
        if (0 < SendLimit)
          if (SendLimit * (200.0 / 1000.0) < tickSendSize)
          {
            //////log4net
            ////int sleep = (int)(200 - tickDuration);
            ////log.Info("        sleep = " + sleep);

            Thread.Sleep((int)(200 - tickDuration));
          }
      }

    }//func


  }//class





  //ブラックリスト　設定テキスト
  public static class Setting_BlackList_Default
  {
    public const string Valve2Pipe =
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






