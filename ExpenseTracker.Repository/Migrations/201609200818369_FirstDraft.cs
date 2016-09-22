namespace ExpenseTracker.Repository.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FirstDraft : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ExpenseGroup",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        UserId = c.String(nullable: false, maxLength: 100),
                        Title = c.String(nullable: false, maxLength: 50),
                        Description = c.String(nullable: false, maxLength: 250),
                        ExpenseGroupStatusId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ExpenseGroupStatus", t => t.ExpenseGroupStatusId)
                .Index(t => t.ExpenseGroupStatusId);
            
            CreateTable(
                "dbo.ExpenseGroupStatus",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Description = c.String(nullable: false, maxLength: 50),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Expense",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Description = c.String(nullable: false, maxLength: 100),
                        Date = c.DateTime(nullable: false, storeType: "date"),
                        Amount = c.Decimal(nullable: false, precision: 18, scale: 0),
                        ExpenseGroupId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.ExpenseGroup", t => t.ExpenseGroupId, cascadeDelete: true)
                .Index(t => t.ExpenseGroupId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Expense", "ExpenseGroupId", "dbo.ExpenseGroup");
            DropForeignKey("dbo.ExpenseGroup", "ExpenseGroupStatusId", "dbo.ExpenseGroupStatus");
            DropIndex("dbo.Expense", new[] { "ExpenseGroupId" });
            DropIndex("dbo.ExpenseGroup", new[] { "ExpenseGroupStatusId" });
            DropTable("dbo.Expense");
            DropTable("dbo.ExpenseGroupStatus");
            DropTable("dbo.ExpenseGroup");
        }
    }
}
