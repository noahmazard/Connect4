using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

public class Music : MonoBehaviour
{
    [SerializeField] private AudioMixerGroup group;
    [SerializeField] private AudioClip clip;
    private static Music instance = null;
    void Start()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.outputAudioMixerGroup = group;
        s.Play();
    }


}
