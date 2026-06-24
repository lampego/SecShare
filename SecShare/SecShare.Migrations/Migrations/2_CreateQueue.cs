using FluentMigrator;
using SecShare.Migrations.Code;

namespace SecShare.Migrations.Migrations;

[Migration(2)]
public class _2_CreateQueue : MyMigration
{
    public override void Up()
    {
        // 1. Create enum schema
        Execute.Sql("CREATE SCHEMA IF NOT EXISTS \"enum\";");

        // 2. Create enum tables
        Create.Table("queue_statuses").InSchema("enum")
            .WithColumn("id").AsInt16().PrimaryKey().NotNullable()
            .WithColumn("name").AsString(200).Unique().NotNullable();

        Insert.IntoTable("queue_statuses").InSchema("enum")
            .Row(new { id = 1, name = "Pending" })
            .Row(new { id = 2, name = "InProcess" })
            .Row(new { id = 3, name = "Success" })
            .Row(new { id = 4, name = "Fail" });

        Create.Table("queue_channels").InSchema("enum")
            .WithColumn("id").AsInt16().PrimaryKey().NotNullable()
            .WithColumn("name").AsString(200).Unique().NotNullable();

        Insert.IntoTable("queue_channels").InSchema("enum")
            .Row(new { id = 1, name = "Default" });

        Create.Table("queue_priorities").InSchema("enum")
            .WithColumn("id").AsInt16().PrimaryKey().NotNullable()
            .WithColumn("name").AsString(200).Unique().NotNullable();

        Insert.IntoTable("queue_priorities").InSchema("enum")
            .Row(new { id = 1, name = "Lowest" })
            .Row(new { id = 2, name = "Low" })
            .Row(new { id = 3, name = "Normal" })
            .Row(new { id = 4, name = "High" })
            .Row(new { id = 5, name = "Highest" });

        // 3. Create queues table
        Create.Table("queues")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("status").AsInt16().NotNullable().WithDefaultValue(1)
            .WithColumn("channel").AsInt16().NotNullable().WithDefaultValue(1)
            .WithColumn("priority").AsInt16().NotNullable().WithDefaultValue(3)
            .WithColumn("error").AsString(1000).Nullable()
            .WithColumn("context_type").AsString(512).NotNullable()
            .WithColumn("context_data").AsString(10000000).NotNullable() // Text/Large object
            .WithColumn("process_at").AsDateTime2().NotNullable()
            .WithColumn("created_at").AsDateTime2().NotNullable()
            .WithColumn("updated_at").AsDateTime2().Nullable()
            .WithColumn("deleted_at").AsDateTime2().Nullable();

        // 4. Create foreign keys
        Create.ForeignKey().FromTable("queues").ForeignColumn("status")
            .ToTable("queue_statuses").InSchema("enum").PrimaryColumn("id");

        Create.ForeignKey().FromTable("queues").ForeignColumn("channel")
            .ToTable("queue_channels").InSchema("enum").PrimaryColumn("id");

        Create.ForeignKey().FromTable("queues").ForeignColumn("priority")
            .ToTable("queue_priorities").InSchema("enum").PrimaryColumn("id");

        // 5. Add delete_at column to files table
        Alter.Table("files")
            .AddColumn("delete_at").AsDateTime2().Nullable();
    }

    public override void Down()
    {
        Delete.Column("delete_at").FromTable("files");
        Delete.Table("queues");
        Delete.Table("queue_priorities").InSchema("enum");
        Delete.Table("queue_channels").InSchema("enum");
        Delete.Table("queue_statuses").InSchema("enum");
        Execute.Sql("DROP SCHEMA IF EXISTS \"enum\";");
    }
}
