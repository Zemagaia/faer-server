using System.Diagnostics;
using NLog;

namespace GameServer.realm; 

public class LogicTicker {
	private static Logger Log = LogManager.GetCurrentClassLogger();
	private RealmManager _manager;
	private ManualResetEvent _mre;
	public RealmTime WorldTime;
	public int MillisPerTick;
       
	public LogicTicker(RealmManager manager) {
		_manager = manager;
		MillisPerTick = 1000 / manager.TPS;
		_mre = new ManualResetEvent(initialState: false);
		WorldTime = default;
	}
       
	public void TickLoop() {
		Log.Info("Logic loop started.");
		var loopTime = 0;
		var t = default(RealmTime);
		var watch = Stopwatch.StartNew();
		while (true) {
			t.TotalElapsedMs = watch.ElapsedMilliseconds;
			t.TickDelta = loopTime / MillisPerTick;
			t.TickCount += t.TickDelta;
			t.ElapsedMsDelta = t.TickDelta * MillisPerTick;
			if (_manager.Terminating)
				break;
       			
			_manager.Monitor.Tick(t);
			_manager.InterServer.Tick(t.ElapsedMsDelta);
			WorldTime.TickDelta += t.TickDelta;
			foreach (var w in _manager.Worlds.Values)
				w.Tick(t);
       			
			t.TickDelta = WorldTime.TickDelta;
			t.ElapsedMsDelta = t.TickDelta * MillisPerTick;
			WorldTime.TickDelta = 0;
			var logicTime = (int)(watch.ElapsedMilliseconds - t.TotalElapsedMs);
			_mre.WaitOne(Math.Max(0, MillisPerTick - logicTime));
			loopTime += (int)(watch.ElapsedMilliseconds - t.TotalElapsedMs) - t.ElapsedMsDelta;
		}
            
		Log.Info("Logic loop stopped.");
	}
}