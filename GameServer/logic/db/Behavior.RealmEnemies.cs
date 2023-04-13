using common;
using GameServer.logic.behaviors;
using GameServer.logic.loot;
using GameServer.logic.transitions;

namespace GameServer.logic
{
    partial class BehaviorDb
    {
        private _ RealmEnemies = () => Behav()
            .Init("Pirate",
                new State(
                    new State("WanderShoot",
                        new Prioritize(
                        new Charge(8.5, range: 1, coolDown: 0),
                        new Wander(speed: 1)
                    ),
                        new Shoot(radius: 4, projectileIndex: 0, coolDown: 2500),
                        new DamageTakenTransition(damage: 20, targetState: "Shoot2", fromMaxHp: false)

                    ),
                    new State("Shoot2",
                        new Shoot(radius: 6, projectileIndex: 1, coolDown: 3250),
                        new TimedTransition(time: 10000, targetState: "WanderShoot"),
                        new Wander(speed: 1)
                    )
                ),
                new TierLoot(1, ItemType.Weapon, 0.02),
                new ItemLoot("Health Potion", 0.03)
        )
            .Init("Piratess",
                new State(
                    new State("WanderShoot",
                        new Prioritize(
                        new Charge(8.5, range: 1, coolDown: 0),
                        new Wander(speed: 1)
                    ),
                        new Shoot(radius: 4, projectileIndex: 0, coolDown: 2500),
                        new DamageTakenTransition(damage: 20, targetState: "Shoot2", fromMaxHp: false)

                    ),
                    new State("Shoot2",
                        new Shoot(radius: 6, projectileIndex: 1, coolDown: 3250),
                        new TimedTransition(time: 10000, targetState: "WanderShoot"),
                        new Wander(speed: 1)
                    )
                ),
                new TierLoot(1, ItemType.Weapon, 0.02),
                new ItemLoot("Health Potion", 0.03)
        )
            .Init("Golden Scorpion",
                new State(
                    new State("WanderSpawnAlly",
                        new Wander(speed: 1),
                        new Reproduce(children: "Scorpion Warrior", coolDown: 10000, densityMax: 5),
                        new Reproduce(densityMax: 2, densityRadius: 6),
                        new Spawn(children: "Scorpion Warrior", maxChildren: 5, initialSpawn: 0.6, coolDown: 2500, givesNoXp: true),
                        new Shoot(radius: 5, projectileIndex: 0, coolDown: 2750)
                    )
                ),
                new GoldDrop(min: 3, max: 10, probability: 1)
            )
            .Init("Scorpion Warrior",
                new State(
                    new State("ProtectTheQueen",
                        new Prioritize(
                            new Protect(speed: 1, protectee: "Golden Scorpion", acquireRange: 6, protectionRange: 4),
                            new Wander(speed: 1)
                            ),
                        new Shoot(radius: 4, projectileIndex: 0, coolDown: 1750)
                        )
                    )
            )

            .Init("Bandit Leader",
                new State(
                    new State("BozoSpotted",
                        new Spawn("Bandit Enemy", coolDown: 8000, maxChildren: 4),
                        new State("CatchBozo",
                            new State("warn_about_grenades",
                                new Taunt(probability: 0.15, text: "Catch!"),
                                new TimedTransition(time: 400, targetState: "wimpy_grenade1")
                            ),
                            new State("wimpy_grenade1",
                                new Grenade(radius: 1.4, damage: 12, coolDown: 10000),
                                new Prioritize(
                                    new StayAbove(speed: 1, altitude: 7),
                                    new Wander(speed: 1)
                                ),
                                new TimedTransition(time: 2000, targetState: "wimpy_grenade2")
                            ),
                            new State("wimpy_grenade2",
                                new Grenade(radius: 1.4, damage: 12, coolDown: 10000),
                                new Prioritize(
                                    new StayAbove(speed: 1.3, altitude: 7),
                                    new Wander(speed: 1.3)
                                ),
                                new TimedTransition(time: 3000, targetState: "slow_follow")
                            ),
                            new State("slow_follow",
                                new Shoot(radius: 6, projectileIndex: 0, coolDown: 1000),
                                new Prioritize(
                                    new StayAbove(speed: 1.6, altitude: 7),
                                    new Charge(speed: 1, range: 3, coolDown: 4000),
                                    new Wander(speed: 1)
                                ),
                                new TimedTransition(time: 4000, targetState: "warn_about_grenades")
                            ),
                            new HpLessTransition(threshold: 0.45, targetState: "meek")
                        ),
                        new State("meek",
                            new Taunt(probability: 0.5, text: "Forget this... run for it!"),
                            new StayBack(speed: 1.3, distance: 6),
                            new Shoot(radius: 6, projectileIndex: 2, coolDown: 2000),
                            new Order(range: 10, children: "Bandit Enemy", targetState: "escape"),
                            new TimedTransition(time: 12000, targetState: "CatchBozo")
                        )
                    )
                ),
                new TierLoot(1, ItemType.Weapon, 0.02),
                new TierLoot(2, ItemType.Weapon, 0.03),
                new TierLoot(1, ItemType.Armor, 0.02),
                new TierLoot(2, ItemType.Armor, 0.03),
                new ItemLoot("Health Potion", 0.12),
                new ItemLoot("Magic Potion", 0.14)
            )

            .Init("Bandit Enemy",
                new State(
                    new State("fast_follow",
                        new Shoot(radius: 3, projectileIndex: 0, coolDown: 2000),
                        new Prioritize(
                            new Protect(speed: 1.5, protectee: "Bandit Leader", acquireRange: 9, protectionRange: 6, reprotectRange: 3),
                            new Charge(speed: 1, range: 3),
                            new Wander(speed: 1)
                            ),
                            new TimedTransition(time: 3000, targetState: "scatter1")
                        ),
                        new State("scatter1",
                            new Prioritize(
                                new Protect(speed: 1.5, protectee: "Bandit Leader", acquireRange: 9, protectionRange: 6, reprotectRange: 3),
                                new Wander(speed: 2.15),
                                new Wander(speed: 1.5)
                                ),
                                new TimedTransition(time: 2000, targetState: "slow_follow")
                            ),
                        new State ("slow_follow",
                            new Shoot(radius: 4.5, coolDown: 1000),
                            new Prioritize(
                                new Protect(speed: 1.5, protectee: "Bandit Leader", acquireRange: 9, protectionRange: 6, reprotectRange: 3),
                                new Charge(speed: 1.5, range: 3, coolDown: 2000),
                                new Wander(speed: 1)
                                ),
                                new TimedTransition(time: 3000, targetState: "scatter2")
                            ),
                        new State ("scatter2",
                            new Prioritize(
                                new Protect(speed: 1.5, protectee: "Bandit Leader", acquireRange: 9, protectionRange: 6, reprotectRange: 3),
                                new Wander(speed: 2.15),
                                new Wander(speed: 1.5)
                                ),
                                new TimedTransition(time: 2000, targetState: "fast_follow")
                            ),
                        new State("escape",
                            new StayBack(speed: 1.3, distance: 6),
                            new TimedTransition(time:15000, targetState: "fast_follow")
                            )
                )
        );
  
    }
}