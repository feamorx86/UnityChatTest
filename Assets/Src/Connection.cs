using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using MiscUtil.IO;
using MiscUtil.Conversion;


public class DataMessage
{
    int service;
    int action;
    string session;
    byte[] data;
    int dataLength;
    int dataOffset;

    public int DataLength
    {
        get { return dataLength; }
        set { dataLength = value; }
    }

    public int DataOffset
    {
        get { return dataOffset; }
        set { dataOffset = value; }
    }
    

    public int Action
    {
        get { return action; }
        set { action = value; }
    }
    

    public string Session
    {
        get { return session; }
        set { session = value; }
    }
    

    public byte[] Data
    {
        get { return data; }
        set { data = value; }
    }

    public int Service
    {
        get { return service; }
        set { service = value; }
    }




}

public class Connection : MonoBehaviour {

    public delegate bool DataHandledDelegate(DataMessage message);

    private TcpClient connection;

    public bool isConnected()
    {
        return connection.Connected;
    }

    private List<DataMessage> messagesToSend = new List<DataMessage>();
    private Dictionary<int, Dictionary<int, List<DataHandledDelegate>>> dataListeners = new Dictionary<int, Dictionary<int, List<DataHandledDelegate>>>();

    public void registerDataListener(int service, int action, DataHandledDelegate handler)
    {
        Dictionary<int, List<DataHandledDelegate>> serviceHandlers;
        if (dataListeners.TryGetValue(service, out serviceHandlers)) 
        {
            List<DataHandledDelegate> actionHandlers;
            if (serviceHandlers.TryGetValue(action, out actionHandlers))
            {
                actionHandlers.Add(handler);
            }
            else
            {
                actionHandlers = new List<DataHandledDelegate>();
                actionHandlers.Add(handler);
                serviceHandlers.Add(action, actionHandlers);
            }
        } 
        else 
        {
            serviceHandlers = new Dictionary<int, List<DataHandledDelegate>>();
            List<DataHandledDelegate> actionHandlers = new List<DataHandledDelegate>();
            actionHandlers.Add(handler);
            serviceHandlers.Add(action, actionHandlers);
            dataListeners.Add(service, serviceHandlers);
        }
    }

    public bool unregisterDataListener(int service, int action, DataHandledDelegate handler)
    {
        bool removed = false;
        Dictionary<int, List<DataHandledDelegate>> serviceHandlers;
        if (dataListeners.TryGetValue(service, out serviceHandlers))
        {
            List<DataHandledDelegate> actionHandlers;
            if (serviceHandlers.TryGetValue(action, out actionHandlers))
            {
                removed = actionHandlers.Remove(handler);
                if (actionHandlers.Count == 0)
                {
                    serviceHandlers.Remove(action);
                    if (serviceHandlers.Count == 0)
                    {
                        dataListeners.Remove(service);
                    }
                }
            }
        }
        return removed;
    }

    public void addMessageToSend(DataMessage message)
    {
        messagesToSend.Add(message);
    }

    private void notifyAction(DataMessage message)
    {
        if (message != null)
        {
            bool handled = false;
            Dictionary<int, List<DataHandledDelegate>> serviceHandlers;
            if (dataListeners.TryGetValue(message.Service, out serviceHandlers))
            {
                List<DataHandledDelegate> actionHandlers;
                if (serviceHandlers.TryGetValue(message.Action, out actionHandlers))
                {
                    List<DataHandledDelegate> completedHandlers = null;
                    foreach (DataHandledDelegate handler in actionHandlers)
                    {
                        if (handler(message))
                        {
                            if (completedHandlers == null) completedHandlers = new List<DataHandledDelegate>();
                            completedHandlers.Add(handler);
                        }
                    }
                    if (actionHandlers.Count > 0)
                        handled = true;
                    if (completedHandlers != null)
                    {
                        foreach (DataHandledDelegate handler in completedHandlers) 
                        {
                            actionHandlers.Remove(handler);
                        }
                        if (actionHandlers.Count <= 0)
                        {
                            serviceHandlers.Remove(message.Action);
                            if (serviceHandlers.Count <= 0)
                            {
                                dataListeners.Remove(message.Service);
                            }
                        }
                    }
                }
            }
            if (!handled)
            {
                Debug.LogWarning("Connection: unsupported message. Service : " + message.Service.ToString() + ", Action : " + message.Action.ToString());
            }
        }
                
    }

