namespace GameServer.networking.packets
{
    public enum PacketId : byte
    {
        FAILURE = 0,
        USEITEM = 1,
        RESKIN_UNLOCK = 2,
        ACCEPTTRADE = 3,
        TRADECHANGED = 4,
        USEPORTAL = 6,
        HELLO = 9,
        PLAYERHIT = 10,
        CHANGEGUILDRANK = 11,
        CREATE = 12,
        SQUAREHIT = 13,
        RECONNECT = 14,
        RESKIN = 15,
        MOVE = 16,
        ARENA_DEATH = 17, // unused
        INVDROP = 18,
        ENEMYHIT = 19,
        CHECKCREDITS = 20,
        NAMERESULT = 22,
        CHOOSENAME = 23,
        GLOBAL_NOTIFICATION = 24,
        INVSWAP = 25,
        LOAD = 26,
        JOINGUILD = 27,
        QUESTOBJID = 28,
        LOGIN_REWARD_MSG = 29, // unused
        GOTO = 30,
        TRADESTART = 31,
        CLAIM_LOGIN_REWARD_MSG = 32, // unused
        NOTIFICATION = 33,
        REQUESTTRADE = 34,
        SHOOTACK = 35,
        ALLYSHOOT = 36,
        SHOWEFFECT = 38,
        CANCELTRADE = 39,
        KEY_INFO_RESPONSE = 40,
        GUILDINVITE = 41,
        UPDATE = 42,
        KEY_INFO_REQUEST = 43,
        ACCOUNTLIST = 44,
        TELEPORT = 45,
        PIC = 46,
        PLAYERTEXT = 47,
        ENTER_ARENA = 48, // unused
        GUILDREMOVE = 49,
        BUYRESULT = 50,
        ENEMYSHOOT = 52,
        PETYARDUPDATE = 53, // unused
        QUEST_ROOM_MSG = 54,
        CHANGETRADE = 55,
        FILE = 56,
        OTHERHIT = 57,
        INVITEDTOGUILD = 58,
        PLAYSOUND = 59,
        SETCONDITION = 60,
        VERIFY_EMAIL = 61,
        EDITACCOUNTLIST = 62,
        INVRESULT = 63,
        PONG = 64,
        PLAYERSHOOT = 66,
        PETUPGRADEREQUEST = 67, // unused
        NEWTICK = 68,
        PASSWORD_PROMPT = 69,
        PET_CHANGE_FORM_MSG = 70, // unused
        SERVER_FULL = 71,
        QUEUE_PING = 72,
        QUEUE_PONG = 73,
        MAPINFO = 74,
        CLIENTSTAT = 75,
        ACTIVEPETUPDATE = 76, // unused
        AOEACK = 77,
        TRADEACCEPTED = 78,
        GOTOACK = 79,
        TRADEREQUESTED = 80,
        CREATE_SUCCESS = 81,
        GUILDRESULT = 82,
        DEATH = 83,
        ACCEPT_ARENA_DEATH = 84, // unused
        PING = 85,
        HATCH_PET = 86, // unused
        ESCAPE = 87,
        AOE = 89,
        ACTIVE_PET_UPDATE_REQUEST = 90, // unused
        UPDATEACK = 91,
        SERVERPLAYERSHOOT = 92,
        BUY = 93,
        TRADEDONE = 94,
        CREATEGUILD = 95,
        TEXT = 96,
        DAMAGE = 97,
        SET_FOCUS = 99,
        // 98 unused
        SWITCH_MUSIC = 100,

        // Market
        MARKET_SEARCH = 101,
        MARKET_SEARCH_RESULT = 102,
        MARKET_BUY = 103,
        MARKET_BUY_RESULT = 104,
        MARKET_ADD = 105,
        MARKET_ADD_RESULT = 106,
        MARKET_REMOVE = 107,
        MARKET_REMOVE_RESULT = 108,
        MARKET_MY_OFFERS = 109,
        MARKET_MY_OFFERS_RESULT = 110,

        // Quests
        FETCH_AVAILABLE_QUESTS = 111,
        FETCH_AVAILABLE_QUESTS_RESULT = 112,
        ACCEPT_QUEST = 113,
        FETCH_CHARACTER_QUESTS = 114,
        FETCH_CHARACTER_QUESTS_RESULT = 115,
        DELIVER_ITEMS_RESULT = 116,

        // Mail
        FETCH_MAIL = 117,
        FETCH_MAIL_RESULT = 118,

        // Account Quests
        FETCH_ACCOUNT_QUESTS = 119,
        FETCH_ACCOUNT_QUESTS_RESULT = 37,

        // Forge
        CRAFT_ITEM = 51,
        CRAFT_ANIMATION = 7,

        // Skill Tree abilities
        OFFENSIVE_ABILITY = 65,
        DEFENSIVE_ABILITY = 88,
        
        // Pets
        FETCH_PETS = 8,
        FETCH_PETS_RESULT = 21,
        DELETE_PET = 53,
        PET_FOLLOW = 67,
        
        CURRENT_TIME = 5,
    }
}