using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DisplayManager : MonoBehaviour
{
    [SerializeField] public Color color1 = Color.yellow;
    [SerializeField] public Color color2 = Color.red;
    [SerializeField] public Color color3 = Color.black;

    [SerializeField] private Text[] texts;
    [SerializeField] private Image loading;

    [SerializeField] private Display3D d3D;
    [SerializeField] private Display2D d2D;

    [SerializeField] private bool is2D;

    [SerializeField] private Button RestartBtn;
    [SerializeField] private Animator anim;

    public static DisplayManager Instance;

    public AudioClip[] Clips = new AudioClip[10];
    public AudioMixerGroup effectGrp;

    private void Awake()
    {
        Instance = this;
    }

    private void Restart()
    {
        texts[0].color = DisplayManager.Instance.color1;
        texts[1].color = DisplayManager.Instance.color2;
        texts[0].text = "0s";
        texts[1].text = "0s";
        texts[3].text = "0s";
        texts[4].text = "0s";
        texts[2].enabled = false;

        RestartBtn.gameObject.SetActive(false);
    }

    private void Start()
    {
        ConnectFour.Instance.OnBoardUpdate += UpdateBoard;
        ConnectFour.Instance.OnTimeUpdate += UpdateTime;
        ConnectFour.Instance.OnReset += Restart;
        Restart();

        d3D.Clips = Clips;
        d2D.Clips = Clips;
        d2D.effectGrp = effectGrp;
        d2D.effectGrp = effectGrp;
    }

    private void Update()
    {
        loading.transform.Rotate(Vector3.forward, Time.deltaTime * 180);
    }

    void UpdateBoard()
    {
        if (ConnectFour.Instance.CurrentNode == null) return;
        UpdateScore(ConnectFour.Instance.CurrentNode);
        RestartBtn.gameObject.SetActive(ConnectFour.Instance.win != Node.State.Empty);
    }

    private void UpdateScore(Node _node)
    {
        Node.State result = _node.GetWinner();
        if (result != Node.State.Empty)
        {
            texts[2].enabled = true;
            texts[2].color = result == Node.State.Player1 ? DisplayManager.Instance.color1 : DisplayManager.Instance.color2;
        }
        else if (_node.remainingStones == 0)
        {
            texts[2].enabled = true;
            texts[2].color = DisplayManager.Instance.color3;
            texts[2].text = "It's a Draw";
        }
    }

    void UpdateTime()
    {
        texts[ConnectFour.Instance.curPlay1 ? 0 : 1].text = $"{ConnectFour.Instance.deltaTime:N3}s";
        texts[ConnectFour.Instance.curPlay1 ? 3 : 4].text = $"{ConnectFour.Instance.maxTime[ConnectFour.Instance.curPlay1 ? 0 : 1]:N3}s";
    }

    public void Switch()
    {
        is2D = !is2D;
        d3D.CanPlay = !is2D;
        anim.SetTrigger("Switch");
    }

    public void Return()
    {
        SceneManager.LoadScene("MenuScene");
    }
}
