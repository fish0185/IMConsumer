using System.Collections.Generic;

namespace IMConsumer.Model
{
    public class SyncKey
    {
        public int Count { get; set; }

        public IEnumerable<SyncKeyKeyValue> List { get; set; }

        public override string ToString()
        {
            var _syncKey = "";
            foreach (var vk in List)
            {
                _syncKey += vk.Key + "_" + vk.Val + "%7C";
            }
            return _syncKey.TrimEnd('%', '7', 'C');
        }
    }

    public struct SyncKeyKeyValue
    {
        public int Key { get; set; }

        public int Val { get; set; }
    }
}
