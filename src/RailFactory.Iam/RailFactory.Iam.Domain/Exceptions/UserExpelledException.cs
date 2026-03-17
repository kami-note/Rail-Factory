namespace RailFactory.Iam.Domain.Exceptions;

public class UserExpelledException : Exception
{
    public UserExpelledException() : base("This account has been expelled.") { }
    public UserExpelledException(string message) : base(message) { }
}
