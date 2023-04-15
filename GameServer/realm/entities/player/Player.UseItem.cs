using Shared;
using Shared.resources;
using GameServer.realm.worlds;
using GameServer.realm.worlds.logic;
using wServer.realm;

namespace GameServer.realm.entities.player
{
    partial class Player
    {
        public const int MaxAbilityDist = 14;

        public static readonly ConditionEffect[] NegativeEffs = {
            new()
            {
                Effect = ConditionEffectIndex.Slowed,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Weak,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Bleeding,
                DurationMS = 0
            },
            new()
            {
                Effect = ConditionEffectIndex.Sick,
                DurationMS = 0
            }
        };

        private readonly object _useLock = new();
        private bool _skinSet;
        private int _flamePillarState;

        public void UseItem(int objId, int slot, float x, float y)
        {
            lock (_useLock)
            {
                var entity = Owner.GetEntity(objId);
                if (entity == null)
                {
                    Client.SendInvResult(1);
                    return;
                }

                if (entity is Player && objId != Id)
                {
                    Client.SendInvResult(1);
                    return;
                }

                var container = entity as IContainer;

                // eheh no more clearing BBQ loot bags
                if (this.Dist(entity) > 3)
                {
                    Client.SendInvResult(1);
                    return;
                }

                var cInv = container?.Inventory.CreateTransaction();

                // get item
                Item item = null;
                foreach (var stack in Stacks.Where(stack => stack.Slot == slot))
                {
                    item = stack.Pull();

                    if (item == null)
                        return;
                    break;
                }

                if (item == null)
                {
                    if (container == null)
                        return;

                    item = cInv[slot];
                }

                if (item == null)
                    return;

                // make sure not trading and trying to consume item
                if (tradeTarget != null && item.Consumable)
                    return;

                // use item
                var slotType = 10;
                if (slot < cInv.Length)
                {
                    slotType = container.SlotTypes[slot];

                    if (item.Consumable)
                    {
                        var db = Manager.Database;
                        
                        var trans = db.Conn.CreateTransaction();
                        var task = trans.ExecuteAsync();
                        task.ContinueWith(t =>
                        {
                            var success = !t.IsCanceled && t.Result;
                            if (!success || !Inventory.Execute(cInv)) // can result in the loss of an item if inv trans fails...
                            {
                                entity.ForceUpdate(slot);
                                return;
                            }

                            Activate(Manager.Logic.WorldTime, item, new Position {X=x, Y=y}, 0, 0, slot == 1);
                        });
                        task.ContinueWith(e =>
                                Log.Error(e.Exception.InnerException.ToString()),
                            TaskContinuationOptions.OnlyOnFaulted);
                        return;
                    }
                }

                if (item.Consumable || item.SlotType == slotType)
                    Activate(Manager.Logic.WorldTime, item, new Position {X=x, Y=y}, 0, 0, slot == 1);
                else
                    Client.SendInvResult(1);
            }
        }

        private long _clientNextUse;
        private long _clientNextUse2;
        private double _coolDown;
        public int NextAttackMpRefill;

