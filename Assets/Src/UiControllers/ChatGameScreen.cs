using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using JsonFx.Json;

public class ChatGameScreen : BaseUiController
{
    private RectTransform myInfoWindiow;
    private RectTransform usersWindow;
    private RectTransform messagesWindow;
    private RectTransform sendControlsPanel;


    private WWWLoader wwwLoader = new WWWLoader();
        
    private ChatGameController chatController;

    public float scrollSpeed = 1;
    
    private RectTransform usersList;
    private Scrollbar usersListScroller;
    private GameObject userItemPrefab;

    private Color selectedUserColor = new Color32(22, 253, 81, 100);
    private Color unselectedUserColor = new Color32(255, 255, 255, 100);

    private ChatGameController.ChatUser selectedUser = null;

    private void clearMessages() {
        for (int i = 0; i < messagesList.childCount; i++)
        {
            GameObject go = messagesList.GetChild(i).gameObject;
            go.SetActive(false);
            GameObject.DestroyObject(go);
        }
    }

    public void selectUser(RectTransform menuItem, int userId)
    {
        //unselect user
        for (int i = 0; i < usersList.childCount; i++)
        {
            GameObject go = usersList.GetChild(i).gameObject;
            go.GetComponent<Image>().color = unselectedUserColor;
        }
        clearMessages();
        selectedUser = chatController.getUser(userId);

        menuItem.GetComponent<Image>().color = selectedUserColor;

        ChatGameController.ChatDialogWithUser dialog = chatController.getDialog(userId);
        if (dialog != null)
        {
            UserInfo userInfo = SystemController.get().getUserInfo();
            foreach (ChatGameController.ChatDialogMessage message in dialog.messages)
            {
                if (userInfo.userId == message.senderId)
                {
                    addMessage(userInfo.firstName, message.message, true);
                }
                else
                {
                    addMessage(dialog.withUser.firstName, message.message, false);
                }
                
            }
        }
    }


