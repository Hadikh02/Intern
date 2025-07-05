using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Intern.Models;

public partial class InternContext : DbContext
{
    public InternContext()
    {
    }

    public InternContext(DbContextOptions<InternContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Agenda> Agenda { get; set; }
    public virtual DbSet<Meeting> Meetings { get; set; }
    public virtual DbSet<MeetingAttendee> MeetingAttendees { get; set; }
    public virtual DbSet<Minute> Minutes { get; set; }
    public virtual DbSet<Notification> Notifications { get; set; }
    public virtual DbSet<Room> Rooms { get; set; }
    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=InternDB");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TimeOnly to TimeSpan converter
        var timeOnlyConverter = new ValueConverter<TimeOnly, TimeSpan>(
            t => t.ToTimeSpan(),
            t => TimeOnly.FromTimeSpan(t)
        );

        modelBuilder.Entity<Agenda>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Agenda__3214EC0700586F3E");

            entity.Property(e => e.Description)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.ItemNumber)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Status)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Topic)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Meeting).WithMany(p => p.Agenda)
                .HasForeignKey(d => d.MeetingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Agenda_Meeting");
        });

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Meeting__3214EC07A4CD6D11");

            entity.ToTable("Meeting");

            entity.Property(e => e.StartTime)
                .HasConversion(timeOnlyConverter)
                .HasColumnType("time");

            entity.Property(e => e.EndTime)
                .HasConversion(timeOnlyConverter)
                .HasColumnType("time");

            entity.Property(e => e.RecordingPath)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Room).WithMany(p => p.Meetings)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Meeting__RoomId__412EB0B6");

            entity.HasOne(d => d.User).WithMany(p => p.Meetings)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Meeting__UserId__403A8C7D");
        });

        modelBuilder.Entity<MeetingAttendee>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__MeetingA__3214EC078877D9C0");

            entity.ToTable("MeetingAttendee");

            entity.Property(e => e.AttendanceStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Pending");

            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Meeting).WithMany(p => p.MeetingAttendees)
                .HasForeignKey(d => d.MeetingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MeetingAt__Meeti__49C3F6B7");

            entity.HasOne(d => d.User).WithMany(p => p.MeetingAttendees)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__MeetingAt__UserI__4AB81AF0");
        });

        modelBuilder.Entity<Minute>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Minute__3214EC07F2B69FFF");

            entity.ToTable("Minute");

            entity.Property(e => e.AssignAction)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.DueDate).HasColumnType("datetime");

            entity.HasOne(d => d.MeetingAttendee).WithMany(p => p.Minutes)
                .HasForeignKey(d => d.MeetingAttendeeId)
                .HasConstraintName("FK_Minute_MeetingAttendee");

            entity.HasOne(d => d.Meeting).WithMany(p => p.Minutes)
                .HasForeignKey(d => d.MeetingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Minute__MeetingI__45F365D3");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__20CF2E129E0B21BD");

            entity.ToTable("Notification");

            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.EventDescription)
                .HasMaxLength(2000)
                .IsUnicode(false);
            entity.Property(e => e.EventType)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.Meeting).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.MeetingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_Meeting");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Room__3214EC07F7CC2ACE");

            entity.ToTable("Room");

            entity.Property(e => e.Location)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.RoomNumber)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.User).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Room_User");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3214EC07C18C929F");

            entity.ToTable("User");

            entity.HasIndex(e => e.Email, "UQ__User__A9D105347D00AED9").IsUnique();

            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.UserType)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
