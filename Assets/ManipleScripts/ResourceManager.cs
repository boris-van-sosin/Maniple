using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Maniple
{

    public static class ResourceManager
    {
        public static class Controls
        {
            public static float MouseScrollSpeed { get { return 25; } }
            public static float KeyboardScrollSpeed { get { return 25; } }
            public static float GlobalScrollCoefficient { get { return 25; } }
            public static float MouseWheelVScrollSpeed { get { return 25; } }
            public static float MouseRotateSpeed { get { return 35; } }
            public static float KeyboardRotateSpeed { get { return 25; } }
            public static float GlobalRotateCoefficient { get { return 40; } }

            public static int ScrollMargin { get { return 15; } }

            public static float MinCameraHeight { get { return 3; } }
            public static float MaxCameraHeight { get { return 80; } } // was 40
        }

        public static class UISettings
        {
            public static int OrdersBarSize { get { return 80; } }
            public static int ResourceBarSize { get { return 80; } }
            public static int SelectedLabelSize { get { return 20; } }
            public static int SelectedLabelHOffset { get { return 20; } }

            public static int BuildAreaHeight { get { return 64; } }
            public static int CardSize { get { return 64; } }
            public static int CardPadding { get { return 2; } }

            public static Rect ResourceSupplyIconRect { get { return _resSupplyIconRect; } }
            public static Rect ResourceSupplyAmountRect { get { return _resSupplyAmountRect; } }

            private static readonly Rect _resSupplyIconRect = new Rect(10, 10, 32, 32);
            private static readonly Rect _resSupplyAmountRect = new Rect(45, 10, 128, 32);
        }

        public static class Production
        {
            public static int UnitTrainCoefficeient { get { return 1; } }

            public static float DefaultProdTime { get { return 1.0f; } }

            public static IEnumerable<GameObject> GetBuilding(string name)
            {
                return _prototypes.GetBuilding(name);
            }

            public static IEnumerable<GameObject> GetUnit(string name)
            {
                return _prototypes.GetUnit(name);
            }

            public static IEnumerable<GameObject> GetWorldObject(string name)
            {
                return _prototypes.GetWorldObject(name);
            }

            public static GameObject GetOtherObject(string name)
            {
                return _prototypes.GetOtherObject(name);
            }

            public static GameObject GetPlayerObject()
            {
                return _prototypes.GetPlayerObject();
            }

            public static Texture2D GetCard(string name)
            {
                return _prototypes.GetCard(name);
            }

            public static Sprite GetCardSprite(string name)
            {
                return _prototypes.GetCardSprite(name);
            }
        }

        public static void SetPrototypeList(Prototypes p)
        {
            if (_prototypes == null)
            {
                _prototypes = p;
            }
        }

        private static Prototypes _prototypes = null;

        public static T GetRandom<T>(IEnumerable<T> lst)
        {
            int numElems = lst.Count();
            if (numElems == 0)
            {
                return lst.ElementAt(10000);
            }
            return lst.ElementAt(UnityEngine.Random.Range(0, numElems));
        }

        public static string GetPath(this Transform current)
        {
            if (current.parent == null)
                return "/" + current.name;
            return current.parent.GetPath() + "/" + current.name;
        }

        public static int ReinforceRadius { get { return 5; } }
    }

    public class ClickHitObject
    {
        public GameObject HitObject { get; set; }
        public Vector3 HitLocation { get; set; }
    }

    public static class GameResources
    {
        public enum ResourceType { Money };

        public static Dictionary<ResourceType, int> MaxResources()
        {
            return new Dictionary<ResourceType, int>()
                {
                    { ResourceType.Money, 100000 }
                };
        }

        public static Dictionary<ResourceType, int> StartResources()
        {
            return new Dictionary<ResourceType, int>()
                {
                    { ResourceType.Money, 2000 }
                };
        }

        public static string ResourceName(ResourceType t)
        {
            return _resourceNames[t];
        }

        private static Dictionary<ResourceType, string> _resourceNames = new Dictionary<ResourceType, string>()
                {
                    { ResourceType.Money, "Supply" }
                };

    }

    public sealed class Tuple<T1, T2>
    {
        public Tuple(T1 first, T2 second)
        {
            Item1 = first;
            Item2 = second;
        }

        public static Tuple<T1,T2> Create(T1 first, T2 second)
        {
            return new Tuple<T1, T2>(first, second);
        }

        public override bool Equals(object obj)
        {
            Tuple<T1, T2> other = obj as Tuple<T1, T2>;

            if (other == null)
            {
                return false;
            }

            return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2);
        }

        public override int GetHashCode()
        {
            return Item1.GetHashCode() ^ Item2.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", Item1, Item2);
        }

        public readonly T1 Item1;
        public readonly T2 Item2;
    }

    public sealed class Tuple<T1, T2, T3>
    {
        public Tuple(T1 first, T2 second, T3 third)
        {
            Item1 = first;
            Item2 = second;
            Item3 = third;
        }

        public static Tuple<T1, T2, T3> Create(T1 first, T2 second, T3 third)
        {
            return new Tuple<T1, T2, T3>(first, second, third);
        }

        public override bool Equals(object obj)
        {
            Tuple<T1, T2, T3> other = obj as Tuple<T1, T2, T3>;

            if (other == null)
            {
                return false;
            }

            return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2) && this.Item3.Equals(other.Item3);
        }

        public override int GetHashCode()
        {
            return Item1.GetHashCode() ^ Item2.GetHashCode() ^ Item3.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", Item1, Item2, Item3);
        }

        public readonly T1 Item1;
        public readonly T2 Item2;
        public readonly T3 Item3;
    }

    public struct ValueTuple<T1, T2>
    {
        public ValueTuple(T1 first, T2 second)
        {
            Item1 = first;
            Item2 = second;
        }

        public static ValueTuple<T1, T2> Create(T1 first, T2 second)
        {
            return new ValueTuple<T1, T2>(first, second);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ValueTuple<T1,T2>))
            {
                return false;
            }
            ValueTuple<T1, T2> other = (ValueTuple<T1, T2>) obj;

            return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2);
        }

        public override int GetHashCode()
        {
            return Item1.GetHashCode() ^ Item2.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", Item1, Item2);
        }

        public readonly T1 Item1;
        public readonly T2 Item2;
    }

    public struct ValueTuple<T1, T2, T3>
    {
        public ValueTuple(T1 first, T2 second, T3 third)
        {
            Item1 = first;
            Item2 = second;
            Item3 = third;
        }

        public static ValueTuple<T1, T2, T3> Create(T1 first, T2 second, T3 third)
        {
            return new ValueTuple<T1, T2, T3>(first, second, third);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ValueTuple<T1, T2, T3>))
            {
                return false;
            }
            ValueTuple<T1, T2, T3> other = (ValueTuple<T1, T2, T3>)obj;

            return this.Item1.Equals(other.Item1) && this.Item2.Equals(other.Item2) && this.Item3.Equals(other.Item3);
        }

        public override int GetHashCode()
        {
            return Item1.GetHashCode() ^ Item2.GetHashCode() ^ Item3.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("({0},{1},{2})", Item1, Item2, Item3);
        }

        public readonly T1 Item1;
        public readonly T2 Item2;
        public readonly T3 Item3;
    }
}

