﻿using System.Collections.Generic;
using System.Linq;

namespace QSB.WorldSync
{
    public static class WorldRegistry
    {
        private static readonly List<WorldObject> _worldObjects = new List<WorldObject>();

        public static void AddObject(WorldObject worldObject)
        {
            _worldObjects.Add(worldObject);
        }

        public static IEnumerable<T> GetObjects<T>()
        {
            return _worldObjects.OfType<T>();
        }

        public static T GetObject<T>(int id) where T : WorldObject
        {
            return GetObjects<T>().FirstOrDefault(x => x.ObjectId == id);
        }

    }
}
