namespace shared
{
    public struct Message<TBody>
    {
        public string Name { get; }
        public TBody Body { get; }

        public Message(string name, TBody body)
        {
            Name = name;
            Body = body;
        }
    }
}
