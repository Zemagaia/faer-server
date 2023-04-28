/*using System.Diagnostics;
using NLog;

namespace GameServer.realm; 

// tick system with variable ticks

public class LogicTicker { 
	private static Logger Log = LogManager.GetCurrentClassLogger();
	private RealmManager _manager;
	public RealmTime WorldTime { get; private set; }
	public int MillisecondsPerTick;
       
	public LogicTicker(RealmManager manager) {
		_manager = manager;
		MillisecondsPerTick = 1000 / manager.TPS;
		WorldTime = new RealmTime();
	}
       
	public void TickLoop() {
		Log.Info("Logic loop started.");
		var loopTime = 0;
		
		var watch = Stopwatch.StartNew();

		var lastMS = 0L;

		var t = new RealmTime();
		while (!_manager.Terminating)
		{
			var currentMS = t.TotalElapsedMs = watch.ElapsedMilliseconds;

			var delta = (int)(currentMS - lastMS);
			if (delta >= MillisecondsPerTick)
			{
				t.TickCount++;
				t.ElapsedMsDelta = delta;

				var start = watch.ElapsedMilliseconds;

				try
				{
					_manager.Monitor.Tick(t);
					_manager.InterServer.Tick(t.ElapsedMsDelta);
					foreach (var w in _manager.Worlds.Values)
						w.Tick(t);
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
				}
				
				var end = watch.ElapsedMilliseconds;
				var logicExecutionTime = (int)(end - start);

				lastMS = currentMS + logicExecutionTime; // logic update time added ontop to offset the latency of each tick to help with stability

				WorldTime = t;
			}
		}
		
		Log.Info("Logic loop stopped.");
	}
}
*/

 // this is old logicticker code
 
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