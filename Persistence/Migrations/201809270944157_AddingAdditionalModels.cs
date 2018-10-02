namespace HistorySql
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingAdditionalModels : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.log_exceptions",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        created_at = c.DateTime(nullable: false),
                        level = c.Int(nullable: false),
                        type = c.String(),
                        message = c.String(),
                    })
                .PrimaryKey(t => t.id);
            
            CreateTable(
                "dbo.logs",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        created_at = c.DateTime(nullable: false),
                        level = c.Int(nullable: false),
                        type = c.String(),
                        message = c.String(),
                    })
                .PrimaryKey(t => t.id);
            
            CreateTable(
                "dbo.settings",
                c => new
                    {
                        id = c.Int(nullable: false),
                        name = c.String(nullable: false, maxLength: 50),
                        value = c.String(),
                    })
                .PrimaryKey(t => t.id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.settings");
            DropTable("dbo.logs");
            DropTable("dbo.log_exceptions");
        }
    }
}
