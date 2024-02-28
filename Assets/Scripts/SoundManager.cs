
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    private const int DEFAULT_PRIORITY = 128;
    public static SoundManager Instance { get; private set; }
    
    public bool IsPlaying { get; private set; }
    
    [SerializeField] private AudioClip mainMenuMusic;
    [Range(0, 1)] [SerializeField] private float mainMenuVolume;

    [SerializeField] private AudioClip[] gameMusic;
    [Range(0, 1)] [SerializeField] private float gameVolume;
    
    [SerializeField] private AudioClip gameOverMusic;
    [Range(0, 1)] [SerializeField] private float gameOverVolume;
    
    [SerializeField] private AudioClip onDeathSound;
    [Range(0, 1)] [SerializeField] private float onDeathVolume;

    
    private AudioSource _audioSource;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (_audioSource == null)
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.ignoreListenerPause = true;
        }
       
        PlayThemeByScene();
    }
    
    public void LowerVolume()
    {
        _audioSource.volume -= 0.1f;
    }
    
    public void IncreaseVolume()
    {
        _audioSource.volume += 0.1f;
    }
    
    
    public void StopPlaying()
    {
        _audioSource.Stop();
        IsPlaying = false;
    }
    
    public void ResumePlaying()
    {
        _audioSource.Play();
        IsPlaying = true;
    }

    /**
     * Plays the background music by the scene's name.
     */
    public void PlayThemeByScene()
    {
        _audioSource.enabled = true;
        _audioSource.Stop();
        var sceneName = SceneManager.GetActiveScene().name;
        switch (sceneName)
        {
            case "MainMenu":
                _audioSource.loop = true;
                _audioSource.clip = mainMenuMusic;
                _audioSource.volume = mainMenuVolume;
                break;
            case "GameOver":
                _audioSource.loop = false;
                _audioSource.clip = gameOverMusic;
                _audioSource.volume = gameOverVolume;
                break;
            default:
                _audioSource.loop = true;
                // var rand = Random.Range(0, gameMusic.Length);
                _audioSource.clip = gameMusic[0];
                _audioSource.volume = gameVolume;
                break;
        }

        _audioSource.Play();
        IsPlaying = true;
    }

    public void PlayDeathSound()
    {
        _audioSource.enabled = true;
        _audioSource.Stop();
        _audioSource.PlayOneShot(onDeathSound, onDeathVolume);
    }
}
