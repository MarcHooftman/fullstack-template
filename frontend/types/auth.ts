import { Role } from "./enums/role";
import { Profile } from "./profile";

export type User = {
    id: string;
    email: string;
    name?: string;
    role?: Role;
    active?: boolean;
    profile?: Profile;
};

export type Token = string | null;

export type JwtPayload = {
    sub?: User['id'];
    iss?: string;
    aud?: string | string[];
    jti?: string;
    exp?: number;
    nbf?: number;
    iat?: number;
    // allow unknown extra claims
    [k: string]: unknown;
} & Pick<User, 'name' | 'role' | 'active'>;

export type AuthContextValue = {
    user: User | null;
    token: Token;
    loading: boolean;
    login: (token: NonNullable<Token>, profile?: Profile) => void;
    logout: () => void;
};