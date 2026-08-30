using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{

    public GameObject score;
    private bool hit;

    void OnTriggerEnter(Collider coll)
    {
        if (hit) return;
        if (coll.gameObject.tag == "Sphere")
        {
            hit = true;
            StartCoroutine(HitPointCenter());
        }
    }

    IEnumerator HitPointCenter()
    {
        foreach (var r in GetComponents<Renderer>()) r.enabled = false;
        foreach (var c in GetComponents<Collider>()) c.enabled = false;

        var mover = GetComponent<TargetMover>();
        if (mover != null) mover.enabled = false;

        var audio = GetComponent<AudioSource>();
        if (audio != null) audio.Play();

        score.SetActive(true);
        yield return new WaitForSeconds(2);
        Destroy(gameObject);
    }
}