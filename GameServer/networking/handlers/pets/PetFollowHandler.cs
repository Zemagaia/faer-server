using common;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming.pets;

namespace GameServer.networking.handlers.pets
{
    class PetFollowHandler : PacketHandlerBase<PetFollow>
    {
        public override PacketId ID => PacketId.PET_FOLLOW;

        protected override void HandlePacket(Client client, PetFollow packet)
        {
            client.Manager.Logic.AddPendingAction(_ => { Handle(client, packet); });
        }

        private void Handle(Client client, PetFollow packet)
        {
            var player = client.Player;
            if (player == null || IsTest(client))
            {
                return;
            }

            var petData = packet.PetData;
            var pets = client.Account.PetDatas.ToList();
            for (var i = 0; i < pets.Count; i++)
            {
                // ignore other pets
                if (pets[i].Id != petData.Id)
                {
                    continue;
                }
                
                var petName = player.Manager.Resources.GameData.ObjectTypeToId[pets[i].ObjectType];
                // unfollow if selected
                if (pets[i].Id == petData.Id && player.PetData.Id == pets[i].Id)
                {
                    player.SendInfo($"{petName} is no longer following you");
                    player.SpawnPet(null);
                    player.PetData = new PetData();
                    continue;
                }

                // follow if not selected
                player.SendInfo($"{petName} is now following you");
                player.PetData = pets[i];
                player.SpawnPet(player.Owner);
            }

            player.Client.Account.PetDatas = pets.ToArray();
        }
    }
}