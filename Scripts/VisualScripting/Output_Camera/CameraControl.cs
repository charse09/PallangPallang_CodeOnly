using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace _Project.Scripts.VisualScripting
{
public class CameraControl : ProcessBase
{
    [Header("Main Virtual Camera Setting")]
    [SerializeField] private CinemachineCamera mainVirtualCamera;        // 컷씬 재생 후 되돌아갈 카메라(메인카메라가 반드시 우선순위가 높아야함)

    [Header("Cutscene Virtual Camera Settings")]
    [SerializeField] private CinemachineCamera cutsceneVirtualCamera;    // 컷씬 재생할 카메라
    [SerializeField] private PlayableDirector playableDirector;

    [Header("Virtual Camera Priority")]
    [SerializeField] private int mainVirtualCameraPriority;
    [SerializeField] private int cutceneVirtualCameraPriority;

    private void Start()
    {
        if(mainVirtualCamera is not null && cutsceneVirtualCamera is not null && playableDirector)
        {
            mainVirtualCameraPriority = mainVirtualCamera.Priority;
            cutceneVirtualCameraPriority = cutsceneVirtualCamera.Priority;
        }
        else
        {
            if (mainVirtualCamera is null) VisualLogger.LogError(this, "Main Virtual Camera is Empty!");
            if (cutsceneVirtualCamera is null) VisualLogger.LogError(this, "Cutscene Virtual Camera is Empty!");
            if (playableDirector is null) VisualLogger.LogError(this, "playableDirector is Empty!");
        }
    }
    
    public override void Execute()
    {
        if (mainVirtualCamera is null || cutsceneVirtualCamera is null || playableDirector is null)
        {
            VisualLogger.LogError(this, "Setting Missing Excute Fail!");
            return;
        }
      
        if (IsOn) return;
        playableDirector.Play();
        StartCoroutine(SwitchToCutscene());     
        IsOn = true;
    }

    private IEnumerator SwitchToCutscene()
    {
        cutsceneVirtualCamera.Priority = mainVirtualCameraPriority + 1;
        mainVirtualCamera.Priority = cutceneVirtualCameraPriority;

        // 재생이 완전히 끝날 때까지 대기
        while (playableDirector.state == PlayState.Playing)
        {
            yield return null;
        }

        cutsceneVirtualCamera.Priority = cutceneVirtualCameraPriority;
        mainVirtualCamera.Priority = mainVirtualCameraPriority;
    }
}
}