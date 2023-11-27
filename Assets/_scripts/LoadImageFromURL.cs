using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI; // 如果你使用UI Image来展示图片，则需要这个命名空间

public class LoadImageFromURL : MonoBehaviour
{
    public string imageUrl; // 这里输入你的图片URL
    public Renderer targetRenderer; // 如果你使用平面来展示图片，则使用这个
    public Image targetImage; // 如果你使用UI来展示图片，则使用这个

    void Start()
    {
        StartCoroutine(GetTexture());
    }

    IEnumerator GetTexture()
    {
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            Texture texture = ((DownloadHandlerTexture)www.downloadHandler).texture;

            if (targetRenderer != null)
            {
                // 适用于场景中的游戏对象的渲染器
                targetRenderer.material.mainTexture = texture;
            }
            else if (targetImage != null)
            {
                // 适用于UI Image控件
                targetImage.sprite = Sprite.Create((Texture2D)texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }
        }
    }
}