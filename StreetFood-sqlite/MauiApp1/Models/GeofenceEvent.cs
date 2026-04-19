using System;
using System.Collections.Generic;
using System.Text;

namespace MauiApp1.Models
{
    public class GeofenceEvent
    {
        public POI POI { get; set; } = null!;
        public GeofenceEventType EventType { get; set; }
    }
}
