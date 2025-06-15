using Intern.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Intern.BackgroundServices

{
    public class RoomStatusBackgroundService :BackgroundService
    {
        private readonly ILogger<RoomStatusBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Check every minute

        public RoomStatusBackgroundService(
      IServiceProvider serviceProvider,
      ILogger<RoomStatusBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Room Status Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<InternContext>();

                    var now = DateTime.UtcNow;

                    // Find rooms with meetings that have ended but still marked as "Not Available"
                    var expiredMeetings = await dbContext.Meetings
                        .Include(m => m.Room)
                        .Where(m => m.EndTime <= now && m.Room.Status == "Not Available")
                        .ToListAsync(stoppingToken);

                    foreach (var meeting in expiredMeetings)
                    {
                        meeting.Room.Status = "Available";
                        _logger.LogInformation($"Room {meeting.Room.Id} status updated to Available");
                    }

                    if (expiredMeetings.Any())
                    {
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in Room Status Background Service");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Room Status Background Service is stopping.");
        }

    }
}
