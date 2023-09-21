using Microsoft.AspNetCore.SignalR;

namespace course_project.Models
{
    public class CommentsHub : Hub
    {
        public async Task SendComment(string author, string text)
        {
            await Clients.All.SendAsync("NewComment", new { author, text });
        }
    }
}
