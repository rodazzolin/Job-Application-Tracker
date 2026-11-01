using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobTracker.Migrations
{
    /// <summary>
    /// Data-only migration: merges the Rejected and Withdrawn statuses into Archived.
    /// Statuses are stored as strings, so EF sees no schema change and this is written by hand.
    /// </summary>
    public partial class ConsolidateArchivedStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // History rows between two closed statuses (e.g. Rejected -> Withdrawn) would become
            // Archived -> Archived, which isn't a change, so drop them first.
            migrationBuilder.Sql("""
                DELETE FROM StatusChanges
                WHERE OldStatus IN ('Rejected', 'Withdrawn')
                  AND NewStatus IN ('Rejected', 'Withdrawn');
                """);

            migrationBuilder.Sql("""
                UPDATE JobApplications SET Status = 'Archived'
                WHERE Status IN ('Rejected', 'Withdrawn');
                """);

            migrationBuilder.Sql("""
                UPDATE StatusChanges SET OldStatus = 'Archived'
                WHERE OldStatus IN ('Rejected', 'Withdrawn');
                """);

            migrationBuilder.Sql("""
                UPDATE StatusChanges SET NewStatus = 'Archived'
                WHERE NewStatus IN ('Rejected', 'Withdrawn');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Lossy: the original Rejected/Withdrawn split can't be recovered, so everything
            // archived goes back to Rejected. Deleted Rejected <-> Withdrawn history isn't restored.
            migrationBuilder.Sql("""
                UPDATE JobApplications SET Status = 'Rejected'
                WHERE Status = 'Archived';
                """);

            migrationBuilder.Sql("""
                UPDATE StatusChanges SET OldStatus = 'Rejected'
                WHERE OldStatus = 'Archived';
                """);

            migrationBuilder.Sql("""
                UPDATE StatusChanges SET NewStatus = 'Rejected'
                WHERE NewStatus = 'Archived';
                """);
        }
    }
}
