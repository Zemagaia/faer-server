using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Shared;
using Shared.resources;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;
using GameServer.realm.entities.vendors;
using GameServer.realm.worlds.logic;
using NLog;
using StackExchange.Redis;
using wServer.realm;
using static GameServer.PacketUtils;

namespace GameServer;

public enum FailureType : sbyte {
    MessageNoDisconnect = -1,
    MessageWithDisconnect,
    ClientUpdateNeeded,
    ForceCloseGame,
    InvalidTeleportTarget
}

public enum PacketId : byte {
    CreateSuccess = 1,
    PlayerShoot = 3,
    Move = 4,
    PlayerText = 5,
    Text = 6,
    ServerPlayerShoot = 7,
    Damage = 8,
    Update = 9,
    UpdateAck = 10,
    Notification = 11,
    NewTick = 12,
    InvSwap = 13,
    UseItem = 14,
    ShowEffect = 15,
    Hello = 16,
    Goto = 17,
    InvDrop = 18,
    InvResult = 19,
    Reconnect = 20,
    Ping = 21,
    Pong = 22,
    MapInfo = 23,
    Teleport = 27,
    UsePortal = 28,
    Death = 29,
    Buy = 30,
    BuyResult = 31,
    Aoe = 32,
    GroundDamage = 33,
    PlayerHit = 34,
    EnemyHit = 35,
    AoeAck = 36,
    ShootAck = 37,
    OtherHit = 38,
    SquareHit = 39,
    GotoAck = 40,
    EditAccountList = 41,
    AccountList = 42,
    QuestObjId = 43,
    ChooseName = 44,
    NameResult = 45,
    CreateGuild = 46,
    GuildResult = 47,
    GuildRemove = 48,
    GuildInvite = 49,
    AllyShoot = 50,
    EnemyShoot = 51,
    RequestTrade = 52,
    TradeRequested = 53,
    TradeStart = 54,
    ChangeTrade = 55,
    TradeChanged = 56,
    AcceptTrade = 57,
    CancelTrade = 58,
    TradeDone = 59,
    TradeAccepted = 60,
    Escape = 63,
    InvitedToGuild = 65,
    JoinGuild = 66,
    ChangeGuildRank = 67,
    PlaySound = 68,
    Reskin = 70,
    ReskinVault = 71,
    Failure = 72,
    MapHello = 73
}

public class Client {
    private static Logger Log = LogManager.GetCurrentClassLogger();
    public Random ClientRandom = new((int) DateTime.Now.Ticks);
    public static Random StaticRandom = new((int) DateTime.Now.Ticks);
    private Server _server;
    public DbAccount Account;
    public DbChar Character;
    public string IP;
    public RealmManager Manager;
    public Player Player;
    public Socket Socket;
    private Memory<byte> ReceiveMem;
    private Memory<byte> SendMem;
    private const int PingPeriod = 3000;
    private const int DcThresold = 15000;
    private long _pingTime = -1L;
    private long _pongTime = -1L;
    private int _serial;

    public Client(Server server, RealmManager manager) {
        _server = server;
        Manager = manager;
        ReceiveMem = GC.AllocateArray<byte>(RECV_BUFFER_LEN, pinned: true).AsMemory();
        SendMem = GC.AllocateArray<byte>(SEND_BUFFER_LEN, pinned: true).AsMemory();
    }

    private async void Receive() {
        if (!Socket.Connected)
            return;

        try {
            while (Socket.Connected) {
                var len = await Socket.ReceiveAsync(ReceiveMem);
                if (len > 0)
                    ProcessPacket(len);
            }
        }
        catch (Exception e) {
            Disconnect();
            if (e is not SocketException se || (se.SocketErrorCode != SocketError.ConnectionReset &&
                                                se.SocketErrorCode != SocketError.Shutdown))
                Log.Error($"Could not receive data from {Account?.Name ?? "[unconnected]"} ({IP}): {e}");
        }
    }

    public void Reset(Socket socket) {
        Account = null;
        Character = null;
        Player = null;
        _pingTime = _pongTime = -1L;
        Socket = socket;
        Socket.DontFragment = true;
        try {
            IP = ((IPEndPoint) socket.RemoteEndPoint).Address.ToString();
        }
        catch (Exception) {
            IP = "";
        }

        Log.Trace("Received client @ {0}.", IP);
        Receive();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async void TrySend(int len) {
        if (!Socket.Connected)
            return;

        try {
            //Log.Error($"Sending packet {(PacketId) SendMem.Span[0]} {len}");
            await Socket.SendAsync(SendMem[..len]);
        }
        catch (Exception e) {
            Disconnect();
            if (e is not SocketException se || (se.SocketErrorCode != SocketError.ConnectionReset &&
                                                se.SocketErrorCode != SocketError.Shutdown))
                Log.Error($"{Account?.Name ?? "[unconnected]"} ({IP}): {e}");
        }
    }

    public void SendAccountList(int id, int[] list) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.AccountList);
        WriteInt(ref ptr, ref spanRef, id);
        WriteShort(ref ptr, ref spanRef, (short) list.Length);
        foreach (var i in list)
            WriteInt(ref ptr, ref spanRef, i);

