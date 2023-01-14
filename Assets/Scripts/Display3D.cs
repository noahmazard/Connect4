using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using Random = System.Random;

public class Display3D : MonoBehaviour
{
    public GameObject StonePrefab;
    public Transform StoneParent;
    public Transform[] Borders;
    public Transform Platform;
    public int delta;
    public bool a = false;
    public bool CanPlay = true;

    public AudioClip[] Clips;
    public AudioMixerGroup effectGrp;

    public GameObject s;
    private void Start()
    {
        s = Instantiate(StonePrefab, transform);
        s.GetComponent<Collider>().enabled = false;
        s.GetComponent<MeshRenderer>().material.color = DisplayManager.Instance.color1;
        s.GetComponent<Rigidbody>().isKinematic = true;
    }

    private void OnEnable()
    {
        ConnectFour.Instance.OnPlay += SpawnStone;
        ConnectFour.Instance.OnReset += Restart;
    }

    private void OnDisable()
    {
        ConnectFour.Instance.OnReset -= Restart;
    }

    private void Update()
    {
        Vector3 sp = Input.mousePosition;
        sp.z = -Camera.main.transform.position.z;
        Vector3 p = Camera.main.ScreenToWorldPoint(sp);
        delta = (int)(p.x - Borders[0].position.x);

        if (!CanPlay || delta < 0 || delta > 6 || ConnectFour.Instance.GetCurrentPlayerType() != ConnectFour.PlayerType.Player)
        {
            s.SetActive(false);
            return;
        }

        s.SetActive(true);
        s.transform.position = Borders[0].position + (delta + 0.5f) * Vector3.right + Vector3.up * 0.5f;

        if (Input.GetMouseButtonDown(0))
        {
           ConnectFour.Instance.PlayerAction(delta);
        }
    }

    private void SpawnStone(int _x)
    {
        Vector3 pos = Borders[0].position + (_x + 0.5f) * Vector3.right + Vector3.up * (CanPlay ? 0.5f : -0.5f);
        GameObject n = Instantiate(StonePrefab, pos, StonePrefab.transform.rotation, StoneParent);

        int r = UnityEngine.Random.Range(0, 5);
        StoneSound sound = n.GetComponent<StoneSound>();
        sound.clips[0] = Clips[r];
        sound.clips[1] = Clips[r+5];
        sound.playSound = CanPlay;
        sound.effectGrp = effectGrp;
        s.GetComponent<MeshRenderer>().material.color = a ? DisplayManager.Instance.color1 : DisplayManager.Instance.color2;
        a = !a;
        n.GetComponent<MeshRenderer>().material.color = a ? DisplayManager.Instance.color1 : DisplayManager.Instance.color2;
    }

    private void Restart()
    {
        StartCoroutine(RestartCor());
    }

    IEnumerator RestartCor()
    {
        for (int i = 0; i < StoneParent.childCount; i++)
        {
            Transform stone = StoneParent.GetChild(i);
            stone.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
            Destroy(stone.gameObject, UnityEngine.Random.Range(1f, 3f));
        }

        Platform.gameObject.SetActive(false);
        yield return new WaitForSeconds(3);
        Platform.gameObject.SetActive(true);
    }
}
