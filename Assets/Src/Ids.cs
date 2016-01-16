using System;
using UnityEngine;

public static class Ids
{
    public static class Services{
        public const int CLIENTS = 10;
        public const int GAMES = 20;
        public const int GAME_RESLOVER = 30;
    }

    public static class Actions {

        public const int HEART_BEAT_ACTION = -1;

        public static class Clients {
            public const int REGISTER_NEW = 100;
            public const int LOGIN = 200;
        }

        public static class Authorizations
        {
            public const int BY_LOGIN_AND_PASSWORD = 10;
        }

        public static class GameResolver
        {
            public const int GET_FULL_USER_INFO = 1;
            public const int GET_USER_INVENTORY = 2;
            public const int START_GAME_REQUEST = 3;
            public const int CANCEL_START_GAME = 4;
            public const int GAME_STARTED = 5;
            public const int GAME_FINISHED = 10;

            public const int GET_GAMES_LIST = 500;
            public const int GET_GAME_DESCRIPTION = 510;
            public const int ADD_GAME_TO_INVENTORY_REQUEST = 550;
        }
        
        public static class SimpleChat {
            private const int BASE = 10100;
            public const int NEW_USER = BASE + 1;
            public const int USER_EXIT= BASE + 2;
            public const int REQUEST_USERS_LIST = BASE + 4;
            public const int LIST_USERS = BASE + 5;
            public const int SEND_MESSAGE = BASE + 6;
            public const int SEND_MESSAGE_RESULT = BASE + 7;
            public const int RECEIVE_MESSAGE = BASE + 8;
            public const int REQUEST_USER_INFO = BASE + 9;

            public const int RESULT_NO_USER_WITH_SUCH_ID = SystemResults.SYSTEM_RESULTS_END + 3;
            public const int RESULT_INVALID_PLAYER_STATUS = SystemResults.SYSTEM_RESULTS_END + 4;
            public const int RESULT_USER_TIMEOUT = SystemResults.SYSTEM_RESULTS_END + 5;
            public const int RESULT_UNSUPPORTED_COMMAND = SystemResults.SYSTEM_RESULTS_END + 10;
            public const int RESULT_ERROR = SystemResults.SYSTEM_RESULTS_END + 30;
            public const int RESULT_END_GAME = SystemResults.SYSTEM_RESULTS_END + 40;
            public const int RESULT_USER_DISCONNECT_REQUEST = SystemResults.SYSTEM_RESULTS_END + 41;
        }
    }

    public static class SystemResults {
        public const int SUCCESS = 0;
        public const int INVALID_SESSION = 10;
        public const int INVALID_DATA = 20;
        public const int INTERNAL_ERROR = 15;
        public const int GAME_IS_UNAVALABLE_NOW = 50;
        public const int NO_GAME_WITH_SUCH_ID = 16;

        public const int SYSTEM_RESULTS_END = 100;
    }


    public static class UserManagerResults
    {
        public const int SUCCESS = 100;
        public const int INVALID_DATA = 110;
        public const int INTERNAL_ERROR = 120;

        public const int REGISTER_UNKNOWN_TYPE = 200;
        public const int REGISTER_SUCH_USER_EXIST = 201;

        public const int LOGIN_OR_PASSWORD_INVALID = 300;
    }

    public static class ErrorHandlers
    {
        public const int SystemController = 100;
        public const int UserInfoController = 200;
    }
}

