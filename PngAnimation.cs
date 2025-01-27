using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PNGAnimation : MonoBehaviour
{
    public string status = "walk";
    public float frameRate = 5f; // 초당 프레임 수
    public List<Sprite> frames_walk = new List<Sprite>();
    public List<Sprite> frames_talk = new List<Sprite>();
    private Image image;
    private int currentFrame = 0;

    void Start()
    {
        image = GetComponent<Image>(); // UI Image 컴포넌트 가져오기
        if (frames_walk.Count > 0) {
            StartCoroutine(PlayAnimation());
        }
    }

    IEnumerator PlayAnimation()
    {
        while (true)
        {
            if (frames_walk.Count > 0)
            {
                image.sprite = frames_walk[currentFrame];
                currentFrame = (currentFrame + 1) % frames_walk.Count;
                yield return new WaitForSeconds(1f / frameRate);
            }
        }
    }
}
