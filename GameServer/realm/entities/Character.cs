using common;
using GameServer.realm.entities.player;

namespace GameServer.realm.entities
{
    public abstract class Character : Entity
    {
        public Random Random = new();

        private readonly SV<int> _hp;
        private readonly SV<int> _maximumHP;

        public int HP
        {
            get => _hp.GetValue();
            set => _hp.SetValue(value);
        }

        public int MaximumHP
        {
            get => _maximumHP.GetValue();
            set => _maximumHP.SetValue(value);
        }

        protected Character(RealmManager manager, ushort objType)
            : base(manager, objType)
        {
            _hp = new SV<int>(this, StatsType.HP, 0);
            _maximumHP = new SV<int>(this, StatsType.MaximumHP, 0);

            if (ObjectDesc != null)
            {
                if (ObjectDesc.SizeStep != 0)
                {
                    var step = Random.Next(0, (ObjectDesc.MaxSize - ObjectDesc.MinSize) / ObjectDesc.SizeStep + 1) *
                               ObjectDesc.SizeStep;
                    SetDefaultSize(ObjectDesc.MinSize + step);
                }
                else
                    SetDefaultSize(ObjectDesc.MinSize);

                SetConditions();

                HP = ObjectDesc.MaxHP;
                MaximumHP = HP;
            }
        }

        private void SetConditions()
        {
            if (ObjectDesc.SlowImmune)
                ApplyImmunity(Immunity.SlowImmune, -1);
            if (ObjectDesc.StunImmune)
                ApplyImmunity(Immunity.StunImmune, -1);
            if (ObjectDesc.UnarmoredImmune)
                ApplyImmunity(Immunity.UnarmoredImmune, -1);
            if (ObjectDesc.StasisImmune)
                ApplyImmunity(Immunity.StasisImmune, -1);
            if (ObjectDesc.ParalyzeImmune)
                ApplyImmunity(Immunity.ParalyzeImmune, -1);
            if (ObjectDesc.CurseImmune)
                ApplyImmunity(Immunity.CurseImmune, -1);
            if (ObjectDesc.PetrifyImmune)
                ApplyImmunity(Immunity.PetrifyImmune, -1);
            if (ObjectDesc.CrippledImmune)
                ApplyImmunity(Immunity.CrippledImmune, -1);

            if (ObjectDesc.Invincible)
                ApplyConditionEffect(ConditionEffectIndex.Invincible);
            if (ObjectDesc.Invulnerable)
                ApplyConditionEffect(ConditionEffectIndex.Invulnerable);
        }

        protected override void ImportStats(StatsType stats, object val)
        {
            if (stats == StatsType.HP) HP = (int)val;
            else if (stats == StatsType.MaximumHP) MaximumHP = (int)val;
            base.ImportStats(stats, val);
        }

        protected override void ExportStats(IDictionary<StatsType, object> stats)
        {
            stats[StatsType.HP] = HP;
            if (this is not Player)
                stats[StatsType.MaximumHP] = MaximumHP;
            base.ExportStats(stats);
        }
    }
}