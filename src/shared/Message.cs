namespace shared
{
    public struct Message
    {
        public string Name { get; }
        public byte[] Data { get; }

        public Message(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }
    }
}
