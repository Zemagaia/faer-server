using Shared.resources;

namespace GameServer.realm.entities.player
{
    partial class Player
    {
        private AbilityDesc[] Abilities = new AbilityDesc[4];
        private int[] LastAbilityUseTime = new int[4];

        private void LoadAbilities()
        {
            var playerDesc = Manager.Resources.GameData.Classes[ObjectType];
            for (var i = 0; i < 4; i++)
                Abilities[i] = playerDesc.Abilities[i];
        }

        public bool TryUseAbility(int time, AbilitySlotType abilitySlotType, byte[] data)
        {
            Console.WriteLine("Time: " + time + " AbilitySlotType: " + abilitySlotType + " Data Length: " + data.Length);

            var index = (int)abilitySlotType; 
            if (index < 0 || index >= Abilities.Length) // safety checks
                return false;

            if (!CanUseAbility(time, index))
                return false;

            var ability = Abilities[index];
            MP -= ability.ManaCost;
            HP -= ability.HealthCost;
            LastAbilityUseTime[index] = time;
            UseAbility(data, index);
            return true;
        }

        private bool CanUseAbility(int time, int index) {
            
            var ability = Abilities[index];
            if (MP < ability.ManaCost || HP < ability.HealthCost - 1)
                return false;
            
            var delta = time - LastAbilityUseTime[index];
            return delta >= ability.CooldownMS;
        }

        private void UseAbility(byte[] data, int index)
        {
            using var rdr = new BinaryReader(new MemoryStream(data));

            var ability = Abilities[index];

            switch (ability.AbilityType)
            {
                case AbilityType.AnomalousBurst:
                    DoAnomalousBurst(rdr);
                    break;
                case AbilityType.ParadoxicalShift:
                    DoParadoxicalShift(rdr);
                    break;
                case AbilityType.Swarm:
                    DoSwarm(rdr);
                    break;
                case AbilityType.Possession:
                    DoPossession(rdr);
                    break;
            }
        }

        private void DoAnomalousBurst(BinaryReader rdr) {
            var angle = rdr.ReadSingle();
            Console.WriteLine($"DoAnomalousBurst: {angle}");
        }

        private void DoParadoxicalShift(BinaryReader rdr)
        {
            Console.WriteLine($"DoAnomalousBurst: No Data");
        }

        private void DoSwarm(BinaryReader rdr)
        {
            Console.WriteLine($"Swarm: No Data");
        }

        private void DoPossession(BinaryReader rdr)
        {
            var objectId = rdr.ReadInt32();
            Console.WriteLine($"DoPossession: {objectId}");
        }
    }
}
