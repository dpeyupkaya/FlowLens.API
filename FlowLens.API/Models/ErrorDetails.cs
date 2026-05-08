using System.Text.Json;

namespace FlowLens.API.Models
{
    public class ErrorDetails
    {
        public int StatusCode { get; set; }
        public string? Error { get; set; } 
        public string? Message { get; set; } 
        public string? Details { get; set; } 

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}