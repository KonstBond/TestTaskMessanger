using TestTaskMessanger.Dbl.Data.Entities;

namespace TestTaskMessanger.Dbl.Exceptions
{
    public class NotFoundException : Exception
    {
        public object? Entity { get; }

        public NotFoundException()
            : this("Entity not found") { }

        public NotFoundException(string message)
            : base(message) { }

        public NotFoundException(string message, object? entity)
            : base(message)
        {
            Entity = entity;
        }
    }
}
