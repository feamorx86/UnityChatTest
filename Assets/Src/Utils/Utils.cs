using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class ProgressBar
{
    private RectTransform root;
    private Text progressText;
    private Image progressImage;
    private float value;
    private float maxValue;

    private float sideProgressMargin;

    public ProgressBar(Transform root, int maxValue)
    {
        setupViews(root);
        this.value = 0f;
        this.maxValue = maxValue;
        setSidesMarginFromViews();
        setProgress(0);
    }

    public ProgressBar(Transform root, int progress, int maxValue)
    {
        setupViews(root);
        this.value = 0f;
        this.maxValue = maxValue;
        setSidesMarginFromViews();
        setProgress(progress);
    }

    public ProgressBar(Transform root, int progress, int maxValue, float sideProgressMargin)
    {
        setupViews(root);
        this.value = 0f;
        this.maxValue = maxValue;
        this.sideProgressMargin = sideProgressMargin;
        setProgress(progress);
    }

    private void setSidesMarginFromViews()
    {
        float width = progressImage.rectTransform.rect.width;
        float x = progressImage.rectTransform.anchoredPosition.x;
        sideProgressMargin = x - width / 2f;
    }

    private void setupViews(Transform root)
    {
        this.root = (RectTransform)root;
        progressImage = root.transform.Find("Progress").GetComponent<Image>();
        progressText = root.transform.Find("Text").GetComponent<Text>();
    }


    public void setProgress(float value)
    {
        if (value < 0f) this.value = 0f;
        else if (value > maxValue) this.value = maxValue;
        else this.value = value;

        float progress = value / maxValue;

        progressText.text = ((int)(progress * 100f)).ToString() + "%";

        /*float allProgressWidth = root.rect.width -2f * sideProgressMargin;
        float width = allProgressWidth * progress;
        float x = sideProgressMargin + width / 2f;

        progressImage.anchoredPosition = new UnityEngine.Vector2(x, progressImage.anchoredPosition.y);
        progressImage.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);        */

        progressImage.fillAmount = progress;
    }

    public int getPrgogress()
    {
        return (int)value;
    }

    public float getPrgogressf()
    {
        return value;
    }
}

public class MessageBox
{
    private RectTransform root;
    private Text titleText;
    private Text messageText;
    private Button okButton;
    private Text okButtonText;

    public MessageBox(Transform root)
    {
        this.root = (RectTransform)root;
        Transform panel = root.Find("Panel");
        okButton = panel.Find("ButtonOk").GetComponent<Button>();
        okButtonText = okButton.transform.Find("Text").GetComponent<Text>();
        titleText = panel.Find("TextLabel").GetComponent<Text>();
        messageText = panel.Find("TextMessage").GetComponent<Text>();
        root.gameObject.SetActive(false);
    }

    private void defaultHandler()
    {
        root.gameObject.SetActive(false);
    }

    public void showMessage(string title, string message, string buttonText, UnityAction onClick)
    {
        titleText.text = title;
        messageText.text = message;
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(defaultHandler);
        if (onClick != null)
        {
            okButton.onClick.AddListener(onClick);
        }
        okButtonText.text = buttonText;
        root.gameObject.SetActive(true);
    }

    public void showMessage(string title, string message, UnityAction onClick)
    {
        showMessage(title, message, "Ok", onClick);
    }

    public void showMessage(string title, string message)
    {
        showMessage(title, message, "Ok", null);
    }

    public void showMessage(string message)
    {
        showMessage("", message, "Ok", null);
    }
}

public class WWWLoader
{
    public delegate void onComplete(WWW www, string tag);

    private class Loading {
        public WWW www;
        public onComplete onComplete;
        public string tag;
    }

    private Dictionary<string, Loading> loaders = new Dictionary<string, Loading>();
    private System.Object locker = new System.Object();

    public void addLoader(string tag, string url, onComplete handler)
    {
        Loading loader = new Loading();
        loader.tag = tag;
        loader.www = new WWW(url);
        loader.onComplete = handler;
        lock (locker)
        {
            loaders[tag] = loader;
        }
        
    }

