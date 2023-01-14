using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class UISettings : MonoBehaviour
{
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider effectSlider;
    public void ChangeVolume()
    {
        mixer.SetFloat("MusicVolume", musicSlider.value);
        mixer.SetFloat("EffectVolume", effectSlider.value);
    }
    
}
