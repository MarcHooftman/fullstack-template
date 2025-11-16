import { User } from "@/types/auth";


// Defined in Api as:
// public record UserDto(
//     string Id,
//     string Email,
//     string Name,
//     Role Role, <- enum: SuperAdmin, Admin, User
//     bool Active,
// );

export type UserResponse = Readonly<User>;