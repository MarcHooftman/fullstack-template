"use client";

import type { ReactNode } from "react";
import useAuth from "@/hooks/useAuth";

export const SignedIn = ({ children }: { children?: ReactNode }) => {
    const { user } = useAuth();
    if (!user) return null;
    return <>{children}</>;
};

export default SignedIn;
