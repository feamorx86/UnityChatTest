using System;
using UnityEngine;

public static class Ids
{
    public static class Services{
        public const int CLIENTS = 10;
    }

    public static class Actions {
        public static class Clients {
            public const int REGISTER_NEW = 100;
            public const int LOGIN = 200;
        }
    }

    public static class Authorizations {
        public const int BY_LOGIN_AND_PASSWORD = 10;

        public const string REGISTER_JSON = "{{ 'type' : {0}, 'id' : '{1}', 'password' : '{2}' }}";
        public const string LOGIN_JSON = "{{ 'type' : {0}, 'id' : '{1}', 'password' : '{2}' }}";
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

