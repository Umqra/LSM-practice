﻿using DataLayer.DataModel;
using DataLayer.Utilities;

namespace DataLayer.SortedDiskTable
{
    public class SortedDiskTableConfiguration
    {
        public IFile TableFile { get; set; }
        public int IndexSpanSize { get; set; }
        public IItemSerializer Serializer { get; set; }
    }
}