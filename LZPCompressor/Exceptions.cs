using System;

namespace LZPCompressor
{
    public class BadInputException : ArgumentException
    {
        public BadInputException(string message) : base(message) { }
    }

    public class NotLZP1InputException : BadInputException
    {
        public NotLZP1InputException() : 
            base("input array is not LZP1 compressed data")
        { }
    }
}
