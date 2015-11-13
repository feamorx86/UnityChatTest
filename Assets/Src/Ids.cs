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
        public static class Clients {
            public const int REGISTER_NEW = 100;
            public const int LOGIN = 200;
        }

        public static class Authorizations
        {
            public const int BY_LOGIN_AND_PASSWORD = 10;

            public const string REGISTER_JSON = "{{ 'type' : {0}, 'id' : '{1}', 'password' : '{2}' }}";
            public const string LOGIN_JSON = "{{ 'type' : {0}, 'id' : '{1}', 'password' : '{2}' }}";
        }

        public static class GameResolver
        {
            public const int GET_FULL_USER_INFO = 1;
            public const int GET_USER_INVENTORY = 2;
            public const int START_GAME_REQUEST = 3;
            public const int CANCEL_START_GAME = 4;
            public const int GAME_STARTED = 5;

            public const int GET_GAMES_LIST = 500;
            public const int GET_GAME_DESCRIPTION = 510;
            public const int ADD_GAME_TO_INVENTORY_REQUEST = 550;
        }
    }

    public static class GameResloverResults {
        public const int SUCCESS = 0;
        public const int INVALID_SESSION = 10;
        public const int INVALID_DATA = 20;
        public const int INTERNAL_ERROR = 15;
        public const int GAME_IS_UNAVALABLE_NOW = 50;
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
}