    public RectTransform addUser(string firstName, string secondName, string imageUri, int id)
    {
        if (userItemPrefab == null)
        {
            userItemPrefab = (GameObject)Resources.Load("UI/ChatScreen/ChatUserInfo");
        }
        GameObject newUserGO = (GameObject)GameObject.Instantiate(userItemPrefab);
        newUserGO.name = "user_" + id.ToString();
        RectTransform newUserItem = (RectTransform)newUserGO.transform;
        newUserItem.SetParent(usersList, false);

        //setup user
        ChatUserInfo userId = newUserGO.GetComponent<ChatUserInfo>();
        userId.UserId = id;
        Text firstNameText = newUserItem.Find("FirstNameText").GetComponent<Text>();
        Text secondNameText = newUserItem.Find("SecondNameText").GetComponent<Text>();
        Image iconImage = newUserItem.Find("IconImage").GetComponent<Image>();
        Button selector = newUserItem.Find("Button").GetComponent<Button>();
        selector.onClick.AddListener(delegate()
        {
            selectUser(newUserItem, id);
        });

        firstNameText.text = firstName;
        secondNameText.text = secondName;

        wwwLoader.addLoader("ChatIconInfo" + id.ToString(), imageUri, delegate(WWW www, string tag)
        {
            if (www != null && String.IsNullOrEmpty(www.error) && www.texture != null)
            {
                Sprite iconSprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
                iconImage.overrideSprite = iconSprite;
            }
            else
            {
                Debug.LogError("Fail to load image for : <" + tag + ">, Error :\n"+www.error);
            }
        });

        float posY, height;

        height = newUserItem.rect.height;
        posY = height / -2f;        

        if (usersList.childCount > 1)
        {
            RectTransform lastUserItem = null;
            for (int i = usersList.childCount - 2; i >= 0; i--)
            {
                if (usersList.GetChild(i).gameObject.activeSelf)
                {
                    lastUserItem = (RectTransform)usersList.GetChild(i);
                    break;
                }
            }

            if (lastUserItem != null)
            {
                float lastOffset = lastUserItem.anchoredPosition.y + lastUserItem.rect.height / -2f;
                posY += lastOffset;
            }
        }

        newUserItem.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        newUserItem.anchoredPosition = new UnityEngine.Vector2(0, posY);

        float listHeight = -posY + height / 2f;
        float listY = listHeight / -2f;
        usersList.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listHeight);
        usersList.anchoredPosition = new Vector2(usersList.anchoredPosition.x, listY);
        return newUserItem;
    }

    public void updateUser(string firstName, string secondName, string imageUri, int id)
    {
        RectTransform userItem = null;
        for (int i = 0; i < usersList.childCount; i++)
        {
            ChatUserInfo userInfo = usersList.GetChild(i).GetComponent<ChatUserInfo>();
            if (userInfo.UserId == id)
            {
                userItem = (RectTransform)usersList.GetChild(i);
            }
        }

        if (userItem != null)
        {
            Text firstNameText = userItem.Find("FirstNameText").GetComponent<Text>();
            Text secondNameText = userItem.Find("SecondNameText").GetComponent<Text>();
            Image iconImage = userItem.Find("IconImage").GetComponent<Image>();
            Button selector = userItem.Find("Button").GetComponent<Button>();


            selector.onClick.AddListener(delegate()
            {
                selectUser(userItem, id);
            });

            firstNameText.text = firstName;
            secondNameText.text = secondName;

            wwwLoader.addLoader("ChatIconInfo" + id.ToString(), imageUri, delegate(WWW www, string tag)
            {
                if (www != null && String.IsNullOrEmpty(www.error) && www.texture != null)
                {
                    Sprite iconSprite = Sprite.Create(www.texture, new Rect(0, 0, www.texture.width, www.texture.height), new Vector2(0.5f, 0.5f));
                    iconImage.overrideSprite = iconSprite;
                }
                else
                {
                    Debug.LogError("Fail to load image for : <" + tag + ">, Error :\n" + www.error);
                }
            });
        }
        else
        {
            addUser(firstName, secondName, imageUri, id);
        }
    }

    private GameObject messagePrefab = null;
    private RectTransform messagesList;
    private Scrollbar messagesListScroller;

    private Button sendMessageButton;
    private InputField sendMessageInputField;


    public void addMessage(String from, String message, bool isMyMessage)
    {
        if (messagePrefab == null) {
            messagePrefab = (GameObject)Resources.Load("UI/ChatScreen/ChatMessage");
        }

        GameObject newMessageGO = (GameObject)GameObject.Instantiate(messagePrefab);
        RectTransform newMessage = (RectTransform)newMessageGO.transform;
        newMessage.SetParent(messagesList, false);

        //setup user
        Text userText = newMessage.Find("User").GetComponent<Text>();
        Text messageText = newMessage.Find("Text").GetComponent<Text>();

        userText.text = from;
        messageText.text = message;

        float elementsOffset = 5f;
        //float textMessageOffset = 20f;
        float innerHeightOffset = 5;
        float posY, height, textHeight;

        textHeight = messageText.preferredHeight;

        float textMessageOffset = innerHeightOffset + userText.rectTransform.rect.height;
        height = textMessageOffset + textHeight + innerHeightOffset;

    
        posY = height / -2f - elementsOffset;

        if (messagesList.childCount > 1)
        {
            RectTransform lastMessage = null;
            for (int i = messagesList.childCount - 2; i >= 0; i--)
            {
                if (messagesList.GetChild(i).gameObject.activeSelf)
                {
                    lastMessage = (RectTransform)messagesList.GetChild(i);
                    break;
                }
            }

            if (lastMessage != null)
            {
                float lastOffset = lastMessage.anchoredPosition.y + lastMessage.rect.height / -2f;
                posY += lastOffset;
            }
        }


        messageText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
        messageText.rectTransform.anchoredPosition = new Vector2(messageText.rectTransform.anchoredPosition.x, -textMessageOffset + textHeight / -2f);

        newMessage.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        newMessage.anchoredPosition = new UnityEngine.Vector2(0, posY);

        if (isMyMessage)
            userText.color = new Color(0.1f, 0.6f, 0.1f);
        else
            userText.color = Color.red; 

        float listHeight = -posY + height / 2f + elementsOffset;
        float listY = listHeight / -2f;
        messagesList.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, listHeight);
        messagesList.anchoredPosition = new Vector2(messagesList.anchoredPosition.x, listY);

        messagesListScroller.value = 1;
        messagesListScroller.onValueChanged.Invoke(1f);
    }

    public void setChatController(ChatGameController value)
    {
        chatController = value;
    }

    public override void Awake()
    {
        base.Awake();

        gameObject.SetActive(true);        
        myInfoWindiow = (RectTransform)transform.Find("MyWindow");
        usersWindow = (RectTransform)transform.Find("UsersWindow");

        RectTransform dataWindow = (RectTransform)transform.Find("DataWindow");

        messagesWindow = (RectTransform)dataWindow.Find("MessagesPanel");
        sendControlsPanel = (RectTransform)dataWindow.Find("ControlsPanel");

        messagesList = (RectTransform)messagesWindow.Find("MessagesList");
        messagesListScroller = messagesWindow.Find("MessagesScrollbar").GetComponent<Scrollbar>();
        messagesListScroller.onValueChanged.AddListener(delegate(float value)
        {
            float listHeight = messagesList.rect.height;
            float windowHeight = ((RectTransform)messagesListScroller.transform).rect.height;
            if (listHeight > windowHeight)
            {
                float yPos = (listHeight / -2f) + value * (listHeight - windowHeight);
                messagesList.anchoredPosition = new UnityEngine.Vector2(usersList.anchoredPosition.x, yPos);
            }
        });

        sendMessageButton = sendControlsPanel.Find("SendButton").GetComponent<Button>();
        sendMessageButton.onClick.AddListener(onSendMessageClick);
        sendMessageInputField = sendControlsPanel.Find("InputField").GetComponent<InputField>();

        usersList = (RectTransform)usersWindow.Find("UsersList");
        usersListScroller = usersWindow.Find("UsersListScroller").GetComponent<Scrollbar>();
        usersListScroller.onValueChanged.AddListener(delegate(float value)
        {
            float listHeight = usersList.rect.height;
            float windowHeight = ((RectTransform)usersListScroller.transform).rect.height;
            if (listHeight > windowHeight)
            {
                float yPos = (listHeight / -2f) + value * (listHeight - windowHeight);
                usersList.anchoredPosition = new UnityEngine.Vector2(usersList.anchoredPosition.x, yPos);
            }
        });

        startWait();
    }

    public override void updateUi(object param)
    {
        base.updateUi(param);
        KeyValuePair<ChatGameController.UiUpdates, object> info = (KeyValuePair<ChatGameController.UiUpdates, object>)param;
        switch (info.Key)
        {
            case ChatGameController.UiUpdates.UpdateUsersList:
                displayeUsers();
                break;
            case ChatGameController.UiUpdates.GameIsReady:
                stopWait();
                break;
            case ChatGameController.UiUpdates.NewUser:
                {
                    ChatGameController.ChatUser user = (ChatGameController.ChatUser)info.Value;
                    addUser(user.firstName, user.secondName, user.iconUri, user.userId);
                }
                break;
            case ChatGameController.UiUpdates.UserRemoved:
                {
                    if (selectedUser!= null && selectedUser.userId == (int)info.Value)
                    {
                        selectedUser = null;
                        clearMessages();
                    }
                    displayeUsers();
                }
                break;
            case ChatGameController.UiUpdates.UserUpdated:
                {
                    ChatGameController.ChatUser user = (ChatGameController.ChatUser)info.Value;
                    updateUser(user.firstName, user.secondName, user.iconUri, user.userId);
                }
                break;
            case ChatGameController.UiUpdates.ReceivedMessage:
                {
                    ChatGameController.ChatDialogMessage message = (ChatGameController.ChatDialogMessage)info.Value;
                    if (selectedUser != null && selectedUser.userId == message.senderId)
                    {
                        addMessage(selectedUser.firstName, message.message, false);
                    }
                }
                break;
        }
    }

    private void displayeUsers()
    {
        for (int i = 0; i < usersList.childCount; i++)
        {
            GameObject go = usersList.GetChild(i).gameObject;
            go.SetActive(false);
            GameObject.DestroyObject(go);
        }
        if (selectedUser != null) 
        {
            RectTransform item = null;
            foreach (ChatGameController.ChatUser user in chatController.getAllUsers().Values)
            {
                if (selectedUser == user) 
                {
                    item = addUser(user.firstName, user.secondName, user.iconUri, user.userId);
                }
            }
            if (item != null)
            {
                selectUser(item, selectedUser.userId);
            }
        }
        else
        {
            foreach (ChatGameController.ChatUser user in chatController.getAllUsers().Values)
            {
                addUser(user.firstName, user.secondName, user.iconUri, user.userId);
            }
        }
    }

    private Boolean isMouseInMessagesWindow = false;

    public void onMouseEnterMessagesWindow()
    {
        isMouseInMessagesWindow = true;
    }

    public void onMouseExitMessagesWindow()
    {
        isMouseInMessagesWindow = false;
    }

    private Boolean isMouseInUsersWindow = false;

    public void onMouseEnterUsersWindow()
    {
        isMouseInUsersWindow = true;
    }

    public void onMouseExitUsersWindow()
    {
        isMouseInUsersWindow = false;
    }

    int counter = 0;

    public void onSendMessageClick()
    {
        if (selectedUser != null && !String.IsNullOrEmpty(sendMessageInputField.text))
        {
            string message = sendMessageInputField.text;
            sendMessageInputField.text = "";
            addMessage(SystemController.get().getUserInfo().firstName, message, true);
            chatController.sendMessage(selectedUser, message);
        }
    }

    void Update()
    {
        if (isMouseInMessagesWindow)
        {
            float speed = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(speed) > Mathf.Epsilon)
            {
               speed = speed * (((RectTransform)messagesListScroller.transform).rect.height / messagesList.rect.height) * scrollSpeed;
               messagesListScroller.value -= speed;
            }
        }

        if (isMouseInUsersWindow)
        {
            float speed = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(speed) > Mathf.Epsilon)
            {
                speed = speed * (((RectTransform)usersListScroller.transform).rect.height / usersList.rect.height) * scrollSpeed;
                usersListScroller.value -= speed;
            }
        }

        wwwLoader.update();
    }
}