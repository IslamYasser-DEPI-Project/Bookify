using System;

namespace Bookify.Application.Exceptions
{
    public class BookingException : Exception
    {
        public BookingException() { }
        public BookingException(string message) : base(message) { }
        public BookingException(string message, Exception inner) : base(message, inner) { }
    }
}
