using common;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming.pets;

namespace GameServer.networking.handlers.pets
{
    class DeletePetHandler : PacketHandlerBase<DeletePet>
    {
        public override PacketId ID => PacketId.DELETE_PET;

        protected override void HandlePacket(Client client, DeletePet packet)
        {
            client.Manager.Logic.AddPendingAction(_ => { Handle(client, packet); });
        }

        private void Handle(Client client, DeletePet packet)
        {
            var player = client.Player;
            if (player == null || IsTest(client))
            {
                return;
            }

            var petData = packet.PetData;
            var pets = client.Account.PetDatas.ToList();
            string petName;
            for (var i = 0; i < pets.Count; i++)
            {
                // ignore other pets
                if (pets[i].Id != petData.Id)
                {
                    continue;
                }

                // unfollow if selected
                if (pets[i].Id == petData.Id && player.PetData.Id == pets[i].Id)
                {
                    player.PetData = new PetData();
                }

                // delete pet
                petName = player.Manager.Resources.GameData.ObjectTypeToId[pets[i].ObjectType];
                player.SendInfo($"{petName} has been released to the wild");
                pets.RemoveAt(i);
            }
            
            var chars = client.Manager.Database.GetAliveCharacters(client.Account);
            foreach (var @char in chars)
            {
                var charPet = client.Manager.Database.Conn.HashGet($"char.{player.AccountId}.{@char}", "petData");
                var charPetData = new PetData(charPet);
                if (charPetData.Id == petData.Id)
                {
                    client.Manager.Database.Conn.HashSet($"char.{player.AccountId}.{@char}", "petData", new PetData().Export());
                }
            }

            player.Client.Account.PetDatas = pets.ToArray();
        }
    }
}