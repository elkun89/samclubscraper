using System;
using scraper.Util;

namespace scraper
{
    public interface IStoreGetter
    {
        String StoreName { get; }

        void InitPageGetter();

        void Get();
    }
}