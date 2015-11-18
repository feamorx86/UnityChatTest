using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using System.Text;
using MiscUtil.IO;
using MiscUtil.Conversion;
using JsonFx.Json;

class UserInfoController
{
    private Connection connection;
    private UnityAction networkErrorHandler;
    private UserManager userManager;

    public UserInfoController(Connection connection, UnityAction onNetworkError, UserManager userManager)
    {
        this.connection = connection;
        this.networkErrorHandler = onNetworkError;
        this.userManager = userManager;
    }

    public delegate void UserInfoResultDelegate(int result, Dictionary<string, System.Object> fullUserInfo);
    public delegate void RequestGameDelegate(int result);

    public Dictionary<String, Object> getUserInfo(Dictionary<String, Object> json)
    {
        Dictionary<String, Object> result = extractField<Dictionary<String, Object>>("userInfo", json, null);
        return result;
    }

    public Dictionary<String, Object> getUserInventory(Dictionary<String, Object> json)
    {
        Dictionary<String, Object> result = extractField<Dictionary<String, Object>>("inventory", json, null);
        return result;
    }

    public Dictionary<String, Object> getUserStatistics(Dictionary<String, Object> json)
    {
        Dictionary<String, Object> result = extractField<Dictionary<String, Object>>("statistics", json, null);
        return result;
    }

    public static T extractField<T>(string field, Dictionary<String, Object> json, T fallback)
    {
        Object result = null;
        if (json != null && json.TryGetValue(field, out result))
        {
            if (!typeof(T).IsInstanceOfType(result))
            {
                result = null;
            }
        }
        return (T)result;
    }

    public String getFirstName(Dictionary<String, Object> userInfo)
    {
        string result = extractField<String>("firstName", userInfo, "");
        return result;
    }

    public String getSecondName(Dictionary<String, Object> userInfo)
    {
        string result = extractField<String>("secondName", userInfo, "");
        return result;
    }

    public String getUserIcon(Dictionary<String, Object> userInfo)
    {
        string result = extractField<String>("icon", userInfo, "");
        return result;
    }

    public List<UserInventoryItem> getInventoryItems(Dictionary<String, Object> userInventory)
    {
        List<UserInventoryItem> result = new List<UserInventoryItem>();
        object itemsObject = extractField<Object[]>("items", userInventory, null);
        Object[] items = (Object[])itemsObject;
        if (items != null)
        {
            foreach (Dictionary<String, Object> json in items)
            {
                UserInventoryItem item = createInventoryItemFromJson(json);
                if (item != null)
                {
                    result.Add(item);
                }
            }
        }
        return result;
    }

    public static UserInventoryItem createInventoryItemFromJson(Dictionary<String, Object> json) 
    {
        UserInventoryItem item = null;
        if (json !=null) 
        {
            try {
                item = new UserInventoryItem();
                    
                item.type = Convert.ToInt32(json["type"]);
                item.itemId = Convert.ToInt64(json["itemId"]);
                item.descriptionId = Convert.ToInt32(json["descriptionId"]);
                item.count = Convert.ToInt32(json["count"]);
                item.name = Convert.ToString(json["name"]);
                item.description = Convert.ToString(json["description"]);
                item.imageUri = Convert.ToString(json["imageUri"]);
            }catch (Exception ex) {
                item = null;
            }                
        }
        return item;
        
    }

    public void requestUserInfo(UserInfoResultDelegate onComplete)
    {
        if (!connection.isConnected())
        {
            networkErrorHandler();
            return;
        }

        DataMessage message = new DataMessage();
        message.Service = Ids.Services.GAME_RESLOVER;
        message.Action = Ids.Actions.GameResolver.GET_FULL_USER_INFO;
        message.Session = userManager.getSession();
        
        Connection.DataHandledDelegate getUserInfoDelegate = null;
        getUserInfoDelegate = delegate(DataMessage msg)
        {
            if (msg.Data != null)
            {
                MemoryStream answareStream = new MemoryStream(msg.Data);
                EndianBinaryReader answareReader = new EndianBinaryReader(BigEndianBitConverter.Big, answareStream);
                int result = answareReader.ReadInt32();

                if (result == Ids.GameResloverResults.SUCCESS)
                {
                    int jsonLenght = answareReader.ReadInt32();
                    String jsonString = null;
                    if (jsonLenght > 0)
                    {
                        byte[] jsonData = answareReader.ReadBytes(jsonLenght);
                        jsonString = Encoding.UTF8.GetString(jsonData);


                        answareReader.Close();
                        answareStream.Close();

                        JsonReader jr = new JsonReader();
                        Dictionary<String, Object> json = jr.Read<Dictionary<string, object>>(jsonString);

                        onComplete(result, json);
                    }
                    else
                    {
                        answareReader.Close();
                        answareStream.Close();
                        onComplete(result, null);
                    }
                }
                else
                {
                    onComplete(result, null);
                }

            }
            return true;
        };
        connection.registerDataListener(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.GET_FULL_USER_INFO, getUserInfoDelegate);
        connection.addMessageToSend(message);
    }

