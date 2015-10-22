using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using System.Text;
using MiscUtil.IO;
using MiscUtil.Conversion;

class AuthorizationController
{
    private Connection connection;
    private UnityAction networkErrorHandler;

    public delegate void RegistrationResultDelegate(bool success, bool invalidData, bool userExist);
    public delegate void LoginResultDelegate(bool success, int result, int clientId, string session);

    public AuthorizationController(Connection connection, UnityAction onNetworkError)
    {
        this.connection = connection;
        this. networkErrorHandler = onNetworkError;
    }

    public void registerUser(string login, string password, string email, RegistrationResultDelegate onComplete)
    {
        if (!connection.isConnected())
        {
            networkErrorHandler();
            return;
        }

        DataMessage message = new DataMessage();
        message.Service = Ids.Services.CLIENTS;
        message.Action = Ids.Actions.Clients.REGISTER_NEW;
        message.Session = null;
        string json = string.Format(Ids.Authorizations.REGISTER_JSON, Ids.Authorizations.BY_LOGIN_AND_PASSWORD, login, password);
        byte[] data = Encoding.UTF8.GetBytes(json);

        MemoryStream stream = new MemoryStream(4 + data.Length);
        EndianBinaryWriter writer = new EndianBinaryWriter(BigEndianBitConverter.Big, stream);
        writer.Write(data.Length);
        writer.Write(data);
        writer.Flush();
        message.Data = stream.GetBuffer();
        message.DataOffset = 0;
        message.DataLength = (int)stream.Position;
        writer.Close();
        stream.Close();
        connection.addMessageToSend(message);
        Connection.DataHandledDelegate sendAuthDelefate = null;
        sendAuthDelefate = delegate (DataMessage msg)
        {
            if (msg.Data != null)
            {
                MemoryStream answareStream = new MemoryStream(msg.Data);
                EndianBinaryReader answareReader = new EndianBinaryReader(BigEndianBitConverter.Big, answareStream);
                int result = answareReader.ReadInt32();
                answareReader.Close();
                answareStream.Close();

                switch (result)
                {
                    case Ids.UserManagerResults.INVALID_DATA:
                        onComplete(false, true, false);
                        break;
                    case Ids.UserManagerResults.REGISTER_SUCH_USER_EXIST:
                        onComplete(false, false, true);
                        break;
                    case Ids.UserManagerResults.SUCCESS:
                        onComplete(true, false, false);
                        break;
                    case Ids.UserManagerResults.REGISTER_UNKNOWN_TYPE:
                    case Ids.UserManagerResults.INTERNAL_ERROR:
                        onComplete(false, false, false);
                        break;
                    default:
                        onComplete(false, false, false);
                        break;
                }
            }
            return true;
        };
        connection.registerDataListener(Ids.Services.CLIENTS, Ids.Actions.Clients.REGISTER_NEW, sendAuthDelefate);
    }

