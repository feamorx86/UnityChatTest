using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System;

public class MenuScreen : BaseUiController {

    private Transform panelRigister, panelLogin, panelRecover;
    private Button buttonTabRegister, buttonTabLogin, buttonTabRecover;

    private InputField inputRegistrationLogin, inputRegistrationPassword, inputRegistrationEmain;
    private Button buttonRegistrationRegister;

    private InputField inputEnterLogin, inputEnterPassword;
    private Button buttonEnterLogin;

    public UserInfoScreen userInfoScreen;

    public override void Awake()
    {
        base.Awake();
        gameObject.SetActive(false);
        setupTabs();
        setupRgistration();
        setupLogin();
        setupRecover();
    }
	

    public override void startWindow()
    {
        base.startWindow();
        gameObject.SetActive(true);
        selectTab(Tabs.Login);

        SystemController controller = SystemController.get();
        /*if (controller.getUserInfo().userLogin == null)
            inputEnterLogin.text = "";
        else
            inputEnterLogin.text = controller.getUserInfo().userLogin;
        if (controller.getUserInfo().userPassword == null)
            inputEnterPassword.text = "";
        else
            inputEnterPassword.text = controller.getUserInfo().userPassword;
         * */
        inputEnterLogin.Select();
    }

    public override void stopWindow()
    {
        base.stopWindow();
        gameObject.SetActive(false);
    }

    private void userLogin(string login, string password)
    {
        if (string.IsNullOrEmpty(login))
        {
            messageBox.showMessage("Error", "Login is empty. Please enter login and try again.");
        }
        else if (string.IsNullOrEmpty(password))
        {
            messageBox.showMessage("Error", "Password is empty. Please enter password and try again.");
        }
        else
        {
            startWait();
            if (Connection.getInstance().isConnected())
            {
                SystemController controller = SystemController.get();
                controller.setAuthorizationInfo(login, password);
                controller.loginUser(onLoginComplete);
            }
            else
            {
                if (!Connection.getInstance().connect())
                {
                    messageBox.showMessage("Network error", "Problem with internet. Check network connection and try again.", stopWait);
                }
                else
                {
                    SystemController controller = SystemController.get();
                    controller.setAuthorizationInfo(login, password);
                    controller.loginUser(onLoginComplete);
                }
            }
        }
    }

    private void onLoginComplete(object param)
    {        
        LoginResults result = (LoginResults)param;
        switch (result)
        {
            case LoginResults.Success:
                SystemController.get().startWindow(userInfoScreen, true);
                break;
            case LoginResults.InvalidData:
                messageBox.showMessage("Login error", "Invalid Login or Password. Check authorization information and try again.", stopWait);
                break;
            case LoginResults.InvalidLoginOrPassword:
                messageBox.showMessage("Login error", "Invalid Login or Password. Check authorization information and try again.", stopWait);
                break;
            case LoginResults.IntentalServerError:
                messageBox.showMessage("Server error", "Server side problems. Wait few seconds and try again.", stopWait);
                break;
            case LoginResults.ConnectionError:
                messageBox.showMessage("Network error", "Problem with internet. Check network connection and try again.", stopWait);
                break;
        }      
    }

    private void userRegister(string login, string password, string email)
    {
        if (string.IsNullOrEmpty(login))
        {
            messageBox.showMessage("Error", "Login is empty. Please enter login and try again.");
        } else if (string.IsNullOrEmpty(password))
        {
            messageBox.showMessage("Error", "Password is empty. Please enter password and try again.");
        } else if (string.IsNullOrEmpty(email))
        {
            messageBox.showMessage("Error", "Email is empty. Please enter email and try again.");
        }
        else
        {
            startWait();
            if (Connection.getInstance().isConnected())
            {
                SystemController controller = SystemController.get();
                controller.setRegistrationInfo(login, password, email);
                controller.registerPlayer(onRegisterComplete);

            }
            else
            {
                if (!Connection.getInstance().connect())
                {
                    messageBox.showMessage("Network error", "Problem with internet. Check network connection and try again.", stopWait);
                }
                else
                {
                    SystemController controller = SystemController.get();
                    controller.setRegistrationInfo(login, password, email);
                    controller.registerPlayer(onRegisterComplete);

                }
            }
        }
    }

