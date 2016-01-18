using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using JsonFx.Json;

public class UserInfoScreen : BaseUiController {

    private RectTransform contentPanel;
    private Scrollbar scroller;

    private RectTransform avalableGamesPanel;

    private SystemController systemController;

    private UserInfoController dataController;
    private WWWLoader wwwLoader = new WWWLoader();
    public MenuScreen loginScreen;

    private Text firstNameText;
    private Text secondNameText;
    private Image userIconImage;

    private Dictionary<string, System.Object> userInfo = null;
    private List<UserInventoryItem> inventory = null;

    public override void  Awake()
    {
 	    base.Awake();
        gameObject.SetActive(false);
        contentPanel = (RectTransform)transform.Find("ContentPanel");
        scroller = transform.Find("Scrollbar").GetComponent<Scrollbar>();
        messageBox = new MessageBox(transform.Find("MessageBox"));

        scroller.onValueChanged.AddListener(delegate(float value)
        {
            float screenHeight = Screen.height;
            float screenWidth = Screen.width;
            float allHeight = contentPanel.rect.height - screenHeight + 50f;
            float allWidth = contentPanel.rect.width;
            float yPos = value * -allHeight + allHeight / 2f + screenHeight / 2f;
            float xPos = (screenWidth - allWidth) / 2f + allWidth / 2;
            contentPanel.position = new UnityEngine.Vector2(xPos, yPos);
        });

        RectTransform userInfoPanel = (RectTransform)contentPanel.Find("UserInfoPanel");
        firstNameText = userInfoPanel.Find("UserNameText").GetComponent<Text>();
        secondNameText = userInfoPanel.Find("UserLavelText").GetComponent<Text>();
        userIconImage = userInfoPanel.Find("UserIcon").GetComponent<Image>();

        avalableGamesPanel = (RectTransform)contentPanel.Find("AvalableGamesPanel");
    }


    public override void startWindow()
    {
        base.startWindow();
        systemController = SystemController.get();
        gameObject.SetActive(true);
        dataController = new UserInfoController();
        startWait();
        dataController.requestUserInfo(onReceivedUserInfo);
    }

    public override void stopWindow()
    {
        base.stopWindow();
        gameObject.SetActive(false);
    }

    protected override void startWait()
    {
        base.startWait();
        contentPanel.gameObject.SetActive(false);
        scroller.gameObject.SetActive(false);
    }

    protected override void stopWait()
    {
        base.stopWait();
        contentPanel.gameObject.SetActive(true);
        scroller.gameObject.SetActive(true);
    }

    private void displayUserInfo()
    {
        string firstName = dataController.getFirstName(userInfo);
        string secondName = dataController.getSecondName(userInfo);
        string userIcon = dataController.getUserIcon(userInfo);

        systemController.getUserInfo().firstName = firstName;
        systemController.getUserInfo().secondtName = secondName;
        systemController.getUserInfo().userIcon = userIcon;

        firstNameText.text = firstName;
        secondNameText.text = secondName;

        wwwLoader.addLoader("load-user-icon", userIcon, delegate(WWW www, string tag)
        {
            if (www != null && www.texture != null)
            {
                Sprite iconSprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
                userIconImage.overrideSprite = iconSprite;
                systemController.getUserInfo().userIconImage = iconSprite;
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
        ui.SetParent(parent, false);
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
        uiButton.onClick.AddListener(delegate()
        {
            long gameId = item.itemId;
            selectedGame = systemController.getLibrary().get(item.descriptionId);
            BaseGameController gameController = selectedGame.createController();
            if (gameController != null)
            {
                systemController.requestGame(gameId, gameController, loadGameDelegate);
            }
            else
            {
                messageBox.showMessage("Fail to crate game controller for item : " + item);
            }
        });
    }

    private LibraryGame selectedGame;

    private void loadGameDelegate(object param)
    {
        RequestGameResults result = (RequestGameResults)param;
        switch (result)
        {
            case RequestGameResults.Success:
                BaseUiController ui = selectedGame.createUiController(transform.parent);
                //systemController.
                systemController.getCurrentGame().setUi(ui);
                systemController.startWindow(ui, true);
                break;
            default:
                messageBox.showMessage("Create game problemn, type = " + result);
                break;
        }
    }

    private void onReceivedUserInfo(object param)
    {
        KeyValuePair<UserInfoController.Result, Dictionary<string, object>> result = (KeyValuePair<UserInfoController.Result, Dictionary<string, object>>)param;
        stopWait();

        switch (result.Key)
        {
            case UserInfoController.Result.Success:
                {
                    userInfo = dataController.getUserInfo(result.Value);
                    displayUserInfo();

                    Dictionary<string, System.Object> userInventory = dataController.getUserInventory(result.Value);
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
                break;
            case UserInfoController.Result.InvalidData:
                messageBox.showMessage("Server error", "Server side problems. Wait few seconds and try again.", backToLoginScreen);
                break;
            case UserInfoController.Result.NetworkError:
                messageBox.showMessage("Network error", "Problem with internet. Check network connection and try again.", backToLoginScreen);
                break;
        }
    }


    private void backToLoginScreen()
    {
        systemController.startWindow(loginScreen, true);
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
