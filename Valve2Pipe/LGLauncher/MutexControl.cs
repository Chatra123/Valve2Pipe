using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace LGLauncher
{
  /// <summary>
  /// ミューテックスの制御
  /// </summary>
  interface IMutexControl
  {
    bool HasControl { get; }
    void Initlize(string name, int multiple = 1);
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
    public void Initlize(string mutexName, int maxCount = 1)
    {
      MutexName = mutexName;
      //ミューテックスのmaxCountは常に１
    }

    /// <summary>
    /// ミューテックス取得まで待機
    /// </summary>
    public bool Get()
    {
      if (HasControl) return HasControl;
      if (string.IsNullOrEmpty(MutexName)) throw new Exception();

      hMutex = new System.Threading.Mutex(false, MutexName);
      GC.KeepAlive(hMutex);       //ガベージコレクション対象から除外

      // Mutex のシグナルを受信できるまで待機
      //　プロセスが強制終了されても基本的には自動で解放される。
      if (hMutex.WaitOne())
      {
        HasControl = true;
      }
      return HasControl;
    }

    /// <summary>
    /// Release
    /// </summary>
    public void Release()
    {
      if (hMutex != null)
      {
        HasControl = false;
        hMutex.ReleaseMutex();
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
  /// セマフォ 　 Name１つで複数（multiRun）取得可
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
    public void Initlize(string semaphoreName, int maxCount = 1)
    {
      SemaphoreName = semaphoreName;
      MaxCount = maxCount;
    }

    /// <summary>
    /// セマフォ取得
    /// </summary>
    public bool Get()
    {
      if (HasControl) return HasControl;
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
        //Log.WriteLine("  timeout of waiting for semaphore");  //LGL
        HasControl = false;
      }
      return HasControl;
    }

    /// <summary>
    /// Release
    /// </summary>
    public void Release()
    {
      if (hSemaphore != null)
      {
        HasControl = false;
        hSemaphore.Release();
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
