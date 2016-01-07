using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System;

public class BaseUiController: MonoBehaviour 
{
    protected Transform waitWindiow;
    protected MessageBox messageBox;

    public virtual void Awake()
    {
        waitWindiow = transform.Find("WaitWindiow");
        waitWindiow.gameObject.SetActive(false);
        messageBox = new MessageBox(transform.Find("MessageBox"));
    }

    public MessageBox getMessageBox()
    {
        return messageBox;
    }
    
    protected virtual void startWait()
    {
        waitWindiow.gameObject.SetActive(true);
    }

    protected virtual void stopWait()
    {
        waitWindiow.gameObject.SetActive(false);
    }

    public virtual void stopWindow()
    {

    }

    public virtual void updateUi(object param)
    {

    }

    public virtual void startWindow()
    {

    }

    public virtual void pauseWindow()
    {

    }

    public virtual void resumeWindow()
    {
    }
}