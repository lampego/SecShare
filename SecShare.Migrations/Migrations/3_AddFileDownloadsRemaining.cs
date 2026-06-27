using FluentMigrator;
using SecShare.Migrations.Code;

namespace SecShare.Migrations.Migrations;

[Migration(3)]
public class _3_AddFileDownloadsRemaining : MyMigration
{
    public override void Up()
    {
        Alter.Table("files")
            .AddColumn("downloads_remaining").AsInt32().NotNullable().WithDefaultValue(1);
    }

    public override void Down()
    {
        Delete.Column("downloads_remaining").FromTable("files");
    }
}
