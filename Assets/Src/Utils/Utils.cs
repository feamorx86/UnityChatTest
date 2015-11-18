using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

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
            loaders.Add(tag, loader);
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
