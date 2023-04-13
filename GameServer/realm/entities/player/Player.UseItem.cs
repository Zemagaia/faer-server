using common;
using common.resources;
using GameServer.networking.packets;
using GameServer.networking.packets.outgoing;
using GameServer.realm.worlds;
using GameServer.realm.worlds.logic;
using StackExchange.Redis;

namespace GameServer.realm.entities.player
{
    partial class Player
    {
        public const int MaxAbilityDist = 14;

        public static readonly ConditionEffect[] NegativeEffs = {
            new()
            {
                Effect = ConditionEffectIndex.Slow,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Paralyzed,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Weak,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Stunned,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Confused,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Blind,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Stupefied,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Unarmored,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Bleeding,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Crippled,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Sick,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Drunk,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Hallucinating,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Hexed,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Unsteady,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Unsighted,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Curse,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Suppressed,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Exposed,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Staggered,
                DurationMS = 0
            }
        };

        private readonly object _useLock = new();
        private bool _skinSet;
        private int _flamePillarState;

        public void UseItem(RealmTime time, int objId, int slot, Position pos, byte activateId, int clientTime)
        {
            lock (_useLock)
            {
                var entity = Owner.GetEntity(objId);
                if (entity == null)
                {
                    Client.SendPacket(new InvResult { Result = 1 });
                    return;
                }

                if (entity is Player && objId != Id)
                {
                    Client.SendPacket(new InvResult { Result = 1 });
                    return;
                }

                var container = entity as IContainer;

                // eheh no more clearing BBQ loot bags
                if (this.Dist(entity) > 3)
                {
                    Client.SendPacket(new InvResult { Result = 1 });
                    return;
                }

                var cInv = container?.Inventory.CreateTransaction();

                // get item
                Item item = null;
                ItemData itemData = null;
                foreach (var stack in Stacks.Where(stack => stack.Slot == slot))
                {
                    item = stack.Pull();

                    if (item == null)
                        return;

                    itemData = ItemData.GenerateData(item);
                    break;
                }

                if (item == null)
                {
                    if (container == null)
                        return;

                    itemData = cInv[slot];
                    item = itemData.Item;
                }

                if (item == null || itemData == null)
                    return;

                // make sure not trading and trying to consume item
                if (tradeTarget != null && item.Consumable)
                    return;

                if (MP < item.MpCost && activateId == 1
                    || MP < item.MpCost2 && activateId == 2
                    || HP < item.HpCost && activateId == 1
                    || HasConditionEffect(ConditionEffects.Suppressed) && slot == 1)
                {
                    Client.SendPacket(new InvResult { Result = 1 });
                    return;
                }

                // use item
                var slotType = 10;
                if (slot < cInv.Length)
                {
                    slotType = container.SlotTypes[slot];

                    if (item.Consumable)
                    {
                        var db = Manager.Database;

                        var successor = new ItemData();
                        if (item.SuccessorId != null)
                            successor = ItemData.GenerateData(item);
                        if (container is not GiftChest)
                            cInv[slot] = successor;

                        if (itemData.Quantity > 0 || item.Quantity > 0 && item.MaxQuantity > 0)
                        {
                            // consume item and add quantity to item data if it's unavailable
                            if (itemData.MaxQuantity == 0 && itemData.Quantity == 0)
                            {
                                itemData.MaxQuantity = item.MaxQuantity;
                                itemData.Quantity = item.Quantity;
                            }
                            if (itemData.Quantity - 1 > 0)
                            {
                                successor = itemData;
                                if (container is not GiftChest)
                                {
                                    itemData.Quantity--;
                                    cInv[slot] = successor;
                                    entity.ForceUpdate(slot);
                                }
                            }
                        }

                        var trans = db.Conn.CreateTransaction();
                        if (container is GiftChest)
                        {
                            SendError("Cannot use consumables from gift chest.");
                            return;
                        }

                        var task = trans.ExecuteAsync();
                        task.ContinueWith(t =>
                        {
                            var success = !t.IsCanceled && t.Result;
                            if (!success || !Inventory.Execute(cInv)) // can result in the loss of an item if inv trans fails...
                            {
                                entity.ForceUpdate(slot);
                                return;
                            }

                            if (slotType > 0)
                            {
                                FameCounter.UseAbility();
                            }
                            else
                            {
                                if (item.ActivateEffects.Any(eff => eff.Effect == ActivateEffects.Heal ||
                                                                    eff.Effect == ActivateEffects.HealNova ||
                                                                    eff.Effect == ActivateEffects.Magic ||
                                                                    eff.Effect == ActivateEffects.MagicNova))
                                {
                                    FameCounter.DrinkPot();
                                }
                            }

                            Activate(time, item, itemData, pos, activateId, clientTime, slot == 1);
                        });
                        task.ContinueWith(e =>
                                Log.Error(e.Exception.InnerException.ToString()),
                            TaskContinuationOptions.OnlyOnFaulted);
                        return;
                    }

                    if (slotType > 0)
                    {
                        FameCounter.UseAbility();
                    }
                }
                else
                {
                    FameCounter.DrinkPot();
                }

                if (item.Consumable || item.SlotType == slotType)
                    Activate(time, item, itemData, pos, activateId, clientTime, slot == 1);
                else
                    Client.SendPacket(new InvResult { Result = 1 });
            }
        }

        private long _clientNextUse;
        private long _clientNextUse2;
        private double _coolDown;
        public int NextAttackMpRefill;

