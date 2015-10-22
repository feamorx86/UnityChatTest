using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using JsonFx.Json;

public class UserScreenController : MonoBehaviour {

    private Transform panelWaiting;
    private RectTransform contentPanne;
	
    public void StartScreen()
    {
        gameObject.SetActive(true);
        displayWaiting(true);
        requestUserInfo();
    }

    private void displayWaiting(bool show)
    {
        panelWaiting.gameObject.SetActive(show);
        requestUserInfo();
        requestAvalableGames();
    }

    private void requestUserInfo()
    {
                
    }

    private void requestAvalableGames()
    {

    }

	void Awake () {
        
        panelWaiting = transform.Find("PanelWait");
        panelWaiting.gameObject.SetActive(false);

        contentPanne = (RectTransform)transform.Find("ContentPanel");

        Scrollbar scroller = transform.Find("Scrollbar").GetComponent<Scrollbar>();
        scroller.onValueChanged.AddListener(delegate(float value)
        {
            float screenHeight = Screen.height;
            float screenWidth = Screen.width;
            float allHeight = contentPanne.rect.height - screenHeight + 50f;
            float allWidth = contentPanne.rect.width;
            float yPos = value * - allHeight + allHeight / 2f + screenHeight /2f;
            float xPos = (screenWidth - allWidth) / 2f + allWidth /2;
            contentPanne.position = new UnityEngine.Vector3(xPos, yPos, 0);
        });

	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
