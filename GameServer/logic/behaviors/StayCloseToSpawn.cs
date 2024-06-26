﻿using System.Xml.Linq;
using Shared;
using GameServer.realm;

namespace GameServer.logic.behaviors; 

internal class StayCloseToSpawn : CycleBehavior
{
    //State storage: target position
    //assume spawn=state entry position

    private float speed;
    private int range;
        
    public StayCloseToSpawn(XElement e)
    {
        speed = e.ParseFloat("@speed");
        range = e.ParseInt("@range", 5);
    }
        
    public StayCloseToSpawn(double speed, int range = 5)
    {
        this.speed = (float)speed;
        this.range = range;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
    {
        state = new Vector2(host.X, host.Y);
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state)
    {
        Status = CycleStatus.NotStarted;

        if (!(state is Vector2))
        {
            state = new Vector2(host.X, host.Y);
            Status = CycleStatus.Completed;
            return;
        }

        var vect = (Vector2) state;
        if ((vect - new Vector2(host.X, host.Y)).Length() > range)
        {
            vect -= new Vector2(host.X, host.Y);
            vect.Normalize();
            var dist = host.GetSpeed(speed) * (time.ElapsedMsDelta / 1000f);
            host.ValidateAndMove(host.X + vect.X * dist, host.Y + vect.Y * dist);

            Status = CycleStatus.InProgress;
        }
        else
            Status = CycleStatus.Completed;
    }
}