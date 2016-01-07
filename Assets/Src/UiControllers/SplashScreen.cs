using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System;
using System.Collections.Generic;
using JsonFx.Json;

public class SplashScreen : MonoBehaviour
{
    private enum States {
        NotStarted=0,
        LoadConfiguration,
        StartConfigre,
        ConfigurationComplete,
        StartSystemController,
        SystemControllerStarted,
        StartConnection,
        ConnectionStarted,
        OpenMenu,
        Complete,
        Error_LoadConfig,
        Error_NetworkError,
    }

    private Image splashImage;
    private Image splashBackground;

    private Text splashText;
    private ProgressBar loadingProgressBar;

    private States state;
    private Dictionary<string, object> config;

    private MessageBox messageBox;

    public BaseUiController menuController;


    public void Awake()
    {
        state = States.NotStarted;
        RectTransform contentPanel = (RectTransform)transform.Find("Content");
        
        splashBackground = contentPanel.GetComponent<Image>();
        splashImage = contentPanel.Find("Image").GetComponent<Image>();
        splashText =  contentPanel.Find("Message").GetComponent<Text>();

        loadingProgressBar = new ProgressBar(contentPanel.Find("LoadingBar"),0, 100);

        messageBox = new MessageBox(transform.Find("MessageBox"));
    }

    private void progress(float percent, string message)
    {
        loadingProgressBar.setProgress(percent);
        splashText.text = message;
    }

    public void Update()
    {
        switch (state)
        {
            case States.NotStarted:
                state = States.LoadConfiguration;
                progress(5f, "Load configuration");
                startLoadConfiguration();
                break;
            case States.StartConfigre:
                state = States.StartSystemController;
                progress(10f, "Initialization of components: system controller");
                initializeSystemController();
                break;
            case States.SystemControllerStarted:
                state = States.StartConnection;
                progress(20f, "Initialization of components: connection");
                initializeConnection();
                break;
            case States.ConnectionStarted:
                state = States.OpenMenu;
                progress(50f, "Initialization of components: complete");
                break;
            case States.OpenMenu:
                state = States.Complete;
                progress(100f, "Open menu");
                openMenu();
                break;
            case States.Error_LoadConfig:
                messageBox.showMessage("Configuration error", "Load configuration error, try to restart application.", exitOnError);
                break;
            case States.Error_NetworkError:
                messageBox.showMessage("Network error", "Problem with connecting to server. Check you internet settings and try again,", exitOnError);
                break;
        }
    }

    private void exitOnError()
    {
        SystemController.stopApplication();
    }

    private void openMenu()
    {
        gameObject.SetActive(false);
        SystemController.get().startWindow(menuController, true);
    }

    private void startLoadConfiguration()
    {
        TextAsset configString = (TextAsset)Resources.Load("Config/config");
        if (configString != null && !String.IsNullOrEmpty(configString.text))
        {
            JsonReader jr = new JsonReader();
            config = jr.Read<Dictionary<String, System.Object>>(configString.text);
            state = States.StartConfigre;
        }
        else
        {
            state = States.Error_LoadConfig;
            Debug.LogError("Fail to load Config");
        }
    }

    private void initializeConnection()
    {
        object serverConfiguration = null;
        if (config.TryGetValue("server", out serverConfiguration))
        {
            Dictionary<String, System.Object> serverConfig = (Dictionary<String, System.Object>)serverConfiguration;
            Connection connection = Connection.getInstance();
            if (serverConfig == null || connection == null)
            {
                state = States.Error_LoadConfig;
            }
            else
            {
                string address = JSONObject.optString(serverConfig, "address", Connection.DEFAULT_ADDRESS);
                int port = JSONObject.optInt(serverConfig, "port", Connection.DEFAULT_PORT);
                connection.setServerAddress(address, port);
                Debug.Log("Connect to = " + address + ":" + port.ToString());
                if (connection.connect())
                {
                    state = States.ConnectionStarted;
                }
                else
                {
                    state = States.Error_NetworkError;
                }
            }
        }
    }

    private void initializeSystemController()
    {
        SystemController.get();
        state = States.SystemControllerStarted;
    }
}