using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DapperEfCoreSimple;

internal class AppDbContext : DbContext
{
    internal DbSet<Player> Players { get; set; }

    internal AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

    }
}

