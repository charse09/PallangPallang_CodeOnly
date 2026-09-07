using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 현재 위치 또는 특정 지점을 플레이어의 새로운 부활 장소(체크포인트)로 등록하는 Output 모듈
    /// </summary>
    public class SetCheckpointOutput : ProcessBase
    {
        [Header("Checkpoint Settings")]
        [Tooltip("체크포인트로 지정할 위치 (비워두면 이 스크립트가 붙은 오브젝트의 현재 위치를 세이브포인트로 사용합니다)")]
        [SerializeField] private Transform checkpointTarget;

        public override void Execute()
        {
            IsOn = false;

            if (checkpointTarget == null)
            {
                checkpointTarget = transform;
            }

            // 공유 데이터 공간에 위치와 회전값 저장
            CheckpointStaticData.SavedPosition = checkpointTarget.position;
            CheckpointStaticData.SavedRotation = checkpointTarget.rotation;
            CheckpointStaticData.HasCheckpoint = true;
            CheckpointStaticData.SceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 새로운 체크포인트가 저장되었습니다: {CheckpointStaticData.SavedPosition}");
#endif

            IsOn = true;
        }
    }
}