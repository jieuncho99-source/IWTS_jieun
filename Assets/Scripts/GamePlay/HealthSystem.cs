using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth { get; private set; }

    public bool IsDead => currentHealth <= 0f;
    public UnityEngine.Events.UnityEvent onDamaged;
    public Renderer playerRenderer;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        ChangePlayerColor(playerRenderer);
    }

    public void ApplyDamage(float amount)
    {
        var ending = GameObject.FindObjectOfType<EndingEffect>();
        if (ending != null && ending.isPlaying) return;

        currentHealth -= amount;
        Debug.Log($"체력: {currentHealth}");

        // 이벤트 호출 지점
        onDamaged?.Invoke();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("사망! 스테이지 처음으로 돌아갑니다.");

        UnityEngine.SceneManagement.SceneManager.LoadScene(
        UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex); // 지금 플레이한 씬 처음 불러오기
    }

    public void RestoreFull()
    {
        currentHealth = maxHealth;
    }

    public void ChangePlayerColor(Renderer playerRenderer)
    {
        if (playerRenderer == null) return;

        var ending = GameObject.FindObjectOfType<EndingEffect>();
        if (ending != null && ending.isPlaying) return;


        // 체력이 0 아래로도 갈 수 있으므로 Clamp
        float clampedHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        // 체력 백분율
        float healthPercent = clampedHealth / maxHealth; // 1.0 ~ 0.0

        // R은 항상 255 유지, G/B는 퍼센트만큼 유지 (255 → 0)
        float r = 255f;
        float g = 255f * healthPercent;
        float b = 255f * healthPercent;

        // 0~255 → 0~1 변환
        Color newColor = new Color(r / 255f, g / 255f, b / 255f, 1f);

        playerRenderer.material.color = newColor;
    }

}
