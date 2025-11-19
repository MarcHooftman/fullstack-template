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

export class ApiErrorObject extends Error {
    message: string;
    statusCode: number;
    details?: string;
    errors?: Record<string, string[]>;
    constructor(message: string, statusCode: number, details?: string, errors?: Record<string, string[]>) {
        super(message);
        this.message = message;
        this.statusCode = statusCode;
        this.details = details;
        this.errors = errors;
    }
}