using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using System.Text;
using MiscUtil.IO;
using MiscUtil.Conversion;

public class UserInfo
{
    public string userLogin;
    public string userPassword;
    public string email;
    public string session;
    public int userId;

    public string firstName;
    public string secondtName;
    public string userIcon;
    public Sprite userIconImage;
}

public enum RegistrationResults
{
    Success = 0,
    InvalidData = 1,
    ConnectionError = 2,
    UserExist = 3,
    IntentalServerError = 4
}

public enum RequestGameResults
{
    Success = 0,
    InvalidData,
    IntentalServerError,
    GameIsUnavalble,
    ConnectionError
}

public enum LoginResults
{
    Success = 0,
    InvalidData = 1,
    InvalidLoginOrPassword = 2,
    ConnectionError = 3,
    IntentalServerError = 4
}

public enum SystemCallbacks
{
    Registration = 0,
    Authorization = 1,
    RequestGame = 2
}

public enum SystemControllerStates
{
    Default,
    RegisterUser,
    LoginUser,
    RequestGame,
    WaitGameStart,
    InGame
}

public class SystemController
{

    private static SystemController instance;

    public static SystemController get()
    {
        if (instance == null)
        {
            instance = new SystemController();
        }
        return instance;
    }

    private BaseGameController currentGame;
    private Connection connection;
    private SystemControllerStates state;
    private GameLibrary gameLibrary;

    private List<BaseUiController> windows = new List<BaseUiController>();

    private UserInfo userInfo = new UserInfo();

    private Dictionary<SystemCallbacks, RunnableDelegate> callbacks = new Dictionary<SystemCallbacks, RunnableDelegate>();

    private SystemController()
    {
        connection = Connection.getInstance();
        connection.registerErrorListener(Ids.ErrorHandlers.SystemController, onConnectionError);
        gameLibrary = new GameLibrary();
    }

    public BaseUiController loadWindow(String name, Transform parent)
    {
        GameObject prefab = (GameObject)Resources.Load(name);
        GameObject windowGameObject = (GameObject)GameObject.Instantiate(prefab);
        RectTransform windowTransform = (RectTransform)windowGameObject.transform;
        BaseUiController windowController = windowGameObject.GetComponent<BaseUiController>();
        windowTransform.SetParent(parent, false);

        return windowController;
    }

