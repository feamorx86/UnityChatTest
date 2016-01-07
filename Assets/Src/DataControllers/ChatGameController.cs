using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using System.Text;
using MiscUtil.IO;
using MiscUtil.Conversion;
using UnityEngine;

public class ChatGameController : BaseGameController
{
    public enum UiUpdates
    {
        UpdateUsersList,
        UserRemoved,
        NewUser,
        UserUpdated,
        GameIsReady,
        ReceivedMessage,
        MessageSended
    }


    private Dictionary<int, ChatUser> users = new Dictionary<int, ChatUser>();
    private Dictionary<int, ChatDialogWithUser> dialogs = new Dictionary<int, ChatDialogWithUser>();
    private ChatUser userInfo;

    public Dictionary<int, ChatUser> getAllUsers()
    {
        return users;
    }
    
    public ChatGameController() : base()
    {
        
    }

    private SystemController systemController;
    private Connection connection;

    private void registerListeners()
    {
        
        connection.registerDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.NEW_USER, onUserEnter);
        connection.registerDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.USER_EXIT, onUserExit);
        connection.registerDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.LIST_USERS, onReceivePlayersList);
        connection.registerDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.RECEIVE_MESSAGE, onReceiveMessage);
        connection.registerDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.SEND_MESSAGE_RESULT, onMessageSenedResult);
        connection.registerDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.REQUEST_USER_INFO, onReceiveUserInfo);
    }

    private int myCallback;

    public void requestUsersList()
    {
        connection.registerDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.REQUEST_USERS_LIST, errorsHandler);
        DataMessage message = new DataMessage(Ids.Services.GAMES, Ids.Actions.SimpleChat.REQUEST_USERS_LIST, systemController.getUserInfo().session);
        message.createWriter().writerInt32(GameId).writerInt32(myCallback).closeWriter();
        connection.addMessageToSend(message);
    }

    public void sendUserReady()
    {
        DataMessage message = new DataMessage(Ids.Services.GAMES, Ids.Actions.SimpleChat.NEW_USER, systemController.getUserInfo().session);
        message.createWriter().writerInt32(GameId).closeWriter();
        connection.addMessageToSend(message);
    }

    public void sendMessage(ChatUser toUser, String textMessage)
    {
        ChatDialogMessage dialogMessage = new ChatDialogMessage(textMessage, systemController.getUserInfo().userId, -1);
        if (dialogs.ContainsKey(toUser.userId))
        {
            dialogs[toUser.userId].messages.Add(dialogMessage);
        }
        else
        {
            ChatDialogWithUser dialog = new ChatDialogWithUser();
            dialog.withUser = toUser;
            dialog.messages.Add(dialogMessage);
            dialogs.Add(toUser.userId, dialog);
        }

        DataMessage message = new DataMessage(Ids.Services.GAMES, Ids.Actions.SimpleChat.SEND_MESSAGE, systemController.getUserInfo().session);
        message.createWriter()
            .writerInt32(GameId)
            .writerInt32(toUser.userId)
            .writerInt32(myCallback)
            .writerString(textMessage)
            .closeWriter();

        connection.addMessageToSend(message);

    }

    public override void startGame()
    {
        base.startGame();

        systemController = SystemController.get();
        connection = Connection.getInstance();

        myCallback = UnityEngine.Random.Range(0, 100);

        registerListeners();
        requestUsersList();
    }

    public void notifyIAmEnter()
    {
        DataMessage message = new DataMessage(Ids.Services.GAMES, Ids.Actions.SimpleChat.NEW_USER, systemController.getUserInfo().session);
        connection.addMessageToSend(message);
    }

    private bool onUserEnter(DataMessage message)
    {
        message.createReader();
        Dictionary<string, object> json = message.readJson();
        message.closeReader();

        ChatUser u = new ChatUser();
        if (u.fromJson(json))
        {
            if (u.userId == systemController.getUserInfo().userId)
            {
                postUpdate(UiUpdates.GameIsReady, null);
            }
            else
            {
                if (!users.ContainsKey(u.userId))
                {
                    users.Add(u.userId, u);
                }
                postUpdate(UiUpdates.NewUser, u);
            }
        }

        return false;
    }

    private bool onUserExit(DataMessage message)
    {
        message.createReader();
        int userId = message.readInt32();
        message.closeReader();

        users.Remove(userId);
        postUpdate(UiUpdates.UpdateUsersList, userId);
        return false;
    }

    private bool onReceivePlayersList(DataMessage message)
    {
        message.createReader();
        int callback = message.readInt32();

        if (callback != myCallback)
        {
            Debug.LogError("receive users list, but invalid Callback");
        }

        Dictionary<string, object> json = message.readJson();
        message.closeReader();
        object[] usersJson = (object[])json["users"];
        for (int i = 0; i < usersJson.Length; i++)
        {
            ChatUser u = new ChatUser();
            if (u.fromJson((Dictionary<string, object>)usersJson[i]))
            {
                users[u.userId] = u;
            }
        }
        postUpdate(UiUpdates.UpdateUsersList, null);
        sendUserReady();
        return false;
    }

    private void requestUserInfo(int userId)
    {
        DataMessage message = new DataMessage(Ids.Services.GAMES, Ids.Actions.SimpleChat.REQUEST_USER_INFO, systemController.getUserInfo().session);
        message.createWriter().writerInt32(GameId).writerInt32(userId).writerInt32(myCallback).closeWriter();
        connection.addMessageToSend(message);
    }

    private bool onReceiveUserInfo(DataMessage message)
    {
        message.createReader();
        int callback = message.readInt32();
        int result = message.readInt32();
        
        switch(result) {
            case Ids.SystemResults.SUCCESS:
                {
                    int requestedUsedId = message.readInt32();
                    Dictionary<string, object> json = message.readJson();
                    message.closeReader();

                    ChatUser user;
                    if (users.ContainsKey(requestedUsedId)) 
                    {
                        user = users[requestedUsedId];
                    } 
                    else
                    {
                        user = new ChatUser();
                        users.Add(requestedUsedId, user);
                    }
                    if (!user.fromJson(json))
                    {
                        Debug.LogError("ReceiveUserInfo : fail to parse user data for "+requestedUsedId.ToString());
                        user.setUnknown(requestedUsedId);
                    }
                    postUpdate(UiUpdates.UserUpdated, user);
                }
                break;
            case Ids.Actions.SimpleChat.RESULT_NO_USER_WITH_SUCH_ID:
                {
                    int requestedUsedId = message.readInt32();
                    message.closeReader();
                    Debug.LogError("ReceiveUserInfo : requested User not found id: " + requestedUsedId.ToString());
                }
                break;
            case Ids.SystemResults.INVALID_SESSION:
                message.closeReader();
                Debug.LogError("ReceiveUserInfo : Internal error - Invalid session");
                break;
            case Ids.SystemResults.INVALID_DATA:
                message.closeReader();
                Debug.LogError("ReceiveUserInfo : Internal error - Invalid data");
                break;
            case Ids.SystemResults.NO_GAME_WITH_SUCH_ID:
                message.closeReader();
                Debug.LogError("ReceiveUserInfo : Internal error - no game with such id");
                break;
        }
        
        return false;
    }

    private bool onReceiveMessage(DataMessage message)
    {
        message.createReader();
        int userId = message.readInt32();
        string userMessage = message.readString();
        message.closeReader();

        ChatUser user = getUser(userId);
        if (user == null)
        {
            user = new ChatUser();
            user.setUnknown(userId);
            users.Add(userId, user);
            requestUserInfo(userId);
        }

        ChatDialogMessage dialogMessage = new ChatDialogMessage(userMessage, user.userId, -1);
        if (dialogs.ContainsKey(userId))
        {
            dialogs[userId].messages.Add(dialogMessage);
        }
        else
        {
            ChatDialogWithUser dialog = new ChatDialogWithUser();
            dialog.withUser = user;
            dialog.messages.Add(dialogMessage);
            dialogs.Add(userId, dialog);
        }
        postUpdate(UiUpdates.ReceivedMessage, dialogMessage);
        return false;
    }

    private bool onMessageSenedResult(DataMessage message)
    {
        message.createReader();
        int result = message.readInt32();
        int callback = message.readInt32();
        message.closeReader();

        if (result != Ids.SystemResults.SUCCESS)
        {    
            Debug.LogError("Fail to send message for callback : " + callback.ToString());
        }
        else
        {
            if (callback != myCallback)
            {
                Debug.LogError("receive send message result, but invalid Callback");
            }
        }
        postUpdate(UiUpdates.MessageSended, callback);
        return false;
    }

    private bool errorsHandler(DataMessage message)
    {
        message.createReader();
        int result = message.readInt32();
        message.closeReader();

        if (!checkErrorResult(result))
        {
            uiController.getMessageBox().showMessage("Internal error!");
        }

        return true;
    }

    public override void finishGame(int code, object parameters)
    {
        connection.unregisterDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.NEW_USER);
        connection.unregisterDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.USER_EXIT);
        connection.unregisterDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.LIST_USERS);
        connection.unregisterDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.RECEIVE_MESSAGE);
        connection.unregisterDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.SEND_MESSAGE_RESULT);
        connection.unregisterDataListener(Ids.Services.GAMES, Ids.Actions.SimpleChat.REQUEST_USER_INFO);

        base.finishGame(code, parameters);
    }

    public override void setUi(BaseUiController uiController)
    {
        base.setUi(uiController);
        ((ChatGameScreen)uiController).setChatController(this);
    }
    
    private int actionCounter = 0;
    private int next()
    {
        int value = actionCounter;
        actionCounter++;
        return value;
    }

    private void postUpdate(UiUpdates updates, object param)
    {
        Handler.getInstance().postAction(updateUi, new KeyValuePair<UiUpdates, object>(updates, param));
    }

    public ChatUser getUser(int id)
    {
        ChatUser user;
        user = users.TryGetValue(id, out user) ? user : null;
        return user;
    }

    public ChatDialogWithUser getDialog(int withUser)
    {
        ChatDialogWithUser dialog;
        dialog = dialogs.TryGetValue(withUser, out dialog) ? dialog : null;
        return dialog;
    }

    public class ChatUser
    {
        public string firstName;
        public string secondName;
        public string iconUri;

        public int userId;

        public bool fromJson(Dictionary<string, object> json)
        {
            bool result = false;
            try
            {
                userId = (Int32)json["id"];
                firstName = (string)json["firstName"];
                secondName = (string)json["secondName"];
                iconUri = (string)json["iconUri"];
                result = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Fail to parse ChatUser json : " + json + ", error :" + ex.ToString());
            }
            return result;
        }

        public void setUnknown(int userId)
        {
            this.userId = userId;
            firstName = "Unknown";
            secondName = "";
            iconUri = "";
        }
    }

    public class ChatDialogMessage
    {
        public string message;
        public int senderId;
        public int id;
        public DateTime time;

        public ChatDialogMessage(string message, int senderId, int id)
        {
            this.message = message;
            this.senderId = senderId;
            this.id = id;
            time = DateTime.Now;
        }
    }

    public class ChatDialogWithUser
    {
        public ChatUser withUser;
        public List<ChatDialogMessage> messages = new List<ChatDialogMessage>();
    }

}