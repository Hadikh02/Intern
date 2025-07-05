using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Intern.Hubs
{
    public class SignalingMessage
    {
        public string Type { get; set; } // "join", "offer", "answer", "ice-candidate", "media-update", "leave"
        public string MeetingId { get; set; }
        public string UserId { get; set; }
        public string ToUserId { get; set; }
        public object Payload { get; set; }
    }

    public class MediaUpdatePayload
    {
        public bool HasVideo { get; set; }
        public bool HasAudio { get; set; }
        public bool IsHandRaised { get; set; }
    }

    public class MeetingHub : Hub
    {
        // Thread-safe collections for connection tracking
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, string>> _meetingParticipants = new();

        private readonly ILogger<MeetingHub> _logger;

        public MeetingHub(ILogger<MeetingHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);

            var userEntry = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId);
            if (userEntry.Key != null)
            {
                _userConnections.TryRemove(userEntry.Key, out _);

                foreach (var meeting in _meetingParticipants)
                {
                    if (meeting.Value.TryRemove(Context.ConnectionId, out var userId))
                    {
                        if (meeting.Value.IsEmpty)
                        {
                            _meetingParticipants.TryRemove(meeting.Key, out _);
                        }

                        await Clients.Group(meeting.Key).SendAsync("UserLeft", new { userId });
                        _logger.LogInformation("User {UserId} left meeting {MeetingId}", userId, meeting.Key);
                    }
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendSignalingMessage(SignalingMessage message)
        {
            if (message == null || string.IsNullOrEmpty(message.Type))
            {
                Context.Abort();
                return;
            }

            _logger.LogDebug("Received message type: {Type} from {UserId} in meeting {MeetingId}",
                message.Type, message.UserId, message.MeetingId);

            try
            {
                switch (message.Type.ToLower())
                {
                    case "join":
                        await HandleJoin(message);
                        break;

                    case "offer":
                    case "answer":
                    case "ice-candidate":
                        await HandlePeerSignal(message);
                        break;

                    case "media-update":
                        await HandleMediaUpdate(message);
                        break;

                    case "leave":
                        await HandleLeave(message);
                        break;

                    default:
                        _logger.LogWarning("Unhandled message type: {Type}", message.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing signaling message");
                throw;
            }
        }

        private async Task HandleJoin(SignalingMessage message)
        {
            if (string.IsNullOrEmpty(message.UserId)) throw new ArgumentNullException(nameof(message.UserId));

            await Groups.AddToGroupAsync(Context.ConnectionId, message.MeetingId);
            _userConnections[message.UserId] = Context.ConnectionId;

            var participants = _meetingParticipants.GetOrAdd(message.MeetingId,
                new ConcurrentDictionary<string, string>());

            participants[Context.ConnectionId] = message.UserId;

            await Clients.GroupExcept(message.MeetingId, Context.ConnectionId)
                .SendAsync("UserJoined", new { userId = message.UserId });
        }

        private async Task HandlePeerSignal(SignalingMessage message)
        {
            if (string.IsNullOrEmpty(message.ToUserId)) return;

            if (_userConnections.TryGetValue(message.ToUserId, out var recipientConnectionId))
            {
                await Clients.Client(recipientConnectionId).SendAsync(message.Type, new
                {
                    fromUserId = message.UserId,
                    meetingId = message.MeetingId
                });
            }
            else
            {
                _logger.LogWarning("Recipient {ToUserId} not found", message.ToUserId);
            }
        }

        private async Task HandleMediaUpdate(SignalingMessage message)
        {
            if (string.IsNullOrEmpty(message.MeetingId)) return;

            await Clients.GroupExcept(message.MeetingId, Context.ConnectionId)
                .SendAsync("MediaUpdate", new
                {
                    userId = message.UserId,
                    hasVideo = (message.Payload as MediaUpdatePayload)?.HasVideo,
                    hasAudio = (message.Payload as MediaUpdatePayload)?.HasAudio,
                    isHandRaised = (message.Payload as MediaUpdatePayload)?.IsHandRaised
                });
        }

        private async Task HandleLeave(SignalingMessage message)
        {
            if (string.IsNullOrEmpty(message.MeetingId)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, message.MeetingId);
            await Clients.Group(message.MeetingId).SendAsync("UserLeft", new { userId = message.UserId });
        }
    }
}