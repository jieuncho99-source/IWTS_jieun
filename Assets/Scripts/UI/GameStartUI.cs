using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem; // 추가

public class GameStartUI : MonoBehaviour
{
    [SerializeField] private Button gameStartBtn;
    [SerializeField] private Button achievementBtn;
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private Graphic target;

    private float _speed = 1f;

    private void Awake()
    {
        achievementBtn.onClick.AddListener(() =>
        {
            achievementPanel.SetActive(true);
        });
    }

    private void Update()
    {
        // 1) 키보드 Space
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;

        // 2) 게임패드 A 버튼 (Xbox 기준 / PS는 Cross 버튼)
        bool gamepadAPressed = Gamepad.current != null && Gamepad.current.aButton.wasPressedThisFrame;

        if (spacePressed || gamepadAPressed)
        {
            if (achievementPanel.activeSelf)
            {
                achievementPanel.SetActive(false);
            }
            else
            {
                GameManager.Scene.LoadScene(Scenes.TUTORIAL);
            }
        }

        // 깜빡이는 효과
        float t = Mathf.PingPong(Time.time * _speed, 1f);
        float a = Mathf.Lerp(30f / 200f, 1f, t);

        var c = target.color;
        c.a = a;
        target.color = c;
    }
}