        private void Activate(RealmTime time, Item item, Position target, byte activateId,
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
                    case ActivateEffects.FixedStat:
                        AEFixedStat(time, item, target, eff);
                        break;
                    case ActivateEffects.IncrementStat:
                        AEIncrementStat(time, item, target, eff);
                        break;
                    case ActivateEffects.Create:
                        AECreate(time, item, target, eff);
                        break;
                    case ActivateEffects.Backpack:
                        AEBackpack(time, item, target, eff);
                        break;
                    case ActivateEffects.UnlockPortal:
                        AEUnlockPortal(time, item, target, eff);
                        break;
                    case ActivateEffects.UnlockEmote:
                        AEUnlockEmote(time, item, eff);
                        break;
                    case ActivateEffects.UnlockSkin:
                        AEUnlockSkin(time, item, target, eff);
                        break;
                    case ActivateEffects.Totem:
                        AETotem(time, item, target, eff);
                        break;
                    default:
                        Log.Warn("Activate effect {0} not implemented.", eff.Effect);
                        break;
                }
            }
        }

        private bool HandleEffectsOnActivate(Item item, Position target, Random rnd, int clientTime, long lastUse)
        {
            // ability should never be null since it's being used
            // do pillar of flame before since weapons might activate
            // even though you didn't have enough mp/hp to activate the ability
            if (!HandlePillarOfFlame(item, clientTime, lastUse)) return false;

            for (var i = 0; i < 6; i++)
            {
                if (Inventory[i] == null)
                {
                    continue;
                }

                HandleRegularActivatePowers(target, rnd, i);
            }

            return true;
        }

        private void HandleRegularActivatePowers(Position target, Random rnd, int i)
        {
            switch (Inventory[i].Power)
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
            if (Inventory[2].Power == "Pillar of Flame")
            {
                _flamePillarState = Math.Min(3, _flamePillarState + 1);
                if (lastUse + _coolDown * 2 < clientTime)
                {
                    _flamePillarState = 0;
                }

                if (MP < item.MpCost + _flamePillarState * 20)
                {
                    Client.SendInvResult(1);
                    return false;
                }

                MP -= _flamePillarState * 20;
            }

            return true;
        }

        private void RefundItem(Item item, string message = "")
        {
            if (!string.IsNullOrWhiteSpace(message))
                SendError(message);

            var slot = Inventory.GetAvailableInventorySlot(item);

            if (slot != -1)
            {
                Inventory[slot] = item;
                return;
            }

            //todo
            //Manager.Database.AddGift(Client.Account, item);
            SendError($"Your inventory is full, and your {item} has been sent to a gift chest.");
        }


        private void AEUnlockEmote(RealmTime time, Item item, ActivateEffect eff)
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

            RefundItem(item, "You already have this emote!");
        }

        private void AEUnlockSkin(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (Owner == null || Owner is Test)
            {
                SendInfo("Can't use skin unlockers in test worlds.");
                return;
            }

            if (Owner is not Vault)
            {
                RefundItem(item, "You can only use this item in your vault.");
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

            RefundItem(item, "You already have this skin!");
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
            foreach (var p in Owner.Players.Values)
            {
                p.Client.SendNotification(Id, $"Unlocked by {Name}", 0xFF00FF00);
            }
            foreach (var player in Owner.Players.Values)
                player.SendInfo($"{world.SBName} unlocked by {Name}");
        }

        private void AEBackpack(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (HasBackpack)
                RefundItem(item);

            HasBackpack = true;
        }

        private void AETotem(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var duration = eff.UseWisMod ? (int)(eff.DurationSec * 1000) : eff.DurationMS;
            duration += _flamePillarState * 1000;
            for (var i = 0; i < eff.BoostValuesStats.Length; i++)
            {
                // Pillar of Flame scale
                var boost = 0;//ScaleFlamePillar(item, i);
                var idx = StatsManager.GetStatIndex((StatsType)eff.BoostValuesStats[i]);
                var amount = eff.BoostValues[i];
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
            SetDefaultSize((ushort) eff.TransformationSkinSize);
            Owner.Timers.Add(new WorldTimer(duration, (_, _) =>
            {
                SetDefaultSkin((ushort) PrevSkin);
                SetDefaultSize((ushort) (PrevSize == 0 ? 100 : PrevSize));
                _skinSet = false;
            }));
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

            foreach (var p in Owner.Players.Values)
            {
                p.Client.SendNotification(Id, $"Opened by" + Name, 0xFF00FF00);
            }
            foreach (var player in Owner.Players.Values)
                player.SendInfo($"{gameData.Portals[objType].DungeonName} opened by {Name}");
        }

        private void AEIncrementStat(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            var statInfo = Manager.Resources.GameData.Classes[ObjectType].Stats;
            string itemName = string.IsNullOrEmpty(item.DisplayName) ? item.ObjectId : item.DisplayName;

            Stats.Base[idx] += (int)eff.Amount;
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
            Stats.Base[idx] = (int)eff.Amount;
        }

        private void AETeleport(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            TeleportPosition(time, target, true);
        }

        private void AEMagicNova(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            this.AOE(eff.Range, true, player =>
                    ActivateHealMp(player as Player, (int)eff.Amount));
                foreach (var p in Owner.Players.Values)
                    if (MathUtils.DistSqr(p.X, Y, X, Y) < RadiusSqr) 
                        p.Client.SendShowEffect(EffectType.AreaBlast, Id, eff.Range, eff.Range, 0 ,0, 0xFFFFFFFF);
        }

        private void AEMagic(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            ActivateHealMp(this, (int)eff.Amount);
        }

        private void AEHealNova(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (!HasConditionEffect(ConditionEffects.Sick))
            {
                var amount = eff.Amount;
                var range = eff.Range;

                this.AOE(range, true, player =>
                {
                    if (!player.HasConditionEffect(ConditionEffects.Sick))
                        ActivateHealHp(player as Player, (int)amount);
                });
                
                foreach (var p in Owner.Players.Values)
                    if (MathUtils.DistSqr(p.X, Y, X, Y) < RadiusSqr) 
                        p.Client.SendShowEffect(EffectType.AreaBlast, Id, eff.Range, eff.Range, 0 ,0, 0xFFFFFFFF);
            }
        }

        private void AEHeal(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            if (!HasConditionEffect(ConditionEffects.Sick))
            {
                ActivateHealHp(this, (int)eff.Amount);
            }
        }

        private void AEConditionEffectAura(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var duration = eff.DurationMS;
            var range = eff.Range;

            this.AOE(range, true, player =>
            {
                player.ApplyConditionEffect(new ConditionEffect()
                {
                    Effect = eff.ConditionEffect.Value,
                    DurationMS = duration
                });
            });
            foreach (var p in Owner.Players.Values)
            {
                if (MathUtils.DistSqr(p.X, Y, X, Y) < RadiusSqr) 
                    p.Client.SendShowEffect(EffectType.AreaBlast, Id, eff.Range, eff.Range, 0 ,0, 0xFFFFFFFF);
            }
        }
        
        private void AEConditionEffectSelf(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var duration = eff.DurationMS;

            ApplyConditionEffect(new ConditionEffect
            {
                Effect = eff.ConditionEffect.Value,
                DurationMS = duration
            });
            foreach (var p in Owner.Players.Values)
            {
                if (MathUtils.DistSqr(p.X, Y, X, Y) < RadiusSqr) 
                    p.Client.SendShowEffect(EffectType.AreaBlast, Id, eff.Range, eff.Range, 0 ,0, 0xFFFFFFFF);
            }
        }

        private void AEStatBoostAura(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            var amount = (int)eff.Amount;
            var duration = eff.DurationMS;
            var range = eff.Range;

            this.AOE(range, true, player =>
            {
                ((Player)player).Stats.Boost.ActivateBoost[idx].Push(amount, eff.NoStack);
                ((Player)player).Stats.ReCalculateValues();

                if (idx == 12)
                    ShieldDamage = ShieldDamage - amount >= 0 ? ShieldDamage - amount : 0;

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
                foreach (var p in Owner.Players.Values)
                {
                    if (MathUtils.DistSqr(p.X, Y, X, Y) < RadiusSqr) 
                        p.Client.SendShowEffect(EffectType.AreaBlast, Id, eff.Range, eff.Range, 0 ,0, 0xFFFFFFFF);
                }
        }

        private void AEStatBoostSelf(RealmTime time, Item item, Position target, ActivateEffect eff)
        {
            var idx = StatsManager.GetStatIndex((StatsType)eff.Stats);
            var s = (int)eff.Amount;
            Stats.Boost.ActivateBoost[idx].Push(s, eff.NoStack);
            if (idx == 12)
                ShieldDamage = ShieldDamage - s >= 0 ? ShieldDamage - s : 0;
            Stats.ReCalculateValues();
            Owner.Timers.Add(new WorldTimer(eff.DurationMS, (_, _) =>
            {
                Stats.Boost.ActivateBoost[idx].Pop(s, eff.NoStack);
                Stats.ReCalculateValues();
            }));
            foreach (var p in Owner.Players.Values)
                if (MathUtils.DistSqr(p.X, Y, X, Y) < RadiusSqr) 
                    p.Client.SendShowEffect(EffectType.Potion, Id, 0, 0, 0, 0, 0xFFFFFFFF);
        }
        
        private void ActivateHealHp(Player player, int amount)
        {
            var maxHp = player.Stats[0];
            var newHp = Math.Min(maxHp, player.HP + amount);
            if (newHp == player.HP)
                return;

            foreach (var p in Owner.Players.Values)
                if (MathUtils.DistSqr(p.X, p.Y, X, Y) < RadiusSqr) {
                    p.Client.SendShowEffect(EffectType.Potion, Id, 0, 0, 0, 0, 0xFFFFFF);
                    p.Client.SendNotification(player.Id, "+" + (newHp - player.HP), 0x00FF00);
                }

            player.HP = newHp;
        }
        
        private void ActivateHealMp(Player player, int amount) {
            var maxMp = player.Stats[1];
            var newMp = Math.Min(maxMp, player.MP + amount);
            if (newMp == player.MP)
                return;
            
            foreach (var p in Owner.Players.Values)
                if (MathUtils.DistSqr(p.X, p.Y, X, Y) < RadiusSqr) {
                    p.Client.SendShowEffect(EffectType.Potion, Id, 0, 0, 0, 0, 0xFFFFFF);
                    p.Client.SendNotification(player.Id, "+" + (newMp - player.MP), 0x9000FF);
                }

            player.MP = newMp;
        }
        
        private void SpawnAlly(Position target, string ally, float duration, bool teleportToOwner = true,
            bool forceSpawn = false)
        {
            if (Owner.Name == "Nexus" && !forceSpawn)
            {
                return;
            }

            var en = Resolve(Owner.Manager, ally);

            en.Move(target.X, target.Y);
            Owner.EnterWorld(en);
            en.SetPlayerOwner(this);
            en.SetPoTp(teleportToOwner);
            en.AlwaysTick = true;
            Owner.Timers.Add(new WorldTimer((int)(duration * 1000), (world, _) =>
            {
                world.LeaveWorld(en);
            }));
        }

        private void HealNova(int amount, float range)
        {
            if (!HasConditionEffect(ConditionEffects.Sick))
            {
                this.AOE(range, true, player =>
                {
                    if (!player.HasConditionEffect(ConditionEffects.Sick))
                        ActivateHealHp(player as Player, amount);
                });
                foreach (var p in Owner.Players.Values)
                    if (MathUtils.DistSqr(p.X, Y, X, Y) < RadiusSqr) 
                        p.Client.SendShowEffect(EffectType.AreaBlast, Id, range, range, 0 ,0, 0xCD4D3B);
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
                ActivateHealHp(this, amount);
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
            foreach (var p in Owner.Players.Values)
            {
                if (MathUtils.DistSqr(p.X, Y, X, Y) < RadiusSqr) 
                    p.Client.SendShowEffect(EffectType.Potion, Id, 0, 0, 0, 0, 0xFFFFFFFF);
            }
        }

        private void DamageBlast(int damage, float range, Position pos, RealmTime time)
        {
            foreach (var p in Owner.Players.Values)
            {
                if (MathUtils.DistSqr(p.X, Y, X, Y) < RadiusSqr) 
                    p.Client.SendShowEffect(EffectType.AreaBlast, Id, range, range, 0 ,0, 0xCD4D3B);
            }

            var enemies = new List<Enemy>();
            Owner.AOE(pos, range, false, entity => enemies.Add(entity as Enemy));

            if (enemies.Count > 0)
                foreach (var enemy in enemies)
                    enemy?.Damage(this, time, damage, true);
        }
    }
}
