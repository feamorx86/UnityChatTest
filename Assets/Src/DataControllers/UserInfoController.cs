﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using System.Text;
using MiscUtil.IO;
using MiscUtil.Conversion;
using JsonFx.Json;

public class UserInfoController
{

    public enum Result{
        Success,
        InvalidData,
        NetworkError
    }

    
    public Dictionary<string, object> getUserInfo(Dictionary<string, object> json)
    {
        Dictionary<string, object> result = extractField<Dictionary<string, object>>("userInfo", json, null);
        return result;
    }

    public Dictionary<string, object> getUserInventory(Dictionary<string, object> json)
    {
        Dictionary<string, object> result = extractField<Dictionary<string, object>>("inventory", json, null);
        return result;
    }

    public Dictionary<string, object> getUserStatistics(Dictionary<string, object> json)
    {
        Dictionary<string, object> result = extractField<Dictionary<string, object>>("statistics", json, null);
        return result;
    }

    public static T extractField<T>(string field, Dictionary<string, object> json, T fallback)
    {
        object result = null;
        if (json != null && json.TryGetValue(field, out result))
        {
            if (!typeof(T).IsInstanceOfType(result))
            {
                result = null;
            }
        }
        return (T)result;
    }

    public string getFirstName(Dictionary<string, object> userInfo)
    {
        string result = extractField<string>("firstName", userInfo, "");
        return result;
    }

    public string getSecondName(Dictionary<string, object> userInfo)
    {
        string result = extractField<string>("secondName", userInfo, "");
        return result;
    }

    public string getUserIcon(Dictionary<string, object> userInfo)
    {
        string result = extractField<string>("icon", userInfo, "");
        return result;
    }

    public List<UserInventoryItem> getInventoryItems(Dictionary<string, object> userInventory)
    {
        List<UserInventoryItem> result = new List<UserInventoryItem>();
        object itemsObject = extractField<object[]>("items", userInventory, null);
        object[] items = (object[])itemsObject;
        if (items != null)
        {
            foreach (Dictionary<string, object> json in items)
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

    public static UserInventoryItem createInventoryItemFromJson(Dictionary<string, object> json) 
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

    private RunnableDelegate onCompleteDelegate;

    private bool onReceivedUserInfo(DataMessage msg)
    {
        if (msg.Data != null)
        {
            msg.createReader();
            int result = msg.readInt32();

            switch (result)
            {
                case Ids.SystemResults.SUCCESS:
                    {
                        Dictionary<String, Object> json = msg.readJson();
                        msg.closeReader();
                        Handler.getInstance().postAction(onCompleteDelegate, new KeyValuePair<Result, Dictionary<string, object>>(Result.Success, json));
                    }
                    break;
                case Ids.SystemResults.INVALID_DATA:
                case Ids.SystemResults.INVALID_SESSION:
                    {
                        msg.closeReader();
                        Handler.getInstance().postAction(onCompleteDelegate, new KeyValuePair<Result, Dictionary<string, object>>(Result.InvalidData, null));
                    }
                    break;
            }

        }
        return true;
    }

    public void requestUserInfo(RunnableDelegate onComplete)
    {
        Connection connection = Connection.getInstance();
        if (!connection.isConnected())
        {
            Handler.getInstance().postAction(onComplete, new KeyValuePair<Result, Dictionary<string, object>>(Result.NetworkError, null));
        }
        else
        {
            DataMessage message = new DataMessage(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.GET_FULL_USER_INFO, SystemController.get().getUserInfo().session);
            this.onCompleteDelegate = onComplete;
            connection.registerDataListener(message, onReceivedUserInfo);
            connection.registerErrorListener(Ids.ErrorHandlers.UserInfoController, onConnectionError);
            connection.addMessageToSend(message);
        }
    }

    private void onConnectionError(int senderId, int code, string message, object param)
    {
        Handler.getInstance().postAction(onCompleteDelegate, new KeyValuePair<Result, Dictionary<string, object>>(Result.NetworkError, null));
        Connection.getInstance().unregisterErrorListener(Ids.ErrorHandlers.UserInfoController);
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
