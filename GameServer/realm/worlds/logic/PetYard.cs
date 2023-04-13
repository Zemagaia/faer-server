using common.resources;
using common.terrain;
using GameServer.networking;
using GameServer.realm.entities;

namespace GameServer.realm.worlds.logic
{
    public class PetYard : World
    {
        private readonly Client _client;
        private readonly int _accountId;

        public PetYard(ProtoWorld proto, Client client = null) : base(proto)
        {
            if (client == null)
                return;

            _client = client;
            _accountId = _client.Account.AccountId;
        }

        public override bool AllowedAccess(Client client)
        {
            return base.AllowedAccess(client) && _accountId == client.Account.AccountId;
        }

        protected override void Init()
        {
            if (IsLimbo)
                return;

            switch (_client.Account.PetYardType)
            {
                case 2:
                    LoadMap("wServer.realm.worlds.maps.PetYard1.wmap");
                    break;
                case 3:
                    LoadMap("wServer.realm.worlds.maps.PetYard2.wmap");
                    break;
                case 4:
                    LoadMap("wServer.realm.worlds.maps.PetYard3.wmap");
                    break;
                case 5:
                    LoadMap("wServer.realm.worlds.maps.PetYard4.wmap");
                    break;
                default:
                    LoadMap("wServer.realm.worlds.maps.PetYard0.wmap");
                    break;
            }

            LoadPets();
        }

        private void LoadPets()
        {
            if (!Manager.Config.serverSettings.enablePets && !Manager.Config.serverSettings.debugMode)
                return;

            var acc = _client.Account;
            foreach (var petData in acc.PetDatas)
            {
                if (_client.Player.PetData.Id == petData.Id)
                {
                    continue;
                }
                
                var pet = new Enemy(Manager, petData.ObjectType);
                var sPos = GetPetSpawnPosition();
                pet.Move(sPos.X, sPos.Y);
                pet.PetData = petData;
                EnterWorld(pet);
            }
        }

        public Position GetPetSpawnPosition()
        {
            var x = 0;
            var y = 0;

            var spawnRegions = Map.Regions.Where(t => t.Value == TileRegion.PetRegion).ToArray();
            if (spawnRegions.Length > 0)
            {
                var sRegion = spawnRegions.ElementAt(Rand.Next(0, spawnRegions.Length));
                x = sRegion.Key.X;
                y = sRegion.Key.Y;
            }

            return new Position() { X = x, Y = y };
        }
    }
}