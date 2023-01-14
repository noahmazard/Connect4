using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class StoneSound : MonoBehaviour
{
    public AudioClip[] clips = new AudioClip[2];
    public AudioSource source;
    public AudioMixerGroup effectGrp;
    public bool playSound = false;
    private bool played = false;

    private void OnCollisionEnter(Collision other)
    {
        if (played || !playSound) return;

        source.clip =  clips[other.gameObject.name == "Platform" ? 0 : 1];
        source.outputAudioMixerGroup = effectGrp;
        source.Play();
        played = true;
    }
}
