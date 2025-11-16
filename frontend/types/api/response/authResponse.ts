import { UserResponse } from "./userResponse";


// Defined in Api as:
// public record LoginResponseDto(string Token, UserDto User);

export type LoginResponse = {
    token: string;
    user: UserResponse;
};
