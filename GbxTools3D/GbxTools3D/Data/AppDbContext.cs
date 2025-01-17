using Microsoft.EntityFrameworkCore;

namespace GbxTools3D.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
