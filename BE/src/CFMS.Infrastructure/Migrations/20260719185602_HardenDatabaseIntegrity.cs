using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CFMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HardenDatabaseIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repair legacy rows before the stricter constraints and index are applied.
            migrationBuilder.Sql(
                """
                UPDATE feedbacks
                SET "CategoryId" = (
                    SELECT "Id"
                    FROM feedback_categories
                    WHERE "Name" = 'Complaint'
                    ORDER BY "CreatedAtUtc"
                    LIMIT 1)
                WHERE "CategoryId" IS NULL;

                UPDATE feedbacks
                SET "Rating" = NULL
                WHERE "Rating" IS NOT NULL
                  AND ("Rating" < 1 OR "Rating" > 5);

                WITH ranked_assignments AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "FeedbackId"
                               ORDER BY "CreatedAtUtc" DESC, "Id" DESC) AS row_number
                    FROM feedback_assignments
                    WHERE "IsActive" = TRUE
                )
                UPDATE feedback_assignments AS assignment
                SET "IsActive" = FALSE
                FROM ranked_assignments AS ranked
                WHERE assignment."Id" = ranked."Id"
                  AND ranked.row_number > 1;
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_feedback_assignments_users_UserId",
                table: "feedback_assignments");

            migrationBuilder.DropIndex(
                name: "IX_feedback_assignments_FeedbackId",
                table: "feedback_assignments");

            migrationBuilder.DropIndex(
                name: "IX_feedback_assignments_UserId",
                table: "feedback_assignments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "feedback_assignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "feedbacks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_Role_Valid",
                table: "users",
                sql: "\"Role\" IN ('Customer', 'SupportStaff', 'DepartmentManager', 'SystemAdmin')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_users_Status_Valid",
                table: "users",
                sql: "\"Status\" IN ('Active', 'Disabled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_notifications_Type_Valid",
                table: "notifications",
                sql: "\"Type\" IN ('FeedbackSubmitted', 'FeedbackAssigned', 'FeedbackStatusChanged', 'FeedbackResponseAdded', 'FeedbackCommentAdded', 'FeedbackResolved', 'FeedbackRejected', 'FeedbackClosed', 'SystemAlert')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedbacks_Priority_Valid",
                table: "feedbacks",
                sql: "\"Priority\" IN ('Low', 'Medium', 'High', 'Urgent')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedbacks_Rating_Range",
                table: "feedbacks",
                sql: "\"Rating\" IS NULL OR \"Rating\" BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedbacks_Status_Valid",
                table: "feedbacks",
                sql: "\"Status\" IN ('New', 'Assigned', 'InProgress', 'WaitingForCustomer', 'Resolved', 'Rejected', 'Closed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedback_status_history_FromStatus_Valid",
                table: "feedback_status_history",
                sql: "\"FromStatus\" IN ('New', 'Assigned', 'InProgress', 'WaitingForCustomer', 'Resolved', 'Rejected', 'Closed')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_feedback_status_history_ToStatus_Valid",
                table: "feedback_status_history",
                sql: "\"ToStatus\" IN ('New', 'Assigned', 'InProgress', 'WaitingForCustomer', 'Resolved', 'Rejected', 'Closed')");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_assignments_FeedbackId",
                table: "feedback_assignments",
                column: "FeedbackId",
                unique: true,
                filter: "\"IsActive\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_users_Role_Valid",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_users_Status_Valid",
                table: "users");

            migrationBuilder.DropCheckConstraint(
                name: "CK_notifications_Type_Valid",
                table: "notifications");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedbacks_Priority_Valid",
                table: "feedbacks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedbacks_Rating_Range",
                table: "feedbacks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedbacks_Status_Valid",
                table: "feedbacks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedback_status_history_FromStatus_Valid",
                table: "feedback_status_history");

            migrationBuilder.DropCheckConstraint(
                name: "CK_feedback_status_history_ToStatus_Valid",
                table: "feedback_status_history");

            migrationBuilder.DropIndex(
                name: "IX_feedback_assignments_FeedbackId",
                table: "feedback_assignments");

            migrationBuilder.AlterColumn<Guid>(
                name: "CategoryId",
                table: "feedbacks",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "feedback_assignments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_feedback_assignments_FeedbackId",
                table: "feedback_assignments",
                column: "FeedbackId");

            migrationBuilder.CreateIndex(
                name: "IX_feedback_assignments_UserId",
                table: "feedback_assignments",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_feedback_assignments_users_UserId",
                table: "feedback_assignments",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id");
        }
    }
}
