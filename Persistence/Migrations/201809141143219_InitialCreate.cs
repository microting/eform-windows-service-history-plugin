namespace HistorySql
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.check_lists",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        workflow_state = c.String(maxLength: 255),
                        version = c.Int(),
                        created_at = c.DateTime(),
                        updated_at = c.DateTime(),
                        sdk_check_list_id = c.Int(),
                    })
                .PrimaryKey(t => t.id);
            
            CreateTable(
                "dbo.fields",
                c => new
                    {
                        id = c.Int(nullable: false, identity: true),
                        workflow_state = c.String(maxLength: 255),
                        version = c.Int(),
                        created_at = c.DateTime(),
                        updated_at = c.DateTime(),
                        check_list_id = c.Int(),
                        sdk_field_id = c.Int(),
                    })
                .PrimaryKey(t => t.id)
                .ForeignKey("dbo.check_lists", t => t.check_list_id)
                .Index(t => t.check_list_id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.fields", "check_list_id", "dbo.check_lists");
            DropIndex("dbo.fields", new[] { "check_list_id" });
            DropTable("dbo.fields");
            DropTable("dbo.check_lists");
        }
    }
}
