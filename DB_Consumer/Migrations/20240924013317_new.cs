using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DBConsumer.Migrations
{
    /// <inheritdoc />
    public partial class @new : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Contacts",
                table: "Contacts");

            migrationBuilder.RenameTable(
                name: "Contacts",
                newName: "Contatos");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Contatos",
                newName: "Telefone");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Contatos",
                newName: "Nome");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Contatos",
                table: "Contatos",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Contatos",
                table: "Contatos");

            migrationBuilder.RenameTable(
                name: "Contatos",
                newName: "Contacts");

            migrationBuilder.RenameColumn(
                name: "Telefone",
                table: "Contacts",
                newName: "Phone");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "Contacts",
                newName: "Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Contacts",
                table: "Contacts",
                column: "Id");
        }
    }
}
