using System.Collections.Generic;
using UnityEngine;

namespace MotelSimulator.Data
{
    [System.Serializable]
    public class RoomSaveData
    {
        public string roomId;
        public RoomType roomType;
        // Grid coords of the 4 corners (min and max define the rect)
        public int gridX;
        public int gridY;
        public int gridWidth;
        public int gridHeight;
    }

    [System.Serializable]
    public class ApartmentSaveData
    {
        public string apartmentId;
        public string apartmentName;
        public int floorNumber;
        public int unitNumber;
        public List<RoomSaveData> rooms = new List<RoomSaveData>();
    }

    [System.Serializable]
    public class MotelSaveData
    {
        public string motelName = "My Motel";
        public List<ApartmentSaveData> apartments = new List<ApartmentSaveData>();
        public long lastSaveTimestamp;
    }
}
