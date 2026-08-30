using UnityEngine;
using System.Collections.Generic;


public enum TargetMoveType
{
    Static,
    Horizontal,
    Vertical,
    Circular,
}
[System.Serializable]
public class SpawnEntry
{
    [Header("When / What")]
    public float time;
    public GameObject prefab;

    [Header("Where / How big")]
    public Vector3 position;
    public float scale = 1f;

    [Header("Lifetime")]
    public float lifetime = 5f;

    [Header("Movement")]
    public TargetMoveType moveType = TargetMoveType.Static;

    [Tooltip("Horizontal/Vertical: units per second. Negative for opposite direction.")]
    public float speed = 2f;

    [Tooltip("Circular: orbit radius around 'position'.")]
    public float radius = 1.5f;

    [Tooltip("Circular: degrees per second. Negative for reverse rotation.")]
    public float angularSpeed = 90f;

    [Tooltip("Circular: starting angle in degrees (0 = +X axis).")]
    public float startAngle = 0f;
}
[CreateAssetMenu(fileName = "SpawnTimeline", menuName = "Slingshot/Spawn Timeline")]
public class SpawnTimeline : ScriptableObject
{
    public List<SpawnEntry> entries = new List<SpawnEntry>();
}
