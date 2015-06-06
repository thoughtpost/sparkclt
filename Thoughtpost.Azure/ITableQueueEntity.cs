using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Thoughtpost.Azure
{
    public interface ITableQueueEntity : ITableEntity
    {
        void SetKeys();

        string ExceptionDetails { get; set; }
    }
}
