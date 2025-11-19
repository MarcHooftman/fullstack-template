"use client";

import { useState } from "react";
import Link from "next/link";
import useAuth from "@/hooks/useAuth";
import useApi from "@/hooks/useApi";

const RegisterForm = () => {
    const { login } = useAuth();
    const { post } = useApi();

    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState("");

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError("");

        if (password !== confirmPassword) {
            setError("Passwords do not match.");
            return;
        }

        try {
            const response = await post("/api/register", { name, email, password });
            if (response?.token) {
                // log the user in immediately after successful registration
                login(response.token, response.profile);
            } else {
                setError(response?.message || "Registration failed. Please try again.");
            }
        } catch (err: any) {
            setError(err?.message || "An error occurred. Please try again.");
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <div>
                <label htmlFor="name">Name:</label>
                <input
                    type="text"
                    id="name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    required
                />
            </div>
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
            <div>
                <label htmlFor="confirmPassword">Confirm Password:</label>
                <input
                    type="password"
                    id="confirmPassword"
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                />
            </div>

            {error && <p style={{ color: "red" }}>{error}</p>}

            <button type="submit">Register</button>

            <p style={{ marginTop: "0.5rem" }}>
                Already have an account? <Link href="/login">Login</Link>
            </p>
        </form>
    );
};

export default RegisterForm;