        private void Activate(RealmTime time, Item item, ItemData itemData, Position target, byte activateId,
            int clientTime, bool isAbility)
        {
            if (IsInvalidTime(time.TotalElapsedMs, clientTime))
                return;

            var activate = activateId > 1 ? item.ActivateEffects2 : item.ActivateEffects;
            var tolerance = 0.98;
            var lastUse = _clientNextUse;
            if (activate != null && isAbility &&
                ((activateId == 1 && clientTime > _clientNextUse) || _clientNextUse == 0) &&
                ((activateId == 2 && clientTime > _clientNextUse2) || _clientNextUse2 == 0))
            {
                if (activateId == 1)
                {
                    if (_clientNextUse == 0) _clientNextUse = clientTime;
                    _coolDown = item.Cooldown * 1000 * (100f / (100 + Stats[11]));
                    _clientNextUse = (long)(clientTime + _coolDown * tolerance);
                }
                else
                {
                    if (_clientNextUse2 == 0) _clientNextUse = clientTime;
                    _clientNextUse2 = (long)(clientTime + item.Cooldown2 * 1000 * (100f / (100 + Stats[11])) * tolerance);
                }
            }
            else if (isAbility && (clientTime < _clientNextUse || activate == null))
                return;

            // Handle item powers
            if (isAbility)
            {
                if (!HandleEffectsOnActivate(item, target, MathUtils.Random, clientTime, lastUse))
                {
                    return;
                }
            }

            if (activate == item.ActivateEffects)
            {
                MP -= item.MpCost;
                HP -= item.HpCost;
            }
            
            if (activate == item.ActivateEffects2)
            {
                MP -= item.MpCost2;
            }

            foreach (var eff in activate)
            {
                switch (eff.Effect)
                {
                    case ActivateEffects.GenericActivate:
                        AEGenericActivate(time, item, target, eff);
                        break;
                    case ActivateEffects.BulletNova:
                        AEBulletNova(time, item, itemData, target, eff, clientTime);
                        break;
                    case ActivateEffects.Shoot:
                        AEShoot(time, item, itemData, target, eff, clientTime);
                        break;
                    case ActivateEffects.StatBoostSelf:
                        AEStatBoostSelf(time, item, target, eff);
                        break;
                    case ActivateEffects.StatBoostAura:
                        AEStatBoostAura(time, item, target, eff);
                        break;
                    case ActivateEffects.ConditionEffectSelf:
                        AEConditionEffectSelf(time, item, target, eff);
                        break;
                    case ActivateEffects.ConditionEffectAura:
                        AEConditionEffectAura(time, item, target, eff);
                        break;
                    case ActivateEffects.ClearConditionEffectAura:
                        AEClearConditionEffectAura(time, item, target, eff);
                        break;
                    case ActivateEffects.Heal:
                        AEHeal(time, item, target, eff);
                        break;
                    case ActivateEffects.HealNova:
                        AEHealNova(time, item, target, eff);
                        break;
                    case ActivateEffects.Magic:
                        AEMagic(time, item, target, eff);
                        break;
                    case ActivateEffects.MagicNova:
                        AEMagicNova(time, item, target, eff);
                        break;
                    case ActivateEffects.Teleport:
                        AETeleport(time, item, target, eff);
                        break;
                    case ActivateEffects.SpawnUndead:
                        AESpawnUndead(time, item, target, eff);
                        break;
                    case ActivateEffects.Trap:
                        AETrap(time, item, target, eff);
                        break;
                    case ActivateEffects.StasisBlast:
                        StasisBlast(time, item, target, eff);
                        break;
                    case ActivateEffects.Decoy:
                        AEDecoy(time, item, target, eff);
                        break;
                    case ActivateEffects.Lightning:
                        AELightning(time, item, target, eff);
                        break;
                    case ActivateEffects.Vial:
                        AEVial(time, item, target, eff);
                        break;
                    case ActivateEffects.RemoveNegativeConditions:
                        AERemoveNegativeConditions(time, item, target, eff);
                        break;
                    case ActivateEffects.RemoveNegativeConditionsSelf:
                        AERemoveNegativeConditionSelf(time, item, target, eff);
                        break;
                    case ActivateEffects.FixedStat:
                        AEFixedStat(time, item, target, eff);
                        break;
                    case ActivateEffects.IncrementStat:
                        AEIncrementStat(time, item, target, eff);
                        break;
                    case ActivateEffects.Create:
                        AECreate(time, item, target, eff);
                        break;
                    case ActivateEffects.Dye:
                        AEDye(time, item, target, eff);
                        break;
                    case ActivateEffects.ShurikenAbility:
                        AEShurikenAbility(time, item, itemData, target, eff, clientTime);
                        break;
                    case ActivateEffects.Fame:
                        AEAddFame(time, item, target, eff);
                        break;
                    case ActivateEffects.Backpack:
                        AEBackpack(time, item, itemData, target, eff);
                        break;
                    case ActivateEffects.MiscBoosts:
                        AEMiscBoosts(time, item, itemData, target, eff);
                        break;
                    case ActivateEffects.UnlockPortal:
                        AEUnlockPortal(time, item, target, eff);
                        break;
                    case ActivateEffects.CreatePet:
                        AECreatePet(time, item, itemData, target, eff);
                        break;
                    case ActivateEffects.UnlockEmote:
                        AEUnlockEmote(time, item, itemData, eff);
                        break;
                    case ActivateEffects.UnlockSkin:
                        AEUnlockSkin(time, item, itemData, target, eff);
                        break;
                    case ActivateEffects.HealingGrenade:
                        AEHealingGrenade(time, item, target, eff);
                        break;
                    case ActivateEffects.Totem:
                        AETotem(time, item, target, eff);
                        break;
                    case ActivateEffects.Card:
                        AECard(time, item, target, eff);
                        break;
                    case ActivateEffects.Orb:
                        AEOrb(time, item, itemData, target, eff, clientTime);
                        break;
                    case ActivateEffects.Tome:
                        AETome(time, item, target, eff);
                        break;
                    default:
                        Log.Warn("Activate effect {0} not implemented.", eff.Effect);
                        break;
                }
            }
        }

        private bool HandleEffectsOnActivate(Item item, Position target, Random rnd, int clientTime, long lastUse)
        {
            if (HasConditionEffect(ConditionEffects.Suppressed))
            {
                return true;
            }

            // ability should never be null since it's being used
            // do pillar of flame before since weapons might activate
            // even though you didn't have enough mp/hp to activate the ability
            if (!HandlePillarOfFlame(item, clientTime, lastUse)) return false;

            for (var i = 0; i < 6; i++)
            {
                if (Inventory[i].Item == null)
                {
                    continue;
                }

                HandleRegularActivatePowers(target, rnd, i);
            }

            return true;
        }

        private void HandleRegularActivatePowers(Position target, Random rnd, int i)
        {
            switch (Inventory[i].Item.Power)
            {
                case "Vindictive Wraith":
                    if (rnd.NextDouble() <= 0.4)
                    {
                        SpawnAlly(target, "Ally Vengeful Spirit", 10.4f);
                    }

                    return;
                case "Intervention" when !OnCooldown(i):
                    if (rnd.NextDouble() <= 0.05)
                    {
                        HealNova(150, 4);
                        SetCooldown(i, 5);
                    }

                    return;
                case "Unholy Knowledge" when !OnCooldown(i):
                    StatBoostSelf(16, 3, 5);
                    SetCooldown(i, (float)_coolDown);
                    return;
                case "Arcane Refill" when !OnCooldown(i):
                    if (NextAttackMpRefill == 0)
                    {
                        NextAttackMpRefill = (int)(Stats[1] * ((double)Stats[7] / 2 / 100));
                    }

                    SetCooldown(i, (float)_coolDown);
                    return;
            }
        }

        private bool HandlePillarOfFlame(Item item, int clientTime, long lastUse)
        {
            if (Inventory[2].Item.Power == "Pillar of Flame")
            {
                _flamePillarState = Math.Min(3, _flamePillarState + 1);
                if (lastUse + _coolDown * 2 < clientTime)
                {
                    _flamePillarState = 0;
                }

                if (MP < item.MpCost + _flamePillarState * 20)
                {
                    Client.SendPacket(new InvResult { Result = 1 });
                    return false;
                }

                MP -= _flamePillarState * 20;
            }

            return true;
        }

        private void RefundItem(ItemData item, string message = "")
        {
            if (!string.IsNullOrWhiteSpace(message))
                SendError(message);

            var slot = Inventory.GetAvailableInventorySlot(item.Item);

            if (slot != -1)
            {
                Inventory[slot] = item;
                return;
            }

            Manager.Database.AddGift(Client.Account, item);
            SendError($"Your inventory is full, and your {item} has been sent to a gift chest.");
        }

