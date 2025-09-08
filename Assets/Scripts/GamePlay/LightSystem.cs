using UnityEngine;

public class LightSystem : MonoBehaviour
{
    public RenderTexture[] rTexture;   // 확인할 RenderTexture
    private Texture2D tex2D;
    private bool isExposedToLight = false;
    private int textureSize;
    public float lightThreshold = 0.48f;

    // 체력 시스템
    [Header("Health")]
    public HealthSystem healthSystem;
    public float damagePerSecond = 10f;

    // 빛
    private bool wasExposedToLight = false;  // 이전 프레임 노출 상태

    [Header("Particle")]
    public ParticleSystem headParticleSystem;  // GameObject 대신 ParticleSystem 참조

    // 그림자로 들어간 뒤 이 시간 후 파티클 방출 중지
    public float particleShowSeconds = 0.5f;

    void Start()
    {
        // 텍스쳐 하나 가져와서 사이즈 저장
        textureSize = rTexture[0].width;

        tex2D = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);

        healthSystem = GetComponent<HealthSystem>();
    }

    // RenderTexture에서 한 번이라도 완전 흰색 픽셀이 발견되면 true 반환
    private bool CheckWhitePixel()
    {
        foreach (RenderTexture cTexture in rTexture)
        {
            RenderTexture.active = cTexture;
            tex2D.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            tex2D.Apply();
            RenderTexture.active = null;

            Color[] pixels = tex2D.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].r >= lightThreshold && pixels[i].g >= lightThreshold && pixels[i].b >= lightThreshold) // 완전 흰색
                {
                    return true; // 하나라도 발견 시 즉시 true 반환
                }
            }
        }

        return false;
    }

    void Update()
    {

        // 현재 햇빛 노출 판정
        isExposedToLight = CheckWhitePixel();

        // 지속 데미지(노출 중에만)
        if (isExposedToLight && healthSystem != null)
            healthSystem.ApplyDamage(damagePerSecond * Time.deltaTime);

        // 상태 전이 감지
        if (isExposedToLight && !wasExposedToLight)
        {
            // 햇빛에 처음 들어옴
            Debug.Log("빛에 노출 시작!");
            SoundManager.Instance.StartLoopSFX(SoundId.longSizzle);

            if (headParticleSystem != null)
            {
                headParticleSystem.Play(); // 방출 시작
                CancelInvoke(nameof(DisableHeadParticle));
            }
        }
        else if (!isExposedToLight && wasExposedToLight)
        {
            // 그림자에 처음 들어옴
            Debug.Log("빛 노출 종료!");
            SoundManager.Instance.StopLoopSFX();

            if (headParticleSystem != null)
            {
                CancelInvoke(nameof(DisableHeadParticle));
                Invoke(nameof(DisableHeadParticle), particleShowSeconds);
            }
        }

        // 상태 갱신
        wasExposedToLight = isExposedToLight;
    }

    void DisableHeadParticle()
    {
        if (headParticleSystem != null)
            headParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        // 이미 생성된 파티클은 자연 소멸, 새 파티클만 중지
    }

}
