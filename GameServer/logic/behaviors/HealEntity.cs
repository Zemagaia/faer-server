using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;

namespace GameServer.logic.behaviors; 

internal class HealEntity : Behavior
{
    //State storage: cooldown timer

    private readonly double _range;
    private readonly string _name;
    private Cooldown _coolDown;
    private readonly int? _amount;

    public HealEntity(XElement e)
    {
        _range = e.ParseFloat("@range");
        _name = e.ParseString("@name");
        _amount = e.ParseNInt("@amount");            
        _coolDown = new Cooldown().Normalize(e.ParseInt("@cooldown", 1000));
    }
        
    public HealEntity(double range, string name = null, int? healAmount = null, Cooldown coolDown = new Cooldown())
    {
        _range = (float)range;
        _name = name;
        _coolDown = coolDown.Normalize();
        _amount = healAmount;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
    {
        state = 0;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state)
    {
        var cool = (int)state;

        if (cool <= 0)
        {
            foreach (var entity in host.GetNearestEntitiesByName(_range, _name).OfType<Enemy>())
            {
                var newHp = entity.ObjectDesc.MaxHP;
                if (_amount != null)
                {
                    var newHealth = (int)_amount + entity.HP;
                    if (newHp > newHealth)
                        newHp = newHealth;
                }
                if (newHp != entity.HP)
                {
                    var n = newHp - entity.HP;
                    entity.HP = newHp;
                    foreach (var p in host.Owner.Players.Values)
                        if (MathUtils.DistSqr(p.X, p.Y, host.X, host.Y) < 16 * 16)
                            p.Client.SendShowEffect(EffectType.Potion, entity.Id, 0, 0, 0, 0, 0xFFFFFFFF);
                    foreach (var p in host.Owner.Players.Values)
                        if (MathUtils.DistSqr(p.X, p.Y, host.X, host.Y) < 16 * 16)
                            p.Client.SendShowEffect(EffectType.Trail, host.Id, entity.X, entity.Y, 0, 0, 0xFFFFFFFF);
                    foreach (var p in host.Owner.Players.Values)
                        if (MathUtils.DistSqr(p.X, p.Y, host.X, host.Y) < 16 * 16)
                            p.Client.SendNotification(entity.Id, "+" + n, 0xFF00FF00);
                }
            }
            cool = _coolDown.Next(Random);
        }
        else
            cool -= time.ElapsedMsDelta;

        state = cool;
    }
}