        private void AECreatePet(RealmTime time, Item item, ItemData itemData, Position target, ActivateEffect eff)
        {
            if (Owner is not PetYard)
            {
                RefundItem(itemData, "Can only use this item in Pet Yard");
                return;
            }

            var client = Client;
            var resources = Manager.Resources;
            var petDatas = client.Account.PetDatas.ToList();
            var maxPets = client.Account.PetYardType * 10 > 50 ? 50 : client.Account.PetYardType * 10;
            if (petDatas.Count >= maxPets)
            {
                RefundItem(itemData, $"You cannot have more than {maxPets} pets");
            }

            // when player creates a character their pet data has Id as 0, so we don't want that
            if (Manager.Database.GetAccountHashField(client.Account, "nextPetId") == 0)
            {
                Manager.Database.IncrementHashField("account." + AccountId, "nextPetId");
            }

            if (!resources.GameData.IdToObjectType.TryGetValue(eff.ObjType, out var objType))
            {
                RefundItem(itemData, "Pet not found. Please contact staff to resolve this issue");
                return;
            }

            var petObjDesc = resources.GameData.ObjectDescs[objType];
            if (!petObjDesc.IsPet)
            {
                RefundItem(itemData, "Item does not spawn a pet. Please contact staff to resolve this issue");
                return;
            }

            // create pet data and add to account pets
            var petData = new PetData();
            Manager.Database.IncrementHashField("account." + AccountId, "nextPetId");
            petData.Id = Manager.Database.GetAccountHashField(client.Account, "nextPetId");
            petData.ObjectType = objType;
            petDatas.Add(petData);
            client.Account.PetDatas = petDatas.ToArray();
            client.Account.FlushAsync();
            SendInfo($"Successfully added {eff.ObjType} to your pets");
            // add pet to world
            var pet = Resolve(Manager, objType);
            pet.Move(X, Y);
            Owner.EnterWorld(pet);
            pet.PetData = petData;
        }

        private void AEUnlockEmote(RealmTime time, Item item, ItemData itemData, ActivateEffect eff)
        {
            if (Owner == null || Owner is Test)
            {
                SendInfo("Can't use emote unlockers in test worlds.");
                return;
            }

            var emotes = Client.Account.Emotes;
            if (!emotes.Contains(eff.Id))
            {
                emotes.Add(eff.Id);
                Client.Account.Emotes = emotes;
                Client.Account.FlushAsync();
                SendInfo($"{eff.Id} ({eff.Id}) Emote unlocked successfully");
                return;
            }

            RefundItem(itemData, "You already have this emote!");
        }

        private void AEUnlockSkin(RealmTime time, Item item, ItemData itemData, Position target, ActivateEffect eff)
        {
            if (Owner == null || Owner is Test)
            {
                SendInfo("Can't use skin unlockers in test worlds.");
                return;
            }

            if (Owner is not Vault)
            {
                RefundItem(itemData, "You can only use this item in your vault.");
                return;
            }

            var acc = Client.Account;
            var ownedSkins = acc.Skins.ToList();

            if (!ownedSkins.Contains(eff.SkinType))
            {
                ownedSkins.Add(eff.SkinType);
                acc.Skins = ownedSkins.ToArray();
                acc.FlushAsync();
                SendInfo("You've unlocked a new skin! Reconnect to reload changes.");
                return;
            }

            RefundItem(itemData, "You already have this skin!");
        }

        private void AEUnlockPortal(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var gameData = Manager.Resources.GameData;

            // find locked portal
            var portals = Owner.StaticObjects.Values
                .Where(s => s is Portal && s.ObjectDesc.ObjectId.Equals(eff.LockedName) && s.DistSqr(this) <= 9)
                .Select(s => s as Portal);
            if (!portals.Any())
                return;
            var portal = portals.Aggregate(
                (curmin, x) => (curmin == null || x.DistSqr(this) < curmin.DistSqr(this) ? x : curmin));
            if (portal == null)
                return;

            // get proto of world
            ProtoWorld proto;
            if (!Manager.Resources.Worlds.Data.TryGetValue(eff.DungeonName, out proto))
            {
                Log.Error("Unable to unlock portal. \"" + eff.DungeonName + "\" does not exist.");
                return;
            }

            if (proto.portals == null || proto.portals.Length < 1)
            {
                Log.Error("World is not associated with any portals.");
                return;
            }

            // create portal of unlocked world
            var portalType = (ushort)proto.portals[0];
            var uPortal = Resolve(Manager, portalType) as Portal;
            if (uPortal == null)
            {
                Log.Error("Error creating portal: {0}", portalType);
                return;
            }

            var portalDesc = gameData.Portals[portal.ObjectType];
            var uPortalDesc = gameData.Portals[portalType];

            // create world
            World world;
            if (proto.id < 0)
                world = Manager.GetWorld(proto.id);
            else
            {
                DynamicWorld.TryGetWorld(proto, Client, out world);
                world = Manager.AddWorld(world ?? new World(proto));
            }

            uPortal.WorldInstance = world;

            // swap portals
            if (!portalDesc.NexusPortal || !Manager.Monitor.RemovePortal(portal))
                Owner.LeaveWorld(portal);
            uPortal.Move(portal.X, portal.Y);
            uPortal.Name = uPortalDesc.DisplayId;
            var uPortalPos = new Position { X = portal.X - .5f, Y = portal.Y - .5f };
            if (!uPortalDesc.NexusPortal || !Manager.Monitor.AddPortal(world.Id, uPortal, uPortalPos))
                Owner.EnterWorld(uPortal);

            // setup timeout
            if (!uPortalDesc.NexusPortal)
            {
                var timeoutTime = gameData.Portals[portalType].Timeout;
                Owner.Timers.Add(new WorldTimer(timeoutTime * 1000, (w, _) => w.LeaveWorld(uPortal)));
            }

            // announce
            Owner.BroadcastPacket(new Notification
            {
                Color = new ARGB(0xFF00FF00),
                ObjectId = Id,
                Message = $"Unlocked by {Name}"
            }, null);
            foreach (var player in Owner.Players.Values)
                player.SendInfo($"{world.SBName} unlocked by {Name}");
        }

        /// <summary>
        /// Hold key to charge light points, press to shoot.
        /// </summary>
        /// <param name="manaDrain">mana drain/s (default: 10)</param>
        /// <param name="lightGain">light gain/s (default: 5)</param>
        /// <param name="shoots">true/false, self explanatory (default: true)</param>
        private void AEOrb(RealmTime time, Item item, ItemData itemData, Position target, ActivateEffect eff,
            long clientTime)
        {
            if (!_isDrainingMana)
            {
                _isDrainingMana = true;
                _manaDrain = eff.ManaDrain;
                _lightGain = eff.LightGain;
                return;
            }

            if (Light >= item.LightEndCost && eff.Shoots)
            {
                Light -= item.LightEndCost;
                AEShoot(time, item, itemData, target, eff, clientTime);
            }

            _isDrainingMana = false;
        }

        /// <summary>Use "types" to swap between types (shown below)</summary>
        /// <summary>DEF./MAIN: condEffs, chances, duration (condition effect)</summary>
        private void AECard(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var roll = new Random().Next(0, eff.MaxRoll);
            var effects = new List<string>(eff.ConditionEffects);
            var chance = new List<int>(eff.Chances).FindIndex(chance => chance >= roll);
            if (!Enum.TryParse(effects[chance], true, out ConditionEffectIndex effect))
            {
                // do something?
                return;
            }
            ApplyConditionEffect(effect, eff.DurationMS);
        }