    public const string ADDRESS = "127.0.0.1";
    public const int PORT = 19790;

    private EndianBinaryWriter writer;
    private EndianBinaryReader reader;

    public void Awake()
    {
        try
        {
            connection = new TcpClient();
            connection.Connect(ADDRESS, PORT);
            writer = new EndianBinaryWriter(EndianBitConverter.Big, connection.GetStream());
            reader = new EndianBinaryReader(EndianBitConverter.Big, connection.GetStream());
        }
        catch (Exception ex)
        {
            Debug.LogError("fail to connect to remote server :"+ex);
        }
    }

    public void Update()
    {
        if (connection.Connected) 
        {
            sendMessages();
            receiveMessages();
        }
	}

    private float receiveMessageMaxTime = 300;

    private void receiveMessages()
    {
        float time = Time.timeSinceLevelLoad;
        DataMessage message = readMessage();
        while (message != null)
        {
            notifyAction(message);
            message = readMessage();
            float last = Time.timeSinceLevelLoad;
            if (last - time > receiveMessageMaxTime)
                break;
        }
    }

    public const int minHeaderSize = 4 //length
        + 4 //service
        + 4 //action
        + 4 //session lenght, session == null
        + 4; //data lenght, data == null

    private DataMessage currentMessage;
    private int currentLenght;

    private DataMessage readMessage()
    {
        DataMessage result = null;
        try
        {
            int avalable = connection.Available;
            if (currentMessage == null)
            {
                if (avalable >= minHeaderSize)
                {
                    currentMessage = new DataMessage();
                    currentLenght = reader.ReadInt32();
                }
                else
                {
                    return null;
                }
            }
            if (currentMessage != null)
            {
                if (avalable >= currentLenght)
                {
                    currentMessage.Service = reader.ReadInt32();
                    currentMessage.Action = reader.ReadInt32();
                    int sessionLenght = reader.ReadInt32();
                    if (sessionLenght > 0)
                    {
                        byte[] session = reader.ReadBytes(sessionLenght);
                        currentMessage.Session = Encoding.UTF8.GetString(session);
                    }
                    int dataLenght = reader.ReadInt32();
                    if (dataLenght > 0)
                    {
                        currentMessage.Data = reader.ReadBytes(dataLenght);
                        result = currentMessage;
                        currentMessage = null;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Fail to read message, ex : " + ex);
            result = null;
        }
        return result;
    }

    private void sendMessages()
    {
        if (connection.Connected)
        {
            if (messagesToSend.Count > 0)
            {
                try
                {
                    foreach (DataMessage mesasge in messagesToSend)
                    {
                        int length = 4 + 4;
                        byte[] session = null; ;
                        length += 4;
                        if (!string.IsNullOrEmpty(mesasge.Session))
                        {
                            session = Encoding.UTF8.GetBytes(mesasge.Session);
                            length += session.Length;
                        }

                        length += 4;
                        int dataLength = mesasge.DataLength - mesasge.DataOffset;
                        if (mesasge.Data != null && dataLength > 0)
                        {
                            length += dataLength;
                        }
                        
                        writer.Write(length);
                        writer.Write(mesasge.Service);
                        writer.Write(mesasge.Action);
                        if (session != null)
                        {
                            writer.Write(session.Length);
                            writer.Write(session, 0, session.Length);
                        }
                        else
                        {
                            writer.Write((int)0);
                        }

                        if (mesasge.Data != null)
                        {
                            writer.Write(mesasge.DataLength);
                            writer.Write(mesasge.Data, mesasge.DataOffset, mesasge.DataLength);
                        }
                        else
                        {
                            writer.Write((int)0);
                        }
                    }
                    connection.GetStream().Flush();
                    messagesToSend.Clear();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Fail to send messages error : " + ex);
                }

            }
        }
}

    public void onDestroy()
    {
        if (connection.Connected)
        {
            writer.Close();
            reader.Close();
            connection.Close();
        }
    }

}
