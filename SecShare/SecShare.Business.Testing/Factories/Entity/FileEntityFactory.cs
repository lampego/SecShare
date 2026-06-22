using Bogus;
using SecShare.Business.Orm.Entities;

namespace SecShare.Business.Testing.Factories.Entity;

internal class FileEntityFactory : IDataFactory<FileEntity>
{
    private readonly Faker<FileEntity> _factory;

    public FileEntityFactory()
    {
        _factory = new Faker<FileEntity>()
            .RuleFor(file => file.StoragePath, faker => $"files/{faker.Random.Guid()}.bin")
            .RuleFor(file => file.Extension, "bin")
            .RuleFor(file => file.MimeType, "application/octet-stream")
            .RuleFor(file => file.OriginalFileName, faker => faker.System.FileName("bin"))
            .RuleFor(file => file.Size, faker => faker.Random.Long(1, 1024))
            .RuleFor(file => file.CreatedAt, faker => faker.Date.Past().ToUniversalTime());
    }

    public FileEntity Generate()
    {
        return _factory.Generate();
    }
}
