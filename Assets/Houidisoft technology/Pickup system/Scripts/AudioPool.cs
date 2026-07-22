using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Very small pooled one-shot AudioSource system for 3D SFX.
/// Put one in the scene (or the RuntimePickup will auto-create one).
/// </summary>
namespace PandDS{
[DisallowMultipleComponent]
public class AudioPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public int poolSize = 12;
    public float defaultMinDistance = 0.5f;
    public float defaultMaxDistance = 20f;

    List<AudioSource> pool;
    int idx = 0;

    void Awake()
    {
        CreatePool();
    }

    void CreatePool()
    {
        if (pool != null) return;
        pool = new List<AudioSource>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"AudioPool_Source_{i}");
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f; // fully 3D
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = defaultMinDistance;
            src.maxDistance = defaultMaxDistance;
            pool.Add(src);
        }
    }

    /// <summary>
    /// Play a clip at the given world position with optional pitch & volume variations.
    /// </summary>
    public void Play(AudioClip clip, Vector3 pos, float volume = 1f, float pitch = 1f, float minDistance = -1f, float maxDistance = -1f)
    {
        if (clip == null) return;
        if (pool == null) CreatePool();

        var src = pool[idx];
        idx = (idx + 1) % pool.Count;

        src.transform.position = pos;
        src.clip = clip;
        src.volume = Mathf.Clamp01(volume);
        src.pitch = Mathf.Clamp(pitch, 0.5f, 2f);
        if (minDistance > 0) src.minDistance = minDistance; else src.minDistance = defaultMinDistance;
        if (maxDistance > 0) src.maxDistance = maxDistance; else src.maxDistance = defaultMaxDistance;

        src.Play();
    }
}}
