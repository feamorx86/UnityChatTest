using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;
using JsonFx.Json;

public class UserScreenController : MonoBehaviour
{

    private RectTransform contentPanel;
    private Scrollbar scroller;

    private RectTransform avalableGamesPanel;

    public Connection connection;
    public UserManager userManager;

    private UserInfoController dataController;
    private WWWLoader wwwLoader = new WWWLoader();

    private MessageBox messageBox;
}