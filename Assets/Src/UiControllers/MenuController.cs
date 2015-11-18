using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System;

public class MenuController : MonoBehaviour {

    private Transform panelWaiting;
    
    private Transform panelMessageBox;
    private Text messageBoxLabel;
    private Text messageBoxMessage;
    private Button messageBoxButton;

    private Transform panelRigister, panelLogin, panelRecover;
    private Button buttonTabRegister, buttonTabLogin, buttonTabRecover;

    private InputField inputRegistrationLogin, inputRegistrationPassword, inputRegistrationEmain;
    private Button buttonRegistrationRegister;

    private InputField inputEnterLogin, inputEnterPassword;
    private Button buttonEnterLogin;


    private AuthorizationController authorizationController;
    public Connection connection;
    public UserManager userManager;
    public UserScreenController userScreenController;

    private MessageBox messageBox;

	void Awake () {
        gameObject.SetActive(true);
        panelWaiting = transform.Find("PanelWait");
        panelWaiting.gameObject.SetActive(false);

        messageBox = new MessageBox((RectTransform)transform.Find("MessageBox"));

        authorizationController = new AuthorizationController(connection, delegate()
        {
            messageBox.showMessage("Network error", "Sorry but there was some Network error. Check check your Internet connection and try again", delegate()
            {
                panelWaiting.gameObject.SetActive(false);
            });
        });

        setupTabs();
        setupRgistration();
        setupLogin();
        setupRecover();        
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
    
    private void userLogin(string login, string password)
    {
        if (string.IsNullOrEmpty(login))
        {
            messageBox.showMessage("Error", "Login is empty. Please enter login and try again.");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            messageBox.showMessage("Error", "Password is empty. Please enter password and try again.");
            return;
        }
        panelWaiting.gameObject.SetActive(true);
        authorizationController.loginUser(login, password, delegate(bool success, int result, int clientId, string session)
        {
            if (!success)
            {
                UnityAction hideWaiting = delegate()
                {
                    panelWaiting.gameObject.SetActive(false);
                };

                if (result == Ids.UserManagerResults.LOGIN_OR_PASSWORD_INVALID)
                {
                    messageBox.showMessage("Login error", "Invalid Login or Password. Check authorization information and try again.", hideWaiting);
                }
                else if (result == Ids.UserManagerResults.INVALID_DATA)
                {
                    messageBox.showMessage("Login error", "Invalid Login or Password. Check authorization information and try again.", hideWaiting);
                }
                else
                {
                    messageBox.showMessage("Error", "Sorry but there was some internal error, try again latter.", hideWaiting);
                }
            }
            else
            {
                userManager.UpdateClient(clientId, session);
                userScreenController.StartScreen();
                gameObject.SetActive(false);
            }
        });

    }

    private void userRegister(string login, string password, string email)
    {
        if (string.IsNullOrEmpty(login))
        {
            messageBox.showMessage("Error", "Login is empty. Please enter login and try again.");
            return;
        }
        if (string.IsNullOrEmpty(password))
        {
            messageBox.showMessage("Error", "Password is empty. Please enter password and try again.");
            return;
        }
        if (string.IsNullOrEmpty(email))
        {
            messageBox.showMessage("Error", "Email is empty. Please enter email and try again.");
            return;
        }
        panelWaiting.gameObject.SetActive(true);
        authorizationController.registerUser(login, password, email, delegate(bool success, bool invalidData, bool userExist)
        {
            UnityAction hideWaiting = delegate() 
            {
                panelWaiting.gameObject.SetActive(false);
            };
            if (invalidData)
            {
                messageBox.showMessage("Error", "Invalid registration information. Check your login, password, email and try again.", hideWaiting);
            }
            else if (userExist)
            {
                messageBox.showMessage("Error", "Sorry but User with such Login already exist. Please enter another Login and try again.", hideWaiting);
            }
            else if (success)
            {
                messageBox.showMessage("Success", "You was successfully Registered! Use this Login and Password to Enter.", delegate()
                {
                    panelWaiting.gameObject.SetActive(false);
                    inputEnterLogin.text = login;
                    inputEnterPassword.text = password;
                    selectTab(Tabs.Login);                    
                });
            }
            else
            {
                messageBox.showMessage("Error", "Sorry but there was some internal error, try again latter.", hideWaiting);
            }
        });
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
	
	void Update () {
	
	}
}
