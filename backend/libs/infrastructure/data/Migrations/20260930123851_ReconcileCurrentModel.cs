using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Nodra.Backend.Infra.Data.Postgres.Migrations
{
    /// <summary>
    /// Registra o modelo atual no snapshot após as migrations anteriores aplicarem o schema.
    /// </summary>
    /// <inheritdoc />
    public partial class ReconcileCurrentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // O schema correspondente já foi aplicado pelas migrations SQL anteriores.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Esta migration não altera o schema.
        }
    }
}
