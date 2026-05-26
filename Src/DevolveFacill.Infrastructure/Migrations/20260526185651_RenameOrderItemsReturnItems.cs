using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevolveFacill.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameOrderItemsReturnItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnItems_OrderItems_OrderItemId",
                table: "ReturnItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ReturnItems_return_requests_ReturnRequestId",
                table: "ReturnItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReturnItems",
                table: "ReturnItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.RenameTable(
                name: "ReturnItems",
                newName: "return_items");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "order_items");

            migrationBuilder.RenameIndex(
                name: "IX_ReturnItems_ReturnRequestId",
                table: "return_items",
                newName: "IX_return_items_ReturnRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_ReturnItems_OrderItemId",
                table: "return_items",
                newName: "IX_return_items_OrderItemId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderId",
                table: "order_items",
                newName: "IX_order_items_OrderId");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "order_items",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "order_items",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Payload",
                table: "order_items",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "order_items",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "order_items",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_return_items",
                table: "return_items",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_order_items",
                table: "order_items",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_items_orders_OrderId",
                table: "order_items",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_return_items_order_items_OrderItemId",
                table: "return_items",
                column: "OrderItemId",
                principalTable: "order_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_return_items_return_requests_ReturnRequestId",
                table: "return_items",
                column: "ReturnRequestId",
                principalTable: "return_requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_items_orders_OrderId",
                table: "order_items");

            migrationBuilder.DropForeignKey(
                name: "FK_return_items_order_items_OrderItemId",
                table: "return_items");

            migrationBuilder.DropForeignKey(
                name: "FK_return_items_return_requests_ReturnRequestId",
                table: "return_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_return_items",
                table: "return_items");

            migrationBuilder.DropPrimaryKey(
                name: "PK_order_items",
                table: "order_items");

            migrationBuilder.RenameTable(
                name: "return_items",
                newName: "ReturnItems");

            migrationBuilder.RenameTable(
                name: "order_items",
                newName: "OrderItems");

            migrationBuilder.RenameIndex(
                name: "IX_return_items_ReturnRequestId",
                table: "ReturnItems",
                newName: "IX_ReturnItems_ReturnRequestId");

            migrationBuilder.RenameIndex(
                name: "IX_return_items_OrderItemId",
                table: "ReturnItems",
                newName: "IX_ReturnItems_OrderItemId");

            migrationBuilder.RenameIndex(
                name: "IX_order_items_OrderId",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderId");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "OrderItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<string>(
                name: "Sku",
                table: "OrderItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Payload",
                table: "OrderItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "OrderItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "OrderItems",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReturnItems",
                table: "ReturnItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnItems_OrderItems_OrderItemId",
                table: "ReturnItems",
                column: "OrderItemId",
                principalTable: "OrderItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ReturnItems_return_requests_ReturnRequestId",
                table: "ReturnItems",
                column: "ReturnRequestId",
                principalTable: "return_requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
