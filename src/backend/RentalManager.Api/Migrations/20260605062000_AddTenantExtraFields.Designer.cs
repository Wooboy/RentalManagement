using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RentalManager.Api.Data;

#nullable disable

namespace RentalManager.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260605062000_AddTenantExtraFields")]
    partial class AddTenantExtraFields
    {
    }
}