    private enum RequestGameStates {
        NotRequestd,
        Requested,
        ReqeustComplete,
        GameStarted
    }

    private RequestGameStates reqeustState = RequestGameStates.NotRequestd;
    private RequestGameDelegate onGameRequestCompleteDelegate = null;
    private RequestGameDelegate onGameStartedDelegate = null;

    public void requestGame(RequestGameDelegate onComplete, RequestGameDelegate onGameStartedDelegate, long gameId)
    {
        this.onGameRequestCompleteDelegate = onComplete;
        this.onGameStartedDelegate = onGameStartedDelegate;

        reqeustState = RequestGameStates.NotRequestd;
        if (!connection.isConnected())
        {
            networkErrorHandler();
            return;
        }

        DataMessage message = new DataMessage();
        message.Service = Ids.Services.GAME_RESLOVER;
        message.Action = Ids.Actions.GameResolver.START_GAME_REQUEST;
        message.Session = userManager.getSession();

        MemoryStream stream = new MemoryStream(8);
        EndianBinaryWriter writer = new EndianBinaryWriter(BigEndianBitConverter.Big, stream);
        writer.Write(gameId);
        writer.Flush();
        message.Data = stream.GetBuffer();
        message.DataOffset = 0;
        message.DataLength = (int)stream.Position;
        writer.Close();
        stream.Close();
        
        reqeustState = RequestGameStates.Requested;
        connection.registerDataListener(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.GET_FULL_USER_INFO, onGameRequestComplete);
        connection.registerDataListener(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.GAME_STARTED, onGameStarted);
        connection.addMessageToSend(message);
    }

    private bool onGameRequestComplete(DataMessage msg)
    {
        if (msg.Data != null)
        {
            MemoryStream answareStream = new MemoryStream(msg.Data);
            EndianBinaryReader answareReader = new EndianBinaryReader(BigEndianBitConverter.Big, answareStream);
            int result = answareReader.ReadInt32();
            answareReader.Close();
            answareStream.Close();
            if (result != Ids.GameResloverResults.SUCCESS)
            {
                //cancel start game handler
                if (reqeustState == RequestGameStates.Requested)
                {
                    connection.unregisterDataListener(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.GAME_STARTED, onGameStarted);
                }
            }
            if (reqeustState == RequestGameStates.Requested)
            {
                onGameRequestCompleteDelegate(result);
            }
            reqeustState = RequestGameStates.ReqeustComplete;
        }
        return true;

    }

    private bool onGameStarted(DataMessage message)
    {
        if (reqeustState == RequestGameStates.Requested)
        {
            onGameRequestCompleteDelegate(Ids.GameResloverResults.SUCCESS);
            connection.unregisterDataListener(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.GET_FULL_USER_INFO, onGameRequestComplete);
        }
        reqeustState = RequestGameStates.GameStarted;
        if (message.Data != null)
        {
            MemoryStream answareStream = new MemoryStream(message.Data);
            EndianBinaryReader answareReader = new EndianBinaryReader(BigEndianBitConverter.Big, answareStream);
            int result = answareReader.ReadInt32();
            answareReader.Close();
            answareStream.Close();

            onGameStartedDelegate(result);
        }
        return true;
    }
}

public class UserInventoryItem
{
    public int type;
    public long itemId;
    public int descriptionId;
    public int count;
    public string name;
    public string description;
    public string imageUri;
}

