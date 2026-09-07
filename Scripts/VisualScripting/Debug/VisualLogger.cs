using UnityEngine;
using System.Diagnostics; // Conditional용
using System; // Action용
using Debug = UnityEngine.Debug;

namespace _Project.Scripts.VisualScripting
{
    public static class VisualLogger
    {
        // ★ 화면에 로그를 띄워줄 '구독자'들을 위한 이벤트
        // 파라미터: (메시지 내용, 색상)
        public static event Action<string, Color> OnLogReceived;

        [Conditional("UNITY_EDITOR")]
        public static void Log(this UnityEngine.Object context, string message, bool showLog = true)
        {
            if (!showLog) return;

            // 1. 유니티 콘솔창 출력
            string objectName = (context != null) ? context.name : "Global";
            Debug.Log($"<color=#00FF00>[{objectName}]</color> {message}", context);

            // 2. 화면 출력 이벤트 발송 (구독자가 있다면 실행됨)
            // 색상은 기본 흰색으로 보냄 (필요하면 파라미터로 받게 수정 가능)
            OnLogReceived?.Invoke($"[{objectName}] {message}", Color.white);
        }

        // (옵션) 에러용 로그 함수도 하나 추가하면 좋습니다.
        [Conditional("UNITY_EDITOR")]
        public static void LogError(this UnityEngine.Object context, string message)
        {
            Debug.LogError($"[{context.name}] {message}", context);
            OnLogReceived?.Invoke($"[Error] {message}", Color.red); // 빨간색 전송
        }
    }
}