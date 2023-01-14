using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayUI : MonoBehaviour
{
    [SerializeField] private Dropdown dropdownP1;
    [SerializeField] private Dropdown dropdownP2;
    [SerializeField] private Dropdown dropdownDiff;

    [SerializeField] private int[] difficultyDepth = {2, 5, 10, 15, 20};

    private void Start()
    {
        if (Rules.Instance != null)
        {
            ToUi(Rules.Instance);
        }
    }

    public void Play()
    {
        Rules rules = Rules.Instance == null ? new GameObject("Rules").AddComponent<Rules>() : Rules.Instance;

        rules.isP1AI = dropdownP1.value == 1;
        rules.isP2AI = dropdownP2.value == 1;
        rules.depthAI = difficultyDepth[dropdownDiff.value];

        SceneManager.LoadScene("MainScene");
    }

    public void ToUi(Rules rules)
    {
        dropdownP1.value = rules.isP1AI ? 1 : 0;
        dropdownP2.value = rules.isP2AI ? 1 : 0;

        dropdownDiff.value = Array.IndexOf(difficultyDepth, rules.depthAI);
    }
}
