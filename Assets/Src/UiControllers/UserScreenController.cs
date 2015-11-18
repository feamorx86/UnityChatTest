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

    private RectTransform avalableGamesPanel;

    public Connection connection;
    public UserManager userManager;
    
    private UserInfoController dataController;
    private WWWLoader wwwLoader = new WWWLoader();

    private MessageBox messageBox;
	
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
        messageBox.showMessage("Error", "Network connection problem");
    }

    private void displayWaiting(bool show)
    {
        contentPanel.gameObject.SetActive(!show);
        scroller.gameObject.SetActive(!show);
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
            if (www != null && www.texture != null)
            {
                Sprite iconSprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
                userIconImage.overrideSprite = iconSprite;
            }
        });
    }

    private void dispayInventory()
    {
        if (inventory != null)
        {
            float startOffset = 80f;
            float width = 150f;
            float distance = 5f;
            for (int i = 0; i < inventory.Count; i++)
            {
                float x = startOffset + i * (width + distance);
                Vector2 position = new Vector2(x, 0);
                addAvalableGame(avalableGamesPanel, inventory[i], position);
            }
        }
    }


    private void addAvalableGame(Transform parent, UserInventoryItem item, Vector2 position)
    {
        RectTransform ui;
        Button uiButton;
        Text uiLabel;

        GameObject uiGameObject = (GameObject)GameObject.Instantiate(Resources.Load<GameObject>("UI/UserScreen/AvalableGame"));
        ui = (RectTransform)uiGameObject.transform;
        ui.parent = parent;
        ui.anchoredPosition = position;
        uiButton = ui.Find("Button").GetComponent<Button>();
        uiLabel = ui.Find("Text").GetComponent<Text>();

        wwwLoader.addLoader("AvalableGame" + item.itemId.ToString(), item.imageUri, delegate(WWW www, string tag)
        {
            if (www.texture != null)
            {
                Sprite iconSprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
                uiButton.image.overrideSprite = iconSprite;
            }
        });

        uiLabel.text = item.name;
        uiButton.onClick.RemoveAllListeners();
        long gameId = item.itemId;
        uiButton.onClick.AddListener(delegate()
        {
            startGame(gameId);
        });
    }

    private void startGame(long gameId)
    {

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
        gameObject.SetActive(false);
        panelWaiting = transform.Find("PanelWait");
        contentPanel = (RectTransform)transform.Find("ContentPanel");
        scroller = transform.Find("Scrollbar").GetComponent<Scrollbar>();
        messageBox = new MessageBox(transform.Find("MessageBox"));

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

        displayWaiting(true);

        RectTransform userInfoPanel = (RectTransform)contentPanel.Find("UserInfoPanel");
        firstNameText = userInfoPanel.Find("UserNameText").GetComponent<Text>();
        secondNameText = userInfoPanel.Find("UserLavelText").GetComponent<Text>();
        userIconImage = userInfoPanel.Find("UserIcon").GetComponent<Image>();

        avalableGamesPanel = (RectTransform)contentPanel.Find("AvalableGamesPanel");
	}

	// Update is called once per frame
    void Update()
    {
        if (scroller.gameObject.activeSelf)
        {
            float speed = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(speed) > Mathf.Epsilon)
            {
                scroller.value += speed / 2;
            }
        }
        wwwLoader.update();
	}
}