    public static void stopApplication()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
             Application.Quit();
        #endif
    }

    public BaseGameController getCurrentGame()
    {
        return currentGame;
    }

    public void startWindow(BaseUiController window, bool removeTop)
    {
        if (windows.Count > 0)
        {
            BaseUiController lastWindow = windows[windows.Count - 1];
            if (removeTop)
            {
                lastWindow.pauseWindow();
                lastWindow.stopWindow();
                windows.RemoveAt(windows.Count - 1);
            }
            else
            {
                lastWindow.pauseWindow();
            }
            
        }
        windows.Add(window);
        window.startWindow();
        window.resumeWindow();
    }

    public GameLibrary getLibrary()
    {
        return gameLibrary;
    }

    public BaseUiController getCurrentWindow()
    {
        BaseUiController lastWindow;
        if (windows.Count > 0)
        {
            lastWindow = windows[windows.Count - 1];
        }
        else
        {
            lastWindow = null;
        }
        return lastWindow;
    }

    public void closeWindow(BaseUiController window)
    {
        if (windows.Count > 0)
        {
            BaseUiController lastWindow = windows[windows.Count - 1];
            if (window == lastWindow)
            {
                windows.RemoveAt(windows.Count - 1);
                if (windows.Count > 0) 
                {
                    lastWindow = windows[windows.Count - 1];
                    window.pauseWindow();
                    window.stopWindow();
                    lastWindow.resumeWindow();
                }
                else
                {
                    window.pauseWindow();
                    window.stopWindow();
                }
            }
            else
            {
                int index = windows.LastIndexOf(window);
                if (index > 0)
                {
                    windows.RemoveAt(index);
                    window.pauseWindow();
                    window.stopWindow();
                }
                else
                {
                    window.pauseWindow();
                    window.stopWindow();
                }
            }
        }
        else
        {
            window.pauseWindow();
            window.stopWindow();
        }
    }

    public void setAuthorizationInfo(string login, string password) {
        userInfo.userLogin = login;
        userInfo.userPassword = password;
    }

    public void setRegistrationInfo(string login, string password, string email)
    {
        userInfo.userLogin = login;
        userInfo.userPassword = password;
        userInfo.email = email;
    }

    public UserInfo getUserInfo()
    {
        return userInfo;
    }

    public void registerPlayer(RunnableDelegate callback)
    {
        if (state != SystemControllerStates.Default)
            throw new InvalidOperationException("Invalid SystemController state, waiting Default, but have : " + state);

        if (connection.isConnected())
        {
            DataMessage message = new DataMessage(Ids.Services.CLIENTS, Ids.Actions.Clients.REGISTER_NEW, null);
            JSONObject json = JSONBuilder.create()
               .with("type", Ids.Actions.Authorizations.BY_LOGIN_AND_PASSWORD)
               .with("id", userInfo.userLogin)
               .with("password", userInfo.userPassword).getJson();

            message.createWriter().writeJson(json).closeWriter();

            state = SystemControllerStates.RegisterUser;
            callbacks[SystemCallbacks.Registration] = callback;
            connection.registerDataListener(message, onRegistrationComplete);
            connection.addMessageToSend(message);
        }
        else
        {
            Handler.getInstance().postAction(callback, RegistrationResults.ConnectionError);
        }
    }

    private bool onRegistrationComplete(DataMessage message)
    {
        RunnableDelegate runnable;

        if (callbacks.TryGetValue(SystemCallbacks.Registration,out runnable) &&
            message!=null && message.Data != null)
        {
            callbacks.Remove(SystemCallbacks.Registration);

            message.createReader();
            int result = message.readInt32();
            message.closeReader();
            switch (result)
            {
                case Ids.UserManagerResults.INVALID_DATA:
                    Handler.getInstance().postAction(runnable, RegistrationResults.InvalidData);
                    break;
                case Ids.UserManagerResults.REGISTER_SUCH_USER_EXIST:
                    Handler.getInstance().postAction(runnable, RegistrationResults.UserExist);
                    break;
                case Ids.UserManagerResults.SUCCESS:
                    Handler.getInstance().postAction(runnable, RegistrationResults.Success);
                    break;
                case Ids.UserManagerResults.REGISTER_UNKNOWN_TYPE:
                case Ids.UserManagerResults.INTERNAL_ERROR:
                    Handler.getInstance().postAction(runnable, RegistrationResults.IntentalServerError);
                    break;
                default:
                    Handler.getInstance().postAction(runnable, RegistrationResults.IntentalServerError);
                    break;
            }
            state = SystemControllerStates.Default;
            
        }
        return true;
    }

    public void loginUser(RunnableDelegate callback)
    {
        if (state != SystemControllerStates.Default)
            throw new InvalidOperationException("Invalid SystemController state, waiting Default, but have : " + state);

        if (connection.isConnected())
        {   
            DataMessage message = new DataMessage(Ids.Services.CLIENTS, Ids.Actions.Clients.LOGIN, null);
            JSONObject json = JSONBuilder.create()
                .with("type", Ids.Actions.Authorizations.BY_LOGIN_AND_PASSWORD)
                .with("id", userInfo.userLogin)
                .with("password", userInfo.userPassword)
                .getJson();
            message.createWriter().writeJson(json).closeWriter();

            state = SystemControllerStates.LoginUser;
            callbacks[SystemCallbacks.Authorization] = callback;
            connection.registerDataListener(message, onLoginComplete);
            connection.addMessageToSend(message);
        }
        else
        {
            Handler.getInstance().postAction(callback, LoginResults.ConnectionError);
        }
    }

    private bool onLoginComplete(DataMessage message)
    {
        RunnableDelegate runnable;
        if (callbacks.TryGetValue(SystemCallbacks.Authorization, out runnable) &&
            message != null && message.Data != null)
        {
            message.createReader();
            int result = message.readInt32();

            callbacks.Remove(SystemCallbacks.Authorization);

            switch (result)
            {
                case Ids.UserManagerResults.INVALID_DATA:
                    message.closeReader();
                    Handler.getInstance().postAction(runnable, LoginResults.InvalidData);
                    break;
                case Ids.UserManagerResults.LOGIN_OR_PASSWORD_INVALID:
                    message.closeReader();
                    Handler.getInstance().postAction(runnable, LoginResults.InvalidLoginOrPassword);
                    break;
                case Ids.UserManagerResults.SUCCESS:
                    {
                        int clientId = message.readInt32();
                        string session = message.Session;
                        message.closeReader();
                        userInfo.session = session;
                        userInfo.userId = clientId;
                        Handler.getInstance().postAction(runnable, LoginResults.Success);
                    }
                    break;
                case Ids.UserManagerResults.REGISTER_UNKNOWN_TYPE:
                case Ids.UserManagerResults.INTERNAL_ERROR:
                    message.closeReader();
                    Handler.getInstance().postAction(runnable, LoginResults.IntentalServerError);
                    break;
                default:
                    message.closeReader();
                    Handler.getInstance().postAction(runnable, LoginResults.IntentalServerError);
                    break;
            }
            state = SystemControllerStates.Default;
            
        }
        else
        {
            UnityEngine.Debug.LogError(GetType().Name + ", onLoginComplete : can`t get callback or empty message = " + message);
        }
        return true;
    }

    public void requestGame(long gameId, BaseGameController controller, RunnableDelegate callback)
    {
        if (state != SystemControllerStates.Default)
            throw new InvalidOperationException("Invalid SystemController state, waiting Default, but have : " + state);

        if (connection.isConnected()) {
            DataMessage message = new DataMessage(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.START_GAME_REQUEST, userInfo.session);
            message.createWriter();
            message.writerLong(gameId).closeWriter();

            state = SystemControllerStates.RequestGame;
            callbacks[SystemCallbacks.RequestGame] = callback;

            currentGame = controller;

            connection.registerDataListener(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.START_GAME_REQUEST, onGameRequestComplete);
            connection.registerDataListener(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.GAME_STARTED, onGameStarted);
            connection.registerDataListener(Ids.Services.GAME_RESLOVER, Ids.Actions.GameResolver.GAME_FINISHED, onGameFinished);
            connection.addMessageToSend(message);
        } else {
            Handler.getInstance().postAction(callback, RequestGameResults.ConnectionError);
        }
    }
    
    private bool onGameRequestComplete(DataMessage message)
    {
        RunnableDelegate runnable;
        if (callbacks.TryGetValue(SystemCallbacks.RequestGame, out runnable) &&
            message != null && message.Data != null)
        {
            message.createReader();
            int result = message.readInt32();
            message.closeReader();
            callbacks.Remove(SystemCallbacks.RequestGame);

            switch (result)
            {
                case Ids.SystemResults.SUCCESS:
                    currentGame.prepareGame();
                    Handler.getInstance().postAction(runnable, RequestGameResults.Success);
                    state = SystemControllerStates.WaitGameStart;

                    break;
                case Ids.SystemResults.GAME_IS_UNAVALABLE_NOW:
                    Handler.getInstance().postAction(runnable, RequestGameResults.GameIsUnavalble);
                    state = SystemControllerStates.Default;
                    break;
                case Ids.SystemResults.INVALID_DATA:
                case Ids.SystemResults.INVALID_SESSION:
                    Handler.getInstance().postAction(runnable, RequestGameResults.InvalidData);
                    state = SystemControllerStates.Default;
                    break;
                case Ids.SystemResults.INTERNAL_ERROR:
                    Handler.getInstance().postAction(runnable, RequestGameResults.IntentalServerError);
                    state = SystemControllerStates.Default;
                    break;
                default:
                    Handler.getInstance().postAction(runnable, RequestGameResults.IntentalServerError);
                    state = SystemControllerStates.Default;
                    break;
            }
        }
        else
        {
            UnityEngine.Debug.LogError(GetType().Name + ", onGameRequestComplete : can`t get callback or empty message = " + message);
        }
        return true;
    }

    private bool onGameStarted(DataMessage message)
    {
        if (state != SystemControllerStates.WaitGameStart)
            throw new InvalidOperationException("Invalid SystemController state, waiting WaitGameStart, but have : " + state);

        userInfo.session = message.Session;

        if (message !=null && message.Data != null)
        {
            message.createReader();
            int gameId = message.readInt32();
            message.closeReader();

            currentGame.GameId = gameId;
            state = SystemControllerStates.InGame;
            currentGame.startGame();
        }
        return true;
    }

    private bool onGameFinished(DataMessage message)
    {
        if (state != SystemControllerStates.InGame)
            throw new InvalidOperationException("Invalid SystemController state, waiting InGame), but have : " + state);

        if (message != null && message.Data != null)
        {
            message.createReader();
            int gameId = message.readInt32();
            int result = message.readInt32();
            Dictionary<string, object> data = message.readJson();
            message.closeReader();

            if (gameId == currentGame.GameId)
            {
                state = SystemControllerStates.Default;
                currentGame.finishGame(result, data);
            }
            else
            {

            }
        }
        return true;
    }


    private void onConnectionError(int senderId, int code, string message, object param)
    {
        //UnityEngine.Debug.LogError(GetType().Name + ", onConnectionError, code : " + code.ToString() + ", message : " + message + ", param : " + param == null ? "<null>" : param.ToString());
        switch (state)
        {
            case SystemControllerStates.RegisterUser:
                {
                    RunnableDelegate runnable;
                    if (callbacks.TryGetValue(SystemCallbacks.Registration, out runnable))
                    {
                        
                        callbacks.Remove(SystemCallbacks.Registration);
                        state = SystemControllerStates.Default;
                        Handler.getInstance().postAction(runnable, RegistrationResults.ConnectionError);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("State == register, but no handler.");
                    }
                }
                break;
            case SystemControllerStates.LoginUser:
                {
                    RunnableDelegate runnable;
                    if (callbacks.TryGetValue(SystemCallbacks.Authorization, out runnable))
                    {
                        callbacks.Remove(SystemCallbacks.Authorization);
                        state = SystemControllerStates.Default;
                        Handler.getInstance().postAction(runnable, LoginResults.ConnectionError);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("State == login, but no handler.");
                    }
                }
                break;
            case SystemControllerStates.RequestGame:
                {
                    RunnableDelegate runnable;
                    if (callbacks.TryGetValue(SystemCallbacks.RequestGame, out runnable))
                    {
                        callbacks.Remove(SystemCallbacks.RequestGame);
                        state = SystemControllerStates.Default;
                        Handler.getInstance().postAction(runnable, RequestGameResults.ConnectionError);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("State == request game, but no handler.");
                    }
                }
                break;
            case SystemControllerStates.InGame:
                {
                    if (currentGame != null)
                    {
                        currentGame.disconnected();
                        state = SystemControllerStates.Default;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("State == in game, but current game is null.");
                    }
                }
                break;
            case SystemControllerStates.Default:
                //UnityEngine.Debug.LogError("State == Default, do nothinc.");
                break;
        }
    }
}
