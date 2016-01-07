﻿using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Text;
using MiscUtil.IO;
using MiscUtil.Conversion;
using JsonFx.Json;

public class LibraryGame
{
    public int id;
    public string alias;
    public Type controllerClass;
    public string prefabName;

    public LibraryGame(int id, string alias, Type controllerClass, string prefabName)
    {
        this.id = id;
        this.alias = alias;
        this.controllerClass = controllerClass;
        this.prefabName = prefabName;
    }

    public BaseGameController createController()
    {
        BaseGameController controller = (BaseGameController)Activator.CreateInstance(controllerClass);
        return controller;
    }

    public BaseUiController createUiController(Transform parent)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabName);
        GameObject gameObject = (GameObject)GameObject.Instantiate(prefab);
        gameObject.transform.SetParent(parent, false);
        BaseUiController ui = gameObject.GetComponent<BaseUiController>();
        return ui;
    }
}

public class GameLibrary
{
    private Dictionary<int, LibraryGame> gamesById = new Dictionary<int, LibraryGame>();
    private Dictionary<string, LibraryGame> gamesByAlias = new Dictionary<string, LibraryGame>();

    
    public GameLibrary()
    {
        createGamesLibrary();
    }

    private void addGame(LibraryGame game)
    {
        gamesById[game.id] = game;
        gamesByAlias[game.alias] = game;
    }

    private void createGamesLibrary()
    {
        addGame(new LibraryGame(10, "test", typeof(BaseGameController), ""));        
        addGame(new LibraryGame(11, "chat", typeof(ChatGameController), "UI/ChatScreen/ChatGame"));
    }

    public LibraryGame get(int id)
    {
        LibraryGame game;
        if (gamesById.TryGetValue(id, out game))
        {
            return game;
        }
        else
        {
            return null;
        }
    }

    public LibraryGame get(string alias)
    {
        LibraryGame game;
        if (gamesByAlias.TryGetValue(alias, out game))
        {
            return game;
        }
        else
        {
            return null;
        }
    }
}