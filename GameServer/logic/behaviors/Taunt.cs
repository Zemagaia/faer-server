﻿using System.Xml.Linq;
using Shared;
using GameServer.realm;
using GameServer.realm.entities;
using GameServer.realm.entities.player;

namespace GameServer.logic.behaviors; 

internal class Taunt : Behavior
{
    //State storage: time

    private float probability = 1;
    private bool broadcast = false;
    private Cooldown cooldown = new Cooldown(0, 0);
    private string[] text;
    private int? ordered;
    
    public Taunt(XElement e)
    {
        text = e.ParseStringArray("@text", '|', new[] { e.ParseString("@text") });
        probability = e.ParseFloat("@probability", 1);
        broadcast = e.ParseBool("@broadcast");
        cooldown = new Cooldown(e.ParseInt("@cooldown"), 0);
    }

    public Taunt(params string[] text)
    {
        this.text = text;
    }

    public Taunt(double probability, params string[] text)
    {
        this.text = text;
        this.probability = (float)probability;
    }
    public Taunt(bool broadcast, params string[] text)
    {
        this.text = text;
        this.broadcast = broadcast;
    }
    public Taunt(Cooldown cooldown, params string[] text)
    {
        this.text = text;
        this.cooldown = cooldown;
    }
        
    /*public Taunt(Cooldown cooldown, int ordered, params string[] text)
    { // ordered made to be int due to conflicts with other constructors
      // pretty hackish but will have to do for now.
      // 0 means false
      // non 0 means true
        this.text = text;
        this.cooldown = cooldown;
        if (ordered != 0)
            this.ordered = 0;
    }*/

    public Taunt(double probability, bool broadcast, params string[] text)
    {
        this.text = text;
        this.probability = (float)probability;
        this.broadcast = broadcast;
    }
    public Taunt(double probability, Cooldown cooldown, params string[] text)
    {
        this.text = text;
        this.probability = (float)probability;
        this.cooldown = cooldown;
    }
    public Taunt(bool broadcast, Cooldown cooldown, params string[] text)
    {
        this.text = text;
        this.broadcast = broadcast;
        this.cooldown = cooldown;
    }

    public Taunt(double probability, bool broadcast, Cooldown cooldown, params string[] text)
    {
        this.text = text;
        this.probability = (float)probability;
        this.broadcast = broadcast;
        this.cooldown = cooldown;
    }

    protected override void OnStateEntry(Entity host, RealmTime time, ref object state)
    {
        state = null;
    }

    protected override void TickCore(Entity host, RealmTime time, ref object state) {
        if (state != null && cooldown.CoolDown == 0) return; //cooldown = 0 -> once per state entry

        int c;
        if (state == null) c = cooldown.Next(Random);
        else c = (int) state;

        c -= time.ElapsedMsDelta;
        state = c;
        if (c > 0) return;

        c = cooldown.Next(Random);
        state = c;

        if (Random.NextDouble() >= probability) return;

        string taunt;
        if (ordered != null) {
            taunt = text[ordered.Value];
            ordered = (ordered.Value + 1) % text.Length;
        }
        else
            taunt = text[Random.Next(text.Length)];

        if (taunt.Contains("{PLAYER}")) {
            var player = host.GetNearestEntity(10, null);
            if (player == null) return;
            taunt = taunt.Replace("{PLAYER}", player.Name);
        }

        taunt = taunt.Replace("{HP}", (host as Enemy)?.HP.ToString());

        if (broadcast) {
            foreach (var p in host.Owner.Players.Values)
                if (p != host)
                    p.Client.SendText(host.ObjectDesc.DisplayId ?? host.ObjectDesc.ObjectId, host.Id, 3, "",
                        taunt, 0xF2963A, 0xF2963A);
        } else
            foreach (var i in host.Owner.PlayersCollision.HitTest(host.X, host.Y, 15).Where(e => e is Player))
                if (i is Player player && host.Dist(player) < 15)
                    player.Client.SendText(host.ObjectDesc.DisplayId ?? host.ObjectDesc.ObjectId, host.Id, 3, "",
                        taunt, 0xF2963A, 0xF2963A);
    }
}