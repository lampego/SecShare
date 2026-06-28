using FluentMigrator;
using SecShare.Migrations.Code;

namespace SecShare.Migrations.Migrations;

[Migration(4)]
public class _4_AddFileContentType : MyMigration
{
    private const string ForeignKeyName = "fk_files_content_type_storage_content_types";

    public override void Up()
    {
        Create.Table("storage_content_types").InSchema("enum")
            .WithColumn("id").AsInt16().PrimaryKey().NotNullable()
            .WithColumn("name").AsString(200).Unique().NotNullable();

        Insert.IntoTable("storage_content_types").InSchema("enum")
            .Row(new { id = 1, name = "Folder" })
            .Row(new { id = 2, name = "File" })
            .Row(new { id = 3, name = "Text" });

        Alter.Table("files")
            .AddColumn("content_type").AsInt16().NotNullable().WithDefaultValue(2);

        Create.ForeignKey(ForeignKeyName)
            .FromTable("files").ForeignColumn("content_type")
            .ToTable("storage_content_types").InSchema("enum").PrimaryColumn("id");
    }

    public override void Down()
    {
        Delete.ForeignKey(ForeignKeyName).OnTable("files");
        Delete.Column("content_type").FromTable("files");
        Delete.Table("storage_content_types").InSchema("enum");
    }
}
