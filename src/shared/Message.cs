namespace shared
{
    public struct Message<TData>
    {
        public string Name { get; }
        public TData Data { get; }

        public Message(string name, TData data)
        {
            Name = name;
            Data = data;
        }
    }
}
