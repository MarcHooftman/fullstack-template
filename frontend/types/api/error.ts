// Defined in Api as:
// public class ApiErrorDto
// {
//     public string Message { get; set; } = string.Empty;
//     public int StatusCode { get; set; }
//     public string? Details { get; set; }
//     public IDictionary<string, string[]>? Errors { get; set; }
// }

export type ApiError = {
    message: string;
    statusCode: number;
    details?: string;
    errors?: Record<string, string[]>;
};