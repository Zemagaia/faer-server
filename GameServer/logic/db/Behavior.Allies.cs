using GameServer.logic.behaviors;

namespace GameServer.logic
{
    partial class BehaviorDb
    {
        private _ Allies = () => Behav()

            // Tiered item Minions
            .Init("Weak Skeleton",
                new State(
                    new Wander(0.2),
                    new StayCloseToOwner(0.5, 4),
                    new State("Attack",
                        new EnemyAoe(2.5, 40, 90, players: false, color: 0xffa500,
                            coolDown: 2500) // orange to differ with enemy aoe
                    )
                )
            )
            .Init("Aged Skeleton",
                new State(
                    new Wander(0.2),
                    new StayCloseToOwner(0.5, 4),
                    new State("Attack",
                        new EnemyAoe(2.8, 55, 110, players: false, color: 0xffa500, coolDown: 2500)
                    )
                )
            )
            .Init("Juvenile Skeleton",
                new State(
                    new Wander(0.2),
                    new StayCloseToOwner(0.5, 4),
                    new State("Attack",
                        new EnemyAoe(2.8, 65, 130, players: false, color: 0xffa500, coolDown: 2000)
                    )
                )
            )
            .Init("Strong Skeleton",
                new State(
                    new Wander(0.2),
                    new StayCloseToOwner(0.5, 4),
                    new State("Attack",
                        new EnemyAoe(2.8, 65, 130, players: false, color: 0xffa500, coolDown: 1800)
                    )
                )
            )
            .Init("Brute Skeleton",
                new State(
                    new Wander(0.2),
                    new StayCloseToOwner(0.5, 4),
                    new State("Attack",
                        new EnemyAoe(3.5, 110, 110, true, players: false, color: 0xffa500, coolDown: 1400)
                    )
                )
            )

            // Undead Lair Minions
            .Init("Sealed Spirit",
                new State(
                    new Wander(0.4),
                    new StayCloseToOwner(0.4),
                    new State("Attack",
                        new EnemyAoe(3.5, 150, 200, players: false, color: 0x87ceeb, coolDown: 1200)
                    )
                )
            )
            .Init("Ally Vengeful Spirit",
                new State(
                    new Wander(0.4),
                    new StayCloseToOwner(0.4),
                    new State("Attack",
                        new EnemyAoe(3, 100, 200, true, players: false, color: 0x186c8e, coolDown: 400)
                    )
                )
            );
    }
}