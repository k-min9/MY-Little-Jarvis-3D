using System;
using UnityEngine;

public class WheelHandler : MonoBehaviour
{
    private int sizePercent = 100;
    private int sizeMax = 200;
    private int sizeMin = 50;

    // void OnMouseOver()
    // {
    //     float scroll = Input.GetAxis("Mouse ScrollWheel");

    //     if (scroll > 0)
    //     {
    //         OnScrollUp();
    //     }
    //     else if (scroll < 0)
    //     {
    //         OnScrollDown();
    //     }
    // }

    void OnScrollUp()
    {
        sizePercent = Math.Min(sizeMax, sizePercent + 10);
        SubCharManager.Instance.setCharSize(transform.parent.gameObject, sizePercent);
    }

    void OnScrollDown()
    {
        sizePercent = Math.Max(sizeMin, sizePercent - 10);
        SubCharManager.Instance.setCharSize(transform.parent.gameObject, sizePercent);
    }
}