    public void loginUser(string login, string password, LoginResultDelegate onComplete)
    {
        if (!connection.isConnected())
        {
            networkErrorHandler();
            return;
        }

        DataMessage message = new DataMessage();
        message.Service = Ids.Services.CLIENTS;
        message.Action = Ids.Actions.Clients.LOGIN;
        message.Session = null;
        string json = string.Format(Ids.Authorizations.LOGIN_JSON, Ids.Authorizations.BY_LOGIN_AND_PASSWORD, login, password);
        byte[] data = Encoding.UTF8.GetBytes(json);

        MemoryStream stream = new MemoryStream(4 + data.Length);
        EndianBinaryWriter writer = new EndianBinaryWriter(BigEndianBitConverter.Big, stream);
        writer.Write(data.Length);
        writer.Write(data);
        writer.Flush();
        message.Data = stream.GetBuffer();
        message.DataOffset = 0;
        message.DataLength = (int)stream.Position;
        writer.Close();
        stream.Close();
        connection.addMessageToSend(message);
        Connection.DataHandledDelegate loginDelegate = null;
        loginDelegate = delegate(DataMessage msg)
        {
            if (msg.Data != null)
            {
                MemoryStream answareStream = new MemoryStream(msg.Data);
                EndianBinaryReader answareReader = new EndianBinaryReader(BigEndianBitConverter.Big, answareStream);
                int result = answareReader.ReadInt32();
                answareReader.Close();
                answareStream.Close();

                switch (result)
                {
                    case Ids.UserManagerResults.INVALID_DATA:
                    case Ids.UserManagerResults.LOGIN_OR_PASSWORD_INVALID:
                        onComplete(false, result, 0, null);
                        break;
                    case Ids.UserManagerResults.SUCCESS:
                        {
                            int clientId = answareReader.ReadInt32();
                            string session = msg.Session;
                            onComplete(true, 0, clientId, session);
                        }
                        break;
                    case Ids.UserManagerResults.REGISTER_UNKNOWN_TYPE:
                    case Ids.UserManagerResults.INTERNAL_ERROR:
                        onComplete(false, result, 0, null);
                        break;
                    default:
                        onComplete(false, Ids.UserManagerResults.INTERNAL_ERROR, 0, null);
                        break;
                }
            }
            return true;
        };
        connection.registerDataListener(Ids.Services.CLIENTS, Ids.Actions.Clients.REGISTER_NEW, loginDelegate);
    }

    public delegate void UserInfoResultDelegate(int result, Dictionary<string, object> data);

    public void requestUserInfp(int clientId, UserInfoResultDelegate onComplete)
    {
        if (!connection.isConnected())
        {
            networkErrorHandler();
            return;
        }

        DataMessage message = new DataMessage();
        message.Service = Ids.Services.CLIENTS;
        message.Action = Ids.Actions.Clients.LOGIN;
        message.Session = null;
        string json = string.Format(Ids.Authorizations.LOGIN_JSON, Ids.Authorizations.BY_LOGIN_AND_PASSWORD, login, password);
        byte[] data = Encoding.UTF8.GetBytes(json);

        MemoryStream stream = new MemoryStream(4 + data.Length);
        EndianBinaryWriter writer = new EndianBinaryWriter(BigEndianBitConverter.Big, stream);
        writer.Write(data.Length);
        writer.Write(data);
        writer.Flush();
        message.Data = stream.GetBuffer();
        message.DataOffset = 0;
        message.DataLength = (int)stream.Position;
        writer.Close();
        stream.Close();
        connection.addMessageToSend(message);
        Connection.DataHandledDelegate loginDelegate = null;
        loginDelegate = delegate(DataMessage msg)
        {
            if (msg.Data != null)
            {
                MemoryStream answareStream = new MemoryStream(msg.Data);
                EndianBinaryReader answareReader = new EndianBinaryReader(BigEndianBitConverter.Big, answareStream);
                int result = answareReader.ReadInt32();
                answareReader.Close();
                answareStream.Close();

                switch (result)
                {
                    case Ids.UserManagerResults.INVALID_DATA:
                    case Ids.UserManagerResults.LOGIN_OR_PASSWORD_INVALID:
                        onComplete(false, result, 0, null);
                        break;
                    case Ids.UserManagerResults.SUCCESS:
                        {
                            int clientId = answareReader.ReadInt32();
                            string session = msg.Session;
                            onComplete(true, 0, clientId, session);
                        }
                        break;
                    case Ids.UserManagerResults.REGISTER_UNKNOWN_TYPE:
                    case Ids.UserManagerResults.INTERNAL_ERROR:
                        onComplete(false, result, 0, null);
                        break;
                    default:
                        onComplete(false, Ids.UserManagerResults.INTERNAL_ERROR, 0, null);
                        break;
                }
            }
            return true;
        };
        connection.registerDataListener(Ids.Services.CLIENTS, Ids.Actions.Clients.REGISTER_NEW, loginDelegate);
    }
}