        private void AEMiscBoosts(RealmTime time, Item item, ItemData itemData, Position target, ActivateEffect eff)
        {
            var id = eff.Id.ToLower();
            if (LTBoostTime + eff.DurationMS > 86400000 ||
                LDBoostTime + eff.DurationMS > 86400000 ||
                id == "xp" && (XPBoostTime + eff.DurationMS > 86400000 || Level >= 300))
            {
                RefundItem(itemData);
                return;
            }

            switch (id)
            {
                case "tier":
                    LTBoostTime += eff.DurationMS;
                    InvokeStatChange(StatsType.LTBoostTime, LTBoostTime / 1000, true);
                    return;
                case "drop":
                    LDBoostTime += eff.DurationMS;
                    InvokeStatChange(StatsType.LDBoostTime, LDBoostTime / 1000, true);
                    return;
                case "xp":
                    XPBoostTime += eff.DurationMS;
                    XPBoosted = true;
                    InvokeStatChange(StatsType.XPBoostTime, XPBoostTime / 1000, true);
                    return;
            }
        }

        private void AEBackpack(RealmTime time, Item item, ItemData itemData, Position target, ActivateEffect eff)
        {
            if (HasBackpack)
                RefundItem(itemData);

            HasBackpack = true;
        }

        private void AEAddFame(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (Owner is Test || Client.Account == null)
                return;

            var acc = Client.Account;
            var trans = Manager.Database.Conn.CreateTransaction();
            Manager.Database.UpdateCurrency(acc, eff.Amount, CurrencyType.Fame, trans)
                .ContinueWith(_ => { CurrentFame = acc.Fame; });
            trans.Execute(CommandFlags.FireAndForget);
        }

        private void AETotem(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            // prevent player from losing mp, might be needed if there's no item cooldown
            if (_isSpawned && eff.NoStack)
            {
                MP += item.MpCost;
                return;
            }

            var duration = eff.UseWisMod ? (int)(UseWisMod(eff.DurationSec) * 1000) : eff.DurationMS;
            duration += _flamePillarState * 1000;
            _isSpawned = true;
            for (var i = 0; i < eff.BoostValuesStats.Length; i++)
            {
                // Pillar of Flame scale
                var boost = ScaleFlamePillar(item, i);
                var idx = StatsManager.GetStatIndex((StatsType)eff.BoostValuesStats[i]);
                var amount = eff.UseWisMod ? (int)UseWisMod(eff.BoostValues[i], 0) : eff.BoostValues[i];
                amount += boost;

                Stats.Boost.ActivateBoost[idx].Push(amount, eff.NoStack);
                Stats.ReCalculateValues();

                // hack job to allow instant heal of NoStack boosts
                if (eff.NoStack && amount > 0 && idx == 0)
                {
                    HP = Math.Min(Stats[0], HP + amount);
                }

                Owner.Timers.Add(new WorldTimer(duration, (_, _) =>
                {
                    Stats.Boost.ActivateBoost[idx].Pop(amount, eff.NoStack);
                    Stats.ReCalculateValues();
                }));
            }

            var dat = Manager.Resources.GameData;
            ushort id;
            dat.IdToObjectType.TryGetValue(eff.TransformationSkin, out id);

            if (!_skinSet)
            {
                PrevSkin = Skin;
                PrevSize = Size;
                _skinSet = true;
            }
            SetDefaultSkin(id);
            SetDefaultSize(eff.TransformationSkinSize);
            Owner.Timers.Add(new WorldTimer(duration, (_, _) =>
            {
                SetDefaultSkin(PrevSkin);
                SetDefaultSize(PrevSize == 0 ? 100 : PrevSize);
                _skinSet = false;
                _isSpawned = false;
            }));
        }

        private int ScaleFlamePillar(Item item, int i)
        {
            if (item.Power != "Pillar of Flame")
            {
                return 0;
            }

            switch (i)
            {
                // Strength
                case 0:
                    return 5 * _flamePillarState;
                // Dexterity
                case 1:
                    return 3 * _flamePillarState;
                // Vitality
                case 2:
                    return -5 * _flamePillarState;
            }

            return 0;
        }

        private void AEShurikenAbility(RealmTime time, Item item, ItemData itemData, Position target,
            ActivateEffect eff, long clientTime)
        {
            if (!HasConditionEffect(ConditionEffects.NinjaSpeedy))
            {
                ApplyConditionEffect(ConditionEffectIndex.NinjaSpeedy);
                return;
            }

            if (MP >= item.MpEndCost)
            {
                MP -= item.MpEndCost;
                AEShoot(time, item, itemData, target, eff, clientTime);
            }

            ApplyConditionEffect(ConditionEffectIndex.NinjaSpeedy, 0);
        }

        private void AEDye(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (item.Texture1 != 0)
                Texture1 = item.Texture1;
            if (item.Texture2 != 0)
                Texture2 = item.Texture2;
        }

        private void AECreate(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var gameData = Manager.Resources.GameData;

            ushort objType;
            if (!gameData.IdToObjectType.TryGetValue(eff.Id, out objType) ||
                !gameData.Portals.ContainsKey(objType))
                return; // object not found, ignore

            var entity = Resolve(Manager, objType);
            var timeoutTime = gameData.Portals[objType].Timeout;

            entity.Move(X, Y);
            Owner.EnterWorld(entity);
            entity.AlwaysTick = true;

            ((Portal)entity).PlayerOpened = true;
            ((Portal)entity).Opener = Name;

            Owner.Timers.Add(new WorldTimer(timeoutTime * 1000, (world, _) => world.LeaveWorld(entity)));

            Owner.BroadcastPacket(new Notification
            {
                Color = new ARGB(0xFF00FF00),
                ObjectId = Id,
                Message = "Opened by " + Name
            }, null);
            foreach (var player in Owner.Players.Values)
                player.SendInfo($"{gameData.Portals[objType].DungeonName} opened by {Name}");
        }

        private void AEIncrementStat(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            var statInfo = Manager.Resources.GameData.Classes[ObjectType].Stats;
            string itemName = string.IsNullOrEmpty(item.DisplayName) ? item.ObjectId : item.DisplayName;

            Stats.Base[idx] += eff.Amount;
            if (Stats.Base[idx] < statInfo[idx].MaxValue)
                SendInfo(
                    $"{itemName} Consumed. {(statInfo[idx].MaxValue - Stats.Base[idx]) / eff.Amount} left to max.");
            else if (Stats.Base[idx] >= statInfo[idx].MaxValue)
                SendInfo($"{itemName} Consumed. You are now maxed on this stat.");

            if (Stats.Base[idx] > statInfo[idx].MaxValue)
            {
                Stats.Base[idx] = statInfo[idx].MaxValue;

                // disallow pot boosting
                // pot boosting
                /*
                var boostAmount = 1;
                if (idx == 0 || idx == 1)
                    boostAmount = 20;
                Stats.Boost.ActivateBoost[idx].AddOffset(boostAmount);
                Stats.ReCalculateValues();
                */
            }
        }

