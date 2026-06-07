using UnityEngine;

namespace MotelSimulator.Data
{
    public enum RoomType
    {
        Unassigned = 0,
        Bedroom = 1,
        Bathroom = 2,
        Kitchen = 3,
        LivingRoom = 4,
        Storage = 5,
        Hallway = 6,
        Office = 7,
        Balcony = 8
    }

    [System.Serializable]
    public class RoomColorConfig
    {
        public RoomType roomType;
        public Color color;
        public string displayName;
    }

    [CreateAssetMenu(fileName = "RoomColorConfig", menuName = "MotelSimulator/Room Color Config")]
    public class RoomColorConfigSO : ScriptableObject
    {
        public RoomColorConfig[] roomColors = new RoomColorConfig[]
        {
            new RoomColorConfig { roomType = RoomType.Unassigned,  color = new Color(0.7f, 0.7f, 0.7f, 0.5f), displayName = "Unassigned"  },
            new RoomColorConfig { roomType = RoomType.Bedroom,     color = new Color(0.4f, 0.6f, 1.0f, 0.7f), displayName = "Bedroom"     },
            new RoomColorConfig { roomType = RoomType.Bathroom,    color = new Color(0.3f, 0.9f, 0.8f, 0.7f), displayName = "Bathroom"    },
            new RoomColorConfig { roomType = RoomType.Kitchen,     color = new Color(1.0f, 0.7f, 0.3f, 0.7f), displayName = "Kitchen"     },
            new RoomColorConfig { roomType = RoomType.LivingRoom,  color = new Color(0.5f, 0.9f, 0.4f, 0.7f), displayName = "Living Room" },
            new RoomColorConfig { roomType = RoomType.Storage,     color = new Color(0.7f, 0.5f, 0.3f, 0.7f), displayName = "Storage"     },
            new RoomColorConfig { roomType = RoomType.Hallway,     color = new Color(0.9f, 0.9f, 0.4f, 0.7f), displayName = "Hallway"     },
            new RoomColorConfig { roomType = RoomType.Office,      color = new Color(0.7f, 0.4f, 0.9f, 0.7f), displayName = "Office"      },
            new RoomColorConfig { roomType = RoomType.Balcony,     color = new Color(0.4f, 0.8f, 0.5f, 0.7f), displayName = "Balcony"     }
        };

        public Color GetColor(RoomType type)
        {
            foreach (var cfg in roomColors)
                if (cfg.roomType == type) return cfg.color;
            return Color.grey;
        }

        public string GetDisplayName(RoomType type)
        {
            foreach (var cfg in roomColors)
                if (cfg.roomType == type) return cfg.displayName;
            return "Unknown";
        }
    }
}
