using FluentMigrator;
using SecShare.Migrations.Code;

namespace SecShare.Migrations.Migrations;

[Migration(99999999)]
[Maintenance(MigrationStage.AfterAll)]
public class ApplyProceduresMigration : MyMigration
{
    public override void Up()
    {
        var sqlFilePath = Path.Combine(AppContext.BaseDirectory, "SQLFiles", "Procedures", "fn_queue_get_top.sql");
        if (File.Exists(sqlFilePath))
        {
            var sql = File.ReadAllText(sqlFilePath);
            Execute.Sql(sql);
        }
        else
        {
            throw new FileNotFoundException($"Stored procedure SQL file not found at: {sqlFilePath}");
        }
    }

    public override void Down()
    {
        Execute.Sql("DROP FUNCTION IF EXISTS fn_queue_get_top(int);");
    }
}
