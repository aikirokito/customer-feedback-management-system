using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CFMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteMvpDataModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "feedbacks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "feedbacks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "feedback_categories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_departments_Name",
                table: "departments",
                column: "Name",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO departments ("Id", "Name", "Description", "IsActive", "CreatedAtUtc", "UpdatedAtUtc")
                VALUES ('11111111-1111-1111-1111-111111111111', 'Customer Support',
                        'Default department for feedback handling.', TRUE, NOW(), NOW())
                ON CONFLICT ("Name") DO NOTHING;

                INSERT INTO feedback_categories ("Id", "Name", "Description", "IsActive", "DepartmentId", "CreatedAtUtc", "UpdatedAtUtc")
                VALUES
                  ('10000000-0000-0000-0000-000000000001', 'Complaint', 'Customer complaints and service issues.', TRUE, '11111111-1111-1111-1111-111111111111', NOW(), NOW()),
                  ('10000000-0000-0000-0000-000000000002', 'Suggestion', 'Suggestions for improvement.', TRUE, '11111111-1111-1111-1111-111111111111', NOW(), NOW()),
                  ('10000000-0000-0000-0000-000000000003', 'Service', 'Feedback about services.', TRUE, '11111111-1111-1111-1111-111111111111', NOW(), NOW()),
                  ('10000000-0000-0000-0000-000000000004', 'Product', 'Feedback about products.', TRUE, '11111111-1111-1111-1111-111111111111', NOW(), NOW()),
                  ('10000000-0000-0000-0000-000000000005', 'Website', 'Feedback about the website.', TRUE, '11111111-1111-1111-1111-111111111111', NOW(), NOW())
                ON CONFLICT ("Name") DO NOTHING;

                UPDATE feedback_categories
                SET "DepartmentId" = '11111111-1111-1111-1111-111111111111'
                WHERE "DepartmentId" IS NULL;

                UPDATE feedbacks f
                SET "CategoryId" = c."Id",
                    "DepartmentId" = c."DepartmentId"
                FROM feedback_categories c
                WHERE LOWER(c."Name") = LOWER(f."Category");

                UPDATE feedbacks
                SET "CategoryId" = (SELECT "Id" FROM feedback_categories WHERE "Name" = 'Complaint' LIMIT 1),
                    "DepartmentId" = '11111111-1111-1111-1111-111111111111'
                WHERE "CategoryId" IS NULL;

                UPDATE users
                SET "DepartmentId" = '11111111-1111-1111-1111-111111111111'
                WHERE "Role" IN ('SupportStaff', 'DepartmentManager');
                """);

            migrationBuilder.DropIndex(
                name: "IX_feedbacks_Category",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "feedbacks");

            migrationBuilder.CreateIndex(
                name: "IX_users_DepartmentId",
                table: "users",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_CategoryId",
                table: "feedbacks",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_DepartmentId",
                table: "feedbacks",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_categories_DepartmentId",
                table: "feedback_categories",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_feedback_categories_departments_DepartmentId",
                table: "feedback_categories",
                column: "DepartmentId",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_feedbacks_departments_DepartmentId",
                table: "feedbacks",
                column: "DepartmentId",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_feedbacks_feedback_categories_CategoryId",
                table: "feedbacks",
                column: "CategoryId",
                principalTable: "feedback_categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_departments_DepartmentId",
                table: "users",
                column: "DepartmentId",
                principalTable: "departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_feedback_categories_departments_DepartmentId",
                table: "feedback_categories");

            migrationBuilder.DropForeignKey(
                name: "FK_feedbacks_departments_DepartmentId",
                table: "feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_feedbacks_feedback_categories_CategoryId",
                table: "feedbacks");

            migrationBuilder.DropForeignKey(
                name: "FK_users_departments_DepartmentId",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "feedbacks",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Complaint");

            migrationBuilder.Sql(
                """
                UPDATE feedbacks f
                SET "Category" = c."Name"
                FROM feedback_categories c
                WHERE f."CategoryId" = c."Id";
                """);

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropIndex(
                name: "IX_users_DepartmentId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_feedbacks_CategoryId",
                table: "feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_feedbacks_DepartmentId",
                table: "feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_feedback_categories_DepartmentId",
                table: "feedback_categories");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "users");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "feedbacks");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "feedback_categories");

            migrationBuilder.CreateIndex(
                name: "IX_feedbacks_Category",
                table: "feedbacks",
                column: "Category");
        }
    }
}
