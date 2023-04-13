using common.resources;
using GameServer.networking.packets.outgoing.quests;

namespace GameServer.realm.logic.quests
{
    public partial class Quests
    {
        /// <summary>
        /// Deliver items
        /// </summary>
        public void UpdateDeliveryStatus(int id, bool acc)
        {
            int i;
            var sendBreak = false;
            // ignore equipment for safety reasons...
            for (i = 6; i < (_player.HasBackpack ? _player.Inventory.Length : 18); i++)
            {
                // make sure to not waste time if item is null
                if (_player.Inventory[i] == new ItemData()) continue;
                if (!acc)
                {
                    HandleCharacterQuestDelivery(id, i, ref sendBreak);
                    if (sendBreak)
                        return;
                    continue;
                }

                HandleAccountQuestDelivery(id, i, ref sendBreak);
                if (sendBreak)
                    return;
            }
        }

        private void HandleAccountQuestDelivery(int id, int i, ref bool sendBreak)
        {
            var quests = _player.Client.Account.AccountQuests;
            for (var n = 0; n < quests.Length; n++)
            {
                // skip quests that don't include delivery tasks or don't match id
                if (quests[n].Deliver.Length == 0 || quests[n].Id != id)
                {
                    continue;
                }

                var goals = quests[n].Goals[0];
                for (var j = 0; j < quests[n].Deliver.Length; j++)
                {
                    var deliverData = quests[n].DeliverDatas[j];
                    // deliver stuff if able
                    if (deliverData == null || _player.Inventory[i].Item == null)
                    {
                        continue;
                    }

                    if (_player.Inventory[i].ObjectType == quests[n].Deliver[j] &&
                        _player.Inventory[i].Quantity == deliverData.MaxQuantity)
                    {
                        quests[n].Delivered[j] = true;
                        quests[n].Goals[0]++;
                        _player.Inventory[i] = new ItemData();
                        break;
                    }
                }

                _player.Client.SendPacket(new DeliverItemsResult
                {
                    Results = quests[n].Delivered
                });

                if (goals != quests[n].Goals[0])
                {
                    sendBreak = true;
                }

                break;
            }

            _player.Client.Account.AccountQuests = quests;
        }

        private void HandleCharacterQuestDelivery(int id, int i, ref bool sendBreak)
        {
            var quests = _player.CharacterQuests;
            for (var n = 0; n < quests.Length; n++)
            {
                // skip quests that don't include delivery tasks or don't match id
                if (quests[n].Deliver.Length == 0 || quests[n].Id != id)
                {
                    continue;
                }

                var goals = quests[n].Goals[0];
                for (var j = 0; j < quests[n].Deliver.Length; j++)
                {
                    var deliverData = quests[n].DeliverDatas[j];
                    // deliver stuff if able
                    if (deliverData == null || _player.Inventory[i].Item == null)
                    {
                        continue;
                    }

                    if (_player.Inventory[i].ObjectType == quests[n].Deliver[j] &&
                        _player.Inventory[i].Quantity == deliverData.MaxQuantity)
                    {
                        quests[n].Delivered[j] = true;
                        quests[n].Goals[0]++;
                        _player.Inventory[i] = new ItemData();
                        break;
                    }
                }

                _player.Client.SendPacket(new DeliverItemsResult
                {
                    Results = quests[n].Delivered
                });

                if (goals != quests[n].Goals[0])
                {
                    sendBreak = true;
                }

                break;
            }
        }
    }
}