    private void onRegisterComplete(object param)
    {
        RegistrationResults result = (RegistrationResults)param;
        switch (result)
        {
            case RegistrationResults.Success:
                stopWait();
                SystemController systemController = SystemController.get();
                inputEnterLogin.text = systemController.getUserInfo().userLogin;
                inputEnterPassword.text = systemController.getUserInfo().userPassword;
                selectTab(Tabs.Login);
                break;
            case RegistrationResults.InvalidData:
                messageBox.showMessage("Error", "Invalid registration information. Check your login, password, email and try again.", stopWait);
                break;
            case RegistrationResults.IntentalServerError:
                messageBox.showMessage("Server error", "Server side problems. Wait few seconds and try again.", stopWait);
                break;
            case RegistrationResults.UserExist:
                messageBox.showMessage("Error", "Sorry but User with such Login already exist. Please enter another Login and try again.", stopWait);
                break;
            case RegistrationResults.ConnectionError:
                messageBox.showMessage("Network error", "Problem with internet. Check network connection and try again.", stopWait);
                break;
        }
    }

    private void setupLogin()
    {
        buttonEnterLogin = panelLogin.Find("ButtonLogin").GetComponent<Button>();
        inputEnterLogin = panelLogin.Find("InputFieldLogin").GetComponent<InputField>();
        inputEnterPassword = panelLogin.Find("InputFieldPassword").GetComponent<InputField>();
        buttonEnterLogin.onClick.AddListener(delegate()
        {
            userLogin(inputEnterLogin.text, inputEnterPassword.text);
        });
    }

    private void setupRgistration()
    {
        buttonRegistrationRegister = panelRigister.Find("ButtonRegister").GetComponent<Button>();
        inputRegistrationLogin = panelRigister.Find("InputFieldLogin").GetComponent<InputField>();
        inputRegistrationPassword = panelRigister.Find("InputFieldPassword").GetComponent<InputField>();
        inputRegistrationEmain = panelRigister.Find("InputFieldEmail").GetComponent<InputField>();

        buttonRegistrationRegister.onClick.AddListener(delegate()
        {
            userRegister(inputRegistrationLogin.text, inputRegistrationPassword.text, inputRegistrationEmain.text);
        });

    }

    private void setupRecover()
    {

    }

    private void setupTabs()
    {
        panelRigister = transform.Find("PanelRegister");
        panelLogin = transform.Find("PanelLogin");
        panelRecover = transform.Find("PanelRecover");
        buttonTabRegister = transform.Find("ButtonRegister").GetComponent<Button>();
        buttonTabLogin = transform.Find("ButtonLogin").GetComponent<Button>();
        buttonTabRecover = transform.Find("ButtonRecover").GetComponent<Button>();

        selectTab(Tabs.Registration);

        buttonTabRegister.onClick.AddListener(delegate()
        {
            selectTab(Tabs.Registration);
        });
        buttonTabLogin.onClick.AddListener(delegate()
        {
            selectTab(Tabs.Login);
        });
        buttonTabRecover.onClick.AddListener(delegate()
        {
            selectTab(Tabs.Recover);
        });
    }

    private enum Tabs{
        Registration,
        Login,
        Recover
    }

    private void selectTab(Tabs tab)
    {
        switch (tab)
        {
            case Tabs.Login:
                panelRigister.gameObject.SetActive(false);
                panelLogin.gameObject.SetActive(true);
                panelRecover.gameObject.SetActive(false);
                buttonTabRegister.enabled = true;
                buttonTabLogin.enabled = false;
                buttonTabRecover.enabled = true;
                break;
            case Tabs.Registration:
                panelRigister.gameObject.SetActive(true);
                panelLogin.gameObject.SetActive(false);
                panelRecover.gameObject.SetActive(false);
                buttonTabRegister.enabled = false;
                buttonTabLogin.enabled = true;
                buttonTabRecover.enabled = true;
                break;
            case Tabs.Recover:
                panelRigister.gameObject.SetActive(false);
                panelLogin.gameObject.SetActive(false);
                panelRecover.gameObject.SetActive(true);
                buttonTabRegister.enabled = true;
                buttonTabLogin.enabled = true;
                buttonTabRecover.enabled = false;
                break;
        }
    }
}
