import { User } from "@/types/auth";

export type UpdateUserRequest = Partial<User>;
export type GetUserRequest = Pick<User, "id">;
export type DeleteUserRequest = Pick<User, "id">;