using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.IO;
using MiscUtil.IO;
using MiscUtil.Conversion;
using JsonFx.Json;
using UnityEngine.Events;

public delegate bool DataHandledDelegate(DataMessage message);
public delegate void ConnectionErrorDelegate(int senderId, int code, string message, object param);

public class Connection : MonoBehaviour {

    public enum ConnectionErrors
    {
        UNKNOWN = 0,
        CONNECT = 1,
        SEND_MESSAGE = 2,
        RECEIVE_MESSAGE = 3
    }


    public const string DEFAULT_ADDRESS = "127.0.0.1";
    public const int DEFAULT_PORT = 19790;

    public const float receiveMessageMaxTime = 300;
    public const int minHeaderSize = 4 //length
        + 4 //service
        + 4 //action
        + 4 //session lenght, session == null
        + 4; //data lenght, data == null


    private static Connection instance = null;

    private TcpClient connection;
    private Stack<DataMessage> messagesToSend = new Stack<DataMessage>();
    private Dictionary<int, Dictionary<int, DataHandledDelegate>> dataListeners = new Dictionary<int, Dictionary<int, DataHandledDelegate>>();
    private Dictionary<int, ConnectionErrorDelegate> errorHandlers = new Dictionary<int, ConnectionErrorDelegate>();

    private EndianBinaryWriter writer;
    private EndianBinaryReader reader;
    
    private string serverAddress = DEFAULT_ADDRESS;
    private int serverPort = DEFAULT_PORT;
        
    private DataMessage currentMessage;
    private int currentLenght;


    public static Connection getInstance()
    {
        return instance;
    }

    public void Awake()
    {
        instance = this;
    }

    public bool connect()
    {
        bool result = false;
        lock (this)
        {
            clear();
            try
            {
                connection = new TcpClient();
                connection.Connect(serverAddress, serverPort);
                writer = new EndianBinaryWriter(EndianBitConverter.Big, connection.GetStream());
                reader = new EndianBinaryReader(EndianBitConverter.Big, connection.GetStream());
                result = true;
            }
            catch (Exception ex)
            {
                result = false;
                Debug.LogError("Fail to connect to remote server (" + serverAddress.ToString() + ":" + serverPort.ToString() + ") error :" + ex);
                fireErrorHandlers(ConnectionErrors.CONNECT, "Connect to server error.", ex);
            }
        }
        return result;
    }

    public bool isConnected()
    {
        return connection.Connected;
    }

    public void Update()
    {
        if (connection != null && connection.Connected)
        {
            sendMessages();
            receiveMessages();
        }
    }

    public void setServerAddress(string address, int port)
    {
        this.serverAddress = address;
        this.serverPort = port;
    }

    public void registerDataListener(DataMessage forMessage, DataHandledDelegate handler)
    {
        registerDataListener(forMessage.Service, forMessage.Action, handler);
    }

    public void registerDataListener(int service, int action, DataHandledDelegate handler)
    {
        lock (dataListeners)
        {
            Dictionary<int, DataHandledDelegate> serviceHandlers;
            if (dataListeners.TryGetValue(service, out serviceHandlers))
            {
                serviceHandlers[action] = handler;
            }
            else
            {
                serviceHandlers = new Dictionary<int, DataHandledDelegate>();
                serviceHandlers[action] = handler;
                dataListeners[service] = serviceHandlers;
            }
        }
    }

    public bool unregisterDataListener(int service, int action)
    {
        bool removed = false;
        lock (dataListeners)
        {
            Dictionary<int, DataHandledDelegate> serviceHandlers;
            if (dataListeners.TryGetValue(service, out serviceHandlers))
            {
                removed = serviceHandlers.Remove(action);
            }
        }
        return removed;
    }

    public void addMessageToSend(DataMessage message)
    {
        if (connection != null && connection.Connected)
        {
            lock (messagesToSend)
            {
                messagesToSend.Push(message);
            }
        }
    }

    private void handleMessage(object param)
    {
        KeyValuePair<DataMessage, DataHandledDelegate> action = (KeyValuePair<DataMessage, DataHandledDelegate>)param;
        try
        {
            if (action.Value(action.Key))
            {
                unregisterDataListener(action.Key.Service, action.Key.Action);
            }
        }
        catch (Exception ex)
        {
            unregisterDataListener(action.Key.Service, action.Key.Action);
            Debug.LogError("Connection, handler message error : " + ex.ToString());
        }
    }

