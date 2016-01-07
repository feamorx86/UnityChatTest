using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;
using UnityEngine;
using System.Text;
using MiscUtil.IO;
using MiscUtil.Conversion;


public class BaseGameController
{
    protected int gameId;
    protected BaseUiController uiController;

    public BaseGameController()
    {
    }

    public int GameId
    {
        get { return gameId; }
        set { gameId = value; }
    }

    public virtual void setUi(BaseUiController uiController)
    {
        this.uiController = uiController;
    }

    protected virtual void updateUi(object param)
    {
        if (uiController != null)
        {
            uiController.updateUi(param);
        }
    }

    public virtual void prepareGame()
    {

    }

    public virtual void startGame()
    {
        
    }

    public virtual void finishGame(int code, object parameters)
    {
        
    }

    public virtual void playerStopGame(object parameters)
    {

    }

    public virtual void disconnected()
    {

    }

    public virtual void connectionRestored(object parameters)
    {

    }

    public virtual void cancelPrepare()
    {

    }

    public bool checkErrorResult(int result)
    {
        bool hasError;
        if (result <= Ids.SystemResults.SYSTEM_RESULTS_END)
        {
            switch (result)
            {
                case Ids.SystemResults.SUCCESS:
                    hasError = false;
                    break;
                case Ids.SystemResults.INVALID_SESSION:
                    Debug.LogError("Invalid session!");
                    hasError = true;
                    break;
                case Ids.SystemResults.INVALID_DATA:
                    Debug.LogError("Invalid data!");
                    hasError = true;
                    break;
                case Ids.SystemResults.NO_GAME_WITH_SUCH_ID:
                    Debug.LogError("No game with such id!");
                    hasError = true;
                    break;
                default:
                    Debug.LogError("Unknown result = "+result.ToString());
                    hasError = true;
                    break;
            }
        }
        else
        {
            hasError= false;//in game result code
        }
        return !hasError;
    }
   
}
