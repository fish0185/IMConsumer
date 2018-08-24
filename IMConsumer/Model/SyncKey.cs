using System;
using System.Collections.Generic;
using System.Text;

namespace IMConsumer.Model
{
    public class SyncKey
    {
        public int Count { get; set; }

        public IEnumerable<SyncKeyKeyValue> List { get; set; }
    }

    public struct SyncKeyKeyValue
    {
        public int Key { get; set; }

        public int Val { get; set; }
    }
}