    private void notifyAction(DataMessage message)
    {
        if (message != null)
        {
            bool handled = false;
            lock (dataListeners)
            {
                Dictionary<int, DataHandledDelegate> serviceHandlers;
                if (dataListeners.TryGetValue(message.Service, out serviceHandlers))
                {
                    DataHandledDelegate actionHandler;
                    if (serviceHandlers.TryGetValue(message.Action, out actionHandler))
                    {
                        Handler.getInstance().postAction(handleMessage, new KeyValuePair<DataMessage, DataHandledDelegate>(message, actionHandler));
                        handled = true;
                    }
                }
            }
            if (!handled)
            {
                Debug.LogWarning("Connection: unsupported message. Service : " + message.Service.ToString() + ", Action : " + message.Action.ToString());
            }
        }       
    }

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
            Debug.LogError("Fail to receive message , error : " + ex);
            fireErrorHandlers(ConnectionErrors.RECEIVE_MESSAGE, "Receive message error", ex);
            result = null;
        }
        return result;
    }

    private void sendMessages()
    {
        if (!connection.Connected) return;

        DataMessage message = null;
        try
        {
            while (true)
            {
                //Pop first message
                lock (messagesToSend)
                {
                    if (messagesToSend.Count > 0)
                    {
                        message = messagesToSend.Pop();
                    }
                    else
                    {
                        break;
                    }
                }

                int length = 4 + 4;
                byte[] session = null; ;
                length += 4;
                if (!string.IsNullOrEmpty(message.Session))
                {
                    session = Encoding.UTF8.GetBytes(message.Session);
                    length += session.Length;
                }

                length += 4;
                int dataLength = message.DataLength - message.DataOffset;
                if (message.Data != null && dataLength > 0)
                {
                    length += dataLength;
                }

                writer.Write(length);
                writer.Write(message.Service);
                writer.Write(message.Action);
                if (session != null)
                {
                    writer.Write(session.Length);
                    writer.Write(session, 0, session.Length);
                }
                else
                {
                    writer.Write((int)0);
                }

                if (message.Data != null)
                {
                    writer.Write(message.DataLength);
                    writer.Write(message.Data, message.DataOffset, message.DataLength);
                }
                else
                {
                    writer.Write((int)0);
                }
                connection.GetStream().Flush();
            }
        }
        catch (Exception ex)
        {
            if (message != null) 
                Debug.LogError("Fail to send message (is null), error : " + ex);
            else 
                Debug.LogError("Fail to send message ("+message.ToString()+"), error : " + ex);
            fireErrorHandlers(ConnectionErrors.SEND_MESSAGE, "Send message error", ex);
        }
    }

    public void registerErrorListener(int senderId, ConnectionErrorDelegate errorHandler)
    {
        errorHandlers[senderId] = errorHandler;
    }

    public bool unregisterErrorListener(int senderId)
    {
        return errorHandlers.Remove(senderId);
    }

    private void fireErrorHandlers(ConnectionErrors error, string message, object param)
    {
        if (errorHandlers.Count > 0)
        {
            foreach (KeyValuePair<int, ConnectionErrorDelegate> i in errorHandlers)
            {
                ErrorHandler handler = new ErrorHandler(i.Key, error, message, param, i.Value);
                Handler.getInstance().postAction(handleError, handler);
            }
        }
    }


    private void handleError(object param)
    {
        ErrorHandler handler = (ErrorHandler)param;
        try
        {
            handler.handle();
        }
        catch (Exception ex)
        {
            Debug.LogError("Connection, handler Error problem : " + ex.ToString());
        }
    }
        
    private void clear()
    {
        lock (messagesToSend)
        {
            messagesToSend.Clear();
        }

        lock (dataListeners)
        {
            dataListeners.Clear();
        }

        if (connection != null)
        {
            if (connection.Connected)
            {
                writer.Close();
                reader.Close();
            }
            connection.Close();
            connection = null;
        }
    }

    public void onDestroy()
    {
        clear();
        lock (errorHandlers)
        {
            errorHandlers.Clear();
        }
    }

    private class ErrorHandler
    {
        private int id;
        private ConnectionErrors error;
        private string message;
        private object param;
        private ConnectionErrorDelegate handler;

        public ErrorHandler(int id, ConnectionErrors code, string message, object param, ConnectionErrorDelegate handler)
        {
            this.id = id;
            this.error = code;
            this.message = message;
            this.param = param;
            this.handler = handler;
        }

        public void handle()
        {
            handler(id, (int)error, message, param);
        }         
    }
}
