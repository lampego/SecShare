using System.ComponentModel;
using System.Reflection;
using SecShare.Api.Common.Dto.Storage;
using SecShare.Api.Dto.RequestResponse.Storage;
using SecShare.Console.Commands;
using Spectre.Console.Cli;

namespace SecShare.Tests.Unit.Console;

public sealed class PasswordRemovalTests
{
    [Fact]
    public void UploadSettings_DoNotExposePasswordOptions()
    {
        var properties = typeof(UploadCommand.Settings).GetProperties();

        Assert.DoesNotContain(properties, property => ContainsPassword(property.Name));
        Assert.DoesNotContain(
            properties
                .SelectMany(property => property.GetCustomAttributes<CommandOptionAttribute>())
                .SelectMany(GetOptionNames),
            ContainsPassword
        );
        Assert.DoesNotContain(
            properties.SelectMany(property => property.GetCustomAttributes<DescriptionAttribute>()),
            attribute => ContainsPassword(attribute.Description)
        );
    }

    [Fact]
    public void ApiUploadContract_DoesNotExposePasswordFields()
    {
        Assert.DoesNotContain(
            typeof(UploadFileOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => ContainsPassword(property.Name)
        );
        Assert.DoesNotContain(
            typeof(UploadFileResponse).GetProperties(BindingFlags.Public | BindingFlags.Instance),
            property => ContainsPassword(property.Name)
        );
    }

    private static bool ContainsPassword(string value)
        => value.Contains("password", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> GetOptionNames(CommandOptionAttribute attribute)
    {
        foreach (var name in attribute.LongNames)
        {
            yield return name;
        }

        foreach (var name in attribute.ShortNames)
        {
            yield return name;
        }

        if (!string.IsNullOrWhiteSpace(attribute.ValueName))
        {
            yield return attribute.ValueName;
        }
    }
}
