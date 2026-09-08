using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;
using Unity.Mathematics;
using System.Linq;

public class TargetSpawner : MonoBehaviour
{
    public SpawnTimeline timeline;
    public bool loop = false;
    public bool autoStart = true;
    private List<SpawnEntry> sortedEntries;
    private int nextIndex;
    private float elapsed;
    private float timelineDuration;
    private bool started;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (timeline == null || timeline.entries == null)
        {
            Debug.LogWarning("TargetSpawner: timeline is not assigned.");
            enabled = false;
            return;
        }

        sortedEntries = new List<SpawnEntry>(timeline.entries);
        sortedEntries.Sort((a, b) => a.time.CompareTo(b.time));

        timelineDuration = 0f;
        foreach (var e in sortedEntries)
        {
            float end = e.time + e.lifetime;
            if (end > timelineDuration)
            {
                timelineDuration = end;
            }
        }

        if (autoStart) StartTimeline();
    }

    public void StartTimeline()
    {
        started = true;
        elapsed = 0f;
        nextIndex = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (!started) return;
        elapsed += Time.deltaTime;

        while (nextIndex < sortedEntries.Count && elapsed >= sortedEntries[nextIndex].time)
        {
            Spawn(sortedEntries[nextIndex]);
            nextIndex++;
        }

        if (loop && nextIndex >= sortedEntries.Count && elapsed >= timelineDuration)
        {
            elapsed = 0f;
            nextIndex = 0;
        }
    }

    private void Spawn(SpawnEntry entry)
    {
        if (entry.prefab == null) return;

        GameObject go = Instantiate(entry.prefab, entry.position, entry.prefab.transform.rotation);
        go.transform.localScale = entry.prefab.transform.localScale * entry.scale;

        TargetMover mover = go.GetComponent<TargetMover>();
        if (mover == null) mover = go.AddComponent<TargetMover>();
        mover.Init(entry);
    }
}
