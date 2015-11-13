using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using JsonFx.Json;

public class UserScreenController : MonoBehaviour {

    private Transform panelWaiting;
    private RectTransform contentPanel;
    private Scrollbar scroller;

    public Connection connection;
    public UserManager userManager;
    
    private UserInfoController dataController;
    private WWWLoader wwwLoader = new WWWLoader();
	
    public void StartScreen()
    {
        gameObject.SetActive(true);
        dataController = new UserInfoController(connection, displayNetworkError, userManager);
        displayWaiting(true);
        requestUserInfo();
    }

    private void displayNetworkError()
    {
        Debug.LogError("Network error");
    }

    private void displayWaiting(bool show)
    {
        panelWaiting.gameObject.SetActive(show);
    }

    private void requestUserInfo()
    {
        dataController.requestUserInfo(onReceivedUserInfo);
    }

    private Text firstNameText;
    private Text secondNameText;
    private Image userIconImage;

    private void displayUserInfo()
    {
        string firstName = dataController.getFirstName(userInfo);
        string secondName = dataController.getSecondName(userInfo);
        string userIcon = dataController.getUserIcon(userInfo);

        firstNameText.text = firstName;
        secondNameText.text = secondName;

        wwwLoader.addLoader("load-user-icon", userIcon, delegate(WWW www, string tag)
        {
            Sprite iconSprite = Sprite.Create(www.texture, userIconImage.sprite.rect, Vector2.zero);
            userIconImage.sprite = iconSprite;
        });
    }

    private void dispayInventory()
    {
        if (inventory != null)
        {

        }
    }

    private Dictionary<string, System.Object> userInfo = null;
    private List<UserInventoryItem> inventory = null;

    private void onReceivedUserInfo(int result, Dictionary<string, System.Object> fullUserInfo)
    {
        displayWaiting(false);
        if (result == Ids.GameResloverResults.SUCCESS)
        {
            userInfo = dataController.getUserInfo(fullUserInfo);
            displayUserInfo();

            Dictionary<string, System.Object> userInventory = dataController.getUserInventory(fullUserInfo);
            if (userInventory != null)
            {
                inventory = dataController.getInventoryItems(userInventory);
            }
            else
            {
                inventory = null;
            }
            dispayInventory();

            //TODO: add staticstics
        }
        else
        {

        }
    }

	void Awake () {
        
        panelWaiting = transform.Find("PanelWait");
        panelWaiting.gameObject.SetActive(false);

        contentPanel = (RectTransform)transform.Find("ContentPanel");

        scroller = transform.Find("Scrollbar").GetComponent<Scrollbar>();
        scroller.onValueChanged.AddListener(delegate(float value)
        {
            float screenHeight = Screen.height;
            float screenWidth = Screen.width;
            float allHeight = contentPanel.rect.height - screenHeight + 50f;
            float allWidth = contentPanel.rect.width;
            float yPos = value * - allHeight + allHeight / 2f + screenHeight /2f;
            float xPos = (screenWidth - allWidth) / 2f + allWidth /2;
            contentPanel.position = new UnityEngine.Vector3(xPos, yPos, 0);
        });

        RectTransform userInfoPanel = (RectTransform)contentPanel.Find("UserInfoPanel");
        firstNameText = userInfoPanel.Find("UserNameText").GetComponent<Text>();
        secondNameText = userInfoPanel.Find("UserLavelText").GetComponent<Text>();
        userIconImage = userInfoPanel.Find("UserIcon").GetComponent<Image>();
	}

	// Update is called once per frame
	void Update () {
        float speed = Input.GetAxis("Mouse ScrollWheel");
        if ( Mathf.Abs(speed) > Mathf.Epsilon)
        {
            scroller.value += speed / 2;
        }
        wwwLoader.update();
	}
}
