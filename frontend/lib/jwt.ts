import { TOKEN_KEY } from "@/consts/auth";
import { JwtPayload, Token, User } from "@/types/auth";

export function decodeToken(token: NonNullable<Token>): JwtPayload {
    const parts = token.split('.');
    if (parts.length < 2) throw new Error('Invalid token');
    try {
        const payload = JSON.parse(atob(parts[1]));
        return payload;
    } catch (e) {
        throw new Error('Failed to decode token payload');
    }
}

export function isTokenExpired(token: NonNullable<Token>): boolean {
    try {
        const payload = decodeToken(token);
        if (!payload.exp) return false;
        const now = Math.floor(Date.now() / 1000);
        return payload.exp < now;
    } catch (e) {
        return true;
    }
}

export function getToken(): Token {
    try {
        return localStorage.getItem(TOKEN_KEY);
    } catch (e) {
        return null;
    }
}

export function setToken(token: NonNullable<Token>) {
    try {
        localStorage.setItem(TOKEN_KEY, token);
    } catch (e) {
        // ignore
    }
}

export function removeToken() {
    try {
        localStorage.removeItem(TOKEN_KEY);
    } catch (e) {
        // ignore
    }
}

export function JwtPayloadToUser(payload: JwtPayload): Partial<User> {
    const { sub: id, name, role, active } = payload;
    return {
        id: id,
        name,
        role,
        active,
    };
}