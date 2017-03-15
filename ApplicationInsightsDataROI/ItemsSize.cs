using System.Threading;

namespace ApplicationInsightsDataROI
{
    class ItemsSize
    {
        private static ItemsSize _collectedItems;
        private static ItemsSize _sentItems;

        public int count;
        public int size;

        public static ItemsSize CollectedItems { get { return LazyInitializer.EnsureInitialized(ref _collectedItems); } }

        public static ItemsSize SentItems { get { return LazyInitializer.EnsureInitialized(ref _sentItems); } }
    }
}
