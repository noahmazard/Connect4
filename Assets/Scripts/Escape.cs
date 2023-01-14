using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Escape : MonoBehaviour
{
    [SerializeField] GameObject panel;
    // Start is called before the first frame update
    void Start()
    {
        panel.SetActive(false);
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    public void QuitGame()
    {
       Application.Quit();
    }
}
