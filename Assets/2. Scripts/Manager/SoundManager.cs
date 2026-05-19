using UnityEngine;
using System.Collections; // 코루틴 사용을 위해 필요

public class SoundManager : MonoBehaviour
{
    // 어디서든 SoundManager.Instance 로 접근할 수 있게 하는 싱글톤 패턴
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource; // 배경음악 전용
    public AudioSource sfxSource; // 효과음 전용 (플레이어 귀에 바로 들리는 2D 사운드)

    [Header("BGM Clips")]
    public AudioClip overworldBGM;
    public AudioClip underworldBGM;
    public AudioClip clearBGM;
    public AudioClip gameoverBGM;
    public AudioClip deathBGM;
    public AudioClip starBGM;
    public AudioClip overworldFastBGM;

    [Header("SFX Clips (효과음)")]
    public AudioClip jumpSmallSound;
    public AudioClip jumpSuperSound;
    public AudioClip coinSound;
    public AudioClip pipeWarpSound;
    public AudioClip itemAppearSound;
    public AudioClip powerUpSound;
    public AudioClip flagPoleSound;
    public AudioClip stompSound;
    public AudioClip warningSound;
    public AudioClip oneUpSound;
    public AudioClip breakblockSound;
    public AudioClip bumpSound;
    public AudioClip fireballSound;
    public AudioClip kickSound;
    // 필요한 사운드가 있다면 여기에 계속 추가하세요!

    // 기본 볼륨 설정
    [Header("Default Volumes")]
    [Range(0f, 1f)] public float defaultBgmVolume = 0.7f;
    [Range(0f, 1f)] public float defaultSfxVolume = 0.6f;


    private void Awake()
    {
        // 씬이 전환되어도 SoundManager가 파괴되지 않고 유지되도록 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 시작할 때 기본 볼륨 적용
            if (bgmSource != null) bgmSource.volume = defaultBgmVolume;
            if (sfxSource != null) sfxSource.volume = defaultSfxVolume;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 1. 글로벌 2D 사운드 재생 (거리와 상관없이 항상 일정한 볼륨으로 들림 - UI, 마리오 점프 등에 적합)
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// 3. BGM 재생 및 변경
    /// </summary>
    // public void PlayBGM(AudioClip clip)
    // {
    //     if (bgmSource != null && clip != null)
    //     {
    //         bgmSource.clip = clip;
    //         bgmSource.loop = true;
    //         bgmSource.Play();
    //     }
    // }

    // 💡 반복 여부(isLoop)를 선택적으로 설정할 수 있도록 매개변수 추가 (기본값은 true)
    public void PlayBGM(AudioClip clip, bool isLoop = true)
    {
        if (bgmSource != null && clip != null)
        {
            bgmSource.clip = clip;
            // 💡 매개변수로 받은 isLoop 값에 따라 반복 여부를 결정합니다.
            bgmSource.loop = isLoop; 
            bgmSource.Play();
        }
    }
}