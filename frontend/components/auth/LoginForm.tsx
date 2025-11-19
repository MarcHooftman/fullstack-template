"use client";

import { useState } from "react";
import Link from "next/link";
import useAuth from "@/hooks/useAuth";
import useApi from "@/hooks/useApi";

interface LoginFormProps {
    redirectTo?: string;
}

const LoginForm = ({ redirectTo }: LoginFormProps) => {
    const { login } = useAuth();
    const { post } = useApi();
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError("");

        try {
            const response = await post("/api/login", { email, password });
            if (response.token) {
                const success = login(response.token, response.profile);
                if (success && redirectTo) {
                    window.location.href = redirectTo;
                }
            } else {
                setError("Login failed. Please check your credentials.");
            }
        } catch (err: any) {
            setError(err?.message || "An error occurred. Please try again.");
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <div>
                <label htmlFor="email">Email:</label>
                <input
                    type="email"
                    id="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    required
                />
            </div>
            <div>
                <label htmlFor="password">Password:</label>
                <input
                    type="password"
                    id="password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    required
                />
            </div>
            {error && <p style={{ color: "red" }}>{error}</p>}
            <button type="submit">Login</button>

            <p style={{ marginTop: "0.5rem" }}>
                Don't have an account? <Link href="/register">Register</Link>
            </p>
        </form>
    );
};

export default LoginForm;