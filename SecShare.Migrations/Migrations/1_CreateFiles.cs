using FluentMigrator;
using SecShare.Migrations.Code;

namespace SecShare.Migrations.Migrations;

[Migration(1)]
public class _1_CreateFiles : MyMigration
{
    public override void Up()
    {
        Create.Table("files")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("storage_path").AsString(2048).NotNullable()
            .WithColumn("extension").AsString(32).Nullable()
            .WithColumn("mime_type").AsString(255).NotNullable()
            .WithColumn("original_file_name").AsString(1024).NotNullable()
            .WithColumn("size").AsInt64().NotNullable()
            .WithColumn("encryption_algorithm").AsString(128).Nullable()
            .WithColumn("encryption_key_id").AsString(256).Nullable()
            .WithColumn("created_at").AsDateTime2().NotNullable()
            .WithColumn("updated_at").AsDateTime2().Nullable()
            .WithColumn("deleted_at").AsDateTime2().Nullable();

        Create.Index("ix_files_created_at")
            .OnTable("files")
            .OnColumn("created_at").Descending();
    }

    public override void Down()
    {
        Delete.Table("files");
    }
}