        private void AEFixedStat(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            Stats.Base[idx] = eff.Amount;
        }

        private void AERemoveNegativeConditionSelf(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            ApplyConditionEffect(NegativeEffs);
            BroadcastSync(new ShowEffect
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff),
                Pos1 = new Position { X = 1 }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AERemoveNegativeConditions(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            this.AOE(eff.Range, true, player => player.ApplyConditionEffect(NegativeEffs));
            BroadcastSync(new ShowEffect
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff),
                Pos1 = new Position { X = eff.Range }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEVial(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            BroadcastSync(new ShowEffect
            {
                EffectType = EffectType.Throw,
                Color = new ARGB(eff.Color),
                TargetObjectId = Id,
                Pos1 = target,
                Duration = eff.ThrowTime
            }, p => this.DistSqr(p) < RadiusSqr);

            var x = new Placeholder(Manager, (int)eff.ThrowTime);
            x.Move(target.X, target.Y);
            Owner.EnterWorld(x);
            Owner.Timers.Add(new WorldTimer((int)eff.ThrowTime, (world, _) =>
            {
                world.BroadcastPacketNearby(new ShowEffect
                {
                    EffectType = EffectType.AreaBlast,
                    Color = new ARGB(eff.Color),
                    TargetObjectId = x.Id,
                    Pos1 = new Position { X = eff.Radius }
                }, x, null);

                var enemies = new List<Enemy>();
                world.AOE(target, eff.Radius, false, entity => enemies.Add(entity as Enemy));

                if (enemies.Count > 0)
                    foreach (var enemy in enemies)
                    {
                        if (eff.ImpactDamage > 0)
                            enemy?.Damage(this, time, PoisonWismod(eff.ImpactDamage, eff.WismodMult), true, true);
                        PoisonEnemy(world, enemy, eff, time);
                    }
            }));
        }

        private void PoisonEnemy(World world, Enemy enemy, ActivateEffect eff, RealmTime time)
        {
            var remainingDmg = (int)StatsManager.GetDefenseDamage(enemy, PoisonWismod(eff.TotalDamage, eff.WismodMult), DamageTypes.True, this);
            var perDmg = remainingDmg * 1000 / eff.DurationMS;

            WorldTimer tmr = null;
            var x = 0;

            Func<World, RealmTime, bool> poisonTick = (w, t) =>
            {
                if (enemy.Owner == null || w == null)
                    return true;

                /*w.BroadcastPacketConditional(new ShowEffect()
                {
                    EffectType = EffectType.Dead,
                    TargetObjectId = enemy.Id,
                    Color = new ARGB(0xffddff00)
                }, p => enemy.DistSqr(p) < RadiusSqr);*/

                if (x % 4 == 0) // make sure to change this if timer delay is changed
                {
                    var thisDmg = perDmg;
                    if (remainingDmg < thisDmg)
                        thisDmg = remainingDmg;

                    enemy.Damage(this, t, thisDmg, true, true, eff.DamageType);
                    remainingDmg -= thisDmg;
                    if (remainingDmg <= 0)
                        return true;
                }

                x++;

                tmr.Reset();
                return false;
            };

            tmr = new WorldTimer(250, poisonTick);
            world.Timers.Add(tmr);
        }

        private void AELightning(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            const double coneRange = Math.PI / 4;
            var mouseAngle = Math.Atan2(target.Y - Y, target.X - X);

            // get starting target
            var startTarget = this.GetNearestEntity(MaxAbilityDist, false, e => 
                e is Enemy && !e.ObjectDesc.IsPet && e.GetPlayerOwner() == null
                && Math.Abs(mouseAngle - Math.Atan2(e.Y - Y, e.X - X)) <= coneRange);

            // no targets? bolt air animation
            if (startTarget == null)
            {
                var noTargets = new Packet[3];
                var angles = new[] { mouseAngle, mouseAngle - coneRange, mouseAngle + coneRange };
                for (var i = 0; i < 3; i++)
                {
                    var x = (int)(MaxAbilityDist * Math.Cos(angles[i])) + X;
                    var y = (int)(MaxAbilityDist * Math.Sin(angles[i])) + Y;
                    noTargets[i] = new ShowEffect
                    {
                        EffectType = EffectType.Trail,
                        TargetObjectId = Id,
                        Color = new ARGB(0xffff0088),
                        Pos1 = new Position
                        {
                            X = x,
                            Y = y
                        },
                        Pos2 = new Position { X = 350 }
                    };
                }

                BroadcastSync(noTargets, p => this.DistSqr(p) < RadiusSqr);
                return;
            }

            var current = startTarget;

            var targets = new Entity[(int)ScepterWismod(eff.MaxTargets, eff.WismodMult, wisPerTarget: eff.WisPerTarget,
                type: "targets")];
            var totalDmg = (int)ScepterWismod(eff.TotalDamage, eff.WismodMult, eff.WisDamageBase,
                type: "damage");

            var decreaseDmg = eff.DecreaseDamage;

            for (var i = 0; i < targets.Length; i++)
            {
                targets[i] = current;
                var next = current.GetNearestEntity(10, false, e =>
                {
                    if (e is not Enemy ||
                        e.ObjectDesc.IsPet || e.GetPlayerOwner() is not null ||
                        e.HasConditionEffect(ConditionEffects.Invincible) ||
                        e.HasConditionEffect(ConditionEffects.Stasis) ||
                        Array.IndexOf(targets, e) != -1)
                        return false;

                    return true;
                });

                if (next == null)
                    break;

                current = next;
            }

            var pkts = new List<Packet>();
            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                    break;

                var prev = i == 0 ? this : targets[i - 1];

                totalDmg -= decreaseDmg;

                (targets[i] as Enemy).Damage(this, time, totalDmg, false, true, eff.DamageType);

                if (eff.ConditionEffect != null)
                    targets[i].ApplyConditionEffect(new ConditionEffect
                    {
                        Effect = eff.ConditionEffect.Value,
                        DurationMS = (int)(eff.EffectDuration * 1000)
                    });

                pkts.Add(new ShowEffect
                {
                    EffectType = EffectType.Lightning,
                    TargetObjectId = prev.Id,
                    Color = new ARGB(0xffff0088),
                    Pos1 = new Position
                    {
                        X = targets[i].X,
                        Y = targets[i].Y
                    },
                    Pos2 = new Position { X = 350 }
                });
            }

            BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
        }

        private float ScepterWismod(float value, float multiplier, float wisDmgBase = 0, int wisPerTarget = 0,
            string type = "damage")
        {
            var wis = Stats.Base[7] + Stats.Boost[7];
            float wisRes = Math.Max(0, wis - 50);

            var wismodVal = Math.Round(wisDmgBase * wisRes * multiplier, 0);
            if (type.Equals("targets"))
            {
                wismodVal = Math.Round(1 * ((int)wisRes / wisPerTarget) * multiplier, 0);
            }

            return value + (float)wismodVal;
        }

        private void AEDecoy(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var decoy = new Decoy(this, eff.DurationMS, 4);
            decoy.Move(X, Y);
            Owner.EnterWorld(decoy);
        }

        private void StasisBlast(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var pkts = new List<Packet>
            {
                new ShowEffect
                {
                    EffectType = EffectType.Concentrate,
                    TargetObjectId = Id,
                    Pos1 = target,
                    Pos2 = new Position { X = target.X + 3, Y = target.Y },
                    Color = new ARGB(0xffffffff)
                }
            };

            Owner.AOE(target, 3, false, enemy =>
            {
                if (enemy.Immunities[(int)Immunity.StasisImmune] > 0)
                {
                    pkts.Add(new Notification
                    {
                        ObjectId = enemy.Id,
                        Color = new ARGB(0xff00ff00),
                        Message = "Immune"
                    });
                }
                else
                {
                    enemy.ApplyConditionEffect(ConditionEffectIndex.Stasis, eff.DurationMS);
                    Owner.Timers.Add(new WorldTimer(eff.DurationMS, (_, _) => enemy.ApplyImmunity(Immunity.StasisImmune, 3000)));
                    pkts.Add(new Notification
                    {
                        ObjectId = enemy.Id,
                        Color = new ARGB(0xffff0000),
                        Message = "Stasis"
                    });
                }
            });
            BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AETrap(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            BroadcastSync(new ShowEffect
            {
                EffectType = EffectType.Throw,
                Color = new ARGB(0xff9000ff),
                TargetObjectId = Id,
                Pos1 = target
            }, p => this.DistSqr(p) < RadiusSqr);

            Owner.Timers.Add(new WorldTimer(1500, (world, _) =>
            {
                var trap = new Trap(
                    this,
                    eff.Radius,
                    eff.TotalDamage,
                    eff.ConditionEffect ?? ConditionEffectIndex.Slow,
                    eff.EffectDuration);
                trap.Move(target.X, target.Y);
                world.EnterWorld(trap);
            }));
        }

        // eff.ObjType, eff.DurationMS, eff.MaxAmount
        private int _minionsSpawned;

        private void AESpawnUndead(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (Owner.Name == "Nexus" || _minionsSpawned >= eff.MaxAmount)
            {
                return;
            }

            Entity en = Resolve(Owner.Manager, eff.ObjType);
            var duration = SkullWismod(eff.DurationMS, eff.WismodMult);

            en.Move(target.X, target.Y);
            Owner.EnterWorld(en);
            _minionsSpawned += 1;
            en.SetPlayerOwner(this);
            en.SetPoTp(false);
            en.AlwaysTick = true;
            Owner.Timers.Add(new WorldTimer(duration, (w, _) =>
            {
                w.LeaveWorld(en);
                _minionsSpawned -= 1;
            }));
        }

        private int SkullWismod(int value, float multiplier)
        {
            var wis = Stats.Base[7] + Stats.Boost[7];
            var wisRes = Math.Max(0, wis - 50);

            var wismodDuration = Math.Round((double)wisRes / 5 * multiplier, 1);

            return value + ((int)wismodDuration * 1000);
        }

        private void AETeleport(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            TeleportPosition(time, target, true);
        }

        private void AEMagicNova(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (!HasConditionEffect(ConditionEffects.Stupefied))
            {
                var pkts = new List<Packet>();
                this.AOE(eff.Range, true, player =>
                    ActivateHealMp(player as Player, eff.Amount, pkts));
                pkts.Add(new ShowEffect
                {
                    EffectType = EffectType.AreaBlast,
                    TargetObjectId = Id,
                    Color = new ARGB(0xffffffff),
                    Pos1 = new Position { X = eff.Range }
                });
                BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
            }
        }

        private void AEMagic(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (!HasConditionEffect(ConditionEffects.Stupefied))
            {
                var pkts = new List<Packet>();
                ActivateHealMp(this, eff.Amount, pkts);
                BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
            }
        }

        private void AEHealNova(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (!HasConditionEffect(ConditionEffects.Sick))
            {
                var amount = eff.Amount;
                var range = eff.Range;
                if (eff.UseWisMod)
                {
                    amount = (int)UseWisMod(eff.Amount, 0);
                    range = UseWisMod(eff.Range);
                }

                var pkts = new List<Packet>();
                this.AOE(range, true, player =>
                {
                    if (!player.HasConditionEffect(ConditionEffects.Sick))
                        ActivateHealHp(player as Player, amount, pkts);
                });
                pkts.Add(new ShowEffect
                {
                    EffectType = EffectType.AreaBlast,
                    TargetObjectId = Id,
                    Color = new ARGB(0xffffffff),
                    Pos1 = new Position { X = range }
                });
                BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
            }
        }

        private void AETome(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (!HasConditionEffect(ConditionEffects.Sick))
            {
                var amount = (int)TomeWismod(eff.Amount, eff.WismodMult);
                var range = TomeWismod(eff.Range, eff.WismodMult, "range");

                var pkts = new List<Packet>();
                var players = new List<Player>();
                this.AOE(range, true, p => players.Add((Player)p));

                amount = (int)(amount * (1 - (eff.EffectiveLoss * (players.Count - 1)))) > (int)(amount * 0.3)
                    ? (int)(amount * (1 - (eff.EffectiveLoss * (players.Count - 1))))
                    : (int)(amount * 0.3);

                if (players.Count > 0)
                    foreach (var player in players)
                        if (!player.HasConditionEffect(ConditionEffects.Sick))
                            ActivateHealHp(player, amount, pkts);

                pkts.Add(new ShowEffect
                {
                    EffectType = EffectType.AreaBlast,
                    TargetObjectId = Id,
                    Color = new ARGB(0xffffffff),
                    Pos1 = new Position { X = range }
                });
                BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
            }
        }

        private float TomeWismod(float value, float multiplier, string type = "amount")
        {
            var wis = Stats.Base[7] + Stats.Boost[7];
            float wisRes = Math.Max(0, wis - 50);

            var wismodVal = Math.Round(30 * ((int)wisRes / 10) * multiplier, 0);
            if (type.Equals("range"))
            {
                wismodVal = Math.Round(0.1 * wisRes * multiplier, 1);
            }

            return value + (float)wismodVal;
        }

        private void AEHeal(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (!HasConditionEffect(ConditionEffects.Sick))
            {
                var pkts = new List<Packet>();
                ActivateHealHp(this, eff.Amount, pkts);
                BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
            }
        }

        private void AEConditionEffectAura(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var duration = eff.DurationMS;
            var range = eff.Range;
            if (eff.UseWisMod)
            {
                duration = (int)(UseWisMod(eff.DurationSec) * 1000);
                range = UseWisMod(eff.Range);
            }

            this.AOE(range, true, player =>
            {
                player.ApplyConditionEffect(new ConditionEffect
                {
                    Effect = eff.ConditionEffect.Value,
                    DurationMS = duration
                });
            });
            var color = 0xffffffff;
            if (eff.ConditionEffect.Value == ConditionEffectIndex.Brave)
                color = 0xffff0000;
            BroadcastSync(new ShowEffect
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(color),
                Pos1 = new Position { X = range }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEClearConditionEffectAura(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            this.AOE(eff.Range, true, player =>
            {
                var condition = eff.CheckExistingEffect;
                ConditionEffects conditions = 0;
                conditions |= (ConditionEffects)(1 << (Byte)condition.Value);
                if (!condition.HasValue || player.HasConditionEffect(conditions))
                {
                    player.ApplyConditionEffect(new ConditionEffect
                    {
                        Effect = eff.ConditionEffect.Value,
                        DurationMS = 0
                    });
                }
            });
        }

        private void AEConditionEffectSelf(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var duration = eff.DurationMS;
            if (eff.UseWisMod)
                duration = (int)(UseWisMod(eff.DurationSec) * 1000);

            ApplyConditionEffect(new ConditionEffect
            {
                Effect = eff.ConditionEffect.Value,
                DurationMS = duration
            });
            BroadcastSync(new ShowEffect
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff),
                Pos1 = new Position { X = 1 }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEStatBoostAura(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            var amount = eff.Amount;
            var duration = eff.DurationMS;
            var range = eff.Range;
            if (eff.UseWisMod)
            {
                amount = (int)UseWisMod(eff.Amount, 0);
                duration = (int)(UseWisMod(eff.DurationSec) * 1000);
                range = UseWisMod(eff.Range);
            }

            this.AOE(range, true, player =>
            {
                ((Player)player).Stats.Boost.ActivateBoost[idx].Push(amount, eff.NoStack);
                ((Player)player).Stats.ReCalculateValues();

                if (idx == 12)
                    ShieldDamage = ShieldDamage - eff.Amount >= 0 ? ShieldDamage - eff.Amount : 0;

                // hack job to allow instant heal of nostack boosts
                if (eff.NoStack && amount > 0 && idx == 0)
                {
                    ((Player)player).HP = Math.Min(((Player)player).Stats[0], ((Player)player).HP + amount);
                }

                Owner.Timers.Add(new WorldTimer(duration, (_, _) =>
                {
                    ((Player)player).Stats.Boost.ActivateBoost[idx].Pop(amount, eff.NoStack);
                    ((Player)player).Stats.ReCalculateValues();
                }));
            });

            if (!eff.NoStack)
                BroadcastSync(new ShowEffect
                {
                    EffectType = EffectType.AreaBlast,
                    TargetObjectId = Id,
                    Color = new ARGB(0xffffffff),
                    Pos1 = new Position { X = range }
                }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEStatBoostSelf(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            var s = eff.Amount;
            Stats.Boost.ActivateBoost[idx].Push(s, eff.NoStack);
            if (idx == 12)
                ShieldDamage = ShieldDamage - eff.Amount >= 0 ? ShieldDamage - eff.Amount : 0;
            Stats.ReCalculateValues();
            Owner.Timers.Add(new WorldTimer(eff.DurationMS, (_, _) =>
            {
                Stats.Boost.ActivateBoost[idx].Pop(s, eff.NoStack);
                Stats.ReCalculateValues();
            }));
            BroadcastSync(new ShowEffect
            {
                EffectType = EffectType.Potion,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff)
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEShoot(RealmTime time, Item item, ItemData itemData, Position target, ActivateEffect eff,
            long clientTime)
        {
            var arcGap = item.ArcGap * Math.PI / 180;
            var startAngle = Math.Atan2(target.Y - Y, target.X - X) - (item.NumProjectiles - 1) / 2 * arcGap;
            var prjDesc = item.Projectiles[0]; //Assume only one

            var sPkts = new Packet[item.NumProjectiles];
            for (var i = 0; i < item.NumProjectiles; i++)
            {
                var proj = CreateProjectile(prjDesc, item.ObjectType,
                    (int)(Stats.GetAttackDamage(prjDesc, true) * itemData.Quality),
                    clientTime, new Position { X = X, Y = Y }, (float)(startAngle + arcGap * i), i);
                if (HasConditionEffect(ConditionEffects.Blind))
                    proj.Damage = 0;
                Owner.EnterWorld(proj);
                FameCounter.Shoot(proj);
                sPkts[i] = new AllyShoot
                {
                    OwnerId = Id,
                    Angle = proj.Angle,
                    ContainerType = item.ObjectType,
                    BulletId = proj.BulletId
                };
            }

            BroadcastSync(sPkts, p => p != this && this.DistSqr(p) < RadiusSqr);
        }

        private void AEBulletNova(RealmTime time, Item item, ItemData itemData, Position target, ActivateEffect eff, int cTime)
        {
            var prjs = new Projectile[eff.NumShots];
            var prjDesc = item.Projectiles[0]; //Assume only one
            var bulletIds = new byte[eff.NumShots];
            var damages = new short[eff.NumShots];
            var damageTypes = new DamageTypes[eff.NumShots];
            for (var i = 0; i < eff.NumShots; i++)
            {
                var proj = CreateProjectile(prjDesc, item.ObjectType,
                    (int)(Random.Next(prjDesc.MinDamage, prjDesc.MaxDamage) * itemData.Quality),
                    time.TotalElapsedMs, target, (float)(i * (Math.PI * 2) / eff.NumShots), i);
                if (HasConditionEffect(ConditionEffects.Blind))
                    proj.Damage = 0;
                Owner.EnterWorld(proj);
                FameCounter.Shoot(proj);
                prjs[i] = proj;
                bulletIds[i] = proj.BulletId;
                damages[i] = (short)proj.Damage;
                damageTypes[i] = proj.DamageType;
            }

            AwaitShootAck(time.TotalElapsedMs, bulletIds);
            var batch = new Packet[]
            {
                new ServerPlayerShoot
                {
                    OwnerId = Id,
                    ContainerType = item.ObjectType,
                    BulletIds = bulletIds,
                    Damages = damages,
                    DamageTypes = damageTypes,
                    StartingPos = target
                },
                new ShowEffect
                {
                    EffectType = EffectType.Trail,
                    Pos1 = target,
                    TargetObjectId = Id,
                    Color = new ARGB(0xFFFF00AA)
                }
            };

            foreach (var plr in Owner.Players.Values
                .Where(p => p.DistSqr(this) < RadiusSqr))
            {
                plr.Client.SendPackets(batch);
            }
        }

        private void AEGenericActivate(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var targetPlayer = eff.Target.Equals("player");
            var centerPlayer = eff.Center.Equals("player");
            var duration = (eff.UseWisMod) ? (int)(UseWisMod(eff.DurationSec) * 1000) : eff.DurationMS;
            var range = (eff.UseWisMod)
                ? UseWisMod(eff.Range)
                : eff.Range;

            if (eff.ConditionEffect != null)
                Owner.AOE((eff.Center.Equals("mouse")) ? target : new Position { X = X, Y = Y }, range, targetPlayer,
                    entity =>
                    {
                        if (!entity.HasConditionEffect(ConditionEffects.Stasis) &&
                            !entity.HasConditionEffect(ConditionEffects.Invincible))
                        {
                            entity.ApplyConditionEffect(new ConditionEffect
                            {
                                Effect = eff.ConditionEffect.Value,
                                DurationMS = duration
                            });
                        }
                    });

            BroadcastSync(new ShowEffect
            {
                EffectType = (EffectType)eff.VisualEffect,
                TargetObjectId = Id,
                Color = new ARGB(eff.Color),
                Pos1 = (centerPlayer) ? new Position { X = range } : target,
                Pos2 = new Position { X = target.X - range, Y = target.Y }
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void AEHealingGrenade(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            BroadcastSync(new ShowEffect
            {
                EffectType = EffectType.Throw,
                Color = new ARGB(0xffddff00),
                TargetObjectId = Id,
                Pos1 = target
            }, p => this.DistSqr(p) < RadiusSqr);

            var x = new Placeholder(Manager, 1500);
            x.Move(target.X, target.Y);
            Owner.EnterWorld(x);
            Owner.Timers.Add(new WorldTimer(1500, (world, _) =>
            {
                world.BroadcastPacketNearby(new ShowEffect
                {
                    EffectType = EffectType.AreaBlast,
                    Color = new ARGB(0xffddff00),
                    TargetObjectId = x.Id,
                    Pos1 = new Position { X = eff.Radius }
                }, x, null);

                world.AOE(target, eff.Radius, true,
                    player => HealingPlayersPoison(world, player as Player, eff));
            }));
        }

        private static void ActivateHealHp(Player player, int amount, List<Packet> pkts)
        {
            var maxHp = player.Stats[0];
            var newHp = Math.Min(maxHp, player.HP + amount);
            if (newHp == player.HP)
                return;

            pkts.Add(new ShowEffect
            {
                EffectType = EffectType.Potion,
                TargetObjectId = player.Id,
                Color = new ARGB(0xffffffff)
            });
            pkts.Add(new Notification
            {
                Color = new ARGB(0xff00ff00),
                ObjectId = player.Id,
                Message = "+" + (newHp - player.HP)
            });

            player.HP = newHp;
        }

        private static void ActivateHealMp(Player player, int amount, List<Packet> pkts)
        {
            var maxMp = player.Stats[1];
            var newMp = Math.Min(maxMp, player.MP + amount);
            if (newMp == player.MP)
                return;

            pkts.Add(new ShowEffect
            {
                EffectType = EffectType.Potion,
                TargetObjectId = player.Id,
                Color = new ARGB(0xffffffff)
            });
            pkts.Add(new Notification
            {
                Color = new ARGB(0xff9000ff),
                ObjectId = player.Id,
                Message = "+" + (newMp - player.MP)
            });

            player.MP = newMp;
        }

        private void HealingPlayersPoison(World world, Player player, ActivateEffect eff)
        {
            var remainingHeal = eff.TotalDamage;
            var perHeal = eff.TotalDamage * 1000 / eff.DurationMS;

            WorldTimer tmr = null;
            var x = 0;

            Func<World, RealmTime, bool> healTick = (w, _) =>
            {
                if (player.Owner == null || w == null)
                    return true;

                if (x % 4 == 0) // make sure to change this if timer delay is changed
                {
                    var thisHeal = perHeal;
                    if (remainingHeal < thisHeal)
                        thisHeal = remainingHeal;

                    List<Packet> pkts = new List<Packet>();

                    ActivateHealHp(player, thisHeal, pkts);
                    w.BroadcastPackets(pkts, null);
                    remainingHeal -= thisHeal;
                    if (remainingHeal <= 0)
                        return true;
                }

                x++;

                tmr.Reset();
                return false;
            };

            tmr = new WorldTimer(250, healTick);
            world.Timers.Add(tmr);
        }

        private int PoisonWismod(int value, float multiplier)
        {
            var wis = Stats.Base[7] + Stats.Boost[7];
            var wisRes = (double)Math.Max(0, wis - 50);

            var wismodDmg = Math.Round((double)value / 10 * (wisRes / 3) * multiplier, 0);

            return value + (int)wismodDmg;
        }

        private float UseWisMod(float value, int offset = 1)
        {
            double totalInt = Stats.Base[7] + Stats.Boost[7];

            if (totalInt < 30)
                return value;

            double m = (value < 0) ? -1 : 1;
            double n = (value * totalInt / 150) + (value * m);
            n = Math.Floor(n * Math.Pow(10, offset)) / Math.Pow(10, offset);
            if (n - (int)n * m >= 1 / Math.Pow(10, offset) * m)
            {
                return ((int)(n * 10)) / 10.0f;
            }

            return (int)n;
        }

        private bool _isSpawned;

        private void SpawnAlly(Position target, string ally, float duration, bool teleportToOwner = true,
            bool forceSpawn = false)
        {
            if (Owner.Name == "Nexus" && !forceSpawn || _isSpawned)
            {
                return;
            }

            var en = Resolve(Owner.Manager, ally);

            en.Move(target.X, target.Y);
            Owner.EnterWorld(en);
            _isSpawned = true;
            en.SetPlayerOwner(this);
            en.SetPoTp(teleportToOwner);
            en.AlwaysTick = true;
            Owner.Timers.Add(new WorldTimer((int)(duration * 1000), (world, _) =>
            {
                world.LeaveWorld(en);
                _isSpawned = false;
            }));
        }

        private void HealNova(int amount, float range)
        {
            if (!HasConditionEffect(ConditionEffects.Sick))
            {
                var pkts = new List<Packet>();
                this.AOE(range, true, player =>
                {
                    if (!player.HasConditionEffect(ConditionEffects.Sick))
                        ActivateHealHp(player as Player, amount, pkts);
                });
                pkts.Add(new ShowEffect
                {
                    EffectType = EffectType.AreaBlast,
                    TargetObjectId = Id,
                    Color = new ARGB(0xffffffff),
                    Pos1 = new Position { X = range }
                });
                BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
            }
        }

        private bool _godlyVigorFirst = true;

        private void HealSelf(int amount, bool isGodlyVigor = false)
        {
            if (isGodlyVigor && _godlyVigorFirst)
            {
                _godlyVigorFirst = false;
                return;
            }

            if (!HasConditionEffect(ConditionEffects.Sick))
            {
                var pkts = new List<Packet>();
                ActivateHealHp(this, amount, pkts);
                BroadcastSync(pkts, p => this.DistSqr(p) < RadiusSqr);
            }
        }

        private void StatBoostSelf(int stat, int amount, float durationSec, bool noStack = false)
        {
            Stats.Boost.ActivateBoost[stat].Push(amount, noStack);
            if (stat == 12)
                ShieldDamage = ShieldDamage - amount >= 0 ? ShieldDamage - amount : 0;
            Stats.ReCalculateValues();
            Owner.Timers.Add(new WorldTimer((int)(durationSec * 1000), (_, _) =>
            {
                Stats.Boost.ActivateBoost[stat].Pop(amount, noStack);
                Stats.ReCalculateValues();
            }));
            BroadcastSync(new ShowEffect
            {
                EffectType = EffectType.Potion,
                TargetObjectId = Id,
                Color = new ARGB(0xffffffff)
            }, p => this.DistSqr(p) < RadiusSqr);
        }

        private void DamageBlast(int damage, float range, Position pos, RealmTime time)
        {
            Owner.BroadcastPacketNearby(new ShowEffect
            {
                EffectType = EffectType.AreaBlast,
                TargetObjectId = Id,
                Color = new ARGB(0xCD4D3B),
                Pos1 = new Position { X = range }
            }, this, null);

            var enemies = new List<Enemy>();
            Owner.AOE(pos, range, false, entity => enemies.Add(entity as Enemy));

            if (enemies.Count > 0)
                foreach (var enemy in enemies)
                    enemy?.Damage(this, time, damage, true);
        }
    }
}