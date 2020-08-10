using System;

namespace Windows
{
    public struct LongFileTime
    {
#pragma warning disable CS0649
        /// <summary>
        /// 100-nanosecond intervals (ticks) since January 1, 1601 (UTC).
        /// </summary>
        internal long TicksSince1601;
#pragma warning restore CS0649

        internal DateTimeOffset ToDateTimeOffset() => new DateTimeOffset(DateTime.FromFileTimeUtc(TicksSince1601));
    }
}