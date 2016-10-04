using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LGLauncher
{
  /// <summary>
  /// ミューテックスの取得
  /// </summary>
  interface IMutexControl
  {
    bool HasControl { get; }
    void Initilize(string name, int multiple = 1);
    bool Get();
    void Release();
  }


  /// <summary>
  /// ミューテックス　  １つのみ取得可
  /// </summary>
  class MutexControl : IMutexControl
  {
    Mutex hMutex;
    string MutexName;
    public bool HasControl { get; private set; }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initilize(string name, int count = 1)
    {
      MutexName = name;
      //ミューテックスのmaxCountは常に１
    }

    /// <summary>
    /// ミューテックス取得まで待機
    /// </summary>
    public bool Get()
    {
      if (HasControl) return true;
      if (string.IsNullOrEmpty(MutexName)) throw new Exception();

      hMutex = new System.Threading.Mutex(false, MutexName);
      GC.KeepAlive(hMutex);       //ガベージコレクション対象から除外

      // Mutex のシグナルを受信できるまで待機
      //　プロセスが強制終了されても基本的には自動で解放される。
      try
      {
        if (hMutex.WaitOne())
          HasControl = true;
      }
      catch (AbandonedMutexException)
      {
        //別のスレッドが解放せずに放棄した Mutexを取得した
        HasControl = false;
      }

      return HasControl;
    }

    /// <summary>
    /// Release
    /// </summary>
    public void Release()
    {
      if (hMutex != null && HasControl)
      {
        hMutex.ReleaseMutex();
        HasControl = false;
      }
      if (hMutex != null)
      {
        hMutex.Close();
        hMutex = null;
      }
    }

    /// <summary>
    /// destructor
    /// </summary>
    ~MutexControl()
    {
      Release();
    }
  }


  /// <summary>
  /// セマフォ 　 複数取得可
  /// </summary>
  class SemaphoreControl : IMutexControl
  {
    Semaphore hSemaphore;
    string SemaphoreName;
    int MaxCount;
    public bool HasControl { get; private set; }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initilize(string name, int count = 1)
    {
      SemaphoreName = name;
      MaxCount = count;
    }

    /// <summary>
    /// セマフォ取得
    /// </summary>
    public bool Get()
    {
      if (HasControl) return true;
      if (string.IsNullOrEmpty(SemaphoreName)) throw new Exception();

      const int timeout_min = 120;
      hSemaphore = new Semaphore(MaxCount, MaxCount, SemaphoreName);
      if (hSemaphore.WaitOne(TimeSpan.FromMinutes(timeout_min)))
      {
        HasControl = true;
      }
      else
      {
        //プロセスが強制終了されているとセマフォが解放されず取得できない。
        //一定時間でタイムアウトさせる。
        //全ての待機プロセスが終了するとセマフォがリセットされ再取得できるようになる。
        HasControl = false;
      }
      return HasControl;
    }


    /// <summary>
    /// Release
    /// </summary>
    public void Release()
    {
      if (hSemaphore != null && HasControl)
      {
        hSemaphore.Release();
        HasControl = false;
      }
      if (hSemaphore != null)
      {
        hSemaphore.Close();
        hSemaphore = null;
      }
    }


    /// <summary>
    /// destructor
    /// </summary>
    ~SemaphoreControl()
    {
      Release();
    }
  }


}