    public void removeLoader(string tag)
    {
        lock (locker)
        {
            loaders.Remove(tag);
        }
    }

    public void clear()
    {
        lock (locker)
        {
            loaders.Clear();
        }
    }

    public void update()
    {
        if (loaders.Count > 0)
        {
            List<KeyValuePair<string, Loading>> complete = null;

            lock (locker)
            {
                foreach (KeyValuePair<string, Loading> loader in loaders)
                {
                    if (loader.Value.www.isDone)
                    {
                        if (complete == null) complete = new List<KeyValuePair<string, Loading>>();
                        complete.Add(loader);
                    }
                }
            }

            if (complete != null)
            {
                foreach (KeyValuePair<string, Loading> loader in complete)
                {
                    lock (locker)
                    {
                        loaders.Remove(loader.Key);
                    }

                    loader.Value.onComplete(loader.Value.www, loader.Key);
                }
            }
        }
    }
}

public class JSONBuilder
{
    public static JSONBuilder create()
    {
        JSONBuilder builder = new JSONBuilder();
        return builder;
    }

    private JSONObject json;

    public JSONBuilder()
    {
        json = new JSONObject();
    }

    public JSONBuilder with(String key, String value)
    {
        json[key] = value;
        return this;
    }

    public JSONBuilder with(String key, int value)
    {
        json[key] = value;
        return this;
    }

    public JSONBuilder with(String key, float value)
    {
        json[key] = value;
        return this;
    }

    public JSONBuilder with(String key, Dictionary<string, object> value)
    {
        json[key] = value;
        return this;
    }

    public Dictionary<string, object> getDictionary()
    {
        return json;
    }

    public JSONObject getJson()
    {
        return (JSONObject)json;
    }
    
    public override string ToString()
    {
        JsonFx.Json.JsonWriter writer = new JsonFx.Json.JsonWriter();
        return writer.Write(json);
    }
}

public class JSONObject : Dictionary<string, object>
{

    public JSONObject()
        : base()
    {

    }
    public JSONObject(IDictionary<string, object> dictionary)
        : base(dictionary)
    {

    }

    public JSONObject(int capacity)
        : base(capacity)
    {

    }


    public static string optString(Dictionary<string, object> json, string key, string fallback)
    {
        string result;
        object value;
        if (json.TryGetValue(key, out value))
        {
            result = value.ToString();
        }
        else
        {
            result = fallback;
        }
        return result;
    }

    public static int optInt(Dictionary<string, object> json, string key, int fallback)
    {
        int result;
        object value;
        if (json.TryGetValue(key, out value))
        {
            result = Convert.ToInt32(value);
        }
        else
        {
            result = fallback;
        }
        return result;
    }

    public static bool optBool(Dictionary<string, object> json, string key, bool fallback)
    {
        bool result;
        object value;
        if (json.TryGetValue(key, out value))
        {
            result = Convert.ToBoolean(value);
        }
        else
        {
            result = fallback;
        }
        return result;
    }

    public static object optObject(Dictionary<string, object> json, string key, object fallback)
    {
        object result;
        if (!json.TryGetValue(key, out result))
        {
            result = fallback;
        }
        return result;
    }

    public static Dictionary<string, object> optJSON(Dictionary<string, object> json, string key, Dictionary<string, object> fallback)
    {
        Dictionary<string, object> result;
        object value;
        if (json.TryGetValue(key, out value))
        {
            result = (Dictionary<string, object>)value;
        }
        else
        {
            result = fallback;
        }
        return result;
    }
}

/*public class DataMessageRunnable 
{
    private DataMessage message;
    private DataHandledDelegate messageHandler;

    public DataMessageRunnable(DataMessage message, DataHandledDelegate messageHandler)
    {
        this.message = message;
        this.messageHandler = messageHandler;
    }

    public void run()
    {
        if (messageHandler(message))
        {
            Connection.getInstance().unregisterDataListener(message.Service, message.Action);
        }
    }
}*/
