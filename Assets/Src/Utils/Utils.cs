using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

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
