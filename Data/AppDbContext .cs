using Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Test> Tests => Set<Test>();
        public DbSet<Theory> Theories => Set<Theory>();
        public DbSet<TheoryTestRelation> TheoryTestRelations => Set<TheoryTestRelation>();
        public DbSet<FileP> Files => Set<FileP>();
        public DbSet<Section> Sections => Set<Section>();

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileP>(entity =>
            {
                entity.ToTable("Files");
                entity.Property(f => f.Name).HasMaxLength(256).IsRequired();
                entity.Property(f => f.FilePath).HasMaxLength(1024).IsRequired();
            });

            // Настройка связи Test -> File
            modelBuilder.Entity<Test>()
                .HasOne(t => t.File)
                .WithMany()
                .HasForeignKey(t => t.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связи Theory -> File
            modelBuilder.Entity<Theory>()
                .HasOne(t => t.File)
                .WithMany()
                .HasForeignKey(t => t.FileId)
                .OnDelete(DeleteBehavior.Cascade);


            // Настройка связи многие-ко-многим
            modelBuilder.Entity<TheoryTestRelation>(entity =>
            {
                entity.HasOne(ttr => ttr.Test)
                      .WithMany(t => t.TheoryTestRelations)
                      .HasForeignKey(ttr => ttr.TestId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ttr => ttr.Theory)
                      .WithMany(t => t.TheoryTestRelations)
                      .HasForeignKey(ttr => ttr.TheoryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка Section (самоссылающаяся связь)
            modelBuilder.Entity<Section>(entity =>
            {
                entity.ToTable("Sections");
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Name).HasMaxLength(200).IsRequired();

                // Связь с родительским разделом
                entity.HasOne(s => s.ParentSection)
                      .WithMany(s => s.ChildSections)
                      .HasForeignKey(s => s.ParentSectionId)
                      .OnDelete(DeleteBehavior.Restrict); // Запрещаем каскадное удаление

                // Навигационные свойства для Theories и Tests
                entity.HasMany(s => s.Theories)
                      .WithOne(t => t.Section)
                      .HasForeignKey(t => t.SectionId)
                      .OnDelete(DeleteBehavior.SetNull); // При удалении раздела, теории остаются без раздела

                entity.HasMany(s => s.Tests)
                      .WithOne(t => t.Section)
                      .HasForeignKey(t => t.SectionId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Индекс для быстрого поиска
                entity.HasIndex(s => s.ParentSectionId);
                entity.HasIndex(s => s.OrderBy);
            });
        }
    }
}

