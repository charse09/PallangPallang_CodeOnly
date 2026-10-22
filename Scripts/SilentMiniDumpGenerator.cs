/*using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

public class SilentMiniDumpGenerator : MonoBehaviour
{
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
    [DllImport("Dbghelp.dll")]
    static extern bool MiniDumpWriteDump(IntPtr hProcess, uint ProcessId, IntPtr hFile, int DumpType, IntPtr ExceptionParam, IntPtr UserStreamParam, IntPtr CallbackParam);

    private static bool isDumping = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        GameObject go = new GameObject("[SilentMiniDumpGenerator]");
        go.AddComponent<SilentMiniDumpGenerator>();
        DontDestroyOnLoad(go);
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 이미 덤프를 뜨고 있거나 한 번 생성했다면 중복 실행 방지
        if (isDumping) return;

        if (type == LogType.Exception || type == LogType.Error)
        {
            isDumping = true;
            CreateMiniDump();
        }
    }

    void CreateMiniDump()
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, $"CrashDump_{DateTime.Now:yyyyMMdd_HHmmss}.dmp");

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Write))
            {
                Process currentProcess = Process.GetCurrentProcess();

                // 2 = MiniDumpWithFullMemory (전체 메모리)
                // 용량이 너무 크면 0 (MiniDumpNormal, 수 MB 단위)으로 변경 가능
                bool success = MiniDumpWriteDump(
                    currentProcess.Handle,
                    (uint)currentProcess.Id,
                    fs.SafeFileHandle.DangerousGetHandle(),
                    2,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero
                );

                if (success)
                {
                    UnityEngine.Debug.Log("덤프 생성 완료: " + path);
                }
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError("덤프 생성 실패: " + ex.Message);
        }
    }
#endif
}*/