using common;

namespace GameServer.networking.packets.incoming
{
    public class Hello : IncomingMessage
    {
        public string BuildVersion { get; set; }
        public int GameId { get; set; }
        public string GUID { get; set; }
        public string Password { get; set; }
        public string Secret { get; set; }
        public int KeyTime { get; set; }
        public byte[] Key { get; set; }
        public string MapJSON { get; set; }

        public override PacketId ID => PacketId.HELLO;

        public override Packet CreateInstance()
        {
            return new Hello();
        }

        protected override void Read(NReader rdr)
        {
            BuildVersion = rdr.ReadUTF();
            GameId = rdr.ReadInt32();
            GUID = rdr.ReadUTF();
            Password = rdr.ReadUTF();
            Secret = rdr.ReadUTF();
            KeyTime = rdr.ReadInt32();
            Key = rdr.ReadBytes(rdr.ReadInt16());
            MapJSON = rdr.Read32UTF();
        }

        protected override void Write(NWriter wtr)
        {
            wtr.WriteUTF(BuildVersion);
            wtr.Write(GameId);
            wtr.WriteUTF(GUID);
            wtr.WriteUTF(Password);
            wtr.WriteUTF(Secret);
            wtr.Write(KeyTime);
            wtr.Write((short)Key.Length);
            wtr.Write(Key);
            wtr.Write32UTF(MapJSON);
        }
    }
}