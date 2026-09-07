using UnityEngine;

public class StartSceneGameManager : MonoBehaviour
{
    [Header("BGM")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip startJingle;
    [SerializeField] private AudioSource bgmAudioSource;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying) bgmAudioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void StopBgm()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }else
        {
            Debug.Log("null : cannot play bgm");
        }
    }

    private void PlayJingle()
    {
        if (audioSource != null && startJingle != null)
        {
            audioSource.PlayOneShot(startJingle);
        }else
        {
            Debug.Log("null : cannot play jingle");
        }
    }
}
