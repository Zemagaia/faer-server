using common;
using GameServer.logic.behaviors;
using GameServer.realm.worlds.logic;

namespace GameServer.realm
{
    public partial class Entity
    {
        private void PetTick(RealmTime time)
        {
            // to explain this: players and their pets share PetData so it will not be null
            // and pets spawned from pet yard also have their own PetData, so it won't be null
            if (PetData is null)
            {
                return;
            }
            
            // follow owner if pet is active
            if (HasOwner())
            {
                FollowPlayer(time);
                return;
            }

            // wander if in pet yard
            if (Owner is PetYard)
            {
                Wander(time);
            }
        }

        private bool HasOwner()
        {
            return _playerOwner != null && _playerOwner.PetData.Id == PetData.Id && _playerOwner.SpectateTarget == null;
        }

        private void FollowPlayer(RealmTime time)
        {
            float dist;
            var vect = new Vector2(_playerOwner.X - X, _playerOwner.Y - Y);
            if (this.Dist(_playerOwner) > 20)
            {
                Move(_playerOwner.X, _playerOwner.Y);
            }
            else if (vect.Length() > 1)
            {
                dist = this.GetSpeed(0.3f) * (time.ElapsedMsDelta / 1000f);
                if (vect.Length() > 3)
                    dist = this.GetSpeed(0.5f + (float)_playerOwner.Stats[4] / 100) * (time.ElapsedMsDelta / 1000f);

                vect.Normalize();
                ValidateAndMove(X + vect.X * dist, Y + vect.Y * dist);
            }
        }

        private void Wander(RealmTime time)
        {
            float dist;
            var random = MathUtils.Random;
            var storage = new Wander.WanderStorage();
            if (storage.RemainingDistance <= 0)
            {
                storage.Direction = new Vector2(random.Next() % 2 == 0 ? -1 : 1, random.Next() % 2 == 0 ? -1 : 1);
                storage.Direction.Normalize();
                storage.RemainingDistance = 600 / 1000f;
            }

            dist = this.GetSpeed(0.1f) * (time.ElapsedMsDelta / 1000f);
            ValidateAndMove(X + storage.Direction.X * dist, Y + storage.Direction.Y * dist);

            storage.RemainingDistance -= dist;
        }
    }
}