using common;
using common.resources;
using GameServer.networking.packets;
using GameServer.networking.packets.incoming.quests;
using GameServer.networking.packets.outgoing;
using GameServer.networking.packets.outgoing.quests;
using GameServer.realm;
using GameServer.realm.worlds;

namespace GameServer.networking.handlers.quests
{
    class AcceptQuestHandler : PacketHandlerBase<AcceptQuest>
    {
        public override PacketId ID => PacketId.ACCEPT_QUEST;

        protected override void HandlePacket(Client client, AcceptQuest packet)
        {
            client.Manager.Logic.AddPendingAction(_ => Handle(client, packet));
        }

        private void Handle(Client client, AcceptQuest packet)
        {
            var player = client.Player;
            if (player == null || IsTest(client))
                return;

            QuestData availableQuest = null;
            QuestData[] availableQuests = null;
            AcceptedQuestData characterQuest = null;
            AcceptedQuestData[] characterQuests = null;
            AcceptedQuestData accountQuest = null;
            AcceptedQuestData[] accountQuests = null;
            AccountMail mail;
            if (packet.Type == AcceptQuest.Accept || packet.Type == AcceptQuest.Dismiss)
            {
                availableQuests = player.AvailableQuests;
                if (availableQuests != null)
                    for (var i = 0; i < availableQuests.Length; i++)
                        if (availableQuests[i].Id == packet.Id)
                        {
                            availableQuest = availableQuests[i];
                            break;
                        }
            }
            else
            {
                // all other actions done with this packet will be related to character quests
                characterQuests = player.CharacterQuests;
                if (characterQuests != null)
                    for (var i = 0; i < characterQuests.Length; i++)
                        if (characterQuests[i].Id == packet.Id)
                        {
                            characterQuest = characterQuests[i];
                            break;
                        }
            }

            if (packet.Type == AcceptQuest.Delete_Account || packet.Type == AcceptQuest.Deliver_Account ||
                packet.Type == AcceptQuest.Scout_Account)
            {
                accountQuests = player.Client.Account.AccountQuests;
                if (accountQuests != null)
                    for (var i = 0; i < accountQuests.Length; i++)
                        if (accountQuests[i].Id == packet.Id)
                        {
                            accountQuest = accountQuests[i];
                            break;
                        }
            }

            switch (packet.Type)
            {
                case AcceptQuest.Accept:
                    if (availableQuest == null) return;
                    // Save us the power from checking anything else if they don't actually have that available quest
                    if (!player.Quests.HasAvailableQuest(availableQuest.Id))
                    {
                        client.SendPacket(new FetchAvailableQuestsResult
                        {
                            Results = availableQuests,
                            Description = $"Quest was not found/does not exist"
                        });
                        return;
                    }

                    // Character quest cap is 9 due to possible performance problems(?)
                    if (player.CharacterQuests.Length >= 9)
                    {
                        client.SendPacket(new FetchAvailableQuestsResult
                        {
                            Results = availableQuests,
                            Description = $"You cannot have more than 9 character quests at a time"
                        });
                        return;
                    }

                    // Add quest to character and remove available quest
                    player.Quests.AddCharacterQuest(availableQuest);

                    // Update available quests as data has been updated
                    availableQuests = player.AvailableQuests;
                    if (availableQuests.Length == 0)
                    {
                        client.SendPacket(new FetchAvailableQuestsResult
                        {
                            Results = availableQuests,
                            Description = $"You do not have any available quests, Quest Added"
                        });
                        return;
                    }

                    client.SendPacket(new FetchAvailableQuestsResult
                    {
                        Results = availableQuests,
                        Description = "Quest added"
                    });
                    return;
                case AcceptQuest.Dismiss:
                    if (availableQuest == null) return;
                    // Save us the power from checking anything else if they don't actually have that available quest
                    if (!player.Quests.HasAvailableQuest(availableQuest.Id))
                    {
                        client.SendPacket(new FetchAvailableQuestsResult
                        {
                            Results = availableQuests,
                            Description = $"Quest was not found/does not exist"
                        });
                        return;
                    }

                    mail = new AccountMail()
                    {
                        AddTime = DateTime.UtcNow.ToUnixTimestamp(),
                        CharacterId = client.Character.CharId,
                        Content = $"Quest <b>\"{availableQuest.Title}\"</b> ({availableQuest.Id}) dismissed!",
                        Priority = 1
                    };
                    player.Mails.Add(mail);
                    player.SendInfo($"New mail: Quest dismissed!");

                    // Remove available quest
                    player.Quests.RemoveAvailableQuest(availableQuest.Id);

                    // Update available quests
                    availableQuests = player.AvailableQuests;
                    if (availableQuests.Length == 0)
                    {
                        client.SendPacket(new FetchAvailableQuestsResult
                        {
                            Results = new QuestData[0],
                            Description = $"You do not have any available quests"
                        });
                        return;
                    }

                    client.SendPacket(new FetchAvailableQuestsResult
                    {
                        Results = availableQuests,
                        Description = ""
                    });
                    return;
                case AcceptQuest.Delete:
                    if (characterQuest == null) return;
                    // Save us the power from checking anything else if they don't actually have that available quest
                    if (!player.Quests.HasCharacterQuest(characterQuest.Id))
                    {
                        client.SendPacket(new FetchCharacterQuestsResult
                        {
                            Results = characterQuests,
                            Description = $"Character quest was not found/does not exist"
                        });
                        return;
                    }

                    mail = new AccountMail()
                    {
                        AddTime = DateTime.UtcNow.ToUnixTimestamp(),
                        CharacterId = client.Character.CharId,
                        Content = $"Quest <b>\"{characterQuest.Title}\"</b> ({characterQuest.Id}) deleted!",
                        Priority = 1
                    };
                    player.Mails.Add(mail);
                    player.SendInfo($"New mail: Quest deleted!");

                    // Remove character quest
                    player.Quests.RemoveCharacterQuest(characterQuest.Id);

                    // Update character quests
                    characterQuests = player.CharacterQuests;
                    if (characterQuests.Length == 0)
                    {
                        client.SendPacket(new FetchCharacterQuestsResult
                        {
                            Results = new AcceptedQuestData[0],
                            Description = $"You do not have any character quests"
                        });
                        return;
                    }

                    client.SendPacket(new FetchCharacterQuestsResult
                    {
                        Results = characterQuests,
                        Description = ""
                    });
                    return;
                case AcceptQuest.Deliver:
                    if (characterQuest == null) return;
                    // Save us the power from checking anything else if they don't actually have that available quest
                    if (!player.Quests.HasCharacterQuest(characterQuest.Id))
                    {
                        client.SendPacket(new FetchCharacterQuestsResult
                        {
                            Results = characterQuests,
                            Description = $"Character quest was not found/does not exist"
                        });
                        return;
                    }

                    // Try to deliver items
                    player.Quests.UpdateDeliveryStatus(characterQuest.Id, false);
                    return;
                case AcceptQuest.Scout:
                    if (characterQuest == null || characterQuest.Scouted) return;
                    // Save us the power from checking anything else if they don't actually have that available quest
                    if (!player.Quests.HasCharacterQuest(characterQuest.Id))
                    {
                        client.SendPacket(new FetchCharacterQuestsResult
                        {
                            Results = characterQuests,
                            Description = $"Character quest was not found/does not exist"
                        });
                        return;
                    }

                    // Get world...
                    var scoutProto = player.Owner.Manager.Resources.Worlds[characterQuest.Scout];
                    scoutProto.scoutQuestActive = true;
                    // Add world!
                    DynamicWorld.TryGetWorld(scoutProto, player.Client, out var world);
                    world = player.Owner.Manager.AddWorld(world ?? new World(scoutProto));
                    // Warp to world
                    player.BroadcastSync(new ShowEffect()
                    {
                        EffectType = EffectType.Earthquake
                    });
                    player.Owner.Timers.Add(new WorldTimer(500, (_, _) =>
                    {
                        player.Client.Reconnect(new Reconnect
                        {
                            Host = "",
                            Port = 2050,
                            GameId = world.Id,
                            Name = world.SBName
                        });
                        world.ScoutQuestActive = scoutProto.scoutQuestActive;
                    }));
                    break;
                case AcceptQuest.Delete_Account:
                    if (accountQuest == null) return;
                    // Save us the power from checking anything else if they don't actually have that available quest
                    if (!player.Quests.HasAccountQuest(accountQuest.Id))
                    {
                        client.SendPacket(new FetchAccountQuestsResult
                        {
                            Results = accountQuests,
                            Description = $"Account quest was not found/does not exist"
                        });
                        return;
                    }

                    mail = new AccountMail()
                    {
                        AddTime = DateTime.UtcNow.ToUnixTimestamp(),
                        CharacterId = client.Character.CharId,
                        Content = $"Account Quest <b>\"{accountQuest.Title}\"</b> ({accountQuest.Id}) deleted!",
                        Priority = 1
                    };
                    player.Mails.Add(mail);
                    player.SendInfo($"New mail: Account Quest deleted!");

                    // Remove account quest
                    player.Quests.RemoveAccountQuest(accountQuest.Id, accountQuest.DailyQuest);

                    // Update account quests
                    accountQuests = player.Client.Account.AccountQuests;

                    if (accountQuests.Length == 0)
                    {
                        client.SendPacket(new FetchAccountQuestsResult
                        {
                            Results = new AcceptedQuestData[0],
                            Description = $"You do not have any account quests"
                        });
                        return;
                    }

                    client.SendPacket(new FetchAccountQuestsResult()
                    {
                        Results = accountQuests,
                        Description = ""
                    });
                    return;
                case AcceptQuest.Deliver_Account:
                    if (accountQuest == null) return;
                    // Save us the power from checking anything else if they don't actually have that available quest
                    if (!player.Quests.HasAccountQuest(accountQuest.Id))
                    {
                        client.SendPacket(new FetchAccountQuestsResult
                        {
                            Results = accountQuests,
                            Description = $"Account quest was not found/does not exist"
                        });
                        return;
                    }

                    // Try to deliver items
                    player.Quests.UpdateDeliveryStatus(accountQuest.Id, true);
                    return;
                case AcceptQuest.Scout_Account:
                    if (accountQuest == null || accountQuest.Scouted) return;
                    // Save us the power from checking anything else if they don't actually have that available quest
                    if (!player.Quests.HasAccountQuest(accountQuest.Id))
                    {
                        client.SendPacket(new FetchAccountQuestsResult
                        {
                            Results = accountQuests,
                            Description = $"Account quest was not found/does not exist"
                        });
                        return;
                    }

                    // Get world...
                    var scoutAccProto = player.Owner.Manager.Resources.Worlds[accountQuest.Scout];
                    scoutAccProto.scoutQuestActive = true;
                    // Add world!
                    DynamicWorld.TryGetWorld(scoutAccProto, player.Client, out var wld);
                    wld = player.Owner.Manager.AddWorld(wld ?? new World(scoutAccProto));
                    // Warp to world
                    player.BroadcastSync(new ShowEffect()
                    {
                        EffectType = EffectType.Earthquake
                    });
                    player.Owner.Timers.Add(new WorldTimer(500, (_, _) =>
                    {
                        player.Client.Reconnect(new Reconnect
                        {
                            Host = "",
                            Port = 2050,
                            GameId = wld.Id,
                            Name = wld.SBName
                        });
                        wld.ScoutQuestActive = scoutAccProto.scoutQuestActive;
                    }));
                    break;
                default:
                    if (availableQuest != null)
                    {
                        client.SendPacket(new FetchAvailableQuestsResult
                        {
                            Results = availableQuests,
                            Description = $"Invalid action type"
                        });
                        return;
                    }

                    if (characterQuest != null)
                    {
                        client.SendPacket(new FetchCharacterQuestsResult
                        {
                            Results = characterQuests,
                            Description = $"Invalid action type"
                        });
                        return;
                    }

                    if (accountQuest != null)
                    {
                        client.SendPacket(new FetchAccountQuestsResult
                        {
                            Results = accountQuests,
                            Description = $"Invalid action type"
                        });
                        return;
                    }

                    return;
            }
        }
    }
}