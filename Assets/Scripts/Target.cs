using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Target : MonoBehaviour
{
    [SerializeField] private int scoreAmount = 300;
    private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;

    public GameObject score;

    private bool hit;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hit) return;
        if (other.gameObject.tag == "Sphere")
        {
            hit = true;
            ScoreManager.Instance.RegisterTargetHit(scoreAmount);
            if (audioSource != null && hitSound != null) audioSource.PlayOneShot(hitSound);
            StartCoroutine(HitPointCenter());
        }
    }

    IEnumerator HitPointCenter()
    {
        foreach (var r in GetComponents<Renderer>()) r.enabled = false;
        foreach (var c in GetComponents<Collider>()) c.enabled = false;
        var mover = GetComponent<TargetMover>();
        if (mover != null) mover.enabled = false;

        score.SetActive(true);
        yield return new WaitForSeconds(2);
        Destroy(gameObject);
    }
}