        TrySend(ptr);
    }

    public void SendAllyShoot(byte bulletId, int ownerId, ushort containerType, float angle) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.AllyShoot);
        WriteByte(ref ptr, ref spanRef, bulletId);
        WriteInt(ref ptr, ref spanRef, ownerId);
        WriteUShort(ref ptr, ref spanRef, containerType);
        WriteFloat(ref ptr, ref spanRef, angle);
        TrySend(ptr);
    }

    public void SendAOE(float x, float y, float radius, ushort damage, ConditionEffectIndex effect, float duration,
        ushort origType, uint color) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.Aoe);
        WriteFloat(ref ptr, ref spanRef, x);
        WriteFloat(ref ptr, ref spanRef, y);
        WriteFloat(ref ptr, ref spanRef, radius);
        WriteUShort(ref ptr, ref spanRef, damage);
        WriteByte(ref ptr, ref spanRef, (byte) effect);
        WriteFloat(ref ptr, ref spanRef, duration);
        WriteUShort(ref ptr, ref spanRef, origType);
        WriteUInt(ref ptr, ref spanRef, color);
        TrySend(ptr);
    }

    public void SendBuyResult(int id, string res) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.BuyResult);
        WriteInt(ref ptr, ref spanRef, id);
        WriteString(ref ptr, ref spanRef, res);
        TrySend(ptr);
    }

    public void SendCreateSuccess(int objId, int charId) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.CreateSuccess);
        WriteInt(ref ptr, ref spanRef, objId);
        WriteInt(ref ptr, ref spanRef, charId);
        TrySend(ptr);
    }

    public void SendDamage(int targetId, ConditionEffects effects, ushort damage, bool kill, byte bulletId,
        int objectId) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.Damage);
        WriteInt(ref ptr, ref spanRef, targetId);
        WriteUInt(ref ptr, ref spanRef, (uint) (int) effects);
        WriteUShort(ref ptr, ref spanRef, damage);
        WriteBool(ref ptr, ref spanRef, kill);
        WriteByte(ref ptr, ref spanRef, bulletId);
        WriteInt(ref ptr, ref spanRef, objectId);
        TrySend(ptr);
    }

    public void SendDeath(int accId, int charId, string killedBy) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.Death);
        WriteInt(ref ptr, ref spanRef, accId);
        WriteInt(ref ptr, ref spanRef, charId);
        WriteString(ref ptr, ref spanRef, killedBy);
        TrySend(ptr);
    }

    public void SendEnemyShoot(byte bulletId, int ownerId, byte bulletType, float x, float y, float angle, short damage,
        byte numShots, float angleInc) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.EnemyShoot);
        WriteByte(ref ptr, ref spanRef, bulletId);
        WriteInt(ref ptr, ref spanRef, ownerId);
        WriteByte(ref ptr, ref spanRef, bulletType);
        WriteFloat(ref ptr, ref spanRef, x);
        WriteFloat(ref ptr, ref spanRef, y);
        WriteFloat(ref ptr, ref spanRef, angle);
        WriteShort(ref ptr, ref spanRef, damage);
        WriteByte(ref ptr, ref spanRef, numShots);
        WriteFloat(ref ptr, ref spanRef, angleInc);
        TrySend(ptr);
    }

    public void SendGoto(int objId, float x, float y) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.Goto);
        WriteInt(ref ptr, ref spanRef, objId);
        WriteFloat(ref ptr, ref spanRef, x);
        WriteFloat(ref ptr, ref spanRef, y);
        TrySend(ptr);
    }

    public void SendGuildResult(bool success, string errorText) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.GuildResult);
        WriteBool(ref ptr, ref spanRef, success);
        WriteString(ref ptr, ref spanRef, errorText);
        TrySend(ptr);
    }

    public void SendInvitedToGuild(string guildName, string name) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.InvitedToGuild);
        WriteString(ref ptr, ref spanRef, guildName);
        WriteString(ref ptr, ref spanRef, name);
        TrySend(ptr);
    }

    public void SendInvResult(int result) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.InvResult);
        WriteInt(ref ptr, ref spanRef, result);
        TrySend(ptr);
    }

    public void SendNameResult(bool success, string errorText) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.NameResult);
        WriteBool(ref ptr, ref spanRef, success);
        WriteString(ref ptr, ref spanRef, errorText);
        TrySend(ptr);
    }

    public void SendMapInfo(int width, int height, string name, string displayName, int diff, int background,
        bool allowTp, bool showDisplays) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.MapInfo);
        WriteInt(ref ptr, ref spanRef, width);
        WriteInt(ref ptr, ref spanRef, height);
        WriteString(ref ptr, ref spanRef, name);
        WriteString(ref ptr, ref spanRef, displayName);
        WriteInt(ref ptr, ref spanRef, background);
        WriteInt(ref ptr, ref spanRef, diff);
        WriteBool(ref ptr, ref spanRef, allowTp);
        WriteBool(ref ptr, ref spanRef, showDisplays);
        TrySend(ptr);
    }

    public void SendNewTick(byte tickId, byte tps, ObjectStats[] stats) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.NewTick);
        var lenPtr = ptr;
        WriteUShort(ref ptr, ref spanRef, 0);
        WriteByte(ref ptr, ref spanRef, tickId);
        WriteByte(ref ptr, ref spanRef, tps);
        WriteShort(ref ptr, ref spanRef, (short) stats.Length);
        for (var i = 0; i < stats.Length; i++) {
            var stat = stats[i];
            WriteInt(ref ptr, ref spanRef, stat.Id);
            WriteFloat(ref ptr, ref spanRef, stat.X);
            WriteFloat(ref ptr, ref spanRef, stat.Y);
            var statTypes = stat.StatTypes;
            if (statTypes == null) {
                WriteShort(ref ptr, ref spanRef, 0);
                continue;
            }

            WriteShort(ref ptr, ref spanRef, (short) statTypes.Length);
            foreach (var (key, value) in statTypes) {
                SendStat(ref ptr, ref spanRef, key, value);
            }
        }
        
        WriteUShort(ref lenPtr, ref spanRef, (ushort)ptr);

        TrySend(ptr);
    }

    public void SendNotification(int objId, string msg, uint color) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.Notification);
        WriteInt(ref ptr, ref spanRef, objId);
        WriteString(ref ptr, ref spanRef, msg);
        WriteUInt(ref ptr, ref spanRef, color);
        TrySend(ptr);
    }

    public void SendPing(int serial) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.Ping);
        WriteInt(ref ptr, ref spanRef, serial);
        TrySend(ptr);
    }

    public void SendShowEffect(EffectType effectType, int targetObjId, float x1, float y1, float x2, float y2,
        uint color) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.ShowEffect);
        WriteByte(ref ptr, ref spanRef, (byte) effectType);
        WriteInt(ref ptr, ref spanRef, targetObjId);
        WriteFloat(ref ptr, ref spanRef, x1);
        WriteFloat(ref ptr, ref spanRef, y1);
        WriteFloat(ref ptr, ref spanRef, x2);
        WriteFloat(ref ptr, ref spanRef, y2);
        WriteUInt(ref ptr, ref spanRef, color);
        TrySend(ptr);
    }

    public void SendText(string name, int objectId, byte bubbleTime, string recipient, string text,
        uint nameColor = 0xFF0000, uint textColor = 0xFFFFFF) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.Text);
        WriteString(ref ptr, ref spanRef, name);
        WriteInt(ref ptr, ref spanRef, objectId);
        WriteByte(ref ptr, ref spanRef, bubbleTime);
        WriteString(ref ptr, ref spanRef, recipient);
        WriteString(ref ptr, ref spanRef, text);
        if (text != "")
            WriteUInt(ref ptr, ref spanRef, textColor);
        if (name != "")
            WriteUInt(ref ptr, ref spanRef, nameColor);
        TrySend(ptr);
    }

    public void SendTradeAccepted(bool[] myOffer, bool[] yourOffer) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.TradeAccepted);
        WriteShort(ref ptr, ref spanRef, (short) myOffer.Length);
        foreach (var offer in myOffer)
            WriteBool(ref ptr, ref spanRef, offer);

        WriteShort(ref ptr, ref spanRef, (short) yourOffer.Length);
        foreach (var offer2 in yourOffer)
            WriteBool(ref ptr, ref spanRef, offer2);

        TrySend(ptr);
    }

    public void SendTradeChanged(bool[] offer) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.TradeChanged);
        WriteShort(ref ptr, ref spanRef, (short) offer.Length);
        foreach (var o in offer)
            WriteBool(ref ptr, ref spanRef, o);

        TrySend(ptr);
    }

    public void SendTradeDone(int code, string desc) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.TradeDone);
        WriteInt(ref ptr, ref spanRef, code);
        WriteString(ref ptr, ref spanRef, desc);
        TrySend(ptr);
    }

    public void SendTradeRequested(string name) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.TradeRequested);
        WriteString(ref ptr, ref spanRef, name);
        TrySend(ptr);
    }

    public void SendTradeStart(TradeItem[] myItems, string yourName, TradeItem[] yourItems) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.TradeStart);
        WriteShort(ref ptr, ref spanRef, (short) myItems.Length);
        for (var i = 0; i < myItems.Length; i++) {
            var offer = myItems[i];
            WriteInt(ref ptr, ref spanRef, offer.Item);
            WriteInt(ref ptr, ref spanRef, offer.SlotType);
            WriteBool(ref ptr, ref spanRef, offer.Tradeable);
            WriteBool(ref ptr, ref spanRef, offer.Included);
        }

        WriteString(ref ptr, ref spanRef, yourName);
        WriteShort(ref ptr, ref spanRef, (short) yourItems.Length);
        for (var j = 0; j < yourItems.Length; j++) {
            var offer2 = yourItems[j];
            WriteInt(ref ptr, ref spanRef, offer2.Item);
            WriteInt(ref ptr, ref spanRef, offer2.SlotType);
            WriteBool(ref ptr, ref spanRef, offer2.Tradeable);
            WriteBool(ref ptr, ref spanRef, offer2.Included);
        }

        TrySend(ptr);
    }

    public void SendUpdate(TileData[] tiles, ObjectDef[] newObjs, int[] drops) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.Update);
        var lenPtr = ptr;
        WriteUShort(ref ptr, ref spanRef, 0);
        WriteShort(ref ptr, ref spanRef, (short) tiles.Length);
        for (var i = 0; i < tiles.Length; i++) {
            var tile = tiles[i];
            WriteShort(ref ptr, ref spanRef, tile.X);
            WriteShort(ref ptr, ref spanRef, tile.Y);
            WriteUShort(ref ptr, ref spanRef, tile.Tile);
        }

        WriteShort(ref ptr, ref spanRef, (short) newObjs.Length);
        for (var j = 0; j < newObjs.Length; j++) {
            var newObj = newObjs[j];
            WriteUShort(ref ptr, ref spanRef, newObj.ObjectType);
            WriteInt(ref ptr, ref spanRef, newObj.Stats.Id);
            WriteFloat(ref ptr, ref spanRef, newObj.Stats.X);
            WriteFloat(ref ptr, ref spanRef, newObj.Stats.Y);
            var statTypes = newObj.Stats.StatTypes;
            if (statTypes == null) {
                WriteShort(ref ptr, ref spanRef, 0);
                continue;
            }

            WriteShort(ref ptr, ref spanRef, (short) statTypes.Length);
            foreach (var (key, value) in statTypes)
                SendStat(ref ptr, ref spanRef, key, value);
        }

        WriteShort(ref ptr, ref spanRef, (short) drops.Length);
        foreach (var drop in drops)
            WriteInt(ref ptr, ref spanRef, drop);
        
        WriteUShort(ref lenPtr, ref spanRef, (ushort)ptr);
        
        TrySend(ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SendStat(ref int ptr, ref byte spanRef, StatsType stat, object value) {
        WriteByte(ref ptr, ref spanRef, (byte) stat);
        switch (stat) {
            case StatsType.PortalUsable:
            case StatsType.HasBackpack:
                WriteBool(ref ptr, ref spanRef, Convert.ToInt32(value) == 1);
                return;
            case StatsType.MerchPrice:
            case StatsType.Tier:
            case StatsType.HealthStackCount:
            case StatsType.MagicStackCount:
                WriteByte(ref ptr, ref spanRef, Convert.ToByte(value));
                return;
            case StatsType.MerchCount:
            case StatsType.GuildRank:
                WriteSByte(ref ptr, ref spanRef, Convert.ToSByte(value));
                return;
            case StatsType.MaxMP:
            case StatsType.MP:
            case StatsType.Strength:
            case StatsType.Wit:
            case StatsType.Defense:
            case StatsType.Resistance:
            case StatsType.Speed:
            case StatsType.Haste:
            case StatsType.Stamina:
            case StatsType.Intelligence:
            case StatsType.Piercing:
            case StatsType.Penetration:
            case StatsType.Tenacity:
            case StatsType.HPBoost:
            case StatsType.MPBoost:
            case StatsType.StrengthBonus:
            case StatsType.WitBonus:
            case StatsType.DefenseBonus:
            case StatsType.ResistanceBonus:
            case StatsType.SpeedBonus:
            case StatsType.HasteBonus:
            case StatsType.StaminaBonus:
            case StatsType.IntelligenceBonus:
            case StatsType.PiercingBonus:
            case StatsType.PenetrationBonus:
            case StatsType.TenacityBonus:
            case StatsType.SellablePrice:
                WriteShort(ref ptr, ref spanRef, Convert.ToInt16(value));
                return;
            case StatsType.Size:
            case StatsType.Inv0:
            case StatsType.Inv1:
            case StatsType.Inv2:
            case StatsType.Inv3:
            case StatsType.Inv4:
            case StatsType.Inv5:
            case StatsType.Inv6:
            case StatsType.Inv7:
            case StatsType.Inv8:
            case StatsType.Inv9:
            case StatsType.Inv10:
            case StatsType.Inv11:
            case StatsType.MerchType:
            case StatsType.AltTextureIndex:
            case StatsType.Inv12:
            case StatsType.Inv13:
            case StatsType.Inv14:
            case StatsType.Inv15:
            case StatsType.Inv16:
            case StatsType.Inv17:
            case StatsType.Inv18:
            case StatsType.Inv19:
            case StatsType.Texture:
                WriteUShort(ref ptr, ref spanRef, Convert.ToUInt16(value));
                return;
            case StatsType.MaxHP:
            case StatsType.HP:
            case StatsType.Condition:
            case StatsType.Gems:
            case StatsType.AccountId:
            case StatsType.Gold:
            case StatsType.Crowns:
            case StatsType.OwnerAccountId:
                WriteInt(ref ptr, ref spanRef, Convert.ToInt32(value));
                return;
            case StatsType.Name:
            case StatsType.Guild:
                WriteString(ref ptr, ref spanRef, (string) value);
                return;
            case StatsType.DamageMultiplier:
            case StatsType.HitMultiplier:
                WriteFloat(ref ptr, ref spanRef, Convert.ToSingle(value));
                return;
            case StatsType.Texture1:
            case StatsType.Texture2:
            case StatsType.Glow:
            case StatsType.None:
                return;
        }

        Log.Error($"Unhandled stat {stat}, value: {value}");
    }

    public void Reconnect(string name, int gameId) {
        if (Account == null) {
            Disconnect("Tried to reconnect an client with a null account...");
            return;
        }

        Log.Trace("Reconnecting client ({0}) @ {1} to {2}...", Account.Name, IP, name);
        ConnectManager.Reconnect(this, gameId);
    }

    public void SendFailure(string text, FailureType errorId = FailureType.MessageWithDisconnect) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(SendMem.Span);
        WriteByte(ref ptr, ref spanRef, (byte) PacketId.Failure);
        WriteInt(ref ptr, ref spanRef, (int) errorId);
        WriteString(ref ptr, ref spanRef, text);
        TrySend(ptr);
        if (errorId == FailureType.MessageWithDisconnect || errorId == FailureType.ForceCloseGame)
            Disconnect();
    }

    private static bool ValidateEntities(Player p, Entity a, Entity b) {
        if (a == null || b == null || a is not IContainer || b is not IContainer || (a is Player && a != p) ||
            (b is Player && b != p) ||
            (a is Container container && container.BagOwners.Length != 0 &&
             !container.BagOwners.Contains(p.AccountId)) || (b is Container container2 &&
                                                             container2.BagOwners.Length != 0 &&
                                                             !container2.BagOwners.Contains(p.AccountId))) {
            return false;
        }

        return MathUtils.DistSqr(a.X, a.Y, b.X, b.Y) <= 1f;
    }

    private static bool ValidateSlotSwap(Player player, IContainer conA, IContainer conB, int slotA, int slotB) {
        return ((slotA < 12 && slotB < 12) || player.HasBackpack) && conB.AuditItem(conA.Inventory[slotA], slotB) &&
               conA.AuditItem(conB.Inventory[slotB], slotA);
    }

    private static bool ValidateItemSwap(Player player, Entity c, Item item) {
        return c == player || item == null || (!item.Untradable && !player.Client.Account.Admin) ||
               IsSoleContainerOwner(player, (IContainer) c);
    }

    private static bool IsSoleContainerOwner(Player player, IContainer con) {
        int[] owners = null;
        if (con is Container container) {
            owners = container.BagOwners;
        }

        return owners != null && owners.Length == 1 && owners.Contains(player.AccountId);
    }

    private static void DropInUntradableBag(Player player, Item item) {
        var container = new Container(player.Manager, 1283, 60000, dying: true) {
            BagOwners = new int[1] {player.AccountId},
            Inventory = {
                [0] = item
            }
        };
        container.Move(player.X + (float) ((StaticRandom.NextDouble() * 2.0 - 1.0) * 0.5),
            player.Y + (float) ((StaticRandom.NextDouble() * 2.0 - 1.0) * 0.5));
        container.SetDefaultSize(75);
        player.Owner.EnterWorld(container);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessPacket(int len) {
        var ptr = 0;
        ref var spanRef = ref MemoryMarshal.GetReference(ReceiveMem.Span);
        while (ptr < len) {
            var packetId = (PacketId) ReadByte(ref ptr, ref spanRef, len);
            //Log.Error($"Reading packet {packetId} {len}");
            switch (packetId) {
                case PacketId.AcceptTrade:
                    ProcessAcceptTrade(ReadBoolArray(ref ptr, ref spanRef, len),
                        ReadBoolArray(ref ptr, ref spanRef, len));
                    break;
                case PacketId.AoeAck:
                    ProcessAoeAck(ReadInt(ref ptr, ref spanRef, len), ReadFloat(ref ptr, ref spanRef, len),
                        ReadFloat(ref ptr, ref spanRef, len));
                    break;
                case PacketId.Buy:
                    ProcessBuy(ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.CancelTrade:
                    ProcessCancelTrade();
                    break;
                case PacketId.ChangeGuildRank:
                    ProcessChangeGuildRank(ReadString(ref ptr, ref spanRef, len), ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.ChangeTrade:
                    ProcessChangeTrade(ReadBoolArray(ref ptr, ref spanRef, len));
                    break;
                case PacketId.ChooseName:
                    ProcessChooseName(ReadString(ref ptr, ref spanRef, len));
                    break;
                case PacketId.CreateGuild:
                    ProcessCreateGuild(ReadString(ref ptr, ref spanRef, len));
                    break;
                case PacketId.EditAccountList:
                    ProcessEditAccountList(ReadInt(ref ptr, ref spanRef, len), ReadBool(ref ptr, ref spanRef, len),
                        ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.EnemyHit:
                    ProcessEnemyHit(ReadByte(ref ptr, ref spanRef, len), ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.Escape:
                    ProcessEscape();
                    break;
                case PacketId.GotoAck:
                    ProcessGotoAck(ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.GroundDamage:
                    ProcessGroundDamage(ReadInt(ref ptr, ref spanRef, len), ReadFloat(ref ptr, ref spanRef, len),
                        ReadFloat(ref ptr, ref spanRef, len));
                    break;
                case PacketId.GuildInvite:
                    ProcessGuildInvite(ReadString(ref ptr, ref spanRef, len));
                    break;
                case PacketId.GuildRemove:
                    ProcessGuildRemove(ReadString(ref ptr, ref spanRef, len));
                    break;
                case PacketId.Hello: {
                    var buildVer = ReadString(ref ptr, ref spanRef, len);
                    var gameId = ReadInt(ref ptr, ref spanRef, len);
                    var guid = ReadString(ref ptr, ref spanRef, len);
                    var pwd = ReadString(ref ptr, ref spanRef, len);
                    var chrId = ReadShort(ref ptr, ref spanRef, len);
                    var createChar = ReadBool(ref ptr, ref spanRef, len);
                    var charType = (ushort) (createChar ? ((ushort) ReadShort(ref ptr, ref spanRef, len)) : 0);
                    var skinType = (ushort) (createChar ? ((ushort) ReadShort(ref ptr, ref spanRef, len)) : 0);
                    ProcessHello(buildVer, gameId, guid, pwd, chrId, createChar, charType, skinType);
                    break;
                }
                case PacketId.MapHello: {
                    var buildVer = ReadString(ref ptr, ref spanRef, len);
                    var guid = ReadString(ref ptr, ref spanRef, len);
                    var pwd = ReadString(ref ptr, ref spanRef, len);
                    var chrId = ReadShort(ref ptr, ref spanRef, len);
                    var fm = new byte[ReadUShort(ref ptr, ref spanRef, len)];
                    // todo memcpy
                    for (var i = 0; i < fm.Length; i++)
                        fm[i] = ReadByte(ref ptr, ref spanRef, len);
                    ProcessMapHello(buildVer, guid, pwd, chrId, fm);
                    break;
                }
                case PacketId.InvDrop:
                    ProcessInvDrop(ReadInt(ref ptr, ref spanRef, len), ReadByte(ref ptr, ref spanRef, len),
                        ReadShort(ref ptr, ref spanRef, len));
                    break;
                case PacketId.InvSwap:
                    ProcessInvSwap(ReadInt(ref ptr, ref spanRef, len), ReadByte(ref ptr, ref spanRef, len),
                        ReadInt(ref ptr, ref spanRef, len), ReadByte(ref ptr, ref spanRef, len));
                    break;
                case PacketId.JoinGuild:
                    ProcessJoinGuild(ReadString(ref ptr, ref spanRef, len));
                    break;
                case PacketId.Move:
                    ProcessMove(ReadByte(ref ptr, ref spanRef, len), ReadInt(ref ptr, ref spanRef, len),
                        ReadFloat(ref ptr, ref spanRef, len), ReadFloat(ref ptr, ref spanRef, len),
                        ReadTimedPosArray(ref ptr, ref spanRef, len));
                    break;
                case PacketId.OtherHit:
                    ProcessOtherHit(ReadInt(ref ptr, ref spanRef, len), ReadByte(ref ptr, ref spanRef, len),
                        ReadInt(ref ptr, ref spanRef, len), ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.PlayerHit:
                    ProcessPlayerHit(ReadByte(ref ptr, ref spanRef, len), ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.PlayerShoot:
                    ProcessPlayerShoot(ReadInt(ref ptr, ref spanRef, len), ReadByte(ref ptr, ref spanRef, len),
                        ReadUShort(ref ptr, ref spanRef, len), ReadFloat(ref ptr, ref spanRef, len),
                        ReadFloat(ref ptr, ref spanRef, len), ReadFloat(ref ptr, ref spanRef, len));
                    break;
                case PacketId.PlayerText:
                    ProcessPlayerText(ReadString(ref ptr, ref spanRef, len));
                    break;
                case PacketId.Pong:
                    ProcessPong(ReadInt(ref ptr, ref spanRef, len), ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.RequestTrade:
                    ProcessRequestTrade(ReadString(ref ptr, ref spanRef, len));
                    break;
                case PacketId.Reskin:
                    ProcessReskin((ushort) ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.ReskinVault:
                    ProcessReskinVault((ushort) ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.ShootAck:
                    ProcessShootAck();
                    break;
                case PacketId.SquareHit:
                    ProcessSquareHit();
                    break;
                case PacketId.Teleport:
                    ProcessTeleport(ReadInt(ref ptr, ref spanRef, len));
                    break;
                case PacketId.UpdateAck:
                    ProcessUpdateAck();
                    break;
                case PacketId.UseItem:
                    ProcessUseItem(ReadInt(ref ptr, ref spanRef, len), ReadInt(ref ptr, ref spanRef, len),
                        ReadByte(ref ptr, ref spanRef, len), (ushort) ReadShort(ref ptr, ref spanRef, len),
                        ReadFloat(ref ptr, ref spanRef, len), ReadFloat(ref ptr, ref spanRef, len),
                        ReadByte(ref ptr, ref spanRef, len));
                    break;
                case PacketId.UsePortal:
                    ProcessUsePortal(ReadInt(ref ptr, ref spanRef, len));
                    break;
                default:
                    Log.Warn($"Unhandled packet '.{packetId}'.");
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessAcceptTrade(bool[] myOffer, bool[] yourOffer) {
        if (Player == null || Player.tradeAccepted) {
            return;
        }

        Player.trade = myOffer;
        if (!Player.tradeTarget.trade.SequenceEqual(yourOffer)) {
            return;
        }

        Player.tradeAccepted = true;
        Player.tradeTarget.Client.SendTradeAccepted(Player.tradeTarget.trade, Player.trade);
        if (!Player.tradeAccepted || !Player.tradeTarget.tradeAccepted) {
            return;
        }

        if (Player.Client.Account.Admin != Player.tradeTarget.Client.Account.Admin) {
            Player.tradeTarget.CancelTrade();
            Player.CancelTrade();
            return;
        }

        var failedMsg = "Error while trading. Trade unsuccessful.";
        var msg = "Trade Successful!";
        var thisItems = new List<Item>();
        var targetItems = new List<Item>();
        var tradeTarget = Player.tradeTarget;
        if (tradeTarget == null || Player.Owner == null || tradeTarget.Owner == null ||
            Player.Owner != tradeTarget.Owner) {
            SendTradeDone(1, failedMsg);
            tradeTarget?.Client.SendTradeDone(1, failedMsg);
            Player.ResetTrade();
        }
        else {
            if (!Player.tradeAccepted || !tradeTarget.tradeAccepted) {
                return;
            }

            var pInvTrans = Player.Inventory.CreateTransaction();
            var tInvTrans = tradeTarget.Inventory.CreateTransaction();
            for (var l = 4; l < Player.trade.Length; l++) {
                if (Player.trade[l]) {
                    thisItems.Add(Player.Inventory[l]);
                    pInvTrans[l] = null;
                }
            }

            for (var k = 4; k < tradeTarget.trade.Length; k++) {
                if (tradeTarget.trade[k]) {
                    targetItems.Add(tradeTarget.Inventory[k]);
                    tInvTrans[k] = null;
                }
            }

            for (var j = 0; j < 12; j++) {
                for (var m = 0; m < thisItems.Count; m++) {
                    if ((tradeTarget.SlotTypes[j] == 0 && tInvTrans[j] == null) || (thisItems[m] != null &&
                            ItemUtils.SlotsMatching(tradeTarget.SlotTypes[j], thisItems[m].SlotType) &&
                            tInvTrans[j] == null)) {
                        tInvTrans[j] = thisItems[m];
                        thisItems.Remove(thisItems[m]);
                        break;
                    }
                }
            }

            for (var i = 0; i < 12; i++) {
                for (var n = 0; n < targetItems.Count; n++) {
                    if ((Player.SlotTypes[i] == 0 && pInvTrans[i] == null) || (targetItems[n] != null &&
                                                                               ItemUtils.SlotsMatching(
                                                                                   Player.SlotTypes[i],
                                                                                   targetItems[n].SlotType) &&
                                                                               pInvTrans[i] == null)) {
                        pInvTrans[i] = targetItems[n];
                        targetItems.Remove(targetItems[n]);
                        break;
                    }
                }
            }

            if (!Inventory.Execute(pInvTrans, tInvTrans)) {
                SendTradeDone(1, failedMsg);
                tradeTarget?.Client.SendTradeDone(1, failedMsg);
                Player.ResetTrade();
                return;
            }

            if (thisItems.Count > 0 || targetItems.Count > 0) {
                msg = "An error occured while trading! Some items were lost!";
            }

            SendTradeDone(1, msg);
            tradeTarget?.Client.SendTradeDone(1, msg);
            Player.ResetTrade();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessAoeAck(int time, float x, float y) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessBuy(int id) {
        (Player?.Owner?.GetEntity(id) as SellableObject)?.Buy(Player);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessCancelTrade() {
        Player?.CancelTrade();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessChangeGuildRank(string name, int rank) {
        if (Player == null) {
            return;
        }

        var targetId = Manager.Database.ResolveId(name);
        if (targetId == 0) {
            Player.SendError("A player with that name does not exist.");
            return;
        }

        var targetCli = Manager.Clients.Keys.SingleOrDefault(c => c.Account.AccountId == targetId);
        var targetAcnt =
            ((targetCli != null) ? targetCli.Account : Manager.Database.GetAccount(targetId));
        if (Account.GuildId <= 0 || Account.GuildRank < 20 ||
            Account.GuildRank <= targetAcnt.GuildRank || Account.GuildRank < rank || rank == 40 ||
            Account.GuildId != targetAcnt.GuildId) {
            Player.SendError("No permission");
            return;
        }

        if (1 == 0) { }

        var text = rank switch {
            0 => "Initiate",
            10 => "Member",
            20 => "Officer",
            30 => "Leader",
            40 => "Founder",
            _ => "",
        };
        if (1 == 0) { }

        var targetRank = targetAcnt.GuildRank;
        if (targetRank == rank) {
            Player.SendError("Player is already a " + text);
            return;
        }

        if (!Manager.Database.ChangeGuildRank(targetAcnt, rank)) {
            Player.SendError("Failed to change rank.");
            return;
        }

        if (targetCli != null) {
            targetCli.Player.GuildRank = (sbyte) rank;
        }

        if (targetRank < rank) {
            Manager.Chat.Guild(Player, targetAcnt.Name + " has been promoted to " + text + ".", announce: true);
        }
        else {
            Manager.Chat.Guild(Player, targetAcnt.Name + " has been demoted to " + text + ".", announce: true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessChangeTrade(bool[] offer) {
        if (Player?.tradeTarget == null) {
            return;
        }

        for (var i = 0; i < offer.Length; i++) {
            if (offer[i] && Player.Inventory[i].Untradable) {
                Player.SendError("You can't trade Untradable items.");
                return;
            }
        }

        Player.tradeAccepted = false;
        Player.tradeTarget.tradeAccepted = false;
        Player.trade = offer;
        Player.tradeTarget.Client.SendTradeChanged(offer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessChooseName(string name) {
        if (Player == null) {
            return;
        }

        Manager.Database.ReloadAccount(Account);
        var length = name.Length;
        if (length < 1 || length > 10 || !name.All(char.IsLetter)) {
            SendNameResult(success: false, "Invalid name");
            return;
        }

        string lockToken = null;
        try {
            while ((lockToken = Manager.Database.AcquireLock("nameLock")) == null) { }

            if (Manager.Database.Conn.HashExists("names", name.ToUpperInvariant())) {
                SendNameResult(success: false, "Duplicated name");
                return;
            }

            if (Account.Credits < 1000) {
                SendNameResult(success: false, "Not enough gold");
                return;
            }

            Manager.Database.UpdateCredit(Account, -1000);
            while (!Manager.Database.RenameIGN(Account, name, lockToken)) { }

            Player.Credits = Account.Credits;
            Player.Name = Account.Name;
            SendNameResult(success: true, "");
        }
        finally {
            if (lockToken != null) {
                Manager.Database.ReleaseLock("nameLock", lockToken);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessCreateGuild(string guildName) {
        if (Player == null) {
            return;
        }

        if (Account.GuildId > 0) {
            SendGuildResult(success: false, "Guild Creation Error: Already in a guild");
            return;
        }

        var guildRes = Manager.Database.CreateGuild(guildName, out var createdGuild);
        if ((int) guildRes > 0) {
            SendGuildResult(success: false,
                "Guild Creation Error: " + guildRes);
            return;
        }

        var addResult = Manager.Database.AddGuildMember(createdGuild, Account, true);
        if ((int) addResult > 0) {
            SendGuildResult(success: false,
                "Guild Creation Error: " + addResult);
            return;
        }

        Player.Guild = createdGuild.Name;
        Player.GuildRank = 40;
        SendGuildResult(success: true, "Success!");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessEditAccountList(int action, bool add, int id) {
        if (Player == null) {
            return;
        }

        var targetPlayer = Player.Owner.GetEntity(id) as Player;
        if (targetPlayer?.Client.Account == null) {
            Player.SendError("Player not found.");
            return;
        }

        switch (action) {
            case 0:
                Manager.Database.LockAccount(Account, targetPlayer.Client.Account, add);
                break;
            case 1:
                Manager.Database.IgnoreAccount(Account, targetPlayer.Client.Account, add);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessEnemyHit(byte bulletId, int targetId) {
        if (Player?.Owner != null && Player.Owner.Enemies.TryGetValue(targetId, out var en))
            Player._projectiles[bulletId].ForceHit(en, Manager.Logic.WorldTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessEscape() {
        if (Player?.Owner != null) {
            Reconnect("Hub", -2);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessGotoAck(int time) {
        Player.GotoAckReceived();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessGroundDamage(int time, float x, float y) {
        if (Player?.Owner != null) {
            //Player.DamagePlayerGround(x, y, dmg);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessGuildInvite(string name) {
        if (Player == null) {
            return;
        }

        if (Account.GuildRank < 20) {
            Player.SendError("Insufficient privileges.");
            return;
        }

        foreach (var client in Manager.Clients.Keys) {
            if (client.Player == null || client.Account == null || !client.Account.Name.Equals(name)) {
                continue;
            }

            if (client.Account.GuildId > 0) {
                Player.SendError("Player is already in a guild.");
                return;
            }

            client.Player.GuildInvite = Account.GuildId;
            client.SendInvitedToGuild(Player.Guild, Account.Name);
            return;
        }

        Player.SendError("Could not find the player to invite.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessGuildRemove(string name) {
        if (Player == null) {
            return;
        }

        if (Account.Name.Equals(name)) {
            Manager.Chat.Guild(Player, Player.Name + " has left the guild.", announce: true);
            if (!Manager.Database.RemoveFromGuild(Account)) {
                Player.SendError("Guild not found.");
                return;
            }

            Player.Guild = "";
            Player.GuildRank = 0;
            return;
        }

        var targetAccId = Manager.Database.ResolveId(name);
        if (targetAccId == 0) {
            Player.SendError("Player not found");
            return;
        }

        var targetClient = (from client in Manager.Clients.Keys
            where client.Account != null
            where client.Account.AccountId == targetAccId
            select client).FirstOrDefault();
        if (targetClient != null) {
            if (Account.GuildRank >= 20 && Account.GuildId == targetClient.Account.GuildId &&
                Account.GuildRank > targetClient.Account.GuildRank) {
                if (!Manager.Database.RemoveFromGuild(targetClient.Account)) {
                    Player.SendError("Guild not found.");
                    return;
                }

                targetClient.Player.Guild = "";
                targetClient.Player.GuildRank = 0;
                Manager.Chat.Guild(Player,
                    targetClient.Player.Name + " has been kicked from the guild by " + Player.Name, announce: true);
                targetClient.Player.SendInfo("You have been kicked from the guild.");
            }
            else {
                Player.SendError("Can't remove member. Insufficient privileges.");
            }

            return;
        }

        var targetAccount = Manager.Database.GetAccount(targetAccId);
        if (Account.GuildRank >= 20 && Account.GuildId == targetAccount.GuildId &&
            Account.GuildRank > targetAccount.GuildRank) {
            if (!Manager.Database.RemoveFromGuild(targetAccount)) {
                Player.SendError("Guild not found.");
                return;
            }

            Manager.Chat.Guild(Player, targetAccount.Name + " has been kicked from the guild by " + Player.Name,
                announce: true);
            Manager.Chat.SendInfo(targetAccId, "You have been kicked from the guild.");
        }
        else {
            Player.SendError("Can't remove member. Insufficient privileges.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessHello(string buildVer, int gameId, string guid, string pwd, int charId, bool createChar,
        ushort charType, ushort skinType) {
        var version = Manager.Config.serverSettings.version;
        if (!version.Equals(buildVer)) {
            SendFailure(version, FailureType.ClientUpdateNeeded);
            return;
        }

        var val = Manager.Database.Verify(guid, pwd, out var acc);
        if (val is LoginStatus.InvalidCredentials or LoginStatus.AccountNotExists) {
            SendFailure("Failed to login: Invalid credentials");
            return;
        }

        if (acc.Banned || Manager.Database.IsIpBanned(IP)) {
            SendFailure("Failed to login: Account banned");
            Log.Info($"Banned user <{acc.Name}> (ip: {IP}) tried to log in.");
            return;
        }

        if (!acc.Admin && Manager.Config.serverInfo.adminOnly) {
            SendFailure("Failed to login: Insufficient permissions");
            return;
        }

        Manager.Database.LogAccountByIp(IP, acc.AccountId);
        acc.IP = IP;
        acc.FlushAsync();
        Account = acc;
        if (createChar) {
            var status = Manager.Database.CreateCharacter(Manager.Resources.GameData, acc, charType, skinType,
                out var character);
            switch (status) {
                case CreateStatus.ReachCharLimit:
                    SendFailure("Too many characters");
                    return;
                case CreateStatus.SkinUnavailable:
                    SendFailure("Skin unavailable");
                    return;
                case CreateStatus.Locked:
                    SendFailure("Class locked");
                    return;
            }

            Character = character;
            Player = new Player(this);
            charId = character.CharId;
        }

        ConnectManager.Connect(this, gameId, charId);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessMapHello(string buildVer, string guid, string pwd, short charId, byte[] fm) {
        var version = Manager.Config.serverSettings.version;
        if (!version.Equals(buildVer)) {
            SendFailure(version, FailureType.ClientUpdateNeeded);
            return;
        }

        var val = Manager.Database.Verify(guid, pwd, out var acc);
        if (val is LoginStatus.InvalidCredentials or LoginStatus.AccountNotExists) {
            SendFailure("Failed to login: Invalid credentials");
            return;
        }

        if (acc.Banned || Manager.Database.IsIpBanned(IP)) {
            SendFailure("Failed to login: Account banned");
            Log.Info($"Banned user <{acc.Name}> (ip: {IP}) tried to log in.");
            return;
        }

        if (!acc.Admin && Manager.Config.serverInfo.adminOnly) {
            SendFailure("Failed to login: Insufficient permissions");
            return;
        }

        Manager.Database.LogAccountByIp(IP, acc.AccountId);
        acc.IP = IP;
        acc.FlushAsync();
        Account = acc;

        ConnectManager.MapConnect(this, charId, fm);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessInvDrop(int objId, byte slotId, short objType) {
        if (Player?.Owner == null || Player.tradeTarget != null) {
            return;
        }

        IContainer con;
        if (objId != Player.Id) {
            if (Player.Owner.GetEntity(objId) is Player) {
                SendInvResult(1);
                return;
            }

            con = Player.Owner.GetEntity(objId) as IContainer;
        }
        else {
            con = Player;
        }

        if (con?.Inventory[slotId] == null) {
            SendInvResult(1);
            return;
        }

        var dropItem = con.Inventory[slotId];
        con.Inventory[slotId] = null;
        Container container;
        if (dropItem.Untradable || Player.Client.Account.Admin) {
            var container2 = new Container(Player.Manager, 1287, 60000, dying: true);
            container2.BagOwners = new int[1] {Player.AccountId};
            container = container2;
        }
        else {
            container = new Container(Player.Manager, 1280, 60000, dying: true);
        }

        container.Inventory[0] = dropItem;
        container.Move(Player.X + (float) ((ClientRandom.NextDouble() * 2.0 - 1.0) * 0.5),
            Player.Y + (float) ((ClientRandom.NextDouble() * 2.0 - 1.0) * 0.5));
        container.SetDefaultSize(75);
        Player.Owner.EnterWorld(container);
        SendInvResult(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessInvSwap(int objId1, byte slotId1, int objId2, byte slotId2) {
        if (Player?.Owner == null) {
            return;
        }

        var a = Player.Owner.GetEntity(objId1);
        var b = Player.Owner.GetEntity(objId2);
        if (!ValidateEntities(Player, a, b) || Player.tradeTarget != null) {
            a.ForceUpdate(slotId1);
            b.ForceUpdate(slotId2);
            SendInvResult(1);
            return;
        }

        var conA = (IContainer) a;
        var conB = (IContainer) b;
        if (b == Player) {
            var stacks = Player.Stacks;
            foreach (var stack in stacks) {
                if (stack.Slot == slotId2) {
                    var stackTrans = conA.Inventory.CreateTransaction();
                    if (stack.Put(stackTrans[slotId1]) == null) {
                        stackTrans[slotId1] = null;
                        Inventory.Execute(stackTrans);
                        SendInvResult(0);
                        return;
                    }
                }
            }
        }

        if (!ValidateSlotSwap(Player, conA, conB, slotId1, slotId2)) {
            a.ForceUpdate(slotId1);
            b.ForceUpdate(slotId2);
            SendInvResult(1);
            return;
        }

        var queue = new Queue<Action>();
        var conATrans = conA.Inventory.CreateTransaction();
        var conBTrans = conB.Inventory.CreateTransaction();
        var itemA = conATrans[slotId1];
        var itemB = conBTrans[slotId2];
        conBTrans[slotId2] = itemA;
        conATrans[slotId1] = itemB;
        if (!ValidateItemSwap(Player, a, itemB)) {
            queue.Enqueue(delegate { DropInUntradableBag(Player, itemB); });
            conATrans[slotId1] = null;
        }

        if (!ValidateItemSwap(Player, b, itemA)) {
            queue.Enqueue(delegate { DropInUntradableBag(Player, itemA); });
            conBTrans[slotId2] = null;
        }

        if (Inventory.Execute(conATrans, conBTrans)) {
            while (queue.Count > 0) {
                queue.Dequeue()();
            }

            SendInvResult(0);
        }
        else {
            a.ForceUpdate(slotId1);
            b.ForceUpdate(slotId2);
            SendInvResult(1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ProcessJoinGuild(string guildName) {
        if (Player == null) {
            return;
        }

        if (!Player.GuildInvite.HasValue) {
            Player.SendError("You have not been invited to a guild.");
            return;
        }

        var guild = Manager.Database.GetGuild(Player.GuildInvite.Value);
        if (guild == null) {
            Player.SendError("Internal server error.");
            return;
        }

        if (!guild.Name.Equals(guildName, StringComparison.InvariantCultureIgnoreCase)) {
            Player.SendError("You have not been invited to join " + guildName + ".");
            return;
        }

        var guildResult = Manager.Database.AddGuildMember(guild, Account);
        if ((int) guildResult > 0) {
            Player.SendError("Could not join guild. (" + guildResult + ")");
            return;
        }

        Player.Guild = guild.Name;
        Player.GuildRank = 0;
        Manager.Chat.Guild(Player, Player.Name + " has joined the guild!", announce: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessMove(byte tickId, int time, float x, float y, TimedPosition[] records) {
        if (Player?.Owner == null || x < 0f || x >= Player.Owner.Map.Width || y < 0f ||
            y >= Player.Owner.Map.Height)
            return;
        Player.MoveReceived(Manager.Logic.WorldTime, tickId, time);
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        if (x != Player.X || y != Player.Y)
            Player.Move(x, y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessOtherHit(int time, byte bulletId, int ownerId, int targetId) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessPlayerHit(byte bulletId, int objId) {
        if (Player?.Owner != null && Player.Owner.Enemies.TryGetValue(objId, out var enemy)) {
            enemy._projectiles[bulletId].ForceHit(Player, Manager.Logic.WorldTime);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessPlayerShoot(int time, byte bulletId, ushort objType, float x, float y, float angle) {
        if (Manager.Resources.GameData.Items.TryGetValue(objType, out var item) && item != Player.Inventory[1]) {
            var prjDesc = item.Projectiles[0];
            var prj = Player.PlayerShootProjectile(bulletId, prjDesc, item.ObjectType, time, x, y, angle, bulletId);
            Player.Owner.EnterWorld(prj);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessPlayerText(string text) {
        if (Player?.Owner == null || text.Length > 512)
            return;

        var manager = Player.Manager;
        if (text[0] == '/') {
            manager.Commands.Execute(Player, Manager.Logic.WorldTime, text);
            return;
        }

        if (Player.Muted) {
            Player.SendError("Muted. You can not talk at this time.");
            return;
        }

        manager.Chat.Say(Player, text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessPong(int serial, int pongTime) {
        Player?.Pong(Manager.Logic.WorldTime, serial, pongTime);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessRequestTrade(string name) {
        Player?.RequestTrade(name);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessReskin(ushort skinId) {
        if (Player == null) {
            return;
        }

        var gameData = Manager.Resources.GameData;
        Account.Reload("skins");
        var ownedSkins = Account.Skins;
        var currentClass = Player.ObjectType;
        var skinData = gameData.Skins;
        var skinSize = 100;
        if (skinId != 0) {
            skinData.TryGetValue(skinId, out var skinDesc);
            if (skinDesc == null) {
                Player.SendError("Unknown skin type.");
                return;
            }

            if (!ownedSkins.Contains(skinId)) {
                Player.SendError("Skin not owned.");
                return;
            }

            if (skinDesc.PlayerClassType != currentClass) {
                Player.SendError("Skin is for different class.");
                return;
            }

            skinSize = skinDesc.Size;
        }

        Player.SetDefaultSkin(skinId);
        Player.SetDefaultSize((ushort) skinSize);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessReskinVault(ushort skinId) {
        if (Player != null) {
            /*if (Account.VaultSkins.Contains(skinId)) {
                Account.VaultSkin = skinId;
            }*/

            Account.FlushAsync();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessShootAck() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessSquareHit() { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessTeleport(int objId) {
        if (Player?.Owner != null) {
            Player.Teleport(Manager.Logic.WorldTime, objId);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessUpdateAck() {
        Player.UpdateAckReceived();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessUseItem(int time, int objId, byte slotId, ushort objType, float x, float y, byte useType) {
        if (Player?.Owner != null) {
            Player.UseItem(objId, slotId, x, y);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessUsePortal(int objId) {
        var entity = Player?.Owner?.GetEntity(objId);
        if (entity == null) {
            return;
        }

        if (entity is GuildHallPortal guildHallPortal) {
            if (string.IsNullOrEmpty(Player.Guild)) {
                Player.SendError("You are not in a guild.");
            }
            else {
                if (guildHallPortal.ObjectType != 1839) {
                    Player.SendInfo("Portal not implemented.");
                    return;
                }

                /*ProtoWorld proto = Player.Manager.Resources.Worlds.Item("GuildHall");
                var world2 = Player.Manager.GetWorld(proto.id);
                Player.Reconnect(world2);*/
            }
        }

        if (entity is not Portal portal || !portal.Usable)
            return;

        lock (portal.CreateWorldLock) {
            var world = portal.WorldInstance;
            if (world != null) {
                Player.Reconnect(world);
                return;
            }

            if (portal.CreateWorldTask == null || portal.CreateWorldTask.IsCompleted) {
                portal.CreateWorldTask = Task.Factory.StartNew(delegate { portal.CreateWorld(Player); }).ContinueWith(
                    delegate(Task e) {
                        if (e.Exception?.InnerException != null) {
                            Log.Error(e.Exception!.InnerException!.ToString());
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
            }

            portal.WorldInstanceSet += Player.Reconnect;
        }
    }

    public void Disconnect(string reason = "") {
        if (reason != "") {
            var log = Log;
            var account = Account;
            log.Info("Disconnecting client ({0}) @ {1}... {2}",
                account?.Name ?? "[unconnected]", IP, reason);
        }

        if (Account != null) {
            try {
                Save();
            }
            catch (Exception e) {
                var msg = e.Message + "\n" + e.StackTrace;
                Log.Error(msg);
            }
        }

        Manager.Disconnect(this);
        _server.Disconnect(this);
    }

    private void Save() {
        var acc = Account;
        if (Character == null || Player == null) {
            Manager.Database.ReleaseLock(acc);
            return;
        }

        Player.SaveToCharacter();
        acc.RefreshLastSeen();
        acc.FlushAsync();
        Manager.Database.SaveCharacter(acc, Character, true)
            .ContinueWith(delegate { Manager.Database.ReleaseLock(acc); });
    }

    public bool KeepAlive(RealmTime time, int position, int count) {
        if (_pingTime == -1) {
            _pingTime = time.TotalElapsedMs - 3000;
            _pongTime = time.TotalElapsedMs;
        }

        if (time.TotalElapsedMs - _pongTime > 15000) {
            Disconnect("Queue connection timeout. (KeepAlive)");
            return false;
        }

        if (time.TotalElapsedMs - _pingTime < 3000) {
            return true;
        }

        _pingTime = time.TotalElapsedMs;
        _serial = (int) _pingTime;
        return true;
    }
}