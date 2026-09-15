using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class StageSelectSceneGameManager : MonoBehaviour
{
    
    [Header("Controler Manager")]
    [SerializeField] private ControlerManager Source;


    [Header("Sound")]
    [SerializeField] private AudioSource jingleAudioSource;
    [SerializeField] private AudioClip selectJingle;
    [SerializeField] private AudioSource bgmAudioSource;

    // スタート演出はしたかどうか
    private bool isStart = false;
    public bool IsStart => isStart;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (bgmAudioSource != null && !bgmAudioSource.isPlaying) bgmAudioSource.Play();
    }

    // Update is called once per frame
    void Update()
    {
        // シーン切り替えのトリガーが入ったら音量を小さくして、早期リターン
        if (isStart)
        {
            bgmAudioSource.volume = Mathf.Lerp(bgmAudioSource.volume, 0f, Time.deltaTime);
            return;
        }
    }

    // ゲームステージのロード
    public void OnEasyButtonClicked()
    {
        // ボタンを複数回クリックしても動かないようにする
        if (!isStart)
        {
            // ステージのロード開始済みにする
            isStart = true;
            StartCoroutine(NextScene("EasyStageScene"));
            PlayJingle(selectJingle);
        }
    }

    IEnumerator NextScene(string sceneName)
    {
        yield return new WaitForSeconds(2);
        SceneManager.LoadScene(sceneName);
    }

    private void PlayJingle(AudioClip clip)
    {
        if (jingleAudioSource != null && clip != null)
        {
            jingleAudioSource.PlayOneShot(clip);
        }else
        {
            Debug.Log("null : cannot play jingle");
        }
    }
}
