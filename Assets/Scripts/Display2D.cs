using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class Display2D : MonoBehaviour
{
    [SerializeField] private Button[] grid;

    public AudioClip[] Clips = null;
    public AudioMixerGroup effectGrp;
    private AudioSource source;
    private bool dontPlaySound = false;
    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable()
    {
        ConnectFour.Instance.OnBoardUpdate += UpdateBoard;
        dontPlaySound = true;
        UpdateBoard();
    }

    private void OnDisable()
    {
        ConnectFour.Instance.OnBoardUpdate -= UpdateBoard;
    }


    void UpdateBoard()
    {
        DisplayBoard(ConnectFour.Instance.CurrentNode);
    }

    void DisplayBoard(Node _node)
    {
        for (int i = 0; i < 42; i++)
        {
            int x = i % 7;
            int y = 5 - i / 7;

            switch (_node.GetState(x, y))
            {
                case Node.State.Empty:
                    grid[i].image.color = Color.white;
                    break;
                case Node.State.Player1:
                    grid[i].image.color = DisplayManager.Instance.color1;
                    break;
                case Node.State.Player2:
                    grid[i].image.color = DisplayManager.Instance.color2;
                    break;
            }
        }

        if (dontPlaySound)
        {
            dontPlaySound = false;
            return;
        }
        source.clip = Clips[UnityEngine.Random.Range(0, 5)];
        source.outputAudioMixerGroup = effectGrp;
        source.Play();
    }
}
