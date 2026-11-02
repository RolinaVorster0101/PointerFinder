namespace PointerFinder;

/// <summary>
/// Detects a "shake": several quick direction reversals along either axis within a short window.
/// A reversal only counts once the pointer has travelled at least MinTravel px in the new direction,
/// so jitter and slow drifting never trigger it.
/// </summary>
sealed class ShakeDetector
{
    const int WindowMs = 800;

    // index = sensitivity (0 low, 1 medium, 2 high): min travel per swing in px, reversals needed
    static readonly (int MinTravel, int Reversals)[] Presets = { (45, 5), (30, 4), (20, 3) };

    readonly Axis _x = new();
    readonly Axis _y = new();

    public int Sensitivity { get; set; } = 1;

    /// <summary>Feed every mouse position. Returns true once when a shake is recognised.</summary>
    public bool Feed(int x, int y, long nowMs)
    {
        var (travel, needed) = Presets[Math.Clamp(Sensitivity, 0, Presets.Length - 1)];
        bool shaken = _x.Feed(x, nowMs, travel, needed) | _y.Feed(y, nowMs, travel, needed);  // non-short-circuit: both axes keep tracking
        if (shaken) { _x.Reset(); _y.Reset(); }
        return shaken;
    }

    sealed class Axis
    {
        readonly Queue<long> _reversals = new();
        bool _started;
        int _direction;   // -1, 0 or +1: current swing direction
        int _extreme;     // furthest point reached in the current direction
        long _lastMs;

        public void Reset()
        {
            _reversals.Clear();
            _direction = 0;
            _started = false;
        }

        public bool Feed(int value, long nowMs, int minTravel, int needed)
        {
            if (!_started || nowMs - _lastMs > WindowMs)
            {
                Reset();
                _started = true;
                _extreme = value;
            }
            _lastMs = nowMs;

            if (_direction == 0)
            {
                if (Math.Abs(value - _extreme) >= minTravel) { _direction = Math.Sign(value - _extreme); _extreme = value; }
                return false;
            }

            if ((value - _extreme) * _direction > 0) { _extreme = value; return false; }        // still going the same way

            if (Math.Abs(value - _extreme) >= minTravel)                                           // swung back far enough
            {
                _direction = -_direction;
                _extreme = value;
                _reversals.Enqueue(nowMs);
                while (_reversals.Count > 0 && nowMs - _reversals.Peek() > WindowMs) _reversals.Dequeue();
                return _reversals.Count >= needed;
            }
            return false;
        }
    }
}
