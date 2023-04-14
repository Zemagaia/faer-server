using common;
using GameServer.realm.entities.player;
using GameServer.realm.worlds;
using StackExchange.Redis;

namespace GameServer.realm
{
    public class ConnectManager {
        private RealmManager _manager;

	public ConnectManager(RealmManager manager)
	{
		_manager = manager;
	}

	public int GetPlayerCount()
	{
		return _manager.Clients.Count;
	}

	public static void Reconnect(Client client, int gameId)
	{
		World world = client.Manager.GetWorld(gameId);
		if (world == null || world.Deleted)
		{
			client.SendText("*Error*", 0, 0, "", "World does not exist.");
			world = client.Manager.GetWorld(-2);
		}
		if (world.IsLimbo)
		{
			world = world.GetInstance(client);
		}
		if (!world.AllowedAccess(client))
		{
			client.SendText("*Error*", 0, 0, "", "Access denied");
			if (gameId == -2)
			{
				client.Disconnect();
				return;
			}
			world = client.Manager.GetWorld(-2);
		}
		int mapSize = Math.Max(world.Map.Width, world.Map.Height);
		client.SendMapInfo(mapSize, mapSize, world.Name, world.SBName, world.Difficulty, world.Background, world.AllowTeleport, world.ShowDisplays);
		client.SendAccountList(0, client.Account.LockList);
		client.SendAccountList(1, client.Account.IgnoreList);
		if (client.Character != null)
		{
			if (client.Character.Dead)
			{
				client.SendFailure("Character is dead");
				return;
			}
			client.Player.Owner.LeaveWorld(client.Player);
			client.Player.DisposeUpdate();
			client.SendCreateSuccess(client.Manager.Worlds[world.Id].EnterWorld(client.Player), client.Character.CharId);
			client.Manager.Clients[client].WorldInstance = client.Player.Owner.Id;
			client.Manager.Clients[client].WorldName = client.Player.Owner.Name;
		}
		else
		{
			client.SendFailure("Failed to load character");
		}
	}

	public static void Connect(Client client, int gameId, int charId)
	{
		if (gameId != -6)
		{
			gameId = -2;
		}
		if (!client.Manager.Database.AcquireLock(client.Account))
		{
			IEnumerable<Client> otherClients = client.Manager.Clients.Keys.Where((Client c) => c == client || (c.Account != null && c.Account.AccountId == client.Account.AccountId));
			foreach (Client otherClient in otherClients)
			{
				otherClient.Disconnect();
			}
			if (!client.Manager.Database.AcquireLock(client.Account))
			{
				client.SendFailure("Account locked (" + client.Manager.Database.GetLockTime(client.Account)?.ToString("%s") + " seconds until timeout)");
				return;
			}
		}
		((RedisObject)client.Account).Reload((string)null);
		if (!client.Manager.TryConnect(client))
		{
			client.SendFailure("Failed to connect");
			return;
		}
		World world = client.Manager.GetWorld(gameId);
		if (world == null || world.Deleted)
		{
			client.SendText("*Error*", 0, 0, "", "World does not exist.");
			world = client.Manager.GetWorld(-2);
		}
		if (world.IsLimbo)
		{
			world = world.GetInstance(client);
		}
		if (!world.AllowedAccess(client))
		{
			client.SendText("*Error*", 0, 0, "", "Access denied");
			if (gameId == -2)
			{
				client.Disconnect();
				return;
			}
			world = client.Manager.GetWorld(-2);
		}
		client.Account.RefreshLastSeen();
		((RedisObject)client.Account).FlushAsync((ITransaction)null);
		int mapSize = Math.Max(world.Map.Width, world.Map.Height);
		client.SendMapInfo(mapSize, mapSize, world.Name, world.SBName, world.Difficulty, world.Background, world.AllowTeleport, world.ShowDisplays);
		client.SendAccountList(0, client.Account.LockList);
		client.SendAccountList(1, client.Account.IgnoreList);
		client.Character = client.Manager.Database.LoadCharacter(client.Account, charId);
		if (client.Character != null)
		{
			if (client.Character.Dead)
			{
				client.SendFailure("Character is dead");
				return;
			}
			client.Player = new Player(client);
			client.SendCreateSuccess(client.Manager.Worlds[world.Id].EnterWorld(client.Player), client.Character.CharId);
			client.Manager.Clients[client].WorldInstance = client.Player.Owner.Id;
			client.Manager.Clients[client].WorldName = client.Player.Owner.Name;
		}
		else
		{
			client.SendFailure("Failed to load character");
		}
	}
    }
}