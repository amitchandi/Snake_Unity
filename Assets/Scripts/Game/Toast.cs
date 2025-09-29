using System.Collections;
using UnityEngine;
using TMPro;

public class Toast : MonoBehaviour
{
    public TMP_Text text;
    public readonly ArrayList queue = new ArrayList();

    bool isRunning = false;

    void Start()
    {
        //DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (!isRunning && queue.Count > 0)
        {
            var t = (QueuedMessage)queue[0];
            queue.RemoveAt(0);
            StartCoroutine(ShowToastCOR(t.Message, t.Duration));
        }
    }

    public void ShowToast(string message, int duration)
    {
        queue.Add(new QueuedMessage(message, duration));
    }

    private IEnumerator ShowToastCOR(string text, int duration)
    {
        isRunning = true;
        Color originalColor = this.text.color;
        Debug.Log(originalColor);
        this.text.text = text;
        this.text.enabled = true;

        //Fade in
        yield return FadeInAndOut(this.text, true, 0.5f);

        //Wait for the duration
        float counter = 0;
        while (counter < duration)
        {
            counter += Time.deltaTime;
            yield return null;
        }

        //Fade out
        yield return FadeInAndOut(this.text, false, 0.5f);

        this.text.enabled = false;
        this.text.color = originalColor;
        isRunning = false;
    }

    IEnumerator FadeInAndOut(TMP_Text targetText, bool fadeIn, float duration)
    {
        //Set Values depending on if fadeIn or fadeOut
        float a, b;
        if (fadeIn)
        {
            a = 0f;
            b = 1f;
        }
        else
        {
            a = 1f;
            b = 0f;
        }

        float counter = 0f;

        while (counter < duration)
        {
            counter += Time.deltaTime;
            float alpha = Mathf.Lerp(a, b, counter / duration);

            targetText.color = new Color(1, 1, 1, alpha);
            yield return null;
        }
    }
}

class QueuedMessage
{
    public string Message { get; set; }
    public int Duration { get; set; }

    public QueuedMessage(string Message, int Duration)
    {
        this.Message = Message;
        this.Duration = Duration;
    }
}