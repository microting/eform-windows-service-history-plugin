namespace HistorySql
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddingResetValuesFieldId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.check_lists", "reset_values_field_id", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.check_lists", "reset_values_field_id");
        }
    }
}
