namespace EventSourcing.Core.Snapshotting
{
    public interface ISnapshottable
    {
        public uint IntervalLength { get; }
        public ISnapshot CreateSnapshot();

        public bool IntervalExceeded(uint previousVersion, uint currentVersion)
        {
            var adjusted = previousVersion - previousVersion % IntervalLength;
            return IntervalLength <= currentVersion - adjusted;
        }
    }
}