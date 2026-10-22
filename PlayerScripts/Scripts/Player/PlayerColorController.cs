using UnityEngine;

public class PlayerColorController : MonoBehaviour
{
    [SerializeField] private Renderer characterRenderer; // 머티리얼이 적용된 렌더러
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color wet1Color = new Color(0.7f, 0.7f, 0.7f); // 약간 어둡게
    [SerializeField] private Color wet2Color = new Color(0.4f, 0.4f, 0.4f); // 아주 어둡게

    public void ChangeColorByState(PlayerConditionState state)
    {
        Color targetColor = normalColor;

        switch (state)
        {
            case PlayerConditionState.Wet1:
                targetColor = wet1Color;
                break;
            case PlayerConditionState.Wet2:
                targetColor = wet2Color;
                break;
            default:
                targetColor = normalColor;
                break;
        }

        // 머티리얼의 메인 컬러(Base Color) 변경
        if (characterRenderer != null)
        {
            characterRenderer.material.color = targetColor;
        }
    }
}