"use client";

import { useEffect, useMemo, useState, useCallback } from "react";
import type { ReactNode } from "react";
import { decodeToken, isTokenExpired, getToken, removeToken, setToken } from "@/lib/jwt";
import { User } from "@/types/auth";
import AuthContext from "@/contexts/AuthContext";

export const AuthProvider = ({ children }: { children: ReactNode }) => {
    const [token, setTokenState] = useState<string | null>(() => getToken());
    const [user, setUser] = useState<User | null>(() => {
        const t = getToken();
        if (!t) return null;
        try {
            const payload = decodeToken(t);
            return { ...payload } as User;
        } catch (e) {
            return null;
        }
    });
    const [loading, setLoading] = useState(false);

    const login = useCallback((t: string) => {
        try {
            setToken(t);
            setTokenState(t);
            const payload = decodeToken(t);
            setUser({ ...payload } as User);
            return true;
        } catch (err) {
            console.error("Invalid token provided to login", err);
            return false;
        }
    }, []);

    const logout = useCallback(() => {
        removeToken();
        setTokenState(null);
        setUser(null);
        // navigate to login page
        try {
            if (window.location.pathname !== "/login") {
                window.location.href = "/login";
            }
        } catch (e) {
            /* ignore */
        }
    }, []);

    useEffect(() => {
        const handler = () => {
            logout();
        };
        window.addEventListener("unauthorized", handler);
        return () => window.removeEventListener("unauthorized", handler);
    }, [logout]);


    // auto-logout on unauthorized event
    useEffect(() => {
        const handler = () => {
            logout();
        };
        window.addEventListener("unauthorized", handler);
        return () => window.removeEventListener("unauthorized", handler);
    }, [logout]);

    // auto-logout on token expiry
    useEffect(() => {
        if (!token) return;
        try {
            if (isTokenExpired(token)) {
                logout();
            }
        } catch (e) {
            console.warn("Error checking token expiry", e);
        }
    }, [token, logout]);

    const value = useMemo(() => ({ user, token, loading, login, logout }), [user, token, loading, login, logout]);

    return <AuthContext.Provider value={value}> {children} </AuthContext.Provider>;
};