using System.Text.Json;

namespace somelib
{
    public record Thingamajig<T>
    {
        private readonly List<T> _list = new();

        public Thingamajig(params T[] initvals)
        {
            _list.AddRange(initvals);
        }

        public void Add(T x)
        {
            _list.Add(x);
        }

        public int Count => _list.Count;

        public override string ToString()
        {
            return JsonSerializer.Serialize(_list);
        }
    }
}