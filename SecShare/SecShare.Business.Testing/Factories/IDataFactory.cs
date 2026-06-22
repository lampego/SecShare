namespace SecShare.Business.Testing.Factories;

public interface IDataFactory<out T>
{
    T Generate();
}
