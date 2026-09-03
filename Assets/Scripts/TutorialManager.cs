using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private TargetSpawner spawner;
    [SerializeField] private float tutorialDuration = 30f;
    [SerializeField] private string nextSceneName = "SlingshotGame";

    [Header("Next-Scene Notice")]
    [Tooltip("残り何秒で予告を表示するか")]
    [SerializeField] private float noticeAtRemaining = 5f;
    [Tooltip("予告として出すGameObject（Text等）")]
    [SerializeField] private GameObject nextSceneNotice;

    void Start()
    {
        if (nextSceneNotice != null) nextSceneNotice.SetActive(false);

        if (startButton != null)
        {
            startButton.onClick.AddListener(BeginTutorial);
        }
    }

    public void BeginTutorial()
    {
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (spawner != null) spawner.StartTimeline();
        StartCoroutine(TutorialTimer());
    }

    private IEnumerator TutorialTimer()
    {
        float beforeNotice = Mathf.Max(0f, tutorialDuration - noticeAtRemaining);
        yield return new WaitForSeconds(beforeNotice);

        if (nextSceneNotice != null) nextSceneNotice.SetActive(true);

        yield return new WaitForSeconds(Mathf.Min(noticeAtRemaining, tutorialDuration));
        SceneManager.LoadScene(nextSceneName);
    }
}
